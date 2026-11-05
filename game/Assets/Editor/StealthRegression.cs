using System;
using System.Linq;
using BaGio12.Story;
using BaGio12.Stealth;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StealthRegression
{
    static NightOneDirector story;
    static NightObserver child;
    static Vector3 patrolStart;
    static float until;
    static int step;
    static double deadline;
    static StealthRegression() { if (SessionState.GetBool("StealthRegression", false)) EditorApplication.playModeStateChanged += Changed; }
    public static void Run()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT"))) throw new Exception("Set isolated THREEGIO12_DATA_ROOT.");
        Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU", "1");
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");
        SessionState.SetBool("StealthRegression", true); EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredPlayMode) return;
        Application.runInBackground = true; step = 0; deadline = EditorApplication.timeSinceStartup + 100;
        EditorApplication.update += Tick;
    }
    static void Check(bool ok, string detail) { if (!ok) throw new Exception("STEALTH REGRESSION: " + detail); }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Stealth test timed out");
            story = UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
            if (story == null || story.GetComponent<BaGio12.Persistence.NightPersistence>()?.Ready != true) return;
            if (step == 0)
            {
                story.player.ResumeControl();
                foreach (var o in story.observers) o.enabled = false;
                var hung = story.observers.First(x => x.flashlight);
                story.player.Teleport(new Vector3(-.3f, .1f, 7));
                var sample = hung.CaptureObserver(); sample.mode = ObserverMode.Chasing; sample.elapsed = 100; sample.lampTime = .8f;
                sample.position = new Vector3(-2.7f, .03f, 7); sample.rotation = Quaternion.Euler(0, 60, 0); hung.ApplyObserver(sample);
                Physics.SyncTransforms(); Check(hung.CanSeePlayer(), "flashlight fixture has no clear line of sight");
                hung.SendMessage("Update"); Check(hung.CaptureObserver().lampTime == 0 && !story.Busy, "interrupted flashlight exposure accumulated");
                // A nearby target behind a wall must enter searching, never get caught through the wall.
                story.player.Teleport(new Vector3(-1.8f, .1f, 7)); sample.rotation = Quaternion.Euler(0, 90, 0); sample.lampTime = 0; hung.ApplyObserver(sample);
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(-2.25f, 1.2f, 7); wall.transform.localScale = new Vector3(.15f, 3, 1);
                Physics.SyncTransforms(); Check(!hung.CanSeePlayer(), "wall did not block line of sight");
                hung.SendMessage("Update"); Check(!story.Busy && hung.Mode == ObserverMode.Searching, "caught through a solid wall");
                wall.GetComponent<Collider>().enabled = false; UnityEngine.Object.Destroy(wall);
                var lien = story.observers.First(x => x.once); var dormant = lien.CaptureObserver(); dormant.mode = ObserverMode.Normal; lien.ApplyObserver(dormant);
                Check(!story.PrologueDeparted && !lien.CanHear, "off-schedule observer hears before appearing");
                lien.Hear(story.player.transform.position, true); Check(lien.Mode == ObserverMode.Normal, "invisible observer reacted to shout");
                dormant.mode = ObserverMode.Searching; lien.ApplyObserver(dormant); lien.SendMessage("Update");
                Check(!lien.ActiveThreat, "off-schedule observer kept an invisible pursuit active");
                // Seed a nearly completed hiding interval through the actual save/restore API.
                var save = story.CaptureSave(); save.position = story.hidingPoints[0].position; save.hidingSeconds = 8;
                foreach (var o in save.observers) o.mode = ObserverMode.Inactive;
                var pursuer = save.observers.First(x => x.id == hung.name); pursuer.mode = ObserverMode.Searching;
                pursuer.position = new Vector3(0, .03f, 30); pursuer.rotation = Quaternion.identity;
                Check(story.ApplySave(save), "hiding save fixture rejected"); story.player.Teleport(save.position + Vector3.forward * .2f);
                story.SendMessage("Update"); Check(story.CaptureSave().hidingSeconds == 0 && hung.ActiveThreat, "movement did not restart hiding interval");
                Check(story.ApplySave(save), "stable hiding fixture rejected"); Physics.SyncTransforms();
                Check(!story.observers.Any(x => x.CanSeePlayer()), "hiding fixture is exposed");
                story.SendMessage("Update"); Check(!hung.ActiveThreat, "eight seconds still and unseen did not lose trail");
                // Observe real NavMesh movement over simulation time, not editor wall time.
                story.ResetPrologue(); story.player.Teleport(story.alleyStart.position);
                child = story.observers.First(x => x.patrol); sample = child.CaptureObserver(); sample.mode = ObserverMode.Normal; sample.elapsed = 1; sample.startDelay = 0;
                child.ApplyObserver(sample); child.enabled = true; patrolStart = child.transform.position;
                until = Time.time + 2; step = 1; return;
            }
            if (Time.time < until) return;
            Check(Vector3.Distance(child.transform.position, patrolStart) > .2f, "patrol path was reset before movement");
            CheckNoiseRecipients();
            CheckCloseCapture();
            Debug.Log("STEALTH_REGRESSION_PASSED"); Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void CheckNoiseRecipients()
    {
        story.ResetPrologue();
        foreach (var observer in story.observers)
        {
            observer.enabled = false; var data = observer.CaptureObserver(); data.mode = ObserverMode.Inactive; data.focusRemaining = 0;
            observer.ApplyObserver(data);
        }
        var listeners = story.observers.Where(x => !x.once && !x.child && !x.stationary).Take(2).ToArray();
        Check(listeners.Length == 2, "hearing fixture needs two adult listeners");
        Vector3 point = story.hidingPoints[0].position;
        story.player.Teleport(point);
        var first = listeners[0].CaptureObserver(); first.mode = ObserverMode.Normal; first.position = new Vector3(-1, .03f, 9); first.focusRemaining = 0;
        var second = listeners[1].CaptureObserver(); second.mode = ObserverMode.Normal; second.position = new Vector3(-1, .03f, 10); second.focusRemaining = 0;
        listeners[0].ApplyObserver(first); listeners[1].ApplyObserver(second);
        var outside = story.observers.First(x => x.stationary); var far = outside.CaptureObserver(); far.mode = ObserverMode.Normal;
        far.position = new Vector3(0, .03f, 12); far.focusRemaining = 0; outside.ApplyObserver(far);
        var inactive = story.observers.First(x => x.child); var silent = inactive.CaptureObserver(); silent.mode = ObserverMode.Inactive;
        silent.position = new Vector3(-1, .03f, 8); silent.focusRemaining = 0; inactive.ApplyObserver(silent);
        var running = story.player.GetType().GetProperty("IsRunning").GetSetMethod(true);
        running.Invoke(story.player, new object[] { true });
        story.SendMessage("Update");
        Check(listeners[0].CaptureObserver().focusRemaining > 0 && listeners[1].CaptureObserver().focusRemaining == 0
            && outside.CaptureObserver().focusRemaining == 0, "running noise reached more than the nearest listener");
        running.Invoke(story.player, new object[] { false });
        listeners[0].ApplyObserver(first); listeners[1].ApplyObserver(second);
        story.State.prologue_beaten = 2; Physics.SyncTransforms();
        Check(story.Hidden && Vector3.Distance(outside.transform.position, point) > 3, "breathing range fixture invalid");
        story.SendMessage("Update");
        Check(listeners.All(x => x.CaptureObserver().focusRemaining > 0), "injured breathing did not reach both listeners within 3m");
        Check(outside.CaptureObserver().focusRemaining == 0 && inactive.CaptureObserver().focusRemaining == 0,
            "breathing reached an out-of-range or inactive listener");
    }
    static void CheckCloseCapture()
    {
        story.ResetPrologue();
        var save = story.CaptureSave(); save.position = new Vector3(-1.8f, .1f, 7); save.immuneRemaining = 0;
        foreach (var observer in save.observers) observer.mode = ObserverMode.Inactive;
        var hung = story.observers.First(x => x.flashlight); var pursuer = save.observers.First(x => x.id == hung.name);
        pursuer.mode = ObserverMode.Chasing; pursuer.position = new Vector3(-2.7f, .03f, 7); pursuer.elapsed = 100; pursuer.lampTime = 0;
        foreach (float yaw in new[] { 270f, 0f })
        {
            pursuer.rotation = Quaternion.Euler(0, yaw, 0);
            Check(story.ApplySave(save), "close capture fixture rejected"); Physics.SyncTransforms();
            Check(!hung.CanSeePlayer(), "close target should be outside the 90-degree view cone");
            hung.SendMessage("Update");
            Check(story.Busy, "close unobstructed target escaped capture behind/beside pursuer, yaw=" + yaw);
        }
    }
    static void Finish(int code)
    { SessionState.SetBool("StealthRegression", false); EditorApplication.update -= Tick; EditorApplication.Exit(code); }
}
