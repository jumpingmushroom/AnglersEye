using System;

namespace AnglersEye.Core.Model
{
    /// <summary>The two assists' rules (PLAN §2.2 items 3 and 4).</summary>
    public static class ReelPolicy
    {
        /// <summary>FishingFloat.TryToHook's hard-coded window.</summary>
        public const float VanillaHookWindow = 0.5f;
        public const float MaxHookWindow = 1.5f;

        /// <summary>Smart reel: don't reel while a hooked fish struggles.</summary>
        public static bool SuppressReel(bool smartReel, bool hooked, bool escaping)
        {
            return smartReel && hooked && escaping;
        }

        public static float HookWindow(bool extended, float seconds)
        {
            if (!extended)
                return VanillaHookWindow;
            return Math.Min(MaxHookWindow, Math.Max(VanillaHookWindow, seconds));
        }

        public static bool InHookWindow(float now, float nibbleTime, float window)
        {
            return now - nibbleTime < window;
        }
    }
}
