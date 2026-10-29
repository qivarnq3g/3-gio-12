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
        public float sensitivity = 0.09f, reach = 2.4f;
        public bool IsCrouched { get; private set; }
        public int ActivePhone { get; private set; }
        public int ItemsCollected { get; private set; }
        InputActionAsset inputs;
        InputAction move, look, sprint, crouch, interact, phoneOne, phoneTwo, cancel;
        CharacterController body;
        float pitch, fall;
        WorldInteractable target;
        string notice = "";
        float noticeUntil;
        bool paused;
        GUIStyle text, title;

        void Awake()
        {
            body = GetComponent<CharacterController>();
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
        }
        void OnEnable() { inputs.Enable(); LockCursor(true); }
        void OnDisable() { inputs.Disable(); LockCursor(false); }
        void OnDestroy() { if (inputs != null) Destroy(inputs); }
        void OnApplicationFocus(bool focused) { if (!focused) { paused = true; LockCursor(false); } }
        static void LockCursor(bool locked)
        { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }

        void Update()
        {
            if (cancel.WasPressedThisFrame())
            {
                if (ActivePhone != 0) SetPhone(0);
                else { paused = !paused; LockCursor(!paused); }
            }
            if (!paused)
            {
                if (phoneOne.WasPressedThisFrame()) SetPhone(ActivePhone == 1 ? 0 : 1);
                if (phoneTwo.WasPressedThisFrame()) SetPhone(ActivePhone == 2 ? 0 : 2);
            }
            target = null;
            bool control = !paused && ActivePhone == 0;
            if (control)
            {
                Vector2 delta = look.ReadValue<Vector2>();
                float scale = look.activeControl?.device is Gamepad ? 160 * Time.deltaTime : sensitivity;
                transform.Rotate(0, delta.x * scale, 0);
                pitch = Mathf.Clamp(pitch - delta.y * scale, -80, 80);
                eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
                if (crouch.WasPressedThisFrame()) SetCrouch(!IsCrouched);
                if (Physics.Raycast(eyes.transform.position, eyes.transform.forward, out var hit, reach, ~(1 << 2), QueryTriggerInteraction.Ignore))
                    target = hit.collider.GetComponentInParent<WorldInteractable>();
                if (target != null && interact.WasPressedThisFrame()) target.Interact(this);
            }
            var axis = control ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) : Vector2.zero;
            float speed = IsCrouched ? crouchSpeed : sprint.IsPressed() ? runSpeed : walkSpeed;
            if (body.isGrounded && fall < 0) fall = -2;
            fall = Mathf.Max(fall + Physics.gravity.y * Time.deltaTime, -30);
            body.Move((transform.TransformDirection(new Vector3(axis.x, 0, axis.y)) * speed + Vector3.up * fall) * Time.deltaTime);
            if (transform.position.y < -10) { body.enabled = false; transform.position = new Vector3(0, 0.1f, -3); body.enabled = true; fall = 0; }
        }
        public bool SetCrouch(bool value)
        {
            if (!value && IsCrouched)
            {
                // Only the extra space above the crouched capsule needs checking.
                Vector3 low = transform.position + Vector3.up * 0.98f;
                Vector3 high = transform.position + Vector3.up * 1.5f;
                if (Physics.CheckCapsule(low, high, 0.27f, ~(1 << 2), QueryTriggerInteraction.Ignore))
                { Notify("Không đủ chỗ để đứng lên"); return false; }
            }
            IsCrouched = value;
            body.height = value ? 1.1f : 1.8f;
            body.center = Vector3.up * (body.height / 2);
            eyes.transform.localPosition = Vector3.up * (value ? 0.9f : 1.6f);
            return true;
        }
        public void ResumeControl() { paused = false; LockCursor(ActivePhone == 0); }
        public void SetPhone(int phone) { ActivePhone = phone; LockCursor(phone == 0 && !paused); }
        public void Collect() { ItemsCollected++; Notify("Đã nhặt vật chứng"); }
        public void Notify(string message) { notice = message; noticeUntil = Time.time + 3; }
        void OnGUI()
        {
            if (text == null) { text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true }; title = new GUIStyle(text) { fontSize = 28, alignment = TextAnchor.MiddleCenter }; }
            GUI.Box(new Rect(12, 12, 580, 95), "");
            GUI.Label(new Rect(24, 20, 555, 85), "PHÒNG THỬ #6 — WASD: đi | Shift: chạy | C: cúi\nE: tương tác | 1/2: điện thoại | Esc: đóng / thả chuột\nVật đã nhặt: " + ItemsCollected + (IsCrouched ? "  •  Đang cúi" : ""), text);
            if (ActivePhone != 0)
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
                GUI.Label(new Rect(30, Screen.height / 2 - 95, Screen.width - 60, 70), ActivePhone == 1 ? "MÁY NGÂN" : "MÁY SINH VIÊN", title);
                GUI.Label(new Rect(30, Screen.height / 2 - 10, Screen.width - 60, 90), "Giao diện giữ chỗ để nối với #17.\nNhân vật đứng yên; thế giới vẫn tiếp tục chạy.", title);
                if (GUI.Button(new Rect(Screen.width / 2 - 90, Screen.height / 2 + 100, 180, 45), "Cất điện thoại (Esc)")) SetPhone(0);
            }
            else if (paused)
            {
                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 50), "Tiếp tục (Esc)")) { ResumeControl(); }
            }
            else
            {
                GUI.Label(new Rect(Screen.width / 2 - 5, Screen.height / 2 - 12, 20, 30), "+", text);
                if (target != null) GUI.Label(new Rect(Screen.width / 2 - 180, Screen.height * 0.7f, 360, 60), "E — " + target.Prompt, title);
            }
            if (Time.time < noticeUntil && ActivePhone == 0) GUI.Label(new Rect(24, 115, 550, 50), notice, text);
        }
    }
}
