using System;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class PlayerTestSmoke
{
    static double next;
    static int stage, frame;
    static double nextCapture;
    static FirstPersonPlayer player;
    static Keyboard keyboard;
    static Vector3 initial;
    static float walked;
    static Key[] held = Array.Empty<Key>();
    static PlayerTestSmoke()
    {
        if (SessionState.GetBool("PlayerSmoke", false)) EditorApplication.playModeStateChanged += Changed;
    }
    public static void Record()
    {
        System.IO.Directory.CreateDirectory("/private/tmp/3gio12-recording");
        SessionState.SetBool("PlayerSmokeRecord", true);
        Run();
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PlayerTest.unity");
        SessionState.SetBool("PlayerSmoke", true);
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
        stage = 0; next = EditorApplication.timeSinceStartup + 1;
        EditorApplication.update += Tick;
    }
    static void Keys(params Key[] keys) { held = keys; }
    static void Require(bool pass, string message) { if (!pass) throw new Exception(message); }
    static void Teleport(Vector3 point)
    {
        var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
        player.transform.position = point; cc.enabled = true;
    }
    static void Tick()
    {
        if (SessionState.GetBool("PlayerSmokeRecord", false) && !Application.isBatchMode && EditorApplication.timeSinceStartup >= nextCapture)
        {
            ScreenCapture.CaptureScreenshot($"/private/tmp/3gio12-recording/{frame++:D5}.png");
            nextCapture = EditorApplication.timeSinceStartup + .1;
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(held));
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            switch (stage++)
            {
                case 0: player.ResumeControl(); initial = player.transform.position; Keys(Key.W); break;
                case 1:
                    if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("/private/tmp/3gio12-player.png");
                    walked = player.transform.position.z - initial.z;
                    Debug.Log($"SMOKE MOVE position={player.transform.position} initial={initial} keyboard={keyboard.wKey.isPressed} focus={Application.isFocused} dt={Time.deltaTime}");
                    Require(walked > 1, "W did not move player");
                    Keys(); Teleport(new Vector3(0,.1f,-5)); initial = player.transform.position; Keys(Key.W, Key.LeftShift); break;
                case 2:
                    Require(player.transform.position.z - initial.z > walked * 1.2f, "Sprint is not faster");
                    Require(player.transform.position.z < .7f, "Walked through closed door");
                    Keys(); Teleport(new Vector3(0,.1f,-3)); Keys(Key.C); break;
                case 3:
                    Require(player.IsCrouched, "C did not crouch"); Keys(); break;
                case 4:
                    Keys(Key.C); break;
                case 5:
                    Require(!player.IsCrouched, "C did not stand"); Keys();
                    Teleport(new Vector3(0,.1f,-.9f));
                    player.eyes.transform.localRotation = Quaternion.Euler(0,0,0);
                    Keys(Key.E); break;
                case 6:
                    Require(UnityEngine.Object.FindFirstObjectByType<TestDoor>().IsOpen, "E did not open door");
                    Keys(); Teleport(new Vector3(0,.1f,2.2f));
                    // Aim downward toward evidence; player look applies its own pitch, so inject mouse input later.
                    var pickup = UnityEngine.Object.FindFirstObjectByType<TestPickup>(); pickup.Interact(player);
                    Require(player.ItemsCollected == 1, "Pickup count incorrect");
                    Keys(Key.Digit1); break;
                case 7:
                    Require(player.ActivePhone == 1, "Phone 1 input failed");
                    Keys(Key.W); initial = player.transform.position; break;
                case 8:
                    if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("/private/tmp/3gio12-phone.png");
                    Require(Vector3.Distance(player.transform.position,initial) < .1f, "Player moved while on phone");
                    Require(Time.timeScale == 1, "Phone paused world"); Keys(Key.Digit2); break;
                case 9:
                    Require(player.ActivePhone == 2, "Phone 2 input failed"); Keys(Key.Escape); break;
                case 10:
                    Require(player.ActivePhone == 0, "Escape did not close phone");
                    Require(UnityEngine.Object.FindFirstObjectByType<BaGio12.Prototype.PhonePrototype>() == null, "Prototype covered test scene");
                    Debug.Log("PLAYER_RUNTIME_SMOKE_PASSED"); Finish(0); return;
            }
            next = EditorApplication.timeSinceStartup + 1;
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code)
    {
        Keys(); InputSystem.RemoveDevice(keyboard);
        SessionState.SetBool("PlayerSmokeRecord", false);
        SessionState.SetBool("PlayerSmoke", false); EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
}
