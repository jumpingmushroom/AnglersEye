using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnglersEye.UI
{
    /// <summary>One line of text under the crosshair on a subtle dark backing (PLAN §2.3).</summary>
    internal static class Strip
    {
        private const float BelowCrosshair = 40f;

        private static RectTransform _root;
        private static TextMeshProUGUI _text;
        private static Hud _builtFor;

        public static void Show(string text, Color color)
        {
            if (!Ensure())
                return;
            if (!_root.gameObject.activeSelf)
                _root.gameObject.SetActive(true);
            if (_text.text != text)
                _text.text = text;
            _text.color = color;
            Place();
        }

        public static void Hide()
        {
            if (_root != null && _root.gameObject.activeSelf)
                _root.gameObject.SetActive(false);
        }

        private static bool Ensure()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_crosshair == null)
                return false;
            // No ?? on Unity objects: a destroyed HUD compares equal to null only through Unity's ==.
            if (_root != null && _builtFor == hud)
                return true;

            _root = UiUtil.Rect("AnglersEyeStrip", hud.m_crosshair.transform.parent);
            var bg = _root.gameObject.AddComponent<Image>();
            bg.sprite = UiUtil.White;
            bg.color = Palette.Backing;
            bg.raycastTarget = false;
            var layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 3, 3);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fit = _root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _text = UiUtil.Text(_root, "Text", 18f, TextAlignmentOptions.Center);
            _builtFor = hud;
            return true;
        }

        private static void Place()
        {
            var cross = (RectTransform)Hud.instance.m_crosshair.transform;
            _root.anchorMin = cross.anchorMin;
            _root.anchorMax = cross.anchorMax;
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = cross.anchoredPosition +
                new Vector2(PluginConfig.OffsetX.Value, -BelowCrosshair + PluginConfig.OffsetY.Value);
            _root.localScale = Vector3.one * PluginConfig.Scale.Value;
        }
    }
}
