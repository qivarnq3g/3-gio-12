using System;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Run without -quit so the Play Mode checks can finish.
[InitializeOnLoad]
public static class EscAudioRegression
{
    const string SessionKey = "EscAudioRegression";
    static FirstPersonPlayer player;
    static PauseMenu menu;
    static Keyboard keyboard;
    static double next, deadline;
    static int step;
    static bool firstHum;

    static EscAudioRegression()
    {
        if (SessionState.GetBool(SessionKey, false))
            EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        try
        {
            Environment.SetEnvironmentVariable("THREEGIO12_DATA_ROOT",
                Path.GetFullPath(Path.Combine("Library", SessionKey, Guid.NewGuid().ToString("N"))));
            Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU", "1");
            EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");
            SessionState.SetBool(SessionKey, true);
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Fail(error); }
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        next = EditorApplication.timeSinceStartup;
        deadline = next + 60;
        EditorApplication.update += Tick;
    }

    static void Check(bool pass, string message)
    {
        if (!pass) throw new InvalidOperationException(message);
    }

    static void PressEsc() => InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
    static void ReleaseEsc() => InputSystem.QueueStateEvent(keyboard, new KeyboardState());

    static void CheckResumed()
    {
        Check(!player.IsPaused && Time.timeScale == 1 && !AudioListener.pause,
            "Esc did not restore gameplay and gameplay audio");
        Check(!HumPlaying(), "Pause-menu hum continued after resuming");
    }

    static bool HumPlaying() => menu.GetComponents<AudioSource>().Any(source =>
        source.loop && source.ignoreListenerPause && source.isPlaying && source.volume > 0);

    static bool CheckPaused(string note)
    {
        var canvas = menu.transform.Find("Pause menu");
        Check(player.IsPaused && Time.timeScale == 0 && AudioListener.pause,
            note + ": Esc did not pause gameplay and gameplay audio");
        Check(canvas != null && canvas.gameObject.activeInHierarchy, note + ": pause menu did not open");
        bool hum = HumPlaying();
        Debug.Log("ESC_AUDIO_REGRESSION " + note + " hum=" + hum);
        return hum;
    }

    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        double now = EditorApplication.timeSinceStartup;
        if (now < next) return;
        next = now + .35;
        try
        {
            Check(now < deadline, "Timed out waiting for Play Mode");
            switch (step)
            {
                case 0:
                    var persistence = UnityEngine.Object.FindFirstObjectByType<NightPersistence>();
                    if (persistence == null || !persistence.Ready) return;
                    player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
                    Check(player != null, "Player missing from Night One");
                    menu = player.GetComponent<PauseMenu>();
                    Check(menu != null, "Pause menu missing from player");
                    player.ResumeControl();
                    keyboard = InputSystem.AddDevice<Keyboard>();
                    keyboard.MakeCurrent();
                    break;
                case 1: CheckResumed(); PressEsc(); break;
                case 2: ReleaseEsc(); break;
                case 3: firstHum = CheckPaused("NOTE_1"); PressEsc(); break;
                case 4: ReleaseEsc(); break;
                case 5: CheckResumed(); PressEsc(); break;
                case 6: ReleaseEsc(); break;
                case 7:
                    bool secondHum = CheckPaused("NOTE_2");
                    Check(!firstHum && !secondHum, "A continuous pause-menu hum played after pressing Esc");
                    PressEsc();
                    break;
                case 8: ReleaseEsc(); break;
                case 9:
                    CheckResumed();
                    Debug.Log("ESC_AUDIO_REGRESSION_PASSED " + Application.unityVersion);
                    Finish(0);
                    return;
            }
            step++;
        }
        catch (Exception error) { Fail(error); }
    }

    static void Fail(Exception error)
    {
        Debug.LogError("ESC_AUDIO_REGRESSION_FAILED " + error.Message);
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
