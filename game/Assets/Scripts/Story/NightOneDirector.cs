using System;
using System.Collections;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Stealth;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed class NightOneDirector : MonoBehaviour
    {
        public FirstPersonPlayer player;
        public GameObject nganPhone, bag, greyGlove;
        public Light parkingAlertLight;
        public NightPassingBike[] passingBikes;
        public Transform alleyStart, parkingSpawn, roomSpawn, bed, bagAtDoor;
        public Transform[] hidingPoints;
        public NightObserver[] observers;
        public Light[] alertLights;
        public NightOneState State { get; private set; } = new NightOneState();
        public StoryClock Clock { get; private set; }
        public readonly CheckpointService Checkpoints = new CheckpointService();
        public NightDialogue Dialogue { get; private set; }
        public event Action<string, string> InternalEvent;
        public event Action<NightTelemetry> TelemetryProduced;
        public bool PrologueDeparted { get; private set; }
        public float PrologueElapsed { get; private set; }
        public bool AlarmAudible => sound != null && sound.isPlaying;
        public string Objective { get; private set; }
        public string Subtitle { get; private set; }
        public string Modal { get; private set; } = "";
        public string BlackTitle { get; private set; } = "";
        public float HoldProgress { get; set; }
        public bool Busy { get; private set; }
        public bool InParking => player.transform.position.z >= 27 && player.transform.position.y < 2;
        public bool InRoom => player.transform.position.y > 7 && player.transform.position.x > 12;
        public bool Dark => !IsIlluminated(player.eyes.transform.position);
        public bool IsIlluminated(Vector3 point)
        {
            return alertLights.Any(light => LightReaches(light, point)) || LightReaches(parkingAlertLight, point)
                || (passingBikes != null && passingBikes.Any(bike => LightReaches(bike.headlight, point)));
        }
        static bool LightReaches(Light light, Vector3 point)
        {
            if (light == null || !light.isActiveAndEnabled) return false;
            var delta=point-light.transform.position;
            if(delta.magnitude>light.range)return false;
            if(light.type==LightType.Spot && Vector3.Angle(light.transform.forward,delta)>light.spotAngle*.5f)return false;
            return !Physics.Raycast(light.transform.position,delta.normalized,delta.magnitude,PlayerGeometry.WorldMask,QueryTriggerInteraction.Ignore);
        }
        public bool Hidden => hidingPoints.Any(x => Vector3.Distance(x.position, player.transform.position) < 1.3f) && !player.IsRunning;
        public bool CanPlace => State.stage == NightStage.Prologue && !State.phonePlaced && !Busy && !observers.Any(x => x.ActiveThreat);
        public bool NotebookOpen { get; private set; }
        public bool HasNgan => State.hasNganPhone;
        float subtitleUntil, roomSeconds, unseenSeconds, immuneUntil;
        int shoutCount;
        bool leftStart;
        float nightStartedAt;
        static bool completedInSession;
        bool isReplay;
        AudioSource sound;
        AudioClip alarmClip;

        void Awake()
        {
            Clock = new StoryClock(State);
            Dialogue = JsonUtility.FromJson<NightDialogue>(Resources.Load<TextAsset>("Story/night-one-dialogue").text);
            sound = gameObject.AddComponent<AudioSource>(); sound.loop = true; sound.volume = .13f;
            alarmClip = AudioClip.Create("Graybox alarm", 22050, 1, 22050, false);
            var samples = new float[22050];
            for (int i=0;i<samples.Length;i++) samples[i] = i % 11025 < 5500 ? Mathf.Sin(2*Mathf.PI*880*i/22050f)*.2f : 0;
            alarmClip.SetData(samples,0); sound.clip = alarmClip;
        }
        void Start()
        {
            player.showTestHud = false;
            player.CanOpenPhone = phone => !Busy && State.stage != NightStage.Prologue && State.stage != NightStage.Ended && (phone == 2 || State.hasNganPhone);
            player.PhoneChanged += PhoneChanged;
            player.FellOutOfWorld += RestoreCheckpoint;
            nightStartedAt=Time.time;
            isReplay=completedInSession;
            TelemetryProduced?.Invoke(new NightStartedTelemetry(isReplay));
            Checkpoints.Save("n1.alley_start", State, alleyStart.position, 0);
            Objective = "Tới nhà xe cuối hẻm. Cúi và đặt chiếc máy dưới chân xe.";
            Say("Đừng để ai thấy.", 6);
            SyncWorld();
        }
        void OnDestroy()
        {
            if (player != null) { player.PhoneChanged -= PhoneChanged; player.FellOutOfWorld -= RestoreCheckpoint; player.CanOpenPhone = null; }
            if (alarmClip != null) Destroy(alarmClip);
        }
        void Update()
        {
            if(player.IsPaused)return;
            if (Time.time > subtitleUntil) Subtitle = "";
            if (HoldProgress > 0) HoldProgress = Mathf.Max(0, HoldProgress - Time.deltaTime * .1f);
            if (State.stage == NightStage.Prologue && !Busy)
            {
                if (!PrologueDeparted && player.transform.position.z > 3) PrologueDeparted=true;
                if(PrologueDeparted)PrologueElapsed+=Time.deltaTime;
                if (player.transform.position.z > 5) leftStart = true;
                bool seen = observers.Any(x => x.CanSeePlayer());
                unseenSeconds = seen ? 0 : unseenSeconds + Time.deltaTime;
                if (Hidden && unseenSeconds >= 8) foreach (var o in observers) o.LoseTrail();
                if (player.IsRunning || (State.prologue_beaten >= 2 && Hidden))
                    foreach (var o in observers)
                        if (Vector3.Distance(o.transform.position, player.transform.position) < (player.IsRunning ? 5 : 3)) o.Hear(player.transform.position, false);
                if (leftStart && player.transform.position.z < 1.2f)
                {
                    if (State.phonePlaced) StartCoroutine(CompletePrologue());
                    else ResetPrologue();
                }
            }
            if (State.stage == NightStage.Room && !Busy)
            {
                if (State.timeJumped && !State.numberArrived)
                { roomSeconds += Time.deltaTime; if (roomSeconds >= 8) ArriveStranger(); }
                if (!State.timeJumped && !State.hasNganPhone && InRoom && Vector3.Distance(player.transform.position, bed.position) < 2) JumpTime();
            }
            SyncWorld();
        }
        public void Say(string text, float duration = 5) { Subtitle = text; subtitleUntil = Time.time + duration; }
        public void EmitInternal(string type, string detail) { InternalEvent?.Invoke(type, detail); Debug.Log($"[Night1 internal] {type}: {detail}"); }
        void EmitChoice(string id) { TelemetryProduced?.Invoke(new ChoiceTelemetry(id)); }
        public void HandInPhone()
        {
            if(!State.hasNganPhone || State.stage==NightStage.Prologue || State.stage==NightStage.Ended)return;
            EmitChoice("0"); End("bystander");
        }
        public void OpenModal(string id) { if(id=="notebook" && State.stage==NightStage.Prologue)return; Modal = id; player.SetPhone(0); player.SetOverlay(true); }
        public void CloseModal() { Modal = ""; NotebookOpen = false; player.SetOverlay(false); }
        public void ToggleNotebook()
        {
            if (Busy || State.stage == NightStage.Ended || State.stage == NightStage.Prologue) return;
            if (NotebookOpen) CloseModal();
            else { OpenModal("notebook"); NotebookOpen = true; }
        }
        void PhoneChanged(int previous, int next)
        {
            if (State.stage == NightStage.Room && InRoom)
            {
                if (next != 0) { player.Teleport(bed.position + new Vector3(0,.23f,-.4f),90); player.SetCrouch(true); }
                if (previous == 1 && next != 1 && !State.timeJumped) JumpTime(true);
                if(previous!=0 && next==0)ApplyBagChange(true);
            }
        }
        public void Act(NightAction action)
        {
            if (Busy || State.stage == NightStage.Ended) return;
            if (State.stage == NightStage.Prologue)
            {
                if (action == NightAction.ReturnToStart && State.phonePlaced) StartCoroutine(CompletePrologue());
                else Say(State.phonePlaced ? "Quay ra đầu hẻm." : "Đừng để ai thấy.");
                return;
            }
            switch (action)
            {
                case NightAction.ParkBike:
                    if (State.bikeParked) { State.Note("Đồng hồ xe: " + State.odo_2100 + " km, lúc 21:00"); Say("Đã ghi số đồng hồ vào Sổ tay."); break; }
                    State.bikeParked = true;
                    State.Note("Đồng hồ xe: " + State.odo_2100 + " km, lúc 21:00");
                    Say(State.prologue_beaten > 0 ? "Đói quá. Trầy hồi nào vậy ta." : "Đói quá.");
                    Objective = "Nhặt chiếc máy đang sáng dưới chân xe đậu cạnh."; break;
                case NightAction.Phone:
                case NightAction.PlacePhone:
                    if (!State.bikeParked) { Say("Dựng xe và tắt máy trước."); break; }
                    if (State.hasNganPhone) OpenModal("leave-phone");
                    else
                    {
                        State.hasNganPhone = true;
                        if (!State.phoneEverPicked)
                        {
                            State.phoneEverPicked = true;
                            foreach (string line in Dialogue.batchA) Deliver("Mẹ · 20:56 · 4 phút trước: " + line);
                            State.stage = NightStage.Exploration;
                            Checkpoints.Save("n1.parking", State, parkingSpawn.position, 180);
                        }
                        Objective = "Đọc tin trên máy Ngân (1), khám phá hẻm hoặc lên phòng 7.";
                        Say("Máy không khóa. Có tin nhắn từ Mẹ."); player.SetPhone(1);
                    }
                    break;
                case NightAction.Shop: if (State.stage != NightStage.LateNight) { if(State.stage==NightStage.Exploration) Clock.Set("21:10"); OpenModal("shop"); } else Say("Tiệm đã đóng cửa."); break;
                case NightAction.Camera: State.saw_shop_camera = true; State.Note("Tiệm đầu hẻm có camera quay ra hẻm"); Say("Đã ghi camera vào Sổ tay."); break;
                case NightAction.Police: if (State.hasNganPhone) OpenModal("police"); else Say("Biển Công an phường ở phía đường lớn."); break;
                case NightAction.Gate:
                    if (State.stage == NightStage.Parking) { Say("Nhặt chiếc máy ở nhà xe trước."); break; }
                    State.helpedLien = true;
                    Say((State.prologue_beaten > 0 ? "Cô Liên: Hồi nãy tụi nó đập một thằng ăn trộm ngoài nhà xe, mà nó vùng chạy mất. " : "") + (State.prologue_beaten>=2 ? "Cô Liên chìa chiếc găng xám nhặt được: Cô cất trong buồng. " : "") + "Cô Liên nhắc căn cước. Bạn phụ dựng chậu rồi lên phòng 7.", 10);
                    Objective = "Đi cầu thang lên tầng 3, vào phòng 7."; break;
                case NightAction.Room: EnterRoom(); break;
                case NightAction.Deposit: State.found_deposit_slip = true; State.Note("Phòng 7, nhận cọc ngày 22/11, không ghi tên"); Say("Giấy nhận cọc không ghi tên."); break;
                case NightAction.Bag:
                    State.bagInspected = true;
                    string contents = State.prologue_beaten >= 2 ? "Áo mưa rách, chỉ còn găng phải." : State.prologue_beaten == 1 ? "Áo mưa rách một tay, đôi găng xám." : "Áo mưa xanh còn ướt, đôi găng xám.";
                    State.Note("Ba lô: sạc dự phòng, chai nước, khẩu trang. " + contents);
                    Say(State.bagMoved ? "Ủa... mình để nó trên nệm hồi nào ta. Ngăn nhỏ bị kẹt." : contents + (State.bagOpen ? " Ngăn chính mở hé." : "") + " Ngăn nhỏ bị kẹt."); break;
                case NightAction.MoveBag: State.bagDisplaced = true; bag.transform.position = bed.position + new Vector3(.9f,.3f,0); State.bagPosition=bag.transform.position;State.hasBagPosition=true; Say("Bạn dời ba lô sang cạnh nệm."); break;
                case NightAction.Wifi: State.Note("Mật khẩu Wi-Fi sau cửa phòng 7"); Say("Đã ghi Wi-Fi vào Sổ tay."); break;
                case NightAction.Mirror: Say("Gương mờ quá, soi không ra mặt."); break;
                case NightAction.Notice: State.Note("Ai để xe qua đêm tự khóa cổ, mất ráng chịu"); Say("Ai để xe qua đêm tự khóa cổ, mất ráng chịu."); break;
                case NightAction.Bed:
                    if (State.stage < NightStage.Room) { Say("Vào phòng 7 trước."); break; }
                    if (!State.timeJumped) { JumpTime(); break; }
                    if (!State.numberArrived) ArriveStranger();
                    if (State.stage != NightStage.LateNight) OpenModal("wait"); else OpenModal(State.clock == "03:12" ? "dawn" : "bed"); break;
                case NightAction.Charge: Say("Bạn cắm sạc máy Ngân."); break;
                case NightAction.LienPhone: State.asked_lien_keep = true; Say("Cô Liên: Cứ giữ, ai kiếm thì đưa."); break;
            }
            SyncWorld();
        }
        public void EnterRoom()
        {
            if (State.stage >= NightStage.Room) { Say("Phòng 7."); return; }
            if (!State.helpedLien) { Say("Phụ cô Liên dựng chậu trước khi lên phòng."); return; }
            State.stage = NightStage.Room; Clock.Set("21:20");
            bag.transform.position = bagAtDoor.position; State.bagPosition=bag.transform.position;State.hasBagPosition=true;
            Checkpoints.Save("n1.room7", State, roomSpawn.position, 90);
            Objective = "Xem đồ trong phòng. Đọc hoặc gửi tin trên máy Ngân.";
            Say("Bạn đặt ba lô xuống sàn cạnh cửa.");
            EmitInternal("area_entered", "n1.room7");
        }
        public void PlacePhone()
        {
            if (!CanPlace || !player.IsCrouched) return;
            State.phonePlaced = true; HoldProgress = 0;
            foreach (var o in observers) o.ResetObserver(true);
            Objective = "Quay ra đầu hẻm.";
            Say("Chiếc máy đã nằm dưới chân xe.");
            EmitInternal("phone_placed", "n1.parking");
        }
        public bool ExposedWhilePlacing()
        {
            var visible = observers.FirstOrDefault(o => o.CanSeePlayer());
            if (visible == null) return false;
            visible.Detect(true); return true;
        }
        IEnumerator CompletePrologue()
        {
            if (Busy) yield break;
            Busy = true; player.SetOverlay(true);
            // First-person glance at the security camera, without moving the body.
            player.eyes.transform.LookAt(new Vector3(-2.1f,2.8f,5));
            yield return new WaitForSeconds(2);
            BlackTitle = "3 GIỜ 12"; yield return new WaitForSeconds(1.5f);
            State.stage = NightStage.Parking; Clock.Set("21:00");
            player.Teleport(parkingSpawn.position,0);
            BlackTitle = ""; Busy = false; player.SetOverlay(false);
            player.RunMultiplier = 1;
            Checkpoints.Save("n1.parking", State, parkingSpawn.position,0);
            Objective = "Dựng xe, tắt máy và xem số đồng hồ.";

        }
        public void JumpTime(bool concealed = false)
        {
            if (State.timeJumped || State.stage != NightStage.Room) return;
            State.timeJumped = true; Clock.Set("21:26");
            State.bagChangePending = true;
            if(concealed || player.ActivePhone!=0)ApplyBagChange(true);
            // Closing a phone reveals changes between frames. Without a phone, conceal sitting down first.
            else if(InRoom && Modal!="")ApplyBagChange(true);
            else StartCoroutine(SitTransition());
            Deliver("Mẹ · 21:26: " + Dialogue.batchB);
            if (State.sent.Any(x => x.StartsWith("camera:"))) Deliver("Mẹ · 21:26: " + (State.linh_knows_shop_camera ? Dialogue.cameraYes : Dialogue.cameraNo));
            if (State.asked_mom_why_here) Deliver("Mẹ · 21:26: " + Dialogue.why);
            if (State.sent.Contains("call")) Deliver("Mẹ · 21:26: " + Dialogue.callReply);
            Deliver("Mẹ · 21:26: " + Dialogue.warning);
        }
        IEnumerator SitTransition()
        {
            Busy=true;player.SetOverlay(true);BlackTitle=" ";
            yield return new WaitForSeconds(.25f);
            player.Teleport(bed.position+new Vector3(0,.23f,-.4f),90);player.SetCrouch(true);
            ApplyBagChange(true);BlackTitle="";Busy=false;player.SetOverlay(false);
        }
        public void ApplyBagChange(bool concealed)
        {
            if(!State.bagChangePending || !concealed)return;
            if(State.bagDisplaced)State.bagOpen=true;
            else {bag.transform.position=bed.position+new Vector3(.5f,.3f,0);State.bagMoved=true;}
            State.bagPosition=bag.transform.position;State.hasBagPosition=true;State.bagChangePending=false;
        }
        public void Send(string id, string text)
        {
            if (!State.hasNganPhone || State.phone_powered_off || State.sent.Contains(id)) return;
            if (id.StartsWith("camera:") && State.sent.Any(x => x.StartsWith("camera:"))) return;
            if (id == "camera:yes" && !State.saw_shop_camera) return;
            if(State.timeJumped)State.afterJumpQuestions.Add(id);
            State.sent.Add(id); State.messages.Add("Bạn · " + State.clock + " · Đã gửi: " + text);
            if (id == "camera:yes") State.linh_knows_shop_camera = true;
            if (id == "why") State.asked_mom_why_here = true;
            EmitInternal("dialogue_choice", id);
            if (State.stage == NightStage.Room && InRoom && !State.timeJumped) JumpTime();
        }
        void Deliver(string line)
        {
            if (State.phone_powered_off) State.pendingMessages.Add(line); else State.messages.Add(line);
        }
        public void TogglePower()
        {
            State.phone_powered_off = !State.phone_powered_off;
            if (!State.phone_powered_off) { State.messages.AddRange(State.pendingMessages); State.pendingMessages.Clear(); }
        }
        public void ArriveStranger()
        {
            if (State.numberArrived) return;
            State.numberArrived = true; Clock.Set("21:35");
            Deliver("Số lạ · 21:35 · Đã đọc: " + Dialogue.stranger);
            Say("Có tin nhắn từ một dãy số lạ.");
            Objective = "Xem tin số lạ hoặc bỏ qua. Tới nệm để chờ khuya.";
            if (!State.hasNganPhone && State.phone_left_count == 1) RecallPhone();
        }
        public void ChooseNumber(string choice)
        {
            if (!State.numberArrived) return;
            if(choice!="blocked" && choice!="replied" && choice!="ignored")return;
            State.number_choice = choice; EmitChoice(choice=="blocked" ? "1a" : choice=="replied" ? "1b" : "1c");
            Say(choice == "replied" ? "Một người phụ nữ bắt máy. Bạn im lặng rồi tắt." : choice == "blocked" ? "Đã chặn số lạ." : "Bạn bỏ qua dãy số.");
        }
        public void LeavePhone()
        {
            if (!State.hasNganPhone) return;
            State.hasNganPhone = false; State.phone_left_count++; EmitChoice("0b");
            player.SetPhone(0); CloseModal(); Say("Bạn để máy lại chỗ cũ.");
            if (State.phone_left_count == 1 && State.timeJumped) RecallPhone();
        }
        void RecallPhone()
        {
            if(State.lienCalled)return;
            State.lienCalled=true;Clock.Set("21:40");
            Say("Cô Liên gọi: Xuống lấy máy đi, nó reo hoài!",8);EmitInternal("lien_called","0b");
        }
        public void WaitUntilLate()
        {
            CloseModal(); State.stage = NightStage.LateNight; Clock.Set("22:30");
            Checkpoints.Save("n1.room7.late", State, roomSpawn.position,90);
            Objective = "Tới nệm: đi ngủ hoặc thức canh tới sáng.";
        }
        public void Rest(bool sleep) { CloseModal(); StartCoroutine(RestSequence(sleep)); }
        IEnumerator RestSequence(bool sleep)
        {
            Busy = true; player.SetPhone(0); player.SetOverlay(true);
            State.night1_slept = sleep; EmitInternal("sleep_choice", sleep ? "sleep" : "stay_awake");
            if (sleep)
            {
                BlackTitle = ""; BlackTitle = "Đêm xuống"; Clock.Set("03:40",1); State.batchD = true;
                if(State.afterJumpQuestions.Count==0)Deliver("Mẹ · 03:40: " + Dialogue.batchD);
                foreach(string question in State.afterJumpQuestions)
                    Deliver("Mẹ · 03:40: "+(question=="why"?Dialogue.why:question=="call"?Dialogue.callReply:question=="camera:yes"?Dialogue.cameraYes:question.StartsWith("camera:")?Dialogue.cameraNo:Dialogue.batchD)); yield return new WaitForSeconds(2);
            }
            else
            {
                foreach (string t in new[] { "23:30", "00:30", "01:30", "02:30" })
                { Clock.Set(t,t.StartsWith("23:")?0:1); Say(t == "23:30" ? "Tiếng xe máy ngang hẻm." : t == "00:30" ? "Đèn hành lang bật rồi tắt." : "Quạt kêu trong phòng."); yield return new WaitForSeconds(2); }
                Clock.Set("03:12",1); State.alarm = true; if (State.hasNganPhone && !State.phone_powered_off) sound.Play();
                Busy = false; player.SetOverlay(false); OpenModal("alarm"); Objective = "Tắt báo thức trên máy Ngân, kiểm tra cửa rồi trở lại nệm.";
                yield break;
            }
            FinishNight();
        }
        public void StopAlarm()
        {
            State.alarm = false; sound.Stop(); player.SetPhone(0);
            Objective = "Kiểm tra hành lang nếu muốn. Trở lại nệm để chờ sáng.";
            OpenModal("dawn");
        }
        public void FinishNight()
        {
            CloseModal(); Busy = false;
            if (!State.hasNganPhone && State.phone_left_count >= 2) { End("bystander"); return; }
            State.alarm=false;State.stage = NightStage.Ended; State.ending = "night1_completed"; Clock.Set("06:29",1);
            BlackTitle = "HẾT ĐÊM 1"; player.SetPhone(0); player.SetOverlay(true); sound.Stop();
            TelemetryProduced?.Invoke(new NightCompletedTelemetry(isReplay,Mathf.Max(0,Mathf.FloorToInt(Time.time-nightStartedAt))));
            completedInSession=true;
        }
        public void End(string ending)
        {
            CloseModal(); State.alarm=false; State.stage = NightStage.Ended; State.ending = ending;
            BlackTitle = "NGƯỜI QUA ĐƯỜNG"; player.SetPhone(0); player.SetOverlay(true); sound.Stop();
            TelemetryProduced?.Invoke(new EndingTelemetry(ending));
        }
        public void Shout(NightObserver source)
        {
            shoutCount++;
            observers.Where(x=>x.startsDormant).ElementAtOrDefault(shoutCount-1)?.Activate();
            if (shoutCount <= alertLights.Length) alertLights[shoutCount-1].enabled = true;
            foreach (var o in observers) if (o != source && Vector3.Distance(o.transform.position,source.transform.position) <= 15) o.Hear(source.transform.position, true);
            Say("Trộm!",2);
        }
        public void Caught()
        {
            if (Busy || Time.time < immuneUntil || State.phonePlaced || State.stage != NightStage.Prologue) return;
            TelemetryProduced?.Invoke(new CaughtTelemetry(InParking?"n1.parking":"n1.alley","n1.alley_start",player.transform.position));
            StartCoroutine(CaptureSequence());
        }
        IEnumerator CaptureSequence()
        {
            Busy = true; player.SetOverlay(true); BlackTitle = State.prologue_beaten == 0 ? "Tiếng xô đẩy. Áo mưa rách." : State.prologue_beaten == 1 ? "Đánh chết nó đi!" : "Giữ nó lại! Gọi công an đi!";
            yield return new WaitForSeconds(1.5f);
            State.prologue_beaten++;
            if (State.prologue_beaten >= 3) ResetPrologue();
            else
            {
                var nearest = hidingPoints.OrderBy(x => Vector3.Distance(x.position, player.transform.position)).First();
                player.Teleport(nearest.position,0); player.SetCrouch(true);
                foreach (var o in observers) o.ResetObserver(false);
                foreach (var l in alertLights.Take(State.prologue_beaten+1)) l.enabled = true;
                if(parkingAlertLight!=null)parkingAlertLight.enabled=true;
                player.RunMultiplier = State.prologue_beaten >= 2 ? 2f/3 : 1;
            }
            BlackTitle = ""; Busy = false; player.SetOverlay(false); immuneUntil = Time.time + 5;
        }
        public void ResetPrologue()
        {
            State = new NightOneState(); Clock = new StoryClock(State);
            player.Teleport(alleyStart.position); player.RunMultiplier = 1;
            foreach (var o in observers) o.ResetObserver(false);
            foreach (var l in alertLights) l.enabled = false;
            leftStart = false; shoutCount = 0; unseenSeconds = 0;PrologueDeparted=false;PrologueElapsed=0;
            if(parkingAlertLight!=null)parkingAlertLight.enabled=false;
            Objective = "Tới nhà xe cuối hẻm. Cúi và đặt chiếc máy dưới chân xe.";
        }
        public void RestoreCheckpoint()
        {
            if (Busy) return;
            if(State.stage==NightStage.Prologue)
            {
                // A safety recovery must never clear persistent capture penalties.
                player.Teleport(alleyStart.position);player.RunMultiplier=State.prologue_beaten>=2?2f/3:1;
                foreach(var observer in observers)observer.ResetObserver(State.phonePlaced);
                foreach(var light in alertLights)light.enabled=false;
                foreach(var light in alertLights.Take(State.prologue_beaten>0?State.prologue_beaten+1:0))light.enabled=true;
                if(parkingAlertLight!=null)parkingAlertLight.enabled=State.prologue_beaten>0;
                leftStart=false;immuneUntil=Time.time+3;return;
            }
            bool replayFromEnd=State.stage==NightStage.Ended;
            CloseModal(); player.SetPhone(0); sound.Stop();
            if (!Checkpoints.Restore(out var state, out var point, out float yaw)) return;
            State = state; Clock = new StoryClock(State); player.Teleport(point,yaw);
            if(replayFromEnd) {isReplay=completedInSession;nightStartedAt=Time.time;TelemetryProduced?.Invoke(new NightStartedTelemetry(isReplay));}
            player.RunMultiplier = State.stage == NightStage.Prologue && State.prologue_beaten >= 2 ? 2f/3 : 1;
            BlackTitle = ""; roomSeconds = 0;
            foreach (var o in observers) o.ResetObserver(State.phonePlaced);
            foreach (var l in alertLights) l.enabled = false;
            Objective = State.stage >= NightStage.Room ? "Khám phá phòng 7 rồi tới nệm." : "Khám phá hẻm, đọc tin và lên phòng 7.";
            bag.transform.position = State.hasBagPosition ? State.bagPosition : bagAtDoor.position;
            SyncWorld();
        }
        void SyncWorld()
        {
            if (nganPhone != null) nganPhone.SetActive(State.phonePlaced && !State.hasNganPhone);
            if (bag != null) bag.SetActive(State.stage >= NightStage.Room);
            if(greyGlove!=null)greyGlove.SetActive(State.helpedLien && State.prologue_beaten>=2);
            if(State.alarm && State.stage!=NightStage.Ended && State.hasNganPhone && !State.phone_powered_off) {if(!sound.isPlaying)sound.Play();}
            else if(sound.isPlaying)sound.Stop();
        }
    }
}
