using AnglersEye.Core;
using AnglersEye.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnglersEye.UI
{
    /// <summary>
    /// A small label above your float naming the fish nibbling or heading for it, and whether the
    /// bait on the float works on it (PLAN §2.2 item 1). Hidden once a fish is hooked.
    /// </summary>
    internal static class FloatLabel
    {
        private const float Above = 0.8f;

        private static RectTransform _root;
        private static TextMeshProUGUI _text;
        private static Hud _builtFor;

        public static void Update(FishingSnapshot s)
        {
            if (!Features.On(Feature.FloatLabel) || s.Float == null || s.Catch != null || s.Subject == null)
            {
                Hide();
                return;
            }
            Camera cam = Utils.GetMainCamera();
            if (cam == null)
            {
                Hide();
                return;
            }
            Vector3 screen = cam.WorldToScreenPointScaled(s.Float.transform.position + Vector3.up * Above);
            if (screen.z < 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height)
            {
                Hide();
                return;
            }
            if (!Ensure())
                return;

            FishInfo info = FishCatalog.For(s.Subject);
            string onFloat = s.Float.GetBait();
            bool works = false;
            foreach (BaitOption b in info.Baits)
                if (b.BaitId == onFloat)
                    works = true;
            BaitAdvice best = BaitAdvisor.Advise(info.Baits, Tackle.Carried(s.Player));
            string needed = best != null ? best.Best.BaitName : "?";
            string text = Labels.OnFloat(info.Name, FishCatalog.Quality(s.Subject), works, needed, UiUtil.Glyphs);

            if (!_root.gameObject.activeSelf)
                _root.gameObject.SetActive(true);
            if (_text.text != text)
                _text.text = text;
            _text.color = works ? Palette.Idle : Palette.Missing;
            _root.position = screen;
            _root.localScale = Vector3.one * PluginConfig.Scale.Value;
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
            if (_root != null && _builtFor == hud)
                return true;
            _root = UiUtil.Rect("AnglersEyeFloatLabel", hud.m_crosshair.transform.parent);
            _root.pivot = new Vector2(0.5f, 0f);
            var bg = _root.gameObject.AddComponent<Image>();
            bg.sprite = UiUtil.White;
            bg.color = Palette.Backing;
            bg.raycastTarget = false;
            var layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fit = _root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _text = UiUtil.Text(_root, "Text", 16f, TextAlignmentOptions.Center);
            _builtFor = hud;
            return true;
        }
    }
}
