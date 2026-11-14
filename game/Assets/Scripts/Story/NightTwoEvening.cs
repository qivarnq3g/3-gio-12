using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Story
{
    public static class NightTwoEvening
    {
        public const int StrangerCount = 4;
        public const float VigilLength = 30, DozeAfter = 10, SecondsPerMinute = 4;
        public static readonly string[] QuestionIds = { "room:given", "room:refused", "Q4", "Q5", "Q6", "Q7" };
        public static readonly string[] StrangerTimes = { "21:15", "21:16", "21:40", "23:30" };
        public static readonly string[] StrangerTexts =
        {
            "Bong oi hom nay me di lam ca chieu",
            "me hoi het ban lop con roi, khong ai biet con di da lat",
            "con o dau cung duoc, con chi can nhan me mot chu",
            "me de den phong con, con ve thi khoi mo cua toi"
        };
        public static string QuestionText(string id)
        {
            switch (id)
            {
                case "Q4": return "Dạ cháu cảm ơn cô ly trà sữa ạ.";
                case "Q5": return "Cô ơi, trong ảnh ăn chè với Ngân là ai vậy ạ?";
                case "Q6": return "Máy bạn ấy có cái app Máy tính nặng tới 3,9 GB, cô có biết không ạ?";
                case "Q7": return "Số lạ nói bạn cùng lớp không ai biết Ngân đi Đà Lạt cô ạ.";
                case "room:given": return "Cháu ở phòng 7.";
                default: return "Cháu xin phép không gửi số phòng.";
            }
        }
    }

    public sealed partial class NightTwoMorning
    {
        public static readonly Vector3 EveningBike = new Vector3(105, .08f, 13);
        public static readonly Vector3 RiceShop = new Vector3(104, .08f, 93);
        public static readonly Vector3 LienDesk = new Vector3(102, .08f, 7.5f);
        float eveningTimer, stillSeconds;
        string calculatorEntry = "";
        public string NganTab { get; private set; } = "messages";

        bool AtHome => State.stage == MorningStage.Evening || State.stage == MorningStage.Home;
        bool EveningClockRuns => !State.riding && (AtHome || State.stage == MorningStage.RiceShop);
        int Minutes => story.State.clockDay * 1440 + (int)TimeSpan.ParseExact(story.State.clock, @"hh\:mm", CultureInfo.InvariantCulture).TotalMinutes;
        static int At(int day, string time) => day * 1440 + (int)TimeSpan.ParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture).TotalMinutes;

        void DeliverDaytimeBatches()
        {
            var m = story.State.messages;
            bool slept = story.State.night1_slept;
            m.Add("Mẹ · 12:20: " + (State.roomReply != "unanswered"
                ? "Cô đọc rồi cháu ạ. Cháu ngủ được là tốt, con trai ở trọ một mình hay thức khuya lắm."
                : "Cháu ơi, cháu còn giữ máy giúp cô chứ? Cô cứ lo lo."));
            m.Add("Mẹ · 12:20: Hôm qua dưới này mưa to, cô lại nhớ con bé. Từ bé nó sợ sấm lắm, sấm một cái là chui tọt vào chăn, lớn rồi vẫn thế. Không biết trên Đà Lạt có mưa không.");
            if (slept)
                m.Add("Mẹ · 12:20: " + (State.roomReply == "given"
                    ? "Tối cô gửi cháu ly trà sữa nhé. Con bé hay uống loại ít đường, trân châu trắng, chắc cháu cũng uống được."
                    : "Ừ, không sao cháu."));
            else
                m.Add("Mẹ · 12:20: Cô muốn gửi cháu chút quà cảm ơn mà không biết gửi đâu. Cháu ở phòng mấy hả cháu?");
            m.Add("Mẹ · 20:10: Cô nói thật với cháu, trước khi con bé đi hai mẹ con có cãi nhau. Vì tiền học tiếng Nhật thôi. Cô nói to quá, nó đóng cửa phòng cả tối. Giờ cô nghĩ lại mà thương.");
            if (State.momSuspicion == 0) m.Add("Mẹ · 20:10: Cháu nhớ ăn tối nhé.");
        }

        void SetEveningClock(string time)
        {
            story.Clock.Set(time, State.stage == MorningStage.Vigil || time.StartsWith("0") ? 2 : 1);
            Scheduled();
        }

        void AddMinutes(int amount)
        {
            int total = Minutes + amount;
            story.Clock.Set(((total % 1440) / 60).ToString("00") + ":" + (total % 60).ToString("00"), total / 1440);
            Scheduled();
        }

        void Scheduled()
        {
            int now = Minutes;
            if (story.State.night1_slept && State.roomReply == "given" && !State.teaArrived && now >= At(1, "21:10"))
            {
                State.teaArrived = true;
                if (AtHome && !State.riding) story.Say("Cô Liên: Phòng 7 ơi! Có ly trà sữa ai gửi nè, xuống lấy đi con!", 6);
                else State.teaCallPending = true;
            }
            if (story.State.number_choice == "blocked") return;
            while (State.strangerDelivered < NightTwoEvening.StrangerCount && now >= At(1, NightTwoEvening.StrangerTimes[State.strangerDelivered]))
            {
                int i = State.strangerDelivered++;
                story.State.messages.Add("0918 xxx 418 · " + NightTwoEvening.StrangerTimes[i] + ": " + NightTwoEvening.StrangerTexts[i] + " · Đã xem");
                if (i == 1 && !Transitioning) story.Say("Người yêu cũ thì nói gì mà không được.", 5);
            }
        }

        void EveningUpdate(Keyboard keys)
        {
            if (State.stage < MorningStage.Evening) return;
            if (EveningClockRuns && Minutes < At(1, "22:59"))
            {
                eveningTimer += Time.deltaTime;
                while (eveningTimer >= NightTwoEvening.SecondsPerMinute) { eveningTimer -= NightTwoEvening.SecondsPerMinute; AddMinutes(1); }
            }
            if (State.stage == MorningStage.Vigil)
            {
                bool moving = keys != null && (keys.wKey.isPressed || keys.aKey.isPressed || keys.sKey.isPressed || keys.dKey.isPressed);
                VigilTick(Time.deltaTime, moving);
            }
        }

        void ArriveHomeEvening()
        {
            if (State.teaCallPending)
            {
                State.teaCallPending = false; State.lienEveningSaid = true;
                story.Say("Cô Liên: Phòng 7 ơi! Có ly trà sữa ai gửi nè, cô để trên bàn đó.", 6);
            }
            else if (State.lienSawMorning && !State.lienEveningSaid)
            {
                State.lienEveningSaid = true;
                story.Say("Cô Liên: Căn cước nghen con.\nBạn: Dạ.", 5);
            }
        }

        public bool BuyRice()
        {
            if (State.stage != MorningStage.RiceShop || State.hasRice || !Nearby(RiceShop, 4)) return false;
            State.hasRice = true;
            story.Say("Cô bán cơm: Còn sườn với cá kho thôi con, lấy gì?\nBạn: Dạ sườn, thêm chút canh cô.\nCô bán cơm: Canh hết rồi, cô chan nước kho thêm cho nghen. ... Ăn trễ vầy hoài là bao tử nó hư đó con.", 9);
            Checkpoint(); return true;
        }

        public bool AskLien()
        {
            if (!AtHome || !Nearby(LienDesk, 4)) return false;
            story.Say(State.teaArrived ? "Cô Liên: Shipper nói đơn ghi gửi chủ trọ, khỏi gọi. Ai mà chu đáo dữ. Bồ hả?" : "Cô Liên: Có ai đâu, có thì cô gọi.", 6);
            return true;
        }

        public bool TakeTea()
        {
            if (!State.teaArrived || State.teaTaken || !AtHome || !Nearby(LienDesk, 4)) return false;
            story.Say("Cô Liên: Shipper nói đơn ghi gửi chủ trọ, khỏi gọi. Ai mà chu đáo dữ. Bồ hả?\nBạn: Dạ không, người quen cô.\nCô Liên: Người quen thì biểu người ta ghi số phòng giùm cô. Cô đâu có ngồi giữ đồ cho cả dãy được.", 9);
            OpenPage("tea"); return true;
        }

        public bool DecideTea(bool drink)
        {
            if (!State.teaArrived || State.teaTaken) return false;
            State.teaTaken = true; State.drankMilkTea = drink;
            ClosePage(); story.Say(drink ? "Ngọt vừa. Cũng ngon." : "Thôi, để đó.", 4);
            Checkpoint(); return true;
        }

        public void OpenNganTab(string tab)
        {
            if (State.stage < MorningStage.Evening) return;
            NganTab = tab; scroll = tab == "messages" ? new Vector2(0, 100000) : Vector2.zero;
            switch (tab)
            {
                case "messages":
                    if (!State.readEveningBatch) { State.readEveningBatch = true; if (State.momSuspicion == 0) story.Say("Ăn sáng, ăn tối. Mẹ ai cũng dặn vậy hả ta.", 5); }
                    break;
                case "photos": if (!State.sawPhotos) { State.sawPhotos = true; story.Say("Từ tháng mười trống trơn.", 4); } break;
                case "storage": if (!State.sawStorage) { State.sawStorage = true; story.Say("Máy tính gì mà nặng hơn game.", 4); } break;
                case "calculator": if (!State.openedCalculator) { State.openedCalculator = true; story.Say("Bình thường mà.", 4); } calculatorEntry = ""; break;
                case "linh": if (!State.sawLinhChat) { State.sawLinhChat = true; story.Say("Bạn thân cũng không biết.", 4); } break;
                case "lock": if (!State.setNganPasscode) story.Say("Để vầy ai cầm cũng đọc được hết.", 4); break;
            }
            if (!State.riding && !Transitioning) Checkpoint();
        }

        public void PlayVlog()
        {
            if (State.stage < MorningStage.Evening) return;
            State.sawVlog = true; story.Say("Con bé dễ thương ghê.", 4); Checkpoint();
        }

        public void PressCalculator(string key)
        {
            if (key == "C") { calculatorEntry = ""; return; }
            if (key != "=") { if (calculatorEntry.Length < 12) calculatorEntry += key; return; }
            if (calculatorEntry == "") return;
            State.vaultWrongAttempts++;
            if (State.vaultWrongAttempts == 2) story.Say("Không có gì. Chắc mình nghĩ nhiều.", 4);
            Checkpoint();
        }
        public string CalculatorDisplay => calculatorEntry == "" ? "0" : calculatorEntry;

        public bool SetNganPasscode()
        {
            if (State.stage < MorningStage.Evening || State.setNganPasscode) return false;
            State.setNganPasscode = true; State.momSuspicion = 1;
            story.Say("Rồi. Cô lên thì đưa mã cho cô.", 4); Checkpoint(); return true;
        }

        public bool CanSend(string id)
        {
            if (State.stage < MorningStage.Evening || State.stage >= MorningStage.Vigil || State.eveningSent.Contains(id)) return false;
            switch (id)
            {
                case "room:given":
                case "room:refused": return !story.State.night1_slept && State.roomReply == "unanswered";
                case "Q4": return State.teaTaken;
                case "Q5": return State.sawPhotos;
                case "Q6": return State.sawStorage;
                case "Q7": return State.strangerDelivered >= 2;
                default: return false;
            }
        }

        public bool SendEvening(string id)
        {
            if (!CanSend(id)) return false;
            State.eveningSent.Add(id);
            if (id == "room:given") State.roomReply = "given";
            if (id == "room:refused") { State.roomReply = "refused"; State.momSuspicion = 1; }
            if (id == "Q7") State.momSuspicion = 1;
            story.State.messages.Add("Bạn · " + story.State.clock + " · Đã gửi: " + NightTwoEvening.QuestionText(id));
            Checkpoint(); return true;
        }

        public bool OpenBed()
        {
            if (!AtHome || !Nearby(Room, 4)) return false;
            if (!State.hasRice) { story.Say("Đói xỉu.", 3); return false; }
            OpenPage("bed"); return true;
        }

        public bool ChooseNight(bool sleep)
        {
            if (State.stage != MorningStage.Home || !State.hasRice || Transitioning) return false;
            ClosePage(); story.player.SetPhone(0); Transitioning = true; story.player.SetOverlay(true);
            Checkpoint();
            StartCoroutine(NightSequence(sleep)); return true;
        }

        IEnumerator NightSequence(bool sleep)
        {
            State.night2Slept = sleep;
            SetEveningClock("23:00");
            story.Say(sleep ? "Ngủ cả chiều rồi mà vẫn buồn ngủ." : "Coi ba giờ mười hai nó có reo nữa không.", 4);
            yield return new WaitForSeconds(2);
            if (sleep)
            {
                SetEveningClock("23:30");
                FinishNightTwo(false); yield break;
            }
            foreach (string t in new[] { "23:30", "00:30", "01:30", "02:30" })
            {
                SetEveningClock(t);
                story.Say(t == "23:30" ? "Tiếng xe máy ngang hẻm." : t == "00:30" ? "Đèn hành lang bật rồi tắt." : "Quạt kêu trong phòng.", 2);
                yield return new WaitForSeconds(2);
            }
            State.stage = MorningStage.Vigil; State.vigilSeconds = 0; stillSeconds = 0;
            if (story.State.night1_slept) RingAlarm();
            else { story.player.Teleport(Room); story.player.SetOverlay(false); }
            Transitioning = false; Checkpoint();
        }

        public void VigilTick(float dt, bool moving)
        {
            if (State.stage != MorningStage.Vigil || State.vigilSeconds >= NightTwoEvening.VigilLength || Transitioning) return;
            State.vigilSeconds = Mathf.Min(NightTwoEvening.VigilLength, State.vigilSeconds + dt);
            stillSeconds = moving ? 0 : stillSeconds + dt;
            if (stillSeconds >= NightTwoEvening.DozeAfter) { FinishNightTwo(true); return; }
            if (State.vigilSeconds >= NightTwoEvening.VigilLength) { RingAlarm(); Checkpoint(); }
        }

        void RingAlarm()
        {
            State.vigilSeconds = NightTwoEvening.VigilLength;
            SetEveningClock("03:12");
            page = "alarm2"; story.player.SetPhone(0); story.player.SetOverlay(true);
        }

        public bool StopNightTwoAlarm()
        {
            if (page != "alarm2") return false;
            FinishNightTwo(false); return true;
        }

        void FinishNightTwo(bool dozed)
        {
            State.vigilDozed = dozed; State.stage = MorningStage.Ended;
            story.Clock.Set("06:29", 2);
            page = ""; story.player.SetPhone(0); story.player.SetOverlay(true);
            Transitioning = false; Checkpoint();
        }
    }
}
