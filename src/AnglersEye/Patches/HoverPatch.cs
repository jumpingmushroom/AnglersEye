using System;
using AnglersEye.Core;
using AnglersEye.Core.Model;
using AnglersEye.UI;
using HarmonyLib;

namespace AnglersEye.Patches
{
    /// <summary>Stars and bait advice under a fish's hover name (PLAN §2.2 item 1).</summary>
    [HarmonyPatch(typeof(Fish), nameof(Fish.GetHoverText))]
    internal static class HoverPatch
    {
        private static void Postfix(Fish __instance, ref string __result)
        {
            try
            {
                // Out of the water the game shows the ItemDrop's own hover text (with stars); leave it.
                if (!Features.On(Feature.HoverInfo) || __instance.IsOutOfWater() || Player.m_localPlayer == null)
                    return;
                FishInfo info = FishCatalog.For(__instance);
                BaitAdvice advice = Tackle.Advise(Player.m_localPlayer, info);
                __result += Labels.HoverSuffix(FishCatalog.Quality(__instance), advice, UiUtil.Glyphs, PluginConfig.ShowOdds.Value);
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("HoverPatch", e);
            }
        }
    }
}
