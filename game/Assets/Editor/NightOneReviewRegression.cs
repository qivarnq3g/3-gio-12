using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class NightOneReviewRegression
{
    static NightOneDirector story;
    static double next;
    static int step, recalls;
    static float pausedAt;
    static Vector3 movedBag;
    static readonly List<NightTelemetry> packets=new List<NightTelemetry>();
    static NightOneReviewRegression()
    {if(SessionState.GetBool("NightReview",false))EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");
        SessionState.SetBool("NightReview",true);EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        Application.runInBackground=true;story=UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
        packets.Clear();story.TelemetryProduced+=packets.Add;
        story.InternalEvent+=(type,detail)=>{if(type=="lien_called")recalls++;};
        step=0;recalls=0;next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;
    }
    static void Check(bool pass,string message){if(!pass)throw new Exception("REVIEW REGRESSION: "+message);}
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            var player=story.player;
            switch(step++)
            {
                case 0:
                    player.ResumeControl();foreach(var observer in story.observers)observer.enabled=false;
                    Check(player.gameObject.layer==PlayerGeometry.PlayerLayer && PlayerGeometry.PlayerLayer!=2,"player layer not migrated");
                    var stack=player.eyes.GetUniversalAdditionalCameraData().cameraStack;
                    Check(stack.Count==1 && stack[0].GetUniversalAdditionalCameraData().renderType==CameraRenderType.Overlay && stack[0].GetUniversalAdditionalCameraData().clearDepth,"arms camera stack invalid");
                    Check(stack[0].cullingMask==PlayerGeometry.ArmsMask && (player.eyes.cullingMask&PlayerGeometry.ArmsMask)==0,"arms render in world camera");
                    Check(!story.Clock.Set("20:55") && story.State.clock=="20:56","clock moved backwards");
                    Check(!story.PrologueDeparted && story.PrologueElapsed==0,"observer timer started before departure");
                    var lien=story.observers.First(x=>x.once);
                    Check(!lien.IsOnSchedule(50,false,0) && !lien.IsOnSchedule(39.9f,true,0) && lien.IsOnSchedule(40,true,0) && !lien.IsOnSchedule(55,true,0),"Lien schedule not relative to departure");
                    story.parkingAlertLight.enabled=true;
                    Check(story.IsIlluminated(new Vector3(0,1.6f,32)),"parking alert light does not illuminate parking");story.parkingAlertLight.enabled=false;
                    var bike=story.passingBikes[0];bike.headlight.enabled=true;
                    Check(bike.VisibleAt(15) && !bike.VisibleAt(23),"passing bike timing wrong");
                    Check(story.IsIlluminated(bike.headlight.transform.position+bike.headlight.transform.forward*3),"bike headlight not included in visibility");bike.headlight.enabled=false;
                    story.State.prologue_beaten=2;story.ToggleNotebook();Check(!story.NotebookOpen && story.Modal=="","notebook opened in prologue");
                    story.OpenModal("notebook");Check(story.Modal=="","direct notebook modal bypass");
                    story.RestoreCheckpoint();Check(story.State.prologue_beaten==2 && Mathf.Abs(player.RunMultiplier-2f/3)<.01f && story.parkingAlertLight.enabled,"recovery cleared injury/alert");
                    player.Teleport(new Vector3(0,-11,10));break;
                case 1:
                    Check(story.State.prologue_beaten==2 && player.transform.position.y>0,"fall recovery cleared captures");
                    story.ResetPrologue();player.SetCrouch(true);
                    var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=player.transform.position+Vector3.up*1.79f;ceiling.transform.localScale=new Vector3(2,.02f,2);Physics.SyncTransforms();
                    Check(!player.SetCrouch(false),"standing check missed top 3 cm");
                    ceiling.GetComponent<Collider>().enabled=false;UnityEngine.Object.Destroy(ceiling);Physics.SyncTransforms();Check(player.SetCrouch(false),"standing check hit own player collider");
                    player.SetPaused(true);pausedAt=Time.time;Check(Time.timeScale==0 && AudioListener.pause,"pause did not stop world/audio");
                    next=EditorApplication.timeSinceStartup+.4;return;
                case 2:
                    Check(Mathf.Abs(Time.time-pausedAt)<.001f,"world clock advanced during pause");player.ResumeControl();Check(Time.timeScale==1 && !AudioListener.pause,"resume did not restore clock/audio");
                    story.State.stage=NightStage.Parking;story.State.phonePlaced=true;story.Clock.Set("21:00");
                    story.Act(NightAction.ParkBike);story.Act(NightAction.Phone);player.SetPhone(0);story.Act(NightAction.Gate);story.EnterRoom();player.Teleport(story.roomSpawn.position,90);
                    story.Act(NightAction.MoveBag);movedBag=story.bag.transform.position;
                    player.SetPhone(1);story.Send("camera:unknown","Cháu không biết.");
                    Check(story.State.timeJumped && story.State.clock=="21:26","phone jump failed");
                    Check(Vector3.Distance(story.bag.transform.position,movedBag)<.001f && story.State.bagOpen && !story.State.bagMoved,"pre-moved bag teleported or pocket stayed shut");
                    player.SetPhone(0);player.Teleport(story.parkingSpawn.position);story.LeavePhone();
                    Check(story.State.clock=="21:40" && story.State.lienCalled && recalls==1,"first recall missing");
                    story.ArriveStranger();story.ArriveStranger();
                    Check(story.State.clock=="21:40" && recalls==1,"late stranger moved clock back or called twice");
                    story.Act(NightAction.Phone);int count=story.State.messages.Count;story.Send("why","Vì sao máy ở nhà xe?");
                    Check(story.State.messages.Count==count+1 && story.State.afterJumpQuestions.Contains("why"),"after-jump question answered immediately");
                    player.SetPhone(0);story.WaitUntilLate();story.Rest(true);next=EditorApplication.timeSinceStartup+2.5;return;
                case 3:
                    Check(story.State.stage==NightStage.Ended && story.State.clockDay==1 && story.State.clock=="06:29","midnight progression failed");
                    Check(story.State.messages.Any(x=>x.Contains("03:40")&&x.Contains(story.Dialogue.why)),"batch D did not answer late why question");
                    story.RestoreCheckpoint();story.State.phone_left_count=0;story.LeavePhone();Check(!story.HasNgan && story.State.phone_left_count==1,"remote phone fixture invalid");
                    story.Rest(false);next=EditorApplication.timeSinceStartup+8.5;return;
                case 4:
                    Check(story.State.alarm && story.State.clock=="03:12" && !story.AlarmAudible,"phone at parking rang inside room");
                    story.CloseModal();story.Act(NightAction.Phone);Check(story.AlarmAudible,"picked-up phone alarm did not become audible");story.TogglePower();break;
                case 5:
                    Check(!story.AlarmAudible,"powered-off alarm still audible");story.StopAlarm();story.CloseModal();story.FinishNight();
                    story.RestoreCheckpoint();story.State.prologue_beaten=2;story.Act(NightAction.Gate);Check(story.Subtitle.Contains("găng xám"),"Lien glove line missing");
                    Check(story.greyGlove.activeSelf,"Lien glove prop missing");
                    var feedback=player.GetComponent<NightInjuryFeedback>();feedback.ProcessMotion(new Vector3(.1f,.1f,0),false);
                    Check(feedback.IsBreathing,"injury breathing missing on stairs after prologue");story.State.prologue_beaten=0;feedback.ProcessMotion(Vector3.zero,false);Check(!feedback.IsBreathing,"uninjured breathing continued");
                    Check(packets.OfType<ChoiceTelemetry>().Any(x=>x.choiceId=="0b"),"leave-phone choice missing");
                    packets.Add(new NightStartedTelemetry(false));
                    packets.Add(new CaughtTelemetry("n1.alley","n1.alley_start",Vector3.zero));
                    packets.Add(new ChoiceTelemetry("1a"));packets.Add(new ChoiceTelemetry("1b"));packets.Add(new ChoiceTelemetry("1c"));packets.Add(new EndingTelemetry("bystander"));
                    File.WriteAllLines("/private/tmp/3gio12-review-events.jsonl",packets.Select(x=>x.ToJson()));
                    story.RestoreCheckpoint();story.State.timeJumped=false;story.State.hasNganPhone=false;story.State.stage=NightStage.Room;
                    story.State.bagMoved=false;story.State.bagDisplaced=false;story.State.bagOpen=false;story.bag.transform.position=story.bagAtDoor.position;
                    player.Teleport(story.roomSpawn.position,90);story.JumpTime();
                    Check(story.Busy && story.BlackTitle!="" && !story.State.bagMoved,"unconcealed bag jump visible before sitting");
                    next=EditorApplication.timeSinceStartup+.5;return;
                case 6:
                    Check(!story.Busy && story.State.bagMoved && !story.State.bagChangePending,"concealed sitting transition failed");
                    Debug.Log("NIGHT_ONE_REVIEW_REGRESSIONS_PASSED");Finish(0);return;
            }
            next=EditorApplication.timeSinceStartup+1;
        }
        catch(Exception error){Debug.LogException(error);Finish(1);}
    }
    static void Finish(int code)
    {story.player.ResumeControl();EditorApplication.update-=Tick;SessionState.SetBool("NightReview",false);EditorApplication.Exit(code);}
}
