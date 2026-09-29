using AnglersEye.Core.Model;
using TMPro;
using UnityEngine;

namespace AnglersEye.UI
{
    internal static class UiUtil
    {
        private static Sprite _white;
        private static Glyphs _glyphs;
        private static TMP_FontAsset _glyphFont;

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
    }
}
