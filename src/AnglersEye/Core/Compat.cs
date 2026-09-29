using System;
using System.Collections.Generic;
using System.Reflection;
using AnglersEye.Core.Model;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace AnglersEye.Core
{
    /// <summary>
    /// Detects other fishing mods once all plugins have loaded (the first frame a world exists)
    /// and applies CompatRules. Each decision is logged once.
    /// </summary>
    internal static class Compat
    {
        public static CompatVerdict Verdict { get; private set; }

        public static bool IsOff(Feature f)
        {
            return Verdict != null && Verdict.IsOff(f);
        }

        public static void EnsureEvaluated()
        {
            if (Verdict != null || ZNetScene.instance == null)
                return;
            var guids = new HashSet<string>(Chainloader.PluginInfos.Keys);
            var owners = new Dictionary<string, IList<string>>
            {
                { CompatRules.FixedUpdate, Owners(typeof(FishingFloat), "FixedUpdate") },
                { CompatRules.TryToHook, Owners(typeof(FishingFloat), "TryToHook") },
                { CompatRules.GetStaminaUse, Owners(typeof(Fish), "GetStaminaUse") }
            };
            Verdict = CompatRules.Evaluate(guids, owners);
            foreach (string r in Verdict.Reasons)
                AnglersEyePlugin.Log.LogInfo("Angler's Eye compat: " + r);
        }

        private static IList<string> Owners(Type type, string method)
        {
            var list = new List<string>();
            MethodBase m = AccessTools.Method(type, method);
            // Fully qualified: our own AnglersEye.Patches namespace would shadow HarmonyLib.Patches.
            HarmonyLib.Patches p = m != null ? Harmony.GetPatchInfo(m) : null;
            if (p == null)
                return list;
            foreach (string o in p.Owners)
                if (o != AnglersEyePlugin.PluginGuid && !list.Contains(o))
                    list.Add(o);
            return list;
        }
    }
}
