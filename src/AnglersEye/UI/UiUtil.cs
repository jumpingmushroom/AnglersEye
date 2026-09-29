using System.Collections.Generic;
using AnglersEye.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnglersEye.UI
{
    internal static class UiUtil
    {
        private static Sprite _white;
        private static Sprite _frame;
        private static Glyphs _glyphs;
        private static TMP_FontAsset _glyphFont;
        private static EnemyHud _starFor;
        private static Sprite _star;
        private static Color _starColor = Color.white;

        /// <summary>A plain white sprite for backgrounds.</summary>
        public static Sprite White
        {
            get
            {
                if (_white != null)
                    return _white;
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (int i = 0; i < px.Length; i++)
                    px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px);
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                _white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                _white.hideFlags = HideFlags.HideAndDontSave;
                return _white;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>The HUD hover text's font, so our text matches the crosshair's.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                Hud hud = Hud.instance;
                return hud != null && hud.m_hoverName != null ? hud.m_hoverName.font : null;
            }
        }

        /// <summary>Label glyphs the HUD font can render, ASCII for the rest (PLAN §1.7.6).</summary>
        public static Glyphs Glyphs
        {
            get
            {
                TMP_FontAsset f = Font;
                if (_glyphs == null || f != _glyphFont)
                {
                    _glyphFont = f;
                    _glyphs = f == null ? Glyphs.Ascii() : Glyphs.Resolve(c => f.HasCharacter(c));
                }
                return _glyphs;
            }
        }

        public static TextMeshProUGUI Text(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            RectTransform rt = Rect(name, parent);
            // Added while inactive so TMP's Awake runs after the font is set; otherwise it looks
            // for its default LiberationSans (not shipped with the game) and logs a warning.
            rt.gameObject.SetActive(false);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = Font;
            if (font != null)
                t.font = font;
            rt.gameObject.SetActive(true);
            t.fontSize = size;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.richText = true;
            t.raycastTarget = false;
            t.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            return t;
        }

        /// <summary>
        /// HUD-font text with the outlined material of the vanilla hover text, so it reads over
        /// grass and water without a backing box.
        /// </summary>
        public static TextMeshProUGUI OutlinedText(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            TextMeshProUGUI t = Text(parent, name, size, align);
            Hud hud = Hud.instance;
            TMP_Text hover = hud != null ? hud.m_hoverName : null;
            // A material only fits the font atlas it was made for.
            if (hover != null && hover.font == t.font && hover.fontSharedMaterial != null)
            {
                t.fontSharedMaterial = hover.fontSharedMaterial;
            }
            else
            {
                t.outlineWidth = 0.2f;
                t.outlineColor = new Color32(0, 0, 0, 255);
            }
            return t;
        }

        private const int FrameSize = 32;
        private const float FrameRadius = 6f;
        private const float FrameLine = 1.25f;
        private const int FrameBorder = 8;

        /// <summary>
        /// A 9-sliced rounded rectangle in Valheim's panel palette (dark fill, thin warm-gold line),
        /// drawn in code so it doesn't depend on any game sprite. Use with Image.Type.Sliced.
        /// </summary>
        public static Sprite Frame
        {
            get
            {
                if (_frame != null)
                    return _frame;
                var tex = new Texture2D(FrameSize, FrameSize, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                var px = new Color32[FrameSize * FrameSize];
                float half = FrameSize / 2f;
                for (int y = 0; y < FrameSize; y++)
                {
                    for (int x = 0; x < FrameSize; x++)
                    {
                        // Signed distance to the rounded rectangle's edge (negative inside).
                        float qx = Mathf.Abs(x + 0.5f - half) - (half - FrameRadius);
                        float qy = Mathf.Abs(y + 0.5f - half) - (half - FrameRadius);
                        float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                            + Mathf.Min(Mathf.Max(qx, qy), 0f) - FrameRadius;
                        float coverage = Mathf.Clamp01(0.5f - d);
                        float line = Mathf.Clamp01(d + FrameLine + 0.5f);
                        Color c = Color.Lerp(Palette.Frame, Palette.FrameLine, line);
                        c.a *= coverage;
                        px[y * FrameSize + x] = c;
                    }
                }
                tex.SetPixels32(px);
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                _frame = Sprite.Create(tex, new Rect(0f, 0f, FrameSize, FrameSize), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(FrameBorder, FrameBorder, FrameBorder, FrameBorder));
                _frame.hideFlags = HideFlags.HideAndDontSave;
                return _frame;
            }
        }

        /// <summary>The vanilla creature-level star (EnemyHud's level_2 icon), or null if it can't be found.</summary>
        public static Sprite Star
        {
            get
            {
                EnemyHud eh = EnemyHud.instance;
                if (eh == null)
                    return null;
                // Look once per EnemyHud: a missing sprite stays missing (Lv N text), no per-frame Find.
                if (_starFor != eh)
                {
                    _starFor = eh;
                    _star = null;
                    Transform level = eh.m_baseHud != null ? eh.m_baseHud.transform.Find("level_2") : null;
                    if (level != null)
                    {
                        foreach (Image i in level.GetComponentsInChildren<Image>(true))
                        {
                            if (i.sprite == null)
                                continue;
                            _star = i.sprite;
                            _starColor = i.color;
                            break;
                        }
                    }
                }
                return _star;
            }
        }

        /// <summary>
        /// Show quality − 1 star icons in a layout row starting at sibling index first, reusing and
        /// growing the pool. False when stars are due but the star sprite is missing: the caller
        /// then shows <see cref="Labels.Level"/> as text.
        /// </summary>
        public static bool Stars(RectTransform row, List<Image> pool, int quality, int first, float size)
        {
            int n = quality - 1;
            Sprite star = n > 0 ? Star : null;
            if (star == null)
                n = 0;
            while (pool.Count < n)
            {
                RectTransform rt = Rect("Star" + pool.Count, row);
                var img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = size;
                le.minHeight = le.preferredHeight = size;
                pool.Add(img);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                bool on = i < n;
                if (pool[i].gameObject.activeSelf != on)
                    pool[i].gameObject.SetActive(on);
                if (!on)
                    continue;
                pool[i].sprite = star;
                pool[i].color = _starColor;
                SiblingIndex(pool[i].transform, first + i);
            }
            return quality <= 1 || star != null;
        }

        /// <summary>Move a layout child only when it isn't already there (each move re-lays out the row).</summary>
        public static void SiblingIndex(Transform t, int index)
        {
            if (t.GetSiblingIndex() != index)
                t.SetSiblingIndex(index);
        }

        /// <summary>A fish's name, with "Lv N" appended when its stars couldn't be drawn.</summary>
        public static string NameWithLevel(string name, int quality, bool starsDrawn)
        {
            string lv = starsDrawn ? "" : Labels.Level(quality);
            return lv.Length == 0 ? name : name + " " + lv;
        }

        /// <summary>Swap characters the HUD font lacks for ASCII ("…" → "...").</summary>
        public static string Printable(string s, TMP_Text t)
        {
            if (s != null && s.IndexOf('…') >= 0 && t.font != null && !t.font.HasCharacter('…'))
                return s.Replace("…", "...");
            return s;
        }

        public static void SetText(TMP_Text t, string s)
        {
            s = Printable(s, t);
            if (t.text != s)
                t.text = s;
        }
    }
}
