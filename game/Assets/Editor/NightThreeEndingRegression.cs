using System;
using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Online;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NightThreeEndingRegression
{
    const string Flag = "NightThreeEndingRegression";
    static double deadline;
    static NightThreeEndingRegression()
    { if (SessionState.GetBool(Flag, false)) EditorApplication.playModeStateChanged += Changed; }
    public static void Run()
    {
        string root = System.Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT");
        if (string.IsNullOrEmpty(root) || System.IO.Path.GetFullPath(root).StartsWith(Application.persistentDataPath))
            throw new Exception("Use an isolated THREEGIO12_DATA_ROOT.");
        System.Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU", "1");
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
    static void Check(bool ok, string message) { if (!ok) throw new Exception("N3 ending: " + message); }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Ending regression timeout");
            var story = UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
            var persistence = story == null ? null : story.GetComponent<NightPersistence>();
            if (persistence == null || !persistence.Ready || persistence.Online?.Profile == null) return;
            Check(persistence.StartNewGame(), "new game");
            persistence.Online.SetConsent(false);
            var events = new List<EndingTelemetry>();
            story.TelemetryProduced += e => { if (e is EndingTelemetry ending) events.Add(ending); };
            var night = story.Morning;
            story.State.hasNganPhone = true; story.FinishNight(); Check(night.Begin(), "N1 handoff with real assets");
            night.State.stage = MorningStage.Ended; night.State.assemblySteps = 4; night.State.lampTested = true;
            Check(night.BeginThree() && night.StopThreeAlarm(), "N3 handoff");
            night.Third.stage = NightThreeStage.DawnWaiting;
            story.player.Teleport(NightTwoMorning.Room); story.player.ResumeControl();
            Check(night.FinishThreeDawn(), "normal dawn");
            Check(events.Count == 0 && !persistence.CanSubmitEvidence, "N3 completion incorrectly unlocked Home");

            // Prepare the approved police choice; no substitute models are created.
            night.Third.stage = NightThreeStage.Evening;
            night.Third.sentCasePhoto = night.Third.answeredCall = night.Third.gotVaultCode = night.Third.openedVault = true;
            story.player.SetOverlay(false);
            Check(night.DecideThreePhone(false) && !night.ThreeHandInPhone(), "remote hand-in accepted");
            var desk = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => t.name == "Night3_PoliceDesk");
            Check(desk.GetComponentsInChildren<MeshFilter>(true).Any(m => AssetDatabase.GetAssetPath(m.sharedMesh) == "Assets/Art/Models/Props/Night3_PoliceDesk.fbx"), "not real desk FBX");
            var node = UnityEngine.Object.FindObjectsByType<MorningInteraction>(FindObjectsSortMode.None)
                .Single(t => t.action == "police3");
            var bounds = node.GetComponent<BoxCollider>().bounds;
            var stand = desk.position - desk.forward * 1.1f;
            stand.y = NightTwoMorning.ThreePolice.y + .1f;
            story.player.Teleport(stand); story.player.AimAt(bounds.center, 1);
            Physics.SyncTransforms();
            Check(Physics.Raycast(story.player.eyes.transform.position, story.player.eyes.transform.forward, out var hit,
                story.player.reach, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore), "no desk ray hit");
            Check(hit.collider.GetComponentInParent<MorningInteraction>() == node, "desk interaction occluded by " + hit.collider.name);
            node.HandleInput(story.player, true, false);
            Check(night.Third.stage == NightThreeStage.Scapegoat && story.State.ending == "scapegoat", "real desk hand-in failed");
            Check(events.Count == 1 && events[0].endingId == "scapegoat" && events[0].night == 3, "missing or wrong ending event");
            Check(!night.ThreeHandInPhone() && events.Count == 1, "duplicate hand-in event");
            Check(!persistence.CanSubmitEvidence && NightSaveData.Valid(story.CaptureSave()), "scapegoat unlocked Home or invalid save");
            Check(persistence.Flush() && persistence.ContinueSaved() && events.Count == 1, "reload repeated ending event");
            Debug.Log("NIGHT_THREE_REAL_DESK_INTERACTION_PASSED");

            // Exercise the existing global ending hook; these are not new N3 story choices.
            Check(!story.SandalEndingUnlocked, "fixture already unlocked");
            story.End("red_thread_sandal"); Check(events.Count == 1 && story.State.ending == "scapegoat", "locked ending accepted");
            story.End("night3_completed"); Check(events.Count == 1, "noncanonical ending emitted");
            story.End(PlayerProfile.HomeEnding);
            Check(events.Count == 2 && persistence.CanSubmitEvidence, "Home event did not unlock evidence without consent");
            var profiles = new LocalJsonStore<PlayerProfile>(System.IO.Path.Combine(NightPersistence.DataRoot, "profile.json"), PlayerProfile.Valid);
            Check(profiles.TryLoad(out var saved, out _) && saved.homeReached && saved.Consent == TelemetryConsent.Declined, "Home profile not persisted");
            story.End(PlayerProfile.HomeEnding); Check(events.Count == 2, "Home event repeated");
            persistence.Online.Profile.Unlock(PlayerProfile.SandalUnlock);
            Check(story.SandalEndingUnlocked, "profile unlock not read");
            story.End("red_thread_sandal");
            Check(events.Count == 3 && events[2].endingId == "red_thread_sandal", "accepted profile unlock did not permit ending");
            Debug.Log("NIGHT_THREE_ENDING_REGRESSION_PASSED"); Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code)
    { EditorApplication.update -= Tick; SessionState.SetBool(Flag, false); EditorApplication.Exit(code); }
}
