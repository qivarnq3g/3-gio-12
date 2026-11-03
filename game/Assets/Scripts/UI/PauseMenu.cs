using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PauseMenu : MonoBehaviour
    {
        static readonly float[] SensitivitySteps = { .4f, .55f, .7f, .85f, 1f, 1.2f, 1.45f, 1.75f, 2.1f };

        sealed class Item
        {
            public RectTransform rect, label, bar;
            public TextMeshProUGUI text;
            public float slide;
        }

        FirstPersonPlayer player;
        CanvasGroup group;
        Item[] items;
        TextMeshProUGUI sensitivityText;
        int selected, sensitivityStep = 4;
        float baseSensitivity;
        bool shown;

        void Awake() { player = GetComponent<FirstPersonPlayer>(); }

        void Start()
        {
            baseSensitivity = player.sensitivity;
            var canvas = GameUi.Canvas(transform, "Pause menu", 100);
            group = canvas.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            GameUi.Fill(GameUi.Box(canvas.transform, "Shade", GameUi.Shade));
            GameUi.Place(GameUi.Box(canvas.transform, "Band", new Color(0, 0, 0, .35f)), new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(820, 4000));

            var column = GameUi.Place(GameUi.Box(canvas.transform, "Column", Color.clear), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(170, 40), new Vector2(620, 640));
            var kicker = GameUi.Text(column, "Kicker", "TẠM DỪNG", 20, GameUi.Accent);
            kicker.characterSpacing = 30;
            GameUi.Place((RectTransform)kicker.transform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(600, 30));
            var title = GameUi.Text(column, "Title", "3 GIỜ 12", 96, GameUi.Ink);
            title.characterSpacing = 8;
            GameUi.Place((RectTransform)title.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-4, -34), new Vector2(620, 120));
            GameUi.Place(GameUi.Box(column, "Rule", GameUi.Accent), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -170), new Vector2(72, 3));

            items = new[]
            {
                MakeItem(column, "Tiếp tục", 0),
                MakeItem(column, "Độ nhạy chuột", 1),
                MakeItem(column, "Thoát game", 2),
            };
            sensitivityText = GameUi.Text(items[1].rect, "Value", "", 30, GameUi.Muted);
            GameUi.Place((RectTransform)sensitivityText.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(330, 0), new Vector2(220, 60));
            ShowSensitivity();

            var card = GameUi.Place(GameUi.Box(canvas.transform, "Controls", GameUi.Card), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-150, 20), new Vector2(470, 430));
            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(40, 40, 36, 36); cardLayout.spacing = 14;
            cardLayout.childControlWidth = cardLayout.childControlHeight = false; cardLayout.childForceExpandWidth = cardLayout.childForceExpandHeight = false;
            var heading = GameUi.Text(card, "Heading", "ĐIỀU KHIỂN", 18, GameUi.Muted);
            heading.characterSpacing = 18; ((RectTransform)heading.transform).sizeDelta = new Vector2(380, 34);
            GameUi.ControlRow(card, "Đi lại", 34, "W", "A", "S", "D");
            GameUi.ControlRow(card, "Nhìn quanh", 34, "Chuột");
            GameUi.ControlRow(card, "Chạy", 34, "Shift");
            GameUi.ControlRow(card, "Cúi người", 34, "C");
            GameUi.ControlRow(card, "Tương tác", 34, "E");
            GameUi.ControlRow(card, "Tạm dừng", 34, "Esc");

            var footer = GameUi.Place(GameUi.Box(canvas.transform, "Footer", Color.clear), Vector2.zero, Vector2.zero, new Vector2(170, 70), new Vector2(900, 34));
            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 34; footerLayout.childAlignment = TextAnchor.MiddleLeft;
            footerLayout.childControlWidth = footerLayout.childControlHeight = false; footerLayout.childForceExpandWidth = footerLayout.childForceExpandHeight = false;
            GameUi.ControlRow(footer, "Chọn", 30, "W", "S");
            GameUi.ControlRow(footer, "Xác nhận", 30, "Enter");
            GameUi.ControlRow(footer, "Quay lại", 30, "Esc");
            canvas.gameObject.SetActive(false);
        }

        Item MakeItem(RectTransform column, string label, int index)
        {
            var rect = GameUi.Place(GameUi.Box(column, "Item " + label, Color.clear), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -215 - index * 70), new Vector2(600, 60));
            var bar = GameUi.Place(GameUi.Box(rect, "Bar", GameUi.Accent), new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(4, 36));
            var text = GameUi.Text(rect, "Label", label, 38, GameUi.Muted);
            var labelRect = GameUi.Place((RectTransform)text.transform, new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(520, 60));
            return new Item { rect = rect, label = labelRect, bar = bar, text = text };
        }

        void Update()
        {
            if (group == null) return;
            bool show = player.IsPaused && !player.HasPauseMenu && player.ActivePhone == 0;
            if (show && !shown) { selected = 0; group.gameObject.SetActive(true); }
            shown = show;
            float dt = Time.unscaledDeltaTime;
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1 : 0, dt * 7);
            if (!show && group.alpha <= 0) { if (group.gameObject.activeSelf) group.gameObject.SetActive(false); return; }
            if (show && !player.PauseChangedThisFrame) HandleInput();
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                bool on = i == selected;
                item.slide = Mathf.MoveTowards(item.slide, on ? 1 : 0, dt * 8);
                item.label.anchoredPosition = new Vector2(22 + item.slide * 14, 0);
                item.text.color = Color.Lerp(GameUi.Muted, GameUi.Ink, item.slide);
                item.bar.localScale = new Vector3(1, item.slide, 1);
            }
        }

        void HandleInput()
        {
            for (int i = 0; i < items.Length; i++)
                if (GameUi.PointerOver(items[i].rect, out bool clicked))
                {
                    selected = i;
                    if (clicked) Activate(i, Mouse.current.position.ReadValue().x > RectTransformUtility.WorldToScreenPoint(null, sensitivityText.rectTransform.position).x ? 1 : -1);
                }
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) selected = (selected + items.Length - 1) % items.Length;
            if (keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) selected = (selected + 1) % items.Length;
            if (selected == 1)
            {
                if (keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame) ChangeSensitivity(-1);
                if (keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame) ChangeSensitivity(1);
            }
            if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame || keys.eKey.wasPressedThisFrame)
                Activate(selected, 1);
        }

        void Activate(int index, int direction)
        {
            if (index == 0) player.ResumeControl();
            else if (index == 1) ChangeSensitivity(direction);
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void ChangeSensitivity(int direction)
        {
            sensitivityStep = Mathf.Clamp(sensitivityStep + direction, 0, SensitivitySteps.Length - 1);
            player.sensitivity = baseSensitivity * SensitivitySteps[sensitivityStep];
            ShowSensitivity();
        }

        void ShowSensitivity()
        {
            sensitivityText.text = "<  " + SensitivitySteps[sensitivityStep].ToString("0.00") + "  >";
        }
    }
}
