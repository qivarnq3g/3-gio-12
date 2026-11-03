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
        const int Resume = 0, Settings = 1, Controls = 2, Quit = 3;
        const float TrackWidth = 260;
        enum Kind { Slider, Toggle, Action }

        sealed class Entry
        {
            public RectTransform rect, label, bar;
            public CanvasGroup band;
            public TextMeshProUGUI text;
            public float slide;
        }

        sealed class Setting
        {
            public string name, hint;
            public Kind kind;
            public float min, max, step;
            public Func<float> get;
            public Action<float> set;
            public Func<float, string> format;
            public Action run;
            public RectTransform rect, bar, minus, plus, track, fill, handle;
            public CanvasGroup band;
            public TextMeshProUGUI text, value, minusText, plusText;
            public float slide;
        }

        FirstPersonPlayer player;
        GameSettings settings, defaults;
        CanvasGroup group, settingsPage, controlsPage, mainFooter, pageFooter;
        RawImage backdrop, grain;
        Texture2D blurred, bandTexture;
        TextMeshProUGUI hint;
        Entry[] entries;
        List<Setting> rows;
        int selected, row, dragging = -1, leftPageFrame = -1;
        bool shown, inPage, dirty, recapture, confirmQuit;
        float repeatAt, quitUntil, grainAt;

        void Awake() { player = GetComponent<FirstPersonPlayer>(); }

        void Start()
        {
            defaults = new GameSettings { sensitivity = player.sensitivity, fieldOfView = player.eyes != null ? player.eyes.fieldOfView : 70 };
            settings = GameSettings.Load(defaults);
            settings.Apply(player);
            Build();
        }

        void OnDestroy()
        {
            if (blurred != null) Destroy(blurred);
            if (player != null) player.PauseMenuLocked = false;
        }

        void Build()
        {
            var canvas = GameUi.Canvas(transform, "Pause menu", 100);
            group = canvas.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            var root = canvas.transform;
            bandTexture = GameUi.Gradient(new Color(1, 1, 1, .09f), new Color(1, 1, 1, 0));

            GameUi.Fill(GameUi.Box(root, "Base", new Color(.01f, .01f, .012f, 1)));
            backdrop = GameUi.Raw(root, "Backdrop", null, Color.white); GameUi.Fill(backdrop.rectTransform); backdrop.enabled = false;
            GameUi.Fill(GameUi.Raw(root, "Wash", GameUi.Gradient(new Color(0, 0, 0, .74f), new Color(0, 0, 0, .3f)), Color.white).rectTransform);
            GameUi.Fill(GameUi.Raw(root, "Floor", GameUi.Gradient(new Color(0, 0, 0, .5f), new Color(0, 0, 0, 0), true), Color.white).rectTransform);
            grain = GameUi.Raw(root, "Grain", GameUi.Noise(), new Color(1, 1, 1, .022f)); GameUi.Fill(grain.rectTransform);

            var column = GameUi.Place(GameUi.Box(root, "Column", Color.clear), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(150, 0), new Vector2(620, 760));
            var kicker = GameUi.Text(column, "Kicker", "TẠM DỪNG", 20, GameUi.Accent);
            kicker.characterSpacing = 30; Top(kicker, 0, 0, 600, 30);
            var title = GameUi.Text(column, "Title", "3 GIỜ 12", 118, GameUi.Ink);
            title.fontStyle = FontStyles.Bold; title.characterSpacing = 4; Top(title, -6, -34, 660, 140);
            GameUi.Place(GameUi.Box(column, "Rule", GameUi.Accent), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -190), new Vector2(72, 3));
            entries = new[]
            {
                MakeEntry(column, "Tiếp tục", Resume),
                MakeEntry(column, "Cài đặt", Settings),
                MakeEntry(column, "Điều khiển", Controls),
                MakeEntry(column, "Thoát game", Quit),
            };

            var panel = GameUi.Place(GameUi.Box(root, "Panel", Color.clear), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(880, 0), new Vector2(920, 760));
            settingsPage = Page(panel, "CÀI ĐẶT");
            rows = new List<Setting>
            {
                Slider("Độ nhạy chuột", "Tốc độ xoay góc nhìn khi di chuột.", .02f, .3f, .01f,
                    () => settings.sensitivity, v => settings.sensitivity = v, v => Number(v * 50, 1)),
                Toggle("Đảo trục dọc", "Đẩy chuột lên thì nhìn xuống.",
                    () => settings.invertY ? 1 : 0, v => settings.invertY = v > .5f),
                Slider("Góc nhìn", "Rộng hơn thì thấy nhiều hơn, nhưng rìa hình bị kéo giãn.", 55, 95, 1,
                    () => settings.fieldOfView, v => settings.fieldOfView = v, v => Number(v, 0)),
                Slider("Rung camera khi đi", "Giảm xuống nếu thấy chóng mặt khi đi hoặc chạy.", 0, 1, .1f,
                    () => settings.headBob, v => settings.headBob = v, Percent),
                Slider("Độ sáng", "Tăng tới khi vừa thấy đồ vật trong chỗ tối, đừng để màu đen bị bạc.", -1.25f, 1.25f, .25f,
                    () => settings.brightness, v => settings.brightness = v, v => Number(v / .25f + 5, 0)),
                Slider("Âm lượng", "Âm lượng chung của game.", 0, 1, .05f,
                    () => settings.volume, v => settings.volume = v, Percent),
                new Setting { name = "Khôi phục mặc định", hint = "Đưa mọi cài đặt ở trên về như lúc đầu.", kind = Kind.Action, run = ResetDefaults },
            };
            for (int i = 0; i < rows.Count; i++) MakeRow(rows[i], i);
            hint = GameUi.Text(settingsPage.transform, "Hint", "", 24, GameUi.Muted);
            hint.textWrappingMode = TextWrappingModes.Normal; Top(hint, 0, -600, 860, 80);
            hint.alignment = TextAlignmentOptions.TopLeft;
            RefreshRows();

            controlsPage = Page(panel, "ĐIỀU KHIỂN");
            var list = GameUi.Place(GameUi.Box(controlsPage.transform, "List", Color.clear), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -84), new Vector2(600, 520));
            var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 22; listLayout.childAlignment = TextAnchor.UpperLeft;
            listLayout.childControlWidth = listLayout.childControlHeight = false; listLayout.childForceExpandWidth = listLayout.childForceExpandHeight = false;
            GameUi.ControlRow(list, "Đi lại", 42, "W", "A", "S", "D");
            GameUi.ControlRow(list, "Nhìn quanh", 42, "Chuột");
            GameUi.ControlRow(list, "Chạy", 42, "Shift");
            GameUi.ControlRow(list, "Cúi người", 42, "C");
            GameUi.ControlRow(list, "Tương tác", 42, "E");
            GameUi.ControlRow(list, "Điện thoại", 42, "1", "2");
            GameUi.ControlRow(list, "Tạm dừng", 42, "Esc");

            mainFooter = Footer(root, ("Chọn", new[] { "W", "S" }), ("Mở", new[] { "Enter" }), ("Tiếp tục", new[] { "Esc" }));
            pageFooter = Footer(root, ("Chọn", new[] { "W", "S" }), ("Chỉnh", new[] { "A", "D" }), ("Quay lại", new[] { "Esc" }));
            pageFooter.alpha = 0;
            canvas.gameObject.SetActive(false);
        }

        static void Top(Graphic graphic, float x, float y, float width, float height)
        {
            GameUi.Place(graphic.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, y), new Vector2(width, height));
        }

        static string Number(float value, int decimals) =>
            value.ToString(decimals == 0 ? "0" : "0.0", CultureInfo.InvariantCulture).Replace('.', ',');

        static string Percent(float value) => Mathf.RoundToInt(value * 100) + "%";

        CanvasGroup Band(RectTransform parent)
        {
            var image = GameUi.Raw(parent, "Band", bandTexture, Color.white);
            GameUi.Fill(image.rectTransform);
            var band = image.gameObject.AddComponent<CanvasGroup>(); band.alpha = 0;
            return band;
        }

        Entry MakeEntry(RectTransform column, string label, int index)
        {
            var rect = GameUi.Place(GameUi.Box(column, "Entry " + label, Color.clear), new Vector2(0, 1), new Vector2(0, 1), new Vector2(-24, -250 - index * 74), new Vector2(600, 64));
            var band = Band(rect);
            var bar = GameUi.Place(GameUi.Box(rect, "Bar", GameUi.Accent), new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(4, 38));
            var number = GameUi.Text(rect, "Number", (index + 1).ToString("00"), 17, GameUi.Faint);
            GameUi.Place(number.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(26, 2), new Vector2(40, 64));
            var text = GameUi.Text(rect, "Label", label, 40, GameUi.Muted);
            var labelRect = GameUi.Place(text.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(70, 0), new Vector2(520, 64));
            return new Entry { rect = rect, label = labelRect, bar = bar, band = band, text = text };
        }

        static CanvasGroup Page(RectTransform panel, string heading)
        {
            var page = GameUi.Fill(GameUi.Box(panel, "Page " + heading, Color.clear));
            var group = page.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
            var title = GameUi.Text(page, "Heading", heading, 20, GameUi.Muted);
            title.characterSpacing = 18; Top(title, 0, 0, 600, 30);
            GameUi.Place(GameUi.Box(page, "Line", GameUi.Faint), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -46), new Vector2(900, 1));
            return group;
        }

        static Setting Slider(string name, string hint, float min, float max, float step, Func<float> get, Action<float> set, Func<float, string> format) =>
            new Setting { name = name, hint = hint, kind = Kind.Slider, min = min, max = max, step = step, get = get, set = set, format = format };

        static Setting Toggle(string name, string hint, Func<float> get, Action<float> set) =>
            new Setting { name = name, hint = hint, kind = Kind.Toggle, min = 0, max = 1, step = 1, get = get, set = set };

        void MakeRow(Setting s, int index)
        {
            s.rect = GameUi.Place(GameUi.Box(settingsPage.transform, "Row " + s.name, Color.clear), new Vector2(0, 1), new Vector2(0, 1), new Vector2(-24, -76 - index * 70), new Vector2(944, 60));
            s.band = Band(s.rect);
            s.bar = GameUi.Place(GameUi.Box(s.rect, "Bar", GameUi.Accent), new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(3, 32));
            s.text = GameUi.Text(s.rect, "Label", s.name, 28, GameUi.Muted);
            GameUi.Place(s.text.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(24, 0), new Vector2(420, 60));
            if (s.kind == Kind.Action) return;
            s.minus = ArrowButton(s.rect, "<", 468, out s.minusText);
            s.plus = ArrowButton(s.rect, ">", 808, out s.plusText);
            s.track = GameUi.Place(GameUi.Box(s.rect, "Track", Color.clear), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(530, 0), new Vector2(TrackWidth, 44));
            if (s.kind == Kind.Slider)
            {
                var line = GameUi.Place(GameUi.Box(s.track, "Line", GameUi.Faint), new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(TrackWidth, 4));
                s.fill = GameUi.Place(GameUi.Box(line, "Fill", GameUi.Accent), new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(0, 4));
                s.handle = GameUi.Place(GameUi.Box(line, "Handle", GameUi.Ink), new Vector2(0, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(6, 24));
                s.value = GameUi.Text(s.rect, "Value", "", 26, GameUi.Ink, TextAlignmentOptions.Right);
                GameUi.Place(s.value.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(858, 0), new Vector2(80, 60));
            }
            else
            {
                s.value = GameUi.Text(s.track, "Value", "", 28, GameUi.Ink, TextAlignmentOptions.Center);
                GameUi.Fill(s.value.rectTransform);
            }
        }

        static RectTransform ArrowButton(RectTransform parent, string label, float x, out TextMeshProUGUI text)
        {
            var button = GameUi.Place(GameUi.Box(parent, "Button " + label, new Color(1, 1, 1, .06f)), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(x, 0), new Vector2(46, 46));
            text = GameUi.Text(button, "Glyph", label, 24, GameUi.Ink, TextAlignmentOptions.Center);
            GameUi.Fill(text.rectTransform);
            return button;
        }

        static CanvasGroup Footer(Transform root, params (string action, string[] keys)[] items)
        {
            var footer = GameUi.Place(GameUi.Box(root, "Footer", Color.clear), Vector2.zero, Vector2.zero, new Vector2(150, 64), new Vector2(1000, 34));
            var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 38; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            foreach (var (action, keys) in items) GameUi.ControlRow(footer, action, 30, 0, keys);
            return footer.gameObject.AddComponent<CanvasGroup>();
        }

        void Update()
        {
            if (group == null) return;
            bool show = player.IsPaused && !player.HasPauseMenu && player.ActivePhone == 0;
            if (show && !shown) Open();
            if (!show && shown) Close();
            shown = show;
            float dt = Time.unscaledDeltaTime;
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1 : 0, dt * 6);
            if (!show && group.alpha <= 0) { if (group.gameObject.activeSelf) group.gameObject.SetActive(false); return; }
            if (show && !player.PauseChangedThisFrame) HandleInput();
            player.PauseMenuLocked = show && (inPage || leftPageFrame == Time.frameCount);
            if (confirmQuit && Time.unscaledTime > quitUntil) confirmQuit = false;
            Animate(dt);
        }

        void LateUpdate()
        {
            if (!shown || !recapture) return;
            recapture = false;
            Capture();
        }

        void Open()
        {
            selected = Resume; inPage = false; confirmQuit = false; dragging = -1;
            group.gameObject.SetActive(true);
            recapture = true;
        }

        void Close()
        {
            inPage = false; dragging = -1;
            player.PauseMenuLocked = false;
            SaveIfDirty();
        }

        void SaveIfDirty()
        {
            if (!dirty) return;
            settings.Save(); dirty = false;
        }

        void Select(int index)
        {
            if (index != selected) confirmQuit = false;
            selected = index;
        }

        void Activate(int index)
        {
            if (index == Resume) player.ResumeControl();
            else if (index == Settings) inPage = true;
            else if (index == Quit)
            {
                if (confirmQuit) QuitGame();
                else { confirmQuit = true; quitUntil = Time.unscaledTime + 4; }
            }
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
            for (int i = 0; i < entries.Length; i++)
                if (GameUi.PointerOver(entries[i].rect, out bool clicked))
                {
                    if (moved || clicked) { Select(i); LeavePage(); }
                    if (clicked) Activate(i);
                }
            if (selected == Settings)
            {
                for (int i = 0; i < rows.Count; i++)
                    if (GameUi.PointerOver(rows[i].rect, out bool clicked))
                    {
                        if (moved || clicked) { inPage = true; row = i; }
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
                if (up) Select((selected + entries.Length - 1) % entries.Length);
                if (down) Select((selected + 1) % entries.Length);
                if (confirm || (selected == Settings && fresh && right)) Activate(selected);
                return;
            }
            if (up) row = (row + rows.Count - 1) % rows.Count;
            if (down) row = (row + 1) % rows.Count;
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
            else if (Contains(s.track, point)) { dragging = index; SetFromPointer(s, point); }
        }

        void Change(Setting s, int direction)
        {
            if (s.kind == Kind.Toggle) { s.set(s.get() > .5f ? 0 : 1); Changed(); return; }
            Assign(s, s.get() + direction * s.step);
        }

        void SetFromPointer(Setting s, Vector2 point)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(s.track, point, null, out var local);
            float t = Mathf.Clamp01(local.x / TrackWidth);
            Assign(s, Mathf.Lerp(s.min, s.max, t));
        }

        void Assign(Setting s, float value)
        {
            float snapped = s.min + Mathf.Round((value - s.min) / s.step) * s.step;
            snapped = Mathf.Clamp(snapped, s.min, s.max);
            if (Mathf.Approximately(snapped, s.get())) return;
            s.set(snapped);
            Changed();
        }

        void Changed()
        {
            settings.Apply(player);
            dirty = true; recapture = true;
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
                bool atMin = value <= s.min + s.step * .01f, atMax = value >= s.max - s.step * .01f;
                if (s.kind == Kind.Toggle)
                {
                    s.value.text = value > .5f ? "Bật" : "Tắt";
                    atMin = atMax = false;
                }
                else
                {
                    float t = Mathf.InverseLerp(s.min, s.max, value);
                    s.fill.sizeDelta = new Vector2(TrackWidth * t, 4);
                    s.handle.anchoredPosition = new Vector2(TrackWidth * t, 0);
                    s.value.text = s.format(value);
                }
                s.minusText.color = atMin ? GameUi.Faint : GameUi.Ink;
                s.plusText.color = atMax ? GameUi.Faint : GameUi.Ink;
            }
        }

        void Animate(float dt)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                float target = i == selected ? inPage ? .45f : 1 : 0;
                entry.slide = Mathf.MoveTowards(entry.slide, target, dt * 8);
                entry.label.anchoredPosition = new Vector2(70 + entry.slide * 14, 0);
                entry.text.color = Color.Lerp(GameUi.Muted, i == Quit && confirmQuit ? GameUi.Accent : GameUi.Ink, Mathf.Max(entry.slide, i == selected ? 1 : 0));
                entry.bar.localScale = new Vector3(1, entry.slide, 1);
                entry.band.alpha = entry.slide;
            }
            entries[Quit].text.text = confirmQuit ? "Nhấn lần nữa để thoát" : "Thoát game";

            settingsPage.alpha = Mathf.MoveTowards(settingsPage.alpha, selected == Settings ? inPage ? 1 : .7f : 0, dt * 7);
            controlsPage.alpha = Mathf.MoveTowards(controlsPage.alpha, selected == Controls ? 1 : 0, dt * 7);
            for (int i = 0; i < rows.Count; i++)
            {
                var s = rows[i];
                s.slide = Mathf.MoveTowards(s.slide, inPage && i == row ? 1 : 0, dt * 9);
                s.band.alpha = s.slide;
                s.bar.localScale = new Vector3(1, s.slide, 1);
                s.text.color = Color.Lerp(GameUi.Muted, GameUi.Ink, s.slide);
            }
            hint.text = inPage ? rows[row].hint : "";
            mainFooter.alpha = Mathf.MoveTowards(mainFooter.alpha, inPage ? 0 : 1, dt * 7);
            pageFooter.alpha = Mathf.MoveTowards(pageFooter.alpha, inPage ? 1 : 0, dt * 7);

            if (Time.unscaledTime >= grainAt)
            {
                grainAt = Time.unscaledTime + .06f;
                grain.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, Screen.width / 256f, Screen.height / 256f);
            }
        }

        void Capture()
        {
            var cam = player.eyes;
            if (cam == null || !cam.isActiveAndEnabled) return;
            int width = 384, height = Mathf.Max(1, Mathf.RoundToInt(width * (float)Screen.height / Mathf.Max(1, Screen.width)));
            var full = RenderTexture.GetTemporary(width * 2, height * 2, 24);
            var small = RenderTexture.GetTemporary(width, height, 0);
            var target = cam.targetTexture;
            cam.targetTexture = full; cam.Render(); cam.targetTexture = target;
            Graphics.Blit(full, small);
            var active = RenderTexture.active;
            RenderTexture.active = small;
            if (blurred == null || blurred.width != width || blurred.height != height)
            {
                if (blurred != null) Destroy(blurred);
                blurred = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            }
            blurred.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(full);
            RenderTexture.ReleaseTemporary(small);
            var pixels = blurred.GetPixels32();
            Soften(pixels, width, height);
            blurred.SetPixels32(pixels);
            blurred.Apply();
            backdrop.texture = blurred;
            backdrop.enabled = true;
        }

        static void Soften(Color32[] pixels, int width, int height)
        {
            int count = pixels.Length;
            var channels = new[] { new float[count], new float[count], new float[count] };
            var buffer = new float[count];
            for (int i = 0; i < count; i++)
            {
                var c = pixels[i];
                float luma = c.r * .299f + c.g * .587f + c.b * .114f;
                channels[0][i] = Mathf.Lerp(luma, c.r, .4f);
                channels[1][i] = Mathf.Lerp(luma, c.g, .4f);
                channels[2][i] = Mathf.Lerp(luma, c.b, .4f);
            }
            foreach (var channel in channels)
                for (int pass = 0; pass < 3; pass++) Blur(channel, buffer, width, height, 5);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = x / (width - 1f) * 2 - 1, dy = y / (height - 1f) * 2 - 1;
                    float k = .95f * (1 - .5f * Mathf.Clamp01((dx * dx + dy * dy) * .5f));
                    int i = y * width + x;
                    pixels[i] = new Color32((byte)Mathf.Clamp(channels[0][i] * k, 0, 255), (byte)Mathf.Clamp(channels[1][i] * k, 0, 255), (byte)Mathf.Clamp(channels[2][i] * k, 0, 255), 255);
                }
        }

        static void Blur(float[] data, float[] buffer, int width, int height, int radius)
        {
            float scale = 1f / (radius * 2 + 1);
            for (int y = 0; y < height; y++)
            {
                int start = y * width;
                float sum = 0;
                for (int k = -radius; k <= radius; k++) sum += data[start + Mathf.Clamp(k, 0, width - 1)];
                for (int x = 0; x < width; x++)
                {
                    buffer[start + x] = sum * scale;
                    sum += data[start + Mathf.Min(x + radius + 1, width - 1)] - data[start + Mathf.Max(x - radius, 0)];
                }
            }
            for (int x = 0; x < width; x++)
            {
                float sum = 0;
                for (int k = -radius; k <= radius; k++) sum += buffer[Mathf.Clamp(k, 0, height - 1) * width + x];
                for (int y = 0; y < height; y++)
                {
                    data[y * width + x] = sum * scale;
                    sum += buffer[Mathf.Min(y + radius + 1, height - 1) * width + x] - buffer[Mathf.Max(y - radius, 0) * width + x];
                }
            }
        }
    }
}
