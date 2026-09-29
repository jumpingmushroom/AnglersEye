using System;

namespace AnglersEye.Core.Model
{
    /// <summary>
    /// Symbols used in labels. The game's HUD font may lack some of them, so each one falls back
    /// to ASCII individually (Resolve), decided at runtime from the font itself.
    /// </summary>
    public sealed class Glyphs
    {
        public string Star, Yes, No, Sep;

        public static Glyphs Unicode()
        {
            return new Glyphs { Star = "★", Yes = "✔", No = "✖", Sep = "·" };
        }

        public static Glyphs Ascii()
        {
            return new Glyphs { Star = "*", Yes = "+", No = "x", Sep = "-" };
        }

        public static Glyphs Resolve(Func<char, bool> hasChar)
        {
            Glyphs u = Unicode(), a = Ascii();
            return new Glyphs
            {
                Star = Pick(u.Star, a.Star, hasChar),
                Yes = Pick(u.Yes, a.Yes, hasChar),
                No = Pick(u.No, a.No, hasChar),
                Sep = Pick(u.Sep, a.Sep, hasChar)
            };
        }

        private static string Pick(string unicode, string ascii, Func<char, bool> hasChar)
        {
            foreach (char c in unicode)
                if (!hasChar(c))
                    return ascii;
            return unicode;
        }
    }
}
