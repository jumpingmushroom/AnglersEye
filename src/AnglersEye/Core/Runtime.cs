using System;

namespace AnglersEye.Core
{
    /// <summary>Per-frame work, driven from the plugin's Update.</summary>
    internal static class Runtime
    {
        public static void Tick()
        {
            try
            {
                FishCatalog.EnsureBuilt();
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("Runtime.Tick", e);
            }
        }
    }
}
