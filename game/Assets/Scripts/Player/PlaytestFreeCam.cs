#if UNITY_EDITOR || PLAYTEST_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PlaytestFreeCam : MonoBehaviour
    {
        public const string ReviewScene = "RealisticPreview";

        public static readonly (Key key, string name, Vector3 eye, float yaw)[] Spots =
        {
            (Key.F5, "Đầu hẻm", new Vector3(0, 1.6f, -1), 180),
            (Key.F6, "Giữa hẻm", new Vector3(.4f, 1.6f, -30), 180),
            (Key.F7, "Cổng nhà trọ", new Vector3(-.6f, 1.6f, -38.5f), 150),
            (Key.F8, "Nhà xe", new Vector3(.3f, 1.6f, -47.5f), 180),
            (Key.F9, "Phòng 7", new Vector3(-2.3f, 11.8f, -42.3f), 200),
            (Key.F10, "Đường lớn", new Vector3(20, 1.6f, 7), 90),
            (Key.F11, "Cây xăng", new Vector3(-105, 1.7f, 15.2f), 350),
        };

        FirstPersonPlayer player;
        CharacterController body;
        PlayerCameraMotion motion;
        PlayerFootsteps steps;
        float yaw, pitch, speed = 6;
        GUIStyle style, noticeStyle;
        string notice;
        float noticeUntil;

        public bool Flying { get; private set; }
        public float Speed => speed;

        void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            body = GetComponent<CharacterController>();
            motion = GetComponent<PlayerCameraMotion>();
            steps = GetComponent<PlayerFootsteps>();
        }

        void Update()
        {
            var keys = Keyboard.current;
            if (keys == null || player.IsPaused || player.ExternalUI || player.ActivePhone != 0) return;
            if (keys.vKey.wasPressedThisFrame) SetFlying(!Flying);
            if (SceneManager.GetActiveScene().name == ReviewScene)
                foreach (var s in Spots)
                    if (keys[s.key].wasPressedThisFrame) GoTo(s.name);
            if (Flying) Fly(keys);
        }

        public void SetFlying(bool on)
        {
            if (on == Flying) return;
            var landing = Vector3.zero;
            if (!on && !TryGround(transform.position, out landing))
            {
                Say("Bên dưới không có nền, bay tới chỗ có sàn rồi bấm V");
                return;
            }
            Flying = on;
            if (on)
            {
                yaw = transform.eulerAngles.y;
                pitch = player.ViewPitch;
                var eye = player.eyes.transform.position;
                SetWalking(false);
                transform.position = eye;
                player.eyes.transform.localPosition = Vector3.zero;
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                return;
            }
            SetWalking(true);
            player.Teleport(landing, yaw);
            player.RestoreView(pitch, false);
        }

        public bool GoTo(string name)
        {
            foreach (var s in Spots)
            {
                if (s.name != name) continue;
                yaw = s.yaw; pitch = 0;
                if (Flying)
                {
                    transform.SetPositionAndRotation(s.eye, Quaternion.Euler(0, yaw, 0));
                    player.eyes.transform.localRotation = Quaternion.identity;
                }
                else player.Teleport(Ground(s.eye), s.yaw);
                Say("Tới: " + s.name);
                return true;
            }
            return false;
        }

        void SetWalking(bool on)
        {
            body.enabled = on;
            player.enabled = on;
            if (motion) motion.enabled = on;
            if (steps) steps.enabled = on;
        }

        static bool TryGround(Vector3 eye, out Vector3 point)
        {
            point = eye - Vector3.up * PlayerGeometry.StandingEye;
            if (!Physics.Raycast(eye + Vector3.up * .2f, Vector3.down, out var hit, 30, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore))
                return false;
            point = hit.point + Vector3.up * .05f;
            return true;
        }

        static Vector3 Ground(Vector3 eye)
        {
            TryGround(eye, out var p);
            return p;
        }

        void Fly(Keyboard keys)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var d = mouse.delta.ReadValue() * player.sensitivity;
                yaw += d.x;
                pitch = Mathf.Clamp(pitch - d.y * (player.InvertY ? -1 : 1), -89, 89);
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0) speed = Mathf.Clamp(speed * (scroll > 0 ? 1.25f : .8f), .5f, 60);
            }
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            player.eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            var dir = Vector3.zero;
            if (keys.wKey.isPressed) dir += player.eyes.transform.forward;
            if (keys.sKey.isPressed) dir -= player.eyes.transform.forward;
            if (keys.dKey.isPressed) dir += transform.right;
            if (keys.aKey.isPressed) dir -= transform.right;
            if (keys.spaceKey.isPressed || keys.eKey.isPressed) dir += Vector3.up;
            if (keys.leftCtrlKey.isPressed || keys.qKey.isPressed) dir -= Vector3.up;
            float boost = keys.leftShiftKey.isPressed ? 3 : 1;
            transform.position += Vector3.ClampMagnitude(dir, 1) * speed * boost * Time.unscaledDeltaTime;
        }

        void Say(string message) { notice = message; noticeUntil = Time.unscaledTime + 3; }

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
                noticeStyle = new GUIStyle(style) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            }
            if (Time.unscaledTime < noticeUntil) GUI.Label(new Rect(0, Screen.height * .12f, Screen.width, 40), notice, noticeStyle);
            if (!Flying) return;
            GUI.Label(new Rect(24, Screen.height - 60, 900, 40),
                $"Đang bay  ·  V: thôi bay  ·  WASD, Space/E lên, Ctrl/Q xuống  ·  Shift: nhanh  ·  Lăn chuột: tốc độ {speed:0.#} m/s", style);
        }
    }
}
#endif
