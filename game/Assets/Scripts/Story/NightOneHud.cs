using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BaGio12.Story
{
    public sealed class NightOneHud : MonoBehaviour
    {
        public NightOneDirector story;
        GUIStyle normal, heading, small;
        Vector2 messagesScroll, notebookScroll;
        void Update()
        {
            var keys=Keyboard.current;
            if(keys==null || story.player.IsPaused || story.player.PauseChangedThisFrame)return;
            if(keys.nKey.wasPressedThisFrame)story.ToggleNotebook();
            if(keys.escapeKey.wasPressedThisFrame && story.Modal!="")story.CloseModal();
        }
        void Init()
        {
            if(normal!=null)return;
            Font font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            normal=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true,font=font};
            heading=new GUIStyle(normal){fontSize=30}; small=new GUIStyle(normal){fontSize=15};
            GUI.skin.button.font=font; GUI.skin.button.fontSize=17; GUI.skin.button.wordWrap=true;
        }
        static void Panel(Rect r, Color color)
        { GUI.color=color; GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white; }
        void OnGUI()
        {
            if(story.player.IsPaused)return;
            Init();
            var original=GUI.matrix; GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            var state=story.State;
            bool ended=state.stage==NightStage.Ended;
            if(story.BlackTitle!="")
            {
                Panel(new Rect(0,0,1280,720),Color.black);
                GUI.Label(new Rect(280,260,750,100),story.BlackTitle,heading);
                if(ended)
                {
                    GUI.Label(new Rect(280,355,730,70),state.ending=="bystander" ? "Bạn giao chiếc máy cho người khác. Câu chuyện của bạn dừng lại ở đây." : "Buổi sáng 25/11 thuộc đêm 2.",normal);
                    if(GUI.Button(new Rect(280,455,260,50),"Chơi lại từ điểm lưu"))story.RestoreCheckpoint();
                    if(GUI.Button(new Rect(560,455,250,50),"Chơi lại đêm 1"))UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                }
            }
            else if(story.player.ActivePhone!=0 && !story.Busy)DrawPhone();
            else if(story.Modal!="")DrawModal();
            else if(!story.Busy)
            {
                Panel(new Rect(20,18,660,115),new Color(.025f,.035f,.05f,.85f));
                GUI.Label(new Rect(38,25,600,35),state.clock+"  •  "+(state.stage==NightStage.Prologue?"Hẻm 12":"Đêm 1"),heading);
                GUI.Label(new Rect(38,70,620,55),story.Objective,normal);
                GUI.Label(new Rect(24,675,1200,35),state.stage==NightStage.Prologue?"WASD đi · Shift chạy · C cúi · Giữ E để đặt máy · Esc tạm dừng":"WASD đi · Shift chạy · C cúi · E tương tác · 1/2 điện thoại · N Sổ tay · Esc tạm dừng",small);
                if(story.Subtitle!="")
                { Panel(new Rect(180,540,920,90),new Color(0,0,0,.8f));GUI.Label(new Rect(200,552,880,75),story.Subtitle,normal); }
                if(story.HoldProgress>.01f)
                { Panel(new Rect(450,510,380,8),Color.gray);Panel(new Rect(450,510,380*story.HoldProgress,8),new Color(.25f,.75f,.65f)); }
                if(state.alarm)
                { GUI.Label(new Rect(300,200,750,100),"DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.",heading); if(GUI.Button(new Rect(470,330,340,55),"Tắt báo thức"))story.StopAlarm(); }
            }
            GUI.matrix=original;
        }
        void DrawPhone()
        {
            var s=story.State;
            Panel(new Rect(0,0,1280,720),new Color(.035f,.045f,.06f));
            GUI.Label(new Rect(65,25,1000,55),(story.player.ActivePhone==1?"Máy Ngân":"Máy Sinh viên")+"  /  "+s.clock,heading);
            if(GUI.Button(new Rect(1080,25,150,48),"Cất máy"))story.player.SetPhone(0);
            if(story.player.ActivePhone==2)
            {
                GUI.Label(new Rect(65,120,900,140),"Máy của bạn.\nThứ Ba, 24/11/2026\nChưa có tin nhắn mới.",normal);return;
            }
            if(GUI.Button(new Rect(65,90,190,42),s.phone_powered_off?"Bật nguồn":"Tắt nguồn"))story.TogglePower();
            if(s.phone_powered_off) {GUI.Label(new Rect(65,170,900,70),"Máy đã tắt nguồn.",heading);return;}
            messagesScroll=GUI.BeginScrollView(new Rect(65,150,1120,290),messagesScroll,new Rect(0,0,1070,Mathf.Max(280,s.messages.Count*58)));
            for(int i=0;i<s.messages.Count;i++)GUI.Label(new Rect(12,i*58,1030,58),s.messages[i],normal);
            GUI.EndScrollView();
            if(story.State.alarm) {if(GUI.Button(new Rect(65,455,1120,60),"DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI. — Tắt báo thức"))story.StopAlarm();return;}
            if(GUI.Button(new Rect(65,455,340,45),"Cháu không biết có camera không"))story.Send("camera:unknown","Cháu không biết.");
            if(GUI.Button(new Rect(425,455,340,45),"Ở nhà xe không có camera"))story.Send("camera:no","Ở nhà xe không có camera.");
            GUI.enabled=s.saw_shop_camera;
            if(GUI.Button(new Rect(785,455,400,45),"Có, tiệm đầu hẻm có camera"))story.Send("camera:yes","Có, tiệm đầu hẻm.");
            GUI.enabled=true;
            if(GUI.Button(new Rect(65,515,340,45),"Vì sao máy ở nhà xe?"))story.Send("why","Vì sao máy ở nhà xe ạ?");
            if(GUI.Button(new Rect(425,515,340,45),"Gọi Mẹ"))story.Send("call","[Cuộc gọi không ai nghe]");
            if(GUI.Button(new Rect(785,515,400,45),"Đặt mã khóa"))story.Say("Máy người ta, đặt mã lỡ người ta không mở được.");
            if(s.numberArrived)
            {
                if(GUI.Button(new Rect(65,580,340,45),"Chặn số lạ"))story.ChooseNumber("blocked");
                if(GUI.Button(new Rect(425,580,340,45),"Trả lời / gọi lại số lạ"))story.ChooseNumber("replied");
                if(GUI.Button(new Rect(785,580,400,45),"Kệ số lạ"))story.ChooseNumber("ignored");
            }
            GUI.Label(new Rect(65,640,1100,65),"Tin gửi đi: Đã gửi. "+(story.Subtitle??""),small);
        }
        bool Button(int row,string label) => GUI.Button(new Rect(310,250+row*65,660,52),label);
        void DrawModal()
        {
            Panel(new Rect(0,0,1280,720),new Color(.025f,.035f,.05f,.97f));
            if(GUI.Button(new Rect(1080,25,150,48),"Đóng (Esc)"))story.CloseModal();
            string id=story.Modal;
            if(id=="notebook")
            {
                GUI.Label(new Rect(65,35,1000,60),"SỔ TAY",heading);
                notebookScroll=GUI.BeginScrollView(new Rect(65,120,1120,475),notebookScroll,new Rect(0,0,1070,Mathf.Max(470,story.State.notebook.Count*65)));
                for(int i=0;i<story.State.notebook.Count;i++)GUI.Label(new Rect(10,i*65,1040,65),story.State.notebook[i],normal);
                GUI.EndScrollView();
                if(GUI.Button(new Rect(65,625,320,50),"Chơi lại từ điểm lưu"))story.RestoreCheckpoint();
                return;
            }
            GUI.Label(new Rect(310,100,720,110),id=="alarm"?"03:12 — DẬY. KIỂM TRA CỬA. ĐỪNG NGỦ LẠI.":id=="shop"?"Tiệm chú Hùng":id=="bed"?"22:30 — Bạn sẽ làm gì?":id=="police"?"Nộp chiếc máy cho công an phường?":id=="leave-phone"?"Để chiếc máy lại nhà xe?":id=="wait"?"Ngồi nghỉ tới 22:30?":"Chờ tới sáng",heading);
            switch(id)
            {
                case "shop":
                    if(Button(0,"Mua mì gói")){story.State.boughtNoodles=true;story.Say("Chú Hùng: Mới dọn vô nhà cô Liên hả?");story.CloseModal();}
                    if(Button(1,"Hỏi có ai mất điện thoại không")){story.Say("Chú Hùng: Đem lên phường đi.");story.CloseModal();}
                    if(Button(2,"Xin xem camera")){story.Say("Chú Hùng: Đầu ghi trong buồng, vợ chú đang ngủ.");story.CloseModal();}break;
                case "police": if(Button(0,"Giao máy và ghi tên"))story.HandInPhone();break;
                case "leave-phone": if(Button(0,"Đặt máy xuống chỗ cũ"))story.LeavePhone();break;
                case "wait": if(Button(0,"Chờ tới 22:30"))story.WaitUntilLate();break;
                case "bed": if(Button(0,"Đi ngủ"))story.Rest(true); if(Button(1,"Thức canh tới sáng"))story.Rest(false);break;
                case "alarm":
                    if(story.State.hasNganPhone && !story.State.phone_powered_off) {if(Button(0,"Tắt báo thức"))story.StopAlarm();}
                    else {GUI.Label(new Rect(310,200,660,50),story.State.hasNganPhone?"Máy Ngân đang tắt nguồn.":"Máy Ngân đang ở nhà xe.",normal);if(Button(0,"Tiếp tục thức canh"))story.OpenModal("dawn");}
                    break;
                case "dawn":
                    if(Button(0,"Tiếp tục chờ tới sáng"))story.FinishNight();
                    if(Button(1,"Ra kiểm tra hành lang trước"))story.CloseModal();break;
            }
        }
    }
}
