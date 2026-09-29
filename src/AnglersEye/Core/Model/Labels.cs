using System;
using System.Text;

namespace AnglersEye.Core.Model
{
    /// <summary>Every string Angler's Eye shows. Names passed in are already localised.</summary>
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

        public static string WithStars(string name, int quality, Glyphs g)
        {
            string s = Stars(quality, g);
            return s.Length == 0 ? name : name + " " + s;
        }

        public static string Odds(float chance)
        {
            return (int)Math.Round(chance * 100f) + "%";
        }

        /// <summary>"Cold bait ✔ (x12)", "Cold bait 60% ✔ (x12)" or "Cold bait ✖".</summary>
        public static string Bait(BaitAdvice a, Glyphs g, bool showOdds)
        {
            string s = a.Best.BaitName + (showOdds ? " " + Odds(a.Best.Chance) : "");
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

        /// <summary>Strip line before a cast: the fish smart bait would target.</summary>
        public static string Target(string name, int quality, BaitAdvice a, Glyphs g, bool showOdds)
        {
            string head = WithStars(name, quality, g);
            if (a == null)
                return head;
            return head + " " + g.Sep + " " + (a.Carried ? "" : "needs ") + Bait(a, g, showOdds);
        }

        /// <summary>Float label: whether the bait on the float works on this fish.</summary>
        public static string OnFloat(string name, int quality, bool baitWorks, string neededBait, Glyphs g)
        {
            string head = WithStars(name, quality, g) + " " + g.Sep + " ";
            return baitWorks ? head + g.Yes : head + "needs " + neededBait + " " + g.No;
        }

        public static string Verdict(Verdict v, Glyphs g, bool shortForm)
        {
            switch (v)
            {
                case Model.Verdict.Likely:
                    return shortForm ? g.Land : g.Land + " can land";
                case Model.Verdict.Tight:
                    return shortForm ? "~" : "~ tight";
                default:
                    return shortForm ? g.No : g.No + " unlikely";
            }
        }

        /// <summary>Strip line while the float is out: the fish near it, line length, forecast.</summary>
        public static string Waiting(string name, int quality, int metres, Verdict? v, Glyphs g)
        {
            string s = WithStars(name, quality, g) + " " + g.Sep + " " + Distance(metres);
            return v.HasValue ? s + " " + g.Sep + " " + Verdict(v.Value, g, true) : s;
        }

        public static string Distance(int metres)
        {
            return metres + "m";
        }

        /// <summary>Strip line while hooked.</summary>
        public static string Hooked(bool escaping, int metres, Verdict? v, Glyphs g)
        {
            string s = (escaping ? g.Struggle + " WAIT" : g.Calm + " REEL") + "  " + Distance(metres);
            return v.HasValue ? s + "  " + Verdict(v.Value, g, false) : s;
        }

        public static string Bite(Glyphs g)
        {
            return g.BiteLeft + " BITE! " + g.BiteRight;
        }

        public static string NeedsBait(string fish, string bait)
        {
            return "Angler's Eye: " + fish + " needs " + bait;
        }
    }
}
