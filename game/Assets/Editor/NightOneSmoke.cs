using System;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Story;
using BaGio12.Stealth;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class NightOneSmoke
{
    static NightOneDirector story;
    static int step, frame;
    static double next, captureTime;
    static bool record;
    static NightOneSmoke()
    { if(SessionState.GetBool("NightOneSmoke",false)) EditorApplication.playModeStateChanged+=Changed; }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");
        SessionState.SetBool("NightOneSmoke",true);
        EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;
        EditorApplication.EnterPlaymode();
    }
    public static void Record()
    {Directory.CreateDirectory("/private/tmp/3gio12-night-record");SessionState.SetBool("NightOneRecord",true);Run();}
    static void Changed(PlayModeStateChange mode)
    {
        if(mode!=PlayModeStateChange.EnteredPlayMode)return;
        story=UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
        Application.runInBackground=true;step=0;frame=0;record=SessionState.GetBool("NightOneRecord",false);
        next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;
    }
    static void Require(bool value,string message){if(!value)throw new Exception("Night1: "+message);}
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if(record && EditorApplication.timeSinceStartup>=captureTime)
        {ScreenCapture.CaptureScreenshot($"/private/tmp/3gio12-night-record/{frame++:D5}.png");captureTime=EditorApplication.timeSinceStartup+.1;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            var s=story.State;
            switch(step++)
            {
                case 0:
                    story.player.ResumeControl();
                    foreach(var o in story.observers)o.enabled=false;
                    Require(s.clock=="20:56","wrong initial clock");
                    Require(!story.player.showTestHud,"test HUD leaked");
                    story.player.SetPhone(1);Require(story.player.ActivePhone==0,"phone allowed during prologue");
                    var path=new NavMeshPath();
                    Require(NavMesh.CalculatePath(story.alleyStart.position,story.parkingSpawn.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"alley to parking nav path disconnected");
                    var observer=story.observers[0];
                    story.player.Teleport(new Vector3(0,.08f,7)); observer.startDelay=0;
                    observer.transform.rotation=Quaternion.Euler(0,90,0); Physics.SyncTransforms();
                    Require(observer.CanSeePlayer(),"observer failed clear line of sight");
                    var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);obstruction.transform.position=new Vector3(-1.3f,1.5f,7);obstruction.transform.localScale=new Vector3(.3f,3,2);Physics.SyncTransforms();
                    Require(!observer.CanSeePlayer(),"observer saw through solid wall");
                    obstruction.GetComponent<Collider>().enabled=false;UnityEngine.Object.Destroy(obstruction);
                    observer.Detect(false);Require(observer.Mode==ObserverMode.Suspicious,"outside parking must become suspicious");
                    observer.Detect(true);Require(observer.Mode==ObserverMode.Chasing && story.alertLights.Any(x=>x.enabled),"chase/shout activation failed");
                    story.ResetPrologue();
                    story.Caught();next=EditorApplication.timeSinceStartup+2;return;
                case 1: Require(s.prologue_beaten==1,"first capture");story.Caught();next=EditorApplication.timeSinceStartup+5.5;return;
                case 2: story.Caught();next=EditorApplication.timeSinceStartup+2;return;
                case 3:
                    Require(s.prologue_beaten==2,"second capture");
                    Require(Mathf.Abs(story.player.RunMultiplier-2f/3)<.01f,"injury sprint multiplier");
                    story.Caught();next=EditorApplication.timeSinceStartup+5.5;return;
                case 4: story.Caught();next=EditorApplication.timeSinceStartup+2;return;
                case 5:
                    Require(story.State.prologue_beaten==0,"third capture must reset");
                    Require(story.State.clock=="20:56","capture changed story clock");
                    story.player.Teleport(new Vector3(0,.1f,30),0);story.player.SetCrouch(true);
                    story.PlacePhone();Require(story.State.phonePlaced,"phone placement blocked");
                    story.player.Teleport(new Vector3(0,.1f,.5f),0);story.Act(NightAction.ReturnToStart);
                    next=EditorApplication.timeSinceStartup+4.5;return;
                case 6:
                    Require(story.State.stage==NightStage.Parking,"prologue did not transition");
                    story.Act(NightAction.ParkBike);story.Act(NightAction.Phone);
                    Require(story.HasNgan && story.State.messages.Count==6,"pickup/batch A");
                    Require(story.State.notebook.Count==1,"odometer notebook entry");
                    story.Send("camera:yes","Có, tiệm đầu hẻm.");Require(!story.State.linh_knows_shop_camera,"camera answer gate bypassed");
                    story.player.SetPhone(0);story.Act(NightAction.Camera);
                    story.Send("camera:yes","Có, tiệm đầu hẻm.");Require(story.State.linh_knows_shop_camera,"camera choice missing");
                    story.Act(NightAction.Gate);story.EnterRoom();
                    story.player.Teleport(story.roomSpawn.position,90);
                    next=EditorApplication.timeSinceStartup+1;return;
                case 7:
                    var route=new NavMeshPath();
                    // Door collider becomes a trigger at runtime; navigation is for NPCs in the alley.
                    Require(NavMesh.CalculatePath(story.parkingSpawn.position,new Vector3(10.4f,8.4f,20.7f),NavMesh.AllAreas,route)&&route.status==NavMeshPathStatus.PathComplete,"stairs are disconnected");
                    story.Act(NightAction.Deposit);story.Act(NightAction.Bag);
                    story.player.SetPhone(1);story.Send("why","Vì sao máy ở nhà xe?");
                    Require(story.State.clock=="21:26" && story.State.timeJumped,"six-minute jump missing");
                    Require(story.State.messages.Any(x=>x.Contains(story.Dialogue.batchB)),"batch B missing");
                    int count=story.State.messages.Count;story.JumpTime();Require(count==story.State.messages.Count,"time jump duplicated batch");
                    story.player.SetPhone(0);story.ArriveStranger();story.ChooseNumber("blocked");
                    story.WaitUntilLate();story.Rest(true);next=EditorApplication.timeSinceStartup+2.5;return;
                case 8:
                    Require(story.State.stage==NightStage.Ended && story.State.batchD,"sleep ending missing");
                    Require(story.State.clock=="06:29","night boundary wrong");
                    story.RestoreCheckpoint();Require(story.State.stage==NightStage.LateNight,"late checkpoint restore failed");
                    story.Rest(false);next=EditorApplication.timeSinceStartup+8.5;return;
                case 9:
                    Require(story.State.alarm && story.State.clock=="03:12","stay-awake alarm missing");
                    Require(!story.State.batchD,"batch D leaked into awake branch");
                    story.StopAlarm();story.CloseModal();story.FinishNight();
                    Require(story.State.ending=="night1_completed" && !story.State.night1_slept,"awake ending failed");
                    story.RestoreCheckpoint();story.End("bystander");Require(story.State.ending=="bystander","police ending failed");
                    story.RestoreCheckpoint();
                    story.State.hasNganPhone=true;story.LeavePhone();Require(story.State.phone_left_count==1 && !story.HasNgan,"first leave failed");
                    story.State.hasNganPhone=true;story.LeavePhone();Require(story.State.phone_left_count==2,"second leave failed");
                    story.FinishNight();Require(story.State.ending=="bystander","second leave should end at dawn");
                    story.RestoreCheckpoint();story.TogglePower();int before=story.State.messages.Count;story.State.numberArrived=false;story.ArriveStranger();
                    Require(story.State.messages.Count==before && story.State.pendingMessages.Count>0,"powered-off phone received visible message");
                    story.TogglePower();Require(story.State.pendingMessages.Count==0 && story.State.messages.Count>before,"power-on did not flush messages");
                    Debug.Log("NIGHT_ONE_RUNTIME_VALIDATION_PASSED");Finish(0);return;
            }
            next=EditorApplication.timeSinceStartup+1.5;
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static void Finish(int code)
    {EditorApplication.update-=Tick;SessionState.SetBool("NightOneSmoke",false);SessionState.SetBool("NightOneRecord",false);EditorApplication.Exit(code);}
}
