using System.Collections.Generic;
using AnglersEye.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnglersEye.UI
{
    /// <summary>
    /// The fishing panel: a small dark, gold-edged frame just above the stamina bar (or above
    /// eitr/adrenaline while they show). A title row with the fish and its stars, and a body
    /// row with bait advice or a big state word (BITE!, REEL, WAIT + struggle bar) and the forecast.
    /// </summary>
    internal static class FishingPanel
    {
        private const float Gap = 6f;
        private const float MinWidth = 170f;
        private const float TextSize = 20f;
        private const float BigSize = 30f;
        private const float StarSize = 18f;
        private const float BarHeight = 5f;

        /// <summary>Hud's eitr/adrenaline animators fade their bar with this bool (Hud.UpdateEitr/UpdateAdrenaline).</summary>
        private static readonly int VisibleHash = Animator.StringToHash("Visible");

        private static readonly Vector3[] Corners = new Vector3[4];
        private static readonly List<Image> StarPool = new List<Image>();

        private static RectTransform _root, _titleRow, _bodyRow, _bodyCol, _bar, _barFill;
        private static TextMeshProUGUI _title, _body, _forecast;
        private static Image _barFillImage;
        private static Hud _builtFor;

        public static void Show(PanelView v)
        {
            if (v == null || !Ensure())
            {
                Hide();
                return;
            }

            bool title = v.Title != null;
            SetActive(_titleRow, title);
            if (title)
            {
                bool drawn = UiUtil.Stars(_titleRow, StarPool, v.Level, 1, StarSize);
                UiUtil.SetText(_title, UiUtil.NameWithLevel(v.Title, v.Level, drawn));
            }

            bool body = v.Body != null;
            bool forecast = v.Forecast != null;
            SetActive(_bodyRow, body || forecast);
            SetActive(_bodyCol, body);
            if (body)
            {
                UiUtil.SetText(_body, v.Body);
                _body.color = Palette.For(v.BodyTone);
                _body.fontSize = v.BodyIsBig ? BigSize : TextSize;
                _body.fontStyle = v.BodyIsBig ? FontStyles.Bold : FontStyles.Normal;
            }
            SetActive(_bar, body && v.Bar.HasValue);
            if (body && v.Bar.HasValue)
            {
                _barFill.anchorMax = new Vector2(v.Bar.Value, 1f);
                _barFillImage.color = Palette.For(v.BodyTone);
            }
            SetActive(_forecast.rectTransform, forecast);
            if (forecast)
            {
                UiUtil.SetText(_forecast, v.Forecast);
                _forecast.color = Palette.For(v.ForecastTone);
            }

            SetActive(_root, true);
            Place(_builtFor);
        }

        public static void Hide()
        {
            if (_root != null)
                SetActive(_root, false);
        }

        private static void SetActive(RectTransform rt, bool on)
        {
            if (rt.gameObject.activeSelf != on)
                rt.gameObject.SetActive(on);
        }

        private static bool Ensure()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_staminaBar2Root == null || hud.m_staminaBar2Root.parent == null)
                return false;
            // No ?? on Unity objects: a destroyed HUD compares equal to null only through Unity's ==.
            if (_root != null && _builtFor == hud)
                return true;

            StarPool.Clear();
            _root = UiUtil.Rect("AnglersEyePanel", hud.m_staminaBar2Root.parent);
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0f);
            var frame = _root.gameObject.AddComponent<Image>();
            frame.sprite = UiUtil.Frame;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            var le = _root.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true; // in case the bars' parent lays out its children
            le.minWidth = MinWidth;
            var layout = _root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 10);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fit = _root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _titleRow = Row("Title", _root, 4f);
            _title = UiUtil.OutlinedText(_titleRow, "Name", TextSize, TextAlignmentOptions.Center);
            _title.color = Palette.Normal;

            _bodyRow = Row("Body", _root, 18f);
            _bodyCol = UiUtil.Rect("State", _bodyRow);
            var col = _bodyCol.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 1f;
            col.childAlignment = TextAnchor.MiddleCenter;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            _body = UiUtil.OutlinedText(_bodyCol, "Text", TextSize, TextAlignmentOptions.Center);

            _bar = UiUtil.Rect("StruggleBar", _bodyCol);
            var track = _bar.gameObject.AddComponent<Image>();
            track.sprite = UiUtil.White;
            track.color = Palette.Track;
            track.raycastTarget = false;
            var barLe = _bar.gameObject.AddComponent<LayoutElement>();
            barLe.minHeight = barLe.preferredHeight = BarHeight;
            _barFill = UiUtil.Rect("Fill", _bar);
            _barFill.anchorMin = Vector2.zero;
            _barFill.anchorMax = Vector2.one;
            _barFill.offsetMin = _barFill.offsetMax = Vector2.zero;
            _barFillImage = _barFill.gameObject.AddComponent<Image>();
            _barFillImage.sprite = UiUtil.White;
            _barFillImage.raycastTarget = false;

            _forecast = UiUtil.OutlinedText(_bodyRow, "Forecast", TextSize, TextAlignmentOptions.Center);

            _builtFor = hud;
            return true;
        }

        private static RectTransform Row(string name, Transform parent, float spacing)
        {
            RectTransform rt = UiUtil.Rect(name, parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            return rt;
        }

        /// <summary>
        /// Centred on the stamina bar, Gap above its top, or above eitr/adrenaline while those are
        /// shown; recomputed every frame as the bars resize, fade in and out, and move (build mode,
        /// ship HUD). The stamina bar counts even while faded, so the panel doesn't jump.
        /// </summary>
        private static void Place(Hud hud)
        {
            var parent = (RectTransform)_root.parent;
            hud.m_staminaBar2Root.GetWorldCorners(Corners);
            float x = (Corners[0].x + Corners[2].x) * 0.5f;
            float z = Corners[0].z;
            float top = Corners[1].y;
            top = Top(hud.m_eitrBarRoot, hud.m_eitrAnimator, top);
            top = Top(hud.m_adrenalineBarRoot, hud.m_adrenalineAnimator, top);

            Vector3 local = parent.InverseTransformPoint(new Vector3(x, top, z));
            Vector2 anchor = parent.rect.center; // anchors are (0.5, 0.5)
            _root.anchoredPosition = new Vector2(local.x - anchor.x + PluginConfig.OffsetX.Value,
                local.y - anchor.y + Gap + PluginConfig.OffsetY.Value);
            _root.localScale = Vector3.one * PluginConfig.Scale.Value;
        }

        /// <summary>
        /// Eitr and adrenaline roots are never deactivated, only faded by their animator's Visible
        /// bool, so that bool (not activeInHierarchy) says whether the bar is on screen.
        /// </summary>
        private static float Top(RectTransform bar, Animator anim, float top)
        {
            if (bar == null || !bar.gameObject.activeInHierarchy || anim == null || !anim.isActiveAndEnabled
                || anim.runtimeAnimatorController == null || !anim.GetBool(VisibleHash))
                return top;
            bar.GetWorldCorners(Corners);
            return Mathf.Max(top, Corners[1].y);
        }
    }
}
