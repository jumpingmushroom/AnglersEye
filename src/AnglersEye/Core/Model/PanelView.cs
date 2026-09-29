using System;

namespace AnglersEye.Core.Model
{
    /// <summary>How a piece of panel text is coloured; the UI maps each tone to a palette colour.</summary>
    public enum Tone
    {
        Normal,
        Good,
        Warn,
        Bad,
        Bite
    }

    /// <summary>
    /// What the fishing panel above the stamina bar shows: a title row (fish name + level) and a
    /// body row (bait advice, or a big state word with an optional struggle bar, plus a forecast
    /// word on the right). Null fields are not shown. Names passed in are already localised.
    /// </summary>
    public sealed class PanelView
    {
        public const string WaitingText = "Waiting…";

        /// <summary>Fish name (or "Waiting…"); null for no title row.</summary>
        public readonly string Title;
        /// <summary>The fish's quality; stars/level are drawn for quality &gt; 1.</summary>
        public readonly int Level;
        public readonly string Body;
        public readonly Tone BodyTone;
        /// <summary>The body is a big state word (BITE!, REEL, WAIT) rather than a line of text.</summary>
        public readonly bool BodyIsBig;
        /// <summary>Remaining fraction of the current struggle, 1 → 0; null for no bar.</summary>
        public readonly float? Bar;
        public readonly string Forecast;
        public readonly Tone ForecastTone;

        private PanelView(string title, int level, string body, Tone bodyTone, bool bodyIsBig, float? bar, Verdict? v)
        {
            Title = title;
            Level = level;
            Body = body;
            BodyTone = bodyTone;
            BodyIsBig = bodyIsBig;
            Bar = bar;
            Forecast = v.HasValue ? WordFor(v.Value) : null;
            ForecastTone = v.HasValue ? ToneFor(v.Value) : Tone.Normal;
        }

        /// <summary>Rod out, nothing cast: the fish smart bait would bait for, and that bait.</summary>
        public static PanelView Target(string name, int quality, BaitAdvice a, bool showOdds)
        {
            if (a == null)
                return new PanelView(name, quality, null, Tone.Normal, false, null, null);
            string bait = a.Best.BaitName + (showOdds ? " " + Labels.Odds(a.Best.Chance) : "");
            return a.Carried
                ? new PanelView(name, quality, bait + " (" + a.CarriedCount + ")", Tone.Normal, false, null, null)
                : new PanelView(name, quality, "needs " + bait, Tone.Bad, false, null, null);
        }

        /// <summary>Float out, nothing biting yet. With no fish near the float, just "Waiting…".</summary>
        public static PanelView Waiting(string name, int quality, Verdict? v)
        {
            if (name == null)
                return new PanelView(WaitingText, 0, null, Tone.Normal, false, null, null);
            return new PanelView(name, quality, WaitingText, Tone.Normal, false, null, v);
        }

        /// <summary>A nibble that can be hooked right now.</summary>
        public static PanelView Bite(string name, int quality)
        {
            return new PanelView(name, name == null ? 0 : quality, "BITE!", Tone.Bite, true, null, null);
        }

        /// <summary>
        /// A fish on the line: REEL while calm, WAIT with a draining bar while it struggles (when
        /// showStruggle), plus the forecast. Null when neither is switched on.
        /// </summary>
        public static PanelView Hooked(string name, int quality, bool struggling, bool showStruggle,
            float remainingEscape, float escapeTotal, Verdict? v)
        {
            if (!showStruggle)
                return v.HasValue ? new PanelView(name, quality, null, Tone.Normal, false, null, v) : null;
            if (struggling)
                return new PanelView(name, quality, "WAIT", Tone.Warn, true, StruggleFraction(remainingEscape, escapeTotal), v);
            return new PanelView(name, quality, "REEL", Tone.Good, true, null, v);
        }

        /// <summary>How much of the current struggle is left, 0..1; 0 when its length is unknown.</summary>
        public static float StruggleFraction(float remaining, float total)
        {
            if (total <= 0f)
                return 0f;
            return Math.Min(1f, Math.Max(0f, remaining / total));
        }

        public static string WordFor(Verdict v)
        {
            switch (v)
            {
                case Verdict.Likely: return "can land";
                case Verdict.Tight: return "tight";
                default: return "unlikely";
            }
        }

        public static Tone ToneFor(Verdict v)
        {
            switch (v)
            {
                case Verdict.Likely: return Tone.Good;
                case Verdict.Tight: return Tone.Warn;
                default: return Tone.Bad;
            }
        }
    }
}
