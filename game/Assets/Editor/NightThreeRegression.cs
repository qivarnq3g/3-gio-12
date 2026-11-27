using System;
using System.IO;
using System.Linq;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NightThreeRegression
{
    const string Flag="NightThreeRegression";
    static NightOneDirector story;
    static NightTwoMorning night;
    static NightPersistence persistence;
    static bool reading;
    static int step;
    static double deadline;
    static NightSaveData decisions;
    static float beforeDawn;
    static NightThreeRegression(){if(SessionState.GetBool(Flag,false))EditorApplication.playModeStateChanged+=Changed;}
    public static void Write()=>Run(false);
    public static void Read()=>Run(true);
    static void Run(bool read)
    {
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT")))throw new Exception("Set isolated THREEGIO12_DATA_ROOT.");
        Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU",read?"0":"1");SessionState.SetBool(Flag,true);SessionState.SetBool(Flag+"Read",read);
        EditorSceneManager.OpenScene(NightOneRealisticSetup.ScenePath);
        EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange mode)
    {
        if(mode!=PlayModeStateChange.EnteredPlayMode)return;
        NightTwoSceneFixtures.AddMorning();
        var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var desk = all.FirstOrDefault(t=>t.name=="Night3_PoliceDesk");
        if (desk == null || !desk.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled)
            || !desk.GetComponentsInChildren<MeshFilter>(true).Any(m => AssetDatabase.GetAssetPath(m.sharedMesh) == "Assets/Art/Models/Props/Night3_PoliceDesk.fbx"))
            throw new Exception("Night 3 regression requires the real police desk FBX; no fixture is permitted.");
        Debug.Log("NIGHT_THREE_REAL_POLICE_DESK_NO_FIXTURE");
        Application.runInBackground=true;reading=SessionState.GetBool(Flag+"Read",false);step=0;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Tick;
    }
    static void Check(bool ok,string reason){if(!ok)throw new Exception("NIGHT_THREE: "+reason);}
    static void Setup(bool firstSlept,bool secondSlept,bool blocked)
    {
        Check(persistence.StartNewGame(),"start new");Check(!night.BeginThree(),"night 3 starts before night 2");
        story.State.hasNganPhone=true;story.State.night1_slept=firstSlept;story.State.prologue_beaten=2;
        story.State.number_choice=blocked?"blocked":"ignored";story.FinishNight();Check(night.Begin(),"N1 to N2");
        // N2 is covered end-to-end by NightTwoEveningRegression. Seed its final
        // checkpoint to test the N2 -> N3 handoff without duplicating its suite.
        night.State.stage=MorningStage.Ended;night.State.assemblySteps=4;night.State.lampTested=true;night.State.night2Slept=secondSlept;
        night.State.odometer=firstSlept?12426:12415;night.State.sawStorage=true;night.State.vaultWrongAttempts=1;night.State.roomReply="given";
        story.Clock.Set("06:29",2);Check(night.BeginThree(),"N2 to N3");
        Check(!night.BeginThree() && story.State.prologue_beaten==2 && story.observers.All(x=>!x.enabled),"handoff/observer isolation");
        Check(night.Third.odometer==(firstSlept?12426:12415)+(secondSlept?11:0),"overnight odometer branch");
    }
    static void Drive()
    {
        for(int i=0;i<2000 && night.Third.routeMetres<FixedRouteRide.Length;i++)night.ThreeRideTick(true,false,.1f);
        Check(night.ThreeDismount(),"gas route arrival");
    }
    static void MorningTrip()
    {
        Check(night.StopThreeAlarm(),"wake");Check(!night.ThreeMount(),"remote mount");
        story.player.Teleport(NightTwoMorning.Parking);Check(night.ThreeMount() && !night.ThreeDismount(),"mount/premature exit");
        night.ThreeRideTick(true,false,.1f);var mid=story.CaptureSave();float metres=night.Third.routeMetres;
        Check(NightSaveData.Valid(mid) && story.ApplySave(mid) && night.Third.riding && story.player.ExternalUI && night.Third.routeMetres==metres,"mid-ride reload");
        Drive();Check(!night.ThreeMount(),"left without fuel");Check(night.RefuelThree() && night.ThreeMount(),"refuel/return");Drive();
        Check(!night.ThreeNap(),"remote/no alarm nap accepted");
        story.player.Teleport(NightTwoMorning.Room);Check(!night.ThreeNap(),"nap without 13:00 alarm");
        story.player.SetPhone(1);Check(!night.OpenThreePhoneTab("profile"),"profile revealed before evening");
        story.player.SetPhone(2);Check(night.SetThreeNapAlarm(),"set 13:00");story.player.SetPhone(0);
        Check(night.ThreeNap() && story.CaptureSave()==null,"nap checkpoint atomicity");
    }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Night 3 timeout");
            story=UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
            if(story==null || (persistence=story.GetComponent<NightPersistence>())?.Ready!=true || story.Morning==null)return;
            night=story.Morning;story.player.ResumeControl();
            if(step!=0 && night.Transitioning)return;
            if(!reading)WriteTick();else ReadTick();
        }
        catch(Exception e){Debug.LogException(e);Finish(null);}
    }
    static void WriteTick()
    {
        switch(step)
        {
            case 0:Setup(true,true,true);MorningTrip();step=1;return;
            case 1:
                Check(night.Third.stage==NightThreeStage.Evening && night.Third.odometer==12449 && story.State.clock=="21:00","evening mileage/clock");
                night.AdvanceThreeMinutes(10);Check(!night.Third.v1Delivered && !night.Third.teaArrived,"blocked voice/wrong tea branch");
                Check(night.UnblockThreeNumber() && night.Third.v1Delivered && !night.UnblockThreeNumber(),"unblock backlog");
                Check(night.HearThreeVoice(false),"V1");Check(!night.AskThreeCode() && !night.AnswerThreeCall(),"proof gates bypassed");
                story.player.Teleport(NightTwoMorning.Room);story.player.RestoreView(80,false);story.player.SetPhone(2);Physics.SyncTransforms();
                Check(night.CaptureThreePhoto() && !night.Third.photos[0].caseBack,"ordinary photo became evidence");
                story.player.RestoreView(0,false);story.player.eyes.transform.position=NightTwoMorning.CaseBackPosition+Vector3.up*.4f;story.player.eyes.transform.LookAt(NightTwoMorning.CaseBackPosition);Physics.SyncTransforms();
                Check(night.CaptureThreePhoto() && night.Third.photos[1].caseBack,"case back camera ray");
                Check(night.Third.photos.All(p=>File.Exists(Path.Combine(NightPersistence.DataRoot,"captures",p.id+".png"))),"actual PNG missing");
                var png=new Texture2D(2,2);png.LoadImage(File.ReadAllBytes(Path.Combine(NightPersistence.DataRoot,"captures",night.Third.photos[1].id+".png")));
                Check(png.width==512 && png.height==288,"camera PNG dimensions");
                var pixels=png.GetPixels();float spread=pixels.Max(c=>c.grayscale)-pixels.Min(c=>c.grayscale);UnityEngine.Object.Destroy(png);
                Check(spread>.01f,"camera PNG is a uniform/empty render");
                var wrong=File.ReadAllBytes(Path.Combine(NightPersistence.DataRoot,"captures",night.Third.photos[0].id+".png"));
                var right=File.ReadAllBytes(Path.Combine(NightPersistence.DataRoot,"captures",night.Third.photos[1].id+".png"));
                Check(!wrong.SequenceEqual(right),"camera frame was not refreshed between photos");
                story.player.SetPhone(1);Check(night.OpenHoaProfile() && story.CaptureSave()==null,"door sequence start");step=2;return;
            case 2:
                Check(night.Third.doorLocked && night.Third.openedHoaProfile && !story.player.ExternalUI,"door sequence did not restore control");
                story.player.SetPhone(2);Check(night.ContactHoaThree("own_phone") && !night.ContactHoaThree("own_phone"),"contact branch/duplicate");
                Check(!night.SendThreePhoto(night.Third.photos[0].id),"wrong photo verified");
                story.player.SetPhone(1);Check(!night.SendThreePhoto(night.Third.photos[1].id),"Ngân phone sends own photo");
                story.player.SetPhone(2);Check(night.SendThreePhoto(night.Third.photos[1].id),"proof send");
                var bad=story.CaptureSave();bad.state.nightThree.speed=float.NaN;
                Check(!NightSaveData.Valid(bad) && !story.ApplySave(bad) && night.Third.sentCasePhoto,"invalid snapshot mutated state");
                Check(persistence.Flush(),"save sent photo");
                Check(night.OpenThreePhoneTab("photos"),"own gallery tab");
                string image=Path.Combine(NightPersistence.DataRoot,"night-three-preview.png");if(File.Exists(image))File.Delete(image);
                if(Application.isBatchMode){Finish("NIGHT_THREE_WRITE_PASSED");return;}
                ScreenCapture.CaptureScreenshot(image);step=3;return;
            default:if(File.Exists(Path.Combine(NightPersistence.DataRoot,"night-three-preview.png")))Finish("NIGHT_THREE_WRITE_PASSED");return;
        }
    }
    static void ReadTick()
    {
        switch(step)
        {
            case 0:
                Check(persistence.ContinueSaved(),"fresh process reload");
                Check(story.InNightThree && night.Third.sentCasePhoto && night.Third.contactMode=="own_phone" && night.Third.photos.Count==2 && night.Third.doorLocked,"cross-process state");
                Check(night.AnswerThreeCall(),"incoming call");
                night.State.sawStorage=false;Check(!night.AskThreeCode(),"code without seeing storage in N2");night.State.sawStorage=true;
                Check(night.AskThreeCode(),"vault code");story.player.SetPhone(1);
                Check(!night.EnterThreeVault("000000") && night.EnterThreeVault("472619"),"calculator code");
                Check(night.ViewThreeVault("journal") && night.ViewThreeVault("intruder") && night.ViewThreeVault("album"),"outer vault");
                Check(night.Transitioning && story.CaptureSave()==null,"album glance atomicity");step=10;return;
            case 10:
                Check(!story.player.ExternalUI && night.Third.viewedVault.Contains("album"),"glance did not return control");
                Check(night.Third.battery==9 && !night.WaitThreeLate(),"low battery bypassed");
                story.player.SetPhone(0);story.player.Teleport(NightTwoMorning.Parking);
                Check(!night.FindThreeCable(),"remote bag");story.player.SetPhone(0);story.player.Teleport(NightTwoMorning.ThreeBag+Vector3.left);
                Check(night.FindThreeCable() && !night.FindThreeCable(),"cable/medicine");story.player.Teleport(NightTwoMorning.Room);
                Check(night.ChargeThree() && night.Third.battery==100 && story.State.notebook.Any(x=>x.Contains("zolpidem 5 mg")),"charge/medicine note");
                Check(night.DecideThreePhone(true),"keep phone");decisions=story.CaptureSave();beforeDawn=night.Third.odometer;
                Check(night.AdviseHoaThree("R2") && !night.AdviseHoaThree("R2"),"R2 duplicate");
                Check(night.WaitThreeLate() && night.ChooseThreeNight(false),"stay awake");step=1;return;
            case 1:
                Check(night.Third.stage==NightThreeStage.DawnWaiting && !night.Third.v2Delivered,"R2 failed to suppress V2");
                Check(night.FinishThreeDawn() && night.Third.odometer==beforeDawn && story.State.clockDay==3,"dawn mileage");
                Check(persistence.Flush() && persistence.ContinueSaved() && night.Third.stage==NightThreeStage.Completed,"completed reload");
                Check(story.ApplySave(decisions),"restore branch point");story.player.SetPhone(0);
                Check(night.DecideThreePhone(false),"police intent");Check(!night.ThreeHandInPhone(),"remote hand-in");
                story.player.Teleport(NightTwoMorning.ThreePolice+Vector3.left);
                Check(night.ThreeHandInPhone() && night.Third.stage==NightThreeStage.Scapegoat && NightSaveData.Valid(story.CaptureSave()),"scapegoat ending");
                Check(story.ApplySave(decisions),"restore no-advice branch");story.player.SetPhone(0);story.player.Teleport(NightTwoMorning.Room);
                Check(night.WaitThreeLate() && night.ChooseThreeNight(true),"sleep without R2/R3");step=2;return;
            case 2:
                Check(night.Third.stage==NightThreeStage.Completed && night.Third.v2Delivered && !night.Third.heardV2 && night.Third.night3Slept && night.Third.odometer==beforeDawn,"sleep V2/dawn");
                var other=JsonUtility.FromJson<NightSaveData>(JsonUtility.ToJson(decisions));var t=other.state.nightThree;
                t.contactMode="none";t.sentCasePhoto=t.answeredCall=t.gotVaultCode=t.openedVault=t.keptPhone=t.policeIntent=false;t.viewedVault.Clear();
                Check(story.ApplySave(other),"prepare Ngan contact branch");story.player.SetPhone(1);
                Check(night.ContactHoaThree("ngan_phone") && night.Third.contactMode=="ngan_phone","Ngan contact mode");
                Check(!night.SendThreePhoto(night.Third.photos[1].id),"Ngan sends own gallery");
                Check(story.ApplySave(decisions),"restore R3 branch");story.player.SetPhone(0);story.player.Teleport(NightTwoMorning.Room);
                Check(night.AdviseHoaThree("R3") && night.WaitThreeLate() && night.ChooseThreeNight(false),"R3 choice");step=11;return;
            case 11:
                Check(!night.Third.v2Delivered && night.FinishThreeDawn(),"R3 did not suppress V2");
                Setup(false,false,false);MorningTrip();step=3;return;
            case 3:
                Check(night.Third.odometer==12427,"awake second night got extra 11 km");night.AdvanceThreeMinutes(10);
                Check(night.Third.teaArrived && night.Third.v1Delivered,"delayed tea/voice");
                Check(night.DecideThreeTea(false),"leave tea");
                Check(night.Third.contactMode=="none" && !night.AskThreeCode() && !night.EnterThreeVault("472619"),"no-contact branch got code");
                story.player.SetPhone(0);story.player.Teleport(NightTwoMorning.Room);beforeDawn=night.Third.odometer;
                Check(night.WaitThreeLate() && night.ChooseThreeNight(true),"no-contact completion");step=4;return;
            default:
                Check(night.Third.stage==NightThreeStage.Completed && !night.Third.v2Delivered && night.Third.odometer==beforeDawn,"no-contact ending");
                Check(persistence.StartNewGame() && !story.InNightThree && !story.InNightTwo && story.observers.All(x=>x.enabled),"restart old gameplay");
                Finish("NIGHT_THREE_READ_PASSED");return;
        }
    }
    static void Finish(string marker)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Flag,false);
        if(marker!=null)Debug.Log(marker);EditorApplication.Exit(marker==null?1:0);
    }
}
