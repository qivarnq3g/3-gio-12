using BaGio12.Gameplay;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        GUIStyle textStyle, titleStyle;
        Vector2 scroll;
        public string Objective
        {
            get
            {
                if (State.stage >= MorningStage.Evening) return EveningObjective;
                switch(State.stage)
                {
                    case MorningStage.Alarm: return "Tắt báo thức 06:30.";
                    case MorningStage.Breakfast: return "Ăn bánh mì đầu hẻm. Máy mình (2) có ứng dụng Sức khỏe.";
                    case MorningStage.ToStore: return "Tới xe máy. W tăng tốc, S/Space phanh, E xuống xe ở điểm dừng.";
                    case MorningStage.Store: return "Mua kệ nhựa bốn tầng và đèn kẹp bàn.";
                    case MorningStage.ReturnHome: return "Bật máy ở cửa hàng, chở đồ về nhà trọ.";
                    case MorningStage.Gate: return "Lách qua cô Liên khi cô cúi lặt rau, vào phòng ráp kệ.";
                    case MorningStage.Assembly: return "Tới khối Ráp kệ, tương tác bốn lần để ráp từng tầng.";
                    case MorningStage.Lamp: return "Kẹp đèn bàn và bật thử một lần.";
                    case MorningStage.Rest: return "11:30 — Nằm nghỉ một chút trên nệm.";
                    default: return "21:00 — Hết lát cắt buổi sáng. Buổi tối Đêm 2 và Đêm 3 đang được triển khai trong #22.";
                }
            }
        }
        static void Background(Rect rect, Color color)
        { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white; }
        bool Choice(int row, string label) => GUI.Button(new Rect(80, 260 + row * 58, 1080, 48), label);
        public void Draw()
        {
            if (textStyle == null)
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 20, wordWrap = true };
                titleStyle = new GUIStyle(textStyle) { fontSize = 28 };
            }
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            if (Transitioning)
            { Background(new Rect(0,0,1280,720), Color.black); GUI.matrix=previous; return; }
            if (DrawEveningOverlay()) { GUI.matrix = previous; return; }
            bool phone = story.player.ActivePhone != 0;
            if (phone || page != "")
            {
                Background(new Rect(0,0,1280,720), new Color(.025f,.04f,.055f));
                if(GUI.Button(new Rect(1090,20,150,45),page=="alarm"?"Tắt báo thức":"Đóng (Esc)"))
                { if(page=="alarm") StopAlarm(); else { ClosePage(); story.player.SetPhone(0); } }
                if(page == "notebook")
                {
                    GUI.Label(new Rect(65,25,900,60), "SỔ TAY — 25/11/2026", titleStyle);
                    scroll=GUI.BeginScrollView(new Rect(65,100,1120,550),scroll,new Rect(0,0,1060,Mathf.Max(540,story.State.notebook.Count*60)));
                    for(int i=0;i<story.State.notebook.Count;i++) GUI.Label(new Rect(0,i*60,1040,60),story.State.notebook[i],textStyle);
                    GUI.EndScrollView();
                }
                else if(page == "alarm")
                {
                    GUI.Label(new Rect(80,80,1080,120), "06:30 — Bống ơi, dậy đi con, muộn rồi.\nBay sang Nhật: còn 8 ngày.", titleStyle);
                    if(Choice(0,"Tắt báo thức")) StopAlarm();
                }
                else if(story.player.ActivePhone == 1 && State.stage >= MorningStage.Evening) DrawNganEvening();
                else if(story.player.ActivePhone == 2)
                {
                    GUI.Label(new Rect(80,70,1000,80), "Máy Sinh viên — Sức khỏe", titleStyle);
                    GUI.Label(new Rect(80,155,1080,100), "Bước chân theo từng quãng hai phút — ngày 25/11", textStyle);
                    GUI.Label(new Rect(80,270,1060,210), story.State.night1_slept
                        ? "02:40  38 bước     02:42  42     02:44  33     02:46  37     02:48  28     02:50  35\n02:52–03:04  0–2 bước mỗi quãng\n03:06  41     03:08  39     03:10  33     03:12  32     03:14  27\n03:16–03:28  0–2 bước mỗi quãng\n03:30  32     03:32  34     03:34  25"
                        : "02:40  3 bước     02:46  5     02:52  2\n03:06  4     03:14  3     03:24  5     03:32  2", textStyle);
                }
                else
                {
                    GUI.Label(new Rect(65,25,1000,60), "Máy Ngân — " + story.State.clock, titleStyle);
                    GUI.Label(new Rect(65,95,1080,60), story.State.night1_slept ? "Báo thức 03:12 · Đã tắt lúc 03:41" : "Báo thức 03:12", textStyle);
                    scroll=GUI.BeginScrollView(new Rect(65,160,1120,255),scroll,new Rect(0,0,1060,Mathf.Max(250,story.State.messages.Count*65)));
                    for(int i=0;i<story.State.messages.Count;i++) GUI.Label(new Rect(0,i*65,1040,65),story.State.messages[i],textStyle);
                    GUI.EndScrollView();
                    if(story.State.night1_slept && State.stage < MorningStage.Evening && State.roomReply == "unanswered")
                    {
                        if(GUI.Button(new Rect(65,465,530,55),"Cháu ở phòng 7.")) ReplyRoom(true);
                        if(GUI.Button(new Rect(615,465,570,55),"Cháu xin phép không gửi số phòng.")) ReplyRoom(false);
                    }
                    GUI.Label(new Rect(65,580,1080,90), "Giao diện tạm cho bản thô, chờ ghép điện thoại #17.", textStyle);
                }
            }
            else
            {
                Background(new Rect(20,18,1000,125), new Color(.02f,.03f,.05f,.9f));
                GUI.Label(new Rect(38,25,950,40), story.State.clock + " — Đêm 2 · Thứ Tư, 25/11/2026", titleStyle);
                GUI.Label(new Rect(38,72,950,70), Objective, textStyle);
                GUI.Label(new Rect(30,650,1220,60), State.riding ? "W tăng tốc · S/Space phanh · E xuống xe khi tới nơi · Esc tạm dừng"
                    : "WASD đi · E tương tác · 1 máy Ngân · 2 máy mình · N Sổ tay · Esc tạm dừng", textStyle);
                if (State.riding)
                {
                    GUI.Label(new Rect(40,165,1050,65), "Tốc độ: " + (State.speed*3.6f).ToString("0") + " km/h    "
                        + (!State.returning && !State.redLightCleared ? "Đèn đỏ — dừng tại vạch" : "Đường thẳng tới điểm dừng"), textStyle);
                    if(FixedRouteRide.AtStop(State)) GUI.Label(new Rect(340,350,700,70), "Đã tới nơi. E — tắt máy, xuống xe.", titleStyle);
                }
                else
                {
                    GUI.Label(new Rect(635,345,25,30), "+", textStyle);
                    if(Physics.Raycast(story.player.eyes.transform.position, story.player.eyes.transform.forward, out var hit, story.player.reach, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore))
                    {
                        var target=hit.collider.GetComponentInParent<MorningInteraction>();
                        if(target!=null) GUI.Label(new Rect(300,490,800,60), "E — " + target.Prompt, textStyle);
                    }
                }
                if(story.Subtitle!="") GUI.Label(new Rect(130,560,1040,80), story.Subtitle, textStyle);
                // Two short eyelid beats after staying up; progressive drowsiness
                // during the final minute before the forced nap.
                bool blink = !story.State.night1_slept && fatiguePulse < 24 && fatiguePulse%12 > 11.4f;
                blink |= State.stage == MorningStage.Rest && State.restSeconds > 20 && State.restSeconds%8 > 7.3f;
                blink |= State.stage == MorningStage.Vigil && State.vigilSeconds%5 > 4.1f;
                if(blink) Background(new Rect(0,0,1280,720), Color.black);
            }
            GUI.matrix = previous;
        }
    }
}
