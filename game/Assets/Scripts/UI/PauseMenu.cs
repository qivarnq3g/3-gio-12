using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PauseMenu : MonoBehaviour
    {
        const int EntryCount = 6;
        const float IdleSeconds = 40, TrackWidth = 220;
        const string Regular = "Fonts/BeVietnamPro-Regular", Medium = "Fonts/BeVietnamPro-Medium";
        static readonly Color Warm = new Color(1, .8f, .52f);
        static readonly Color Paper = new Color(.97f, .93f, .85f);
        enum Kind { Slider, Toggle, Action }

        sealed class Entry
        {
            public RawImage stripe;
            public RectTransform rect, label;
            public TextMeshProUGUI text;
            public float glow;
        }

        sealed class Item
        {
            public string label, confirm;
            public Action run;
        }

        sealed class Setting
        {
            public string name, hint;
            public Kind kind;
            public float min, max, step;
            public bool peek;
            public Func<float> get;
            public Action<float> set;
            public Func<float, string> format;
            public Action run;
            public RectTransform rect, minus, plus;
            public RawImage stripe;
            public RectTransform fill, handle;
            public TextMeshProUGUI text, value, minusText, plusText;
            public float glow;
        }

        FirstPersonPlayer player;
        GameSettings settings, defaults;
        CanvasGroup group, ceiling, light, settingsPage, controlsPage, mainFooter, startFooter, pageFooter;
        RawImage grain;
        TextMeshProUGUI hint, darkHint, paused, warning, status;
        readonly List<Item> items = new List<Item>();
        int settingsIndex, controlsIndex, confirmIndex = -1;
        IGameSession Session => player.Session;
        bool AtStart => Session != null && Session.AtStart;
        Entry[] entries;
        List<Setting> rows;
        AudioSource clicks, hum;
        int selected, row, dragging = -1, leftPageFrame = -1;
        bool shown, inPage, dirty, lightsOn;
        float quitUntil, grainAt, repeatAt, lastInput, flickerAt = -10, lightLevel;

        void Awake() { player = GetComponent<FirstPersonPlayer>(); }

        void Start()
        {
            defaults = new GameSettings { sensitivity = player.sensitivity, fieldOfView = player.eyes != null ? player.eyes.fieldOfView : 70 };
            settings = GameSettings.Load(defaults);
            settings.Apply(player);
            clicks = MakeSource(false);
            hum = MakeSource(true);
        }

        void OnDestroy()
        {
            if (player != null) player.PauseMenuLocked = false;
        }

        AudioSource MakeSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.ignoreListenerPause = true; source.loop = loop;
            if (loop) { source.clip = MenuSounds.Hum; source.volume = 0; }
            return source;
        }

        static TextMeshProUGUI Words(Transform parent, string name, string value, float size, Color color, bool medium = false, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var text = GameUi.Text(parent, name, value, size, color, align);
            text.font = GameUi.Font(medium ? Medium : Regular);
            return text;
        }

        static RawImage StripeImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var image = GameUi.Raw(parent, name, GameUi.Stripe(Mathf.RoundToInt(size.x / 2), Mathf.RoundToInt(size.y / 2), 0), new Color(Warm.r, Warm.g, Warm.b, 0));
            GameUi.Place(image.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), position, size);
            return image;
        }

        void Build()
        {
            var canvas = GameUi.Canvas(transform, "Pause menu", 100);
            group = canvas.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            var root = canvas.transform;

            var ceilingRect = GameUi.Fill(GameUi.Box(root, "Ceiling", new Color(.045f, .04f, .036f, 1)));
            ceiling = ceilingRect.gameObject.AddComponent<CanvasGroup>();
            GameUi.Fill(GameUi.Raw(ceilingRect, "Plaster", GameUi.Mottle(192, 108, 37), new Color(.11f, .1f, .085f, .9f)).rectTransform);

            var lightRect = GameUi.Fill(GameUi.Box(root, "Light", Color.clear));
            light = lightRect.gameObject.AddComponent<CanvasGroup>(); light.alpha = 0;

            entries = new Entry[EntryCount];
            for (int i = 0; i < EntryCount; i++) entries[i] = MakeEntry(lightRect, i);

            var wordmark = Words(root, "Wordmark", "3 GIỜ 12", 26, new Color(Paper.r, Paper.g, Paper.b, .3f), true);
            wordmark.characterSpacing = 24;
            GameUi.Place(wordmark.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -90), new Vector2(500, 40));
            paused = Words(root, "Paused", "Tạm dừng", 20, new Color(Paper.r, Paper.g, Paper.b, .3f));
            GameUi.Place(paused.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(162, -128), new Vector2(300, 30));
            warning = Words(root, "Content warning", "Cảnh báo nội dung: bạo lực, truy đuổi, âm thanh bất ngờ và tình huống gây căng thẳng.", 20, new Color(1, .72f, .6f, .75f));
            GameUi.Place(warning.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(162, -128), new Vector2(1500, 30));
            status = Words(root, "Status", "", 20, new Color(Paper.r, Paper.g, Paper.b, .55f));
            GameUi.Place(status.rectTransform, Vector2.zero, Vector2.zero, new Vector2(162, 110), new Vector2(1500, 30));

            settingsPage = Page(lightRect, "Cài đặt");
            rows = new List<Setting>
            {
                Slider("Độ nhạy chuột", "Tốc độ xoay góc nhìn khi di chuột.", .02f, .3f, .01f,
                    () => settings.sensitivity, v => settings.sensitivity = v, v => Number(v * 50, 1)),
                Toggle("Đảo trục dọc", "Đẩy chuột lên thì nhìn xuống.",
                    () => settings.invertY ? 1 : 0, v => settings.invertY = v > .5f),
                Slider("Góc nhìn", "Rộng hơn thì thấy nhiều hơn, nhưng rìa hình bị kéo giãn. Nền tối mờ đi để thấy cảnh phía sau.", 55, 95, 1,
                    () => settings.fieldOfView, v => settings.fieldOfView = v, v => Number(v, 0), true),
                Slider("Rung camera khi đi", "Giảm xuống nếu thấy chóng mặt khi đi hoặc chạy.", 0, 1, .1f,
                    () => settings.headBob, v => settings.headBob = v, Percent),
                Slider("Độ sáng", "Tăng tới khi vừa thấy đồ vật trong chỗ tối, đừng để màu đen bị bạc. Nền tối mờ đi để thấy cảnh phía sau.", -1.25f, 1.25f, .25f,
                    () => settings.brightness, v => settings.brightness = v, v => Number(v / .25f + 5, 0), true),
                Slider("Âm lượng", "Âm lượng chung của game.", 0, 1, .05f,
                    () => settings.volume, v => settings.volume = v, Percent),
            };
            if (Session != null)
            {
                int last = QualitySettings.names.Length - 1;
                rows.Add(Slider("Chất lượng đồ họa", "Giảm xuống nếu game chạy giật trên máy yếu.", 0, last, 1,
                    () => Session.Quality, v => Session.Quality = Mathf.RoundToInt(v), v => QualitySettings.names[Mathf.Clamp(Mathf.RoundToInt(v), 0, last)]));
                rows.Add(Toggle("Cảnh báo nội dung", "Hiện cảnh báo nội dung ở màn hình đầu khi mở game.",
                    () => Session.ShowWarning ? 1 : 0, v => Session.ShowWarning = v > .5f));
            }
            rows.Add(new Setting { name = "Khôi phục mặc định", hint = "Đưa mọi cài đặt ở trên về như lúc đầu.", kind = Kind.Action, run = ResetDefaults });
            for (int i = 0; i < rows.Count; i++) MakeRow(rows[i], i);
            hint = Words(settingsPage.transform, "Hint", "", 22, new Color(Paper.r, Paper.g, Paper.b, .6f));
            hint.textWrappingMode = TextWrappingModes.Normal; hint.alignment = TextAlignmentOptions.TopLeft;
            GameUi.Place(hint.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(940, 186 - rows.Count * 64), new Vector2(800, 80));
            RefreshRows();

            controlsPage = Page(lightRect, "Điều khiển");
            var controls = new (string action, string keys)[]
            {
                ("Đi lại", "W   A   S   D"), ("Nhìn quanh", "Chuột hoặc phím mũi tên"), ("Chạy", "Shift"), ("Cúi người", "C"),
                ("Tương tác", "E"), ("Điện thoại", "1   2"), ("Tạm dừng", "Esc"),
            };
            for (int i = 0; i < controls.Length; i++)
            {
                float y = 230 - i * 64;
                var label = Words(controlsPage.transform, "Action " + controls[i].action, controls[i].action, 26, new Color(Paper.r, Paper.g, Paper.b, .8f));
                GameUi.Place(label.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(940, y), new Vector2(400, 50));
                var keys = Words(controlsPage.transform, "Keys " + controls[i].action, controls[i].keys, 24, new Color(Warm.r, Warm.g, Warm.b, .85f), true, TextAlignmentOptions.Right);
                GameUi.Place(keys.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(1240, y), new Vector2(500, 50));
            }

            mainFooter = Footer(root, "W S chọn", "Enter mở", "Esc chơi tiếp");
            startFooter = Footer(root, "W S chọn", "Enter mở");
            startFooter.alpha = 0;
            pageFooter = Footer(root, "W S chọn", "A D chỉnh", "Esc quay lại");
            pageFooter.alpha = 0;

            darkHint = Words(root, "Dark hint", "Di chuột hoặc bấm phím để bật đèn", 20, new Color(Paper.r, Paper.g, Paper.b, .35f), false, TextAlignmentOptions.Center);
            GameUi.Place(darkHint.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(900, 40));
            darkHint.alpha = 0;

            grain = GameUi.Raw(root, "Grain", GameUi.Noise(), new Color(1, 1, 1, .025f)); GameUi.Fill(grain.rectTransform);
            canvas.gameObject.SetActive(false);
        }

        static string Number(float value, int decimals) =>
            value.ToString(decimals == 0 ? "0" : "0.0", CultureInfo.InvariantCulture).Replace('.', ',');

        static string Percent(float value) => Mathf.RoundToInt(value * 100) + "%";

        Entry MakeEntry(RectTransform parent, int index)
        {
            float y = 190 - index * 84;
            var stripe = StripeImage(parent, "Light " + index, new Vector2(110, y), new Vector2(760, 110));
            var rect = GameUi.Place(GameUi.Box(parent, "Entry " + index, Color.clear), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(150, y), new Vector2(620, 70));
            var text = Words(rect, "Label", "", 38, Paper);
            text.characterSpacing = 2;
            var labelRect = GameUi.Place(text.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(20, 0), new Vector2(600, 70));
            return new Entry { stripe = stripe, rect = rect, label = labelRect, text = text };
        }

        static CanvasGroup Page(RectTransform parent, string heading)
        {
            var page = GameUi.Fill(GameUi.Box(parent, "Page " + heading, Color.clear));
            var group = page.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
            var title = Words(page, "Heading", heading, 20, new Color(Paper.r, Paper.g, Paper.b, .45f));
            title.characterSpacing = 12;
            GameUi.Place(title.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(940, 300), new Vector2(400, 30));
            GameUi.Place(GameUi.Box(page, "Rule", new Color(Paper.r, Paper.g, Paper.b, .12f)), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(940, 272), new Vector2(800, 1));
            return group;
        }

        static Setting Slider(string name, string hint, float min, float max, float step, Func<float> get, Action<float> set, Func<float, string> format, bool peek = false) =>
            new Setting { name = name, hint = hint, kind = Kind.Slider, min = min, max = max, step = step, get = get, set = set, format = format, peek = peek };

        static Setting Toggle(string name, string hint, Func<float> get, Action<float> set) =>
            new Setting { name = name, hint = hint, kind = Kind.Toggle, min = 0, max = 1, step = 1, get = get, set = set };

        void MakeRow(Setting s, int index)
        {
            float y = 230 - index * 64;
            s.stripe = StripeImage(settingsPage.transform, "Light " + s.name, new Vector2(880, y), new Vector2(900, 92));
            s.rect = GameUi.Place(GameUi.Box(settingsPage.transform, "Row " + s.name, Color.clear), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(920, y), new Vector2(840, 56));
            s.text = Words(s.rect, "Label", s.name, 26, Paper);
            GameUi.Place(s.text.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(20, 0), new Vector2(400, 56));
            if (s.kind == Kind.Action) return;
            s.minus = Glyph(s.rect, "<", 430, out s.minusText);
            s.plus = Glyph(s.rect, ">", 716, out s.plusText);
            if (s.kind == Kind.Slider)
            {
                var track = GameUi.Place(GameUi.Box(s.rect, "Track", new Color(Paper.r, Paper.g, Paper.b, .16f)), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(480, 0), new Vector2(TrackWidth, 2));
                s.fill = GameUi.Place(GameUi.Box(track, "Fill", Warm), new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(0, 2));
                s.handle = GameUi.Place(GameUi.Box(track, "Handle", Paper), new Vector2(0, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(3, 18));
                s.value = Words(s.rect, "Value", "", 24, Paper, true, TextAlignmentOptions.Right);
                GameUi.Place(s.value.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(760, 0), new Vector2(80, 56));
            }
            else
            {
                s.value = Words(s.rect, "Value", "", 24, Paper, true, TextAlignmentOptions.Center);
                GameUi.Place(s.value.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(480, 0), new Vector2(TrackWidth, 56));
            }
        }

        static RectTransform Glyph(RectTransform parent, string label, float x, out TextMeshProUGUI text)
        {
            var hit = GameUi.Place(GameUi.Box(parent, "Button " + label, Color.clear), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(x, 0), new Vector2(36, 56));
            text = Words(hit, "Glyph", label, 22, Paper, false, TextAlignmentOptions.Center);
            GameUi.Fill(text.rectTransform);
            return hit;
        }

        static CanvasGroup Footer(Transform root, params string[] items)
        {
            var footer = GameUi.Place(GameUi.Box(root, "Footer", Color.clear), Vector2.zero, Vector2.zero, new Vector2(162, 64), new Vector2(1000, 30));
            var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 44; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            foreach (var item in items)
            {
                var text = Words(footer, item, item, 19, new Color(Paper.r, Paper.g, Paper.b, .4f));
                text.rectTransform.sizeDelta = new Vector2(text.GetPreferredValues(item).x + 4, 30);
            }
            return footer.gameObject.AddComponent<CanvasGroup>();
        }

        void Layout()
        {
            items.Clear();
            var session = Session;
            if (AtStart)
            {
                if (session.HasSave) Add("Tiếp tục bản đã lưu", () => session.ContinueSaved());
                Add("Chơi mới Đêm 1", () => session.StartNewGame(), session.HasSave ? "Nhấn lần nữa để chơi lại từ đầu" : null);
            }
            else
            {
                Add("Tiếp tục", player.ResumeControl);
                if (session != null) Add("Chơi lại Đêm 1", () => session.StartNewGame(), "Nhấn lần nữa để chơi lại từ đầu");
            }
            settingsIndex = items.Count; Add("Cài đặt", null);
            controlsIndex = items.Count; Add("Điều khiển", null);
            if (AtStart && session.HasSave) Add("Xóa tiến độ", () => session.DeleteProgress(), "Nhấn lần nữa để xóa tiến độ");
            Add(session != null && !AtStart ? "Lưu và thoát" : "Thoát game", QuitGame, "Nhấn lần nữa để thoát");
            for (int i = 0; i < entries.Length; i++)
            {
                bool used = i < items.Count;
                entries[i].rect.gameObject.SetActive(used); entries[i].stripe.gameObject.SetActive(used);
                if (used) entries[i].text.text = items[i].label;
            }
            selected = Mathf.Clamp(selected, 0, items.Count - 1);
            confirmIndex = -1;
        }

        void Add(string label, Action run, string confirm = null) => items.Add(new Item { label = label, run = run, confirm = confirm });

        public void ShowSettings()
        {
            if (group == null) Build();
            Layout(); selected = settingsIndex; inPage = true; row = 0;
        }

        void Update()
        {
            if (group == null)
            {
                if (!player.IsPaused) return;
                Build();
            }
            bool show = player.IsPaused && !player.HasPauseMenu && player.ActivePhone == 0;
            if (show && !shown) Open();
            if (!show && shown) Close();
            shown = show;
            float dt = Time.unscaledDeltaTime;
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1 : 0, dt * 6);
            if (!show && group.alpha <= 0)
            {
                if (group.gameObject.activeSelf) group.gameObject.SetActive(false);
                return;
            }
            if (show && !player.PauseChangedThisFrame)
            {
                if (AnyInput())
                {
                    lastInput = Time.unscaledTime;
                    if (!lightsOn) { SwitchLights(true); }
                    else HandleInput();
                }
                else if (lightsOn && Time.unscaledTime - lastInput > IdleSeconds) SwitchLights(false);
            }
            player.PauseMenuLocked = show && (inPage || leftPageFrame == Time.frameCount || AtStart);
            if (confirmIndex >= 0 && Time.unscaledTime > quitUntil) confirmIndex = -1;
            Animate(dt);
        }

        static bool AnyInput()
        {
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            return (keys != null && keys.anyKey.isPressed) ||
                   (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0 || mouse.leftButton.isPressed || mouse.scroll.ReadValue().sqrMagnitude > 0));
        }

        void Open()
        {
            bool keepPage = inPage;
            selected = 0; dragging = -1;
            Layout();
            if (keepPage) selected = settingsIndex;
            inPage = keepPage;
            if (Session != null) { Session.ImportSettings(settings); settings.Apply(player); RefreshRows(); }
            group.gameObject.SetActive(true);
            lastInput = Time.unscaledTime;
            lightLevel = 0;
            SwitchLights(true);
        }

        void Close()
        {
            inPage = false; dragging = -1;
            player.PauseMenuLocked = false;
            lightsOn = false;
            hum.Stop();
            SaveIfDirty();
        }

        void SwitchLights(bool on)
        {
            if (lightsOn == on) return;
            lightsOn = on;
            clicks.pitch = on ? 1 : .82f;
            clicks.PlayOneShot(MenuSounds.Relay, on ? 1f : .7f);
            if (on) { flickerAt = Time.unscaledTime; if (!hum.isPlaying) hum.Play(); }
        }

        void SaveIfDirty()
        {
            if (!dirty) return;
            settings.Save(); dirty = false;
            Session?.SaveSettings(settings);
        }

        void Select(int index)
        {
            if (index == selected) return;
            confirmIndex = -1;
            selected = index;
            Tick();
        }

        void Tick() => clicks.PlayOneShot(MenuSounds.Tick, .6f);

        void Activate(int index)
        {
            if (index == settingsIndex) { inPage = true; Tick(); return; }
            var item = items[index];
            if (item.run == null) return;
            if (item.confirm != null && confirmIndex != index) { confirmIndex = index; quitUntil = Time.unscaledTime + 4; Tick(); return; }
            confirmIndex = -1;
            Tick();
            item.run();
            if (shown && player.IsPaused) Layout();
        }

        void LeavePage()
        {
            if (!inPage) return;
            inPage = false; dragging = -1; leftPageFrame = Time.frameCount;
            SaveIfDirty();
        }

        void QuitGame()
        {
            SaveIfDirty();
            if (Session != null && !Session.PrepareQuit()) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void HandleInput()
        {
            var mouse = Mouse.current;
            bool moved = mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0;
            for (int i = 0; i < items.Count; i++)
                if (GameUi.PointerOver(entries[i].rect, out bool clicked))
                {
                    if (moved || clicked) { Select(i); LeavePage(); }
                    if (clicked) Activate(i);
                }
            if (selected == settingsIndex)
            {
                for (int i = 0; i < rows.Count; i++)
                    if (GameUi.PointerOver(rows[i].rect, out bool clicked))
                    {
                        if (moved || clicked) { if (!inPage || row != i) Tick(); inPage = true; row = i; }
                        if (clicked) Click(rows[i], i, mouse.position.ReadValue());
                    }
                if (dragging >= 0)
                {
                    if (mouse != null && mouse.leftButton.isPressed) SetFromPointer(rows[dragging], mouse.position.ReadValue());
                    else dragging = -1;
                }
            }

            var keys = Keyboard.current;
            if (keys == null) return;
            bool up = keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame;
            bool down = keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame;
            bool confirm = keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame || keys.eKey.wasPressedThisFrame;
            bool right = keys.dKey.isPressed || keys.rightArrowKey.isPressed;
            bool left = keys.aKey.isPressed || keys.leftArrowKey.isPressed;
            bool fresh = keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame;
            if (!inPage)
            {
                if (up) Select((selected + items.Count - 1) % items.Count);
                if (down) Select((selected + 1) % items.Count);
                if (confirm || (selected == settingsIndex && fresh && right)) Activate(selected);
                return;
            }
            if (up) { row = (row + rows.Count - 1) % rows.Count; Tick(); }
            if (down) { row = (row + 1) % rows.Count; Tick(); }
            var current = rows[row];
            int direction = right ? 1 : left ? -1 : 0;
            if (direction != 0 && current.kind != Kind.Action)
            {
                if (fresh) { Change(current, direction); repeatAt = Time.unscaledTime + .38f; }
                else if (current.kind == Kind.Slider && Time.unscaledTime >= repeatAt) { Change(current, direction); repeatAt = Time.unscaledTime + .07f; }
            }
            if (confirm)
            {
                if (current.kind == Kind.Toggle) Change(current, 1);
                else if (current.kind == Kind.Action) current.run();
            }
            if (keys.escapeKey.wasPressedThisFrame || keys.backspaceKey.wasPressedThisFrame) LeavePage();
        }

        static bool Contains(RectTransform rect, Vector2 point) => RectTransformUtility.RectangleContainsScreenPoint(rect, point);

        void Click(Setting s, int index, Vector2 point)
        {
            if (s.kind == Kind.Action) { s.run(); return; }
            if (Contains(s.minus, point)) Change(s, -1);
            else if (Contains(s.plus, point)) Change(s, 1);
            else if (s.kind == Kind.Toggle) Change(s, 1);
            else { dragging = index; SetFromPointer(s, point); }
        }

        void Change(Setting s, int direction)
        {
            if (s.kind == Kind.Toggle) { s.set(s.get() > .5f ? 0 : 1); Changed(); return; }
            Assign(s, s.get() + direction * s.step);
        }

        void SetFromPointer(Setting s, Vector2 point)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(s.rect, point, null, out var local);
            float t = Mathf.Clamp01((local.x + s.rect.rect.width * s.rect.pivot.x - 480) / TrackWidth);
            Assign(s, Mathf.Lerp(s.min, s.max, t));
        }

        void Assign(Setting s, float value)
        {
            float snapped = Mathf.Clamp(s.min + Mathf.Round((value - s.min) / s.step) * s.step, s.min, s.max);
            if (Mathf.Approximately(snapped, s.get())) return;
            s.set(snapped);
            Changed();
        }

        void Changed()
        {
            settings.Apply(player);
            dirty = true;
            Tick();
            RefreshRows();
        }

        void ResetDefaults()
        {
            settings = defaults.Copy();
            Changed();
        }

        void RefreshRows()
        {
            foreach (var s in rows)
            {
                if (s.kind == Kind.Action) continue;
                float value = s.get();
                float t = s.kind == Kind.Toggle ? value : Mathf.InverseLerp(s.min, s.max, value);
                if (s.fill != null) { s.fill.sizeDelta = new Vector2(TrackWidth * t, 2); s.handle.anchoredPosition = new Vector2(TrackWidth * t, 0); }
                s.value.text = s.kind == Kind.Toggle ? value > .5f ? "Bật" : "Tắt" : s.format(value);
                bool atMin = s.kind == Kind.Slider && value <= s.min + s.step * .01f;
                bool atMax = s.kind == Kind.Slider && value >= s.max - s.step * .01f;
                s.minusText.alpha = atMin ? .15f : .6f;
                s.plusText.alpha = atMax ? .15f : .6f;
            }
        }

        float Flicker()
        {
            float t = Time.unscaledTime - flickerAt;
            if (t < .04f) return .9f;
            if (t < .09f) return .1f;
            if (t < .14f) return 1;
            if (t < .19f) return .35f;
            return 1;
        }

        void Animate(float dt)
        {
            lightLevel = Mathf.MoveTowards(lightLevel, lightsOn ? 1 : 0, dt * (lightsOn ? 12 : 2.5f));
            bool peek = inPage && selected == settingsIndex && rows[row].peek;
            light.alpha = lightLevel * (lightsOn ? Flicker() : 1);
            ceiling.alpha = Mathf.MoveTowards(ceiling.alpha, peek ? .12f : 1, dt * 4);
            hum.volume = Mathf.MoveTowards(hum.volume, lightsOn && shown ? .08f : 0, dt * .5f);
            if (!lightsOn && hum.volume <= 0 && hum.isPlaying) hum.Stop();

            for (int i = 0; i < items.Count; i++)
            {
                var entry = entries[i];
                float target = i == selected ? inPage ? .55f : 1 : 0;
                entry.glow = Mathf.MoveTowards(entry.glow, target, dt * 6);
                entry.stripe.color = new Color(Warm.r, Warm.g, Warm.b, entry.glow * .22f);
                entry.label.anchoredPosition = new Vector2(20 + entry.glow * 12, 0);
                entry.text.alpha = .34f + entry.glow * .66f;
                bool confirming = i == confirmIndex;
                entry.text.color = confirming ? new Color(1, .55f, .45f, entry.text.alpha) : new Color(Paper.r, Paper.g, Paper.b, entry.text.alpha);
                entry.text.text = confirming ? items[i].confirm : items[i].label;
            }
            paused.text = AtStart ? "" : "Tạm dừng";
            warning.alpha = AtStart && Session.ShowWarning ? 1 : 0;
            status.text = Session != null ? Session.Status : "";

            settingsPage.alpha = Mathf.MoveTowards(settingsPage.alpha, selected == settingsIndex ? inPage ? 1 : .6f : 0, dt * 6);
            controlsPage.alpha = Mathf.MoveTowards(controlsPage.alpha, selected == controlsIndex ? 1 : 0, dt * 6);
            for (int i = 0; i < rows.Count; i++)
            {
                var s = rows[i];
                s.glow = Mathf.MoveTowards(s.glow, inPage && i == row ? 1 : 0, dt * 8);
                s.stripe.color = new Color(Warm.r, Warm.g, Warm.b, s.glow * .16f);
                s.text.alpha = .5f + s.glow * .5f;
            }
            hint.text = inPage ? rows[row].hint : "";
            darkHint.alpha = Mathf.MoveTowards(darkHint.alpha, lightsOn ? 0 : .35f, dt * (lightsOn ? 8 : .4f));
            mainFooter.alpha = Mathf.MoveTowards(mainFooter.alpha, inPage || AtStart ? 0 : 1, dt * 7);
            startFooter.alpha = Mathf.MoveTowards(startFooter.alpha, !inPage && AtStart ? 1 : 0, dt * 7);
            pageFooter.alpha = Mathf.MoveTowards(pageFooter.alpha, inPage ? 1 : 0, dt * 7);

            if (Time.unscaledTime >= grainAt)
            {
                grainAt = Time.unscaledTime + .06f;
                grain.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, Screen.width / 256f, Screen.height / 256f);
            }
        }
    }
}
