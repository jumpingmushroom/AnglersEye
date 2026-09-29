using System;
using System.Collections.Generic;
using System.Linq;
using AnglersEye.Core.Model;

namespace AnglersEye.Core
{
    /// <summary>
    /// "anglerseye" prints status; "anglerseye fish" every fish's bait table and fight numbers.
    /// Output is mirrored to the BepInEx log for build/logs.sh.
    /// </summary>
    internal static class ConsoleCommands
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("anglerseye", "Angler's Eye: feature and compat status (fish = bait table for every fish)",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
                    if (sub == "fish")
                        Fish(args.Context);
                    else
                        Status(args.Context);
                });
        }

        internal static void Say(Terminal ctx, string line)
        {
            if (ctx != null)
                ctx.AddString(line);
            AnglersEyePlugin.Log.LogInfo(line);
        }

        private static void Status(Terminal ctx)
        {
            Say(ctx, "Angler's Eye " + AnglersEyePlugin.PluginVersion + (PluginConfig.Enabled.Value ? "" : " (disabled)") +
                     ": " + FishCatalog.All.Count + " fish known.");
            var on = new List<string>();
            var off = new List<string>();
            foreach (Feature f in Enum.GetValues(typeof(Feature)))
            {
                if (f == Feature.None)
                    continue;
                (Features.On(f) ? on : off).Add(CompatRules.Describe(f));
            }
            Say(ctx, "Angler's Eye:   on: " + (on.Count > 0 ? string.Join(", ", on) : "nothing"));
            Say(ctx, "Angler's Eye:   off: " + (off.Count > 0 ? string.Join(", ", off) : "nothing"));
            if (Compat.Verdict == null)
                Say(ctx, "Angler's Eye:   compat: not checked yet (load a world)");
            else if (Compat.Verdict.Reasons.Count == 0)
                Say(ctx, "Angler's Eye:   compat: no other fishing mods found");
            else
                foreach (string r in Compat.Verdict.Reasons)
                    Say(ctx, "Angler's Eye:   compat: " + r);
        }

        private static void Fish(Terminal ctx)
        {
            try
            {
                RodParams r = FishCatalog.Rod;
                Say(ctx, "Angler's Eye:   float: reel " + r.PullStaminaUse + "/s x" + r.PullStaminaUseMaxSkillMultiplier +
                         " at max skill, speed " + r.PullLineSpeed + "-" + r.PullLineSpeedMaxSkill + " m/s, hooked drain " +
                         r.HookedStaminaPerSec + "-" + r.HookedStaminaPerSecMaxSkill + "/s, range " + FishCatalog.FloatRange + "m");
                foreach (FishInfo f in FishCatalog.All.Values.OrderBy(f => f.Prefab, StringComparer.Ordinal))
                {
                    FishParams p = f.Params;
                    string baits = f.Baits.Count == 0 ? "no bait" :
                        string.Join(", ", f.Baits.Select(b => b.BaitName + " " + Labels.Odds(b.Chance)));
                    Say(ctx, "Angler's Eye:   " + f.Prefab + " " + f.Name + ": " + baits + " | stamina " + p.StaminaUse + "/" +
                             p.EscapeStaminaUse + ", struggle " + p.EscapeMin + "-" + p.EscapeMax + " +" + p.EscapeMaxPerLevel +
                             "/level, pause " + p.EscapeWaitMin + "-" + p.EscapeWaitMax + "s");
                }
            }
            catch (Exception e)
            {
                Say(ctx, "Angler's Eye: couldn't list fish: " + e.Message);
                AnglersEyePlugin.WarnOnce("anglerseye fish", e);
            }
        }
    }
}
