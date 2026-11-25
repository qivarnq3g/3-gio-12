using System;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Persistence;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
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
                    case NightThreeStage.MorningRoom:return "Đặt báo thức 13:00 trên máy mình (2), rồi nằm nghỉ.";
                    case NightThreeStage.Evening:return Third.policeIntent && MissingThreeAssets!="" ? MissingThreeAssets : "Máy Ngân (1): tin thoại, lịch sử chat và trang cá nhân. Máy mình (2): camera, thư viện và liên lạc.";
                    case NightThreeStage.LateNight:return "23:30 — Tới nệm, chọn đi ngủ hay thức canh.";
                    case NightThreeStage.DawnWaiting:return "23:47 — Xem tin thoại nếu có, rồi trở lại nệm chờ sáng.";
                    default:return "Hết Đêm 3.";
                }
            }
        }
        void DrawThree()
        {
            if(Transitioning)
            {
                if(threeBlackout)Background(new Rect(0,0,1280,720),Color.black);
                else Label(new Rect(100,550,1080,120),story.Subtitle,textStyle);
                return;
            }
            if(ThreeEnded)
            {
                Background(new Rect(0,0,1280,720),Color.black);
                Label(new Rect(160,220,1050,80),Third.stage==NightThreeStage.Scapegoat?"KẺ GÁNH TỘI":"HẾT ĐÊM 3",titleStyle);
                Label(new Rect(160,320,1050,100),Third.stage==NightThreeStage.Scapegoat?"Cán bộ nhận máy, bảo bạn ngồi chờ ghi lời khai.":"Sáng 27/11. Kết thúc ba đêm.",textStyle);
                if(Button(new Rect(160,470,420,55),"Chơi mới Đêm 1"))story.GetComponent<NightPersistence>().StartNewGame();return;
            }
            if(page!="")
            {
                Background(new Rect(0,0,1280,720),new Color(.025f,.04f,.055f));
                if(page=="alarm3")
                {
                    Label(new Rect(80,80,1080,140),"06:30 — Bống ơi, dậy đi con, muộn rồi.\nBay sang Nhật: còn 7 ngày.",titleStyle);
                    if(Choice(0,"Tắt báo thức (Esc)"))StopThreeAlarm();return;
                }
                if(Button(new Rect(1090,20,150,45),"Đóng (Esc)"))CloseThreePage();
                if(page=="notebook"){DrawThreeLog(story.State.notebook);return;}
                if(page=="tea3")
                {
                    Label(new Rect(80,100,1080,120),"Ít đường. Trân châu trắng. Cảm ơn cháu. Mẹ Ngân.",titleStyle);
                    if(Choice(0,"Uống"))DecideThreeTea(true);if(Choice(1,"Để đó"))DecideThreeTea(false);return;
                }
                Label(new Rect(80,100,1080,100),ThreeObjective,titleStyle);
                if(Third.stage==NightThreeStage.Evening){if(Choice(0,"Chờ tới 23:30")) {if(WaitThreeLate())page="bed3";}}
                if(Third.stage==NightThreeStage.LateNight)
                {if(Choice(1,"Đi ngủ"))ChooseThreeNight(true);if(Choice(2,"Thức canh"))ChooseThreeNight(false);}
                if(Third.stage==NightThreeStage.DawnWaiting && Choice(1,"Chờ sáng"))FinishThreeDawn();
                Label(new Rect(80,580,1080,80),story.Subtitle,textStyle);return;
            }
            if(story.player.ActivePhone!=0){DrawThreePhone();return;}
            Background(new Rect(20,18,1150,130),new Color(.02f,.03f,.05f,.9f));
            Label(new Rect(38,25,1100,40),story.State.clock+" — Đêm 3 · Thứ Năm, 26/11/2026",titleStyle);
            Label(new Rect(38,75,1100,65),ThreeObjective,textStyle);
            Label(new Rect(30,660,1220,55),Third.riding?"W tăng tốc · S/Space phanh · E xuống xe khi tới nơi · Esc tạm dừng":"WASD đi · E tương tác · 1 máy Ngân · 2 máy mình · N Sổ tay · Esc tạm dừng",textStyle);
            if(Third.riding)Label(new Rect(60,200,1000,60),"Tốc độ: "+(Third.speed*3.6f).ToString("0")+" km/h"+(Third.routeMetres>=80?" · E xuống xe":""),textStyle);
            else
            {
                HudKit.Label(new Rect(HudKit.Width/2-8,HudKit.Height/2-16,24,32),"+",24,HudKit.Paper);
                if(Physics.Raycast(story.player.eyes.transform.position,story.player.eyes.transform.forward,out var hit,story.player.reach,PlayerGeometry.WorldMask,QueryTriggerInteraction.Ignore))
                {var node=hit.collider.GetComponentInParent<MorningInteraction>();if(node!=null)HudKit.Prompt("E",node.Prompt);}
            }
            Label(new Rect(100,560,1080,85),story.Subtitle,textStyle);
        }
        void DrawThreeLog(System.Collections.Generic.List<string> lines)
        {
            float width = HudRect(new Rect(0,0,1100,0)).width;
            float height=lines.Sum(x=>HudKit.TextHeight(x,30,width)+27);
            scroll=HudKit.BeginScroll(HudRect(new Rect(65,150,1120,345)),scroll,width,Mathf.Max(510,height));
            float y=0;
            foreach(string line in lines)
            { float h=HudKit.TextHeight(line,30,width); HudKit.Label(new Rect(0,y,width,h),line,30,HudKit.Paper); y+=h+27; }
            HudKit.EndScroll();
        }
        public bool OpenThreePhoneTab(string id)
        {
            if(!ThreeReady || story.player.ActivePhone==0)return false;
            bool ngan=story.player.ActivePhone==1;
            string[] allowed=ngan?new[]{"messages","stranger","profile","fake","devices","calculator","vault"}:new[]{"contacts","camera","photos","clock"};
            if(Array.IndexOf(allowed,id)<0 || (ngan && Third.stage<NightThreeStage.Evening && id!="messages"))return false;
            if(threeTab==id)return true;threeTab=id;scroll=Vector2.zero;
            if(ngan){PhoneOperation();Checkpoint();}return true;
        }
        void DrawThreePhone()
        {
            bool ngan=story.player.ActivePhone==1;
            Background(new Rect(0,0,1280,720),new Color(.025f,.04f,.055f));
            Label(new Rect(65,25,1000,50),(ngan?"Máy Ngân · "+Third.battery+"%":"Máy Sinh viên")+" · "+story.State.clock,titleStyle);
            if(Button(new Rect(1090,20,150,45),"Cất máy"))story.player.SetPhone(0);
            string[] tabs=ngan?new[]{"messages","stranger","profile","fake","devices","calculator","vault"}:new[]{"contacts","camera","photos","clock"};
            string[] labels=ngan?new[]{"Tin nhắn","Dãy số","Trang cá nhân","Mẹ","Nimbo","Máy tính","Két ngoài"}:new[]{"Liên lạc","Camera","Thư viện ảnh","Đồng hồ"};
            if(ngan && Third.stage<NightThreeStage.Evening){tabs=new[]{"messages"};labels=new[]{"Tin nhắn"};threeTab="messages";}
            for(int i=0;i<tabs.Length;i++)if(Button(new Rect(65+i*158,85,150,45),labels[i]))OpenThreePhoneTab(tabs[i]);
            if(!ngan)
            {
                switch(threeTab)
                {
                    case "camera":DrawThreeCamera();break;
                    case "photos":DrawThreePhotos();break;
                    case "clock":
                        Label(new Rect(80,170,1080,100),Third.alarm1300?"Báo thức 13:00"+(Third.stage>=NightThreeStage.Evening?" · Đã tắt lúc 13:00":""):"Chưa đặt báo thức chiều.",titleStyle);
                        if(Third.stage==NightThreeStage.MorningRoom && Choice(0,"Đặt báo thức 13:00"))SetThreeNapAlarm();break;
                    default:
                        if(!Third.doorLocked){Label(new Rect(80,180,1080,120),"Chưa có liên lạc mới.",textStyle);break;}
                        if(Third.contactMode=="none" && Choice(0,"Nhắn cô Hòa bằng máy mình"))ContactHoaThree("own_phone");
                        if(Choice(1,"Gọi cô Hòa"))story.Say("Không biết nói gì với người mẹ đang khóc. Thôi, nhắn trước.");
                        if(Third.sentCasePhoto&&!Third.answeredCall && Choice(2,"Cô Hòa đang gọi — Nghe"))AnswerThreeCall();
                        if(Third.answeredCall&&State.sawStorage&&!Third.gotVaultCode && Choice(3,"Hỏi về cái app Máy tính 3,9 GB"))AskThreeCode();break;
                }
            }
            else switch(threeTab)
            {
                case "stranger":case "history":
                    Label(new Rect(65,150,1100,110),Third.v1Delivered?"Tin thoại 0:19 · Tiếng máy quét mã vạch, loa siêu thị...":"Chưa có tin thoại mới.",textStyle);
                    if(story.State.number_choice=="blocked" && Button(new Rect(65,260,500,45),"Bỏ chặn dãy số"))UnblockThreeNumber();
                    if(Third.v1Delivered && Button(new Rect(65,260,500,45),"Nghe tin thoại 0:19"))HearThreeVoice(false);
                    if(Button(new Rect(585,260,580,45),"Cuộn xem lịch sử chat và miếng vải")){threeTab="history";OpenThreeHistory();}
                    if(Third.sawHistory)Label(new Rect(65,325,1100,210),NightThreeContent.History,textStyle);
                    if(Third.doorLocked && Third.contactMode=="none" && Button(new Rect(65,555,1100,45),"Nhắn cô Hòa từ máy Ngân"))ContactHoaThree("ngan_phone");break;
                case "profile":
                    Label(new Rect(65,155,1100,210),NightThreeContent.HoaProfile,textStyle);
                    if(!Third.openedHoaProfile && Button(new Rect(65,390,1100,45),"Xem bài đăng tìm con"))OpenHoaProfile();break;
                case "fake":Label(new Rect(65,160,1100,150),"Mẹ\nTrang trống · Tham gia tháng 11/2026\nSố liên hệ khác số của Hòa Nguyễn.",textStyle);break;
                case "devices":Label(new Rect(65,160,1100,150),"Nimbo > Thiết bị\nRino 9 · Đã đăng xuất · 23/11",textStyle);break;
                case "calculator":
                    Label(new Rect(65,150,700,65), vaultEntry == "" ? "0" : vaultEntry, titleStyle);
                    for (int i=0;i<CalculatorKeys.Length;i++)
                        if (Button(new Rect(65+(i%3)*150,230+(i/3)*80,140,70),CalculatorKeys[i]))
                        {
                            string key=CalculatorKeys[i];
                            if(key=="C")vaultEntry="";
                            else if(key=="=")EnterThreeVault(vaultEntry);
                            else if(vaultEntry.Length<12)vaultEntry+=key;
                        }
                    break;
                case "vault":
                    if(!Third.openedVault){Label(new Rect(65,170,1100,80),"Máy tính vẫn là máy tính bình thường.",textStyle);break;}
                    for(int i=0;i<NightThreeContent.VaultIds.Length;i++)
                    {
                        string id=NightThreeContent.VaultIds[i];if(id=="intruder" && State.vaultWrongAttempts==0)continue;
                        if(Button(new Rect(65,150+i*48,270,43),NightThreeContent.VaultNames[i]))ViewThreeVault(id);
                    }
                    Label(new Rect(370,155,800,370),NightThreeContent.VaultText(vaultItem),textStyle);break;
                default:DrawThreeLog(story.State.messages);break;
            }
            if(ngan && Third.sentCasePhoto&&!Third.answeredCall && Button(new Rect(65,610,520,40),"Cô Hòa đang gọi — Nghe"))AnswerThreeCall();
            if(ngan && Third.answeredCall&&State.sawStorage&&!Third.gotVaultCode && Button(new Rect(600,610,560,40),"Hỏi về app Máy tính 3,9 GB"))AskThreeCode();
            if(Third.openedVault && (threeTab=="messages" || threeTab=="contacts"))
            {
                if(Button(new Rect(65,510,530,45),"Giữ máy, giải thích với cô Hòa"))DecideThreePhone(true);
                if(Button(new Rect(615,510,550,45),"Mang máy tới công an phường"))DecideThreePhone(false);
                if(Third.keptPhone)
                    for(int i=0;i<3;i++)if(Button(new Rect(65+i*365,565,350,40),new[]{"R1: Giữ kín chuyện cháu","R2: Chỉ nhắn máy cháu","R3: Có người đang đọc"}[i]))AdviseHoaThree("R"+(i+1));
            }
            if(Third.v2Delivered && Button(new Rect(65,660,520,40),"Tin thoại 23:47 — Nghe"))HearThreeVoice(true);
            Label(new Rect(610,660,560,40),story.Subtitle,textStyle);
        }
        void DrawThreeCamera()
        {
            EnsureThreeCamera();var active=RenderTexture.active;
            if(Event.current.type==EventType.Repaint){try{RenderThreeCamera();}finally{RenderTexture.active=active;}}
            HudKit.Image(HudRect(new Rect(65,155,768,432)),threeCamera);
            if(Button(new Rect(870,200,290,50),"Chụp ảnh"))CaptureThreePhoto();
            if(Button(new Rect(870,280,130,45),"←"))story.player.transform.Rotate(0,-5,0);
            if(Button(new Rect(1020,280,130,45),"→"))story.player.transform.Rotate(0,5,0);
            if(Button(new Rect(870,340,130,45),"↑"))story.player.RestoreView(story.player.ViewPitch-5,story.player.IsCrouched);
            if(Button(new Rect(1020,340,130,45),"↓"))story.player.RestoreView(story.player.ViewPitch+5,story.player.IsCrouched);
        }
        void DrawThreePhotos()
        {
            float height=Mathf.Max(320,Third.photos.Count*55);
            scroll=HudKit.BeginScroll(HudRect(new Rect(65,150,350,350)),scroll,290,height*1.5f);
            for(int i=0;i<Third.photos.Count;i++)if(HudKit.Action(0,i*82,"·","Ảnh "+(i+1)))SelectThreePhoto(Third.photos[i].id);
            HudKit.EndScroll();
            if(threePhotoPreview!=null)HudKit.Image(HudRect(new Rect(450,155,710,330)),threePhotoPreview);
            if(selectedPhoto!="" && Button(new Rect(450,510,710,50),"Gửi ảnh đã chọn cho cô Hòa"))SendThreePhoto(selectedPhoto);
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
