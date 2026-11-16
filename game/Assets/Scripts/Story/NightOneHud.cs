using System;
using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BaGio12.Story
{
    public sealed class NightOneHud : MonoBehaviour
    {
        public NightOneDirector story;
        readonly HudChoices phoneChoices = new HudChoices(), modalChoices = new HudChoices(), endChoices = new HudChoices(), alarmChoices = new HudChoices();
        Vector2 messagesScroll, notebookScroll;
        int shownMessages = -1, shownNotes = -1;
        string shownModal = "";
        void Update()
        {
            var keys=Keyboard.current;
            if(keys==null || story.player.IsPaused || story.player.PauseChangedThisFrame)return;
            if(keys.nKey.wasPressedThisFrame)story.ToggleNotebook();
            if(keys.escapeKey.wasPressedThisFrame && story.Modal!="")story.CloseModal();
        }
        void OnGUI()
        {
            if(story.player.IsPaused)return;
            var state=story.State;
            if(story.BlackTitle!="") DrawTitle(state);
            else if(story.player.ActivePhone!=0 && !story.Busy)DrawPhone();
            else if(story.Modal!="")DrawModal();
            else if(!story.Busy)DrawPlay(state);
        }
        static int Pick(HudChoices list, float x, float y, List<(string label, Action run, bool enabled)> items, float size = 32, float row = 70, float width = 900)
        {
            int index = HudKit.Choices(list, x, y, items.Select(i => i.label).ToList(), items.Select(i => i.enabled).ToList(), size, row, width);
            if (index >= 0) items[index].run();
            return index;
        }
        void DrawTitle(NightOneState state)
        {
            HudKit.Fill(new Rect(0,0,HudKit.Width,HudKit.Height),Color.black);
            bool ended=state.stage==NightStage.Ended;
            float y=ended?360:HudKit.Height/2-40;
            HudKit.Label(new Rect(HudKit.Margin,y,HudKit.Width-2*HudKit.Margin,80),story.BlackTitle,52,HudKit.Paper,true,ended?TextAnchor.UpperLeft:TextAnchor.UpperCenter);
            if(!ended)return;
            HudKit.Label(new Rect(HudKit.Margin,y+90,1100,80),state.ending=="bystander"?"Bạn giao chiếc máy cho người khác. Câu chuyện của bạn dừng lại ở đây.":"Buổi sáng 25/11 thuộc đêm 2.",26,HudKit.Fade(HudKit.Paper,.75f));
            Pick(endChoices,HudKit.Margin,y+210,new List<(string,Action,bool)>
            {
                ("Chơi lại từ điểm lưu",story.RestoreCheckpoint,true),
                ("Chơi lại đêm 1",()=>story.GetComponent<BaGio12.Persistence.NightPersistence>().StartNewGame(),true),
            });
            HudKit.Footer("W S","Chọn","Enter","Xác nhận");
        }
        void DrawPlay(NightOneState state)
        {
            HudKit.ShadeLeft(new Rect(0,0,980,230),.7f);
            HudKit.Label(new Rect(64,48,800,30),state.clock+"   ·   "+(state.stage==NightStage.Prologue?"Hẻm 12":"Đêm 1"),21,HudKit.Fade(HudKit.Warm,.75f),true);
            HudKit.Label(new Rect(64,84,780,120),story.Objective,28,HudKit.Paper);
            if(state.stage==NightStage.Prologue&&!state.phonePlaced) HudKit.Footer(true,"C","Cúi","Giữ E","Đặt máy","Esc","Tạm dừng");
            else if(state.stage==NightStage.Prologue) HudKit.Footer(true,"C","Cúi","Esc","Tạm dừng");
            else if(story.HasNgan) HudKit.Footer(true,"E","Tương tác","1","Máy Ngân","2","Máy của bạn","N","Sổ tay","Esc","Tạm dừng");
            else HudKit.Footer(true,"E","Tương tác","2","Máy của bạn","N","Sổ tay","Esc","Tạm dừng");
            HudKit.Subtitle(story.Subtitle);
            if(story.HoldProgress>.01f)
            {
                float width=420,x=(HudKit.Width-width)/2,y=HudKit.Height*.64f+70;
                HudKit.Fill(new Rect(x,y,width,3),HudKit.Fade(HudKit.Paper,.18f));
                HudKit.Fill(new Rect(x,y,width*story.HoldProgress,3),HudKit.Warm);
            }
            if(state.alarm)
            {
                HudKit.ShadeBand(new Rect(HudKit.Width/2-900,250,1800,320),.8f);
                HudKit.Label(new Rect(0,300,HudKit.Width,80),"DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.",48,HudKit.Alarm,true,TextAnchor.UpperCenter);
                Pick(alarmChoices,HudKit.Width/2-220,420,new List<(string,Action,bool)>{("Tắt báo thức",story.StopAlarm,true)},30,66,440);
            }
        }
        void DrawPhone()
        {
            var s=story.State;
            HudKit.Backdrop(.97f);
            float left=HudKit.Margin, inner=HudKit.Width-2*HudKit.Margin;
            float listWidth=Mathf.Min(1040,inner*.56f), choiceX=left+listWidth+80, choiceWidth=HudKit.Width-HudKit.Margin-choiceX;
            HudKit.Heading(left,70,s.clock,story.player.ActivePhone==1?"Máy Ngân":"Máy của bạn",listWidth);
            if(HudKit.Action(HudKit.Width-HudKit.Margin-170,110,"Esc","Cất máy"))story.player.SetPhone(0);
            if(story.player.ActivePhone==2)
            {
                HudKit.Label(new Rect(left,220,listWidth,200),"Thứ Ba, 24/11/2026\nChưa có tin nhắn mới.",26,HudKit.Fade(HudKit.Paper,.75f));
                HudKit.Subtitle(story.Subtitle,70);
                return;
            }
            var items=new List<(string label,Action run,bool enabled)>();
            if(s.alarm)
            {
                HudKit.Label(new Rect(left,220,listWidth,120),"DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.",40,HudKit.Alarm,true);
                Pick(phoneChoices,choiceX,230,new List<(string,Action,bool)>{("Tắt báo thức",story.StopAlarm,true)},28,62,choiceWidth);
                HudKit.Footer("W S","Chọn","Enter","Xác nhận");
                return;
            }
            if(s.phone_powered_off)
            {
                HudKit.Label(new Rect(left,220,listWidth,60),"Máy đã tắt nguồn.",30,HudKit.Fade(HudKit.Paper,.6f));
                items.Add(("Bật nguồn",story.TogglePower,true));
            }
            else
            {
                DrawMessages(s,new Rect(left,214,listWidth,HudKit.Height-214-200));
                if(!s.sent.Any(x=>x.StartsWith("camera:")))
                {
                    items.Add(("Cháu không biết có camera không",()=>story.Send("camera:unknown","Cháu không biết."),true));
                    items.Add(("Ở nhà xe không có camera",()=>story.Send("camera:no","Ở nhà xe không có camera."),true));
                    items.Add(("Có, tiệm đầu hẻm có camera",()=>story.Send("camera:yes","Có, tiệm đầu hẻm."),s.saw_shop_camera));
                }
                if(!s.sent.Contains("why"))items.Add(("Vì sao máy ở nhà xe?",()=>story.Send("why","Vì sao máy ở nhà xe ạ?"),true));
                if(!s.sent.Contains("call"))items.Add(("Gọi Mẹ",()=>story.Send("call","[Cuộc gọi không ai nghe]"),true));
                if(s.numberArrived && !s.number_chosen)
                {
                    items.Add(("Chặn số lạ",()=>story.ChooseNumber("blocked"),true));
                    items.Add(("Trả lời / gọi lại số lạ",()=>story.ChooseNumber("replied"),true));
                    items.Add(("Kệ số lạ",()=>story.ChooseNumber("ignored"),true));
                }
                items.Add(("Đặt mã khóa",()=>story.Say("Máy người ta, đặt mã lỡ người ta không mở được."),true));
                items.Add(("Tắt nguồn",story.TogglePower,true));
            }
            HudKit.Label(new Rect(choiceX,214,choiceWidth,30),"Trả lời",20,HudKit.Fade(HudKit.Paper,.45f));
            float row=Mathf.Min(62,(HudKit.Height-260-200)/Mathf.Max(1,items.Count));
            Pick(phoneChoices,choiceX,252,items,Mathf.Min(26,row*.44f),row,choiceWidth);
            HudKit.Footer("W S","Chọn","Enter","Gửi");
            HudKit.Subtitle(story.Subtitle,70);
        }
        void DrawMessages(NightOneState s,Rect area)
        {
            var heights=new float[s.messages.Count];
            float total=0;
            for(int i=0;i<s.messages.Count;i++)
            {
                Split(Shown(s.messages[i]),out _,out string body);
                heights[i]=30+HudKit.TextHeight(body,25,area.width-20)+26; total+=heights[i];
            }
            if(s.messages.Count!=shownMessages){shownMessages=s.messages.Count;messagesScroll.y=float.MaxValue;}
            messagesScroll=GUI.BeginScrollView(HudKit.S(area),messagesScroll,HudKit.S(0,0,area.width-20,Mathf.Max(area.height,total)),false,false,GUIStyle.none,GUIStyle.none);
            float y=0;
            for(int i=0;i<s.messages.Count;i++)
            {
                Split(Shown(s.messages[i]),out string meta,out string body);
                bool mine=meta.StartsWith("Bạn");
                HudKit.Fill(new Rect(0,y+4,2,heights[i]-22),HudKit.Fade(mine?HudKit.Paper:HudKit.Warm,mine?.18f:.45f));
                HudKit.Label(new Rect(18,y,area.width-40,28),meta,18,HudKit.Fade(mine?HudKit.Paper:HudKit.Warm,.6f));
                HudKit.Label(new Rect(18,y+28,area.width-40,heights[i]-28),body,25,HudKit.Fade(HudKit.Paper,mine?.65f:.95f));
                y+=heights[i];
            }
            GUI.EndScrollView();
        }
        static void Split(string message,out string meta,out string body)
        {
            int at=message.IndexOf(": ");
            if(at<0){meta="";body=message;return;}
            meta=message.Substring(0,at);body=message.Substring(at+2);
        }
        string Shown(string message)
        {
            const string tag=" · 4 phút trước";
            if(!message.Contains(tag))return message;
            var parts=story.State.clock.Split(':');
            if(parts.Length!=2||!int.TryParse(parts[0],out int h)||!int.TryParse(parts[1],out int m))return message;
            int now=h*60+m; if(now<20*60)now+=1440;
            int ago=now-(20*60+56);
            return message.Replace(tag,ago<60?" · "+Mathf.Max(1,ago)+" phút trước":"");
        }
        void DrawModal()
        {
            string id=story.Modal;
            if(id!=shownModal){shownModal=id;modalChoices.Reset();}
            HudKit.Backdrop(.95f);
            float left=HudKit.Margin;
            if(HudKit.Action(HudKit.Width-HudKit.Margin-150,110,"Esc","Đóng"))story.CloseModal();
            if(id=="notebook")
            {
                HudKit.Heading(left,70,story.State.clock,"Sổ tay");
                var notes=story.State.notebook;
                float width=Mathf.Min(1200,HudKit.Width-2*left), total=0;
                foreach(var note in notes)total+=HudKit.TextHeight(note,26,width-20)+22;
                if(notes.Count!=shownNotes){shownNotes=notes.Count;notebookScroll.y=0;}
                var area=new Rect(left,214,width,HudKit.Height-214-260);
                notebookScroll=GUI.BeginScrollView(HudKit.S(area),notebookScroll,HudKit.S(0,0,width-20,Mathf.Max(area.height,total)),false,false,GUIStyle.none,GUIStyle.none);
                float y=0;
                if(notes.Count==0)HudKit.Label(new Rect(0,0,width,40),"Chưa ghi gì.",26,HudKit.Fade(HudKit.Paper,.45f));
                foreach(var note in notes)
                {
                    float h=HudKit.TextHeight(note,26,width-20);
                    HudKit.Label(new Rect(0,y,width-20,h+4),note,26,HudKit.Fade(HudKit.Paper,.9f));
                    y+=h+22;
                }
                GUI.EndScrollView();
                Pick(modalChoices,left,HudKit.Height-230,new List<(string,Action,bool)>{("Chơi lại từ điểm lưu",story.RestoreCheckpoint,true)},28,62);
                HudKit.Footer("W S","Chọn","Enter","Xác nhận","N","Đóng sổ tay");
                return;
            }
            string title=id=="alarm"?"DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.":id=="shop"?"Tiệm chú Hùng":id=="bed"?"Bạn sẽ làm gì?":id=="police"?"Nộp chiếc máy cho công an phường?":id=="leave-phone"?"Để chiếc máy lại nhà xe?":id=="wait"?"Ngồi nghỉ tới 22:30?":"Chờ tới sáng";
            HudKit.Heading(left,70,story.State.clock,"");
            HudKit.Label(new Rect(left,104,1400,70),title,40,id=="alarm"?HudKit.Alarm:HudKit.Paper,true);
            var items=new List<(string label,Action run,bool enabled)>();
            float y0=250;
            switch(id)
            {
                case "shop":
                    items.Add(("Mua mì gói",()=>{story.State.boughtNoodles=true;story.Say("Chú Hùng: Mới dọn vô nhà cô Liên hả?");story.CloseModal();},true));
                    items.Add(("Hỏi có ai mất điện thoại không",()=>{story.Say("Chú Hùng: Đem lên phường đi.");story.CloseModal();},true));
                    items.Add(("Xin xem camera",()=>{story.Say("Chú Hùng: Đầu ghi trong buồng, vợ chú đang ngủ.");story.CloseModal();},true));break;
                case "police": items.Add(("Giao máy và ghi tên",story.HandInPhone,true));break;
                case "leave-phone": items.Add(("Đặt máy xuống chỗ cũ",story.LeavePhone,true));break;
                case "wait": items.Add(("Chờ tới 22:30",story.WaitUntilLate,true));break;
                case "bed": items.Add(("Đi ngủ",()=>story.Rest(true),true)); items.Add(("Thức canh tới sáng",()=>story.Rest(false),true));break;
                case "alarm":
                    if(story.State.hasNganPhone && !story.State.phone_powered_off) items.Add(("Tắt báo thức",story.StopAlarm,true));
                    else
                    {
                        HudKit.Label(new Rect(left,200,1200,40),story.State.hasNganPhone?"Máy Ngân đang tắt nguồn.":"Máy Ngân đang ở nhà xe.",26,HudKit.Fade(HudKit.Paper,.7f));
                        y0=290; items.Add(("Tiếp tục thức canh",()=>story.OpenModal("dawn"),true));
                    }
                    break;
                case "dawn":
                    items.Add(("Tiếp tục chờ tới sáng",story.FinishNight,true));
                    items.Add(("Ra kiểm tra hành lang trước",story.CloseModal,true));break;
            }
            Pick(modalChoices,left,y0,items);
            HudKit.Footer("W S","Chọn","Enter","Xác nhận");
            HudKit.Subtitle(story.Subtitle,70);
        }
    }
}
