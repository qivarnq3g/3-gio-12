using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        public InputActionAsset inputTemplate;
        public Camera eyes;
        public float walkSpeed = 3, runSpeed = 5, crouchSpeed = 1.5f;
        public float sensitivity = 0.09f, reach = 2.4f, keyTurnSpeed = 120;
        public bool showTestHud = true;
        public bool ExternalUI { get; set; }
        public bool HasPauseMenu { get; set; }
        public bool PauseMenuLocked { get; set; }
        public IGameSession Session { get; set; }
        public bool InvertY { get; set; }
        public float ViewPitch => pitch;
        public float RunMultiplier { get; set; } = 1f;
        public float WalkMultiplier { get; set; } = 1f;
        public bool SprintAllowed { get; set; } = true;
        public bool CrouchAllowed { get; set; } = true;
        public bool StaminaLimited { get; set; }
        public float staminaSeconds = 3.2f, staminaRecovery = .22f, staminaResume = .4f;
        public float Stamina { get; private set; } = 1f;
        public bool Winded { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused => paused;
        public uint PoseRevision { get; private set; }
        public bool PauseChangedThisFrame => pauseFrame == Time.frameCount;
        public System.Func<int, bool> CanOpenPhone;
        public event System.Action<int, int> PhoneChanged;
        public event System.Action FellOutOfWorld;
        public bool IsCrouched { get; private set; }
        public int ActivePhone { get; private set; }
        public int ItemsCollected { get; private set; }
        InputActionAsset inputs;
        InputAction move, look, lookKeys, sprint, crouch, interact, phoneOne, phoneTwo, cancel;
        CharacterController body;
        float pitch, fall;
        WorldInteractable target;
        string notice = "";
        float noticeUntil;
        bool paused;
        int pauseFrame = -1;
        float previousTimeScale = 1;
        bool previousAudioPause;
        GUIStyle text, title;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            if (!GetComponent<PlayerCameraMotion>()) gameObject.AddComponent<PlayerCameraMotion>();
            if (!GetComponent<PlayerFootsteps>()) gameObject.AddComponent<PlayerFootsteps>();
            if (!GetComponent<ControlsHint>()) gameObject.AddComponent<ControlsHint>();
            if (!GetComponent<PauseMenu>()) gameObject.AddComponent<PauseMenu>();
#if UNITY_EDITOR || PLAYTEST_BUILD
            if (!GetComponent<PlaytestReadout>()) gameObject.AddComponent<PlaytestReadout>();
            if (!GetComponent<PlaytestFreeCam>()) gameObject.AddComponent<PlaytestFreeCam>();
#endif
            inputs = Instantiate(inputTemplate);
            var map = inputs.FindActionMap("Player", true);
            move = map.FindAction("Move", true); look = map.FindAction("Look", true);
            sprint = map.FindAction("Sprint", true); crouch = map.FindAction("Crouch", true);
            interact = map.FindAction("Interact", true);
            // Tap interaction in this scene; do not change the shared prototype asset.
            interact.ApplyBindingOverride(0, new InputBinding { overrideInteractions = "Press" });
            phoneOne = map.AddAction("PhoneOne", InputActionType.Button, "<Keyboard>/1");
            phoneTwo = map.AddAction("PhoneTwo", InputActionType.Button, "<Keyboard>/2");
            cancel = map.AddAction("Close", InputActionType.Button, "<Keyboard>/escape");
            for (int i = 0; i < move.bindings.Count; i++)
                if (move.bindings[i].path != null && move.bindings[i].path.EndsWith("Arrow")) move.ApplyBindingOverride(i, "");
            lookKeys = map.AddAction("LookKeys", InputActionType.Value);
            lookKeys.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        }
        void OnEnable() { inputs.Enable(); LockCursor(true); }
        void OnDisable() { if(inputs!=null) inputs.Disable(); SetPaused(false); LockCursor(false); }
        void OnDestroy() { if (inputs != null) Destroy(inputs); }
        void OnApplicationFocus(bool focused) { if (!focused && Application.isPlaying) SetPaused(true); }
        static void LockCursor(bool locked)
        { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }

        void Update()
        {
            if (cancel.WasPressedThisFrame())
            {
                if (paused) { if (!PauseMenuLocked) ResumeControl(); }
                else if (ActivePhone != 0) SetPhone(0);
                else if (!ExternalUI) SetPaused(!paused);
            }
            if (!paused)
            {
                if (phoneOne.WasPressedThisFrame()) SetPhone(ActivePhone == 1 ? 0 : 1);
                if (phoneTwo.WasPressedThisFrame()) SetPhone(ActivePhone == 2 ? 0 : 2);
            }
            target = null;
            bool control = !paused && ActivePhone == 0 && !ExternalUI;
            if (control)
            {
                Vector2 delta = look.ReadValue<Vector2>();
                float scale = look.activeControl?.device is Gamepad ? 160 * Time.deltaTime : sensitivity;
                transform.Rotate(0, delta.x * scale, 0);
                pitch = Mathf.Clamp(pitch - delta.y * scale * (InvertY ? -1 : 1), -80, 80);
                Vector2 turn = lookKeys.ReadValue<Vector2>() * keyTurnSpeed * Time.deltaTime;
                transform.Rotate(0, turn.x, 0);
                pitch = Mathf.Clamp(pitch - turn.y * .7f, -80, 80);
                eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
                if (crouch.WasPressedThisFrame() && (CrouchAllowed || IsCrouched)) SetCrouch(!IsCrouched);
                if (Physics.Raycast(eyes.transform.position, eyes.transform.forward, out var hit, reach, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore))
                    target = hit.collider.GetComponentInParent<WorldInteractable>();
                if (target != null) target.HandleInput(this, interact.WasPressedThisFrame(), interact.IsPressed());
            }
            var axis = control ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) : Vector2.zero;
            IsRunning = control && SprintAllowed && !IsCrouched && !Winded && sprint.IsPressed() && axis.sqrMagnitude > .01f;
            if (StaminaLimited && IsRunning) Stamina = Mathf.Max(0, Stamina - Time.deltaTime / staminaSeconds);
            else Stamina = Mathf.Min(1, Stamina + Time.deltaTime * staminaRecovery);
            if (!StaminaLimited) Stamina = 1;
            if (Stamina <= 0) { Winded = true; IsRunning = false; }
            else if (Winded && Stamina >= staminaResume) Winded = false;
            float speed = IsCrouched ? crouchSpeed : IsRunning ? runSpeed * RunMultiplier : walkSpeed * WalkMultiplier * (Winded ? .8f : 1);
            if (body.isGrounded && fall < 0) fall = -2;
            fall = Mathf.Max(fall + Physics.gravity.y * Time.deltaTime, -30);
            body.Move((transform.TransformDirection(new Vector3(axis.x, 0, axis.y)) * speed + Vector3.up * fall) * Time.deltaTime);
            if (transform.position.y < -10 && FellOutOfWorld != null) { FellOutOfWorld.Invoke(); return; }
            if (transform.position.y < -10) { body.enabled = false; transform.position = new Vector3(0, 0.1f, -3); body.enabled = true; fall = 0; }
        }
        public bool SetCrouch(bool value)
        {
            if (!value && IsCrouched)
            {
                // Only the extra space above the crouched capsule needs checking.
                Vector3 low = transform.position + Vector3.up * (PlayerGeometry.CrouchedHeight - PlayerGeometry.Radius);
                Vector3 high = transform.position + Vector3.up * (PlayerGeometry.StandingHeight - PlayerGeometry.Radius);
                if (Physics.CheckCapsule(low, high, PlayerGeometry.Radius, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore))
                { Notify("Không đủ chỗ để đứng lên"); return false; }
            }
            IsCrouched = value;
            body.height = value ? PlayerGeometry.CrouchedHeight : PlayerGeometry.StandingHeight;
            body.center = Vector3.up * (body.height / 2);
            eyes.transform.localPosition = Vector3.up * (value ? PlayerGeometry.CrouchedEye : PlayerGeometry.StandingEye);
            return true;
        }
        public void RestoreStamina() { Stamina = 1; Winded = false; }
        public void SetPaused(bool value)
        {
            if (paused == value) return;
            if (value) {previousTimeScale=Time.timeScale;previousAudioPause=AudioListener.pause;Time.timeScale=0;AudioListener.pause=true;}
            else {Time.timeScale=previousTimeScale;AudioListener.pause=previousAudioPause;}
            paused=value; pauseFrame=Time.frameCount; LockCursor(!value && ActivePhone==0 && !ExternalUI);
        }
        public void ResumeControl() { SetPaused(false); LockCursor(ActivePhone == 0 && !ExternalUI); }
        public void SetPhone(int phone)
        {
            if (phone != 0 && (ExternalUI || (CanOpenPhone != null && !CanOpenPhone(phone)))) return;
            int previous = ActivePhone;
            ActivePhone = phone;
            LockCursor(phone == 0 && !paused && !ExternalUI);
            if (previous != phone) PhoneChanged?.Invoke(previous, phone);
        }
        public void SetOverlay(bool open) { ExternalUI = open; LockCursor(!open && ActivePhone == 0 && !paused); }
        public void Teleport(Vector3 point, float yaw = 0)
        {
            PoseRevision++;
            body.enabled = false; transform.SetPositionAndRotation(point, Quaternion.Euler(0, yaw, 0));
            body.enabled = true; fall = 0; pitch = 0; SetCrouch(false);
        }
        public void AimAt(Vector3 point, float blend)
        {
            Vector3 d = point - eyes.transform.position;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float aim = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yaw, 0), blend);
            pitch = Mathf.Clamp(Mathf.LerpAngle(pitch, aim, blend), -80, 80);
            eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        public void RestoreView(float savedPitch, bool crouched)
        {
            SetCrouch(crouched); pitch = Mathf.Clamp(savedPitch, -80, 80);
            eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        public void Collect() { ItemsCollected++; Notify("Đã nhặt vật chứng"); }
        public void Notify(string message) { notice = message; noticeUntil = Time.time + 3; }
        void OnGUI()
        {
            if (text == null) { text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true }; title = new GUIStyle(text) { fontSize = 28, alignment = TextAnchor.MiddleCenter }; }
            if (paused && HasPauseMenu) return;
            if (paused) return;
            if (!showTestHud)
            {
                if (!paused && ActivePhone == 0 && !ExternalUI && target != null && !string.IsNullOrEmpty(target.Prompt)) GUI.Label(new Rect(Screen.width/2 - 220, Screen.height*.64f, 440, 65), "E - " + target.Prompt, title);
                return;
            }
            GUI.Box(new Rect(12, 12, 580, 95), "");
            GUI.Label(new Rect(24, 20, 555, 85), "PHÒNG THỬ #6 - WASD: đi | Shift: chạy | C: cúi\nE: tương tác | 1/2: điện thoại | Esc: đóng / thả chuột\nVật đã nhặt: " + ItemsCollected + (IsCrouched ? "  •  Đang cúi" : ""), text);
            if (ActivePhone != 0)
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
                GUI.Label(new Rect(30, Screen.height / 2 - 95, Screen.width - 60, 70), ActivePhone == 1 ? "MÁY NGÂN" : "MÁY SINH VIÊN", title);
                GUI.Label(new Rect(30, Screen.height / 2 - 10, Screen.width - 60, 90), "Giao diện giữ chỗ để nối với #17.\nNhân vật đứng yên; thế giới vẫn tiếp tục chạy.", title);
                if (GUI.Button(new Rect(Screen.width / 2 - 90, Screen.height / 2 + 100, 180, 45), "Cất điện thoại (Esc)")) SetPhone(0);
            }
            else
            {
                GUI.Label(new Rect(Screen.width / 2 - 5, Screen.height / 2 - 12, 20, 30), "+", text);
                if (target != null && !string.IsNullOrEmpty(target.Prompt)) GUI.Label(new Rect(Screen.width / 2 - 180, Screen.height * 0.7f, 360, 60), "E - " + target.Prompt, title);
            }
            if (Time.time < noticeUntil && ActivePhone == 0) GUI.Label(new Rect(24, 115, 550, 50), notice, text);
        }
    }
}
