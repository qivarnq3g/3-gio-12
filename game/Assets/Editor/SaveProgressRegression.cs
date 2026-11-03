using System;
using System.IO;
using BaGio12.Persistence;
using BaGio12.Story;
using BaGio12.Stealth;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run Write and Read in separate Unity processes with the same isolated DATA_ROOT.
[InitializeOnLoad]
public static class SaveProgressRegression
{
    static double deadline, next;
    static bool reading, menuOnly;
    static int step;
    static NightOneDirector story;
    static NightPersistence persistence;
    static SaveProgressRegression()
    { if (SessionState.GetBool("SaveProgressReview", false)) EditorApplication.playModeStateChanged += Changed; }
    static void Check(bool pass, string message) { if (!pass) throw new Exception("SAVE REGRESSION: " + message); }
    public static void Write() { SessionState.SetBool("SaveProgressMenu", false); Run(false); }
    public static void Menu() { SessionState.SetBool("SaveProgressMenu", true); Run(false); }
    public static void Read() { SessionState.SetBool("SaveProgressMenu", false); Run(true); }
    static void Run(bool read)
    {
        string root = Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT");
        if (string.IsNullOrEmpty(root) || Path.GetFullPath(root).StartsWith(Application.persistentDataPath))
            throw new Exception("Set THREEGIO12_DATA_ROOT to an isolated test directory.");
        if (!read) StorageCases(root + "-storage");
        SessionState.SetBool("SaveProgressRead", read); SessionState.SetBool("SaveProgressReview", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");
        EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }
    static void StorageCases(string root)
    {
        Directory.CreateDirectory(root);
        string file = Path.Combine(root, "preferences.json");
        var store = new LocalJsonStore<GamePreferences>(file, GamePreferences.Valid);
        Check(!store.TryLoad(out _, out _), "missing slot loaded");
        Check(store.Save(new GamePreferences { volume = .2f }, out _), "first write");
        Check(store.Save(new GamePreferences { volume = .8f }, out _), "second write");
        File.WriteAllText(file, "{truncated");
        Check(store.TryLoad(out var recovered, out var message) && Mathf.Abs(recovered.volume - .2f) < .001f && message != "", "backup recovery");
        Check(store.Save(new GamePreferences { volume = .5f }, out _), "repair save");
        File.WriteAllText(file, File.ReadAllText(file).Replace("\"version\":1", "\"version\":99"));
        Check(store.TryLoad(out recovered, out _) && Mathf.Abs(recovered.volume - .2f) < .001f, "corrupt primary overwrote good backup");
        File.Delete(file + ".bak"); Check(!store.TryLoad(out _, out message) && message != "", "future envelope accepted");
        Check(!store.Save(new GamePreferences { volume = float.NaN }, out _), "invalid preferences saved");
        File.WriteAllText(file + ".tmp", "interrupted write");
        Check(store.Delete(out _) && !File.Exists(file) && !File.Exists(file + ".tmp") && !File.Exists(file + ".bak"), "delete left files");
        File.WriteAllText(Path.Combine(root, "blocked"), "file, not directory");
        var blocked = new LocalJsonStore<GamePreferences>(Path.Combine(root, "blocked", "data.json"), GamePreferences.Valid);
        Check(!blocked.Save(new GamePreferences(), out message) && message != "", "IO failure not reported");
        Debug.Log("SAVE_STORAGE_CASES_PASSED");
    }
    static void Changed(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        reading = SessionState.GetBool("SaveProgressRead", false); menuOnly = SessionState.GetBool("SaveProgressMenu", false); step = 0;
        deadline = EditorApplication.timeSinceStartup + 90; next = EditorApplication.timeSinceStartup + 2;
        Application.runInBackground = true;
        EditorApplication.update += Tick;
    }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Save regression timeout");
            story = UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
            persistence = story == null ? null : story.GetComponent<NightPersistence>();
            if (persistence == null || !persistence.Ready) return;
            if (menuOnly)
            {
                Directory.CreateDirectory(NightPersistence.DataRoot);
                switch (step++)
                {
                    case 0: ScreenCapture.CaptureScreenshot(Path.Combine(NightPersistence.DataRoot, "menu.png")); break;
                    case 1: persistence.OpenSettings(); break;
                    case 2: ScreenCapture.CaptureScreenshot(Path.Combine(NightPersistence.DataRoot, "settings.png")); break;
                    default: Debug.Log("SAVE_MENU_CAPTURE_PASSED"); Finish(0); return;
                }
                next = EditorApplication.timeSinceStartup + 1; return;
            }
            if (step++ == 0)
            {
                Check(story.player.IsPaused && Time.timeScale == 0, "startup menu did not pause world");
                if (!reading)
                {
                    Check(persistence.StartNewGame(), "new game failed");
                    story.State.stage = NightStage.Parking; story.State.phonePlaced = true; story.Clock.Set("21:00");
                    story.Act(NightAction.ParkBike); story.Act(NightAction.Phone); story.Act(NightAction.Gate); story.EnterRoom();
                    story.player.Teleport(story.roomSpawn.position, 73); story.player.SetPhone(1); story.Send("camera:unknown", "Cháu không biết.");
                    story.TogglePower(); story.ArriveStranger(); story.Act(NightAction.MoveBag);
                    story.State.prologue_beaten = 2; story.player.RestoreView(23, true); story.player.SetPaused(true);
                    var observer = story.observers[0].CaptureObserver(); observer.mode = ObserverMode.Searching; observer.elapsed = 42; observer.attention = .7f;
                    story.observers[0].ApplyObserver(observer); story.alertLights[0].enabled = true;
                    persistence.Preferences.volume = .37f; persistence.Preferences.mouseSensitivity = .13f;
                    persistence.Preferences.quality = 0; persistence.Preferences.showContentWarning = false;
                    Check(persistence.SavePreferences(), "preferences write failed");
                    Check(story.State.clock == "21:35" && story.State.pendingMessages.Count > 0, "fixture not constructed");
                    next = EditorApplication.timeSinceStartup + 3; return;
                }
                Check(persistence.ContinueSaved(), "new process could not continue"); story.player.SetPaused(true);
                Check(story.State.stage == NightStage.Room && story.State.clock == "21:35" && story.State.prologue_beaten == 2, "state/clock/injury lost");
                Check(Vector3.Distance(story.player.transform.position, story.roomSpawn.position) < .02f && Mathf.Abs(Mathf.DeltaAngle(story.player.transform.eulerAngles.y, 73)) < .01f, "pose lost: actual=" + story.player.transform.position + " expected=" + story.roomSpawn.position + " yaw=" + story.player.transform.eulerAngles.y);
                Check(story.player.IsCrouched && Mathf.Abs(story.player.ViewPitch - 23) < .01f && story.player.ActivePhone == 1, "view/crouch/phone lost");
                Check(story.State.phone_powered_off && story.State.pendingMessages.Count > 0 && story.State.sent.Contains("camera:unknown"), "phone queues/choices lost");
                Check(Vector3.Distance(story.bag.transform.position, story.State.bagPosition) < .001f && story.State.bagDisplaced, "bag lost");
                Check(story.observers[0].Mode == ObserverMode.Searching && Mathf.Abs(story.observers[0].Attention - .7f) < .001f
                    && Mathf.Abs(story.observers[0].CaptureObserver().elapsed - 42) < .001f && story.alertLights[0].enabled, "AI timers/lights lost");
                Check(Mathf.Abs(AudioListener.volume - .37f) < .001f && Mathf.Abs(story.player.sensitivity - .13f) < .001f
                    && QualitySettings.GetQualityLevel() == 0 && !persistence.Preferences.showContentWarning, "preferences not restored across process");
                story.Checkpoints.Restore(out var checkpoint, out _, out _); Check(checkpoint.clock == "21:20", "checkpoint replaced by current state");
                // Restore while the menu is open; it must remain paused until explicitly resumed.
                story.OpenModal("notebook"); Check(persistence.Flush() && persistence.ContinueSaved(), "modal save failed");
                Check(story.Modal == "notebook" && story.player.ExternalUI, "modal lost"); story.CloseModal();
                story.FinishNight(); Check(persistence.Flush() && persistence.ContinueSaved() && story.State.stage == NightStage.Ended && story.BlackTitle != "", "ended state lost");
                story.player.SetPaused(true); story.RestoreCheckpoint(); Check(story.State.stage == NightStage.Room && story.State.clock == "21:20", "checkpoint replay after reload failed");
                Check(persistence.DeleteProgress(), "delete failed"); next = EditorApplication.timeSinceStartup + 3; return;
            }
            if (!reading)
            {
                var store = new LocalJsonStore<NightSaveData>(Path.Combine(NightPersistence.DataRoot, "night-one.json"), NightSaveData.Valid);
                Check(store.TryLoad(out var automatic, out _) && automatic.state.clock == "21:35" && automatic.state.prologue_beaten == 2
                    && automatic.activePhone == 1, "automatic save interval did not persist the current session");
                Debug.Log("SAVE_WRITE_PROCESS_PASSED"); Finish(0); return;
            }
            Check(!persistence.HasSave && !File.Exists(Path.Combine(NightPersistence.DataRoot, "night-one.json.bak")), "deleted slot was recreated by autosave");
            Check(File.Exists(Path.Combine(NightPersistence.DataRoot, "preferences.json")), "deletion removed settings");
            // A failed first save must keep the menu/error visible, rather than silently entering gameplay.
            string blockedTemp = Path.Combine(NightPersistence.DataRoot, "night-one.json.tmp"); Directory.CreateDirectory(blockedTemp);
            Check(!persistence.StartNewGame() && story.player.IsPaused && story.player.PauseMenuLocked && persistence.Status != "", "failed new-game write hid the error");
            Directory.Delete(blockedTemp);
            Check(persistence.StartNewGame() && story.State.stage == NightStage.Prologue && story.State.messages.Count == 0 && story.State.prologue_beaten == 0, "replay did not reset progress");
            Check(Mathf.Abs(story.player.sensitivity - .13f) < .001f, "replay reset preferences");
            Debug.Log("SAVE_READ_PROCESS_PASSED"); Finish(0);
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update -= Tick; SessionState.SetBool("SaveProgressReview", false);
        EditorApplication.Exit(code);
    }
}
