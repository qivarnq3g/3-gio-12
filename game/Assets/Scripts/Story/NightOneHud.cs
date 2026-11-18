using System;
using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Prototype;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BaGio12.Story
{
    public sealed class NightOneHud : MonoBehaviour
    {
        const string NganPhoneScreen = "nganPhone";
        public NightOneDirector story;
        readonly HudChoices phoneChoices = new HudChoices(), modalChoices = new HudChoices(), endChoices = new HudChoices(), alarmChoices = new HudChoices();
        readonly HudFade playFade = new HudFade();
        readonly HudSwap objective = new HudSwap(), clock = new HudSwap(), footer = new HudSwap(), title = new HudSwap();
        readonly Dictionary<int, float> messageAt = new Dictionary<int, float>();
        Vector2 messagesScroll, notebookScroll;
        int shownMessages = -1, shownNotes = -1, soundedMessages;
        float openedAt = -10, closedAt = -10, nextMessageSlot;
        string current = "", closing = "";
        HudChoices ghost = new HudChoices();
        bool started, alarmWasOn;
        float Now => Time.unscaledTime;
        bool Quiet => Time.timeSinceLevelLoad < .6f;

        void Update()
        {
            Transition();
            var keys=Keyboard.current;
            if(keys==null || story.player.IsPaused || story.player.PauseChangedThisFrame)return;
            if(keys.nKey.wasPressedThisFrame)story.ToggleNotebook();
            if(keys.escapeKey.wasPressedThisFrame && story.Modal!="")story.CloseModal();
        }

        string ScreenId()
        {
            if(story.BlackTitle!="")return "title";
            if(story.player.ActivePhone==1 && !story.Busy && PhonePrototype.Instance!=null)return NganPhoneScreen;
            if(story.player.ActivePhone!=0 && !story.Busy)return "phone"+story.player.ActivePhone;
            if(story.Modal!="")return "modal:"+story.Modal;
            return "";
        }

        static bool IsHudOverlay(string id) => id!="" && id!="title" && id!=NganPhoneScreen;

        void Transition()
        {
            if(!started)
            {
                started=true; current=ScreenId(); openedAt=-10;
                for(int i=0;i<story.State.messages.Count;i++)messageAt[i]=-10;
                soundedMessages=story.State.messages.Count;
                objective.Set(HudKit.Glue(story.Objective),false); clock.Set(story.State.clock,false);
                return;
            }
            string id=ScreenId();
            if(story.State.alarm && !alarmWasOn)alarmChoices.Reset(.25f);
            alarmWasOn=story.State.alarm;
            if(id==current)return;
            bool wasOverlay=IsHudOverlay(current), isOverlay=IsHudOverlay(id);
            if(wasOverlay){closing=current;closedAt=Now;ghost=(current.StartsWith("phone")?phoneChoices:modalChoices).Snapshot();}
            if(!Quiet)
            {
                if(isOverlay)HudKit.Play(MenuSounds.Open,.8f);
                else if(wasOverlay)HudKit.Play(MenuSounds.Close,.8f);
            }
            current=id; openedAt=Now;
            if(id.StartsWith("phone")){phoneChoices.Reveal(.12f);nextMessageSlot=Now+.2f;}
            if(id.StartsWith("modal:"))modalChoices.Reset(.1f);
            if(id=="title"){title.Set(story.BlackTitle);endChoices.Reset(1.1f);}
        }

        void OnGUI()
        {
            if(story.player.IsPaused){playFade.Snap(false);return;}
            var state=story.State;
            bool overlay=IsHudOverlay(current);
            bool covered=overlay || current==NganPhoneScreen;
            float play=playFade.Step(current=="" && !story.Busy,.28f,.12f);
            if(current!="title" && play>.001f){HudKit.Push(HudKit.OutCubic(play));DrawPlay(state);HudKit.Pop();}
            float closeAge=Now-closedAt, closeAlpha=closing!=""?1-HudKit.InQuad(closeAge/.16f):0;
            if(closing!="" && closeAlpha<=0)closing="";
            float openAge=Now-openedAt, back=overlay?Mathf.Clamp01(openAge/.2f):0;
            float backdrop=Mathf.Max(back,closeAlpha);
            if(backdrop>.001f)HudKit.Backdrop(.97f*HudKit.OutCubic(backdrop));
            if(closing!="" && closing!=current)
            {
                HudKit.Push(closeAlpha,0,6*(1-closeAlpha),true);
                DrawScreen(closing,ghost);
                HudKit.Pop();
            }
            if(current=="title")DrawTitle(state);
            else if(overlay)
            {
                float rise=HudKit.Reveal(openAge,.04f,.26f);
                HudKit.Push(rise,0,14*(1-rise));
                DrawScreen(current,null);
                HudKit.Pop();
            }
            if(current!="title" && (covered || !story.Busy))HudKit.Subtitle(story.Subtitle,covered?70:150);
            if(current!="title")DrawImpact();
        }

        void DrawScreen(string id,HudChoices list)
        {
            if(id.StartsWith("phone"))DrawPhone(id=="phone1"?1:2,list??phoneChoices);
            else if(id.StartsWith("modal:"))DrawModal(id.Substring(6),list??modalChoices);
        }

        static int Pick(HudChoices list, float x, float y, List<(string label, Action run, bool enabled)> items, float size = 32, float row = 70, float width = 900)
        {
            int index = HudKit.Choices(list, x, y, items.Select(i => i.label).ToList(), items.Select(i => i.enabled).ToList(), size, row, width);
            if (index >= 0) items[index].run();
            return index;
        }

        void DrawTitle(NightOneState state)
        {
            title.Set(story.BlackTitle);
            float age=title.Age;
            HudKit.Fill(new Rect(0,0,HudKit.Width,HudKit.Height),Color.black);
            bool ended=state.stage==NightStage.Ended;
            float y=ended?360:HudKit.Height/2-40;
            float head=HudKit.Reveal(age,.25f,.7f);
            HudKit.Push(head,0,10*(1-head));
            HudKit.Label(new Rect(HudKit.Margin,y,HudKit.Width-2*HudKit.Margin,80),story.BlackTitle,52,HudKit.Paper,true,ended?TextAnchor.UpperLeft:TextAnchor.UpperCenter);
            HudKit.Pop();
            if(!ended)return;
            float sub=HudKit.Reveal(age,.7f,.6f);
            HudKit.Push(sub);
            HudKit.Label(new Rect(HudKit.Margin,y+90,1100,80),state.ending=="bystander"?"Bạn giao chiếc máy cho người khác. Câu chuyện của bạn dừng lại ở đây.":"Buổi sáng 25/11 thuộc đêm 2.",26,HudKit.Fade(HudKit.Paper,.75f));
            HudKit.Pop();
            bool ready=endChoices.Age>=0;
            HudKit.Push(ready?1:0,0,0,!ready);
            Pick(endChoices,HudKit.Margin,y+210,new List<(string,Action,bool)>
            {
                ("Chơi lại từ điểm lưu",story.RestoreCheckpoint,true),
                ("Chơi lại đêm 1",()=>story.GetComponent<BaGio12.Persistence.NightPersistence>().StartNewGame(),true),
            });
            HudKit.Pop();
            HudKit.Push(HudKit.Reveal(endChoices.Age,.2f,.4f));
            HudKit.Footer("W S","Chọn","Enter","Xác nhận");
            HudKit.Pop();
        }

        void DrawPlay(NightOneState state)
        {
            bool fresh=objective.Set(HudKit.Glue(story.Objective),!Quiet);
            if(fresh && !Quiet && !string.IsNullOrEmpty(story.Objective))HudKit.Play(MenuSounds.Note,.7f);
            clock.Set(state.clock,!Quiet);
            HudKit.ShadeLeft(new Rect(0,0,980,230),.7f);
            float tick=1-Mathf.Clamp01(clock.Age/.9f);
            HudKit.Label(new Rect(64,48,800,30),state.clock+"   ·   "+(state.stage==NightStage.Prologue?"Hẻm 12":"Đêm 1"),21,HudKit.Fade(HudKit.Warm,.75f+.25f*tick),true);
            float age=objective.Age, gone=1-HudKit.InQuad(age/.12f), shown=HudKit.Reveal(age,.13f,.32f);
            float height=Mathf.Max(34,HudKit.TextHeight(objective.Current,28,780));
            float flash=1-Mathf.Clamp01(age/.9f);
            HudKit.Fill(new Rect(40,90,3,height-4),HudKit.Fade(HudKit.Warm,.3f+.6f*flash));
            if(gone>0 && objective.Previous!=""){HudKit.Push(gone,8*(1-gone));HudKit.Label(new Rect(64,84,780,120),objective.Previous,28,HudKit.Paper);HudKit.Pop();}
            HudKit.Push(shown,-14*(1-shown));
            HudKit.Label(new Rect(64,84,780,120),objective.Current,28,HudKit.Paper);
            HudKit.Pop();
            string[] keys;
            if(state.stage==NightStage.Prologue&&!state.phonePlaced) keys=new[]{"Shift","Chạy","C","Cúi","Giữ E","Đặt máy","Esc","Tạm dừng"};
            else if(state.stage==NightStage.Prologue) keys=new[]{"Shift","Chạy","C","Cúi","Esc","Tạm dừng"};
            else if(story.HasNgan) keys=new[]{"E","Tương tác","1","Máy Ngân","2","Máy của bạn","N","Sổ tay","Esc","Tạm dừng"};
            else keys=new[]{"E","Tương tác","2","Máy của bạn","N","Sổ tay","Esc","Tạm dừng"};
            footer.Set(string.Join("|",keys),!Quiet);
            if(story.player.enabled)
            {
                HudKit.Push(HudKit.Reveal(footer.Age,0,.3f));
                HudKit.Footer(true,keys);
                HudKit.Pop();
            }
            if(state.stage==NightStage.Prologue){DrawAttention();DrawStamina();}
            if(Time.time<story.LockScreenUntil)DrawLockScreen();
            if(story.OdometerShown)DrawOdometer();
            HudKit.Hold(story.HoldProgress);
            if(state.alarm)
            {
                float pulse=.72f+.28f*Mathf.Sin(Now*5.5f);
                HudKit.ShadeBand(new Rect(HudKit.Width/2-900,250,1800,320),.8f);
                HudKit.Label(new Rect(0,300,HudKit.Width,80),"DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.",48,HudKit.Fade(HudKit.Alarm,pulse),true,TextAnchor.UpperCenter);
                Pick(alarmChoices,HudKit.Width/2-220,420,new List<(string,Action,bool)>{("Tắt báo thức",story.StopAlarm,true)},30,66,440);
            }
        }

        void DrawPhone(int phone,HudChoices choices)
        {
            var s=story.State;
            float left=HudKit.Margin, inner=HudKit.Width-2*HudKit.Margin;
            float listWidth=Mathf.Min(1040,inner*.56f), choiceX=left+listWidth+80, choiceWidth=HudKit.Width-HudKit.Margin-choiceX;
            HudKit.Heading(left,70,s.clock,phone==1?"Máy Ngân":"Máy của bạn",listWidth);
            if(HudKit.Action(HudKit.Width-HudKit.Margin-170,110,"Esc","Cất máy"))story.player.SetPhone(0);
            if(phone==2)
            {
                HudKit.Label(new Rect(left,220,listWidth,200),"Thứ Ba, 24/11/2026\nChưa có tin nhắn mới.",26,HudKit.Fade(HudKit.Paper,.75f));
                return;
            }
            var items=new List<(string label,Action run,bool enabled)>();
            if(s.alarm)
            {
                HudKit.Label(new Rect(left,220,listWidth,120),"DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.",40,HudKit.Fade(HudKit.Alarm,.72f+.28f*Mathf.Sin(Now*5.5f)),true);
                Pick(choices,choiceX,230,new List<(string,Action,bool)>{("Tắt báo thức",story.StopAlarm,true)},28,62,choiceWidth);
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
            Pick(choices,choiceX,252,items,Mathf.Min(26,row*.44f),row,choiceWidth);
            HudKit.Footer("W S","Chọn","Enter","Gửi");
        }
        void DrawAttention()
        {
            var cam=story.player.eyes;
            foreach(var o in story.observers)
            {
                if(o==null||!o.isActiveAndEnabled||o.Attention<.02f||!o.Shown)continue;
                Vector3 head=o.transform.position+Vector3.up*1.6f;
                if(Physics.Linecast(cam.transform.position,head,out var hit,~0,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(o.transform)&&!hit.transform.IsChildOf(story.player.transform))continue;
                Vector3 p=cam.WorldToScreenPoint(o.transform.position+Vector3.up*2.05f);
                if(p.z<=0)continue;
                HudKit.Meter(new Vector2(p.x,Screen.height-p.y),o.Attention,o.Mode==BaGio12.Stealth.ObserverMode.Chasing);
            }
        }

        void DrawLockScreen()
        {
            float w=420, h=520, x=(HudKit.Width-w)/2, y=(HudKit.Height-h)/2-40;
            HudKit.Fill(new Rect(x,y,w,h),HudKit.Fade(HudKit.Night,.96f));
            HudKit.Fill(new Rect(x,y,w,2),HudKit.Fade(HudKit.Warm,.35f));
            HudKit.Label(new Rect(x,y+44,w,90),"20:58",72,HudKit.Paper,false,TextAnchor.UpperCenter);
            HudKit.Label(new Rect(x,y+136,w,30),"Thứ Ba, 24/11",20,HudKit.Fade(HudKit.Paper,.55f),false,TextAnchor.UpperCenter);
            HudKit.Fill(new Rect(x+24,y+210,w-48,104),HudKit.Fade(HudKit.Paper,.1f));
            HudKit.Label(new Rect(x+44,y+224,w-88,28),"Mẹ · 20:57",18,HudKit.Fade(HudKit.Warm,.8f));
            HudKit.Label(new Rect(x+44,y+254,w-88,50),"Ngân ơi con đâu rồi?",24,HudKit.Paper);
        }

        void DrawOdometer()
        {
            float w=640, h=300, x=(HudKit.Width-w)/2, y=HudKit.Height*.2f;
            HudKit.Fill(new Rect(x,y,w,h),HudKit.Fade(HudKit.Night,.94f));
            HudKit.Fill(new Rect(x,y,3,h),HudKit.Fade(HudKit.Warm,.6f));
            HudKit.Label(new Rect(x+40,y+28,w-80,30),"Đồng hồ xe",20,HudKit.Fade(HudKit.Warm,.8f),true);
            HudKit.Label(new Rect(x+40,y+62,w-80,70),NightOneDirector.Km(story.State.odo_2100)+" km",54,HudKit.Paper);
            HudKit.Fill(new Rect(x+40,y+164,w-80,96),new Color(.95f,.93f,.85f,.95f));
            HudKit.Label(new Rect(x+60,y+176,w-120,80),"Tem dán đầu xe: Thay nhớt lần sau ở "+NightOneDirector.Km(NightOneDirector.OilChangeKm)+" km",24,new Color(.08f,.07f,.06f,1));
        }

        void DrawStamina()
        {
            float stamina=story.player.Stamina;
            if(stamina>=.99f)return;
            float width=240, x=(HudKit.Width-width)/2, y=HudKit.Height*.64f+100;
            HudKit.Fill(new Rect(x,y,width,3),HudKit.Fade(HudKit.Paper,.16f));
            HudKit.Fill(new Rect(x,y,width*stamina,3),story.player.Winded?HudKit.Fade(HudKit.Alarm,.9f):HudKit.Fade(HudKit.Paper,.7f));
        }

        Texture2D vignette;
        void DrawImpact()
        {
            var full=new Rect(0,0,HudKit.Width,HudKit.Height);
            if(story.RedFlash>.01f)HudKit.Fill(full,new Color(.5f,0,0,story.RedFlash*.5f));
            if(story.Daze>.01f)
            {
                if(vignette==null)
                {
                    vignette=new Texture2D(128,128,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
                    for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                    {
                        float d=new Vector2((x-63.5f)/63.5f,(y-63.5f)/63.5f).magnitude;
                        vignette.SetPixel(x,y,new Color(.08f,0,0,Mathf.SmoothStep(0,1,(d-.35f)/.75f)));
                    }
                    vignette.Apply();
                }
                float pulse=.85f+.15f*Mathf.Sin(Time.time*5.5f);
                var previous=GUI.color;
                GUI.color=new Color(1,1,1,Mathf.Clamp01(story.Daze*1.2f)*pulse);GUI.DrawTexture(HudKit.S(full),vignette);GUI.color=previous;
            }
            if(story.Fade>.01f && story.BlackTitle=="")HudKit.Fill(full,new Color(0,0,0,story.Fade));
        }
        void OnDestroy(){if(vignette!=null)Destroy(vignette);}

        void DrawMessages(NightOneState s,Rect area)
        {
            int count=s.messages.Count;
            if(count<messageAt.Count){messageAt.Clear();for(int i=0;i<count;i++)messageAt[i]=-10;soundedMessages=count;}
            if(Event.current.type==EventType.Repaint)
            {
                for(int i=0;i<count;i++)
                    if(!messageAt.ContainsKey(i)){float t=Mathf.Max(Now,nextMessageSlot);messageAt[i]=t;nextMessageSlot=t+.11f;}
                if(soundedMessages<count && messageAt[soundedMessages]<=Now){HudKit.Play(MenuSounds.Message,.75f);soundedMessages++;}
            }
            var heights=new float[count];
            float total=0;
            for(int i=0;i<count;i++)
            {
                Split(Shown(s.messages[i]),out _,out string body);
                heights[i]=30+HudKit.TextHeight(body,25,area.width-20)+26; total+=heights[i];
            }
            if(count!=shownMessages){shownMessages=count;messagesScroll.y=float.MaxValue;}
            messagesScroll=HudKit.BeginScroll(area,messagesScroll,area.width-20,total);
            float y=0;
            for(int i=0;i<count;i++)
            {
                Split(Shown(s.messages[i]),out string meta,out string body);
                bool mine=meta.StartsWith("Bạn");
                float age=Now-(messageAt.TryGetValue(i,out float at)?at:-10);
                float shown=HudKit.Reveal(age,0,.32f), fresh=age<1.6f?1-Mathf.Clamp01(age/1.6f):0;
                HudKit.Push(shown,0,16*(1-shown));
                HudKit.Fill(new Rect(0,y+4,2,heights[i]-22),HudKit.Fade(mine?HudKit.Paper:HudKit.Warm,(mine?.18f:.45f)+.5f*fresh));
                HudKit.Label(new Rect(18,y,area.width-40,28),meta,18,HudKit.Fade(mine?HudKit.Paper:HudKit.Warm,.6f));
                HudKit.Label(new Rect(18,y+28,area.width-40,heights[i]-28),body,25,HudKit.Fade(HudKit.Paper,mine?.65f:.95f));
                HudKit.Pop();
                y+=heights[i];
            }
            HudKit.EndScroll();
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
            int ago=now-(20*60+(message.Contains("20:56")?56:57));
            return message.Replace(tag,ago<60?" · "+Mathf.Max(1,ago)+" phút trước":"");
        }

        void DrawModal(string id,HudChoices choices)
        {
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
                notebookScroll=HudKit.BeginScroll(area,notebookScroll,width-20,total);
                float y=0;
                if(notes.Count==0)HudKit.Label(new Rect(0,0,width,40),"Chưa ghi gì.",26,HudKit.Fade(HudKit.Paper,.45f));
                for(int i=0;i<notes.Count;i++)
                {
                    float h=HudKit.TextHeight(notes[i],26,width-20);
                    float shown=HudKit.Reveal(choices.Age,.04f*i-.1f,.28f);
                    HudKit.Push(shown,0,8*(1-shown));
                    HudKit.Label(new Rect(0,y,width-20,h+4),notes[i],26,HudKit.Fade(HudKit.Paper,.9f));
                    HudKit.Pop();
                    y+=h+22;
                }
                HudKit.EndScroll();
                Pick(choices,left,HudKit.Height-230,new List<(string,Action,bool)>{("Chơi lại từ điểm lưu",story.RestoreCheckpoint,true)},28,62);
                HudKit.Footer("W S","Chọn","Enter","Xác nhận","N","Đóng sổ tay");
                return;
            }
            string heading=id=="alarm"?"DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.":id=="shop"?"Tiệm chú Hùng":id=="bed"?"Bạn sẽ làm gì?":id=="police"?"Nộp chiếc máy cho công an phường?":id=="leave-phone"?"Để chiếc máy lại nhà xe?":id=="wait"?"Ngồi nghỉ tới 22:30?":"Chờ tới sáng";
            HudKit.Heading(left,70,story.State.clock,"");
            HudKit.Label(new Rect(left,104,1400,70),heading,40,id=="alarm"?HudKit.Fade(HudKit.Alarm,.72f+.28f*Mathf.Sin(Now*5.5f)):HudKit.Paper,true);
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
            Pick(choices,left,y0,items);
            HudKit.Footer("W S","Chọn","Enter","Xác nhận");
        }
    }
}
