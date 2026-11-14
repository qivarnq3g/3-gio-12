using System.Collections.Generic;
using BaGio12.Persistence;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        static readonly string[] NganTabs = { "messages", "photos", "storage", "calculator", "video", "linh", "lock" };
        static readonly string[] NganTabLabels = { "Tin nhắn", "Ảnh", "Bộ nhớ", "Máy tính", "Video", "Linh", "Khóa màn hình" };
        static readonly string[] CalculatorKeys = { "7", "8", "9", "4", "5", "6", "1", "2", "3", "C", "0", "=" };

        string EveningObjective
        {
            get
            {
                switch (State.stage)
                {
                    case MorningStage.Evening: return "21:00. Đói. Xe ở nhà xe, đi mua cơm. Máy Ngân (1) còn nhiều thứ để xem.";
                    case MorningStage.ToRice: return "Chạy ra quán cơm. W tăng tốc, S/Space phanh, E xuống xe ở điểm dừng.";
                    case MorningStage.RiceShop: return State.hasRice ? "Bật máy ở xe, về nhà trọ." : "Mua cơm ở quán.";
                    case MorningStage.RiceReturn: return "Chạy về nhà trọ.";
                    case MorningStage.Home: return "Ăn tối. Khi xong, ra nệm chọn đi ngủ hay thức canh.";
                    case MorningStage.Vigil: return "Thức canh. Đứng yên lâu là ngủ gật.";
                    default: return "Hết đêm 2.";
                }
            }
        }

        bool DrawEveningOverlay()
        {
            if (State.stage == MorningStage.Ended)
            {
                Background(new Rect(0, 0, 1280, 720), Color.black);
                GUI.Label(new Rect(280, 260, 750, 100), "HẾT ĐÊM 2", titleStyle);
                GUI.Label(new Rect(280, 330, 750, 70), State.vigilDozed ? "Bạn ngủ gật lúc nào không hay. Trời đã sáng." : "Sáng 26/11 thuộc đêm 3, đang được làm trong #22.", textStyle);
                if (GUI.Button(new Rect(280, 420, 300, 50), "Chơi lại đêm 1")) story.GetComponent<NightPersistence>().RequestNewGame();
                return true;
            }
            if (page != "tea" && page != "bed" && page != "alarm2") return false;
            Background(new Rect(0, 0, 1280, 720), new Color(.025f, .04f, .055f));
            if (page == "tea")
            {
                GUI.Label(new Rect(80, 80, 1080, 60), "Ly trà sữa trên bàn cô Liên", titleStyle);
                GUI.Label(new Rect(80, 160, 1080, 90), "Giấy dán trên ly: Ít đường. Trân châu trắng. \"Cảm ơn cháu. Mẹ Ngân\"", textStyle);
                GUI.Label(new Rect(80, 220, 1080, 40), "Gửi thật luôn.", textStyle);
                if (Choice(0, "Uống")) DecideTea(true);
                if (Choice(1, "Để đó")) DecideTea(false);
            }
            else if (page == "bed")
            {
                GUI.Label(new Rect(80, 80, 1080, 60), story.State.clock + ". Nệm", titleStyle);
                if (Choice(0, "Đi ngủ")) ChooseNight(true);
                if (Choice(1, "Thức canh tới sáng")) ChooseNight(false);
                if (Choice(2, "Chưa, để sau (Esc)")) ClosePage();
            }
            else
            {
                GUI.Label(new Rect(80, 80, 1080, 120), "03:12. Báo thức máy Ngân reo.", titleStyle);
                if (Choice(0, "Tắt báo thức")) StopNightTwoAlarm();
            }
            return true;
        }

        void DrawNganEvening()
        {
            if (NganTab == "messages" && !State.readEveningBatch) OpenNganTab("messages");
            GUI.Label(new Rect(65, 25, 1000, 50), "Máy Ngân · " + story.State.clock, titleStyle);
            for (int i = 0; i < NganTabs.Length; i++)
            {
                bool active = NganTab == NganTabs[i];
                if (GUI.Button(new Rect(65 + i * 160, 85, 150, 45), active ? "[" + NganTabLabels[i] + "]" : NganTabLabels[i])) OpenNganTab(NganTabs[i]);
            }
            switch (NganTab)
            {
                case "photos": DrawPhotos(); break;
                case "storage": DrawStorage(); break;
                case "calculator": DrawCalculator(); break;
                case "video": DrawVlog(); break;
                case "linh": DrawLinhChat(); break;
                case "lock": DrawLock(); break;
                default: DrawEveningMessages(); break;
            }
            GUI.Label(new Rect(65, 660, 1080, 40), "Giao diện tạm cho bản thô, chờ ghép điện thoại #17.", textStyle);
        }

        void DrawEveningMessages()
        {
            var lines = story.State.messages;
            scroll = GUI.BeginScrollView(new Rect(65, 145, 1120, 300), scroll, new Rect(0, 0, 1060, Mathf.Max(290, lines.Count * 62)));
            for (int i = 0; i < lines.Count; i++) GUI.Label(new Rect(0, i * 62, 1040, 62), lines[i], textStyle);
            GUI.EndScrollView();
            var options = new List<string>();
            foreach (string id in NightTwoEvening.QuestionIds) if (CanSend(id)) options.Add(id);
            for (int i = 0; i < options.Count; i++)
                if (GUI.Button(new Rect(65 + (i % 2) * 565, 460 + (i / 2) * 58, 550, 50), NightTwoEvening.QuestionText(options[i]))) SendEvening(options[i]);
        }

        void DrawPhotos()
        {
            GUI.Label(new Rect(65, 150, 1080, 40), "Thư viện ảnh. Không có ảnh nào từ 03/10.", textStyle);
            GUI.Label(new Rect(65, 200, 1080, 200),
                "03/12/2019 · Hai ly chè, bàn tay người kia cầm muỗng. \"1 năm\"\n" +
                "03/12/2021 · Hai ly chè, bàn tay người kia cầm muỗng. \"3 năm. Vẫn chè đậu đỏ.\"\n" +
                "03/12/2024 · Hai ly chè, bàn tay người kia cầm muỗng. \"5 năm. thằng này không bao giờ chịu chụp mặt\"", textStyle);
            if (GUI.Button(new Rect(65, 410, 1080, 50), "03/12/2025 · \"6 năm. Năm sau tao mà đậu học bổng là mày ăn một mình nhé hehe.\""))
                story.Say("Ai mà ghét chụp hình dữ vậy.", 4);
        }

        void DrawStorage()
        {
            GUI.Label(new Rect(65, 150, 1080, 40), "Cài đặt > Bộ nhớ", textStyle);
            GUI.Label(new Rect(65, 200, 1080, 220),
                "Máy tính · 3,9 GB · cài ngày 05/10/2026\nẢnh · 0,4 GB\nTin nhắn · 0,2 GB", textStyle);
        }

        void DrawCalculator()
        {
            GUI.Label(new Rect(65, 150, 520, 60), CalculatorDisplay, titleStyle);
            for (int i = 0; i < CalculatorKeys.Length; i++)
                if (GUI.Button(new Rect(65 + (i % 3) * 130, 220 + (i / 3) * 90, 120, 80), CalculatorKeys[i])) PressCalculator(CalculatorKeys[i]);
        }

        void DrawVlog()
        {
            GUI.Label(new Rect(65, 150, 1080, 40), "Vlog Tết 2026 · 1:40 · quay dọc · hành lang tầng 3 cư xá C4", textStyle);
            if (GUI.Button(new Rect(65, 200, 200, 50), State.sawVlog ? "Xem lại" : "Phát")) PlayVlog();
            if (!State.sawVlog) return;
            GUI.Label(new Rect(65, 265, 1100, 380),
                "Ngân: Rồi, chúc mừng năm mới cả nhà. Hôm nay em giới thiệu với mọi người một nhân vật rất quan trọng.\n" +
                "Bé Na: Chị Ngân ơi lì xì!\n" +
                "Ngân: Từ từ, chưa giới thiệu mà đòi rồi. Đây là Na, đệ tử ruột của chị. Na, chào mọi người đi.\n" +
                "Bé Na: Chào mọi người. Lì xì đi chị!\n" +
                "Ngân: Đệ tử ruột thì phải biết mật hiệu. Na gõ cho mọi người coi.\n" +
                "Bé Na gõ lên cánh cửa: Cốc. ... Cốc cốc.\n" +
                "Ngân: Một cái, rồi hai cái liền. Đêm nào nghe gõ vậy là chị biết Na lên. Người khác gõ chị không mở đâu nha.\n" +
                "Bé Na: Giờ lì xì được chưa?", textStyle);
        }

        void DrawLinhChat()
        {
            GUI.Label(new Rect(65, 150, 1080, 40), "Linh", titleStyle);
            GUI.Label(new Rect(65, 200, 1100, 400),
                "Ngân · 28/9: Tao đặt vé ngày 3/12 đấy. Ngày của tụi mình. Để tao nhớ là tao sẽ về.\n" +
                "Linh · 28/9: ừ.\n\n" +
                "Linh · 17/11, 21:04: Mày đi Đà Lạt thật à? Sao không rủ tao.  · Đã xem\n" +
                "Linh · 19/11, 20:47: Cô Hòa lo lắm đấy. Mày nhắn cô một câu đi.  · Đã xem\n" +
                "Linh · 22/11, 22:15: Về sớm. Tao đợi.  · Đã xem", textStyle);
        }

        void DrawLock()
        {
            GUI.Label(new Rect(65, 150, 1080, 40), "Cài đặt > Khóa màn hình", textStyle);
            GUI.Label(new Rect(65, 200, 1080, 40), State.setNganPasscode ? "Mã 6 số: đã đặt." : "Mã 6 số: chưa đặt.", textStyle);
            if (!State.setNganPasscode && GUI.Button(new Rect(65, 260, 300, 50), "Đặt mã máy")) SetNganPasscode();
        }
    }
}
