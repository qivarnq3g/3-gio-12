using System;
using System.Collections.Generic;
using BaGio12.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        static readonly string[] NganTabs = { "messages", "photos", "storage", "calculator", "video", "linh", "lock" };
        static readonly string[] NganTabLabels = { "Tin nhắn", "Ảnh", "Bộ nhớ", "Máy tính", "Video", "Linh", "Khóa màn hình" };
        static readonly string[] CalculatorKeys = { "7", "8", "9", "4", "5", "6", "1", "2", "3", "C", "0", "=" };
        int tabKeyFrame = -1;

        string EveningObjective
        {
            get
            {
                switch (State.stage)
                {
                    case MorningStage.Evening: return "21:00. Đói. Xe ở nhà xe, đi mua cơm. Máy Ngân (1) còn nhiều thứ để xem.";
                    case MorningStage.ToRice: return "Chạy ra quán cơm. Tới điểm dừng thì xuống xe.";
                    case MorningStage.RiceShop: return State.hasRice ? "Bật máy ở xe, về nhà trọ." : riceShop == null ? MissingEveningAssets : "Mua cơm ở quán.";
                    case MorningStage.RiceReturn: return "Chạy về nhà trọ.";
                    case MorningStage.Home: return "Ăn tối. Khi xong, ra nệm chọn đi ngủ hay thức canh.";
                    case MorningStage.Vigil: return "Thức canh. Đứng yên lâu là ngủ gật.";
                    default: return "Hết đêm 2.";
                }
            }
        }

        void DrawEveningPage(float left)
        {
            HudKit.Heading(left, 70, story.State.clock, "");
            var items = new List<(string label, Action run, bool enabled)>();
            float y0 = 250;
            if (page == "tea")
            {
                HudKit.Label(new Rect(left, 104, 1400, 70), "Ly trà sữa trên bàn cô Liên", 40, HudKit.Paper, true);
                HudKit.Label(new Rect(left, 190, 1400, 40), "Giấy dán trên ly: Ít đường. Trân châu trắng. \"Cảm ơn cháu. Mẹ Ngân\"", 26, HudKit.Fade(HudKit.Paper, .8f));
                HudKit.Label(new Rect(left, 236, 1400, 40), "Gửi thật luôn.", 26, HudKit.Fade(HudKit.Paper, .6f));
                items.Add(("Uống", () => DecideTea(true), true));
                items.Add(("Để đó", () => DecideTea(false), true));
                y0 = 310;
            }
            else
            {
                CloseAction("Để sau");
                HudKit.Label(new Rect(left, 104, 1400, 70), "Bạn sẽ làm gì?", 40, HudKit.Paper, true);
                items.Add(("Đi ngủ", () => ChooseNight(true), true));
                items.Add(("Thức canh tới sáng", () => ChooseNight(false), true));
            }
            Pick(screenChoices, left, y0, items);
            HudKit.Footer("W S", "Chọn", "Enter", "Xác nhận");
        }

        bool TabClicked(Rect virtualRect)
        {
            var e = Event.current;
            if (e == null || e.type != EventType.MouseDown || e.button != 0 || Cursor.lockState == CursorLockMode.Locked) return false;
            if (!HudKit.S(virtualRect).Contains(e.mousePosition)) return false;
            e.Use();
            return true;
        }

        void StepTabs()
        {
            if (Event.current.type != EventType.Repaint || tabKeyFrame == Time.frameCount) return;
            tabKeyFrame = Time.frameCount;
            var keys = Keyboard.current;
            if (keys == null) return;
            int step = keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame ? -1 : keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame ? 1 : 0;
            if (step == 0) return;
            int at = Array.IndexOf(NganTabs, NganTab);
            OpenNganTab(NganTabs[(Mathf.Max(0, at) + step + NganTabs.Length) % NganTabs.Length]);
            HudKit.Tick();
        }

        void DrawNganEvening()
        {
            if (NganTab == "messages" && !State.readEveningBatch) OpenNganTab("messages");
            PhoneLayout(out float left, out float listWidth, out float choiceX, out float choiceWidth);
            HudKit.Heading(left, 70, story.State.clock, "Máy Ngân", listWidth);
            CloseAction("Cất máy");
            StepTabs();
            float x = left;
            for (int i = 0; i < NganTabs.Length; i++)
            {
                bool active = NganTab == NganTabs[i];
                float w = HudKit.TextWidth(NganTabLabels[i], 22, active) + 8;
                HudKit.Label(new Rect(x, 196, w + 20, 32), NganTabLabels[i], 22, active ? HudKit.Warm : HudKit.Fade(HudKit.Paper, .5f), active, TextAnchor.MiddleLeft, false);
                if (active) HudKit.Fill(new Rect(x, 230, w, 2), HudKit.Fade(HudKit.Warm, .8f));
                if (TabClicked(new Rect(x - 8, 190, w + 16, 44))) { OpenNganTab(NganTabs[i]); HudKit.Tick(); }
                x += w + 36;
            }
            var body = new Rect(left, 260, listWidth, HudKit.Height - 260 - 180);
            switch (NganTab)
            {
                case "photos": DrawPhotos(body, choiceX, choiceWidth); break;
                case "storage": DrawStorage(body); break;
                case "calculator": DrawCalculator(body); break;
                case "video": DrawVlog(body, choiceX, choiceWidth); break;
                case "linh": DrawLinhChat(body); break;
                case "lock": DrawLock(body, choiceX, choiceWidth); break;
                default: DrawEveningMessages(body, choiceX, choiceWidth); break;
            }
        }

        void TabFooter(bool choices, string confirm = "Xác nhận")
        {
            if (choices) HudKit.Footer("A D", "Đổi mục", "W S", "Chọn", "Enter", confirm, "Esc", "Cất máy");
            else HudKit.Footer("A D", "Đổi mục", "Esc", "Cất máy");
        }

        void TextBlock(Rect area, string text)
        {
            float total = HudKit.TextHeight(text, 25, area.width - 20);
            scroll = HudKit.BeginScroll(area, scroll, area.width - 20, total);
            HudKit.Label(new Rect(0, 0, area.width - 20, total + 4), text, 25, HudKit.Fade(HudKit.Paper, .9f));
            HudKit.EndScroll();
        }

        void DrawEveningMessages(Rect area, float choiceX, float choiceWidth)
        {
            DrawMessageList(area);
            var items = new List<(string label, Action run, bool enabled)>();
            foreach (string id in NightTwoEvening.QuestionIds)
                if (CanSend(id)) { string send = id; items.Add((NightTwoEvening.QuestionText(id), () => SendEvening(send), true)); }
            if (items.Count == 0) { TabFooter(false); return; }
            HudKit.Label(new Rect(choiceX, 246, choiceWidth, 30), "Trả lời", 20, HudKit.Fade(HudKit.Paper, .45f));
            float row = Mathf.Min(62, (HudKit.Height - 290 - 200) / Mathf.Max(1, items.Count));
            Pick(screenChoices, choiceX, 284, items, Mathf.Min(26, row * .44f), row, choiceWidth);
            TabFooter(true, "Gửi");
        }

        void DrawPhotos(Rect area, float choiceX, float choiceWidth)
        {
            HudKit.Label(new Rect(area.x, area.y, area.width, 30), "Thư viện ảnh. Không có ảnh nào từ 03/10.", 20, HudKit.Fade(HudKit.Warm, .7f));
            TextBlock(new Rect(area.x, area.y + 44, area.width, area.height - 44),
                "03/12/2019\nHai ly chè, bàn tay người kia cầm muỗng. \"1 năm\"\n\n" +
                "03/12/2021\nHai ly chè, bàn tay người kia cầm muỗng. \"3 năm. Vẫn chè đậu đỏ.\"\n\n" +
                "03/12/2024\nHai ly chè, bàn tay người kia cầm muỗng. \"5 năm. thằng này không bao giờ chịu chụp mặt\"\n\n" +
                "03/12/2025\n\"6 năm. Năm sau tao mà đậu học bổng là mày ăn một mình nhé hehe.\"");
            Pick(screenChoices, choiceX, 284, new List<(string, Action, bool)> { ("Xem kỹ ảnh 03/12/2025", () => story.Say("Ai mà ghét chụp hình dữ vậy.", 4), true) }, 26, 62, choiceWidth);
            TabFooter(true);
        }

        void DrawStorage(Rect area)
        {
            HudKit.Label(new Rect(area.x, area.y, area.width, 30), "Cài đặt   ·   Bộ nhớ", 20, HudKit.Fade(HudKit.Warm, .7f));
            TextBlock(new Rect(area.x, area.y + 44, area.width, area.height - 44), "Máy tính   3,9 GB   cài ngày 05/10/2026\nẢnh   0,4 GB\nTin nhắn   0,2 GB");
            TabFooter(false);
        }

        void DrawCalculator(Rect area)
        {
            HudKit.Fill(new Rect(area.x, area.y, 420, 84), HudKit.Fade(HudKit.Paper, .08f));
            HudKit.Label(new Rect(area.x + 20, area.y + 10, 380, 64), CalculatorDisplay, 44, HudKit.Paper, false, TextAnchor.MiddleRight, false);
            if (Event.current.type == EventType.Repaint && Keyboard.current != null)
            {
                var keys = Keyboard.current;
                Key[] pad = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };
                for (int d = 0; d < pad.Length; d++) if (keys[pad[d]].wasPressedThisFrame) PressCalculator(d.ToString());
                if (keys.backspaceKey.wasPressedThisFrame || keys.deleteKey.wasPressedThisFrame) PressCalculator("C");
                if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.numpadEqualsKey.wasPressedThisFrame) PressCalculator("=");
            }
            for (int i = 0; i < CalculatorKeys.Length; i++)
            {
                var cell = new Rect(area.x + (i % 3) * 140, area.y + 104 + (i / 3) * 84, 130, 74);
                HudKit.Fill(cell, HudKit.Fade(HudKit.Paper, .07f));
                HudKit.Fill(new Rect(cell.x, cell.yMax - 2, cell.width, 2), HudKit.Fade(HudKit.Paper, .2f));
                HudKit.Label(cell, CalculatorKeys[i], 30, HudKit.Paper, false, TextAnchor.MiddleCenter, false);
                if (TabClicked(cell)) { PressCalculator(CalculatorKeys[i]); HudKit.Tick(); }
            }
            HudKit.Footer("A D", "Đổi mục", "Bàn phím số", "Nhập", "Enter", "Bằng", "Backspace", "Xóa", "Esc", "Cất máy");
        }

        void DrawVlog(Rect area, float choiceX, float choiceWidth)
        {
            HudKit.Label(new Rect(area.x, area.y, area.width, 30), "Vlog Tết 2026   ·   1:40   ·   quay dọc   ·   hành lang tầng 3 cư xá C4", 20, HudKit.Fade(HudKit.Warm, .7f));
            Pick(screenChoices, choiceX, 284, new List<(string, Action, bool)> { (State.sawVlog ? "Xem lại" : "Phát", () => PlayVlog(), true) }, 26, 62, choiceWidth);
            if (State.sawVlog)
                TextBlock(new Rect(area.x, area.y + 44, area.width, area.height - 44),
                    "Ngân: Rồi, chúc mừng năm mới cả nhà. Hôm nay em giới thiệu với mọi người một nhân vật rất quan trọng.\n" +
                    "Bé Na: Chị Ngân ơi lì xì!\n" +
                    "Ngân: Từ từ, chưa giới thiệu mà đòi rồi. Đây là Na, đệ tử ruột của chị. Na, chào mọi người đi.\n" +
                    "Bé Na: Chào mọi người. Lì xì đi chị!\n" +
                    "Ngân: Đệ tử ruột thì phải biết mật hiệu. Na gõ cho mọi người coi.\n" +
                    "Bé Na gõ lên cánh cửa: Cốc. ... Cốc cốc.\n" +
                    "Ngân: Một cái, rồi hai cái liền. Đêm nào nghe gõ vậy là chị biết Na lên. Người khác gõ chị không mở đâu nha.\n" +
                    "Bé Na: Giờ lì xì được chưa?");
            TabFooter(true);
        }

        void DrawLinhChat(Rect area)
        {
            TextBlock(area,
                "Ngân · 28/9\nTao đặt vé ngày 3/12 đấy. Ngày của tụi mình. Để tao nhớ là tao sẽ về.\n\n" +
                "Linh · 28/9\nừ.\n\n" +
                "Linh · 17/11, 21:04   ·   Đã xem\nMày đi Đà Lạt thật à? Sao không rủ tao.\n\n" +
                "Linh · 19/11, 20:47   ·   Đã xem\nCô Hòa lo lắm đấy. Mày nhắn cô một câu đi.\n\n" +
                "Linh · 22/11, 22:15   ·   Đã xem\nVề sớm. Tao đợi.");
            TabFooter(false);
        }

        void DrawLock(Rect area, float choiceX, float choiceWidth)
        {
            HudKit.Label(new Rect(area.x, area.y, area.width, 30), "Cài đặt   ·   Khóa màn hình", 20, HudKit.Fade(HudKit.Warm, .7f));
            HudKit.Label(new Rect(area.x, area.y + 44, area.width, 40), State.setNganPasscode ? "Mã 6 số: đã đặt." : "Mã 6 số: chưa đặt.", 26, HudKit.Fade(HudKit.Paper, .9f));
            if (State.setNganPasscode) { TabFooter(false); return; }
            Pick(screenChoices, choiceX, 284, new List<(string, Action, bool)> { ("Đặt mã máy", () => SetNganPasscode(), true) }, 26, 62, choiceWidth);
            TabFooter(true);
        }
    }
}
