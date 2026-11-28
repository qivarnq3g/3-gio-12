using System;
using System.IO;
using BaGio12.Gameplay;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Graphical review only. Seed story prerequisites; exercise the actual HUD through keyboard input.
[InitializeOnLoad]
public static class NightPhoneUiRegression
{
    const string Flag = "NightPhoneUiReview";
    static Keyboard keyboard, competingKeyboard;
    static bool ownsKeyboard;
    static KeyboardState heldState;
    static double next, deadline;
    static int step;
    static NightOneDirector story;
    static NightTwoMorning night;
    static readonly string[] captures = { "n1-phone", "n2-alarm", "n2-ngan", "n2-own", "n3-alarm", "n3-stranger", "n3-profile" };
    static NightPhoneUiRegression()
    { if (SessionState.GetBool(Flag, false)) EditorApplication.playModeStateChanged += Changed; }
    public static void RunWithCompetingKeyboard()
    {
        SessionState.SetBool(Flag + "Competing", true);
        StartReview();
    }
    public static void Run()
    {
        SessionState.SetBool(Flag + "Competing", false);
        StartReview();
    }
    static void StartReview()
    {
        if (Application.isBatchMode) throw new Exception("UI review requires a graphical Editor.");
        string root = Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT");
        if (string.IsNullOrEmpty(root) || Path.GetFullPath(root).StartsWith(Application.persistentDataPath))
            throw new Exception("Use an isolated DATA_ROOT.");
        Directory.CreateDirectory(root);
        foreach (string file in captures) File.Delete(Path.Combine(root, file + ".png"));
        Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU", "1");
        SessionState.SetBool(Flag, true);
        EditorSceneManager.OpenScene(NightOneRealisticSetup.ScenePath);
        EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        if (SessionState.GetBool(Flag + "Competing", false))
        {
            competingKeyboard = InputSystem.AddDevice<Keyboard>("ReviewCurrentKeyboardOwner");
            InputSystem.onAfterUpdate += KeepCompetingKeyboardCurrent;
        }
        // Windows' physical hook owns Keyboard.current after every input update.
        // Feed the HUD's active input device, rather than an unrelated synthetic one.
        keyboard = competingKeyboard;
#if UNITY_EDITOR_WIN
        if (keyboard == null) keyboard = PhysicalKeyboardHook.Device;
#endif
        ownsKeyboard = keyboard == null;
        if (ownsKeyboard) keyboard = InputSystem.AddDevice<Keyboard>("PhoneUiReviewKeyboard");
        heldState = new KeyboardState();
        InputSystem.onBeforeUpdate += QueueHeldState;
        Debug.Log("PHONE_UI_REVIEW_INPUT_DEVICE " + keyboard.name + " owned=" + ownsKeyboard);
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.position = new Rect(50, 50, 1280, 800); view.Show(); view.Focus();
        step = 0; next = EditorApplication.timeSinceStartup + 2; deadline = next + 100;
        EditorApplication.update += Tick;
    }
    static void KeepCompetingKeyboardCurrent() => competingKeyboard?.MakeCurrent();
    static void Check(bool ok, string message) { if (!ok) throw new Exception("PHONE UI: " + message); }
    static void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(NightPersistence.DataRoot, name + ".png"));
    // Registered after the hook's Push callback so a physical state refresh cannot
    // overwrite the review's held key before the same input update reaches the HUD.
    static void QueueHeldState()
    { if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, heldState); }
    static void Key(bool down) => heldState = down ? new KeyboardState(UnityEngine.InputSystem.Key.Enter) : new KeyboardState();
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Phone UI timeout");
            if (EditorApplication.timeSinceStartup < next) return;
            story = UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
            var persistence = story == null ? null : story.GetComponent<NightPersistence>();
            if (persistence == null || !persistence.Ready) return;
            night = story.Morning; story.player.ResumeControl();
            switch (step++)
            {
                case 0:
                    Check(persistence.StartNewGame(), "new game"); story.State.hasNganPhone = true;
                    story.State.stage = NightStage.Room; story.player.SetPhone(1); break;
                case 1: Capture("n1-phone"); break;
                case 2: story.player.SetPhone(0); story.FinishNight(); Check(night.Begin(), "begin N2"); break;
                case 3: Capture("n2-alarm"); break;
                case 4: Key(true); break;
                case 5:
                    Key(false); Check(night.State.stage == MorningStage.Breakfast, "Enter did not dismiss N2 alarm");
                    story.player.SetPhone(1); break;
                case 6: Capture("n2-ngan"); break;
                case 7: story.player.SetPhone(2); break;
                case 8: Capture("n2-own"); break;
                case 9:
                    story.player.SetPhone(0); night.State.stage = MorningStage.Ended;
                    night.State.assemblySteps = 4; night.State.lampTested = true;
                    Check(night.BeginThree(), "begin N3"); break;
                case 10: Capture("n3-alarm"); break;
                case 11: Key(true); break;
                case 12:
                    Key(false); Check(night.Third.stage == NightThreeStage.ToGas, "Enter did not dismiss N3 alarm");
                    night.Third.stage = NightThreeStage.Evening; story.State.number_choice = "blocked";
                    night.Third.v1Delivered = true; story.player.SetPhone(1);
                    Check(night.OpenThreePhoneTab("stranger"), "N3 stranger tab"); break;
                case 13: Capture("n3-stranger"); break;
                case 14: Key(true); break;
                case 15:
                    Key(false); Check(story.State.number_choice != "blocked", "Enter did not activate unblock action");
                    heldState = new KeyboardState(UnityEngine.InputSystem.Key.D); break;
                case 16:
                    heldState = new KeyboardState();
                    var tab = typeof(NightTwoMorning).GetField("threeTab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    Check((string)tab.GetValue(night) == "profile", "D did not switch to profile tab"); break;
                case 17: Capture("n3-profile"); break;
                case 18:
                    foreach (string file in captures)
                        Check(File.Exists(Path.Combine(NightPersistence.DataRoot, file + ".png")), "missing " + file);
                    Debug.Log("NIGHT_PHONE_UI_KEYBOARD_CAPTURE_PASSED"); Finish(0); return;
            }
            next = EditorApplication.timeSinceStartup + 1;
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }
    static void Finish(int code)
    {
        InputSystem.onBeforeUpdate -= QueueHeldState;
        if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.onAfterUpdate -= KeepCompetingKeyboardCurrent;
        if (competingKeyboard != null) InputSystem.RemoveDevice(competingKeyboard);
        if (ownsKeyboard && keyboard != null) InputSystem.RemoveDevice(keyboard);
        SessionState.SetBool(Flag, false); EditorApplication.update -= Tick; EditorApplication.Exit(code);
    }
}
