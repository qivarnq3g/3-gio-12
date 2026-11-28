using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Persistence;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        static readonly string[] ThreeNganTabs = { "messages", "stranger", "profile", "fake", "devices", "calculator", "vault" };
        static readonly string[] ThreeNganLabels = { "Tin nhắn", "Dãy số", "Trang cá nhân", "Mẹ", "Nimbo", "Máy tính", "Két ngoài" };
        static readonly string[] ThreeOwnTabs = { "contacts", "camera", "photos", "clock" };
        static readonly string[] ThreeOwnLabels = { "Liên lạc", "Camera", "Thư viện ảnh", "Đồng hồ" };

        string ThreeObjective
        {
            get
            {
                switch(Third.stage)
                {
                    case NightThreeStage.Alarm:return "Tắt báo thức 06:30.";
                    case NightThreeStage.ToGas:return "Xuống nhà xe, bật máy. Đèn báo xăng nháy đỏ.";
                    case NightThreeStage.GasStation:return "Nhờ nhân viên đổ xăng, nắp phía trước.";
                    case NightThreeStage.GasReturn:return "Bật máy, về nhà trọ.";
                    case NightThreeStage.MorningRoom:return "Đặt báo thức 13:00 trên máy của bạn (2), rồi nằm nghỉ.";
                    case NightThreeStage.Evening:return Third.policeIntent && MissingThreeAssets!="" ? MissingThreeAssets : "Máy Ngân (1): tin thoại, lịch sử chat và trang cá nhân. Máy của bạn (2): camera, thư viện và liên lạc.";
                    case NightThreeStage.LateNight:return "23:30. Tới nệm, chọn đi ngủ hay thức canh.";
                    case NightThreeStage.DawnWaiting:return "23:47. Xem tin thoại nếu có, rồi trở lại nệm chờ sáng.";
                    default:return "Hết Đêm 3.";
                }
            }
        }

        string ThreeScreenId()
        {
            if (ThreeEnded) return "three:ended";
            if (page != "") return "three:page:" + page;
            if (story.player.ActivePhone != 0) return "three:phone" + story.player.ActivePhone + ":" + threeTab;
            return "";
        }

        void DrawThree()
        {
            if (Transitioning)
            {
                if (threeBlackout) HudKit.Fill(new Rect(0, 0, HudKit.Width, HudKit.Height), Color.black);
                else if (story.Subtitle != "") HudKit.Subtitle(story.Subtitle);
                return;
            }
            string id = ThreeScreenId();
            TrackScreen(id);
            if (id == "three:ended")
            {
                bool scapegoat = Third.stage == NightThreeStage.Scapegoat;
                DrawEndedScreen(scapegoat ? "Kẻ gánh tội" : "Hết Đêm 3", scapegoat ? "Cán bộ nhận máy, bảo bạn ngồi chờ ghi lời khai." : "Sáng 27/11. Kết thúc ba đêm.",
                    new List<(string, Action, bool)> { ("Chơi mới Đêm 1", () => story.GetComponent<NightPersistence>().StartNewGame(), true) });
                return;
            }
            if (id == "")
            {
                bool atStop = Third.riding && Third.routeMetres >= FixedRouteRide.Length && Third.speed <= .05f;
                DrawPlayHud("Đêm 3   ·   Thứ Năm, 26/11", ThreeObjective, Third.riding, Third.speed, "", atStop, false);
                return;
            }
            float age = Now - screenAt, rise = HudKit.Reveal(age, .04f, .26f);
            HudKit.Backdrop(.97f * HudKit.OutCubic(Mathf.Clamp01(age / .2f)));
            HudKit.Push(rise, 0, 14 * (1 - rise));
            if (page != "") DrawThreePage();
            else DrawThreePhone();
            HudKit.Pop();
            if (story.Subtitle != "") HudKit.Subtitle(story.Subtitle);
        }

        void DrawThreePage()
        {
            float left = HudKit.Margin;
            if (page == "alarm3")
            {
                HudKit.Heading(left, 70, "06:30", "Báo thức");
                HudKit.Label(new Rect(left, 220, 1400, 60), "Bống ơi, dậy đi con, muộn rồi.", 40, HudKit.Fade(HudKit.Alarm, .72f + .28f * Mathf.Sin(Now * 5.5f)), true);
                HudKit.Label(new Rect(left, 290, 1400, 40), "Bay sang Nhật: còn 7 ngày.", 26, HudKit.Fade(HudKit.Paper, .75f));
                Pick(screenChoices, left, 380, new List<(string, Action, bool)> { ("Tắt báo thức", () => StopThreeAlarm(), true) });
                HudKit.Footer("W S", "Chọn", "Enter", "Xác nhận");
                return;
            }
            if (page == "notebook") { DrawNotebook(left); return; }
            CloseAction("Đóng");
            HudKit.Heading(left, 70, story.State.clock, "");
            var items = new List<(string label, Action run, bool enabled)>();
            float y0 = 250;
            if (page == "tea3")
            {
                HudKit.Label(new Rect(left, 104, 1400, 70), "Ly trà sữa trên bàn cô Liên", 40, HudKit.Paper, true);
                HudKit.Label(new Rect(left, 190, 1400, 40), "Ít đường. Trân châu trắng. Cảm ơn cháu. Mẹ Ngân.", 26, HudKit.Fade(HudKit.Paper, .8f));
                items.Add(("Uống", () => DecideThreeTea(true), true));
                items.Add(("Để đó", () => DecideThreeTea(false), true));
                y0 = 280;
            }
            else
            {
                float h = Mathf.Max(60, HudKit.TextHeight(ThreeObjective, 36, 1400, true));
                HudKit.Label(new Rect(left, 104, 1400, h + 10), ThreeObjective, 36, HudKit.Paper, true);
                y0 = 130 + h;
                if (Third.stage == NightThreeStage.Evening) items.Add(("Chờ tới 23:30", () => { if (WaitThreeLate()) page = "bed3"; }, true));
                if (Third.stage == NightThreeStage.LateNight)
                {
                    items.Add(("Đi ngủ", () => ChooseThreeNight(true), true));
                    items.Add(("Thức canh", () => ChooseThreeNight(false), true));
                }
                if (Third.stage == NightThreeStage.DawnWaiting) items.Add(("Chờ sáng", () => FinishThreeDawn(), true));
            }
            if (items.Count == 0) { HudKit.Footer("Esc", "Đóng"); return; }
            Pick(screenChoices, left, y0, items);
            HudKit.Footer("W S", "Chọn", "Enter", "Xác nhận", "Esc", "Đóng");
        }

        public bool OpenThreePhoneTab(string id)
        {
            if(!ThreeReady || story.player.ActivePhone==0)return false;
            bool ngan=story.player.ActivePhone==1;
            string[] allowed=ngan?ThreeNganTabs:ThreeOwnTabs;
            if(Array.IndexOf(allowed,id)<0 || (ngan && Third.stage<NightThreeStage.Evening && id!="messages"))return false;
            if(threeTab==id)return true;threeTab=id;scroll=Vector2.zero;
            if(ngan){PhoneOperation();Checkpoint();}return true;
        }

        void DrawThreePhone()
        {
            bool ngan = story.player.ActivePhone == 1;
            PhoneLayout(out float left, out float listWidth, out float choiceX, out float choiceWidth);
            HudKit.Heading(left, 70, story.State.clock + (ngan ? "   ·   Pin " + Third.battery + "%" : ""), ngan ? "Máy Ngân" : "Máy của bạn", listWidth);
            CloseAction("Cất máy");
            string[] tabs = ngan ? ThreeNganTabs : ThreeOwnTabs, labels = ngan ? ThreeNganLabels : ThreeOwnLabels;
            if (ngan && Third.stage < NightThreeStage.Evening) { tabs = new[] { "messages" }; labels = new[] { "Tin nhắn" }; threeTab = "messages"; }
            if (!ngan && Array.IndexOf(ThreeOwnTabs, threeTab) < 0) threeTab = "contacts";
            string shownTab = threeTab == "history" ? "stranger" : threeTab;
            DrawTabStrip(tabs, labels, shownTab, tab => OpenThreePhoneTab(tab));
            var body = new Rect(left, 260, listWidth, HudKit.Height - 260 - 180);
            var items = new List<(string label, Action run, bool enabled)>();
            bool calculator = false;
            if (!ngan)
            {
                switch (threeTab)
                {
                    case "camera": DrawThreeCamera(body, items); break;
                    case "photos": DrawThreePhotos(body, items); break;
                    case "clock":
                        HudKit.Label(new Rect(body.x, body.y, body.width, 40), Third.alarm1300 ? "Báo thức 13:00" + (Third.stage >= NightThreeStage.Evening ? "   ·   Đã tắt lúc 13:00" : "") : "Chưa đặt báo thức chiều.", 30, HudKit.Paper);
                        if (Third.stage == NightThreeStage.MorningRoom) items.Add(("Đặt báo thức 13:00", () => SetThreeNapAlarm(), true));
                        break;
                    default:
                        if (!Third.doorLocked) { HudKit.Label(new Rect(body.x, body.y, body.width, 40), "Chưa có liên lạc mới.", 26, HudKit.Fade(HudKit.Paper, .6f)); break; }
                        HudKit.Label(new Rect(body.x, body.y, body.width, 40), "Cô Hòa", 30, HudKit.Paper, true);
                        if (Third.contactMode == "none") items.Add(("Nhắn cô Hòa bằng máy của bạn", () => ContactHoaThree("own_phone"), true));
                        items.Add(("Gọi cô Hòa", () => story.Say("Không biết nói gì với người mẹ đang khóc. Thôi, nhắn trước."), true));
                        if (Third.sentCasePhoto && !Third.answeredCall) items.Add(("Cô Hòa đang gọi. Nghe máy", () => AnswerThreeCall(), true));
                        if (Third.answeredCall && State.sawStorage && !Third.gotVaultCode) items.Add(("Hỏi về ứng dụng Máy tính 3,9 GB", () => AskThreeCode(), true));
                        break;
                }
            }
            else
            {
                switch (threeTab)
                {
                    case "stranger":
                    case "history":
                        string voice = Third.v1Delivered ? "Tin thoại 0:19   ·   Tiếng máy quét mã vạch, loa siêu thị..." : "Chưa có tin thoại mới.";
                        TextBlock(body, Third.sawHistory ? voice + "\n\n" + NightThreeContent.History : voice);
                        if (story.State.number_choice == "blocked") items.Add(("Bỏ chặn dãy số", () => UnblockThreeNumber(), true));
                        if (Third.v1Delivered) items.Add(("Nghe tin thoại 0:19", () => HearThreeVoice(false), true));
                        items.Add(("Cuộn xem lịch sử chat và miếng vải", () => { threeTab = "history"; OpenThreeHistory(); }, true));
                        if (Third.doorLocked && Third.contactMode == "none") items.Add(("Nhắn cô Hòa từ máy Ngân", () => ContactHoaThree("ngan_phone"), true));
                        break;
                    case "profile":
                        TextBlock(body, NightThreeContent.HoaProfile);
                        if (!Third.openedHoaProfile) items.Add(("Xem bài đăng tìm con", () => OpenHoaProfile(), true));
                        break;
                    case "fake": TextBlock(body, "Mẹ\nTrang trống   ·   Tham gia tháng 11/2026\nSố liên hệ khác số của Hòa Nguyễn."); break;
                    case "devices": TextBlock(body, "Nimbo   ·   Thiết bị\nRino 9   ·   Đã đăng xuất   ·   23/11"); break;
                    case "calculator":
                        calculator = true;
                        DrawCalculatorPad(body, vaultEntry == "" ? "0" : vaultEntry, key =>
                        {
                            if (key == "C") vaultEntry = "";
                            else if (key == "=") EnterThreeVault(vaultEntry);
                            else if (vaultEntry.Length < 12) vaultEntry += key;
                        });
                        break;
                    case "vault":
                        if (!Third.openedVault) { TextBlock(body, "Máy tính vẫn là máy tính bình thường."); break; }
                        TextBlock(body, NightThreeContent.VaultText(vaultItem));
                        for (int i = 0; i < NightThreeContent.VaultIds.Length; i++)
                        {
                            string item = NightThreeContent.VaultIds[i];
                            if (item == "intruder" && State.vaultWrongAttempts == 0) continue;
                            items.Add((NightThreeContent.VaultNames[i], () => ViewThreeVault(item), true));
                        }
                        break;
                    default: DrawMessageList(body); break;
                }
                if (Third.sentCasePhoto && !Third.answeredCall) items.Add(("Cô Hòa đang gọi. Nghe máy", () => AnswerThreeCall(), true));
                if (Third.answeredCall && State.sawStorage && !Third.gotVaultCode) items.Add(("Hỏi về ứng dụng Máy tính 3,9 GB", () => AskThreeCode(), true));
            }
            if (Third.openedVault && (threeTab == "messages" || threeTab == "contacts"))
            {
                items.Add(("Giữ máy, giải thích với cô Hòa", () => DecideThreePhone(true), true));
                items.Add(("Mang máy tới công an phường", () => DecideThreePhone(false), true));
                if (Third.keptPhone)
                {
                    items.Add(("Dặn cô Hòa: giữ kín chuyện cháu", () => AdviseHoaThree("R1"), true));
                    items.Add(("Dặn cô Hòa: chỉ nhắn vào máy cháu", () => AdviseHoaThree("R2"), true));
                    items.Add(("Dặn cô Hòa: có người đang đọc tin nhắn", () => AdviseHoaThree("R3"), true));
                }
            }
            if (Third.v2Delivered) items.Add(("Nghe tin thoại 23:47", () => HearThreeVoice(true), true));
            if (calculator) return;
            if (items.Count == 0) { TabFooter(false); return; }
            HudKit.Label(new Rect(choiceX, 246, choiceWidth, 30), "Làm gì", 20, HudKit.Fade(HudKit.Paper, .45f));
            float row = Mathf.Min(62, (HudKit.Height - 290 - 200) / Mathf.Max(1, items.Count));
            Pick(screenChoices, choiceX, 284, items, Mathf.Min(26, row * .44f), row, choiceWidth);
            TabFooter(true);
        }

        void DrawThreeCamera(Rect area, List<(string label, Action run, bool enabled)> items)
        {
            EnsureThreeCamera();var active=RenderTexture.active;
            if(Event.current.type==EventType.Repaint){try{RenderThreeCamera();}finally{RenderTexture.active=active;}}
            float w = Mathf.Min(area.width, area.height * 16 / 9f);
            HudKit.Image(new Rect(area.x, area.y, w, w * 9 / 16f), threeCamera);
            items.Add(("Chụp ảnh", () => CaptureThreePhoto(), true));
            items.Add(("Quay sang trái", () => story.player.transform.Rotate(0, -5, 0), true));
            items.Add(("Quay sang phải", () => story.player.transform.Rotate(0, 5, 0), true));
            items.Add(("Ngẩng lên", () => story.player.RestoreView(story.player.ViewPitch - 5, story.player.IsCrouched), true));
            items.Add(("Cúi xuống", () => story.player.RestoreView(story.player.ViewPitch + 5, story.player.IsCrouched), true));
        }

        void DrawThreePhotos(Rect area, List<(string label, Action run, bool enabled)> items)
        {
            if (Third.photos.Count == 0) HudKit.Label(new Rect(area.x, area.y, area.width, 40), "Chưa có ảnh nào.", 26, HudKit.Fade(HudKit.Paper, .6f));
            if (threePhotoPreview != null)
            {
                float w = Mathf.Min(area.width, area.height * threePhotoPreview.width / Mathf.Max(1f, threePhotoPreview.height));
                HudKit.Image(new Rect(area.x, area.y, w, w * threePhotoPreview.height / Mathf.Max(1f, threePhotoPreview.width)), threePhotoPreview);
            }
            for (int i = 0; i < Third.photos.Count; i++)
            {
                string photo = Third.photos[i].id;
                items.Add(("Ảnh " + (i + 1) + (selectedPhoto == photo ? "   ·   đang xem" : ""), () => SelectThreePhoto(photo), true));
            }
            if (selectedPhoto != "") items.Add(("Gửi ảnh đã chọn cho cô Hòa", () => SendThreePhoto(selectedPhoto), true));
        }

        void SelectThreePhoto(string id)
        {
            if(!Third.photos.Any(p=>p.id==id))return;selectedPhoto=id;
            if(threePhotoPreview!=null){Destroy(threePhotoPreview);threePhotoPreview=null;}
            try{var p=ThreePhotoPath(id);if(!File.Exists(p))return;byte[] bytes=File.ReadAllBytes(p);if(bytes.Length>4*1024*1024)return;
                var image=new Texture2D(2,2);if(image.LoadImage(bytes))threePhotoPreview=image;else Destroy(image);}
            catch(IOException){story.Say("Không đọc được tệp ảnh.");}
            catch(UnauthorizedAccessException){story.Say("Không có quyền đọc tệp ảnh.");}
        }
    }
}
