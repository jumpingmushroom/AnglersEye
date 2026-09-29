using BepInEx.Configuration;

namespace AnglersEye
{
    public static class PluginConfig
    {
        public static ConfigEntry<bool> Enabled;

        public static ConfigEntry<bool> HoverInfo;
        public static ConfigEntry<bool> FloatLabel;
        public static ConfigEntry<bool> ShowOdds;

        public static ConfigEntry<bool> SmartBait;
        public static ConfigEntry<float> AimConeDegrees;

        public static ConfigEntry<bool> BiteCue;
        public static ConfigEntry<bool> BiteSound;
        public static ConfigEntry<float> BiteVolume;
        public static ConfigEntry<bool> StruggleIndicator;

        public static ConfigEntry<bool> Forecast;

        public static ConfigEntry<bool> SmartReel;
        public static ConfigEntry<bool> ExtendedHookWindow;
        public static ConfigEntry<float> HookWindowSeconds;

        public static ConfigEntry<float> Scale;
        public static ConfigEntry<float> OffsetX;
        public static ConfigEntry<float> OffsetY;

        public static ConfigEntry<bool> Verbose;

        private static ConfigurationManagerAttributes Attr(int order, bool advanced = false)
        {
            return new ConfigurationManagerAttributes { Order = order, IsAdvanced = advanced };
        }

        public static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("1 - General", "Enabled", true,
                new ConfigDescription("Master switch for everything Angler's Eye shows or does.", null, Attr(100)));

            HoverInfo = cfg.Bind("2 - Identify", "HoverInfo", true,
                new ConfigDescription("When you look at a fish, add its stars and the bait it takes (✔ carried, ✖ not) to the hover text.", null, Attr(90)));
            FloatLabel = cfg.Bind("2 - Identify", "FloatLabel", true,
                new ConfigDescription("Show a small label above your float naming the fish heading for it, and whether your bait works on it.", null, Attr(89)));
            ShowOdds = cfg.Bind("2 - Identify", "ShowOdds", false,
                new ConfigDescription("Also show the bait's chance per nibble, e.g. 'Cold bait 60%'.", null, Attr(88)));

            SmartBait = cfg.Bind("3 - Bait", "SmartBait", true,
                new ConfigDescription("On cast, equip the carried bait that works best on the fish you're aiming at. Tells you if you carry none.", null, Attr(80)));
            AimConeDegrees = cfg.Bind("3 - Bait", "AimConeDegrees", 10f,
                new ConfigDescription("How far off your aim (in degrees) a fish can be and still count as the one you're aiming at.",
                    new AcceptableValueRange<float>(3f, 30f), Attr(79)));

            BiteCue = cfg.Bind("4 - Cues", "BiteCue", true,
                new ConfigDescription("Flash 'BITE!' under the crosshair while a nibble can be hooked.", null, Attr(70)));
            BiteSound = cfg.Bind("4 - Cues", "BiteSound", true,
                new ConfigDescription("Play a short sound on a nibble you can hook.", null, Attr(69)));
            BiteVolume = cfg.Bind("4 - Cues", "BiteVolume", 0.7f,
                new ConfigDescription("Volume of the bite sound.", new AcceptableValueRange<float>(0f, 1f), Attr(68)));
            StruggleIndicator = cfg.Bind("4 - Cues", "StruggleIndicator", true,
                new ConfigDescription("While a fish is hooked, show REEL when it's calm and WAIT when it's struggling.", null, Attr(67)));

            Forecast = cfg.Bind("5 - Forecast", "Forecast", true,
                new ConfigDescription("Estimate whether you have the stamina to land the fish: ✓ can land, ~ tight, ✖ unlikely.", null, Attr(60)));

            SmartReel = cfg.Bind("6 - Assists", "SmartReel", false,
                new ConfigDescription("While you hold Block, only reel while the fish is calm. Stamina costs stay vanilla.", null, Attr(50)));
            ExtendedHookWindow = cfg.Bind("6 - Assists", "ExtendedHookWindow", false,
                new ConfigDescription("Give yourself longer than vanilla's 0.5 s to hook a nibble.", null, Attr(49)));
            HookWindowSeconds = cfg.Bind("6 - Assists", "HookWindowSeconds", 1.0f,
                new ConfigDescription("Hook window when ExtendedHookWindow is on.", new AcceptableValueRange<float>(0.5f, 1.5f), Attr(48)));

            Scale = cfg.Bind("7 - UI", "Scale", 1f,
                new ConfigDescription("Size of the text under the crosshair and the float label.", new AcceptableValueRange<float>(0.5f, 2f), Attr(40)));
            OffsetX = cfg.Bind("7 - UI", "OffsetX", 0f,
                new ConfigDescription("Horizontal nudge of the text under the crosshair, in pixels (positive is right).", new AcceptableValueRange<float>(-1500f, 1500f), Attr(39)));
            OffsetY = cfg.Bind("7 - UI", "OffsetY", 0f,
                new ConfigDescription("Vertical nudge of the text under the crosshair, in pixels (positive is up).", new AcceptableValueRange<float>(-1000f, 1000f), Attr(38)));

            Verbose = cfg.Bind("8 - Logging", "Verbose", false,
                new ConfigDescription("Log smart bait and compat decisions to the BepInEx log.", null, Attr(5, advanced: true)));
        }
    }
}
