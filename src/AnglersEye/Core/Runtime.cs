using System;
using AnglersEye.Core.Model;
using AnglersEye.UI;
using UnityEngine;

namespace AnglersEye.Core
{
    /// <summary>Per-frame work, driven from the plugin's Update: state, then the fishing panel and float label (PLAN §2.3).</summary>
    internal static class Runtime
    {
        private const float AimInterval = 0.25f;

        private static float _nextAim;
        private static FishSighting _aim;
        private static BaitAdvice _aimAdvice;

        public static void Tick()
        {
            try
            {
                FishCatalog.EnsureBuilt();
                Compat.EnsureEvaluated();
                FishingState.Refresh();
                UpdatePanel(FishingState.Current);
                FloatLabel.Update(FishingState.Current);
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("Runtime.Tick", e);
                FishingPanel.Hide();
                FloatLabel.Hide();
            }
        }

        private static void UpdatePanel(FishingSnapshot s)
        {
            if (!PluginConfig.Enabled.Value || s.Player == null || !s.RodEquipped)
            {
                FishingPanel.Hide();
                return;
            }

            if (s.Catch != null)
            {
                Verdict? v = Features.On(Feature.Forecast) ? Forecast(s, s.Catch, true) : (Verdict?)null;
                FishingPanel.Show(PanelView.Hooked(FishCatalog.For(s.Catch).Name, FishCatalog.Quality(s.Catch),
                    s.Escaping, Features.On(Feature.StruggleIndicator), s.RemainingEscape, s.EscapeTotal, v));
                return;
            }

            if (s.Float != null && BiteCue.Active)
            {
                FishingPanel.Show(s.Subject != null
                    ? PanelView.Bite(FishCatalog.For(s.Subject).Name, FishCatalog.Quality(s.Subject))
                    : PanelView.Bite(null, 0));
                return;
            }

            if (s.Float != null)
            {
                if (!Features.On(Feature.Forecast) && !Features.On(Feature.FloatLabel))
                {
                    FishingPanel.Hide();
                    return;
                }
                if (s.Subject == null)
                {
                    FishingPanel.Show(PanelView.Waiting(null, 0, null));
                    return;
                }
                Verdict? v = Features.On(Feature.Forecast) ? Forecast(s, s.Subject, false) : (Verdict?)null;
                FishingPanel.Show(PanelView.Waiting(FishCatalog.For(s.Subject).Name, FishCatalog.Quality(s.Subject), v));
                return;
            }

            // Rod out, nothing cast: the fish smart bait would bait for.
            if (!Features.On(Feature.SmartBait))
            {
                FishingPanel.Hide();
                return;
            }
            if (Time.time >= _nextAim)
            {
                _nextAim = Time.time + AimInterval;
                _aim = Targeting.Aim(s.Player);
                FishInfo aimedFish;
                _aimAdvice = _aim != null && FishCatalog.All.TryGetValue(_aim.Species, out aimedFish)
                    ? Tackle.Advise(s.Player, aimedFish)
                    : null;
            }
            FishInfo target;
            if (_aim == null || !FishCatalog.All.TryGetValue(_aim.Species, out target))
            {
                FishingPanel.Hide();
                return;
            }
            FishingPanel.Show(PanelView.Target(target.Name, _aim.Quality, _aimAdvice, PluginConfig.ShowOdds.Value));
        }

        private static Verdict Forecast(FishingSnapshot s, Fish f, bool hooked)
        {
            FishInfo info = FishCatalog.For(f);
            return CatchForecast.Estimate(FishCatalog.Rod, info.Params, FishCatalog.Quality(f), s.LineLength,
                s.Player.GetSkillFactor(Skills.SkillType.Fishing), s.Player.GetStamina(), hooked,
                hooked ? s.RemainingEscape : 0f).Verdict;
        }
    }
}
