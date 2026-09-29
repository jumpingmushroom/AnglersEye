using AnglersEye.Core.Model;

namespace AnglersEye.Core
{
    internal static class Features
    {
        /// <summary>Master switch, the feature's own toggle, and not stood down for another mod.</summary>
        public static bool On(Feature f)
        {
            return PluginConfig.Enabled.Value && PluginConfig.Toggle(f) && !Compat.IsOff(f);
        }
    }
}
