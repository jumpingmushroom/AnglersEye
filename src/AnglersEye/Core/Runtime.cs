using System;
using AnglersEye.Core.Model;
using AnglersEye.UI;
using UnityEngine;

namespace AnglersEye.Core
{
    /// <summary>Per-frame work, driven from the plugin's Update: state, then the strip (PLAN §2.3).</summary>
    internal static class Runtime
    {
        private const float AimInterval = 0.25f;

        private static float _nextAim;
        private static FishSighting _aim;

        public static void Tick()
        {
            try
            {
                FishCatalog.EnsureBuilt();
                Compat.EnsureEvaluated();
                FishingState.Refresh();
                UpdateStrip(FishingState.Current);
                FloatLabel.Update(FishingState.Current);
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("Runtime.Tick", e);
                Strip.Hide();
                FloatLabel.Hide();
            }
        }

        private static void UpdateStrip(FishingSnapshot s)
        {
            if (!PluginConfig.Enabled.Value || s.Player == null || !s.RodEquipped)
            {
                Strip.Hide();
                return;
            }
            Glyphs g = UiUtil.Glyphs;

            if (s.Catch != null)
            {
                bool struggle = Features.On(Feature.StruggleIndicator);
                Verdict? v = Features.On(Feature.Forecast) ? Forecast(s, s.Catch, true) : (Verdict?)null;
                if (!struggle && !v.HasValue)
                {
                    Strip.Hide();
                    return;
                }
                if (struggle)
                    Strip.Show(Labels.Hooked(s.Escaping, M(s.LineLength), v, g), s.Escaping ? Palette.Struggle : Palette.Calm);
                else
                    Strip.Show(Labels.Distance(M(s.LineLength)) + "  " + Labels.Verdict(v.Value, g, false), Palette.Idle);
                return;
            }

            if (s.Float != null)
            {
                if (!Features.On(Feature.Forecast) && !Features.On(Feature.FloatLabel))
                {
                    Strip.Hide();
                    return;
                }
                if (s.Subject == null)
                {
                    Strip.Show(Labels.Distance(M(s.LineLength)), Palette.Idle);
                    return;
                }
                FishInfo info = FishCatalog.For(s.Subject);
                Verdict? v = Features.On(Feature.Forecast) ? Forecast(s, s.Subject, false) : (Verdict?)null;
                Strip.Show(Labels.Waiting(info.Name, FishCatalog.Quality(s.Subject), M(s.LineLength), v, g), Palette.Idle);
                return;
            }

            // Rod out, nothing cast: the fish smart bait would bait for.
            if (!Features.On(Feature.SmartBait))
            {
                Strip.Hide();
                return;
            }
            if (Time.time >= _nextAim)
            {
                _nextAim = Time.time + AimInterval;
                _aim = Targeting.Aim(s.Player);
            }
            FishInfo target;
            if (_aim == null || !FishCatalog.All.TryGetValue(_aim.Species, out target))
            {
                Strip.Hide();
                return;
            }
            BaitAdvice a = Tackle.Advise(s.Player, target);
            Strip.Show(Labels.Target(target.Name, _aim.Quality, a, g, PluginConfig.ShowOdds.Value),
                a != null && !a.Carried ? Palette.Missing : Palette.Idle);
        }

        private static Verdict Forecast(FishingSnapshot s, Fish f, bool hooked)
        {
            FishInfo info = FishCatalog.For(f);
            return CatchForecast.Estimate(FishCatalog.Rod, info.Params, FishCatalog.Quality(f), s.LineLength,
                s.Player.GetSkillFactor(Skills.SkillType.Fishing), s.Player.GetStamina(), hooked,
                hooked ? s.RemainingEscape : 0f).Verdict;
        }

        private static int M(float metres)
        {
            return Mathf.Max(0, Mathf.RoundToInt(metres));
        }
    }
}
