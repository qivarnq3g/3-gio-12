using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Gameplay
{
    public sealed class HudChoices
    {
        public int selected;
        internal int handledFrame = -1, flashIndex = -1;
        internal float[] glow = new float[0];
        internal float revealAt = -10, flashAt = -10;
        public void Reset(float delay = 0) { selected = 0; Reveal(delay); }
        public void Reveal(float delay = 0) { revealAt = Time.unscaledTime + delay; }
        public float Age => Time.unscaledTime - revealAt;
        public HudChoices Snapshot() => new HudChoices { selected = selected, glow = (float[])glow.Clone(), revealAt = -10, flashIndex = flashIndex, flashAt = flashAt };
    }

    public sealed class HudFade
    {
        float value;
        int frame = -1;
        public float Value => value;
        public float Step(bool on, float fadeIn = .2f, float fadeOut = .14f)
        {
            if (frame != Time.frameCount)
            {
                frame = Time.frameCount;
                value = Mathf.MoveTowards(value, on ? 1 : 0, Time.unscaledDeltaTime / Mathf.Max(.001f, on ? fadeIn : fadeOut));
            }
            return value;
        }
        public void Snap(bool on) { value = on ? 1 : 0; frame = Time.frameCount; }
    }

    public sealed class HudSwap
    {
        public string Current = "", Previous = "";
        float changedAt = -10;
        public float Age => Time.unscaledTime - changedAt;
        public bool Set(string text, bool animate = true)
        {
            text ??= "";
            if (text == Current) return false;
            Previous = Current; Current = text;
            changedAt = animate ? Time.unscaledTime : -10;
            return true;
        }
    }

    public static class HudKit
    {
        public const float Height = 1080, Margin = 160;
        public static readonly Color Paper = new Color(.97f, .93f, .85f);
        public static readonly Color Warm = new Color(1, .8f, .52f);
        public static readonly Color Alarm = new Color(.86f, .24f, .18f);
        public static readonly Color Night = new Color(.035f, .032f, .03f);
        public static float Width => Screen.height <= 0 ? 1920 : Screen.width * Height / Screen.height;
        static float Scale => Screen.height <= 0 ? 1 : Screen.height / Height;

        static Font regular, medium;
        static Texture2D stripe, shadeLeft, shadeBand, grain;
        static AudioSource sound;
        static readonly Dictionary<long, GUIStyle> styles = new Dictionary<long, GUIStyle>();
        static readonly Dictionary<string, float> hover = new Dictionary<string, float>();
        static readonly Dictionary<string, int> hoverFrame = new Dictionary<string, int>();
        static readonly Stack<(float alpha, Vector2 offset, bool locked)> layers = new Stack<(float, Vector2, bool)>();
        static float layerAlpha = 1;
        static Vector2 layerOffset;
        static bool layerLocked;
        static float promptShown, holdShown, holdFlashAt = -10, lastHold;
        static string promptKey = "", promptText = "";
        static int promptFrame = -1;
        static readonly HudSwap subtitle = new HudSwap();
        static readonly HudFade subtitleFade = new HudFade();

        static Font Regular { get { if (regular == null) regular = Resources.Load<Font>("Fonts/BeVietnamPro-Regular"); return regular; } }
        static Font Medium { get { if (medium == null) medium = Resources.Load<Font>("Fonts/BeVietnamPro-Medium"); return medium; } }

        public static float OutCubic(float t) { t = Mathf.Clamp01(t); return 1 - (1 - t) * (1 - t) * (1 - t); }
        public static float InQuad(float t) { t = Mathf.Clamp01(t); return t * t; }
        public static float Reveal(float age, float delay, float duration) => OutCubic((age - delay) / duration);

        public static Color Fade(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);
        public static Rect S(Rect r) { float k = Scale; return new Rect((r.x + layerOffset.x) * k, (r.y + layerOffset.y) * k, r.width * k, r.height * k); }
        public static Rect S(float x, float y, float w, float h) => S(new Rect(x, y, w, h));

        public static void Push(float alpha, float dx = 0, float dy = 0, bool locked = false)
        {
            layers.Push((layerAlpha, layerOffset, layerLocked));
            layerAlpha *= Mathf.Clamp01(alpha);
            layerOffset += new Vector2(dx, dy);
            layerLocked |= locked;
        }

        public static void Pop()
        {
            if (layers.Count == 0) return;
            (layerAlpha, layerOffset, layerLocked) = layers.Pop();
        }

        public static Vector2 BeginScroll(Rect area, Vector2 scroll, float contentWidth, float contentHeight)
        {
            var screen = S(area);
            layers.Push((layerAlpha, layerOffset, layerLocked));
            layerOffset = Vector2.zero;
            return GUI.BeginScrollView(screen, scroll, S(0, 0, contentWidth, Mathf.Max(area.height, contentHeight)), false, false, GUIStyle.none, GUIStyle.none);
        }

        public static void EndScroll()
        {
            GUI.EndScrollView();
            Pop();
        }

        public static GUIStyle Style(float size, bool bold = false, TextAnchor align = TextAnchor.UpperLeft, bool wrap = true)
        {
            int pixels = Mathf.Max(8, Mathf.RoundToInt(size * Scale));
            long key = pixels | (bold ? 1L << 20 : 0) | ((long)align << 21) | (wrap ? 1L << 30 : 0);
            if (styles.TryGetValue(key, out var style) && style.font != null) return style;
            style = new GUIStyle { font = bold ? Medium : Regular, fontSize = pixels, alignment = align, wordWrap = wrap, richText = false, clipping = TextClipping.Overflow };
            style.normal.textColor = Color.white;
            styles[key] = style;
            return style;
        }

        static void Tint(Color color) => GUI.color = Fade(color, layerAlpha);

        public static void Label(Rect r, string text, float size, Color color, bool bold = false, TextAnchor align = TextAnchor.UpperLeft, bool wrap = true)
        {
            if (string.IsNullOrEmpty(text) || layerAlpha * color.a <= .002f) return;
            var previous = GUI.color; Tint(color);
            GUI.Label(S(r), text, Style(size, bold, align, wrap));
            GUI.color = previous;
        }

        public static string Glue(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int i = text.TrimEnd().LastIndexOf(' ');
            return i > 0 && text.Length - i - 1 <= 3 ? text.Substring(0, i) + ' ' + text.Substring(i + 1) : text;
        }

        public static float TextHeight(string text, float size, float width, bool bold = false) =>
            Style(size, bold).CalcHeight(new GUIContent(text), width * Scale) / Scale;

        public static float TextWidth(string text, float size, bool bold = false) =>
            Style(size, bold, TextAnchor.UpperLeft, false).CalcSize(new GUIContent(text)).x / Scale;

        public static void Fill(Rect r, Color color)
        {
            if (layerAlpha * color.a <= .002f) return;
            var previous = GUI.color; Tint(color);
            GUI.DrawTexture(S(r), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void Texture(Rect r, Texture2D texture, Color color)
        {
            if (layerAlpha * color.a <= .002f) return;
            var previous = GUI.color; Tint(color);
            GUI.DrawTexture(S(r), texture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        public static void Backdrop(float alpha = 1)
        {
            if (grain == null) grain = GameUi.Mottle(192, 108, 37);
            var offset = layerOffset; layerOffset = Vector2.zero;
            Fill(new Rect(0, 0, Width, Height), Fade(Night, alpha));
            Texture(new Rect(0, 0, Width, Height), grain, new Color(.11f, .1f, .085f, .55f * alpha));
            layerOffset = offset;
        }

        public static void ShadeLeft(Rect r, float alpha = .75f)
        {
            if (shadeLeft == null) shadeLeft = GameUi.Gradient(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0));
            Texture(r, shadeLeft, new Color(1, 1, 1, alpha));
        }

        public static void ShadeBand(Rect r, float alpha = .7f)
        {
            if (shadeBand == null)
            {
                shadeBand = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int i = 0; i < 256; i++) { float u = i / 255f * 2 - 1; shadeBand.SetPixel(i, 0, new Color(0, 0, 0, Mathf.Clamp01(1.25f - u * u * 1.25f))); }
                shadeBand.Apply();
            }
            Texture(r, shadeBand, new Color(1, 1, 1, alpha));
        }

        public static void Glow(Rect r, float amount)
        {
            if (amount <= .001f) return;
            if (stripe == null) stripe = GameUi.Stripe(380, 55, 0);
            Texture(r, stripe, Fade(Warm, Mathf.Min(1.6f, amount) * .22f));
        }

        public static float KeyCap(float x, float y, string key, float height = 34, float alpha = 1)
        {
            float width = Mathf.Max(height, TextWidth(key, height * .5f) + height * .55f);
            Fill(new Rect(x, y, width, height), new Color(1, 1, 1, .09f * alpha));
            Fill(new Rect(x, y + height - 2, width, 2), new Color(1, 1, 1, .26f * alpha));
            Label(new Rect(x, y, width, height), key, height * .5f, Fade(Paper, alpha), false, TextAnchor.MiddleCenter, false);
            return width;
        }

        public static void Footer(params string[] pairs) => Footer(false, pairs);

        public static void Footer(bool right, params string[] pairs)
        {
            float total = 0;
            for (int i = 0; i + 1 < pairs.Length; i += 2) total += Mathf.Max(30, TextWidth(pairs[i], 15) + 16.5f) + 10 + TextWidth(pairs[i + 1], 19) + 40;
            float x = right ? Width - 64 - total + 40 : Margin, y = Height - 64 - 30;
            if (right) ShadeBand(new Rect(x - 90, y - 26, total + 140, 82), .6f);
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                x += KeyCap(x, y, pairs[i], 30, .55f) + 10;
                Label(new Rect(x, y, 400, 30), pairs[i + 1], 19, Fade(Paper, .45f), false, TextAnchor.MiddleLeft, false);
                x += TextWidth(pairs[i + 1], 19) + 40;
            }
        }

        public static void Prompt(string key, string text, bool hideNow = false)
        {
            if (hideNow) { promptShown = 0; return; }
            if (Event.current.type == EventType.Repaint && promptFrame != Time.frameCount)
            {
                promptFrame = Time.frameCount;
                bool on = !string.IsNullOrEmpty(text);
                if (on) { promptKey = key; promptText = text; }
                promptShown = Mathf.MoveTowards(promptShown, on ? 1 : 0, Time.unscaledDeltaTime / (on ? .16f : .12f));
            }
            if (promptShown <= .001f || string.IsNullOrEmpty(promptText)) return;
            float e = OutCubic(promptShown);
            float width = TextWidth(promptText, 26) + 70, x = (Width - width) / 2, y = Height * .64f;
            Push(e, 0, 6 * (1 - e));
            ShadeBand(new Rect(x - 120, y - 18, width + 240, 82), .55f);
            float cap = KeyCap(x, y + 4, promptKey, 38);
            Label(new Rect(x + cap + 16, y, width, 46), promptText, 26, Paper, false, TextAnchor.MiddleLeft, false);
            Pop();
        }

        public static void Hold(float progress)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (progress <= .001f && lastHold > .85f) { holdFlashAt = Time.unscaledTime; Play(MenuSounds.Confirm, .7f); }
                lastHold = progress;
                holdShown = progress <= .001f ? 0 : Mathf.MoveTowards(holdShown, progress, Time.unscaledDeltaTime * 3);
            }
            float width = 420, x = (Width - width) / 2, y = Height * .64f + 70;
            float flash = 1 - Mathf.Clamp01((Time.unscaledTime - holdFlashAt) / .45f);
            if (holdShown > .001f)
            {
                Fill(new Rect(x, y, width, 3), Fade(Paper, .18f));
                Fill(new Rect(x, y, width * holdShown, 3), Warm);
                Fill(new Rect(x + width * holdShown - 3, y - 2, 6, 7), Fade(Warm, .5f));
            }
            if (flash > 0)
            {
                Fill(new Rect(x - 6 * (1 - flash), y - 1, width + 12 * (1 - flash), 5), Fade(Warm, flash));
                Glow(new Rect(x - 80, y - 30, width + 160, 64), flash);
            }
        }

        public static void Subtitle(string text, float bottom = 150)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (on) subtitle.Set(Glue(text));
            float shown = subtitleFade.Step(on, .18f, .3f);
            if (shown <= .001f || string.IsNullOrEmpty(subtitle.Current)) return;
            float lineIn = Reveal(subtitle.Age, 0, .22f);
            string current = subtitle.Current;
            float width = Mathf.Min(1180, Width - 2 * Margin);
            int split = current.IndexOf(": ");
            string speaker = split > 0 && split <= 24 ? current.Substring(0, split) : "";
            string line = speaker != "" ? current.Substring(split + 2) : current;
            float height = TextHeight(line, 30, width);
            float top = Height - bottom - height - (speaker != "" ? 38 : 0);
            Push(OutCubic(shown));
            ShadeBand(new Rect((Width - width) / 2 - 220, top - 30, width + 440, Height - bottom - top + 60), .72f);
            Push(lineIn, 0, 8 * (1 - lineIn));
            if (speaker != "") Label(new Rect((Width - width) / 2, top, width, 34), speaker, 22, Fade(Warm, .9f), true, TextAnchor.UpperCenter);
            Label(new Rect((Width - width) / 2, top + (speaker != "" ? 38 : 0), width, height + 4), line, 30, Paper, false, TextAnchor.UpperCenter);
            Pop(); Pop();
        }

        public static void Heading(float x, float y, string caption, string title, float width = 1200)
        {
            if (!string.IsNullOrEmpty(caption)) Label(new Rect(x, y, width, 30), caption, 20, Fade(Paper, .45f));
            Label(new Rect(x, y + 34, width, 60), title, 40, Paper, true);
            Fill(new Rect(x, y + 104, Mathf.Min(width, 800), 1), Fade(Paper, .12f));
        }

        static bool MouseIn(Rect virtualRect, out bool clicked)
        {
            clicked = false;
            var e = Event.current;
            if (e == null || layerLocked || Cursor.lockState == CursorLockMode.Locked) return false;
            bool over = S(virtualRect).Contains(e.mousePosition);
            clicked = over && e.type == EventType.MouseDown && e.button == 0;
            return over;
        }

        public static void Play(AudioClip clip, float volume = 1)
        {
            if (clip == null) return;
            if (sound == null)
            {
                var go = new GameObject("Hud sound") { hideFlags = HideFlags.HideAndDontSave };
                sound = go.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 0; sound.ignoreListenerPause = true;
            }
            sound.PlayOneShot(clip, volume);
        }

        public static void Tick() => Play(MenuSounds.Tick, .6f);

        public static bool Action(float x, float y, string key, string label)
        {
            float width = TextWidth(label, 22) + 60;
            var area = new Rect(x - 12, y - 6, width + 24, 46);
            bool over = MouseIn(area, out bool clicked);
            string id = key + label;
            hover.TryGetValue(id, out float h);
            if (Event.current.type == EventType.Repaint)
            {
                if (!hoverFrame.TryGetValue(id, out int frame) || frame != Time.frameCount)
                {
                    hoverFrame[id] = Time.frameCount;
                    if (over && h <= .001f) Tick();
                    h = Mathf.MoveTowards(h, over ? 1 : 0, Time.unscaledDeltaTime * 8);
                    hover[id] = h;
                }
            }
            Glow(new Rect(x - 40, y - 16, width + 80, 66), h);
            float cap = KeyCap(x, y, key, 34, .7f + .3f * h);
            Label(new Rect(x + cap + 12 + 4 * h, y, width, 34), label, 22, Fade(Paper, .7f + .3f * h), false, TextAnchor.MiddleLeft, false);
            if (clicked) Event.current.Use();
            return clicked;
        }

        public static int Choices(HudChoices list, float x, float y, IList<string> labels, IList<bool> enabled = null, float size = 32, float rowHeight = 70, float width = 900)
        {
            int count = labels.Count;
            if (count == 0) return -1;
            if (list.glow.Length != count) { list.glow = new float[count]; list.selected = Mathf.Clamp(list.selected, 0, count - 1); }
            bool Usable(int i) => enabled == null || i >= enabled.Count || enabled[i];
            if (!Usable(list.selected)) for (int i = 0; i < count; i++) if (Usable(i)) { list.selected = i; break; }
            int activated = -1;
            bool repaint = Event.current.type == EventType.Repaint;
            float age = Time.unscaledTime - list.revealAt;
            for (int i = 0; i < count; i++)
            {
                var row = new Rect(x, y + i * rowHeight, width, rowHeight - 6);
                bool over = MouseIn(row, out bool clicked);
                if (!Usable(i)) { if (clicked) { Play(MenuSounds.Deny, .6f); Event.current.Use(); } continue; }
                if (clicked) { list.selected = i; activated = i; Event.current.Use(); }
                else if (repaint && list.handledFrame != Time.frameCount && over && list.selected != i) { list.selected = i; Tick(); }
            }
            if (repaint && list.handledFrame != Time.frameCount)
            {
                list.handledFrame = Time.frameCount;
                var keys = Keyboard.current;
                if (keys != null && !layerLocked)
                {
                    int step = keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame ? -1 : keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame ? 1 : 0;
                    if (step != 0)
                        for (int n = 1; n <= count; n++) { int i = (list.selected + step * n + count * 4) % count; if (Usable(i)) { list.selected = i; Tick(); break; } }
                    if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) activated = list.selected;
                }
                for (int i = 0; i < count; i++) list.glow[i] = Mathf.MoveTowards(list.glow[i], i == list.selected ? 1 : 0, Time.unscaledDeltaTime * 6);
            }
            float flashAge = Time.unscaledTime - list.flashAt;
            for (int i = 0; i < count; i++)
            {
                float g = list.glow[i];
                float flash = i == list.flashIndex ? 1 - Mathf.Clamp01(flashAge / .3f) : 0;
                float shown = Reveal(age, .06f * i, .24f);
                var row = new Rect(x, y + i * rowHeight, width, rowHeight - 6);
                Push(shown, -10 * (1 - shown));
                Glow(new Rect(x - 40, row.y - rowHeight * .2f, width * .85f, rowHeight * 1.4f), g + flash * .6f);
                float alpha = Usable(i) ? .5f + g * .5f : .2f;
                Label(new Rect(x + 20 + g * 12, row.y, width - 40, row.height), labels[i], size, Fade(Color.Lerp(Paper, Warm, flash), alpha), false, TextAnchor.MiddleLeft, false);
                Pop();
            }
            if (activated >= 0 && !Usable(activated)) { Play(MenuSounds.Deny, .6f); activated = -1; }
            if (activated >= 0) { list.flashIndex = activated; list.flashAt = Time.unscaledTime; Play(MenuSounds.Confirm, .5f); }
            return activated;
        }

        public static void Meter(Vector2 screenPoint, float amount, bool chasing)
        {
            float k = Scale, width = 90 * k, height = 4 * k;
            var rect = new Rect(screenPoint.x - width / 2, screenPoint.y, width, height);
            float fade = Mathf.Clamp01(amount * 6);
            float pulse = chasing ? .7f + .3f * Mathf.Sin(Time.unscaledTime * 9) : 1;
            var previous = GUI.color;
            GUI.color = new Color(0, 0, 0, .6f * fade); GUI.DrawTexture(new Rect(rect.x - k, rect.y - k, rect.width + 2 * k, rect.height + 2 * k), Texture2D.whiteTexture);
            GUI.color = Fade(chasing ? Alarm : Warm, fade * pulse); GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(amount), rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
