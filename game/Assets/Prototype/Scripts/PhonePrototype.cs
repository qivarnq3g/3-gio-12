using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BaGio12.Prototype
{
    // Interactive in-game phone system: lock screen, home screen, Tic (chat),
    // Nimbo (photos), Ghi am (voice memos), Mosa (ARG/offline).
    // Supports opening/closing during gameplay, choice persistence, and scenario loading.
    public class PhonePrototype : MonoBehaviour
    {
        public static PhonePrototype Instance { get; private set; }

        public static event Action<bool> OnPhoneVisibilityChanged;
        public static event Action<string, string> OnChoiceSelected; // contactId, choiceText
        public static event Action<string> OnMessageReceived;

        static readonly Color Ink = new Color(0.95f, 0.95f, 0.97f);
        static readonly Color Dim = new Color(1f, 1f, 1f, 0.55f);
        static readonly Color Faint = new Color(1f, 1f, 1f, 0.32f);
        static readonly Color Surface = new Color(0.067f, 0.067f, 0.078f);
        static readonly Color Raised = new Color(0.115f, 0.115f, 0.13f);
        static readonly Color Hair = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color Accent = new Color(0.24f, 0.56f, 0.49f);
        static readonly Color AccentSoft = new Color(0.24f, 0.56f, 0.49f, 0.18f);
        static readonly Color Theirs = new Color(0.16f, 0.16f, 0.18f);
        static readonly Color WarningRed = new Color(0.86f, 0.24f, 0.22f);

        // Vector icons drawn with Painter2D (see DrawIcon)
        const string IcBack = "back", IcSearch = "search", IcCamera = "camera", IcLock = "lock",
                     IcSend = "send", IcLight = "light", IcAdd = "add", IcCall = "call",
                     IcMore = "more", IcMic = "mic", IcPlay = "play", IcPause = "pause",
                     IcClose = "close", IcPhone = "phone";

        PrototypeData data;
        Font uiFont, lightFont, boldFont;
        VisualElement rootContainer, phoneContainer, phone, screenHost;
        VisualElement hudPill;
        Label hudBadge, hudTime, clock;
        readonly Stack<System.Action> back = new Stack<System.Action>();
        readonly Dictionary<string, List<Message>> shown = new Dictionary<string, List<Message>>();
        // Per contact: the choice the player picked and the bubble it produced.
        readonly Dictionary<string, Choice> chose = new Dictionary<string, Choice>();
        readonly Dictionary<string, Message> mine = new Dictionary<string, Message>();
        Coroutine typing;
        Coroutine audioPlayingCoroutine;

        public bool IsOpen { get; private set; } = true;
        private string currentScenario = "Prototype/dem-1";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<PhonePrototype>() != null) return;
            var go = new GameObject("PhonePrototype");
            go.AddComponent<PhonePrototype>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            LoadScenarioData(currentScenario);
            LoadSavedChoices();

            uiFont = Resources.Load<Font>("Fonts/NotoSans-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            boldFont = Resources.Load<Font>("Fonts/NotoSans-Bold") ?? uiFont;
            lightFont = uiFont;

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1080, 1920);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 1f;
            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = settings;

            rootContainer = doc.rootVisualElement;
            rootContainer.style.flexGrow = 1;
            rootContainer.style.unityFontDefinition = FontDefinition.FromFont(uiFont);
            rootContainer.style.color = Ink;

            BuildHUD(rootContainer);
            BuildPhone(rootContainer);

            // Default display: Phone open
            SetPhoneOpen(true);
            ShowLock();
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            if (kb.tabKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
            {
                TogglePhone();
            }
            else if (kb.escapeKey.wasPressedThisFrame && IsOpen)
            {
                if (back.Count > 0)
                {
                    GoBack();
                }
                else
                {
                    ClosePhone();
                }
            }
        }

        public void LoadScenario(string scenarioResourcePath)
        {
            currentScenario = scenarioResourcePath;
            LoadScenarioData(currentScenario);
            shown.Clear();
            chose.Clear();
            mine.Clear();
            LoadSavedChoices();
            if (clock != null) clock.text = data.lockTime;
            if (hudTime != null) hudTime.text = data.lockTime + "  [Tab]";
            if (IsOpen) ShowHome();
            UpdateHudBadge();
        }

        void LoadScenarioData(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                asset = Resources.Load<TextAsset>("Prototype/chat-mau");
            }

            if (asset != null)
            {
                data = JsonUtility.FromJson<PrototypeData>(asset.text);
            }
            else
            {
                data = new PrototypeData { lockTime = "3:12", lockDate = "Thứ năm, 26 tháng 11" };
            }
        }

        // Scoped by scenario so night 2 can reuse contact ids (e.g. "me") without
        // inheriting night 1's answer.
        string ChoiceKey(Contact c)
        {
            string scenario = string.IsNullOrEmpty(data.scenarioId) ? currentScenario : data.scenarioId;
            return $"BaGio12_{scenario}_Choice_{c.id}";
        }

        static string ChoiceId(Choice ch) => string.IsNullOrEmpty(ch.id) ? ch.text : ch.id;

        void LoadSavedChoices()
        {
            if (data?.contacts == null) return;
            foreach (var c in data.contacts)
            {
                string key = ChoiceKey(c);
                if (!PlayerPrefs.HasKey(key) || c.choices == null) continue;
                string savedId = PlayerPrefs.GetString(key);
                var choice = c.choices.Find(ch => ChoiceId(ch) == savedId);
                if (choice == null) continue;  // script changed since the save; offer the choices again
                Pick(c, choice);
                // The conversation was already read through before the save, so restore all of it in order.
                var sh = Shown(c);
                sh.Clear();
                sh.AddRange(Script(c));
            }
        }

        void Pick(Contact c, Choice choice)
        {
            chose[c.id] = choice;
            mine[c.id] = new Message { from = "me", text = choice.text, time = LastTime(c) };
        }

        void SaveChoice(Contact c, Choice choice)
        {
            PlayerPrefs.SetString(ChoiceKey(c), ChoiceId(choice));
            PlayerPrefs.Save();
            OnChoiceSelected?.Invoke(c.id, choice.text);
            Debug.Log($"[PhonePrototype] Saved choice for {c.name}: {choice.text}");
        }

        // Everything this contact's chat should end up showing: the incoming messages,
        // then the player's answer and its replies once a choice is made.
        List<Message> Script(Contact c)
        {
            var all = new List<Message>(c.messages);
            if (chose.TryGetValue(c.id, out var choice))
            {
                all.Add(mine[c.id]);
                if (choice.replies != null) all.AddRange(choice.replies);
            }
            return all;
        }

        static string LastTime(Contact c) => c.messages.Count > 0 ? c.messages[c.messages.Count - 1].time : "";

        // ---------- Open / Close / HUD ----------
        public void TogglePhone() => SetPhoneOpen(!IsOpen);
        public void OpenPhone() => SetPhoneOpen(true);
        public void ClosePhone() => SetPhoneOpen(false);

        public void SetPhoneOpen(bool open)
        {
            IsOpen = open;
            if (phoneContainer != null)
            {
                phoneContainer.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (hudPill != null)
            {
                hudPill.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
                UpdateHudBadge();
            }

            if (open)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            OnPhoneVisibilityChanged?.Invoke(open);
        }

        void BuildHUD(VisualElement root)
        {
            hudPill = Row();
            hudPill.name = "hud-phone-pill";
            hudPill.style.position = Position.Absolute;
            hudPill.style.right = 36;
            hudPill.style.bottom = 36;
            hudPill.style.height = 70;
            hudPill.style.paddingLeft = 24;
            hudPill.style.paddingRight = 28;
            hudPill.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.88f);
            Radius(hudPill, 35);
            hudPill.style.borderTopWidth = hudPill.style.borderBottomWidth = hudPill.style.borderLeftWidth = hudPill.style.borderRightWidth = 2;
            hudPill.style.borderTopColor = hudPill.style.borderBottomColor = hudPill.style.borderLeftColor = hudPill.style.borderRightColor = new Color(0.24f, 0.56f, 0.49f, 0.6f);

            var phoneIc = Icon(IcPhone, 32, Accent);
            phoneIc.style.marginRight = 14;
            hudPill.Add(phoneIc);

            hudTime = T(data.lockTime + "  [Tab]", 26, Ink, boldFont);
            hudPill.Add(hudTime);

            hudBadge = new Label("1");
            hudBadge.style.fontSize = 20;
            hudBadge.style.color = Color.white;
            hudBadge.style.backgroundColor = WarningRed;
            hudBadge.style.marginLeft = 16;
            hudBadge.style.paddingLeft = 12;
            hudBadge.style.paddingRight = 12;
            hudBadge.style.paddingTop = 4;
            hudBadge.style.paddingBottom = 4;
            Radius(hudBadge, 14);
            hudBadge.style.unityFontDefinition = FontDefinition.FromFont(boldFont);
            hudPill.Add(hudBadge);

            hudPill.RegisterCallback<PointerUpEvent>(_ => OpenPhone());
            root.Add(hudPill);
        }

        int GetTotalUnreadCount()
        {
            int total = 0;
            if (data?.contacts != null)
            {
                foreach (var c in data.contacts)
                {
                    total += Mathf.Max(0, Script(c).Count - Shown(c).Count);
                }
            }
            return total;
        }

        void UpdateHudBadge()
        {
            if (hudBadge == null) return;
            int unread = GetTotalUnreadCount();
            if (unread > 0)
            {
                hudBadge.style.display = DisplayStyle.Flex;
                hudBadge.text = unread.ToString();
            }
            else
            {
                hudBadge.style.display = DisplayStyle.None;
            }
        }

        void BuildPhone(VisualElement root)
        {
            phoneContainer = new VisualElement();
            phoneContainer.name = "phone-container";
            phoneContainer.style.flexGrow = 1;
            phoneContainer.style.width = Length.Percent(100);
            phoneContainer.style.height = Length.Percent(100);
            phoneContainer.style.alignItems = Align.Center;
            phoneContainer.style.justifyContent = Justify.Center;
            phoneContainer.style.backgroundColor = new Color(0, 0, 0, 0.65f); // semi-transparent backdrop

            // Bezel
            var bezel = new VisualElement();
            bezel.style.backgroundColor = new Color(0.02f, 0.02f, 0.025f);
            Radius(bezel, 92);
            Pad(bezel, 16);
            bezel.style.borderTopWidth = bezel.style.borderBottomWidth = bezel.style.borderLeftWidth = bezel.style.borderRightWidth = 3;
            bezel.style.borderTopColor = bezel.style.borderBottomColor = bezel.style.borderLeftColor = bezel.style.borderRightColor = new Color(1, 1, 1, 0.08f);

            // Close button top-right above bezel
            var topBar = Row();
            topBar.style.position = Position.Absolute;
            topBar.style.top = 24;
            topBar.style.right = 36;
            topBar.style.paddingLeft = 20;
            topBar.style.paddingRight = 20;
            topBar.style.paddingTop = 10;
            topBar.style.paddingBottom = 10;
            topBar.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.85f);
            Radius(topBar, 20);
            var closeIcon = Icon(IcClose, 26, Dim);
            topBar.Add(closeIcon);
            var closeTxt = T(" Đóng [Tab]", 24, Dim, boldFont);
            closeTxt.style.marginLeft = 8;
            topBar.Add(closeTxt);
            topBar.RegisterCallback<PointerUpEvent>(_ => ClosePhone());
            phoneContainer.Add(topBar);

            phoneContainer.Add(bezel);
            phone = new VisualElement();
            phone.style.backgroundColor = Surface;
            phone.style.overflow = Overflow.Hidden;
            Radius(phone, 78);
            bezel.Add(phone);

            phoneContainer.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float h = phoneContainer.resolvedStyle.height * 0.94f;
                float w = phoneContainer.resolvedStyle.width * 0.94f;
                float ph = Mathf.Min(h, w * 19.5f / 9f) - 38;
                phone.style.height = ph;
                phone.style.width = ph * 9f / 19.5f;
            });

            screenHost = new VisualElement();
            screenHost.style.position = Position.Absolute;
            screenHost.style.left = screenHost.style.right = screenHost.style.top = screenHost.style.bottom = 0;
            phone.Add(screenHost);
            phone.Add(StatusBar());
            phone.Add(HomeIndicator());

            root.Add(phoneContainer);
        }

        // ---------- Chrome ----------
        VisualElement StatusBar()
        {
            var bar = Row();
            bar.style.position = Position.Absolute; bar.style.left = bar.style.right = bar.style.top = 0;
            bar.style.height = 92; bar.style.paddingLeft = 64; bar.style.paddingRight = 56; bar.style.paddingTop = 18;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.pickingMode = PickingMode.Ignore;
            clock = T(data.lockTime, 34, Ink, boldFont); bar.Add(clock);
            var right = Row(); right.pickingMode = PickingMode.Ignore;
            right.Add(T("4G", 26, Ink, boldFont));
            var sig = Row(); sig.style.alignItems = Align.FlexEnd; sig.style.marginLeft = 10; sig.style.height = 24;
            for (int i = 0; i < 4; i++) { var b = new VisualElement(); b.style.width = 6; b.style.height = 8 + i * 5; b.style.marginLeft = 3; Radius(b, 2); b.style.backgroundColor = i < 2 ? Ink : Faint; sig.Add(b); }
            right.Add(sig);
            right.Add(Battery(0.17f));
            bar.Add(right);
            foreach (var c in bar.Query<VisualElement>().ToList()) c.pickingMode = PickingMode.Ignore;
            return bar;
        }

        VisualElement Battery(float level)
        {
            var w = Row(); w.style.marginLeft = 16;
            var body = new VisualElement(); body.style.width = 48; body.style.height = 24; Radius(body, 7);
            body.style.borderTopWidth = body.style.borderBottomWidth = body.style.borderLeftWidth = body.style.borderRightWidth = 2;
            body.style.borderTopColor = body.style.borderBottomColor = body.style.borderLeftColor = body.style.borderRightColor = Dim;
            Pad(body, 3);
            var fill = new VisualElement(); fill.style.width = Length.Percent(level * 100); fill.style.flexGrow = 0; fill.style.height = Length.Percent(100);
            fill.style.backgroundColor = new Color(0.95f, 0.36f, 0.3f); Radius(fill, 3); body.Add(fill);
            var cap = new VisualElement(); cap.style.width = 4; cap.style.height = 10; cap.style.backgroundColor = Dim; cap.style.marginLeft = 2; Radius(cap, 2);
            w.Add(cap);
            return w;
        }

        VisualElement HomeIndicator()
        {
            var hit = new VisualElement();
            hit.style.position = Position.Absolute; hit.style.left = hit.style.right = hit.style.bottom = 0; hit.style.height = 56;
            hit.style.alignItems = Align.Center; hit.style.justifyContent = Justify.Center;
            var line = new VisualElement(); line.style.width = 300; line.style.height = 10; Radius(line, 5); line.style.backgroundColor = new Color(1, 1, 1, 0.7f);
            line.pickingMode = PickingMode.Ignore;
            hit.Add(line);
            hit.RegisterCallback<PointerUpEvent>(_ => { if (screenHost.childCount > 0 && screenHost[0].name != "lock") ShowHome(); });
            return hit;
        }

        void Switch(VisualElement screen, System.Action backTo, string name = null)
        {
            if (typing != null) { StopCoroutine(typing); typing = null; }
            if (audioPlayingCoroutine != null) { StopCoroutine(audioPlayingCoroutine); audioPlayingCoroutine = null; }
            screenHost.Clear();
            screen.name = name ?? "";
            screen.style.flexGrow = 1;
            screenHost.Add(screen);
            back.Clear();
            if (backTo != null) back.Push(backTo);
        }

        void GoBack() { if (back.Count > 0) back.Pop()(); }

        // ---------- Screens ----------
        void ShowLock()
        {
            var s = new VisualElement(); s.style.alignItems = Align.Center;
            Cover(s, "Scenes/phong-tro-blur", Color.white);
            var lockIc = Icon(IcLock, 34, Dim); lockIc.style.marginTop = 150; s.Add(lockIc);
            var date = T(data.lockDate, 36, Ink); date.style.marginTop = 26; s.Add(date);
            var big = T(data.lockTime, 250, Ink, lightFont); big.style.marginTop = -10; big.style.letterSpacing = -6; s.Add(big);

            if (data.contacts != null && data.contacts.Count > 0)
            {
                var c = data.contacts[0];
                var note = Glass(); note.style.marginTop = 120; note.style.width = Length.Percent(90);
                var head = Row();
                head.Add(AppIcon("Icons/icon-tic", 52));
                var app = T("Tíc", 28, Dim, boldFont); app.style.marginLeft = 16; head.Add(app);
                var when = T("vừa xong", 26, Faint); when.style.marginLeft = 12; when.style.flexGrow = 1; head.Add(when);
                note.Add(head);
                var who = T(c.name, 34, Ink, boldFont); who.style.marginTop = 16; note.Add(who);
                note.Add(T(c.preview, 34, Ink));
                s.Add(note);
            }

            var bottom = Row(); bottom.style.position = Position.Absolute; bottom.style.bottom = 110; bottom.style.left = 90; bottom.style.right = 90;
            bottom.style.justifyContent = Justify.SpaceBetween;
            bottom.Add(Circle(IcLight)); bottom.Add(Circle(IcCamera));
            s.Add(bottom);
            var hint = T("Chạm để mở khóa", 28, Dim); hint.style.position = Position.Absolute; hint.style.bottom = 150; s.Add(hint);

            s.RegisterCallback<PointerUpEvent>(_ => ShowHome());
            Switch(s, null, "lock");
        }

        void ShowHome()
        {
            var s = new VisualElement();
            Cover(s, "Scenes/phong-tro", new Color(0.72f, 0.72f, 0.72f));
            var shade = new VisualElement(); shade.style.position = Position.Absolute; shade.style.left = shade.style.right = shade.style.top = 0; shade.style.height = 700;
            shade.style.backgroundColor = new Color(0, 0, 0, 0.35f); shade.pickingMode = PickingMode.Ignore; s.Add(shade);

            var widget = new VisualElement(); widget.style.marginTop = 170; widget.style.marginLeft = 72; widget.pickingMode = PickingMode.Ignore;
            var wt = T(data.lockTime, 150, Ink, lightFont); wt.style.letterSpacing = -4; Shadow(wt); widget.Add(wt);
            var wd = T(data.lockDate, 34, Ink); Shadow(wd); widget.Add(wd);
            s.Add(widget);

            // 4 Apps: Tic (chat), Nimbo (photos), Ghi am (voice memos), Mosa (ARG/browser)
            var grid = Row(); grid.style.marginTop = 110; grid.style.paddingLeft = 32; grid.style.paddingRight = 32; grid.style.justifyContent = Justify.SpaceAround;
            grid.Add(App("Icons/icon-tic", "Tíc", ShowTicList));
            grid.Add(App("Icons/icon-nimbo", "Nimbo", ShowNimbo));
            grid.Add(AppVector(IcMic, "Ghi âm", new Color(0.85f, 0.35f, 0.25f), ShowVoiceMemos));
            grid.Add(App("Icons/icon-mosa", "Mosa", ShowMosa));
            s.Add(grid);

            var dots = Row(); dots.style.position = Position.Absolute; dots.style.bottom = 120; dots.style.left = 0; dots.style.right = 0; dots.style.justifyContent = Justify.Center;
            for (int i = 0; i < 3; i++) { var d = new VisualElement(); d.style.width = d.style.height = 12; Radius(d, 6); d.style.marginLeft = d.style.marginRight = 7; d.style.backgroundColor = i == 0 ? Ink : Faint; dots.Add(d); }
            s.Add(dots);
            Switch(s, null, "home");
        }

        // ---------- Tic (Chat) ----------
        void ShowTicList()
        {
            var s = AppScreen();
            var head = Row(); head.style.paddingLeft = 56; head.style.paddingRight = 48; head.style.marginTop = 130; head.style.justifyContent = Justify.SpaceBetween;
            head.Add(T("Tíc", 68, Ink, boldFont));
            var acts = Row(); acts.Add(Icon(IcCamera, 40, Ink)); var add = Icon(IcAdd, 40, Ink); add.style.marginLeft = 40; acts.Add(add); head.Add(acts);
            s.Add(head);
            var search = Row(); search.style.marginLeft = search.style.marginRight = 48; search.style.marginTop = 28; search.style.height = 92;
            search.style.backgroundColor = Raised; Radius(search, 46); search.style.paddingLeft = 34;
            search.Add(Icon(IcSearch, 32, Faint)); var ph = T("Tìm kiếm cuộc trò chuyện", 32, Faint); ph.style.marginLeft = 20; search.Add(ph);
            s.Add(search);

            var list = new ScrollView(ScrollViewMode.Vertical); list.style.marginTop = 24; list.style.flexGrow = 1;
            foreach (var c in data.contacts)
            {
                var row = Row(); row.style.paddingLeft = 48; row.style.paddingRight = 48; row.style.height = 168;
                row.Add(Avatar(c.name, 108));
                var col = new VisualElement(); col.style.marginLeft = 30; col.style.flexGrow = 1; col.style.flexShrink = 1;
                col.style.borderBottomWidth = 1; col.style.borderBottomColor = Hair; col.style.height = Length.Percent(100); col.style.justifyContent = Justify.Center;
                var top = Row(); top.style.justifyContent = Justify.SpaceBetween;
                top.Add(T(c.name, 38, Ink, boldFont));
                var sh = Shown(c);
                var lastTime = sh.Count > 0 ? sh[sh.Count - 1].time : (c.messages.Count > 0 ? c.messages[c.messages.Count - 1].time : "");
                top.Add(T(lastTime, 26, Faint));
                col.Add(top);
                var bottom = Row(); bottom.style.justifyContent = Justify.SpaceBetween; bottom.style.marginTop = 6;
                var last = sh.Count > 0 ? (sh[sh.Count - 1].from == "me" ? "Bạn: " : "") + sh[sh.Count - 1].text : c.preview;
                var lt = T(last, 31, sh.Count == 0 ? Ink : Dim); lt.style.flexShrink = 1; lt.style.overflow = Overflow.Hidden; lt.style.textOverflow = TextOverflow.Ellipsis; lt.style.whiteSpace = WhiteSpace.NoWrap;
                bottom.Add(lt);
                int unread = Mathf.Max(0, Script(c).Count - sh.Count);
                if (unread > 0)
                {
                    var badge = new VisualElement(); badge.style.minWidth = 44; badge.style.height = 44; Radius(badge, 22); badge.style.backgroundColor = WarningRed;
                    badge.style.alignItems = Align.Center; badge.style.justifyContent = Justify.Center;
                    badge.Add(T(unread.ToString(), 26, Color.white, boldFont)); bottom.Add(badge);
                }
                col.Add(bottom);
                row.Add(col);
                var contact = c;
                row.RegisterCallback<PointerUpEvent>(_ => ShowChat(contact));
                list.Add(row);
            }
            s.Add(list);
            Switch(s, ShowHome, "tic");
        }

        void ShowChat(Contact c)
        {
            var s = AppScreen();
            var head = Row(); head.style.marginTop = 92; head.style.height = 140; head.style.paddingLeft = 20; head.style.paddingRight = 44;
            head.style.borderBottomWidth = 1; head.style.borderBottomColor = Hair; head.style.flexShrink = 0;
            var b = Icon(IcBack, 44, Ink); Pad(b, 22); b.RegisterCallback<PointerUpEvent>(_ => GoBack()); head.Add(b);
            head.Add(Avatar(c.name, 84));
            var nm = new VisualElement(); nm.style.marginLeft = 24; nm.style.flexGrow = 1;
            nm.Add(T(c.name, 38, Ink, boldFont));
            var presence = T("Đang hoạt động", 26, Dim); nm.Add(presence);
            head.Add(nm);
            head.Add(Icon(IcCall, 40, Ink)); var more = Icon(IcMore, 40, Ink); more.style.marginLeft = 40; head.Add(more);
            s.Add(head);

            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.flexGrow = 1;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            var cc = scroll.contentContainer; cc.style.paddingLeft = 36; cc.style.paddingRight = 36; cc.style.paddingTop = 30; cc.style.paddingBottom = 20;
            var day = T(string.IsNullOrEmpty(data.chatDay) ? "Hôm nay" : data.chatDay, 26, Faint); day.style.alignSelf = Align.Center; day.style.marginBottom = 26; cc.Add(day);
            s.Add(scroll);

            var choiceBox = new VisualElement(); choiceBox.style.paddingLeft = 36; choiceBox.style.paddingRight = 36; choiceBox.style.flexShrink = 0;
            s.Add(choiceBox);

            var input = Row(); input.style.flexShrink = 0; input.style.marginLeft = 28; input.style.marginRight = 28; input.style.marginTop = 14; input.style.marginBottom = 76;
            input.Add(Icon(IcAdd, 44, Dim));
            var field = Row(); field.style.flexGrow = 1; field.style.height = 92; field.style.marginLeft = 22; field.style.marginRight = 22; field.style.backgroundColor = Raised; Radius(field, 46); field.style.paddingLeft = 36;
            var placeholder = T("Nhập tin nhắn...", 32, Faint); field.Add(placeholder); input.Add(field);
            input.Add(Icon(IcMic, 44, Dim));
            s.Add(input);
            Switch(s, ShowTicList, "chat");

            Message prev = null;
            foreach (var m in Shown(c)) { cc.Add(Bubble(m, prev)); prev = m; }
            // Script(c) includes the answer and its replies, so leaving mid-reply resumes them here.
            var queue = Pending(c);
            typing = StartCoroutine(Play(c, queue, scroll, presence, () => OfferChoices(c, scroll, presence, choiceBox)));
        }

        List<Message> Pending(Contact c)
        {
            var sh = Shown(c);
            return Script(c).FindAll(m => !sh.Contains(m));
        }

        IEnumerator Play(Contact c, List<Message> queue, ScrollView scroll, Label presence, System.Action done)
        {
            var cc = scroll.contentContainer;
            foreach (var m in queue)
            {
                VisualElement dots = null;
                if (m.from != "me")
                {
                    presence.text = "đang soạn tin..."; presence.style.color = Accent;
                    dots = TypingBubble(); cc.Add(dots); ScrollEnd(scroll, dots);
                    yield return new WaitForSeconds(m.delay);
                    dots.RemoveFromHierarchy();
                }
                presence.text = "Đang hoạt động"; presence.style.color = Dim;
                var sh = Shown(c);
                var prev = sh.Count > 0 ? sh[sh.Count - 1] : null;
                sh.Add(m);
                OnMessageReceived?.Invoke(m.text);
                if (!string.IsNullOrEmpty(m.time)) clock.text = m.time.TrimStart('0');
                var b = Bubble(m, prev); cc.Add(b); ScrollEnd(scroll, b);
                yield return new WaitForSeconds(0.35f);
            }
            typing = null;
            done?.Invoke();
        }

        void OfferChoices(Contact c, ScrollView scroll, Label presence, VisualElement box)
        {
            if (chose.ContainsKey(c.id) || c.choices == null || c.choices.Count == 0) return;
            box.Clear();
            var label = T("Chọn câu trả lời:", 26, Faint); label.style.marginBottom = 12; label.style.marginLeft = 8; box.Add(label);
            foreach (var ch in c.choices)
            {
                var choice = ch;
                var btn = Row(); btn.style.marginBottom = 14; btn.style.paddingTop = 24; btn.style.paddingBottom = 24; btn.style.paddingLeft = 34; btn.style.paddingRight = 34;
                Radius(btn, 40); btn.style.backgroundColor = AccentSoft;
                btn.style.borderTopWidth = btn.style.borderBottomWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = 2;
                btn.style.borderTopColor = btn.style.borderBottomColor = btn.style.borderLeftColor = btn.style.borderRightColor = new Color(0.24f, 0.56f, 0.49f, 0.7f);
                var bt = T(choice.text, 30, Ink); bt.style.flexGrow = 1; bt.style.whiteSpace = WhiteSpace.Normal; btn.Add(bt);
                btn.Add(Icon(IcSend, 32, new Color(0.45f, 0.8f, 0.7f)));
                btn.RegisterCallback<PointerUpEvent>(_ =>
                {
                    Pick(c, choice);
                    SaveChoice(c, choice);
                    box.Clear();
                    typing = StartCoroutine(Play(c, Pending(c), scroll, presence, null));
                });
                box.Add(btn);
            }
        }

        // ---------- Nimbo (Photos) ----------
        void ShowNimbo()
        {
            var s = AppScreen();
            var head = Row(); head.style.paddingLeft = 56; head.style.marginTop = 130; head.style.justifyContent = Justify.SpaceBetween; head.style.paddingRight = 48;
            head.Add(T("Ảnh", 68, Ink, boldFont)); head.Add(Icon(IcSearch, 40, Ink)); s.Add(head);
            var sync = Row(); sync.style.marginLeft = 56; sync.style.marginTop = 8;
            var dot = new VisualElement(); dot.style.width = dot.style.height = 14; Radius(dot, 7); dot.style.backgroundColor = new Color(0.95f, 0.65f, 0.25f); sync.Add(dot);
            var st = T("Đã đồng bộ ảnh hiện trường", 28, Dim); st.style.marginLeft = 14; sync.Add(st); s.Add(sync);
            var day = T(string.IsNullOrEmpty(data.albumDay) ? "Hôm nay" : data.albumDay, 34, Ink, boldFont); day.style.marginLeft = 56; day.style.marginTop = 50; day.style.marginBottom = 18; s.Add(day);

            var grid = Row(); grid.style.flexWrap = Wrap.Wrap; grid.style.alignItems = Align.FlexStart; grid.style.paddingLeft = 20; grid.style.paddingRight = 20;
            if (data.photos != null)
            {
                foreach (var p in data.photos)
                {
                    var cell = new VisualElement(); cell.style.width = 320; cell.style.height = 320; cell.style.marginLeft = cell.style.marginRight = cell.style.marginTop = cell.style.marginBottom = 6;
                    Radius(cell, 16); cell.style.overflow = Overflow.Hidden;
                    Cover(cell, p.resource, Color.white);
                    var tag = T(p.caption, 24, Ink, boldFont); tag.style.position = Position.Absolute; tag.style.right = 14; tag.style.bottom = 10; Shadow(tag); cell.Add(tag);
                    var photo = p;
                    cell.RegisterCallback<PointerUpEvent>(_ => ShowPhoto(photo));
                    grid.Add(cell);
                }
            }
            s.Add(grid);
            Switch(s, ShowHome, "nimbo");
        }

        void ShowPhoto(Photo p)
        {
            var s = new VisualElement(); s.style.backgroundColor = Color.black;
            var img = new VisualElement(); img.style.flexGrow = 1; img.style.marginTop = 190; img.style.marginBottom = 230;
            var sp = Resources.Load<Sprite>(p.resource);
            if (sp != null) { img.style.backgroundImage = new StyleBackground(sp); img.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); }
            s.Add(img);
            var top = Row(); top.style.position = Position.Absolute; top.style.top = 92; top.style.left = 20; top.style.right = 44; top.style.height = 100;
            var b = Icon(IcBack, 44, Ink); Pad(b, 22); b.RegisterCallback<PointerUpEvent>(_ => GoBack()); top.Add(b);
            var meta = new VisualElement(); meta.style.marginLeft = 10;
            meta.Add(T(p.caption, 34, Ink, boldFont)); if (!string.IsNullOrEmpty(p.time)) meta.Add(T("Chụp lúc " + p.time, 26, Dim)); top.Add(meta);
            s.Add(top);
            Switch(s, ShowNimbo, "photo");
        }

        // ---------- Voice Memos (Ghi am) ----------
        void ShowVoiceMemos()
        {
            var s = AppScreen();
            var head = Row(); head.style.paddingLeft = 56; head.style.paddingRight = 48; head.style.marginTop = 130; head.style.justifyContent = Justify.SpaceBetween;
            head.Add(T("Ghi âm", 68, Ink, boldFont));
            var cnt = T($"{data?.voiceMemos?.Count ?? 0} bản ghi", 30, Faint);
            head.Add(cnt);
            s.Add(head);

            var list = new ScrollView(ScrollViewMode.Vertical); list.style.marginTop = 36; list.style.flexGrow = 1;
            if (data?.voiceMemos != null && data.voiceMemos.Count > 0)
            {
                foreach (var memo in data.voiceMemos)
                {
                    var card = new VisualElement();
                    card.style.marginLeft = card.style.marginRight = 40;
                    card.style.marginBottom = 20;
                    card.style.paddingTop = card.style.paddingBottom = 26;
                    card.style.paddingLeft = card.style.paddingRight = 32;
                    card.style.backgroundColor = Raised;
                    Radius(card, 28);

                    var top = Row(); top.style.justifyContent = Justify.SpaceBetween;
                    top.Add(T(memo.title, 34, Ink, boldFont));
                    top.Add(T(memo.duration, 28, Accent, boldFont));
                    card.Add(top);

                    var btm = Row(); btm.style.marginTop = 12; btm.style.justifyContent = Justify.SpaceBetween;
                    btm.Add(T(memo.date + " • " + memo.time, 26, Dim));
                    var playBtn = Row();
                    playBtn.Add(Icon(IcPlay, 26, Accent));
                    var plTxt = T(" Nghe", 26, Accent); plTxt.style.marginLeft = 6;
                    playBtn.Add(plTxt);
                    btm.Add(playBtn);
                    card.Add(btm);

                    var m = memo;
                    card.RegisterCallback<PointerUpEvent>(_ => ShowVoiceMemoDetail(m));
                    list.Add(card);
                }
            }
            else
            {
                var empty = T("Chưa có đoạn ghi âm nào", 32, Faint);
                empty.style.alignSelf = Align.Center; empty.style.marginTop = 100;
                list.Add(empty);
            }
            s.Add(list);
            Switch(s, ShowHome, "voicememos");
        }

        void ShowVoiceMemoDetail(VoiceMemo memo)
        {
            var s = AppScreen();
            var head = Row(); head.style.marginTop = 92; head.style.height = 140; head.style.paddingLeft = 20; head.style.paddingRight = 44;
            head.style.borderBottomWidth = 1; head.style.borderBottomColor = Hair; head.style.flexShrink = 0;
            var b = Icon(IcBack, 44, Ink); Pad(b, 22); b.RegisterCallback<PointerUpEvent>(_ => GoBack()); head.Add(b);
            var titleBox = new VisualElement(); titleBox.style.marginLeft = 20; titleBox.style.flexGrow = 1;
            titleBox.Add(T(memo.title, 36, Ink, boldFont));
            titleBox.Add(T(memo.date + " lúc " + memo.time, 26, Dim));
            head.Add(titleBox);
            s.Add(head);

            // Audio Player Controls & Waveform simulation
            var playerBox = Glass();
            playerBox.style.marginTop = 36; playerBox.style.marginLeft = playerBox.style.marginRight = 40;
            playerBox.style.alignItems = Align.Center;

            // Waveform equalizer bars
            var waveRow = Row(); waveRow.style.height = 90; waveRow.style.alignItems = Align.Center; waveRow.style.justifyContent = Justify.Center;
            var bars = new List<VisualElement>();
            for (int i = 0; i < 28; i++)
            {
                var bar = new VisualElement();
                bar.style.width = 10; bar.style.height = 16 + (i % 6) * 10;
                bar.style.marginLeft = bar.style.marginRight = 4;
                Radius(bar, 5); bar.style.backgroundColor = Accent;
                waveRow.Add(bar);
                bars.Add(bar);
            }
            playerBox.Add(waveRow);

            var timer = T("00:00 / " + memo.duration, 28, Dim);
            timer.style.marginTop = 16;
            playerBox.Add(timer);

            // Play / Pause button
            var playButton = Circle(IcPlay);
            playButton.style.marginTop = 24;
            playButton.style.backgroundColor = Accent;
            bool isPlaying = false;
            playButton.RegisterCallback<PointerUpEvent>(_ =>
            {
                isPlaying = !isPlaying;
                playButton.Clear();
                playButton.Add(Icon(isPlaying ? IcPause : IcPlay, 44, Color.white));
                if (isPlaying)
                {
                    if (audioPlayingCoroutine != null) StopCoroutine(audioPlayingCoroutine);
                    audioPlayingCoroutine = StartCoroutine(AnimateWaveform(bars, timer, memo.duration));
                }
                else
                {
                    if (audioPlayingCoroutine != null) { StopCoroutine(audioPlayingCoroutine); audioPlayingCoroutine = null; }
                }
            });
            playerBox.Add(playButton);
            s.Add(playerBox);

            // Transcript (Giải mã âm thanh)
            var transBox = new VisualElement();
            transBox.style.marginTop = 36; transBox.style.marginLeft = transBox.style.marginRight = 40;
            transBox.Add(T("Giải mã bản ghi âm:", 30, Faint, boldFont));
            var transCard = Glass(); transCard.style.marginTop = 14; transCard.style.backgroundColor = Raised;
            var transText = T(memo.transcript, 30, Ink);
            transText.style.whiteSpace = WhiteSpace.Normal;
            transCard.Add(transText);
            transBox.Add(transCard);
            s.Add(transBox);

            Switch(s, ShowVoiceMemos, "memo_detail");
        }

        IEnumerator AnimateWaveform(List<VisualElement> bars, Label timer, string totalDuration)
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                int secs = Mathf.FloorToInt(elapsed);
                timer.text = $"00:{(secs % 60):D2} / {totalDuration}";
                for (int i = 0; i < bars.Count; i++)
                {
                    float wave = Mathf.Sin(elapsed * 8f + i * 0.4f);
                    bars[i].style.height = 14 + Mathf.Abs(wave) * 60;
                }
                yield return null;
            }
        }

        // ---------- Mosa (Browser / ARG) ----------
        void ShowMosa()
        {
            var s = AppScreen(); s.style.alignItems = Align.Center; s.style.justifyContent = Justify.Center;
            var ic = AppIcon("Icons/icon-mosa", 160); ic.style.opacity = 0.5f; s.Add(ic);
            var t1 = T("Mosa Network", 40, Ink, boldFont); t1.style.marginTop = 40; s.Add(t1);
            var t2 = T("Không có kết nối Internet tại khu vực này", 30, Dim); t2.style.marginTop = 10; s.Add(t2);
            var t3 = T("Tín hiệu 4G bị nhiễu sóng lạ lúc " + data.lockTime, 26, Faint); t3.style.marginTop = 10; s.Add(t3);
            var retry = T("Thử dò lại sóng", 32, Accent, boldFont); retry.style.marginTop = 50;
            retry.RegisterCallback<PointerUpEvent>(_ => retry.text = "Đang quét tần số...");
            s.Add(retry);
            Switch(s, ShowHome, "mosa");
        }

        // ---------- Building blocks ----------
        List<Message> Shown(Contact c)
        {
            if (!shown.TryGetValue(c.id, out var l)) shown[c.id] = l = new List<Message>();
            return l;
        }

        VisualElement AppScreen() { var s = new VisualElement(); s.style.backgroundColor = Surface; return s; }

        VisualElement Bubble(Message m, Message prev)
        {
            bool mine = m.from == "me";
            bool grouped = prev != null && prev.from == m.from;
            var b = new VisualElement();
            b.style.alignSelf = mine ? Align.FlexEnd : Align.FlexStart;
            b.style.maxWidth = Length.Percent(76);
            b.style.backgroundColor = mine ? Accent : Theirs;
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 40;
            if (mine) { b.style.borderBottomRightRadius = 12; if (grouped) b.style.borderTopRightRadius = 12; }
            else { b.style.borderBottomLeftRadius = 12; if (grouped) b.style.borderTopLeftRadius = 12; }
            b.style.paddingTop = 20; b.style.paddingBottom = 16; b.style.paddingLeft = 32; b.style.paddingRight = 28;
            b.style.marginTop = grouped ? 6 : 22;
            var t = T(m.text, 34, Ink); t.style.whiteSpace = WhiteSpace.Normal; b.Add(t);
            if (!string.IsNullOrEmpty(m.time))
            {
                var tm = T(m.time + (mine ? "  Đã gửi" : ""), 22, mine ? new Color(1, 1, 1, 0.7f) : Faint);
                tm.style.alignSelf = Align.FlexEnd; tm.style.marginTop = 4; b.Add(tm);
            }
            return b;
        }

        VisualElement TypingBubble()
        {
            var b = Row(); b.style.alignSelf = Align.FlexStart; b.style.backgroundColor = Theirs; Radius(b, 40); b.style.borderBottomLeftRadius = 12;
            b.style.paddingLeft = 30; b.style.paddingRight = 30; b.style.height = 76; b.style.marginTop = 22;
            for (int i = 0; i < 3; i++)
            {
                var d = new VisualElement(); d.style.width = d.style.height = 14; Radius(d, 7); d.style.marginLeft = d.style.marginRight = 6; d.style.backgroundColor = Dim;
                int k = i; d.schedule.Execute(() => d.style.opacity = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(Time.time * 4f + k))).Every(60);
                b.Add(d);
            }
            return b;
        }

        static void ScrollEnd(ScrollView sv, VisualElement e) => sv.schedule.Execute(() => sv.ScrollTo(e)).StartingIn(40);

        VisualElement App(string icon, string name, System.Action open)
        {
            var a = new VisualElement(); a.style.alignItems = Align.Center; a.style.width = 210;
            a.Add(AppIcon(icon, 150));
            var l = T(name, 28, Ink); l.style.marginTop = 14; Shadow(l); a.Add(l);
            a.RegisterCallback<PointerUpEvent>(_ => open());
            return a;
        }

        VisualElement AppVector(string vectorIcon, string name, Color bg, System.Action open)
        {
            var a = new VisualElement(); a.style.alignItems = Align.Center; a.style.width = 210;
            var iconBox = new VisualElement();
            iconBox.style.width = iconBox.style.height = 150;
            Radius(iconBox, 38);
            iconBox.style.backgroundColor = bg;
            iconBox.style.alignItems = Align.Center; iconBox.style.justifyContent = Justify.Center;
            iconBox.Add(Icon(vectorIcon, 70, Color.white));
            a.Add(iconBox);

            var l = T(name, 28, Ink); l.style.marginTop = 14; Shadow(l); a.Add(l);
            a.RegisterCallback<PointerUpEvent>(_ => open());
            return a;
        }

        VisualElement AppIcon(string res, float size)
        {
            var e = new VisualElement(); e.style.width = size; e.style.height = size; Radius(e, size * 0.25f);
            var sp = Resources.Load<Sprite>(res);
            if (sp != null) e.style.backgroundImage = new StyleBackground(sp); else e.style.backgroundColor = Accent;
            return e;
        }

        VisualElement Avatar(string name, float size)
        {
            var av = new VisualElement(); av.style.width = av.style.height = size; Radius(av, size / 2); av.style.flexShrink = 0;
            int h = Mathf.Abs(name.GetHashCode());
            av.style.backgroundColor = Color.HSVToRGB((h % 360) / 360f, 0.35f, 0.5f);
            av.style.alignItems = Align.Center; av.style.justifyContent = Justify.Center;
            av.Add(T(name.Substring(0, 1).ToUpper(), size * 0.42f, Ink, boldFont));
            return av;
        }

        VisualElement Glass()
        {
            var c = new VisualElement(); c.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.72f); Radius(c, 44);
            c.style.paddingTop = 30; c.style.paddingBottom = 32; c.style.paddingLeft = 36; c.style.paddingRight = 36;
            return c;
        }

        VisualElement Circle(string glyph)
        {
            var c = new VisualElement(); c.style.width = c.style.height = 120; Radius(c, 60);
            c.style.backgroundColor = new Color(0, 0, 0, 0.45f); c.style.alignItems = Align.Center; c.style.justifyContent = Justify.Center;
            c.Add(Icon(glyph, 44, Ink));
            return c;
        }

        VisualElement Icon(string kind, float size, Color color)
        {
            var e = new VisualElement(); e.style.width = size; e.style.height = size; e.style.flexShrink = 0;
            e.generateVisualContent += ctx => DrawIcon(kind, ctx.painter2D, e.contentRect, color);
            return e;
        }

        static void DrawIcon(string kind, Painter2D p, Rect r, Color c)
        {
            float s = Mathf.Min(r.width, r.height);
            Vector2 P(float x, float y) => new Vector2(r.x + x * s, r.y + y * s);
            p.strokeColor = c; p.fillColor = c; p.lineWidth = s * 0.09f; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            void Line(params Vector2[] pts) { p.BeginPath(); p.MoveTo(pts[0]); for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]); p.Stroke(); }
            void Shape(bool fill, params Vector2[] pts) { p.BeginPath(); p.MoveTo(pts[0]); for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]); p.ClosePath(); if (fill) p.Fill(); else p.Stroke(); }
            void Circle(Vector2 ctr, float rad, bool fill) { p.BeginPath(); p.Arc(ctr, rad * s, Angle.Degrees(0), Angle.Degrees(360)); p.ClosePath(); if (fill) p.Fill(); else p.Stroke(); }
            switch (kind)
            {
                case "back": Line(P(0.62f, 0.18f), P(0.3f, 0.5f), P(0.62f, 0.82f)); break;
                case "search": Circle(P(0.43f, 0.43f), 0.26f, false); Line(P(0.63f, 0.63f), P(0.86f, 0.86f)); break;
                case "add": Line(P(0.5f, 0.16f), P(0.5f, 0.84f)); Line(P(0.16f, 0.5f), P(0.84f, 0.5f)); break;
                case "send": Shape(true, P(0.12f, 0.16f), P(0.9f, 0.5f), P(0.12f, 0.84f), P(0.26f, 0.5f)); break;
                case "camera":
                    Shape(false, P(0.08f, 0.3f), P(0.33f, 0.3f), P(0.4f, 0.17f), P(0.6f, 0.17f), P(0.67f, 0.3f), P(0.92f, 0.3f), P(0.92f, 0.84f), P(0.08f, 0.84f));
                    Circle(P(0.5f, 0.56f), 0.15f, false); break;
                case "lock":
                    Line(P(0.32f, 0.46f), P(0.32f, 0.32f)); Line(P(0.68f, 0.46f), P(0.68f, 0.32f));
                    p.BeginPath(); p.Arc(P(0.5f, 0.32f), 0.18f * s, Angle.Degrees(180), Angle.Degrees(360)); p.Stroke();
                    Shape(true, P(0.2f, 0.46f), P(0.8f, 0.46f), P(0.8f, 0.88f), P(0.2f, 0.88f)); break;
                case "light":
                    Shape(false, P(0.28f, 0.1f), P(0.72f, 0.1f), P(0.62f, 0.38f), P(0.38f, 0.38f));
                    Shape(false, P(0.38f, 0.38f), P(0.62f, 0.38f), P(0.62f, 0.9f), P(0.38f, 0.9f)); break;
                case "call":
                    Line(P(0.24f, 0.16f), P(0.38f, 0.16f), P(0.46f, 0.34f), P(0.36f, 0.44f), P(0.56f, 0.64f), P(0.66f, 0.54f), P(0.84f, 0.62f), P(0.84f, 0.76f), P(0.72f, 0.86f));
                    break;
                case "more": Circle(P(0.5f, 0.2f), 0.07f, true); Circle(P(0.5f, 0.5f), 0.07f, true); Circle(P(0.5f, 0.8f), 0.07f, true); break;
                case "mic":
                    Shape(false, P(0.38f, 0.1f), P(0.62f, 0.1f), P(0.62f, 0.56f), P(0.38f, 0.56f));
                    p.BeginPath(); p.Arc(P(0.5f, 0.5f), 0.24f * s, Angle.Degrees(0), Angle.Degrees(180)); p.Stroke();
                    Line(P(0.5f, 0.74f), P(0.5f, 0.9f)); break;
                case "play":
                    Shape(true, P(0.3f, 0.2f), P(0.8f, 0.5f), P(0.3f, 0.8f)); break;
                case "pause":
                    Shape(true, P(0.28f, 0.22f), P(0.42f, 0.22f), P(0.42f, 0.78f), P(0.28f, 0.78f));
                    Shape(true, P(0.58f, 0.22f), P(0.72f, 0.22f), P(0.72f, 0.78f), P(0.58f, 0.78f)); break;
                case "close":
                    Line(P(0.22f, 0.22f), P(0.78f, 0.78f)); Line(P(0.78f, 0.22f), P(0.22f, 0.78f)); break;
                case "phone":
                    Shape(false, P(0.26f, 0.14f), P(0.74f, 0.14f), P(0.74f, 0.86f), P(0.26f, 0.86f));
                    Circle(P(0.5f, 0.76f), 0.05f, true);
                    Line(P(0.42f, 0.22f), P(0.58f, 0.22f)); break;
            }
        }

        Label T(string s, float size, Color color, Font font = null)
        {
            var l = new Label(s); l.style.fontSize = size; l.style.color = color;
            l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0;
            l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0;
            if (font != null) l.style.unityFontDefinition = FontDefinition.FromFont(font);
            return l;
        }

        static void Cover(VisualElement e, string res, Color tint)
        {
            var sp = Resources.Load<Sprite>(res);
            if (sp == null) return;
            e.style.backgroundImage = new StyleBackground(sp);
            e.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            e.style.unityBackgroundImageTintColor = tint;
        }

        static void Shadow(Label l) => l.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = 8, color = new Color(0, 0, 0, 0.7f) };

        static VisualElement Row()
        {
            var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; return r;
        }

        static void Radius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        static void Pad(VisualElement e, float p)
        {
            e.style.paddingLeft = p; e.style.paddingRight = p; e.style.paddingTop = p; e.style.paddingBottom = p;
        }
    }
}
