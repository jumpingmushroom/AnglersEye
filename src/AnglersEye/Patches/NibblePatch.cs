using System;
using AnglersEye.Core;
using AnglersEye.UI;
using HarmonyLib;

namespace AnglersEye.Patches
{
    /// <summary>
    /// RPC_Nibble sets m_nibbleTime only for a correct-bait nibble it accepts (PLAN §1.3), so a
    /// changed m_nibbleTime means a hookable bite on our float.
    /// </summary>
    [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.RPC_Nibble))]
    internal static class NibblePatch
    {
        private static void Prefix(FishingFloat __instance, out float __state)
        {
            __state = __instance.m_nibbleTime;
        }

        private static void Postfix(FishingFloat __instance, bool correctBait, float __state)
        {
            try
            {
                if (correctBait && __instance.m_nibbleTime != __state && FishingState.IsLocal(__instance))
                    BiteCue.Fire();
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("NibblePatch", e);
            }
        }
    }
}
