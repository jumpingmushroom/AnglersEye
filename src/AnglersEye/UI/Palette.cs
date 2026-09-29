using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.UI
{
    internal static class Palette
    {
        /// <summary>Warm off-white, like the HUD's own text.</summary>
        public static readonly Color Normal = new Color(0.93f, 0.91f, 0.86f, 1f);
        public static readonly Color Good = new Color(0.56f, 0.87f, 0.42f, 1f);
        public static readonly Color Warn = new Color(1f, 0.71f, 0.24f, 1f);
        public static readonly Color Bad = new Color(0.97f, 0.43f, 0.36f, 1f);
        public static readonly Color Bite = new Color(1f, 0.9f, 0.3f, 1f);
        /// <summary>Panel fill and its thin warm-gold edge, after Valheim's own panels.</summary>
        public static readonly Color Frame = new Color(0.08f, 0.07f, 0.06f, 0.78f);
        public static readonly Color FrameLine = new Color(0.78f, 0.63f, 0.31f, 0.9f);
        /// <summary>The empty part of the struggle bar.</summary>
        public static readonly Color Track = new Color(0f, 0f, 0f, 0.55f);

        public static Color For(Tone t)
        {
            switch (t)
            {
                case Tone.Good: return Good;
                case Tone.Warn: return Warn;
                case Tone.Bad: return Bad;
                case Tone.Bite: return Bite;
                default: return Normal;
            }
        }
    }
}
