using System.Collections;
using BaGio12.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Story
{
    // Follow-up nights reuse the main realistic scene; progression lives in the save DTO.
    public sealed partial class NightTwoMorning : MonoBehaviour
    {
        public NightOneDirector story;
        public NightTwoMorningState State => story.InNightTwo ? story.State.nightTwo : null;
        public bool Transitioning { get; private set; }
        public static Vector3 Room => ScenePoint("CHECKPOINT_N1_Room7", new Vector3(-3.8f,10.28f,-44));
        public static Vector3 Parking => ScenePoint("CHECKPOINT_N1_Garage", new Vector3(0,.08f,-50));
        public static Vector3 Store => ScenePoint("PARK_N2_HomewareShop", new Vector3(-154,.08f,14));
        public static Vector3 Lien => ScenePoint("OBSERVER_Lien_Gate", new Vector3(-1.8f,0,-42));
        public static Vector3 Breakfast => ScenePoint("NPC_N2_BanhMiSeller", new Vector3(-2.3f,0,1.4f));
        public static Vector3 Gate => new Vector3(0,.08f,-41.6f);
        string page = "";
        bool wasActive;
        float fatiguePulse;

        public bool Begin()
        {
            if (story.InNightTwo || story.Busy || story.State.ending != "night1_completed") return false;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "NightOneRealistic")
            { story.Say("Đêm 2 phải chạy trong cảnh NightOneRealistic."); return false; }
            assetsCheckedAt = -100;
            if (!ValidateSceneAssets()) { story.Say(MissingSceneAssets, 10); return false; }
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

        void Start() => SyncWorld();

        public void SyncWorld()
        {
            bool active = story.InNightTwo;
            if (active) EnsureWorld();
            if (active) RefreshWorld();
            if (story.InNightThree) { EnsureThreeWorld(); RefreshThreeWorld(); }
            if (threeWorld != null) threeWorld.SetActive(story.InNightThree);
            if (!story.InNightThree && threeDoor!=null)threeDoor.SetFollowupLocked(false);
            if (world != null) world.SetActive(active);
            if (!active) ResetScenePresentation();
            if (!active && wasActive) story.player.eyes.backgroundColor = originalSky;
            if (active || wasActive)
                foreach (var observer in story.observers) observer.enabled = !active;
            wasActive = active;
            if (!active) { StopAllCoroutines(); Transitioning = false; page = ""; }
        }

        public void RestoreControl()
        {
            StopAllCoroutines(); Transitioning = false; page = "";
            if (story.InNightThree) { RestoreThreeControl(); return; }
            if (State.stage == MorningStage.Alarm) page = "alarm";
            if (State.stage == MorningStage.Vigil && State.vigilSeconds >= NightTwoEvening.VigilLength) page = "alarm2";
            story.player.SetOverlay(State.riding || page != "" || State.stage == MorningStage.Ended);
            if (State.riding) SetRidePose();
        }

        void Update()
        {
            if (story.InNightThree) { UpdateThree(); return; }
            if (!story.InNightTwo || story.player.IsPaused || Transitioning || State.stage == MorningStage.Ended) return;
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
                { if (page == "alarm") StopAlarm(); else if (page == "alarm2") StopNightTwoAlarm(); else ClosePage(); }
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
            EveningUpdate(keys);
            fatiguePulse += Time.deltaTime;
        }

        void SetRidePose()
        {
            // Route distance remains save-compatible; interpolate along the actual alley and street.
            float progress = Mathf.Clamp01(State.routeMetres / FixedRouteRide.Length);
            if (State.returning) progress = 1 - progress;
            bool evening = State.stage == MorningStage.ToRice || State.stage == MorningStage.RiceReturn;
            Vector3 start = evening ? EveningBike : Parking, corner = new Vector3(0,.08f,8), finish = evening ? RiceShop : Store;
            float alley = Vector3.Distance(start,corner), road = Vector3.Distance(corner,finish);
            float travelled = progress * (alley + road);
            Vector3 position = travelled < alley ? Vector3.Lerp(start,corner,travelled/alley)
                : Vector3.Lerp(corner,finish,(travelled-alley)/road);
            Vector3 forward = travelled < alley ? corner-start : finish-corner;
            if (State.returning) forward = -forward;
            story.player.Teleport(position, Quaternion.LookRotation(forward).eulerAngles.y);
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
            if (State.stage != MorningStage.Breakfast || !Nearby(Breakfast)) return false;
            State.stage = MorningStage.ToStore; story.Clock.Set("08:10", 1);
            story.Say("Cô bán bánh mì: Mới dọn tới hả cháu?"); Checkpoint(); return true;
        }
        public bool Mount()
        {
            if (State.riding || page != "" || story.player.ActivePhone != 0) return false;
            bool returning; Vector3 at; MorningStage next;
            switch (State.stage)
            {
                case MorningStage.ToStore: returning = false; at = Parking; next = MorningStage.ToStore; break;
                case MorningStage.ReturnHome: returning = true; at = Store; next = MorningStage.ReturnHome; break;
                case MorningStage.Evening: returning = false; at = EveningBike; next = MorningStage.ToRice; break;
                case MorningStage.RiceShop: if (!State.hasRice) return false; returning = true; at = RiceShop; next = MorningStage.RiceReturn; break;
                default: return false;
            }
            if (!Nearby(at, 5)) return false;
            bool evening = next == MorningStage.ToRice || next == MorningStage.RiceReturn;
            State.stage = next;
            State.riding = true; State.returning = returning;
            State.routeMetres = State.speed = State.stoppedSeconds = 0;
            State.redLightCleared = returning || evening;
            if (next == MorningStage.ToRice)
            {
                if (!State.sawBikeMoved) { State.sawBikeMoved = true; story.Say("Ủa. ... Chắc cô Liên dọn nhà xe.", 6); }
                SetEveningClock("21:30");
            }
            else if (next == MorningStage.RiceReturn) AddMinutes(4);
            LogEngine(true); story.player.SetOverlay(true); SetRidePose(); Checkpoint(); return true;
        }
        public bool Dismount()
        {
            if (!State.riding || !FixedRouteRide.AtStop(State)) return false;
            bool evening = State.stage == MorningStage.ToRice || State.stage == MorningStage.RiceReturn;
            State.riding = false; State.odometer += evening ? .5f : 1.5f;
            if (State.stage == MorningStage.ToRice) { State.stage = MorningStage.RiceShop; AddMinutes(6); }
            else if (State.stage == MorningStage.RiceReturn) { State.stage = MorningStage.Home; AddMinutes(5); }
            else
            {
                State.stage = State.returning ? MorningStage.Gate : MorningStage.Store;
                story.Clock.Set(State.returning ? "09:20" : "08:30", 1);
            }
            LogEngine(false); story.player.SetOverlay(false);
            story.player.Teleport((State.returning ? evening ? EveningBike : Parking : evening ? RiceShop : Store) + Vector3.left * 1.2f, 90);
            if (evening && State.returning) ArriveHomeEvening();
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
            if (State.stage != MorningStage.Gate || !Nearby(Gate)) return false;
            State.lienSawMorning |= LienCanSee(story.player.eyes.transform.position);
            State.stage = MorningStage.Assembly;
            story.Say(State.lienSawMorning ? "Cô Liên: Căn cước nghen con, phường người ta xuống kiểm đó. Thôi lên đi." : "Bạn lách qua lúc cô Liên đang cúi lặt rau.", 8);
            Checkpoint(); return true;
        }
        public bool Assemble(int step)
        {
            if (State.stage != MorningStage.Assembly || step != State.assemblySteps || !Nearby(Room, 4)) return false;
            if (rack == null) { story.Say(MissingSceneAssets, 8); return false; }
            State.assemblySteps++;
            story.Say("Ráp tầng kệ " + State.assemblySteps + "/4.");
            if (State.assemblySteps == 4) State.stage = MorningStage.Lamp;
            Checkpoint(); return true;
        }
        public bool TestLamp()
        {
            if (State.stage != MorningStage.Lamp || !Nearby(Room, 4)) return false;
            if (clampLamp == null) { story.Say(MissingSceneAssets, 8); return false; }
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
            DeliverDaytimeBatches();
            story.player.Teleport(Room); story.player.SetOverlay(false); Transitioning = false;
            story.Say("Chín giờ rồi. Ngủ gì mà ngủ dữ vậy.", 5);
            Checkpoint();
        }
        void Checkpoint()
        {
            string id = story.InNightThree ? "n3." + Third.stage.ToString().ToLowerInvariant() : "n2." + State.stage.ToString().ToLowerInvariant();
            story.Checkpoints.Save(id, story.State, story.player.transform.position, story.player.transform.eulerAngles.y);
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
