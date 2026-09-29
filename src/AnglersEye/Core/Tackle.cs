using System.Collections.Generic;
using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.Core
{
    internal static class Tackle
    {
        private static readonly Dictionary<string, int> _carried = new Dictionary<string, int>();
        private static int _carriedFrame = -1;
        private static Player _carriedFor;

        /// <summary>A fishing rod is a weapon whose projectile is a FishingFloat. No names hardcoded.</summary>
        public static bool IsRod(ItemDrop.ItemData w)
        {
            if (w == null || string.IsNullOrEmpty(w.m_shared.m_ammoType))
                return false;
            Attack a = w.m_shared.m_attack;
            return a != null && a.m_attackProjectile != null && a.m_attackProjectile.GetComponent<FishingFloat>() != null;
        }

        /// <summary>Carried stack totals by prefab name (the key fish bait tables use). Cached per frame per player; read-only.</summary>
        public static IReadOnlyDictionary<string, int> Carried(Player p)
        {
            if (_carriedFrame == Time.frameCount && _carriedFor == p)
                return _carried;
            _carried.Clear();
            foreach (ItemDrop.ItemData item in p.GetInventory().GetAllItems())
            {
                if (item.m_dropPrefab == null)
                    continue;
                int n;
                _carried.TryGetValue(item.m_dropPrefab.name, out n);
                _carried[item.m_dropPrefab.name] = n + item.m_stack;
            }
            _carriedFrame = Time.frameCount;
            _carriedFor = p;
            return _carried;
        }

        public static BaitAdvice Advise(Player p, FishInfo info)
        {
            return info == null ? null : BaitAdvisor.Advise(info.Baits, Carried(p));
        }
    }
}
