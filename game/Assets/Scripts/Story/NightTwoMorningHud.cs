using System;
using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        const int textStyle = 20, titleStyle = 28;
        static Rect HudRect(Rect r) => new Rect(HudKit.Margin + r.x * (HudKit.Width - 2 * HudKit.Margin) / 1280f, r.y * 1.5f, r.width * (HudKit.Width - 2 * HudKit.Margin) / 1280f, r.height * 1.5f);
        static void Label(Rect r, string text, int size) => HudKit.Label(HudRect(r), text, size * 1.5f, HudKit.Paper);
        static bool Button(Rect r, string text) => HudKit.Action(HudRect(r).x, HudRect(r).y, "·", text);
        Vector2 scroll;
        readonly HudChoices screenChoices = new HudChoices();
        readonly HudSwap objectiveSwap = new HudSwap();
        string shownScreen = "";
        float screenAt = -10;
        int shownMessageCount = -1;
        bool hintDismissed;
        static float Now => Time.unscaledTime;

        public string Objective
        {
            get
            {
                if (State.stage >= MorningStage.Evening) return EveningObjective;
                switch(State.stage)
                {
                    case MorningStage.Alarm: return "Tắt báo thức 06:30.";
                    case MorningStage.Breakfast: return "Ăn bánh mì đầu hẻm. Máy của bạn (2) có ứng dụng Sức khỏe.";
                    case MorningStage.ToStore: return "Tới xe máy, chạy ra cửa hàng. Tới điểm dừng thì xuống xe.";
                    case MorningStage.Store: return "Mua kệ nhựa bốn tầng và đèn kẹp bàn.";
                    case MorningStage.ReturnHome: return "Bật máy ở cửa hàng, chở đồ về nhà trọ.";
                    case MorningStage.Gate: return "Lách qua cô Liên khi cô cúi lặt rau, vào phòng ráp kệ.";
                    case MorningStage.Assembly: return "Tới kệ trong phòng, tương tác bốn lần để ráp từng tầng.";
                    case MorningStage.Lamp: return clampLamp == null ? MissingSceneAssets : "Kẹp đèn bàn và bật thử một lần.";
                    case MorningStage.Rest: return "11:30. Nằm nghỉ một chút trên nệm.";
                    default: return "21:00. Hết lát cắt buổi sáng.";
                }
            }
        }

        static void Background(Rect rect, Color color)
        { HudKit.Fill(new Rect(rect.x * HudKit.Width / 1280f, rect.y * 1.5f, rect.width * HudKit.Width / 1280f, rect.height * 1.5f), color); }
        bool Choice(int row, string label) => Button(new Rect(80, 260 + row * 58, 1080, 48), label);

        string ScreenId()
        {
            if (State.stage == MorningStage.Ended) return "ended";
            if (page != "") return "page:" + page;
            if (story.player.ActivePhone != 0) return "phone" + story.player.ActivePhone + (story.player.ActivePhone == 1 ? ":" + NganTab : "");
            return "";
        }

        void TrackScreen(string id)
        {
            if (id == shownScreen) return;
            bool wasOverlay = shownScreen != "", isOverlay = id != "";
            if (isOverlay && !wasOverlay) HudKit.Play(MenuSounds.Open, .8f);
            else if (!isOverlay && wasOverlay) HudKit.Play(MenuSounds.Close, .8f);
            shownScreen = id; screenAt = Now;
            scroll = Vector2.zero;
            screenChoices.Reset(id == "ended" ? 1.1f : .1f);
            shownMessageCount = -1;
        }

        static int Pick(HudChoices list, float x, float y, List<(string label, Action run, bool enabled)> items, float size = 30, float row = 66, float width = 900)
        {
            int index = HudKit.Choices(list, x, y, items.Select(i => i.label).ToList(), items.Select(i => i.enabled).ToList(), size, row, width);
            if (index >= 0) items[index].run();
            return index;
        }

        public void Draw()
        {
            if (!hintDismissed) { hintDismissed = true; var hint = story.player.GetComponent<ControlsHint>(); if (hint != null) hint.Dismiss(); }
            if (story.InNightThree) { DrawThree(); return; }
            if (Transitioning) { HudKit.Fill(new Rect(0, 0, HudKit.Width, HudKit.Height), Color.black); return; }
            string id = ScreenId();
            TrackScreen(id);
            if (id == "ended") { DrawEnded(); return; }
            if (id == "") { DrawPlay(); return; }
            float age = Now - screenAt, rise = HudKit.Reveal(age, .04f, .26f);
            HudKit.Backdrop(.97f * HudKit.OutCubic(Mathf.Clamp01(age / .2f)));
            HudKit.Push(rise, 0, 14 * (1 - rise));
            if (page != "") DrawPage();
            else if (story.player.ActivePhone == 1 && State.stage >= MorningStage.Evening) DrawNganEvening();
            else if (story.player.ActivePhone == 1) DrawNganMorning();
            else DrawOwnPhone();
            HudKit.Pop();
            if (story.Subtitle != "") HudKit.Subtitle(story.Subtitle, 70);
        }

        void DrawPlay()
        {
            bool fresh = objectiveSwap.Set(HudKit.Glue(Objective));
            if (fresh && !string.IsNullOrEmpty(Objective)) HudKit.Play(MenuSounds.Note, .7f);
            HudKit.ShadeLeft(new Rect(0, 0, 980, 230), .7f);
            HudKit.Label(new Rect(64, 48, 800, 30), story.State.clock + "   ·   Đêm 2   ·   Thứ Tư, 25/11", 21, HudKit.Fade(HudKit.Warm, .85f), true);
            float age = objectiveSwap.Age, shown = HudKit.Reveal(age, .13f, .32f), flash = 1 - Mathf.Clamp01(age / .9f);
            float height = Mathf.Max(34, HudKit.TextHeight(objectiveSwap.Current, 28, 780));
            HudKit.Fill(new Rect(40, 90, 3, height - 4), HudKit.Fade(HudKit.Warm, .3f + .6f * flash));
            HudKit.Push(shown, -14 * (1 - shown));
            HudKit.Label(new Rect(64, 84, 780, 120), objectiveSwap.Current, 28, HudKit.Paper);
            HudKit.Pop();
            if (State.riding)
            {
                string road = !State.returning && !State.redLightCleared ? "Đèn đỏ phía trước, dừng tại vạch." : "Đường thẳng tới điểm dừng.";
                HudKit.Label(new Rect(64, 100 + height, 780, 30), (State.speed * 3.6f).ToString("0") + " km/h   ·   " + road, 22, HudKit.Fade(HudKit.Paper, .7f));
                if (FixedRouteRide.AtStop(State)) HudKit.Prompt("E", "Tắt máy, xuống xe");
                HudKit.Footer(true, "W", "Tăng tốc", "S", "Phanh", "E", "Xuống xe", "Esc", "Tạm dừng");
            }
            else
            {
                if (story.State.hasNganPhone) HudKit.Footer(true, "E", "Tương tác", "1", "Máy Ngân", "2", "Máy của bạn", "N", "Sổ tay", "Esc", "Tạm dừng");
                else HudKit.Footer(true, "E", "Tương tác", "2", "Máy của bạn", "N", "Sổ tay", "Esc", "Tạm dừng");
                if (Physics.Raycast(story.player.eyes.transform.position, story.player.eyes.transform.forward, out var hit, story.player.reach, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    var target = hit.collider.GetComponentInParent<MorningInteraction>();
                    if (target != null) HudKit.Prompt("E", target.Prompt);
                }
            }
            if (story.Subtitle != "") HudKit.Subtitle(story.Subtitle);
            bool blink = !story.State.night1_slept && fatiguePulse < 24 && fatiguePulse % 12 > 11.4f;
            blink |= State.stage == MorningStage.Rest && State.restSeconds > 20 && State.restSeconds % 8 > 7.3f;
            blink |= State.stage == MorningStage.Vigil && State.vigilSeconds % 5 > 4.1f;
            if (blink) HudKit.Fill(new Rect(0, 0, HudKit.Width, HudKit.Height), Color.black);
        }

        void CloseAction(string label)
        {
            if (HudKit.Action(HudKit.Width - HudKit.Margin - 170, 110, "Esc", label))
            { if (page != "") ClosePage(); else story.player.SetPhone(0); }
        }

        void DrawPage()
        {
            float left = HudKit.Margin;
            float pulse = .72f + .28f * Mathf.Sin(Now * 5.5f);
            switch (page)
            {
                case "alarm":
                    HudKit.Heading(left, 70, "06:30", "Báo thức");
                    HudKit.Label(new Rect(left, 220, 1400, 60), "Bống ơi, dậy đi con, muộn rồi.", 40, HudKit.Fade(HudKit.Alarm, pulse), true);
                    HudKit.Label(new Rect(left, 290, 1400, 40), "Bay sang Nhật: còn 8 ngày.", 26, HudKit.Fade(HudKit.Paper, .75f));
                    Pick(screenChoices, left, 380, new List<(string, Action, bool)> { ("Tắt báo thức", () => StopAlarm(), true) });
                    HudKit.Footer("W S", "Chọn", "Enter", "Xác nhận");
                    return;
                case "alarm2":
                    HudKit.Heading(left, 70, "03:12", "Báo thức máy Ngân reo");
                    HudKit.Label(new Rect(left, 220, 1400, 60), "DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.", 40, HudKit.Fade(HudKit.Alarm, pulse), true);
                    Pick(screenChoices, left, 320, new List<(string, Action, bool)> { ("Tắt báo thức", () => StopNightTwoAlarm(), true) });
                    HudKit.Footer("W S", "Chọn", "Enter", "Xác nhận");
                    return;
                case "notebook":
                    DrawNotebook(left);
                    return;
                default:
                    DrawEveningPage(left);
                    return;
            }
        }

        void DrawNotebook(float left)
        {
            CloseAction("Đóng");
            HudKit.Heading(left, 70, story.State.clock, "Sổ tay");
            var notes = story.State.notebook;
            float width = Mathf.Min(1200, HudKit.Width - 2 * left), total = 0;
            foreach (var note in notes) total += HudKit.TextHeight(note, 26, width - 20) + 22;
            var area = new Rect(left, 214, width, HudKit.Height - 214 - 180);
            scroll = HudKit.BeginScroll(area, scroll, width - 20, total);
            float y = 0;
            if (notes.Count == 0) HudKit.Label(new Rect(0, 0, width, 40), "Chưa ghi gì.", 26, HudKit.Fade(HudKit.Paper, .45f));
            for (int i = 0; i < notes.Count; i++)
            {
                float h = HudKit.TextHeight(notes[i], 26, width - 20);
                float shown = HudKit.Reveal(screenChoices.Age, .04f * i - .1f, .28f);
                HudKit.Push(shown, 0, 8 * (1 - shown));
                HudKit.Label(new Rect(0, y, width - 20, h + 4), notes[i], 26, HudKit.Fade(HudKit.Paper, .9f));
                HudKit.Pop();
                y += h + 22;
            }
            HudKit.EndScroll();
            HudKit.Footer("N", "Đóng sổ tay", "Esc", "Đóng");
        }

        static void PhoneLayout(out float left, out float listWidth, out float choiceX, out float choiceWidth)
        {
            left = HudKit.Margin;
            float inner = HudKit.Width - 2 * HudKit.Margin;
            listWidth = Mathf.Min(1040, inner * .56f); choiceX = left + listWidth + 80; choiceWidth = HudKit.Width - HudKit.Margin - choiceX;
        }

        void DrawOwnPhone()
        {
            PhoneLayout(out float left, out float listWidth, out _, out _);
            HudKit.Heading(left, 70, story.State.clock, "Máy của bạn", listWidth);
            CloseAction("Cất máy");
            HudKit.Label(new Rect(left, 214, listWidth, 30), "Sức khỏe   ·   Bước chân theo từng quãng hai phút, ngày 25/11", 20, HudKit.Fade(HudKit.Warm, .7f));
            string steps = story.State.night1_slept
                ? "02:40   38 bước\n02:42   42\n02:44   33\n02:46   37\n02:48   28\n02:50   35\n02:52 đến 03:04   0 đến 2 bước mỗi quãng\n03:06   41\n03:08   39\n03:10   33\n03:12   32\n03:14   27\n03:16 đến 03:28   0 đến 2 bước mỗi quãng\n03:30   32\n03:32   34\n03:34   25"
                : "02:40   3 bước\n02:46   5\n02:52   2\n03:06   4\n03:14   3\n03:24   5\n03:32   2";
            float total = HudKit.TextHeight(steps, 25, listWidth - 20);
            scroll = HudKit.BeginScroll(new Rect(left, 256, listWidth, HudKit.Height - 256 - 180), scroll, listWidth - 20, total);
            HudKit.Label(new Rect(0, 0, listWidth - 20, total + 4), steps, 25, HudKit.Fade(HudKit.Paper, .9f));
            HudKit.EndScroll();
            if (story.State.hasNganPhone) HudKit.Footer("1", "Máy Ngân", "Esc", "Cất máy");
            else HudKit.Footer("Esc", "Cất máy");
        }

        void DrawNganMorning()
        {
            PhoneLayout(out float left, out float listWidth, out float choiceX, out float choiceWidth);
            HudKit.Heading(left, 70, story.State.clock, "Máy Ngân", listWidth);
            CloseAction("Cất máy");
            HudKit.Label(new Rect(left, 180, listWidth, 30), story.State.night1_slept ? "Báo thức 03:12   ·   Đã tắt lúc 03:41" : "Báo thức 03:12", 20, HudKit.Fade(HudKit.Warm, .7f));
            DrawMessageList(new Rect(left, 228, listWidth, HudKit.Height - 228 - 180));
            var items = new List<(string label, Action run, bool enabled)>();
            if (story.State.night1_slept && State.roomReply == "unanswered")
            {
                items.Add(("Cháu ở phòng 7.", () => ReplyRoom(true), true));
                items.Add(("Cháu xin phép không gửi số phòng.", () => ReplyRoom(false), true));
            }
            if (items.Count > 0)
            {
                HudKit.Label(new Rect(choiceX, 214, choiceWidth, 30), "Trả lời", 20, HudKit.Fade(HudKit.Paper, .45f));
                Pick(screenChoices, choiceX, 252, items, 26, 62, choiceWidth);
                HudKit.Footer("W S", "Chọn", "Enter", "Gửi", "2", "Máy của bạn", "Esc", "Cất máy");
            }
            else HudKit.Footer("2", "Máy của bạn", "Esc", "Cất máy");
        }

        void DrawMessageList(Rect area)
        {
            var lines = story.State.messages;
            int count = lines.Count;
            var heights = new float[count];
            float total = 0;
            for (int i = 0; i < count; i++)
            {
                SplitMessage(lines[i], out _, out string body);
                heights[i] = 30 + HudKit.TextHeight(body, 25, area.width - 20) + 26; total += heights[i];
            }
            if (count != shownMessageCount) { shownMessageCount = count; scroll.y = float.MaxValue; }
            scroll = HudKit.BeginScroll(area, scroll, area.width - 20, total);
            float y = 0;
            for (int i = 0; i < count; i++)
            {
                SplitMessage(lines[i], out string meta, out string body);
                bool mine = meta.StartsWith("Bạn");
                HudKit.Fill(new Rect(0, y + 4, 2, heights[i] - 22), HudKit.Fade(mine ? HudKit.Paper : HudKit.Warm, mine ? .18f : .45f));
                HudKit.Label(new Rect(18, y, area.width - 40, 28), meta, 18, HudKit.Fade(mine ? HudKit.Paper : HudKit.Warm, .6f));
                HudKit.Label(new Rect(18, y + 28, area.width - 40, heights[i] - 28), body, 25, HudKit.Fade(HudKit.Paper, mine ? .65f : .95f));
                y += heights[i];
            }
            HudKit.EndScroll();
        }

        static void SplitMessage(string message, out string meta, out string body)
        {
            int at = message.IndexOf(": ");
            if (at < 0) { meta = ""; body = message; return; }
            meta = message.Substring(0, at); body = message.Substring(at + 2);
        }

        void DrawEnded()
        {
            HudKit.Fill(new Rect(0, 0, HudKit.Width, HudKit.Height), Color.black);
            float age = Now - screenAt, y = 360, left = HudKit.Margin;
            float head = HudKit.Reveal(age, .25f, .7f);
            HudKit.Push(head, 0, 10 * (1 - head));
            HudKit.Label(new Rect(left, y, HudKit.Width - 2 * left, 80), "Hết Đêm 2", 52, HudKit.Paper, true);
            HudKit.Pop();
            float sub = HudKit.Reveal(age, .7f, .6f);
            HudKit.Push(sub);
            HudKit.Label(new Rect(left, y + 90, 1100, 80), State.vigilDozed ? "Bạn ngủ gật lúc nào không hay. Trời đã sáng." : "Sáng 26/11/2026.", 26, HudKit.Fade(HudKit.Paper, .75f));
            HudKit.Pop();
            bool ready = screenChoices.Age >= 0;
            HudKit.Push(ready ? 1 : 0, 0, 0, !ready);
            Pick(screenChoices, left, y + 210, new List<(string, Action, bool)>
            {
                ("Sáng 26/11 - Bắt đầu Đêm 3", () => BeginThree(), true),
                ("Chơi lại đêm 1", () => story.GetComponent<BaGio12.Persistence.NightPersistence>().StartNewGame(), true),
            }, 32, 70);
            HudKit.Pop();
            HudKit.Push(HudKit.Reveal(screenChoices.Age, .2f, .4f));
            HudKit.Footer("W S", "Chọn", "Enter", "Xác nhận");
            HudKit.Pop();
        }
    }
}
