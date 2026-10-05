using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AnglersEye
{
    /// <summary>
    /// A fishing assistant. Reads fish, bait and float data already present on the client and
    /// never changes vanilla stamina costs, spawns, bait rules or drops. Purely client-side.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim.x86_64")]
    public sealed class AnglersEyePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jumpingmushroom.anglerseye";
        public const string PluginName = "Angler's Eye";
        public const string PluginVersion = "0.1.1";

        internal static ManualLogSource Log;
        internal static AnglersEyePlugin Instance;

        private static readonly HashSet<string> Warned = new HashSet<string>();
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;
            PluginConfig.Bind(base.Config);
            Core.ConsoleCommands.Register();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(AnglersEyePlugin).Assembly);

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            Core.Runtime.Tick();
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }

        /// <summary>Log an exception once per key, so a broken patch can't flood the log every frame.</summary>
        internal static void WarnOnce(string key, Exception e)
        {
            if (Warned.Add(key))
                Log.LogWarning(key + ": " + e);
        }
    }
}
