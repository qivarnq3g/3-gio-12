#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BaGio12.Gameplay
{
    public static class PhysicalKeyboardHook
    {
        const int KeyboardLowLevel = 13;
        const uint MessageQuit = 0x12, MessageRehook = 0x8001;
        const uint KeyPacket = 0xE7, KeyBackspace = 0x08;
        const uint FlagExtended = 0x01, FlagUp = 0x80;

        [StructLayout(LayoutKind.Sequential)]
        struct KeyInfo { public uint vkCode, scanCode, flags, time; public IntPtr extraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        struct Message { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y; }
        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern int GetMessage(out Message message, IntPtr hwnd, uint min, uint max);
        [DllImport("user32.dll")] static extern bool PeekMessage(out Message message, IntPtr hwnd, uint min, uint max, uint remove);
        [DllImport("user32.dll")] static extern bool PostThreadMessage(uint thread, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint process);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll")] static extern uint GetCurrentProcessId();
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);

        static readonly HookProc Callback = OnKey;
        static readonly Key[] Plain = new Key[0x60], Extended = new Key[0x60];
        static readonly bool[] Pressed = new bool[512], Recovered = new bool[512];
        static readonly object Gate = new object();
        static readonly StringBuilder typed = new StringBuilder(), deleted = new StringBuilder(), added = new StringBuilder();
        static IntPtr hook;
        static Thread worker;
        static uint workerId, processId;
        static int revision, pushedRevision = -1;
        static Key lastRecovered = Key.None;
        static bool numberMarks;
        static Keyboard device;

        public static Keyboard Device => device;
        public static bool Hooked => hook != IntPtr.Zero;

        static PhysicalKeyboardHook()
        {
            Row(0x10, "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "LeftBracket", "RightBracket", "Enter", "LeftCtrl");
            Row(0x1E, "A", "S", "D", "F", "G", "H", "J", "K", "L", "Semicolon", "Quote", "Backquote", "LeftShift", "Backslash");
            Row(0x2C, "Z", "X", "C", "V", "B", "N", "M", "Comma", "Period", "Slash", "RightShift");
            Row(0x01, "Escape", "Digit1", "Digit2", "Digit3", "Digit4", "Digit5", "Digit6", "Digit7", "Digit8", "Digit9", "Digit0", "Minus", "Equals", "Backspace", "Tab");
            Row(0x38, "LeftAlt", "Space", "CapsLock", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10");
            Extended[0x1C] = Key.NumpadEnter; Extended[0x1D] = Key.RightCtrl; Extended[0x38] = Key.RightAlt;
            Extended[0x48] = Key.UpArrow; Extended[0x50] = Key.DownArrow; Extended[0x4B] = Key.LeftArrow; Extended[0x4D] = Key.RightArrow;
        }

        static void Row(int first, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++) Plain[first + i] = (Key)Enum.Parse(typeof(Key), keys[i]);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Start()
        {
            if (Application.isBatchMode || worker != null) return;
            processId = GetCurrentProcessId();
            device = InputSystem.AddDevice<Keyboard>("PhysicalKeyboard");
            InputSystem.onBeforeUpdate += Push;
            InputSystem.onAfterUpdate += KeepCurrent;
            Application.focusChanged += OnFocus;
            Application.quitting += Shutdown;
            using (var ready = new ManualResetEventSlim())
            {
                worker = new Thread(() => Run(ready)) { IsBackground = true, Name = "PhysicalKeyboardHook" };
                worker.Start();
                ready.Wait(1000);
            }
        }

        public static void Shutdown()
        {
            InputSystem.onBeforeUpdate -= Push;
            InputSystem.onAfterUpdate -= KeepCurrent;
            Application.focusChanged -= OnFocus;
            Application.quitting -= Shutdown;
            if (worker != null) { PostThreadMessage(workerId, MessageQuit, IntPtr.Zero, IntPtr.Zero); worker.Join(1000); worker = null; }
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            device = null;
            Clear();
        }

        static void Run(ManualResetEventSlim ready)
        {
            workerId = GetCurrentThreadId();
            PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
            Install();
            ready.Set();
            while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
                if (message.message == MessageRehook) Install();
            if (hook != IntPtr.Zero) UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
        }

        static void Install()
        {
            if (hook != IntPtr.Zero) UnhookWindowsHookEx(hook);
            hook = SetWindowsHookEx(KeyboardLowLevel, Callback, GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) Debug.LogWarning("PhysicalKeyboardHook: SetWindowsHookEx failed " + Marshal.GetLastWin32Error());
        }

        [AOT.MonoPInvokeCallback(typeof(HookProc))]
        static IntPtr OnKey(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                var info = (KeyInfo)Marshal.PtrToStructure(lParam, typeof(KeyInfo));
                lock (Gate) Handle(info);
            }
            return CallNextHookEx(hook, code, wParam, lParam);
        }

        static void Handle(KeyInfo info)
        {
            bool down = (info.flags & FlagUp) == 0;
            if (info.vkCode == KeyPacket)
            {
                if (down) Recover((char)info.scanCode);
                return;
            }
            if (info.vkCode == KeyBackspace)
            {
                if (down && typed.Length > 0) { deleted.Insert(0, typed[typed.Length - 1]); typed.Length--; }
                return;
            }
            deleted.Length = 0; added.Length = 0;
            var key = info.scanCode >= 0x60 ? Key.None : (info.flags & FlagExtended) != 0 ? Extended[info.scanCode] : Plain[info.scanCode];
            if (key == Key.None) return;
            int k = (int)key;
            if (down)
            {
                if (!OwnsForeground()) return;
                Type(key);
                if (!Pressed[k]) { Pressed[k] = true; revision++; }
                return;
            }
            if (!Pressed[k] && !Recovered[k])
            {
                numberMarks = key >= Key.Digit1 && key <= Key.Digit9;
                if (lastRecovered != Key.None) Recovered[(int)lastRecovered] = false;
            }
            Pressed[k] = false; Recovered[k] = false;
            if (lastRecovered == key) lastRecovered = Key.None;
            revision++;
        }

        static void Type(Key key)
        {
            if (key >= Key.A && key <= Key.Z) typed.Append((char)('a' + (key - Key.A)));
            else if (key >= Key.Digit1 && key <= Key.Digit0) typed.Append(key == Key.Digit0 ? '0' : (char)('1' + (key - Key.Digit1)));
            else if (key != Key.LeftShift && key != Key.RightShift) typed.Length = 0;
            if (typed.Length > 32) typed.Remove(0, typed.Length - 32);
        }

        static void Recover(char c)
        {
            typed.Append(c); added.Append(c);
            var key = VietnameseKeys.Trigger(deleted.ToString(), added.ToString(), numberMarks);
            if (key == Key.None || !OwnsForeground()) return;
            if (lastRecovered != Key.None && lastRecovered != key) Recovered[(int)lastRecovered] = false;
            Recovered[(int)key] = true; lastRecovered = key; revision++;
        }

        static bool OwnsForeground()
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint owner);
            return owner == processId;
        }

        static void OnFocus(bool focused)
        {
            if (!focused) Clear();
            else if (worker != null) PostThreadMessage(workerId, MessageRehook, IntPtr.Zero, IntPtr.Zero);
        }

        static void Clear()
        {
            lock (Gate)
            {
                Array.Clear(Pressed, 0, Pressed.Length); Array.Clear(Recovered, 0, Recovered.Length);
                typed.Length = 0; deleted.Length = 0; added.Length = 0; lastRecovered = Key.None; revision++;
            }
        }

        static void Push()
        {
            if (device == null || !device.added) return;
            var state = new KeyboardState();
            lock (Gate)
            {
                if (revision == pushedRevision) return;
                pushedRevision = revision;
                foreach (Key key in Enum.GetValues(typeof(Key)))
                {
                    int k = (int)key;
                    if (key != Key.None && k < Pressed.Length && (Pressed[k] || Recovered[k])) state.Set(key, true);
                }
            }
            InputSystem.QueueStateEvent(device, state);
        }

        static void KeepCurrent()
        {
            if (device != null && device.added && Keyboard.current != device) device.MakeCurrent();
        }
    }
}
#endif
