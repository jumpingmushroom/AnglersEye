using System;
using System.Text;

namespace AnglersEye.Core.Model
{
    /// <summary>Strings Angler's Eye shows outside the fishing panel (PanelView). Names passed in are already localised.</summary>
    public static class Labels
    {
        public static string Stars(int quality, Glyphs g)
        {
            if (quality <= 1)
                return "";
            var sb = new StringBuilder();
            for (int i = 1; i < quality; i++)
                sb.Append(g.Star);
            return sb.ToString();
        }

        /// <summary>Text level for when the game's star sprite can't be borrowed: "Lv 3"; empty for quality 1.</summary>
        public static string Level(int quality)
        {
            return quality > 1 ? "Lv " + quality : "";
        }

        public static string Odds(float chance)
        {
            return (int)Math.Round(chance * 100f) + "%";
        }

        /// <summary>"Cold bait", or "Cold bait 60%" with showOdds.</summary>
        public static string BaitName(BaitOption b, bool showOdds)
        {
            return b.BaitName + (showOdds ? " " + Odds(b.Chance) : "");
        }

        /// <summary>"Cold bait + (x12)", "Cold bait 60% + (x12)" or "Cold bait ✖" (the HUD font lacks ✔, so it falls back to +).</summary>
        public static string Bait(BaitAdvice a, Glyphs g, bool showOdds)
        {
            string s = BaitName(a.Best, showOdds);
            return a.Carried ? s + " " + g.Yes + " (x" + a.CarriedCount + ")" : s + " " + g.No;
        }

        /// <summary>Appended to the vanilla hover name: stars, then a bait line.</summary>
        public static string HoverSuffix(int quality, BaitAdvice a, Glyphs g, bool showOdds)
        {
            string s = quality > 1 ? " " + Stars(quality, g) : "";
            if (a != null)
                s += "\nBait: " + Bait(a, g, showOdds);
            return s;
        }

        /// <summary>Float label, after the fish's name and level: whether the bait on the float works on it.</summary>
        public static string OnFloat(bool baitWorks, string neededBait, Glyphs g)
        {
            return " " + g.Sep + (baitWorks ? " bait ok" : " needs " + neededBait);
        }

        public static string NeedsBait(string fish, string bait)
        {
            return "Angler's Eye: " + fish + " needs " + bait;
        }
    }
}
