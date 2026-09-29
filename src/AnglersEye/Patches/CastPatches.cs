using System;
using AnglersEye.Core;
using HarmonyLib;

namespace AnglersEye.Patches
{
    [HarmonyPatch]
    internal static class CastPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Attack), nameof(Attack.StartDraw))]
        private static void StartDraw(Humanoid character, ItemDrop.ItemData weapon)
        {
            Run(character, weapon);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        private static void Start(Humanoid character, ItemDrop.ItemData weapon)
        {
            Run(character, weapon);
        }

        private static void Run(Humanoid character, ItemDrop.ItemData weapon)
        {
            try
            {
                SmartBait.BeforeCast(character, weapon);
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("SmartBait", e);
            }
        }
    }
}
