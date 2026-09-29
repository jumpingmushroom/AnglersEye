using System;
using System.Collections.Generic;

namespace AnglersEye.Core.Model
{
    [Flags]
    public enum Feature
    {
        None = 0,
        HoverInfo = 1,
        FloatLabel = 2,
        SmartBait = 4,
        BiteCue = 8,
        StruggleIndicator = 16,
        Forecast = 32,
        SmartReel = 64,
        HookWindow = 128
    }

    public sealed class CompatVerdict
    {
        public Feature Disabled;
        public readonly List<string> Reasons = new List<string>();

        public bool IsOff(Feature f)
        {
            return (Disabled & f) != 0;
        }
    }

    /// <summary>Which features step aside for other fishing mods (PLAN §2.6).</summary>
    public static class CompatRules
    {
        public const string Hooked = "Azumatt.Hooked";
        public const string TrollingFishing = "sighsorry.TrollingFishing";

        public const string FixedUpdate = "FishingFloat.FixedUpdate";
        public const string TryToHook = "FishingFloat.TryToHook";
        public const string GetStaminaUse = "Fish.GetStaminaUse";

        private static readonly KeyValuePair<Feature, string>[] Names =
        {
            new KeyValuePair<Feature, string>(Feature.HoverInfo, "hover info"),
            new KeyValuePair<Feature, string>(Feature.FloatLabel, "float label"),
            new KeyValuePair<Feature, string>(Feature.SmartBait, "smart bait"),
            new KeyValuePair<Feature, string>(Feature.BiteCue, "bite cue"),
            new KeyValuePair<Feature, string>(Feature.StruggleIndicator, "struggle indicator"),
            new KeyValuePair<Feature, string>(Feature.Forecast, "forecast"),
            new KeyValuePair<Feature, string>(Feature.SmartReel, "smart reel"),
            new KeyValuePair<Feature, string>(Feature.HookWindow, "extended hook window")
        };

        public static CompatVerdict Evaluate(ICollection<string> pluginGuids, IDictionary<string, IList<string>> foreignPatchOwners)
        {
            var v = new CompatVerdict();
            if (pluginGuids.Contains(Hooked))
                Off(v, Feature.SmartBait | Feature.SmartReel | Feature.HookWindow | Feature.Forecast |
                       Feature.BiteCue | Feature.StruggleIndicator, "Hooked (" + Hooked + ")");
            if (pluginGuids.Contains(TrollingFishing))
                Off(v, Feature.SmartBait | Feature.SmartReel | Feature.HookWindow | Feature.Forecast,
                    "Trolling Fishing (" + TrollingFishing + ")");

            Patched(v, foreignPatchOwners, FixedUpdate, Feature.SmartReel);
            Patched(v, foreignPatchOwners, TryToHook, Feature.HookWindow);
            Patched(v, foreignPatchOwners, GetStaminaUse, Feature.Forecast);
            return v;
        }

        public static string Describe(Feature f)
        {
            var parts = new List<string>();
            foreach (KeyValuePair<Feature, string> kv in Names)
                if ((f & kv.Key) != 0)
                    parts.Add(kv.Value);
            return string.Join(", ", parts);
        }

        private static void Patched(CompatVerdict v, IDictionary<string, IList<string>> owners, string method, Feature f)
        {
            IList<string> o;
            if (owners != null && owners.TryGetValue(method, out o) && o != null && o.Count > 0)
                Off(v, f, string.Join(", ", o) + " patches " + method);
        }

        private static void Off(CompatVerdict v, Feature f, string who)
        {
            v.Disabled |= f;
            v.Reasons.Add(who + ": " + Describe(f) + " off");
        }
    }
}
