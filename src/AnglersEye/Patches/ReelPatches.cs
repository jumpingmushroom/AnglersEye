using System;
using AnglersEye.Core;
using AnglersEye.Core.Model;
using HarmonyLib;
using UnityEngine;

namespace AnglersEye.Patches
{
    [HarmonyPatch]
    internal static class ReelPatches
    {
        /// <summary>True only inside the local float's FixedUpdate while its fish struggles.</summary>
        [ThreadStatic]
        private static bool _suppressBlock;

        [HarmonyPrefix, HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.FixedUpdate))]
        private static void FloatUpdatePrefix(FishingFloat __instance)
        {
            _suppressBlock = false;
            try
            {
                if (!Features.On(Feature.SmartReel) || !FishingState.IsLocal(__instance))
                    return;
                Fish f = __instance.GetCatch();
                _suppressBlock = ReelPolicy.SuppressReel(true, f != null, f != null && f.IsEscaping());
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("SmartReel", e);
            }
        }

        [HarmonyFinalizer, HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.FixedUpdate))]
        private static void FloatUpdateFinalizer()
        {
            _suppressBlock = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsBlocking))]
        private static bool IsBlockingPrefix(ref bool __result)
        {
            if (!_suppressBlock)
                return true;
            __result = false;
            return false;
        }

        /// <summary>FishingFloat.TryToHook with the configured window instead of 0.5 s.</summary>
        [HarmonyPrefix, HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.TryToHook))]
        private static bool TryToHookPrefix(FishingFloat __instance)
        {
            try
            {
                if (!Features.On(Feature.HookWindow) || !FishingState.IsLocal(__instance))
                    return true;
                float window = ReelPolicy.HookWindow(true, PluginConfig.HookWindowSeconds.Value);
                if (__instance.m_nibbler != null && ReelPolicy.InHookWindow(Time.time, __instance.m_nibbleTime, window) &&
                    __instance.GetCatch() == null)
                {
                    __instance.Message("$msg_fishing_hooked", prioritized: true);
                    __instance.SetCatch(__instance.m_nibbler);
                    __instance.m_nibbler = null;
                    Game.instance.IncrementPlayerStat(PlayerStatType.FishHooked);
                }
                return false;
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("HookWindow", e);
                return true;
            }
        }
    }
}
