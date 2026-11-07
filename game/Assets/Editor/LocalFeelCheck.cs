using System;
using System.IO;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class LocalFeelCheck
{
    static double next;
    static int stage, steps;
    static FirstPersonPlayer player;
    static Keyboard keyboard;
    static Key[] held = Array.Empty<Key>();
    static float low, high, baseFov;
    static Vector3 start;

    static LocalFeelCheck()
    {
        if (SessionState.GetBool("LocalFeelCheck", false)) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RealisticPreview.unity");
        SessionState.SetBool("LocalFeelCheck", true);
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        Application.runInBackground = true;
        player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        player.GetComponent<PlayerCameraMotion>().Step += _ => steps++;
        stage = 0; next = EditorApplication.timeSinceStartup + 1;
        EditorApplication.update += Tick;
    }

    static void Window() { low = float.MaxValue; high = float.MinValue; steps = 0; start = player.transform.position; }

    static void Tick()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(held));
        if (pendingRelease && mouse != null && EditorApplication.timeSinceStartup > next - .15) { pendingRelease = false; InputSystem.QueueStateEvent(mouse, new MouseState { position = mouse.position.ReadValue() }); }
        EditorApplication.QueuePlayerLoopUpdate();
        float y = player.eyes.transform.localPosition.y;
        low = Mathf.Min(low, y); high = Mathf.Max(high, y);
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            switch (stage++)
            {
                case 0: player.ResumeControl(); baseFov = player.eyes.fieldOfView; held = Array.Empty<Key>(); next += 2.2; return;
                case 1: Capture("hint"); Window(); held = new[] { Key.W }; next = EditorApplication.timeSinceStartup + 2; return;
                case 2:
                    Debug.Log($"FEEL walk bob={high - low:F3} steps={steps} moved={(player.transform.position - start).magnitude:F2} fov={player.eyes.fieldOfView:F1}");
                    Window(); held = new[] { Key.W, Key.LeftShift }; next += 2; return;
                case 3:
                    Debug.Log($"FEEL run bob={high - low:F3} steps={steps} moved={(player.transform.position - start).magnitude:F2} fov={player.eyes.fieldOfView:F1} base={baseFov:F1} running={player.IsRunning}");
                    Capture("run"); held = Array.Empty<Key>(); next = EditorApplication.timeSinceStartup + 1.2; return;
                case 4: Window(); next += .8; return;
                case 5: Debug.Log($"FEEL idle bob={high - low:F3} steps={steps} fov={player.eyes.fieldOfView:F1}"); player.SetPaused(true); next += .8; return;
                case 6:
                    var menu = player.transform.Find("Pause menu");
                    Debug.Log($"FEEL menu active={menu != null && menu.gameObject.activeSelf} alpha={(menu != null ? menu.GetComponent<CanvasGroup>().alpha : -1):F2}");
                    Capture("menu"); script = 0; stage = 100; next = EditorApplication.timeSinceStartup + .3; return;
                case 100: RunScript(); stage = 100; return;
            }
        }
        catch (Exception e) { Debug.LogError("FEEL FAIL " + e); Finish(1); }
        next = EditorApplication.timeSinceStartup + 1;
    }

    static int script;
    static Mouse mouse;
    static readonly (string action, Key[] keys)[] Steps =
    {
        ("tap", new[] { Key.S }), ("tap", new[] { Key.Enter }), ("tap", new[] { Key.A }), ("log", null),
        ("tap", new[] { Key.S }), ("tap", new[] { Key.S }), ("tap", new[] { Key.S }), ("tap", new[] { Key.S }),
        ("tap", new[] { Key.D }), ("tap", new[] { Key.D }), ("log", null), ("wait", null), ("shot:peek", null),
        ("click", null), ("log", null), ("wait", null), ("shot:settings", null), ("tap", new[] { Key.Escape }), ("state", null),
        ("tap", new[] { Key.S }), ("wait", null), ("shot:controls", null), ("dark", null), ("wait", null), ("shot:dark", null),
        ("tap", new[] { Key.Escape }), ("state", null), ("end", null),
    };

    static void RunScript()
    {
        var pause = player.GetComponent<PauseMenu>();
        if (held.Length > 0) { held = Array.Empty<Key>(); next = EditorApplication.timeSinceStartup + .25; return; }
        var (action, keys) = Steps[script++];
        if (action.StartsWith("shot:")) { Capture(action.Substring(5)); next = EditorApplication.timeSinceStartup + 2.5; return; }
        next = EditorApplication.timeSinceStartup + .25;
        switch (action)
        {
            case "tap": held = keys; return;
            case "log": Debug.Log($"FEEL settings sensitivity={player.sensitivity:F3} fov={player.eyes.fieldOfView:F1} locked={player.PauseMenuLocked}"); return;
            case "wait": next = EditorApplication.timeSinceStartup + 1.5; return;
            case "dark": typeof(PauseMenu).GetMethod("SwitchLights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pause, new object[] { false }); return;
            case "state": Debug.Log($"FEEL state paused={player.IsPaused} locked={player.PauseMenuLocked}"); return;
            case "click":
                var minus = player.transform.Find("Pause menu/Light/Page Cài đặt/Row Độ nhạy chuột/Button <") as RectTransform;
                if (mouse == null) mouse = InputSystem.AddDevice<Mouse>();
                mouse.MakeCurrent();
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, minus.TransformPoint(minus.rect.center));
                Debug.Log($"FEEL click at={point} screen={Screen.width}x{Screen.height} cursor={Cursor.visible}");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point, delta = new Vector2(1, 0) });
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
                pendingRelease = true; return;
            case "end": PlayerPrefs.DeleteKey("player-settings-v1"); Finish(0); return;
        }
    }

    static bool pendingRelease;

    static void Capture(string name)
    {
        var cam = player.eyes;
        var canvases = player.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = .3f; }
        Canvas.ForceUpdateCanvases();
        var rt = new RenderTexture(1600, 900, 24);
        var data = cam.GetUniversalAdditionalCameraData(); var aa = data.antialiasing; data.antialiasing = AntialiasingMode.None;
        var previous = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = previous;
        data.antialiasing = aa;
        RenderTexture.active = rt;
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes($"Logs/Feel_{name}.png", tex.EncodeToPNG());
        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
    }

    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("LocalFeelCheck", false);
        EditorApplication.Exit(code);
    }
}
