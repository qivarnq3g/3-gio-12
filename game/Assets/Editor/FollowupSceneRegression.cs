using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FollowupSceneRegression
{
    public static void Inventory()
    {
        EditorSceneManager.OpenScene(NightOneRealisticSetup.ScenePath);
        var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (string name in new[] { "CHECKPOINT_N1_Room7", "CHECKPOINT_N1_Garage", "PARK_N2_HomewareShop", "BUY_N2_ShelfAndLamp", "BanhMi_Cart", "NPC_N2_BanhMiSeller", "BH_F3_Room7_Rack", "Night2_ClampLamp", "PersonLien", "HomewareShop_ShelfKit_Box" })
        {
            var t = transforms.FirstOrDefault(x => x.name == name);
            if (t == null) { Debug.Log("FOLLOWUP_MISSING " + name); continue; }
            var renderers = t.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"FOLLOWUP_MODEL {name} position={t.position:F3} active={t.gameObject.activeInHierarchy} renderers={renderers.Length}");
            foreach (var r in renderers.Take(3)) Debug.Log($"FOLLOWUP_RENDERER {r.name} enabled={r.enabled} center={r.bounds.center:F3} size={r.bounds.size:F3}");
        }
        foreach (var t in transforms.Where(x => x.name.IndexOf("rack", StringComparison.OrdinalIgnoreCase) >= 0 || x.name.IndexOf("clamp", StringComparison.OrdinalIgnoreCase) >= 0))
            Debug.Log($"FOLLOWUP_CANDIDATE {t.name} active={t.gameObject.activeInHierarchy} position={t.position:F3}");
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && ((r.bounds.center.y > 10 && r.bounds.center.y < 13 && r.bounds.center.x > -7 && r.bounds.center.x < -1 && r.bounds.center.z < -41 && r.bounds.center.z > -49) || Vector3.Distance(r.bounds.center,new Vector3(-2.3f,.6f,2.2f)) < 2)))
            Debug.Log($"FOLLOWUP_VISIBLE {r.name} center={r.bounds.center:F3} size={r.bounds.size:F3}");
        EditorApplication.Exit(0);
    }

    static double deadline;
    static FollowupSceneRegression()
    {
        if (SessionState.GetBool("FollowupSceneRegression",false)) EditorApplication.playModeStateChanged += Changed;
    }
    public static void Run()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT"))) throw new Exception("Use an isolated THREEGIO12_DATA_ROOT.");
        Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU","1");
        SessionState.SetBool("FollowupSceneRegression",true);
        EditorSceneManager.OpenScene(NightOneRealisticSetup.ScenePath);
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredPlayMode) return;
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update += Tick;
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Follow-up scene timeout.");
            var story = UnityEngine.Object.FindFirstObjectByType<BaGio12.Story.NightOneDirector>();
            if (story?.Morning == null || story.GetComponent<BaGio12.Persistence.NightPersistence>()?.Ready != true) return;
            var morning = story.Morning;
            Check(!morning.Begin(),"Night 2 was accessible before the Night 1 ending.");
            story.State.stage = BaGio12.Story.NightStage.Ended;
            story.State.ending = "night1_completed";
            bool ready = morning.ValidateSceneAssets();
            string before = JsonUtility.ToJson(story.State);
            bool began = morning.Begin();
            Check(began == ready,"Night 2 readiness disagrees with actual scene assets.");
            if (!ready)
            {
                Check(JsonUtility.ToJson(story.State) == before,"Blocked transition changed campaign state.");
                Check(!story.InNightTwo,"Missing scene assets activated Night 2.");
                Check(GameObject.Find("Night 2 — realistic interactions") == null,"Blocked transition created a partial world.");
                Debug.Log("FOLLOWUP_ASSET_BLOCKER " + morning.MissingSceneAssets);
            }
            else
            {
                Check(story.InNightTwo && story.player.transform.position.y > 9,"Night 2 did not start in the actual third-floor room.");
                Check(GameObject.Find("Night 2 morning — temporary graybox") == null,"Legacy sandbox was created.");
                Check(BaGio12.Persistence.NightSaveData.Valid(story.CaptureSave()),"Realistic follow-up save is invalid.");
            }
            Finish(true);
        }
        catch (Exception e) { Debug.LogException(e); Finish(false); }
    }
    static void Finish(bool success)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("FollowupSceneRegression",false);
        if (success) Debug.Log("FOLLOWUP_SCENE_GUARD_PASSED");
        EditorApplication.Exit(success ? 0 : 1);
    }
}
