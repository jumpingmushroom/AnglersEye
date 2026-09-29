using System.Collections.Generic;

namespace AnglersEye.Core.Model
{
    /// <summary>One entry of a fish's bait table: the bait prefab and its chance per nibble.</summary>
    public sealed class BaitOption
    {
        public readonly string BaitId;
        public readonly string BaitName;
        public readonly float Chance;

        public BaitOption(string baitId, string baitName, float chance)
        {
            BaitId = baitId;
            BaitName = baitName;
            Chance = chance;
        }
    }

    public sealed class BaitAdvice
    {
        /// <summary>The carried bait to use, or the best bait overall when none is carried.</summary>
        public BaitOption Best;
        public bool Carried;
        public int CarriedCount;
    }

    public static class BaitAdvisor
    {
        public static BaitAdvice Advise(IReadOnlyList<BaitOption> table, IReadOnlyDictionary<string, int> carried)
        {
            List<BaitOption> merged = Merge(table);
            if (merged.Count == 0)
                return null;

            // Highest chance first; the id keeps the order stable.
            merged.Sort((a, b) => a.Chance != b.Chance ? b.Chance.CompareTo(a.Chance) : string.CompareOrdinal(a.BaitId, b.BaitId));

            BaitOption best = null;
            int bestCount = 0;
            foreach (BaitOption o in merged)
            {
                int n;
                if (carried == null || !carried.TryGetValue(o.BaitId, out n) || n <= 0)
                    continue;
                if (best == null || o.Chance > best.Chance || (o.Chance == best.Chance && n > bestCount))
                {
                    best = o;
                    bestCount = n;
                }
            }

            if (best != null)
                return new BaitAdvice { Best = best, Carried = true, CarriedCount = bestCount };
            return new BaitAdvice { Best = merged[0], Carried = false, CarriedCount = 0 };
        }

        /// <summary>
        /// The game's bait test passes if any matching entry's roll passes, so a bait listed twice
        /// works with chance 1 - (1-a)(1-b).
        /// </summary>
        private static List<BaitOption> Merge(IReadOnlyList<BaitOption> table)
        {
            var merged = new List<BaitOption>();
            if (table == null)
                return merged;
            var index = new Dictionary<string, int>();
            foreach (BaitOption o in table)
            {
                if (o == null || string.IsNullOrEmpty(o.BaitId))
                    continue;
                int i;
                if (index.TryGetValue(o.BaitId, out i))
                {
                    BaitOption m = merged[i];
                    merged[i] = new BaitOption(m.BaitId, m.BaitName, 1f - (1f - m.Chance) * (1f - o.Chance));
                }
                else
                {
                    index[o.BaitId] = merged.Count;
                    merged.Add(o);
                }
            }
            return merged;
        }
    }
}
