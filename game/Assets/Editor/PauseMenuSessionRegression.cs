using System;
using System.Collections;
using System.Reflection;
using BaGio12.Gameplay;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Simulate the focus-loss menu opening before the persistence coroutine attaches its session.
[InitializeOnLoad]
public static class PauseMenuSessionRegression
{
    const string Flag = "PauseMenuSessionRegression";
    static double deadline;
    static PauseMenuSessionRegression()
    { if (SessionState.GetBool(Flag, false)) EditorApplication.playModeStateChanged += Changed; }
    public static void Run()
    {
        string root = System.Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT");
        if (string.IsNullOrEmpty(root) || System.IO.Path.GetFullPath(root).StartsWith(Application.persistentDataPath))
            throw new Exception("Use an isolated THREEGIO12_DATA_ROOT.");
        EditorSceneManager.OpenScene(NightOneRealisticSetup.ScenePath);
        SessionState.SetBool(Flag, true);
        EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        Application.runInBackground = true; deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update += Tick;
    }
    static void Invoke(PauseMenu menu, string method) => typeof(PauseMenu).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
    static bool Has(PauseMenu menu, string label)
    {
        var items = (IList)typeof(PauseMenu).GetField("items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
        foreach (var item in items) if ((string)item.GetType().GetField("label").GetValue(item) == label) return true;
        return false;
    }
    static void Check(bool ok, string error) { if (!ok) throw new Exception(error); }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Menu regression timeout");
            var story = UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
            var persistence = story == null ? null : story.GetComponent<NightPersistence>();
            if (persistence == null || !persistence.Ready) return;
            var player = story.player; var menu = player.GetComponent<PauseMenu>();
            var original = player.Session;
            try
            {
                player.SetPaused(true); player.Session = null; Invoke(menu, "Update"); Invoke(menu, "Layout");
                Check(Has(menu, "Tiếp tục"), "Missing early-pause fixture");
                typeof(PauseMenu).GetField("noticeSeen", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
                var session = new Session { ask = true, ShowWarning = true }; player.Session = session; Invoke(menu, "Update");
                Check(Has(menu, "Gửi thống kê ẩn danh") && !Has(menu, "Chơi mới Đêm 1"), "Consent gate lost after late session attachment");
                Check(menu.NoticeVisible, "Content warning lost after late session attachment");
                menu.ShowSettings();
                Check(!(bool)typeof(PauseMenu).GetField("inPage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu), "Settings bypassed consent gate");
                session.AnswerTelemetryConsent(false); Invoke(menu, "Update");
                Check(Has(menu, "Chơi mới Đêm 1") && !Has(menu, "Tiếp tục"), "Startup actions stayed stale after session attachment");
                session.evidence = true; Invoke(menu, "Update");
                Check(Has(menu, "Gửi bằng chứng cho công an"), "Evidence entry did not refresh");
                session.evidence = false; Invoke(menu, "Update");
                Check(!Has(menu, "Gửi bằng chứng cho công an"), "Evidence entry remained after permission removed");
                session.saved = true; Invoke(menu, "Update");
                Check(Has(menu, "Tiếp tục bản đã lưu") && Has(menu, "Xóa tiến độ"), "Save availability did not refresh");
                session.start = false; Invoke(menu, "Update");
                Check(Has(menu, "Chơi lại Đêm 1") && Has(menu, "Lưu và thoát") && !Has(menu, "Xóa tiến độ"), "Playing actions stayed stale");
            }
            finally { player.Session = original; }
            Debug.Log("PAUSE_MENU_SESSION_REGRESSION_PASSED"); Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update -= Tick; SessionState.SetBool(Flag, false); EditorApplication.Exit(code);
    }
    sealed class Session : IGameSession
    {
        public bool start = true, saved, ask, evidence;
        public bool AtStart => start;
        public bool HasSave => saved;
        public string Status => "";
        public string SaveSummary => "Regression fixture";
        public bool ShowWarning { get; set; }
        public int Quality { get; set; }
        public bool ContinueSaved() => false;
        public bool StartNewGame() => false;
        public bool DeleteProgress() => false;
        public bool PrepareQuit() => false;
        public void ImportSettings(GameSettings settings) { }
        public bool SaveSettings(GameSettings settings) => false;
        public bool AskTelemetryConsent => ask;
        public bool TelemetryConsent { get; set; }
        public void AnswerTelemetryConsent(bool granted) { TelemetryConsent = granted; ask = false; }
        public bool CanSubmitEvidence => evidence;
        public void SubmitEvidence() { }
    }
}
