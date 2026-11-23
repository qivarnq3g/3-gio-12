#if UNITY_EDITOR || PLAYTEST_BUILD
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PlaytestReadout : MonoBehaviour
    {
        const float RefreshSeconds = .1f, ProbeDistance = 50;

        FirstPersonPlayer player;
        Canvas canvas;
        TextMeshProUGUI coordinates, diagnostics;
        float nextRefresh, smoothedFrame = 1 / 60f;
        bool visible;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern short GetAsyncKeyState(int virtualKey);
        bool windowsF3Down;
#endif
        InputAction toggleAction;
        bool shortcutRequested;
        float nextShortcut;

        void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            toggleAction = new InputAction("Toggle readout", InputActionType.Button, "<Keyboard>/f3");
            toggleAction.AddBinding("<Keyboard>/backquote");
            toggleAction.AddBinding("<Keyboard>/f1");
            toggleAction.performed += _ => shortcutRequested = true;
        }

        void OnEnable() => toggleAction.Enable();
        void OnDisable() => toggleAction.Disable();
        void OnDestroy() => toggleAction.Dispose();

        void Start()
        {
            canvas = GameUi.Canvas(transform, "Playtest readout", 90);
            canvas.GetComponent<CanvasGroup>().blocksRaycasts = false;
            var panel = GameUi.Place(GameUi.Box(canvas.transform, "Panel", new Color(0, 0, 0, .6f)),
                Vector2.one, Vector2.one, new Vector2(-24, -24), new Vector2(760, 182));

            coordinates = GameUi.Text(panel, "Coordinates", "", 24, GameUi.Ink, TextAlignmentOptions.TopLeft);
            coordinates.enableAutoSizing = true;
            coordinates.fontSizeMin = 17;
            coordinates.fontSizeMax = 24;
            GameUi.Place(coordinates.rectTransform, Vector2.up, Vector2.up, new Vector2(16, -12), new Vector2(728, 32));

            diagnostics = GameUi.Text(panel, "Diagnostics", "", 18, GameUi.Ink, TextAlignmentOptions.TopLeft);
            diagnostics.overflowMode = TextOverflowModes.Ellipsis;
            GameUi.Place(diagnostics.rectTransform, Vector2.up, Vector2.up, new Vector2(16, -50), new Vector2(728, 120));
            canvas.gameObject.SetActive(false);
        }

        void Update()
        {
            smoothedFrame = Mathf.Lerp(smoothedFrame, Time.unscaledDeltaTime, .05f);
            // The low-level hook and the Windows key state catch F3 when the Input System device misses a quick tap.
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            shortcutRequested |= PhysicalKeyboardHook.ConsumeReadoutToggle();
#endif
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            bool f3Down = (GetAsyncKeyState(0x72) & 0x8000) != 0;
            shortcutRequested |= Application.isFocused && f3Down && !windowsF3Down;
            windowsF3Down = f3Down;
#endif
            // The same press can arrive from several sources on neighbouring frames.
            if (shortcutRequested && !player.IsPaused && Time.unscaledTime >= nextShortcut)
            {
                visible = !visible;
                nextShortcut = Time.unscaledTime + .075f;
            }
            shortcutRequested = false;

            bool show = visible && !player.IsPaused;
            if (canvas.gameObject.activeSelf != show)
            {
                canvas.gameObject.SetActive(show);
                nextRefresh = 0;
            }

            if (!show || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;

            var position = transform.position;
            float yaw = Mathf.Repeat(transform.eulerAngles.y, 360);
            float pitch = Mathf.DeltaAngle(0, player.ViewPitch);
            coordinates.text = string.Format(CultureInfo.InvariantCulture,
                "X {0:F2}   Y {1:F2}   Z {2:F2}  |  Hướng {3:F0}° · Góc dọc {4:F0}°",
                position.x, position.y, position.z, yaw, pitch);

            var eye = player.eyes != null ? player.eyes.transform : transform;
            diagnostics.text = string.Format(CultureInfo.InvariantCulture,
                "Dưới chân: {0}\nĐang nhìn: {1}\n{2}   {3:F0} FPS   {4}\nV: bay   F5 hẻm, F6 giữa hẻm, F7 cổng, F8 nhà xe, F9 phòng 7",
                Hit(position + Vector3.up * .5f, Vector3.down),
                Hit(eye.position, eye.forward),
                SceneManager.GetActiveScene().name,
                1 / Mathf.Max(smoothedFrame, .0001f), SystemInfo.graphicsDeviceType);
        }

        string Hit(Vector3 origin, Vector3 direction)
        {
            foreach (var hit in Sorted(Physics.RaycastAll(origin, direction, ProbeDistance, ~0, QueryTriggerInteraction.Ignore)))
                if (!hit.collider.transform.IsChildOf(transform)) return hit.collider.name;
            return "(trống)";
        }

        static RaycastHit[] Sorted(RaycastHit[] hits)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            return hits;
        }
    }
}
#endif
