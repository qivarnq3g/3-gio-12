using System;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class NightOneWalkSmoke
{
    static NightOneDirector story;
    static Keyboard keyboard;
    static Key[] held=Array.Empty<Key>();
    static double next;
    static int step;
    static float pausedAt;
    static Vector3 location;
    static Func<bool> until;
    static double deadline;
    static bool breathed;
    static NightOneWalkSmoke(){if(SessionState.GetBool("NightWalk",false))EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        Environment.SetEnvironmentVariable("THREEGIO12_DATA_ROOT",System.IO.Path.GetFullPath(System.IO.Path.Combine("Library","NightWalk",Guid.NewGuid().ToString("N"))));
        Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU","1");
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");SessionState.SetBool("NightWalk",true);
        EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        story=UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();Application.runInBackground=true;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();step=0;Wait(1,null,0);EditorApplication.update+=Tick;
    }
    static void Check(bool value,string message){if(!value)throw new Exception("WALK REVIEW: "+message);}
    static void Wait(double least,Func<bool> condition,double limit){var now=EditorApplication.timeSinceStartup;next=now+least;until=condition;deadline=now+Math.Max(least,limit);}
    static bool Climbed(Transform body)=>body.position.y>.3f && body.position.z>14.5f;
    static string Capture(string name)=>System.IO.Path.GetFullPath(System.IO.Path.Combine("Logs",name));
    static void Tick()
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));EditorApplication.QueuePlayerLoopUpdate();
        if(step==1 && story.player.GetComponent<NightInjuryFeedback>().IsBreathing)breathed=true;
        var now=EditorApplication.timeSinceStartup;
        if(now<next || (until!=null && !until() && now<deadline))return;
        try
        {
            var player=story.player;
            switch(step++)
            {
                case 0:
                    player.ResumeControl();foreach(var observer in story.observers)observer.enabled=false;
                    story.State.stage=NightStage.Room;story.State.timeJumped=true;story.State.numberArrived=true;story.State.hasNganPhone=true;story.State.prologue_beaten=2;
                    player.Teleport(new Vector3(6,.05f,13.2f));held=new[]{Key.W};breathed=false;Wait(0,()=>Climbed(player.transform)&&breathed,10);break;
                case 1:
                    Check(Climbed(player.transform),"W did not climb actual stairs");
                    Check(breathed,"real stair climbing did not produce breath feedback");
                    ScreenCapture.CaptureScreenshot(Capture("3gio12-stairs-review.png"));held=new[]{Key.Escape};Wait(.2,()=>player.IsPaused,5);break;
                case 2:
                    Check(player.IsPaused && Time.timeScale==0,"Esc did not pause gameplay");pausedAt=Time.time;location=player.transform.position;held=Array.Empty<Key>();Wait(1,null,0);break;
                case 3:
                    Check(Mathf.Abs(Time.time-pausedAt)<.001f && Vector3.Distance(location,player.transform.position)<.001f,"paused world moved");held=new[]{Key.Escape};Wait(.2,()=>!player.IsPaused,5);break;
                case 4:
                    Check(!player.IsPaused && Time.timeScale==1,"Esc did not resume");held=new[]{Key.W,Key.LeftShift};player.SetPhone(1);location=player.transform.position;pausedAt=Time.time;Wait(1,()=>Time.time>pausedAt,5);break;
                case 5:
                    Check(Time.timeScale==1 && Time.time>pausedAt && Vector3.Distance(location,player.transform.position)<.05f,"phone paused world or moved character");
                    held=Array.Empty<Key>();player.SetPhone(0);player.Teleport(new Vector3(0,.05f,.5f),180);Wait(1,null,0);break;
                case 6:
                    ScreenCapture.CaptureScreenshot(Capture("3gio12-arms-overlay-review.png"));Wait(1,null,0);break;
                case 7:
                    Debug.Log("NIGHT_ONE_WALK_REVIEW_PASSED");Finish(0);return;
            }
        }
        catch(Exception error){Debug.LogException(error);Finish(1);}
    }
    static void Finish(int code)
    {held=Array.Empty<Key>();story.player.ResumeControl();InputSystem.RemoveDevice(keyboard);EditorApplication.update-=Tick;SessionState.SetBool("NightWalk",false);EditorApplication.Exit(code);}
}
