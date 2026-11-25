using System;
using System.IO;
using System.Linq;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NightTwoMorningRegression
{
    static NightOneDirector story;
    static NightTwoMorning morning;
    static NightPersistence persistence;
    static int step;
    static double deadline;
    static bool reading;
    static NightTwoMorningRegression()
    { if (SessionState.GetBool("NightTwoMorningRegression",false)) EditorApplication.playModeStateChanged += Changed; }
    public static void Write() => Run(false);
    public static void Read() => Run(true);
    static void Run(bool read)
    {
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT"))) throw new Exception("Set isolated THREEGIO12_DATA_ROOT.");
        Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU",read?"0":"1");
        SessionState.SetBool("NightTwoMorningRead",read); SessionState.SetBool("NightTwoMorningRegression",true);
        EditorSceneManager.OpenScene(NightOneRealisticSetup.ScenePath);
        EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange mode)
    {
        if(mode!=PlayModeStateChange.EnteredPlayMode)return;
        NightTwoSceneFixtures.AddMorning();
        Application.runInBackground=true; reading=SessionState.GetBool("NightTwoMorningRead",false);
        step=0;deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;
    }
    static GameObject Find(string name) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==name).gameObject;
    static void Check(bool value,string text) { if(!value) throw new Exception("NIGHT_TWO: "+text); }
    static void DriveToEnd()
    {
        for(int i=0;i<2000 && !FixedRouteRide.AtStop(morning.State);i++) FixedRouteRide.Step(morning.State,1,false,.1f);
        Check(FixedRouteRide.AtStop(morning.State),"ride never arrived");
        Check(morning.Dismount(),"could not dismount at arrival");
    }
    static void StartMorning(bool slept)
    {
        Check(persistence.StartNewGame(),"new game failed");
        Check(!UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Night2 props").gameObject.activeSelf,"Night 2 props leaked into Night 1");
        Check(!story.InNightTwo && !morning.Begin(),"night 2 available before night 1 completion");
        Check(story.observers.All(x=>x.enabled),"night 1 observers left disabled");
        story.State.hasNganPhone=true;story.State.night1_slept=slept;story.State.prologue_beaten=2;
        story.FinishNight(); Check(morning.Begin(),"night 1 handoff");
        Check(story.State.prologue_beaten==2 && story.State.night1_slept==slept && story.State.clock=="06:30", "night 1 choices lost");
        Check(morning.State.odometer==(slept?12411:12400),"overnight mileage branch");
        Check(!morning.Begin() && !morning.TestLamp() && !morning.BuyFurniture(),"out-of-order action accepted");
        morning.SendMessage("ClosePage");morning.SendMessage("OpenPage","notebook");
        Check(story.player.ExternalUI && morning.State.stage==MorningStage.Alarm,"alarm screen dismissed without progressing");
        morning.SendMessage("RefreshWorld");
        Check(!Find("Night2_Shelf").activeSelf && !Find("Night2_ClampLamp").activeSelf && Find("Night2_BreadCart").activeInHierarchy,"premature shelf/lamp or hidden breakfast");
        Check(morning.StopAlarm(),"wake");
        story.player.Teleport(NightTwoMorning.Room); Check(!morning.EatBreakfast(),"remote breakfast accepted");
        story.player.Teleport(NightTwoMorning.Breakfast); Check(morning.EatBreakfast(),"breakfast");
        Check(!morning.Mount(),"remote bike mount accepted");
        story.player.Teleport(NightTwoMorning.Parking); Check(morning.Mount(),"outbound mount");
        Check(!morning.Dismount(),"dismounted while route incomplete");
        // Both auto-stop at red and actual service braking are independently checked.
        var braking=new NightTwoMorningState{riding=true,speed=8,routeMetres=10};
        FixedRouteRide.Step(braking,1,true,.1f); Check(braking.speed<8,"brake did not decelerate");
        for(int i=0;i<65;i++)FixedRouteRide.Step(morning.State,1,false,.1f);
        Check(morning.State.routeMetres==FixedRouteRide.StopLine && !morning.State.redLightCleared,"red light crossed before waiting");
        Check(morning.State.stoppedSeconds<FixedRouteRide.RedSeconds,"red duration not exercised");
        var mid=story.CaptureSave(); Check(NightSaveData.Valid(mid),"mid-ride save invalid");
        float stopped=morning.State.stoppedSeconds;
        Check(story.ApplySave(mid) && morning.State.riding && story.player.ExternalUI && morning.State.stoppedSeconds==stopped,"mid-ride restore lost control/timer");
        DriveToEnd(); Check(morning.State.stage==MorningStage.Store && morning.State.redLightCleared,"store arrival");
        Check(morning.BuyFurniture(),"purchase"); Check(morning.Mount(),"return mount");DriveToEnd();
        Check(morning.State.stage==MorningStage.Gate && morning.State.odoLog.Count==4 && morning.State.odometer==(slept?12414:12403),"return mileage/log");
        Check(morning.State.odoLog.Select(x=>x.engineOn).SequenceEqual(new[]{true,false,true,false}),"engine log order");
    }
    static void PassGate(bool seen)
    {
        var origin = NightTwoMorning.Lien + Vector3.up * 1.5f;
        var eye = origin + Vector3.back * 1.5f;
        morning.State.gateElapsed=seen?9:0;
        Physics.SyncTransforms(); Check(morning.LienCanSee(eye)==seen,"gate visibility fixture");
        if(seen)
        {
            var wall = new GameObject("Regression sight blocker");
            wall.transform.position = (origin + eye) / 2;
            wall.AddComponent<BoxCollider>().size = new Vector3(.5f,.5f,.2f);
            Physics.SyncTransforms();Check(!morning.LienCanSee(eye),"Lien sees through wall");
            wall.GetComponent<Collider>().enabled=false;UnityEngine.Object.Destroy(wall);Physics.SyncTransforms();
            // Exercise the observer update in the real scene before approaching the gate.
            story.player.Teleport(eye - Vector3.up * 1.6f);
            story.player.eyes.transform.position = eye;
            morning.SendMessage("Update");
        }
        story.player.Teleport(NightTwoMorning.Gate);
        Check(morning.PassGate() && morning.State.lienSawMorning==seen && !story.Busy,"gate became failure/chase");
        Check(story.State.prologue_beaten==2,"gate applied a capture penalty");
        story.player.Teleport(NightTwoMorning.Room);
        Check(!morning.Assemble(2),"shelf skips a tier");
    }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Night two test timeout");
            story=UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
            if(story==null || (persistence=story.GetComponent<NightPersistence>())?.Ready!=true || story.Morning==null)return;
            morning=story.Morning;story.player.ResumeControl();
            if(step==0)
            {
                if(reading)
                {
                    Check(persistence.ContinueSaved(),"fresh process cannot continue");
                    Check(story.InNightTwo && morning.State.stage==MorningStage.Assembly && morning.State.assemblySteps==2
                        && morning.State.roomReply=="given" && morning.State.lienSawMorning && morning.State.odometer==12414,"cross-process data lost");
                    Check(!morning.Assemble(0) && morning.Assemble(2) && morning.Assemble(3),"continue assembly");
                    morning.SendMessage("RefreshWorld");
                    Check(Find("Night2_ClampLamp").activeInHierarchy && Find("Tier_4").activeInHierarchy,"completed shelf/lamp visual state");
                    Check(morning.TestLamp(),"lamp");Check(persistence.Flush(),"pre-nap flush");
                    Check(morning.Rest() && story.CaptureSave()==null,"transition emitted half-built save");step=1;return;
                }
                StartMorning(false); Check(!morning.ReplyRoom(true),"D6 shown on staying-up branch");PassGate(false);
                for(int i=0;i<4;i++)Check(morning.Assemble(i),"assembly branch");
                Check(morning.TestLamp(),"stay-up branch lamp");
                morning.State.restSeconds=60;morning.SendMessage("Update");
                Check(morning.Transitioning,"forced nap did not start");step=2;return;
            }
            if(step==2)
            {
                if(morning.Transitioning)return;
                Check(morning.State.stage==MorningStage.Evening && story.State.clock=="21:00" && morning.State.odometer==12414,"awake branch afternoon mileage");
                StartMorning(true);PassGate(true);
                var beforeReply=story.CaptureSave();
                Check(morning.ReplyRoom(false) && morning.State.momSuspicion==1,"refused room branch");
                Check(story.ApplySave(beforeReply) && morning.State.roomReply=="unanswered","room choice rollback");
                int before=story.State.messages.Count;
                Check(morning.ReplyRoom(true) && !morning.ReplyRoom(false) && story.State.messages.Count==before+1,"reply generated daytime mom response or duplicate");
                Check(morning.Assemble(0) && morning.Assemble(1),"partial assembly");
                morning.SendMessage("RefreshWorld");
                Check(Find("Tier_2").activeInHierarchy && !Find("Tier_3").activeInHierarchy && !Find("Night2_ClampLamp").activeSelf,"partial shelf visual state");
                var invalid=story.CaptureSave(); invalid.state.nightTwo.speed=float.NaN;
                Check(!NightSaveData.Valid(invalid) && !story.ApplySave(invalid),"invalid campaign data accepted");
                Check(morning.State.assemblySteps==2,"invalid save mutated world");
                Check(persistence.Flush(),"write partial assembly");
                var store=new LocalJsonStore<NightSaveData>(Path.Combine(NightPersistence.DataRoot,"night-one.json"),NightSaveData.Valid);
                Check(store.TryLoad(out var disk,out _) && disk.state.nightTwo.assemblySteps==2,"disk snapshot missing campaign");
                story.player.Teleport(NightTwoMorning.Room + Vector3.back,330);story.player.RestoreView(8,false);
                string preview=Path.Combine(NightPersistence.DataRoot,"morning-preview.png");
                if(File.Exists(preview))File.Delete(preview);
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) { Finish("NIGHT_TWO_WRITE_PASSED"); return; }
                ScreenCapture.CaptureScreenshot(preview);step=3;return;
            }
            if(step==1)
            {
                if(morning.Transitioning)return;
                Check(morning.State.stage==MorningStage.Evening && morning.State.odometer==12425 && story.State.clock=="21:00","nap result");
                Check(morning.State.momSuspicion==0 && story.State.messages.Any(x=>x.StartsWith("Mẹ · 12:20:")),"scheduled messages");
                Check(persistence.Flush() && persistence.ContinueSaved() && morning.State.stage==MorningStage.Evening,"evening reload");
                Check(persistence.StartNewGame() && !story.InNightTwo && story.observers.All(x=>x.enabled),"restart did not restore night one");
                Finish("NIGHT_TWO_READ_PASSED");
            }
            if(step==3 && File.Exists(Path.Combine(NightPersistence.DataRoot,"morning-preview.png"))) Finish("NIGHT_TWO_WRITE_PASSED");
        }
        catch(Exception e){Debug.LogException(e);Finish(null);}
    }
    static void Finish(string marker)
    {
        EditorApplication.update-=Tick;SessionState.SetBool("NightTwoMorningRegression",false);
        if(marker!=null)Debug.Log(marker);
        EditorApplication.Exit(marker==null?1:0);
    }
}
