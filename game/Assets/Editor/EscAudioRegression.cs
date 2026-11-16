using System;
using System.IO;
using System.Reflection;
using BaGio12.Gameplay;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run without -quit: this test exits after its Play Mode checks complete.
[InitializeOnLoad]
public static class EscAudioRegression
{
    const string SessionKey = "EscAudioRegression";
    static FirstPersonPlayer player;
    static NightInjuryFeedback feedback;
    static AudioSource sound, breath;
    static double deadline;
    static int step;
    static (bool paused, float timeScale, bool listenerPaused, bool soundPlaying, bool breathPlaying) noteOne;

    static EscAudioRegression()
    {
        if (SessionState.GetBool(SessionKey, false)) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        try
        {
            // Keep preferences and automatic saves away from the player's real data.
            Environment.SetEnvironmentVariable("THREEGIO12_DATA_ROOT",
                Path.GetFullPath(Path.Combine("Library", "EscAudioRegressionData", Guid.NewGuid().ToString("N"))));
            Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU", "1");
            EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");
            SessionState.SetBool(SessionKey, true);
            EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        Application.runInBackground = true;
        step = 0;
        deadline = EditorApplication.timeSinceStartup + 60;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception("ESC AUDIO REGRESSION: " + message);
    }

    static AudioSource Source(object owner, string fieldName)
    {
        var field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Check(field != null, "Missing AudioSource field: " + fieldName);
        var source = field.GetValue(owner) as AudioSource;
        Check(source != null, "AudioSource was not initialized: " + fieldName);
        Check(!source.playOnAwake, fieldName + ".playOnAwake must be false");
        Check(!source.ignoreListenerPause, fieldName + " must respect AudioListener.pause");
        return source;
    }

    static void CheckPaused(string note)
    {
        feedback.ProcessMotion(Vector3.zero, false);
        Check(player.IsPaused, note + ": player is not paused");
        Check(Time.timeScale == 0, note + ": timeScale must be zero");
        Check(AudioListener.pause, note + ": AudioListener must be paused");
        Check(!sound.isPlaying && !breath.isPlaying, note + ": alarm and breathing must be silent");
    }

    static void CheckResumed()
    {
        Check(!player.IsPaused, "ResumeControl did not resume the player");
        Check(Time.timeScale == 1, "ResumeControl did not restore timeScale to one");
        Check(!AudioListener.pause, "ResumeControl left AudioListener paused");
    }

    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            Check(EditorApplication.timeSinceStartup <= deadline, "Timed out waiting for the scene");
            if (step == 0)
            {
                var director = UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
                player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
                feedback = UnityEngine.Object.FindFirstObjectByType<NightInjuryFeedback>();
                Check(director != null && player != null && feedback != null, "Required scene components are missing");
                if (director.GetComponent<NightPersistence>()?.Ready != true) return;
                Check(director.player == player && feedback.director == director, "Scene references do not match");
                sound = Source(director, "sound");
                breath = Source(feedback, "breath");
                player.ResumeControl();
                CheckResumed();
                Check(!sound.isPlaying && !breath.isPlaying, "Audio started automatically on scene entry");

                player.SetPaused(true); // NOTE 1: first Esc after opening the game.
                step = 1;
                return;
            }
            if (step == 1)
            {
                CheckPaused("NOTE 1");
                noteOne = (player.IsPaused, Time.timeScale, AudioListener.pause, sound.isPlaying, breath.isPlaying);
                player.SetPaused(true); // Repeated menu requests must not replace the saved state.
                player.ResumeControl();
                CheckResumed();
                player.ResumeControl();
                CheckResumed();

                player.SetPaused(true); // NOTE 2: Esc after returning to gameplay.
                step = 2;
                return;
            }

            CheckPaused("NOTE 2");
            var noteTwo = (player.IsPaused, Time.timeScale, AudioListener.pause, sound.isPlaying, breath.isPlaying);
            Check(noteTwo == noteOne, "NOTE 2 differs from NOTE 1");
            player.ResumeControl();
            CheckResumed();

            // Exercise the source-level guard without the listener masking a missing Pause().
            breath.Play();
            Check(breath.isPlaying, "Could not start the breathing pause fixture");
            player.SetPaused(true);
            AudioListener.pause = false;
            feedback.ProcessMotion(Vector3.zero, false);
            Check(!breath.isPlaying, "ProcessMotion did not pause active breathing");
            player.ResumeControl();
            breath.Stop();
            CheckResumed();

            // A menu must also preserve an existing slow-motion/audio-pause state.
            Time.timeScale = .5f;
            AudioListener.pause = true;
            player.SetPaused(true);
            player.SetPaused(true);
            player.ResumeControl();
            Check(!player.IsPaused && Time.timeScale == .5f && AudioListener.pause,
                "Pause/resume overwrote the previous global state");
            Time.timeScale = 1;
            AudioListener.pause = false;
            player.ResumeControl();
            CheckResumed();
            Debug.Log("ESC_AUDIO_REGRESSION_PASSED");
            Finish(0);
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }

    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= Changed;
        SessionState.SetBool(SessionKey, false);
        try { if (player != null) player.ResumeControl(); }
        catch (Exception error) { Debug.LogException(error); code = 1; }
        EditorApplication.Exit(code);
    }
}
