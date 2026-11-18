using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BaGio12.Gameplay
{
    public static class GameUi
    {
        public static readonly Color Ink = new Color(.93f, .91f, .87f);
        public static readonly Color Muted = new Color(.93f, .91f, .87f, .52f);
        public static readonly Color Faint = new Color(.93f, .91f, .87f, .22f);
        public static readonly Color Accent = new Color(.78f, .16f, .13f);
        public static readonly Color Shade = new Color(.02f, .02f, .025f, .82f);
        public static readonly Color Card = new Color(.06f, .06f, .07f, .72f);

        static readonly Dictionary<string, TMP_FontAsset> fonts = new Dictionary<string, TMP_FontAsset>();

        public static TMP_FontAsset Font(string resource)
        {
            if (fonts.TryGetValue(resource, out var cached) && cached != null) return cached;
            var source = Resources.Load<Font>(resource);
            TMP_FontAsset asset = null;
            if (source != null)
            {
                asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                if (asset != null && TMP_Settings.defaultFontAsset != null) asset.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            }
            fonts[resource] = asset != null ? asset : TMP_Settings.defaultFontAsset;
            return fonts[resource];
        }

        public static Texture2D Stripe(int width, int height, float shear)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[width * height];
            float slant = shear * height;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float v = y / (height - 1f) * 2 - 1;
                    float across = Mathf.Exp(-v * v * 3.2f);
                    float left = slant * y / (height - 1f);
                    float u = Mathf.Clamp01((x - left) / Mathf.Max(1, width - slant));
                    float along = Mathf.SmoothStep(0, 1, u / .12f) * Mathf.Pow(1 - u, 1.6f);
                    byte a = (byte)(255 * Mathf.Clamp01(across * along));
                    pixels[y * width + x] = new Color32(255, 255, 255, a);
                }
            texture.SetPixels32(pixels); texture.Apply();
            return texture;
        }

        public static Texture2D Mottle(int width, int height, int seed)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float n = Mathf.PerlinNoise(seed + x * .035f, seed + y * .035f) * .6f + Mathf.PerlinNoise(seed * 2 + x * .12f, y * .12f) * .4f;
                    float dx = x / (width - 1f) * 2 - 1, dy = y / (height - 1f) * 2 - 1;
                    float vignette = Mathf.Clamp01(1 - (dx * dx * .6f + dy * dy) * .55f);
                    byte v = (byte)(255 * Mathf.Clamp01(n * vignette));
                    pixels[y * width + x] = new Color32(v, v, v, 255);
                }
            texture.SetPixels32(pixels); texture.Apply();
            return texture;
        }

        public static Canvas Canvas(Transform owner, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            go.transform.SetParent(owner, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        public static RectTransform Box(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            if (color.a > 0) { var image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = false; }
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string value, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            text.text = value; text.fontSize = size; text.color = color; text.alignment = align;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        public static RawImage Raw(Transform parent, string name, Texture texture, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = texture; image.color = color; image.raycastTarget = false;
            return image;
        }

        public static Texture2D Gradient(Color from, Color to, bool vertical = false, int size = 256)
        {
            var texture = new Texture2D(vertical ? 1 : size, vertical ? size : 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int i = 0; i < size; i++)
            {
                var color = Color.Lerp(from, to, Mathf.SmoothStep(0, 1, i / (size - 1f)));
                if (vertical) texture.SetPixel(0, i, color); else texture.SetPixel(i, 0, color);
            }
            texture.Apply();
            return texture;
        }

        public static Texture2D Noise(int size = 256)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            var random = new System.Random(312);
            for (int i = 0; i < pixels.Length; i++) { byte v = (byte)random.Next(256); pixels[i] = new Color32(v, v, v, 255); }
            texture.SetPixels32(pixels); texture.Apply();
            return texture;
        }

        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static RectTransform KeyCap(Transform parent, string label, float height = 34)
        {
            var text = Text(parent, "Key " + label, label, height * .5f, Ink, TextAlignmentOptions.Center);
            float width = Mathf.Max(height, text.GetPreferredValues(label).x + height * .55f);
            var cap = Box(parent, "Cap " + label, new Color(1, 1, 1, .09f));
            var border = Box(cap, "Edge", new Color(1, 1, 1, .26f));
            Place(border, new Vector2(.5f, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(width, 2));
            cap.sizeDelta = new Vector2(width, height);
            text.transform.SetParent(cap, false); Fill((RectTransform)text.transform);
            return cap;
        }

        public static RectTransform ControlRow(Transform parent, string action, float height, params string[] keys) => ControlRow(parent, action, height, height * 4.6f, keys);

        public static RectTransform ControlRow(Transform parent, string action, float height, float keyColumn, params string[] keys)
        {
            var row = Box(parent, "Row " + action, Color.clear);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            float keysWidth = -layout.spacing;
            foreach (var key in keys) keysWidth += KeyCap(row, key, height).sizeDelta.x + layout.spacing;
            var gap = Box(row, "Gap", Color.clear); gap.sizeDelta = new Vector2(Mathf.Max(6, keyColumn - keysWidth), height);
            var text = Text(row, "Action", action, height * .56f, Ink);
            ((RectTransform)text.transform).sizeDelta = new Vector2(text.GetPreferredValues(action).x + 4, height);
            float width = -layout.spacing;
            foreach (RectTransform child in row) width += child.sizeDelta.x + layout.spacing;
            row.sizeDelta = new Vector2(width, height);
            return row;
        }

        public static bool PointerOver(RectTransform rect, out bool clicked)
        {
            clicked = false;
            var mouse = Mouse.current;
            if (mouse == null || !Cursor.visible) return false;
            bool over = RectTransformUtility.RectangleContainsScreenPoint(rect, mouse.position.ReadValue());
            clicked = over && mouse.leftButton.wasPressedThisFrame;
            return over;
        }
    }
}
