using System;
using System.Collections;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Stealth;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightOneDirector : MonoBehaviour
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
        public float LockScreenUntil { get; private set; }
        public bool OdometerShown { get; private set; }
        public const int OilChangeKm = 13000;
        public static string Km(int km) => (km / 1000) + "." + (km % 1000).ToString("000");
        string OdometerNote => "Đồng hồ xe: " + Km(State.odo_2100) + " km, lúc 21:00";
        bool attentionHintShown, runHintShown, washerLineShown;
        const string AttentionHint = "Người này đang bắt đầu để ý tới bạn, và khi thanh trên đầu họ đầy thì bạn sẽ bị phát hiện. Bạn hãy lùi vào chỗ tối hoặc bấm C để cúi người xuống cho tới khi thanh vơi đi.";
        const string RunHint = "Tiếng chân chạy làm những người ở gần nghe thấy và quay lại nhìn, nên khi đi ngang qua ai đó bạn nên bước chậm lại.";
        const string WasherLine = "Người phụ nữ rửa chén (nói vọng vào nhà): Ba thằng Bi ơi, lát ra khóa cổ xe giùm em nghen. Dạo này trong hẻm mất đồ hoài, bữa trước có đứa vô nhà xe hút xăng đó.";
        public string Subtitle { get; private set; }
        public string Modal { get; private set; } = "";
        public string BlackTitle { get; private set; } = "";
        public float HoldProgress { get; set; }
        public bool Busy { get; private set; }
        public Bounds parkingZone = new Bounds(new Vector3(0, -499, 527), new Vector3(2000, 1002, 1000));
        public Bounds roomZone = new Bounds(new Vector3(512, 507, 0), new Vector3(1000, 1000, 2000));
        public Transform securityCamera;
        public GameObject ownBike, mouthBike, pushBike;
        public float Fade { get; private set; }
        public float RedFlash { get; private set; }
        public float Daze { get; private set; }
        public bool Pushing => State.stage == NightStage.Parking && !State.bikeArrived && pushBike != null;
        float stillSeconds;
        Vector3 lastPlayerPoint;
        AudioSource sfx, ringSource;
        public GameObject prologueGate;
        public Transform fallenPot;
        public string[] Conversation { get; private set; } = new string[0];
        int conversationId;
        bool potCached, potApplied, potShownDown;
        Quaternion potUpright;
        Vector3 potHome;
        float potGround;
        public bool InParking => parkingZone.Contains(player.transform.position);
        public bool InRoom => roomZone.Contains(player.transform.position);
        float AlleyProgress => Vector3.Dot(player.transform.position - alleyStart.position, Vector3.ProjectOnPlane(alleyStart.forward, Vector3.up).normalized);
        float StartYaw => alleyStart.eulerAngles.y;
        Vector3 FromBed(Vector3 offset) => bed.position + Quaternion.Euler(0, bed.eulerAngles.y, 0) * offset;
        float BedYaw => bed.eulerAngles.y + 90;
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
        float subtitleUntil, roomSeconds, immuneUntil;
        float hidingSeconds;
        Vector3 hideAnchor;
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
            sfx = gameObject.AddComponent<AudioSource>(); sfx.volume = .7f;
            ringSource = gameObject.AddComponent<AudioSource>(); ringSource.volume = .5f;
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
            Checkpoints.Save("n1.alley_start", State, alleyStart.position, StartYaw);
            Objective = "Bạn cần mang chiếc điện thoại đang cầm trên tay vào nhà xe ở cuối hẻm, rồi cúi xuống đặt nó trên nền xi măng, ngay dưới chân chiếc Wave màu đỏ.";
            Say("Trong hẻm vẫn còn người ngồi trước cửa nhà, và bạn không được để bất kỳ ai nhìn thấy mình trên đường vào nhà xe.", 9);
            SyncWorld();
            gameObject.AddComponent<BaGio12.Persistence.NightPersistence>().story = this;
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
            var flat = Vector3.ProjectOnPlane(player.transform.position - lastPlayerPoint, Vector3.up);
            stillSeconds = flat.magnitude < Time.deltaTime * .3f ? stillSeconds + Time.deltaTime : 0;
            lastPlayerPoint = player.transform.position;
            if (!Busy) { RedFlash = Mathf.Max(0, RedFlash - Time.deltaTime * 2); Daze = Mathf.Max(0, Daze - Time.deltaTime / 7); }
            if (Pushing && !Busy && InParking) StartCoroutine(ArriveWithBike());
            if (State.stage == NightStage.Prologue && !Busy)
            {
                if (!PrologueDeparted && AlleyProgress > 1.4f) PrologueDeparted=true;
                if(PrologueDeparted)PrologueElapsed+=Time.deltaTime;
                if (AlleyProgress > 3.4f) leftStart = true;
                bool seen = observers.Any(x => x.CanSeePlayer());
                // Remaining unseen while moving is not eight seconds of staying in cover.
                if (!Hidden || seen || Vector3.Distance(hideAnchor, player.transform.position) > .05f)
                { hidingSeconds = 0; hideAnchor = player.transform.position; }
                else hidingSeconds += Time.deltaTime;
                if (hidingSeconds >= 8) foreach (var o in observers) o.LoseTrail();
                if (player.IsRunning)
                {
                    var listener = observers.Where(o => o.CanHear && Vector3.Distance(o.transform.position, player.transform.position) <= 5)
                        .OrderBy(o => Vector3.Distance(o.transform.position, player.transform.position)).FirstOrDefault();
                    if (listener != null)
                    {
                        listener.Hear(player.transform.position, false);
                        if (!runHintShown) { runHintShown = true; Say(RunHint, 9); }
                    }
                }
                if (!washerLineShown)
                {
                    var washer = observers.FirstOrDefault(o => o.name.StartsWith("Người phụ nữ"));
                    if (washer != null && Vector3.Distance(washer.transform.position, player.transform.position) < 7) { washerLineShown = true; Say(WasherLine, 10); }
                }
                if (!attentionHintShown && observers.Any(o => o.Mode == ObserverMode.Normal && o.Attention > .15f)) { attentionHintShown = true; Say(AttentionHint, 10); }
                if ((State.prologue_beaten >= 2 || player.Winded) && Hidden)
                    foreach (var observer in observers.Where(o => o.CanHearBreathing && Vector3.Distance(o.transform.position, player.transform.position) <= 3))
                        observer.HearBreathing(player.transform.position);
                if (leftStart && State.phonePlaced && AlleyProgress < 2.4f && !observers.Any(o => o.Mode == ObserverMode.Chasing)) StartCoroutine(CompletePrologue());
                else if (leftStart && AlleyProgress < -.4f && !State.phonePlaced) ResetPrologue();
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
        public void Speak(string[] lines)
        {
            Conversation = lines; conversationId++;
            if (lines.Length == 0) return;
            if (isActiveAndEnabled) StartCoroutine(SpeakLines(lines, conversationId)); else Say(lines[0]);
        }
        static float LineSeconds(string line) => Mathf.Clamp(2.5f + line.Length * .05f, 3, 7);
        IEnumerator SpeakLines(string[] lines, int id)
        {
            foreach (var line in lines)
            {
                if (id != conversationId) yield break;
                float seconds = LineSeconds(line);
                Say(line, seconds + 1);
                for (float t = 0; t < seconds; t += player.IsPaused ? 0 : Time.deltaTime) { yield return null; if (id != conversationId || Subtitle != line) yield break; }
            }
        }
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
            if (restoringSave) return;
            if (State.stage == NightStage.Room && InRoom)
            {
                if (next != 0) { player.Teleport(FromBed(new Vector3(0,.23f,-.4f)),BedYaw); player.SetCrouch(true); }
                if (previous == 1 && next != 1 && !State.timeJumped) JumpTime(true);
                if(previous!=0 && next==0)ApplyBagChange(true);
            }
        }
        public void Act(NightAction action)
        {
            if (Busy || State.stage == NightStage.Ended) return;
            if (State.stage == NightStage.Prologue)
            {
                if (action == NightAction.ReturnToStart && State.phonePlaced && !observers.Any(o => o.Mode == ObserverMode.Chasing)) StartCoroutine(CompletePrologue());
                else if (State.phonePlaced) Say("Vẫn còn người đang đuổi theo, bạn cần cắt đuôi họ trước khi quay ra đầu hẻm.");
                return;
            }
            if (Pushing) { Say("Bạn cần dắt chiếc xe máy vào nhà xe ở cuối hẻm trước đã."); return; }
            switch (action)
            {
                case NightAction.ParkBike:
                    if (State.bikeParked) { State.Note(OdometerNote); Say("Số đồng hồ xe đã nằm trong Sổ tay, bạn có thể bấm N để xem lại bất cứ lúc nào."); break; }
                    odometerRoutine = StartCoroutine(ReadOdometer()); break;
                case NightAction.Phone:
                case NightAction.PlacePhone:
                    EndOdometer();
                    if (!State.bikeParked) { Say("Bạn cần dựng xe vào chỗ và cúi xuống xem đồng hồ xe trước đã."); break; }
                    if (State.hasNganPhone) OpenModal("leave-phone");
                    else
                    {
                        State.hasNganPhone = true;
                        if (!State.phoneEverPicked)
                        {
                            State.phoneEverPicked = true;
                            foreach (string line in Dialogue.batchA) Deliver("Mẹ · 20:57 · 4 phút trước: " + line);
                            State.stage = NightStage.Exploration;
                            Checkpoints.Save("n1.parking", State, parkingSpawn.position, parkingSpawn.eulerAngles.y + 180);
                        }
                        Objective = "Bạn hãy đọc tin nhắn trên máy của Ngân bằng phím 1, rồi đi xem quanh hẻm hoặc lên phòng 7 ở tầng ba nhà trọ cô Liên.";
                        Say("Sinh viên (nghĩ): Không cài mật khẩu luôn hả trời."); player.SetPhone(1);
                    }
                    break;
                case NightAction.Shop: if (State.stage != NightStage.LateNight) { if(State.stage==NightStage.Exploration) Clock.Set("21:10"); OpenModal("shop"); } else Say("Tiệm tạp hóa của chú Hùng đã đóng cửa rồi."); break;
                case NightAction.Camera: State.saw_shop_camera = true; State.Note("Tiệm đầu hẻm có camera quay ra hẻm"); Say("Bạn đã ghi chiếc camera của tiệm đầu hẻm vào Sổ tay."); break;
                case NightAction.Police: if (State.hasNganPhone) OpenModal("police"); else Say("Biển Công an phường nằm ở phía đường lớn, ngay ngoài đầu hẻm."); break;
                case NightAction.Gate:
                    if (State.stage == NightStage.Parking) { Say("Bạn cần xem chiếc điện thoại đang sáng ở nhà xe trước khi vào nhà trọ."); break; }
                    if (State.helpedLien) { Say("Cô Liên: Mưa gì mà mưa hoài."); break; }
                    State.helpedLien = true;
                    var lines = new System.Collections.Generic.List<string> { "Cô Liên: Phòng 7 hả con? Ừ, đó, để cái đó sát vô tường giùm cô... Mưa gì mà mưa hoài." };
                    if (State.prologue_beaten > 0) lines.AddRange(new[] { "Cô Liên: Hồi nãy tụi nó đập một thằng ăn trộm ngoài nhà xe, mà nó vùng chạy mất. Con coi xe con có sao không.", "Sinh viên: Dạ... chắc không sao cô." });
                    if (State.prologue_beaten >= 2) lines.AddRange(new[] { "Cô Liên chìa ra chiếc găng xám: Nó rớt cái này nè. Cô cất trong buồng, ai mất thì kiếm cô.", "Sinh viên: Dạ." });
                    lines.AddRange(new[] { "Cô Liên: À, cái căn cước. Hôm trước con nói mai đưa. Mai của con là mai nào?", "Sinh viên: Dạ, cái bóp con rớt hôm té xe, mất luôn căn cước. Con đang làm lại.", "Cô Liên: Làm lẹ nghen con. Phường người ta xuống hỏi mà cô không có gì đưa là cô bị phạt đó." });
                    Speak(lines.ToArray());
                    Objective = "Bạn hãy đi cầu thang lên tầng ba và vào phòng 7, phòng trọ của bạn."; break;
                case NightAction.Room: EnterRoom(); break;
                case NightAction.Deposit: State.found_deposit_slip = true; State.Note("Phòng 7, nhận cọc ngày 22/11, không ghi tên"); Say("Tờ giấy nhận cọc phòng 7 không ghi tên người thuê, và bạn đã ghi nó vào Sổ tay."); break;
                case NightAction.Bag:
                    State.bagInspected = true;
                    string contents = State.prologue_beaten >= 2 ? "Áo mưa rách, chỉ còn găng phải." : State.prologue_beaten == 1 ? "Áo mưa rách một tay, đôi găng xám." : "Áo mưa xanh còn ướt, đôi găng xám.";
                    State.Note("Ba lô: sạc dự phòng, chai nước, khẩu trang. " + contents);
                    Say(State.bagMoved ? "Ủa... mình để nó trên nệm hồi nào ta. Ngăn nhỏ bị kẹt." : contents + (State.bagOpen ? " Ngăn chính mở hé." : "") + " Ngăn nhỏ bị kẹt."); break;
                case NightAction.MoveBag: State.bagDisplaced = true; bag.transform.position = FromBed(new Vector3(.9f,.3f,0)); State.bagPosition=bag.transform.position;State.hasBagPosition=true; Say("Bạn dời ba lô sang cạnh nệm."); break;
                case NightAction.Wifi: State.Note("Mật khẩu Wi-Fi sau cửa phòng 7"); Say("Bạn đã ghi mật khẩu Wi-Fi dán sau cửa phòng 7 vào Sổ tay."); break;
                case NightAction.Mirror: Say("Sinh viên (nghĩ): Gương gì mờ căm, soi không ra cái mặt."); break;
                case NightAction.Notice: State.Note("Ai để xe qua đêm tự khóa cổ, mất ráng chịu"); Say("Tờ giấy dán trên cột ghi: Ai để xe qua đêm tự khóa cổ, mất ráng chịu."); break;
                case NightAction.Bed:
                    if (State.stage < NightStage.Room) { Say("Bạn cần vào phòng 7 trước đã."); break; }
                    if (!State.timeJumped) { JumpTime(); break; }
                    if (!State.numberArrived) ArriveStranger();
                    if (State.stage != NightStage.LateNight) OpenModal("wait"); else OpenModal(State.clock == "03:12" ? "dawn" : "bed"); break;
                case NightAction.Charge: Say("Bạn cắm sạc máy Ngân."); break;
                case NightAction.LienPhone:
                    State.asked_lien_keep = true;
                    if (State.hasNganPhone) Speak(new[] { "Sinh viên: Cô ơi, con lượm được cái điện thoại dưới nhà xe, cô giữ giùm được không cô?", "Cô Liên: Điện thoại ai? ... Thôi thôi, cô không giữ đâu. Bữa trước có đứa bỏ quên cái sạc dưới nhà xe, kiếm không ra là nói cô lấy. Con giữ đi, ai kiếm thì đưa." });
                    else Say("Cô Liên: Mưa gì mà mưa hoài."); break;
            }
            SyncWorld();
        }
        public void EnterRoom()
        {
            if (State.stage >= NightStage.Room) { Say("Bạn đang ở trong phòng 7 rồi."); return; }
            if (!State.helpedLien) { Say("Cô Liên đang loay hoay với chậu cây ở cổng, bạn hãy phụ cô dựng lại trước khi lên phòng."); return; }
            State.stage = NightStage.Room; Clock.Set("21:20");
            bag.transform.position = bagAtDoor.position; State.bagPosition=bag.transform.position;State.hasBagPosition=true;
            Checkpoints.Save("n1.room7", State, roomSpawn.position, roomSpawn.eulerAngles.y + 90);
            Objective = "Bạn hãy xem qua đồ đạc trong phòng, rồi đọc hoặc trả lời tin nhắn trên máy của Ngân.";
            Speak(new[] { "Bạn đặt ba lô xuống sàn cạnh cửa.", "Sinh viên (nghĩ): Trống trơn. Mai mua cái kệ với cái đèn bàn, chứ ở vầy buồn chết." });
            EmitInternal("area_entered", "n1.room7");
        }
        public void PlacePhone()
        {
            if (!CanPlace || !player.IsCrouched) return;
            State.phonePlaced = true; HoldProgress = 0;
            Objective = "Chiếc điện thoại đã nằm dưới chân chiếc Wave, giờ bạn hãy quay ra đầu hẻm mà không để ai thấy.";
            LockScreenUntil = Time.time + 2.6f;
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
            Busy = true; player.SetOverlay(true); HoldProgress = 0;
            // First-person glance at the security camera, without moving the body.
            var lens = securityCamera != null ? securityCamera.position : new Vector3(-2.1f,2.8f,5);
            for (float t = 0; t < 2; t += Time.deltaTime) { player.AimAt(lens, Mathf.Clamp01(Time.deltaTime * 5)); yield return null; }
            float master = AudioListener.volume;
            if (mouthBike != null)
            {
                var parts = mouthBike.GetComponentsInChildren<Renderer>();
                Vector3 seat = mouthBike.transform.position + Vector3.up * .8f;
                if (parts.Length > 0) { var b = parts[0].bounds; foreach (var r in parts) b.Encapsulate(r.bounds); seat = b.center; }
                for (float t = 0; t < 1.4f; t += Time.deltaTime) { player.AimAt(seat, Mathf.Clamp01(Time.deltaTime * 3)); yield return null; }
                // The body stops; the world drains away before the cut.
                for (float t = 0; t < 3.2f; t += Time.deltaTime)
                {
                    AudioListener.volume = master * (1 - Mathf.SmoothStep(0, 1, t / 3.2f));
                    Fade = Mathf.SmoothStep(0, 1, (t - 1.4f) / 1.8f);
                    yield return null;
                }
                Fade = 1; yield return new WaitForSeconds(1);
            }
            BlackTitle = "3 GIỜ 12"; yield return new WaitForSeconds(2f);
            State.stage = NightStage.Parking; Clock.Set("21:00");
            player.RunMultiplier = 1; player.RestoreStamina(); Daze = 0; RedFlash = 0;
            if (pushBike != null)
            {
                BlackTitle = "21:00"; yield return new WaitForSeconds(1.6f);
                State.bikeArrived = false;
                player.Teleport(alleyStart.position, StartYaw);
                Checkpoints.Save("n1.arrival", State, alleyStart.position, StartYaw);
                Objective = "Bạn hãy dắt chiếc xe máy vào nhà xe ở cuối hẻm và dựng nó vào chỗ trống cạnh chiếc Wave màu đỏ.";
                Say("Sinh viên (nghĩ): Dắt vô nhà xe rồi còn leo ba tầng nữa mới tới phòng. Mệt quá trời.", 6);
            }
            else
            {
                State.bikeArrived = true;
                player.Teleport(parkingSpawn.position,parkingSpawn.eulerAngles.y);
                Checkpoints.Save("n1.parking", State, parkingSpawn.position,parkingSpawn.eulerAngles.y);
                Objective = "Bạn hãy dựng xe vào chỗ trống cạnh chiếc Wave màu đỏ, rồi cúi xuống xem đồng hồ xe.";
            }
            SyncWorld();
            BlackTitle = ""; AudioListener.volume = master;
            for (float t = 0; t < 1.2f; t += Time.deltaTime) { Fade = 1 - t / 1.2f; yield return null; }
            Fade = 0; Busy = false; player.SetOverlay(false);
        }
        Coroutine odometerRoutine;
        void EndOdometer()
        {
            if (odometerRoutine == null) return;
            StopCoroutine(odometerRoutine); odometerRoutine = null;
            OdometerShown = false; player.SetCrouch(false); player.SetOverlay(false);
        }
        IEnumerator ReadOdometer()
        {
            State.bikeParked = true; State.Note(OdometerNote);
            player.SetOverlay(true); player.SetCrouch(true);
            Vector3 gauge = ownBike != null ? ownBike.transform.position + Vector3.up * .95f : player.eyes.transform.position + player.transform.forward * .8f + Vector3.down * .5f;
            for (float t = 0; t < .6f; t += Time.deltaTime) { player.AimAt(gauge, Mathf.Clamp01(Time.deltaTime * 8)); yield return null; }
            OdometerShown = true;
            Say("Sinh viên (nghĩ): Còn chừng " + ((OilChangeKm - State.odo_2100) / 100 * 100) + " cây nữa là tới hạn thay nhớt.", 4);
            for (float t = 0; t < 3f; t += Time.deltaTime) { player.AimAt(gauge, Mathf.Clamp01(Time.deltaTime * 8)); yield return null; }
            OdometerShown = false; player.SetCrouch(false); player.SetOverlay(false); odometerRoutine = null;
            Say("Số đồng hồ xe vừa được ghi vào Sổ tay, bạn có thể bấm N để xem lại bất cứ lúc nào.", 6);
            yield return new WaitForSeconds(6);
            if (State.hasNganPhone) yield break;
            Say(State.prologue_beaten > 0 ? "Sinh viên (nghĩ): Đói quá. Trầy hồi nào vậy ta." : "Sinh viên (nghĩ): Đói quá. Lát ra đầu hẻm mua gói mì.", 5);
            yield return new WaitForSeconds(5);
            if (State.hasNganPhone) yield break;
            Say("Màn hình một chiếc điện thoại dưới chân chiếc Wave vừa sáng lên với dòng chữ: Pin yếu, còn 15%.", 7);
            Objective = "Dưới chân chiếc Wave màu đỏ có một chiếc điện thoại vừa sáng màn hình, bạn hãy lại gần xem đó là máy của ai.";
        }
        IEnumerator ArriveWithBike()
        {
            Busy = true; player.SetOverlay(true);
            for (float t = 0; t < .35f; t += Time.deltaTime) { Fade = t / .35f; yield return null; }
            State.bikeArrived = true; SyncWorld();
            Checkpoints.Save("n1.parking", State, parkingSpawn.position, parkingSpawn.eulerAngles.y);
            Objective = "Bạn hãy dựng xe vào chỗ trống cạnh chiếc Wave màu đỏ, rồi cúi xuống xem đồng hồ xe.";
            for (float t = 0; t < .5f; t += Time.deltaTime) { Fade = 1 - t / .5f; yield return null; }
            Fade = 0; Busy = false; player.SetOverlay(false);
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
            player.Teleport(FromBed(new Vector3(0,.23f,-.4f)),BedYaw);player.SetCrouch(true);
            ApplyBagChange(true);BlackTitle="";Busy=false;player.SetOverlay(false);
        }
        public void ApplyBagChange(bool concealed)
        {
            if(!State.bagChangePending || !concealed)return;
            if(State.bagDisplaced)State.bagOpen=true;
            else {bag.transform.position=FromBed(new Vector3(.5f,.3f,0));State.bagMoved=true;}
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
            Say("Một dãy số lạ không lưu tên vừa nhắn tin vào máy của Ngân.");
            Objective = "Bạn có thể đọc tin của dãy số lạ hoặc bỏ qua, rồi tới nệm để chờ tới khuya.";
            if (!State.hasNganPhone && State.phone_left_count == 1) RecallPhone();
        }
        public void ChooseNumber(string choice)
        {
            if (!State.numberArrived || State.number_chosen) return;
            if(choice!="blocked" && choice!="replied" && choice!="ignored")return;
            State.number_choice = choice; State.number_chosen = true; EmitChoice(choice=="blocked" ? "1a" : choice=="replied" ? "1b" : "1c");
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
            Checkpoints.Save("n1.room7.late", State, roomSpawn.position,roomSpawn.eulerAngles.y + 90);
            Objective = "Bạn hãy tới nệm, rồi chọn đi ngủ hoặc thức canh tới sáng.";
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
            Objective = "Bạn có thể ra kiểm tra hành lang nếu muốn, rồi trở lại nệm để chờ trời sáng.";
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
            foreach (var o in observers.Where(x => x.flashlight && x != source)) o.GuardExit(ExitGuardPoint);
            Say(source.child ? "Mẹ ơi! Có ăn trộm!" : source.stationary ? "Đâu? Nó chạy đâu rồi?" : "Trộm! Trộm xe bà con ơi!", 2.5f);
        }
        public void Suspect(NightObserver source, bool call)
        {
            string line = source.child ? "Mẹ ơi, có người lạ nè!" : source.displayName == "Chú Hùng" ? "Ê, ai đứng đó vậy?"
                : source.displayName == "Cô Liên" ? "Ai đó? Kiếm ai mà đứng trong nhà xe tui vậy?" : "Ai vậy? Kiếm nhà ai hả?";
            Say(line, 3);
            if (call) foreach (var o in observers) if (o != source && Vector3.Distance(o.transform.position,source.transform.position) <= 15) o.Hear(source.transform.position, true);
        }
        Vector3 ExitGuardPoint => alleyStart.position + Vector3.ProjectOnPlane(alleyStart.forward, Vector3.up).normalized * 1.6f;
        public float Conspicuousness(NightObserver observer)
        {
            float dim = Dark ? .6f : 1;
            if (InParking) return (player.IsRunning ? 1.6f : player.IsCrouched ? 1.1f : .55f) * dim;
            float distance = Vector3.Distance(observer.transform.position, player.transform.position);
            float factor = player.IsRunning ? 1.3f : player.IsCrouched ? .7f : Hidden ? .8f
                : stillSeconds > 2.5f && distance < 6 ? .5f : State.prologue_beaten > 0 ? .3f : 0;
            return factor * dim;
        }
        public void Caught()
        {
            if (Busy || Time.time < immuneUntil || State.stage != NightStage.Prologue) return;
            StartCoroutine(CaptureSequence());
        }
        static readonly string[][] CaptureBeats =
        {
            new[] { "Bắt được rồi! Nó đây nè!", "Áo mưa rách toạc." },
            new[] { "Đánh chết nó đi!", "Chiếc găng tuột khỏi tay." },
            new[] { "Giữ nó lại! Gọi công an đi!", "Không vùng ra được." },
        };
        IEnumerator CaptureSequence()
        {
            Busy = true; player.SetOverlay(true); HoldProgress = 0;
            Vector3 caughtAt = player.transform.position;
            TelemetryProduced?.Invoke(new CaughtTelemetry(InParking?"n1.parking":"n1.alley","n1.alley_start",caughtAt));
            var hunters = observers.Where(o => o.ActiveThreat).ToArray();
            var grabber = hunters.OrderBy(o => Vector3.Distance(o.transform.position, caughtAt)).FirstOrDefault();
            sfx.PlayOneShot(NightSfx.Grab); RedFlash = 1;
            for (float t = 0; t < .55f; t += Time.deltaTime)
            {
                Vector3 face = grabber != null ? grabber.transform.position + Vector3.up * 1.5f : player.eyes.transform.position + player.transform.forward;
                player.AimAt(face + UnityEngine.Random.insideUnitSphere * .25f * (1 - t / .55f), Mathf.Clamp01(Time.deltaTime * 16));
                yield return null;
            }
            player.SetCrouch(true); sfx.PlayOneShot(NightSfx.Scuffle);
            for (float t = 0; t < .5f; t += Time.deltaTime)
            {
                player.AimAt(player.transform.position + player.transform.forward * .5f + UnityEngine.Random.insideUnitSphere * .2f, Mathf.Clamp01(Time.deltaTime * 9));
                Fade = t / .5f; yield return null;
            }
            Fade = 1;
            int level = Mathf.Clamp(State.prologue_beaten, 0, 2);
            var beats = CaptureBeats[level];
            int hits = 2 + level * 2;
            BlackTitle = beats[0];
            for (int i = 0; i < hits; i++) { sfx.PlayOneShot(NightSfx.Thump, .7f + .15f * level); yield return new WaitForSeconds(UnityEngine.Random.Range(.28f, .5f)); }
            BlackTitle = beats[1];
            sfx.PlayOneShot(level == 0 ? NightSfx.Rip : NightSfx.Scuffle);
            yield return new WaitForSeconds(level == 2 ? 2.6f : 1.6f);
            State.prologue_beaten++;
            if (State.prologue_beaten >= 3)
            {
                BlackTitle = ""; ResetPrologue(); Fade = 0; RedFlash = 0; Daze = 0;
                Busy = false; player.SetOverlay(false); immuneUntil = Time.time + 5; yield break;
            }
            var nearest = hidingPoints.OrderBy(x => Vector3.Distance(x.position, caughtAt)).First();
            player.Teleport(nearest.position,nearest.eulerAngles.y); player.SetCrouch(true);
            foreach (var o in observers) o.ResetObserver(false);
            foreach (var hunter in hunters) hunter.SearchFrom(caughtAt);
            foreach (var l in alertLights.Take(State.prologue_beaten+1)) l.enabled = true;
            if(parkingAlertLight!=null)parkingAlertLight.enabled=true;
            player.RunMultiplier = State.prologue_beaten >= 2 ? 2f/3 : 1;
            player.RestoreStamina();
            BlackTitle = ""; Daze = 1; immuneUntil = Time.time + 6; hidingSeconds = 0; hideAnchor = player.transform.position;
            float master = AudioListener.volume;
            ringSource.clip = NightSfx.Ring; ringSource.Play();
            for (float t = 0; t < 2.4f; t += Time.deltaTime)
            {
                Fade = 1 - Mathf.SmoothStep(0, 1, t / 2.4f);
                AudioListener.volume = master * Mathf.Lerp(.15f, 1, t / 2.4f);
                if (t > 1.2f && Busy) { Busy = false; player.SetOverlay(false); }
                yield return null;
            }
            Fade = 0; AudioListener.volume = master; Busy = false; player.SetOverlay(false);
            Say(State.prologue_beaten == 1 ? "Nó chạy đâu rồi? Kiếm coi!" : "Còn ở quanh đây. Rọi đèn coi!", 3);
        }
        public void ResetPrologue()
        {
            State = new NightOneState(); Clock = new StoryClock(State);
            player.Teleport(alleyStart.position, StartYaw); player.RunMultiplier = 1; player.RestoreStamina();
            foreach (var o in observers) o.ResetObserver(false);
            foreach (var l in alertLights) l.enabled = false;
            leftStart = false; shoutCount = 0; hidingSeconds = 0; hideAnchor = player.transform.position; PrologueDeparted=false;PrologueElapsed=0;
            if(parkingAlertLight!=null)parkingAlertLight.enabled=false;
            Objective = "Bạn cần mang chiếc điện thoại đang cầm trên tay vào nhà xe ở cuối hẻm, rồi cúi xuống đặt nó trên nền xi măng, ngay dưới chân chiếc Wave màu đỏ.";
        }
        public void RestoreCheckpoint()
        {
            if (Busy) return;
            if(State.stage==NightStage.Prologue)
            {
                // A safety recovery must never clear persistent capture penalties.
                player.Teleport(alleyStart.position, StartYaw);player.RunMultiplier=State.prologue_beaten>=2?2f/3:1;player.RestoreStamina();
                foreach(var observer in observers)observer.ResetObserver(State.phonePlaced);
                foreach(var light in alertLights)light.enabled=false;
                foreach(var light in alertLights.Take(State.prologue_beaten>0?State.prologue_beaten+1:0))light.enabled=true;
                if(parkingAlertLight!=null)parkingAlertLight.enabled=State.prologue_beaten>0;
                leftStart=false;hidingSeconds=0;hideAnchor=player.transform.position;immuneUntil=Time.time+3;return;
            }
            bool replayFromEnd=State.stage==NightStage.Ended;
            CloseModal(); player.SetPhone(0); sound.Stop();
            if (!Checkpoints.Restore(out var state, out var point, out float yaw)) return;
            State = state; Clock = new StoryClock(State); player.Teleport(point,yaw);
            if(replayFromEnd) {isReplay=completedInSession;nightStartedAt=Time.time;TelemetryProduced?.Invoke(new NightStartedTelemetry(isReplay));}
            player.RunMultiplier = State.stage == NightStage.Prologue && State.prologue_beaten >= 2 ? 2f/3 : 1;
            BlackTitle = ""; roomSeconds = 0; hidingSeconds = 0; hideAnchor = player.transform.position;
            foreach (var o in observers) o.ResetObserver(State.phonePlaced);
            foreach (var l in alertLights) l.enabled = false;
            Objective = State.stage >= NightStage.Room ? "Khám phá phòng 7 rồi tới nệm." : "Khám phá hẻm, đọc tin và lên phòng 7.";
            bag.transform.position = State.hasBagPosition ? State.bagPosition : bagAtDoor.position;
            SyncWorld();
        }
        void SyncWorld()
        {
            if (State.stage > NightStage.Prologue && (State.bikeParked || State.phoneEverPicked || pushBike == null)) State.bikeArrived = true;
            bool pushing = Pushing;
            if (pushBike != null) pushBike.SetActive(pushing);
            if (ownBike != null) ownBike.SetActive(State.stage != NightStage.Prologue && !pushing);
            if (mouthBike != null) mouthBike.SetActive(State.stage == NightStage.Prologue);
            player.SprintAllowed = !pushing; player.CrouchAllowed = !pushing;
            player.WalkMultiplier = pushing ? .5f : 1;
            player.StaminaLimited = State.stage == NightStage.Prologue;
            if (nganPhone != null) nganPhone.SetActive(State.phonePlaced && !State.hasNganPhone);
            if (nganPhone != null && nganPhone.transform.Find("Screen glow") == null)
            {
                var glow = new GameObject("Screen glow").AddComponent<Light>(); glow.transform.SetParent(nganPhone.transform, false);
                glow.transform.position = nganPhone.transform.position + Vector3.up * .06f;
                glow.type = LightType.Point; glow.range = 1.1f; glow.intensity = 1.2f; glow.color = new Color(.7f, .85f, 1f); glow.shadows = LightShadows.None;
            }
            if (bag != null) bag.SetActive(State.stage >= NightStage.Room);
            if(greyGlove!=null)greyGlove.SetActive(State.helpedLien && State.prologue_beaten>=2);
            if (prologueGate != null) prologueGate.SetActive(State.stage == NightStage.Prologue);
            if (fallenPot != null)
            {
                var potRenderer = fallenPot.GetComponent<Renderer>();
                if (!potCached) { potCached = true; potUpright = fallenPot.rotation; potHome = fallenPot.position; potGround = potRenderer != null ? potRenderer.bounds.min.y : potHome.y; }
                bool down = !State.helpedLien;
                if (!potApplied || down != potShownDown)
                {
                    potApplied = true; potShownDown = down;
                    fallenPot.SetPositionAndRotation(potHome, down ? Quaternion.Euler(0, 0, -90) * potUpright : potUpright);
                    if (down && potRenderer != null) fallenPot.position += Vector3.up * (potGround - potRenderer.bounds.min.y);
                }
            }
            if(State.alarm && State.stage!=NightStage.Ended && State.hasNganPhone && !State.phone_powered_off) {if(!sound.isPlaying)sound.Play();}
            else if(sound.isPlaying)sound.Stop();
        }
    }
}
