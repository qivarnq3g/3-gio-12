using System.Collections;
using BaGio12.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Story
{
    // First vertical slice of #22. The temporary world is deliberately isolated
    // from Quang's environment branch; all progression lives in the save DTO.
    public sealed partial class NightTwoMorning : MonoBehaviour
    {
        public NightOneDirector story;
        public NightTwoMorningState State => story.InNightTwo ? story.State.nightTwo : null;
        public bool Transitioning { get; private set; }
        public static readonly Vector3 Room = new Vector3(100, .08f, 0);
        public static readonly Vector3 Parking = new Vector3(108, .08f, 12);
        public static readonly Vector3 Store = new Vector3(108, .08f, 92);
        public static readonly Vector3 Lien = new Vector3(102, 0, 9);
        string page = "";
        bool wasActive;
        float fatiguePulse;

        public bool Begin()
        {
            if (story.InNightTwo || story.Busy || story.State.ending != "night1_completed") return false;
            story.State.nightTwo = NightTwoMorningState.FromNightOne(story.State);
            story.State.nightTwoStarted = true;
            story.State.stage = NightStage.Room; story.State.ending = "";
            story.State.alarm = false; story.State.phone_powered_off = false;
            story.State.hasNganPhone = true;
            story.Clock.Set("06:30", 1);
            story.player.SetPhone(0); story.CloseModal();
            SyncWorld(); story.player.Teleport(Room, 0); story.player.RestoreView(0, false);
            if (story.State.night1_slept)
                story.State.messages.Add("Mẹ · 03:40: Cháu ở phòng mấy? Cô muốn gửi chút quà cảm ơn.");
            Checkpoint(); RestoreControl(); return true;
        }

        public void SyncWorld()
        {
            bool active = story.InNightTwo;
            if (active) EnsureWorld();
            if (active) RefreshWorld();
            if (world != null) world.SetActive(active);
            if (!active && wasActive) story.player.eyes.backgroundColor = originalSky;
            if (active || wasActive)
                foreach (var observer in story.observers) observer.enabled = !active;
            wasActive = active;
            if (!active) { StopAllCoroutines(); Transitioning = false; page = ""; }
        }

        public void RestoreControl()
        {
            StopAllCoroutines(); Transitioning = false; page = "";
            if (State.stage == MorningStage.Alarm) page = "alarm";
            story.player.SetOverlay(State.riding || page != "");
            if (State.riding) SetRidePose();
        }

        void Update()
        {
            if (!story.InNightTwo || story.player.IsPaused || Transitioning) return;
            RefreshWorld();
            var keys = Keyboard.current;
            if (State.riding)
            {
                if (keys != null && keys.escapeKey.wasPressedThisFrame && !story.player.PauseChangedThisFrame)
                { story.player.SetPaused(true); return; }
                FixedRouteRide.Step(State, keys != null && keys.wKey.isPressed ? 1 : 0,
                    keys != null && (keys.sKey.isPressed || keys.spaceKey.isPressed), Time.deltaTime);
                SetRidePose();
                if (keys != null && keys.eKey.wasPressedThisFrame) Dismount();
                return;
            }
            if (keys != null && !story.player.PauseChangedThisFrame)
            {
                if (keys.escapeKey.wasPressedThisFrame && page != "")
                { if (page == "alarm") StopAlarm(); else ClosePage(); }
                if (keys.nKey.wasPressedThisFrame) OpenPage("notebook");
            }
            if (State.stage == MorningStage.Gate)
            {
                State.gateElapsed = (State.gateElapsed + Time.deltaTime) % 11;
                if (!State.lienSawMorning && LienCanSee(story.player.eyes.transform.position))
                {
                    State.lienSawMorning = true;
                    story.Say("Cô Liên: Căn cước nghen con, phường người ta xuống kiểm đó. Thôi lên đi.", 8);
                }
            }
            if (State.stage == MorningStage.Rest)
            {
                State.restSeconds += Time.deltaTime;
                if (State.restSeconds >= 60) Rest();
            }
            fatiguePulse += Time.deltaTime;
        }

        void SetRidePose()
        {
            float z = State.returning ? 92 - State.routeMetres : 12 + State.routeMetres;
            story.player.Teleport(new Vector3(112, .35f, z), State.returning ? 180 : 0);
            story.player.RestoreView(0, false);
        }
        bool Nearby(Vector3 point, float distance = 3) => Vector3.Distance(story.player.transform.position, point) <= distance;
        public bool StopAlarm()
        {
            if (State.stage != MorningStage.Alarm) return false;
            State.stage = MorningStage.Breakfast; ClosePage();
            story.Say("Bống ơi, dậy đi con, muộn rồi."); Checkpoint(); return true;
        }
        public bool EatBreakfast()
        {
            if (State.stage != MorningStage.Breakfast || !Nearby(new Vector3(96, .08f, 7))) return false;
            State.stage = MorningStage.ToStore; story.Clock.Set("08:10", 1);
            story.Say("Cô bán bánh mì: Mới dọn tới hả cháu?"); Checkpoint(); return true;
        }
        public bool Mount()
        {
            if (State.riding || page != "" || story.player.ActivePhone != 0) return false;
            bool returning = State.stage == MorningStage.ReturnHome;
            if (State.stage != MorningStage.ToStore && !returning) return false;
            if (!Nearby(returning ? Store : Parking, 5)) return false;
            State.riding = true; State.returning = returning;
            State.routeMetres = State.speed = State.stoppedSeconds = 0;
            State.redLightCleared = returning;
            LogEngine(true); story.player.SetOverlay(true); SetRidePose(); Checkpoint(); return true;
        }
        public bool Dismount()
        {
            if (!State.riding || !FixedRouteRide.AtStop(State)) return false;
            State.riding = false; State.odometer += 1.5f;
            State.stage = State.returning ? MorningStage.Gate : MorningStage.Store;
            story.Clock.Set(State.returning ? "09:20" : "08:30", 1);
            LogEngine(false); story.player.SetOverlay(false);
            story.player.Teleport((State.returning ? Parking : Store) + Vector3.left * 1.2f, 90);
            Checkpoint(); return true;
        }
        void LogEngine(bool on)
        {
            State.odoLog.Add(new OdometerEntry { time = story.State.clock, km = State.odometer, engineOn = on });
            story.State.notebook.Add(story.State.clock + " — Đồng hồ xe: " + State.odometer.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " km");
        }
        public bool BuyFurniture()
        {
            if (State.stage != MorningStage.Store || !Nearby(Store, 4)) return false;
            State.stage = MorningStage.ReturnHome; story.Clock.Set("09:00", 1);
            story.Say("Bạn trả tiền mặt, buộc thùng kệ và đèn kẹp bàn lên xe."); Checkpoint(); return true;
        }
        public bool LienCanSee(Vector3 eye)
        {
            if (State.stage != MorningStage.Gate || State.gateElapsed < 8) return false;
            Vector3 origin = Lien + Vector3.up * 1.5f;
            Vector3 delta = eye - origin;
            if (delta.magnitude > 8 || Vector3.Angle(Vector3.back, delta) > 45) return false;
            return !Physics.Raycast(origin, delta.normalized, delta.magnitude, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore);
        }
        public bool PassGate()
        {
            if (State.stage != MorningStage.Gate || !Nearby(new Vector3(100, .08f, 7))) return false;
            State.lienSawMorning |= LienCanSee(story.player.eyes.transform.position);
            State.stage = MorningStage.Assembly;
            story.Say(State.lienSawMorning ? "Cô Liên: Căn cước nghen con, phường người ta xuống kiểm đó. Thôi lên đi." : "Bạn lách qua lúc cô Liên đang cúi lặt rau.", 8);
            Checkpoint(); return true;
        }
        public bool Assemble(int step)
        {
            if (State.stage != MorningStage.Assembly || step != State.assemblySteps || !Nearby(Room, 4)) return false;
            State.assemblySteps++;
            story.Say("Ráp tầng kệ " + State.assemblySteps + "/4.");
            if (State.assemblySteps == 4) State.stage = MorningStage.Lamp;
            Checkpoint(); return true;
        }
        public bool TestLamp()
        {
            if (State.stage != MorningStage.Lamp || !Nearby(Room, 4)) return false;
            State.lampTested = true; State.stage = MorningStage.Rest; story.Clock.Set("11:30", 1);
            story.Say("Buồn ngủ quá. Nằm nghỉ một chút."); Checkpoint(); return true;
        }
        public bool ReplyRoom(bool give)
        {
            if (!story.State.night1_slept || State.stage >= MorningStage.Evening || State.roomReply != "unanswered") return false;
            State.roomReply = give ? "given" : "refused";
            if (!give) State.momSuspicion = 1;
            story.State.messages.Add("Bạn · " + story.State.clock + " · Đã gửi: " + (give ? "Cháu ở phòng 7." : "Cháu xin phép không gửi số phòng."));
            // No reply from Mẹ while the student is awake.
            Checkpoint();
            return true;
        }
        public bool Rest()
        {
            if (State.stage != MorningStage.Rest || Transitioning) return false;
            ClosePage(); story.player.SetPhone(0); Transitioning = true; story.player.SetOverlay(true);
            StartCoroutine(RestSequence()); return true;
        }
        IEnumerator RestSequence()
        {
            yield return new WaitForSeconds(2);
            if (story.State.night1_slept && State.roomReply != "given") State.momSuspicion = 1;
            State.stage = MorningStage.Evening; State.odometer += 11;
            story.Clock.Set("21:00", 1);
            story.State.messages.Add("Mẹ · 12:20: " + (State.momSuspicion == 1 ? "Cháu còn giữ máy chứ?" : "Con bé sợ sấm lắm, sấm một cái là chui vào chăn."));
            story.State.messages.Add("Mẹ · 12:20: " + (State.roomReply == "given" ? "Tối cô gửi cháu ly trà sữa cảm ơn nhé." : "Ừ, không sao cháu."));
            if (!story.State.night1_slept) story.State.messages.Add("Mẹ · 12:20: Nó hay dậy học bài. Cháu ở phòng mấy?");
            story.State.messages.Add("Mẹ · 20:10: " + (State.momSuspicion == 1 ? "Cháu còn giữ máy chứ?" : "Cháu nhớ ăn tối nhé."));
            story.player.Teleport(Room); story.player.SetOverlay(false); Transitioning = false; Checkpoint();
        }
        void Checkpoint()
        {
            story.Checkpoints.Save("n2." + State.stage.ToString().ToLowerInvariant(), story.State, story.player.transform.position, story.player.transform.eulerAngles.y);
        }
        void OpenPage(string value)
        {
            if (State.stage == MorningStage.Alarm) return;
            page = value; story.player.SetPhone(0); story.player.SetOverlay(true);
        }
        void ClosePage()
        {
            if (State.stage == MorningStage.Alarm) return;
            page = ""; story.player.SetOverlay(false);
        }
    }
}
