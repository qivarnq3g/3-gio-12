using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Gameplay
{
    public sealed class HudChoices
    {
        public int selected;
        internal int handledFrame = -1, count;
        internal float[] glow = new float[0];
        public void Reset() { selected = 0; }
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
        static AudioSource sound;        static readonly Dictionary<long, GUIStyle> styles = new Dictionary<long, GUIStyle>();

        static Font Regular { get { if (regular == null) regular = Resources.Load<Font>("Fonts/BeVietnamPro-Regular"); return regular; } }
        static Font Medium { get { if (medium == null) medium = Resources.Load<Font>("Fonts/BeVietnamPro-Medium"); return medium; } }

        public static Color Fade(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);
        public static Rect S(Rect r) { float k = Scale; return new Rect(r.x * k, r.y * k, r.width * k, r.height * k); }
        public static Rect S(float x, float y, float w, float h) => S(new Rect(x, y, w, h));

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

        public static void Label(Rect r, string text, float size, Color color, bool bold = false, TextAnchor align = TextAnchor.UpperLeft, bool wrap = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            var previous = GUI.color; GUI.color = color;
            GUI.Label(S(r), text, Style(size, bold, align, wrap));
            GUI.color = previous;
        }

        public static float TextHeight(string text, float size, float width, bool bold = false) =>
            Style(size, bold).CalcHeight(new GUIContent(text), width * Scale) / Scale;

        public static float TextWidth(string text, float size, bool bold = false) =>
            Style(size, bold, TextAnchor.UpperLeft, false).CalcSize(new GUIContent(text)).x / Scale;

        public static void Fill(Rect r, Color color)
        {
            var previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(S(r), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void Texture(Rect r, Texture2D texture, Color color)
        {
            var previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(S(r), texture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        public static void Backdrop(float alpha = 1)
        {
            if (grain == null) grain = GameUi.Mottle(192, 108, 37);
            Fill(new Rect(0, 0, Width, Height), Fade(Night, alpha));
            Texture(new Rect(0, 0, Width, Height), grain, new Color(.11f, .1f, .085f, .55f * alpha));
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
            Texture(r, stripe, Fade(Warm, amount * .22f));
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

        public static void Prompt(string key, string text, float progress = 0)
        {
            float width = TextWidth(text, 26) + 70, x = (Width - width) / 2, y = Height * .64f;
            ShadeBand(new Rect(x - 120, y - 18, width + 240, 82), .55f);
            float cap = KeyCap(x, y + 4, key, 38);
            Label(new Rect(x + cap + 16, y, width, 46), text, 26, Paper, false, TextAnchor.MiddleLeft, false);
            if (progress > .01f)
            {
                Fill(new Rect(x, y + 56, width, 3), Fade(Paper, .18f));
                Fill(new Rect(x, y + 56, width * Mathf.Clamp01(progress), 3), Warm);
            }
        }

        public static void Subtitle(string text, float bottom = 150)
        {
            if (string.IsNullOrEmpty(text)) return;
            float width = Mathf.Min(1180, Width - 2 * Margin);
            int split = text.IndexOf(": ");
            string speaker = split > 0 && split <= 24 ? text.Substring(0, split) : "";
            string line = speaker != "" ? text.Substring(split + 2) : text;
            float height = TextHeight(line, 30, width);
            float top = Height - bottom - height - (speaker != "" ? 38 : 0);
            ShadeBand(new Rect((Width - width) / 2 - 220, top - 30, width + 440, Height - bottom - top + 60), .72f);
            if (speaker != "") Label(new Rect((Width - width) / 2, top, width, 34), speaker, 22, Fade(Warm, .9f), true, TextAnchor.UpperCenter);
            Label(new Rect((Width - width) / 2, top + (speaker != "" ? 38 : 0), width, height + 4), line, 30, Paper, false, TextAnchor.UpperCenter);
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
            if (e == null || Cursor.lockState == CursorLockMode.Locked) return false;
            bool over = S(virtualRect).Contains(e.mousePosition);
            clicked = over && e.type == EventType.MouseDown && e.button == 0;
            return over;
        }

        public static void Tick()
        {
            if (sound == null)
            {
                var go = new GameObject("Hud sound") { hideFlags = HideFlags.HideAndDontSave };
                sound = go.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 0; sound.ignoreListenerPause = true;
            }
            sound.PlayOneShot(MenuSounds.Tick, .6f);
        }

        public static bool Action(float x, float y, string key, string label)
        {
            float width = TextWidth(label, 22) + 60;
            var area = new Rect(x - 12, y - 6, width + 24, 46);
            bool over = MouseIn(area, out bool clicked);
            Glow(new Rect(x - 40, y - 16, width + 80, 66), over ? 1 : 0);
            float cap = KeyCap(x, y, key, 34, over ? 1 : .7f);
            Label(new Rect(x + cap + 12, y, width, 34), label, 22, Fade(Paper, over ? 1 : .7f), false, TextAnchor.MiddleLeft, false);
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
            for (int i = 0; i < count; i++)
            {
                var row = new Rect(x, y + i * rowHeight, width, rowHeight - 6);
                bool over = MouseIn(row, out bool clicked);
                if (!Usable(i)) continue;
                if (clicked) { list.selected = i; activated = i; Event.current.Use(); }
                else if (repaint && list.handledFrame != Time.frameCount && over && list.selected != i) { list.selected = i; Tick(); }
            }
            if (repaint && list.handledFrame != Time.frameCount)
            {
                list.handledFrame = Time.frameCount;
                var keys = Keyboard.current;
                if (keys != null)
                {
                    int step = keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame ? -1 : keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame ? 1 : 0;
                    if (step != 0)
                        for (int n = 1; n <= count; n++) { int i = (list.selected + step * n + count * 4) % count; if (Usable(i)) { list.selected = i; Tick(); break; } }
                    if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) activated = list.selected;
                }
                for (int i = 0; i < count; i++) list.glow[i] = Mathf.MoveTowards(list.glow[i], i == list.selected ? 1 : 0, Time.unscaledDeltaTime * 6);
            }
            for (int i = 0; i < count; i++)
            {
                float g = list.glow[i];
                var row = new Rect(x, y + i * rowHeight, width, rowHeight - 6);
                Glow(new Rect(x - 40, row.y - rowHeight * .2f, width * .85f, rowHeight * 1.4f), g);
                float alpha = Usable(i) ? .5f + g * .5f : .2f;
                Label(new Rect(x + 20 + g * 12, row.y, width - 40, row.height), labels[i], size, Fade(Paper, alpha), false, TextAnchor.MiddleLeft, false);
            }
            if (activated >= 0 && !Usable(activated)) activated = -1;
            if (activated >= 0) Tick();
            return activated;
        }

        public static void Meter(Vector2 screenPoint, float amount, bool chasing)
        {
            float k = Scale, width = 90 * k, height = 4 * k;
            var rect = new Rect(screenPoint.x - width / 2, screenPoint.y, width, height);
            var previous = GUI.color;
            GUI.color = new Color(0, 0, 0, .6f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = chasing ? Alarm : Warm; GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(amount), rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
