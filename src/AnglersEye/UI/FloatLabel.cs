using System.Collections.Generic;
using AnglersEye.Core;
using AnglersEye.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnglersEye.UI
{
    /// <summary>
    /// A small label above your float naming the fish nibbling or heading for it, and whether the
    /// bait on the float works on it (PLAN §2.2 item 1). Outlined text, no box, like the game's own
    /// world labels. Hidden once a fish is hooked.
    /// </summary>
    internal static class FloatLabel
    {
        private const float Above = 0.8f;
        private const float TextSize = 20f;
        private const float StarSize = 18f;

        private static readonly List<Image> StarPool = new List<Image>();

        private static RectTransform _root;
        private static TextMeshProUGUI _name, _bait;
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
            int quality = FishCatalog.Quality(s.Subject);
            string onFloat = s.Float.GetBait();
            bool works = false;
            foreach (BaitOption b in info.Baits)
            {
                if (b.BaitId == onFloat)
                {
                    works = true;
                    break;
                }
            }
            BaitAdvice best = Tackle.Advise(s.Player, info);
            string needed = best != null ? best.Best.BaitName : "?";

            bool drawn = UiUtil.Stars(_root, StarPool, quality, 1, StarSize);
            UiUtil.SetText(_name, UiUtil.NameWithLevel(info.Name, quality, drawn));
            UiUtil.SiblingIndex(_bait.transform, _root.childCount - 1);
            UiUtil.SetText(_bait, Labels.OnFloat(works, needed, UiUtil.Glyphs));
            _bait.color = works ? Palette.Normal : Palette.Bad;

            if (!_root.gameObject.activeSelf)
                _root.gameObject.SetActive(true);
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
            // No ?? on Unity objects: a destroyed HUD compares equal to null only through Unity's ==.
            if (_root != null && _builtFor == hud)
                return true;
            StarPool.Clear();
            _root = UiUtil.Rect("AnglersEyeFloatLabel", hud.m_crosshair.transform.parent);
            _root.pivot = new Vector2(0.5f, 0f);
            var layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fit = _root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _name = UiUtil.OutlinedText(_root, "Name", TextSize, TextAlignmentOptions.Center);
            _name.color = Palette.Normal;
            _bait = UiUtil.OutlinedText(_root, "Bait", TextSize, TextAlignmentOptions.Center);
            _builtFor = hud;
            return true;
        }
    }
}
