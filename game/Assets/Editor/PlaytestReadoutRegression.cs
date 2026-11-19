using System;
using System.Linq;
using TMPro;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class PlaytestReadoutRegression
{
    const string SessionKey = "PlaytestReadoutRegression";
    const string ScenePath = "Assets/Scenes/Night01/NightOneRealistic.unity";

    static PlaytestReadout readout;
    static FirstPersonPlayer player;
    static Keyboard keyboard;
    static int step;
    static double next;

    static PlaytestReadoutRegression()
    {
        if (SessionState.GetBool(SessionKey, false))
            EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        Debug.Log("PLAYTEST_READOUT_REGRESSION_START");
        EditorSceneManager.OpenScene(ScenePath);
        SessionState.SetBool(SessionKey, true);
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            player = UnityEngine.Object.FindAnyObjectByType<FirstPersonPlayer>();
            Require(player != null, "FirstPersonPlayer missing from realistic scene");
            readout = player.GetComponent<PlaytestReadout>();
            Require(readout != null, "PlaytestReadout was not attached to player");
            keyboard = InputSystem.AddDevice<Keyboard>();
            step = 0;
            next = EditorApplication.timeSinceStartup + .5;
            EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail(ex); }
    }

    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            switch (step++)
            {
                case 0:
                    player.ResumeControl();
                    Require(!readout.IsVisible, "coordinates should be hidden by default");
                    PressF3();
                    break;
                case 1:
                    Require(readout.IsVisible && PanelIsActive(), "F3 did not show coordinates");
                    string value = readout.GetReadoutText();
                    Require(value.IndexOfAny(new[] { '\r', '\n' }) < 0 && value.Contains("X ") &&
                            value.Contains("Y ") && value.Contains("Z ") && value.Contains("Hướng") &&
                            value.Contains("Góc dọc"),
                        "readout should show coordinates and view direction on one line");
                    var labels = readout.GetComponentsInChildren<TextMeshProUGUI>();
                    var coordinates = labels.Single(label => label.name == "Coordinates");
                    var diagnostics = labels.Single(label => label.name == "Diagnostics");
                    Require(coordinates.text == value, "coordinate label differs from the public readout");
                    Require(diagnostics.text.Contains("Dưới chân:") && diagnostics.text.Contains("Đang nhìn:") &&
                            diagnostics.text.Contains("FPS") && diagnostics.text.Contains("F9 phòng 7") &&
                            diagnostics.text.Split('\n').Length == 4,
                        "F3 must retain all four diagnostic lines below the coordinates");
                    diagnostics.ForceMeshUpdate();
                    Require(!diagnostics.isTextTruncated && diagnostics.preferredHeight <= diagnostics.rectTransform.rect.height,
                        "diagnostic lines are clipped");
                    player.SetPaused(true);
                    PressF3();
                    break;
                case 2:
                    Require(player.IsPaused && readout.IsVisible && !PanelIsActive(), "F3 interfered with the pause menu");
                    player.ResumeControl();
                    PressF3();
                    break;
                case 3:
                    Require(!readout.IsVisible && !PanelIsActive(), "second F3 press did not hide coordinates");
                    Debug.Log("PLAYTEST_READOUT_REGRESSION_PASSED");
                    Finish(0);
                    break;
            }
        }
        catch (Exception ex) { Fail(ex); }
    }

    static void PressF3()
    {
        // A fast press and release can arrive in the same input update.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F3));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        next = EditorApplication.timeSinceStartup + .5;
    }

    static bool PanelIsActive() => player.transform.Find("Playtest readout").gameObject.activeInHierarchy;

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Issue 70: " + message);
    }

    static void Fail(Exception ex)
    {
        Debug.LogException(ex);
        Finish(1);
    }

    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= Changed;
        SessionState.SetBool(SessionKey, false);
        EditorApplication.Exit(code);
    }
}
