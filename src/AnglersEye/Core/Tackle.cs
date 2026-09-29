using System.Collections.Generic;
using AnglersEye.Core.Model;

namespace AnglersEye.Core
{
    internal static class Tackle
    {
        /// <summary>A fishing rod is a weapon whose projectile is a FishingFloat. No names hardcoded.</summary>
        public static bool IsRod(ItemDrop.ItemData w)
        {
            if (w == null || string.IsNullOrEmpty(w.m_shared.m_ammoType))
                return false;
            Attack a = w.m_shared.m_attack;
            return a != null && a.m_attackProjectile != null && a.m_attackProjectile.GetComponent<FishingFloat>() != null;
        }

        /// <summary>Carried stack totals by prefab name (the key fish bait tables use).</summary>
        public static Dictionary<string, int> Carried(Player p)
        {
            var d = new Dictionary<string, int>();
            foreach (ItemDrop.ItemData item in p.GetInventory().GetAllItems())
            {
                if (item.m_dropPrefab == null)
                    continue;
                int n;
                d.TryGetValue(item.m_dropPrefab.name, out n);
                d[item.m_dropPrefab.name] = n + item.m_stack;
            }
            return d;
        }

        public static BaitAdvice Advise(Player p, FishInfo info)
        {
            return info == null ? null : BaitAdvisor.Advise(info.Baits, Carried(p));
        }
    }
}
