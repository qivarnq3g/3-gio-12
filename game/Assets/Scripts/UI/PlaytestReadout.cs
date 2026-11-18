#if UNITY_EDITOR || DEVELOPMENT_BUILD || PLAYTEST_BUILD
using System.Globalization;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PlaytestReadout : MonoBehaviour
    {
        const float RefreshSeconds = .1f, ShortcutDebounce = .075f;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        const int VirtualKeyF3 = 0x72;
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int virtualKey);
        bool windowsF3Down;
#endif

        FirstPersonPlayer player;
        InputAction toggleReadout;
        Canvas canvas;
        TextMeshProUGUI text;
        float nextRefresh, nextShortcut;
        bool visible, shortcutRequested;
        GUIStyle boxStyle, labelStyle;
        string cachedReadout = "";

        public bool IsVisible => visible;
        public void Toggle()
        {
            visible = !visible;
            UpdateCanvasVisibility();
            if (visible)
            {
                cachedReadout = Describe();
                if (text != null) text.text = cachedReadout;
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
                var panel = GameUi.Place(GameUi.Box(canvas.transform, "Panel", new Color(0, 0, 0, .75f)), Vector2.one, Vector2.one, new Vector2(-24, -24), new Vector2(640, 52));
                text = GameUi.Text(panel, "Readout", "", 24, GameUi.Ink, TextAlignmentOptions.Right);
                text.font = GameUi.Font("Fonts/BeVietnamPro-Regular");
                text.enableAutoSizing = true;
                text.fontSizeMin = 17;
                text.fontSizeMax = 24;
                GameUi.Place(text.rectTransform, Vector2.one, Vector2.one, new Vector2(-14, 0), new Vector2(612, 52));
                UpdateCanvasVisibility();
            }
            catch (System.Exception ex)
            {
                if (canvas != null) Destroy(canvas.gameObject);
                canvas = null;
                text = null;
                Debug.LogWarning($"[PlaytestReadout] Canvas UI init warning: {ex.Message}. Falling back to OnGUI.");
            }
        }

        void UpdateCanvasVisibility()
        {
            bool show = visible && !player.IsPaused;
            if (canvas != null && canvas.gameObject.activeSelf != show)
                canvas.gameObject.SetActive(show);
        }

        void Update()
        {
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
            if (!visible || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            cachedReadout = Describe();
            if (text != null) text.text = cachedReadout;
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
                    alignment = TextAnchor.MiddleRight,
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
                var content = new GUIContent(cachedReadout);
                float w = Mathf.Min(labelStyle.CalcSize(content).x + 28, Screen.width - 48);
                var rect = new Rect(Screen.width - w - 24, 24, w, 34);
                GUI.color = new Color(0, 0, 0, .85f);
                GUI.Box(rect, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 14, rect.y, rect.width - 28, rect.height), content, labelStyle);
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
    }
}
#endif
