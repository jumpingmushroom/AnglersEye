using System;

namespace AnglersEye.Core.Model
{
    /// <summary>The float's reeling numbers (FishingFloat fields, read from its prefab).</summary>
    public sealed class RodParams
    {
        public float PullStaminaUse = 10f;
        public float PullStaminaUseMaxSkillMultiplier = 0.2f;
        public float PullLineSpeed = 1f;
        public float PullLineSpeedMaxSkill = 2f;
        public float HookedStaminaPerSec = 1f;
        public float HookedStaminaPerSecMaxSkill = 0.2f;
    }

    /// <summary>A fish's fight numbers (Fish fields, read from its prefab).</summary>
    public sealed class FishParams
    {
        public float StaminaUse = 1f;
        public float EscapeStaminaUse = 2f;
        public float EscapeMin = 0.5f;
        public float EscapeMax = 3f;
        public float EscapeMaxPerLevel = 1.5f;
        public float EscapeWaitMin = 0.75f;
        public float EscapeWaitMax = 4f;
    }

    public enum Verdict
    {
        Likely,
        Tight,
        Unlikely
    }

    public struct Forecast
    {
        public readonly float Cost;
        public readonly Verdict Verdict;

        public Forecast(float cost, Verdict verdict)
        {
            Cost = cost;
            Verdict = verdict;
        }
    }

    /// <summary>
    /// Expected stamina to land a fish if you reel only while it's calm (PLAN §2.4). Struggles are
    /// random, so this uses average struggle and pause lengths; stamina regen is ignored.
    /// </summary>
    public static class CatchForecast
    {
        /// <summary>The game lands the fish once the line is this short.</summary>
        public const float LandedAt = 0.5f;

        /// <summary>Stamina must exceed the cost by this factor for "can land".</summary>
        public const float Margin = 1.25f;

        public static Forecast Estimate(RodParams rod, FishParams fish, int quality, float lineLength,
            float skillFactor, float stamina, bool hooked, float remainingEscape)
        {
            float s = Math.Min(1f, Math.Max(0f, skillFactor));
            int q = Math.Max(1, quality);

            float speed = Lerp(rod.PullLineSpeed, rod.PullLineSpeedMaxSkill, s);
            float reelTime = Math.Max(0f, lineLength - LandedAt) / Math.Max(0.0001f, speed);

            float expEscape = Math.Max(0f, (fish.EscapeMin + fish.EscapeMax + q * fish.EscapeMaxPerLevel) / 2f);
            float expWait = (fish.EscapeWaitMin + fish.EscapeWaitMax) / 2f;

            // Hooking starts a struggle; once hooked, only what's left of the current one counts.
            float wall = hooked ? Math.Max(0f, remainingEscape) : expEscape;
            if (reelTime > 0f)
            {
                if (expWait <= 0f)
                    return new Forecast(float.PositiveInfinity, Verdict.Unlikely);
                float calmFraction = expWait / (expWait + expEscape);
                wall += reelTime / calmFraction;
            }

            float pull = rod.PullStaminaUse + fish.StaminaUse * q;
            float reelCost = Lerp(pull, pull * rod.PullStaminaUseMaxSkillMultiplier, s) * reelTime;
            float passiveCost = Lerp(rod.HookedStaminaPerSec, rod.HookedStaminaPerSecMaxSkill, s) * wall;
            float cost = reelCost + passiveCost;
            return new Forecast(cost, Judge(cost, stamina));
        }

        public static Verdict Judge(float cost, float stamina)
        {
            if (stamina >= cost * Margin)
                return Verdict.Likely;
            return stamina >= cost ? Verdict.Tight : Verdict.Unlikely;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
