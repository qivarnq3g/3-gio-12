#if UNITY_EDITOR || DEVELOPMENT_BUILD || PLAYTEST_BUILD
using System.Globalization;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PlaytestReadout : MonoBehaviour
    {
        const float RefreshSeconds = .1f, ShortcutDebounce = .075f, ProbeDistance = 50;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        const int VirtualKeyF3 = 0x72;
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int virtualKey);
        bool windowsF3Down;
#endif

        FirstPersonPlayer player;
        InputAction toggleReadout;
        Canvas canvas;
        TextMeshProUGUI coordinates, diagnostics;
        float nextRefresh, nextShortcut, smoothedFrame = 1 / 60f;
        bool visible, shortcutRequested;
        GUIStyle boxStyle, labelStyle;
        string cachedReadout = "", cachedDiagnostics = "";

        public bool IsVisible => visible;
        public void Toggle()
        {
            visible = !visible;
            UpdateCanvasVisibility();
            if (visible)
            {
                RefreshText();
            }
            Debug.Log($"[PlaytestReadout] Toggled -> Visible = {visible}");
        }
        public string GetReadoutText() => Describe();

        void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            toggleReadout = new InputAction("Toggle coordinates", InputActionType.Button, "<Keyboard>/f3");
            toggleReadout.AddBinding("<Keyboard>/backquote");
            toggleReadout.AddBinding("<Keyboard>/f1");
            toggleReadout.performed += OnTogglePressed;
        }

        void OnEnable() => toggleReadout.Enable();
        void OnDisable() => toggleReadout.Disable();
        void OnDestroy() => toggleReadout.Dispose();
        void OnTogglePressed(InputAction.CallbackContext _)
        {
            if (!player.IsPaused) shortcutRequested = true;
        }

        void Start()
        {
            Init();
        }

        void Init()
        {
            if (canvas != null) return;
            try
            {
                canvas = GameUi.Canvas(transform, "Playtest readout", 90);
                canvas.GetComponent<CanvasGroup>().blocksRaycasts = false;
                var panel = GameUi.Place(GameUi.Box(canvas.transform, "Panel", new Color(0, 0, 0, .6f)),
                    Vector2.one, Vector2.one, new Vector2(-24, -24), new Vector2(760, 182));

                coordinates = GameUi.Text(panel, "Coordinates", "", 24, GameUi.Ink, TextAlignmentOptions.TopLeft);
                coordinates.font = GameUi.Font("Fonts/BeVietnamPro-Regular");
                coordinates.enableAutoSizing = true;
                coordinates.fontSizeMin = 17;
                coordinates.fontSizeMax = 24;
                GameUi.Place(coordinates.rectTransform, Vector2.up, Vector2.up, new Vector2(16, -12), new Vector2(728, 32));

                diagnostics = GameUi.Text(panel, "Diagnostics", "", 18, GameUi.Ink, TextAlignmentOptions.TopLeft);
                diagnostics.font = coordinates.font;
                diagnostics.overflowMode = TextOverflowModes.Ellipsis;
                GameUi.Place(diagnostics.rectTransform, Vector2.up, Vector2.up, new Vector2(16, -50), new Vector2(728, 120));
                UpdateCanvasVisibility();
            }
            catch (System.Exception ex)
            {
                if (canvas != null) Destroy(canvas.gameObject);
                canvas = null;
                coordinates = diagnostics = null;
                Debug.LogWarning($"[PlaytestReadout] Canvas UI init warning: {ex.Message}. Falling back to OnGUI.");
            }
        }

        void UpdateCanvasVisibility()
        {
            bool show = visible && !player.IsPaused;
            if (canvas != null && canvas.gameObject.activeSelf != show)
            {
                canvas.gameObject.SetActive(show);
                nextRefresh = 0;
            }
        }

        void Update()
        {
            smoothedFrame = Mathf.Lerp(smoothedFrame, Time.unscaledDeltaTime, .05f);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            shortcutRequested |= PhysicalKeyboardHook.ConsumeReadoutToggle();
#endif
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            bool f3Down = (GetAsyncKeyState(VirtualKeyF3) & 0x8000) != 0;
            shortcutRequested |= Application.isFocused && f3Down && !windowsF3Down;
            windowsF3Down = f3Down;
#endif
            // Windows and Unity can report the same press on neighboring frames.
            if (shortcutRequested && !player.IsPaused && Time.unscaledTime >= nextShortcut)
            {
                Toggle();
                nextShortcut = Time.unscaledTime + ShortcutDebounce;
            }
            shortcutRequested = false;
            UpdateCanvasVisibility();
            if (!visible || player.IsPaused || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            RefreshText();
        }

        void RefreshText()
        {
            cachedReadout = Describe();
            cachedDiagnostics = DescribeDiagnostics();
            if (coordinates != null) coordinates.text = cachedReadout;
            if (diagnostics != null) diagnostics.text = cachedDiagnostics;
        }

        void OnGUI()
        {
            if (canvas != null || !visible || player.IsPaused) return;

            // Render lớp phủ OnGUI đảm bảo luôn luôn nhìn thấy trên màn hình
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }

            var origColor = GUI.color;
            var origMatrix = GUI.matrix;
            int origDepth = GUI.depth;
            try
            {
                GUI.depth = -2000;
                GUI.matrix = Matrix4x4.identity;
                var content = new GUIContent(cachedReadout + "\n" + cachedDiagnostics);
                float w = Mathf.Min(labelStyle.CalcSize(content).x + 28, Screen.width - 48);
                float height = labelStyle.CalcHeight(content, w - 28) + 24;
                var rect = new Rect(Screen.width - w - 24, 24, w, height);
                GUI.color = new Color(0, 0, 0, .85f);
                GUI.Box(rect, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 14, rect.y + 12, rect.width - 28, rect.height - 24), content, labelStyle);
            }
            finally
            {
                GUI.color = origColor;
                GUI.matrix = origMatrix;
                GUI.depth = origDepth;
            }
        }

        string Describe()
        {
            var p = transform.position;
            float yaw = Mathf.Repeat(transform.eulerAngles.y, 360);
            float pitch = Mathf.DeltaAngle(0, player.ViewPitch);
            return string.Format(CultureInfo.InvariantCulture,
                "X {0:F2}   Y {1:F2}   Z {2:F2}  |  Hướng {3:F0}° · Góc dọc {4:F0}°",
                p.x, p.y, p.z, yaw, pitch);
        }
        string DescribeDiagnostics()
        {
            var eye = player.eyes != null ? player.eyes.transform : transform;
            return string.Format(CultureInfo.InvariantCulture,
                "Dưới chân: {0}\nĐang nhìn: {1}\n{2}   {3:F0} FPS   {4}\nV: bay   F5 hẻm, F6 giữa hẻm, F7 cổng, F8 nhà xe, F9 phòng 7",
                Hit(transform.position + Vector3.up * .5f, Vector3.down),
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
