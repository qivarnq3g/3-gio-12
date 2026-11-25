using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        public NightThreeState Third => story.InNightThree ? story.State.nightThree : null;
        string threeTab = "messages", vaultEntry = "", vaultItem = "";
        bool threeBlackout;
        bool ThreeEnded => Third.stage == NightThreeStage.Completed || Third.stage == NightThreeStage.Scapegoat;
        bool ThreeReady => story.InNightThree && !Transitioning && !story.player.IsPaused && !ThreeEnded;

        public bool BeginThree()
        {
            if(!story.InNightTwo || story.InNightThree || State.stage!=MorningStage.Ended || Transitioning)return false;
            story.State.nightThree = new NightThreeState { odometer=State.odometer+(State.night2Slept?11:0) };
            story.State.nightThreeStarted=true;story.Clock.Set("06:30",2);
            story.player.SetPhone(0);story.CloseModal();page="alarm3";
            story.player.Teleport(Room);story.player.RestoreView(0,false);
            if(State.night2Slept) DeliverThreeAnswers("03:40");
            SyncWorld();RestoreThreeControl();Checkpoint();return true;
        }
        void DeliverThreeAnswers(string time)
        {
            foreach(string id in State.eveningSent.Where(x=>x.StartsWith("Q")))
            {
                string answer = id=="Q4"?"Cháu uống được là cô mừng.":id=="Q5"?"Bạn thân nó đấy cháu, thằng Linh, chơi với nhau từ lớp 6.":id=="Q6"?"Cô không rành điện thoại cháu ạ.":"Cháu có mở máy con bé ra xem gì không?";
                story.State.messages.Add("Mẹ · "+time+": "+answer);
            }
            story.State.messages.Add("Mẹ · "+time+": "+(State.momSuspicion==0?"Cháu nhớ ăn sáng nhé.":"Cháu có mở máy con bé ra xem gì không?"));
        }
        void RestoreThreeControl()
        {
            threeBlackout=false;vaultEntry="";vaultItem="";threeTab="messages";
            selectedPhoto="";if(threePhotoPreview!=null){Destroy(threePhotoPreview);threePhotoPreview=null;}
            page=Third.stage==NightThreeStage.Alarm?"alarm3":"";
            story.player.SetOverlay(Third.riding || page!="" || ThreeEnded);
            if(Third.riding) ThreeRidePose();
        }
        void UpdateThree()
        {
            if(!ThreeReady)return;
            RefreshThreeWorld();var keys=Keyboard.current;
            if(Third.riding)
            {
                if(keys!=null && keys.escapeKey.wasPressedThisFrame && !story.player.PauseChangedThisFrame){story.player.SetPaused(true);return;}
                ThreeRideTick(keys!=null && keys.wKey.isPressed,keys!=null && (keys.sKey.isPressed || keys.spaceKey.isPressed),Time.deltaTime);
                ThreeRidePose();if(keys!=null && keys.eKey.wasPressedThisFrame)ThreeDismount();return;
            }
            if(keys!=null && !story.player.PauseChangedThisFrame)
            {
                if(keys.escapeKey.wasPressedThisFrame && page!="") {if(page=="alarm3")StopThreeAlarm();else CloseThreePage();}
                if(keys.nKey.wasPressedThisFrame && Third.stage!=NightThreeStage.Alarm)OpenThreePage("notebook");
            }
            if(Third.stage==NightThreeStage.Evening && Minutes<At(2,"23:29"))
            {
                Third.clockSeconds+=Time.deltaTime;
                while(Third.clockSeconds>=4){Third.clockSeconds-=4;AdvanceThreeMinutes(1);}
            }
            ScheduleThree();
        }
        public bool StopThreeAlarm()
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.Alarm)return false;
            Third.stage=NightThreeStage.ToGas;CloseThreePage();story.Say("Bống ơi, dậy đi con, muộn rồi.");Checkpoint();return true;
        }
        public bool ThreeMount()
        {
            if(!ThreeReady || Third.riding || page!="" || story.player.ActivePhone!=0)return false;
            bool returning=Third.stage==NightThreeStage.GasReturn;
            if(Third.stage!=NightThreeStage.ToGas && !returning)return false;
            if(!Nearby(returning?ThreeGas:Parking,5))return false;
            Third.returning=returning;Third.riding=true;Third.routeMetres=Third.speed=0;
            story.Clock.Set(returning?"07:10":"07:00",2);LogThreeEngine(true);
            story.Say(returning?"Đã đổ xăng. Về nhà trọ.":"Đèn báo xăng nháy đỏ.");
            story.player.SetOverlay(true);ThreeRidePose();Checkpoint();return true;
        }
        public void ThreeRideTick(bool throttle,bool brake,float dt)
        {
            if(!ThreeReady || !Third.riding || !BaGio12.Persistence.GamePreferences.Finite(dt) || dt<=0)return;
            dt=Mathf.Min(dt,.1f);
            Third.speed=Mathf.MoveTowards(Third.speed,brake?0:throttle?FixedRouteRide.MaxSpeed:0,(brake?12:3)*dt);
            Third.routeMetres=Mathf.Min(FixedRouteRide.Length,Third.routeMetres+Third.speed*dt);
            if(Third.routeMetres==FixedRouteRide.Length)Third.speed=0;
        }
        void ThreeRidePose()
        {
            float progress=Mathf.Clamp01(Third.routeMetres/FixedRouteRide.Length);
            if(Third.returning)progress=1-progress;
            Vector3 corner=new Vector3(0,.08f,8);float alley=Vector3.Distance(Parking,corner),road=Vector3.Distance(corner,ThreeGas);
            float travelled=progress*(alley+road);
            Vector3 position=travelled<alley?Vector3.Lerp(Parking,corner,travelled/alley):Vector3.Lerp(corner,ThreeGas,(travelled-alley)/road);
            Vector3 forward=travelled<alley?corner-Parking:ThreeGas-corner;
            story.player.Teleport(position,Quaternion.LookRotation(Third.returning?-forward:forward).eulerAngles.y);
            story.player.RestoreView(0,false);
        }
        public bool ThreeDismount()
        {
            if(!ThreeReady || !Third.riding || Third.routeMetres<FixedRouteRide.Length || Third.speed>.05f)return false;
            Third.riding=false;Third.odometer+=.5f;
            Third.stage=Third.returning?NightThreeStage.MorningRoom:NightThreeStage.GasStation;
            story.Clock.Set(Third.returning?"07:20":"07:10",2);LogThreeEngine(false);
            story.player.SetOverlay(false);story.player.Teleport((Third.returning?Parking:ThreeGas)+Vector3.left*1.2f,90);Checkpoint();return true;
        }
        void LogThreeEngine(bool on)
        {
            State.odoLog.Add(new OdometerEntry{time=story.State.clock,km=Third.odometer,engineOn=on});
            story.State.notebook.Add(story.State.clock+" — Đồng hồ xe: "+Third.odometer.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+" km");
        }
        public bool RefuelThree()
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.GasStation || !Nearby(ThreeGas,4))return false;
            Third.refuelled=true;Third.stage=NightThreeStage.GasReturn;
            story.Say("Nhân viên: Mở yên giúp anh.\nBạn: Dạ, nắp xăng ở đằng trước.",6);Checkpoint();return true;
        }
        public bool SetThreeNapAlarm()
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.MorningRoom || story.player.ActivePhone!=2)return false;
            Third.alarm1300=true;story.Clock.Set("11:30",2);story.Say("Đã đặt báo thức 13:00.");Checkpoint();return true;
        }
        public bool ThreeNap()
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.MorningRoom || !Nearby(Room,4))return false;
            if(!Third.alarm1300){story.Say("Hôm qua nằm chút mà dậy chín giờ tối. Đặt báo thức trên máy mình (2).");return false;}
            StartThreeTransition(true);StartCoroutine(ThreeNapSequence());return true;
        }
        IEnumerator ThreeNapSequence()
        {
            yield return new WaitForSeconds(2);
            Third.stage=NightThreeStage.Evening;Third.odometer+=11;story.Clock.Set("21:00",2);
            if(!State.night2Slept)DeliverThreeAnswers("12:20");
            story.State.messages.Add("Mẹ · 12:20: "+(State.momSuspicion==0?"Cô vừa nấu xong nồi canh, lại nhớ con bé. Cháu ăn trưa chưa?":"Cháu còn giữ máy chứ?"));
            story.State.messages.Add("Mẹ · 20:10: "+(State.momSuspicion==0?"Cô sắp lên rồi, cuối tuần này. Cháu giữ máy giúp cô thêm vài hôm nữa nhé.":"Cuối tuần cô lên lấy."));
            story.player.Teleport(Room);story.Say("Ai tắt vậy. ... Ba ngày rồi. Mai đi khám.",6);EndThreeTransition();
        }
        public void AdvanceThreeMinutes(int amount)
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.Evening || amount<0)return;
            int total=Mathf.Min(At(2,"23:29"),Minutes+amount);
            story.Clock.Set((total%1440/60).ToString("00")+":"+(total%60).ToString("00"),total/1440);ScheduleThree();
        }
        void ScheduleThree()
        {
            if(Third.stage<NightThreeStage.Evening)return;
            if(!Third.v1Delivered && Minutes>=At(2,"21:05") && story.State.number_choice!="blocked")
            {Third.v1Delivered=true;story.State.messages.Add("0918 xxx 418 · 21:05 · Tin thoại 0:19");}
            if(!Third.teaArrived && !story.State.night1_slept && State.roomReply=="given" && Minutes>=At(2,"21:10"))
            {Third.teaArrived=true;story.Say("Cô Liên: Phòng 7 ơi! Có ly trà sữa ai gửi nè!",6);}
            if(!Third.v2Delivered && Third.gotVaultCode && !Third.askedTextMyNumber && !Third.toldReadReceipt && Minutes>=At(2,"23:47"))
            {Third.v2Delivered=true;story.State.messages.Add("Cô Hòa → máy Ngân · 23:47 · Tin thoại 0:38");}
        }
        public bool UnblockThreeNumber()
        {
            if(!ThreeReady || Third.stage<NightThreeStage.Evening || story.State.number_choice!="blocked")return false;
            story.State.number_choice="ignored";ScheduleThree();Checkpoint();return true;
        }
        public bool HearThreeVoice(bool late)
        {
            if(!ThreeReady || (late?!Third.v2Delivered:!Third.v1Delivered))return false;
            PhoneOperation();if(late)Third.heardV2=true;else Third.heardV1=true;
            story.Say(late?"Cô Hòa: Cô vừa đọc mã cái máy tính cho cậu sinh viên đang giữ máy con. Cậu ấy nói con để lại...":"Bống ơi... con ở đâu. Mẹ đang ở quầy, mẹ không gọi được. Con nhắn mẹ một chữ thôi...",10);
            Checkpoint();return true;
        }
        public bool OpenThreeHistory()
        {
            if(!ThreeReady || Third.stage<NightThreeStage.Evening)return false;
            Third.sawHistory=true;PhoneOperation();StartCoroutine(ThreeGlance(25,()=>{}));return true;
        }
        public bool OpenHoaProfile()
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.Evening)return false;
            PhoneOperation();if(Third.openedHoaProfile)return true;
            StartThreeTransition(false);StartCoroutine(LockThreeDoor());return true;
        }
        IEnumerator LockThreeDoor()
        {
            story.Say("Vậy ai nhắn cho mình ba ngày nay?",6);
            story.player.Teleport(Room);yield return MoveThree(Room+Vector3.forward*2.2f,1);
            if(threeDoor!=null)threeDoor.SetFollowupLocked(true);yield return new WaitForSeconds(.4f);
            yield return MoveThree(Room,1);Third.openedHoaProfile=Third.doorLocked=true;EndThreeTransition();
        }
        IEnumerator MoveThree(Vector3 destination,float seconds)
        {
            Vector3 from=story.player.transform.position;float t=0;
            while(t<seconds){t+=Time.deltaTime;story.player.Teleport(Vector3.Lerp(from,destination,Mathf.Clamp01(t/seconds)));yield return null;}
        }
        public bool ContactHoaThree(string mode)
        {
            if(!ThreeReady || !Third.doorLocked || Third.contactMode!="none" || (mode!="own_phone" && mode!="ngan_phone")
                || story.player.ActivePhone!=(mode=="own_phone"?2:1) || (mode=="ngan_phone" && story.State.number_choice=="blocked"))return false;
            Third.contactMode=mode;PhoneOperation();
            story.State.messages.Add(mode=="own_phone"?"Cô Hòa: may hom nay co bao nhieu nguoi goi bao biet con toi o dau roi doi tien. chau chung minh di":"Cô Hòa: Ai dang cam may cua con toi? Con dang o dau?");
            Checkpoint();return true;
        }
        public bool AnswerThreeCall()
        {
            if(!ThreeReady || !Third.sentCasePhoto || Third.answeredCall)return false;
            Third.answeredCall=true;PhoneOperation();
            story.State.messages.Add("Cô Hòa · 0:31: Cái miếng vải ấy... cô thêu đấy.");
            story.Say("Cô Hòa: Chiều nay lúc hai giờ cô thấy nó đọc tin cô rồi...\nBạn: Dạ... dạ...\nHai giờ chiều... mình đang ngủ mà.",12);Checkpoint();return true;
        }
        public bool AskThreeCode()
        {
            if(!ThreeReady || !Third.answeredCall || !State.sawStorage || Third.gotVaultCode)return false;
            Third.gotVaultCode=true;PhoneOperation();
            story.State.messages.Add("Cô Hòa · Tin thoại: Bốn bảy hai, sáu một chín. Nó bắt cô thuộc... nếu có người cầm máy nó mà hỏi đúng cái máy tính ấy thì đọc cho người ta.");Checkpoint();return true;
        }
        public bool EnterThreeVault(string code)
        {
            if(!ThreeReady || Third.stage<NightThreeStage.Evening || story.player.ActivePhone!=1)return false;
            PhoneOperation();
            if(code!="472619" || !Third.gotVaultCode){if(Third.wrongAttempts<int.MaxValue)Third.wrongAttempts++;return false;}
            Third.openedVault=true;threeTab="vault";Checkpoint();return true;
        }
        public bool ViewThreeVault(string id)
        {
            if(!ThreeReady || !Third.openedVault || Array.IndexOf(NightThreeContent.VaultIds,id)<0 || (id=="intruder" && State.vaultWrongAttempts==0))return false;
            vaultItem=id;PhoneOperation();bool first=!Third.viewedVault.Contains(id);
            if(first)Third.viewedVault.Add(id);
            if(first && (id=="album" || id=="knock"))StartCoroutine(ThreeGlance(id=="album"?55:0,()=>{}));
            else Checkpoint();return true;
        }
        IEnumerator ThreeGlance(float pitch,Action finish)
        {
            StartThreeTransition(false);float previous=story.player.ViewPitch;
            if(pitch==0)story.player.transform.rotation=Quaternion.LookRotation((Room+Vector3.forward*3)-story.player.transform.position);
            story.player.RestoreView(pitch,false);yield return new WaitForSeconds(1);
            story.player.RestoreView(previous,false);finish();EndThreeTransition();
        }
        public bool DecideThreePhone(bool keep)
        {
            if(!ThreeReady || !Third.openedVault || Third.stage!=NightThreeStage.Evening)return false;
            Third.keptPhone=keep;Third.policeIntent=!keep;PhoneOperation();
            story.State.messages.Add(keep?"Bạn: Người giả làm cô là đầu mối duy nhất, nộp máy thì nó im luôn.\nCô Hòa: co len phuong hai lan roi, 19 va 24. nguoi ta bao cho. [Ảnh biên nhận 19/11]":"Cô Hòa: chau mang may len cong an phuong ngay di");Checkpoint();return true;
        }
        public bool AdviseHoaThree(string id)
        {
            if(!ThreeReady || !Third.keptPhone || (id!="R1" && id!="R2" && id!="R3"))return false;
            if((id=="R1" && Third.askedKeepSecret)||(id=="R2" && Third.askedTextMyNumber)||(id=="R3" && Third.toldReadReceipt))return false;
            if(id=="R1")Third.askedKeepSecret=true;else if(id=="R2")Third.askedTextMyNumber=true;else Third.toldReadReceipt=true;
            PhoneOperation();story.State.messages.Add("Bạn: "+(id=="R1"?"Cô đừng kể chuyện cháu với ai nhé.":id=="R2"?"Cô đừng nhắn vào máy Ngân nữa, nhắn cho số cháu thôi.":"Tin cô nhắn vào máy Ngân đã xem ngay là có người đang đọc."));Checkpoint();return true;
        }
        public bool ThreeHandInPhone()
        {
            if(!ThreeReady || !Third.policeIntent || !Nearby(ThreePolice,3))return false;
            if(VisibleModel("Night3_PoliceDesk","env_police_station")==null){story.Say(MissingThreeAssets);return false;}
            Third.stage=NightThreeStage.Scapegoat;Third.ending="scapegoat";story.State.ending="scapegoat";
            story.player.SetPhone(0);story.player.SetOverlay(true);Checkpoint();return true;
        }
        public bool FindThreeCable()
        {
            if(!ThreeReady || Third.stage<NightThreeStage.Evening || Third.battery>9 || Third.foundZolpidem || !Nearby(ThreeBag,3))return false;
            Third.foundZolpidem=true;story.State.Note("Vỉ thuốc: Lê Thị Bé, zolpidem 5 mg");
            story.Say("Thuốc ngủ? ... Chắc ba lô cũ mua ngoài chợ, người ta để quên. Trong ngăn có cáp sạc.",8);Checkpoint();return true;
        }
        public bool ChargeThree()
        {
            if(!ThreeReady || !Third.foundZolpidem || !Nearby(Room,4))return false;
            Third.charged=true;Third.battery=100;Checkpoint();return true;
        }
        void PhoneOperation()
        {
            if(Third.charged || Third.stage<NightThreeStage.Evening)return;int previous=Third.battery;Third.battery=Mathf.Max(9,Third.battery-3);
            if(previous>9 && Third.battery==9)story.Say("Máy Ngân: pin yếu 9%. Sạc dự phòng bấm không lên đèn. Tìm cáp trong ba lô.",8);
        }
        public bool WaitThreeLate()
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.Evening || !Nearby(Room,4))return false;
            if(Third.openedVault && !Third.keptPhone){story.Say("Quyết định giữ máy hay mang tới phường trước.");return false;}
            if(Third.battery==9 && !Third.charged){story.Say("Tìm cáp trong ba lô và cắm sạc trước.");return false;}
            Third.stage=NightThreeStage.LateNight;story.Clock.Set("23:30",2);Checkpoint();return true;
        }
        public bool ChooseThreeNight(bool sleep)
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.LateNight || !Nearby(Room,4))return false;
            Third.night3Slept=sleep;StartThreeTransition(true);StartCoroutine(ThreeNightSequence(sleep));return true;
        }
        IEnumerator ThreeNightSequence(bool sleep)
        {
            yield return new WaitForSeconds(2);story.Clock.Set("23:47",2);ScheduleThree();
            if(sleep){story.Clock.Set("06:30",3);Third.stage=NightThreeStage.Completed;}
            else Third.stage=NightThreeStage.DawnWaiting;
            EndThreeTransition();
        }
        public bool FinishThreeDawn()
        {
            if(!ThreeReady || Third.stage!=NightThreeStage.DawnWaiting || !Nearby(Room,4))return false;
            story.Clock.Set("06:30",3);Third.stage=NightThreeStage.Completed;story.player.SetPhone(0);story.player.SetOverlay(true);Checkpoint();return true;
        }
        void StartThreeTransition(bool black)
        {Transitioning=true;threeBlackout=black;story.player.SetPhone(0);page="";story.player.SetOverlay(true);}
        void EndThreeTransition()
        {Transitioning=false;threeBlackout=false;page="";RefreshThreeWorld();story.player.SetOverlay(ThreeEnded);Checkpoint();}
        void OpenThreePage(string value){if(Third.stage==NightThreeStage.Alarm)return;page=value;story.player.SetPhone(0);story.player.SetOverlay(true);}
        void CloseThreePage(){page="";story.player.SetOverlay(false);}
    }
}
