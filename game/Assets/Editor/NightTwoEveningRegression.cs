using System;
using System.IO;
using System.Linq;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NightTwoEveningRegression
{
    const string Flag = "NightTwoEveningRegression";
    static NightOneDirector story;
    static NightTwoMorning night;
    static NightPersistence persistence;
    static int step;
    static double deadline;

    static NightTwoEveningRegression()
    { if (SessionState.GetBool(Flag, false)) EditorApplication.playModeStateChanged += Changed; }

    public static void Run()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT"))) throw new Exception("Set isolated THREEGIO12_DATA_ROOT.");
        Environment.SetEnvironmentVariable("THREEGIO12_SKIP_START_MENU", "1");
        SessionState.SetBool(Flag, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");
        EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredPlayMode) return;
        Application.runInBackground = true;
        step = 0; deadline = EditorApplication.timeSinceStartup + 90; EditorApplication.update += Tick;
    }

    static void Check(bool value, string text) { if (!value) throw new Exception("NIGHT_TWO_EVENING: " + text); }
    static bool HasMessage(string part) => story.State.messages.Any(x => x.Contains(part));

    static void Drive()
    {
        for (int i = 0; i < 2000 && !FixedRouteRide.AtStop(night.State); i++) FixedRouteRide.Step(night.State, 1, false, .1f);
        Check(FixedRouteRide.AtStop(night.State), "ride never arrived");
        Check(night.Dismount(), "could not dismount");
    }

    static void PlayMorning(bool slept, string reply, string numberChoice)
    {
        Check(persistence.StartNewGame(), "new game");
        story.State.hasNganPhone = true; story.State.night1_slept = slept; story.State.number_choice = numberChoice;
        story.FinishNight(); Check(night.Begin(), "night 1 handoff");
        Check(night.StopAlarm(), "alarm");
        story.player.Teleport(new Vector3(96, .08f, 7)); Check(night.EatBreakfast(), "breakfast");
        if (reply != null) Check(night.ReplyRoom(reply == "given"), "morning room reply");
        story.player.Teleport(NightTwoMorning.Parking); Check(night.Mount(), "morning mount");
        Drive(); Check(night.BuyFurniture(), "purchase"); Check(night.Mount(), "morning return"); Drive();
        story.player.Teleport(new Vector3(100, .08f, 7)); night.State.gateElapsed = 0; Check(night.PassGate(), "gate");
        story.player.Teleport(NightTwoMorning.Room);
        for (int i = 0; i < 4; i++) Check(night.Assemble(i), "shelf");
        Check(night.TestLamp(), "lamp"); Check(night.Rest(), "rest");
    }

    static void RiceTrip()
    {
        story.player.Teleport(NightTwoMorning.Room); Check(!night.Mount(), "remote evening mount");
        story.player.Teleport(NightTwoMorning.EveningBike); Check(night.Mount(), "evening mount");
        Check(night.State.stage == MorningStage.ToRice && night.State.sawBikeMoved && night.State.redLightCleared, "evening ride setup");
        Drive(); Check(night.State.stage == MorningStage.RiceShop, "rice shop arrival");
        Check(!night.Mount(), "left without rice");
        story.player.Teleport(NightTwoMorning.Room); Check(!night.BuyRice(), "remote rice");
        story.player.Teleport(NightTwoMorning.RiceShop); Check(night.BuyRice() && !night.BuyRice(), "buy rice once");
        story.player.Teleport(NightTwoMorning.Store); Check(night.Mount(), "rice return mount");
        Drive(); Check(night.State.stage == MorningStage.Home, "home arrival");
    }

    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Night two evening test timeout");
            story = UnityEngine.Object.FindFirstObjectByType<NightOneDirector>();
            if (story == null || (persistence = story.GetComponent<NightPersistence>())?.Ready != true || story.Morning == null) return;
            night = story.Morning; story.player.ResumeControl();
            if (step % 2 == 1 && night.Transitioning) return;
            switch (step)
            {
                case 0: PlayMorning(true, "given", "ignored"); step = 1; return;
                case 1: TeaBranch(); step = 2; return;
                case 2:
                    if (!Application.isBatchMode && !File.Exists(Path.Combine(NightPersistence.DataRoot, "evening-preview.png"))) return;
                    Sleep(); step = 3; return;
                case 3:
                    Check(night.State.stage == MorningStage.Ended && night.State.night2Slept && !night.State.vigilDozed, "sleep ending");
                    Check(story.State.clock == "06:29" && story.State.clockDay == 2, "sleep clock");
                    Check(night.State.strangerDelivered == 4 && HasMessage("me de den phong con"), "late stranger message");
                    Check(persistence.Flush() && persistence.ContinueSaved() && night.State.stage == MorningStage.Ended, "ended reload");
                    PlayMorning(false, null, "ignored"); step = 5; return;
                case 5: DozeBranch(); step = 7; return;
                case 7:
                    Check(night.State.stage == MorningStage.Vigil && night.State.vigilSeconds < 1 &&!story.player.ExternalUI, "two-night vigil starts in room");
                    night.VigilTick(5, true); night.VigilTick(9, false);
                    Check(night.State.stage == MorningStage.Vigil, "dozed too early");
                    night.VigilTick(2, false);
                    Check(night.State.stage == MorningStage.Ended && night.State.vigilDozed && !night.State.teaArrived, "doze ending");
                    PlayMorning(true, "refused", "blocked"); step = 9; return;
                case 9: AlarmBranch(); step = 11; return;
                case 11:
                    Check(night.State.stage == MorningStage.Vigil && night.State.vigilSeconds == NightTwoEvening.VigilLength && story.State.clock == "03:12", "single vigil rings alarm");
                    var ringing = story.CaptureSave(); Check(NightSaveData.Valid(ringing), "alarm save invalid");
                    Check(story.ApplySave(ringing) && night.StopNightTwoAlarm(), "alarm restored after load");
                    Check(night.State.stage == MorningStage.Ended && !night.State.vigilDozed && night.State.strangerDelivered == 0, "alarm ending");
                    Check(persistence.StartNewGame() && !story.InNightTwo && story.observers.All(x => x.enabled), "restart did not restore night one");
                    Finish("NIGHT_TWO_EVENING_PASSED"); return;
            }
        }
        catch (Exception e) { Debug.LogException(e); Finish(null); }
    }

    static void TeaBranch()
    {
        Check(night.State.stage == MorningStage.Evening && story.State.clock == "21:00" && night.State.odometer == 12425, "evening start");
        Check(HasMessage("Tối cô gửi cháu ly trà sữa nhé") && HasMessage("Cháu nhớ ăn tối nhé.") && HasMessage("Cô nói thật với cháu"), "daytime batches");
        RiceTrip();
        Check(night.State.odometer == 12426 && night.State.odoLog.Count == 8 && night.State.odoLog[4].km == 12425, "evening mileage");
        Check(night.State.odoLog.Select(x => x.engineOn).SequenceEqual(new[] { true, false, true, false, true, false, true, false }), "evening engine log");
        Check(night.State.teaArrived && !night.State.teaCallPending && night.State.lienEveningSaid, "tea announced on return");
        Check(night.State.strangerDelivered >= 2 && HasMessage("khong ai biet con di da lat"), "stranger messages");
        story.player.Teleport(NightTwoMorning.Room); Check(!night.TakeTea(), "remote tea");
        story.player.Teleport(NightTwoMorning.LienDesk); Check(night.TakeTea() && story.player.ExternalUI, "tea page");
        Check(night.DecideTea(true) && night.State.drankMilkTea && !night.DecideTea(false), "tea choice");
        Check(!night.SendEvening("Q5") && !night.SendEvening("room:given"), "locked question sent");
        night.OpenNganTab("photos"); night.OpenNganTab("storage");
        Check(night.SendEvening("Q5") && night.SendEvening("Q4") && night.SendEvening("Q6") && !night.SendEvening("Q4"), "evening questions");
        Check(night.State.momSuspicion == 0 && night.SendEvening("Q7") && night.State.momSuspicion == 1, "Q7 suspicion");
        night.OpenNganTab("calculator"); night.PressCalculator("1"); night.PressCalculator("2"); night.PressCalculator("=");
        Check(night.State.vaultWrongAttempts == 1, "vault attempt");
        Check(night.SetNganPasscode() && !night.SetNganPasscode(), "passcode");
        var snap = story.CaptureSave(); Check(NightSaveData.Valid(snap), "evening save invalid");
        night.State.vaultWrongAttempts = 9;
        Check(story.ApplySave(snap) && night.State.vaultWrongAttempts == 1 && night.State.setNganPasscode && night.State.eveningSent.Count == 4, "evening restore");
        var bad = story.CaptureSave(); bad.state.nightTwo.vigilSeconds = float.NaN; Check(!NightSaveData.Valid(bad), "NaN vigil accepted");
        bad = story.CaptureSave(); bad.state.nightTwo.eveningSent.Add("Q9"); Check(!NightSaveData.Valid(bad), "unknown question accepted");
        story.player.SetPhone(1); night.OpenNganTab("messages");
        string preview = Path.Combine(NightPersistence.DataRoot, "evening-preview.png");
        if (File.Exists(preview)) File.Delete(preview);
        ScreenCapture.CaptureScreenshot(preview);
    }

    static void Sleep()
    {
        story.player.SetPhone(0);
        story.player.Teleport(NightTwoMorning.Room); Check(night.OpenBed() && night.ChooseNight(true), "sleep choice");
    }

    static void DozeBranch()
    {
        Check(HasMessage("Cháu ở phòng mấy hả cháu?") && !night.State.teaArrived, "awake branch batch");
        Check(night.SendEvening("room:given") && night.State.roomReply == "given" && !night.SendEvening("room:refused"), "evening room reply");
        story.player.Teleport(NightTwoMorning.Room); Check(!night.ChooseNight(false), "night choice before dinner");
        RiceTrip();
        Check(!night.State.teaArrived && night.State.odometer == 12415, "awake branch mileage");
        story.player.Teleport(NightTwoMorning.Room); Check(night.ChooseNight(false), "vigil choice");
    }

    static void AlarmBranch()
    {
        Check(night.State.momSuspicion == 1 && !HasMessage("Cháu nhớ ăn tối nhé.") && HasMessage("Ừ, không sao cháu."), "suspicious batch");
        RiceTrip();
        Check(night.State.strangerDelivered == 0 && !night.State.teaArrived, "blocked number or tea leaked");
        story.player.Teleport(NightTwoMorning.Room); Check(night.ChooseNight(false), "single vigil choice");
    }

    static void Finish(string marker)
    {
        EditorApplication.update -= Tick; SessionState.SetBool(Flag, false);
        if (marker != null) Debug.Log(marker);
        EditorApplication.Exit(marker == null ? 1 : 0);
    }
}
