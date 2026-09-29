using System;
using System.Collections.Generic;
using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.Core
{
    internal sealed class FishInfo
    {
        public string Prefab;
        public string NameToken;
        public List<BaitOption> Baits;
        public FishParams Params;

        public string Name => Localization.instance != null ? Localization.instance.Localize(NameToken) : NameToken;
    }

    /// <summary>
    /// Every fish prefab's bait table and fight numbers, plus the float's reeling numbers, read
    /// from ZNetScene at runtime (PLAN §1.1, §1.5) so modded fish work. Rebuilt per world.
    /// </summary>
    internal static class FishCatalog
    {
        private static readonly Dictionary<string, FishInfo> ByPrefab = new Dictionary<string, FishInfo>();
        private static readonly HashSet<string> BaitAmmoTypes = new HashSet<string>();
        private static ZNetScene _builtFor;

        public static RodParams Rod = new RodParams();
        public static float FloatRange = 10f;
        public static float MaxDistance = 30f;

        public static IReadOnlyDictionary<string, FishInfo> All
        {
            get
            {
                EnsureBuilt();
                return ByPrefab;
            }
        }

        public static void EnsureBuilt()
        {
            ZNetScene zs = ZNetScene.instance;
            if (zs == null || zs == _builtFor)
                return;
            ByPrefab.Clear();
            BaitAmmoTypes.Clear();
            foreach (GameObject go in zs.m_prefabs)
            {
                if (go == null)
                    continue;
                Fish f = go.GetComponent<Fish>();
                if (f != null)
                    ByPrefab[go.name] = Build(go.name, f);
                FishingFloat ff = go.GetComponent<FishingFloat>();
                if (ff != null)
                    ReadFloat(ff);
            }
            _builtFor = zs;
        }

        public static FishInfo For(Fish f)
        {
            EnsureBuilt();
            string key = Utils.GetPrefabName(f.gameObject);
            FishInfo info;
            if (!ByPrefab.TryGetValue(key, out info))
            {
                info = Build(key, f);
                ByPrefab[key] = info;
            }
            return info;
        }

        /// <summary>Whether some fish's bait table lists a bait of this ammo type (PLAN §1.7 / controller Ruling 9).</summary>
        public static bool IsBaitAmmoType(string ammoType)
        {
            EnsureBuilt();
            return !string.IsNullOrEmpty(ammoType) && BaitAmmoTypes.Contains(ammoType);
        }

        /// <summary>The fish's level (1 = no stars), from its ItemDrop (PLAN §1.1).</summary>
        public static int Quality(Fish f)
        {
            ItemDrop d = f.m_itemDrop != null ? f.m_itemDrop : f.GetComponent<ItemDrop>();
            return d != null ? Math.Max(1, d.m_itemData.m_quality) : 1;
        }

        private static FishInfo Build(string prefab, Fish f)
        {
            var baits = new List<BaitOption>();
            foreach (Fish.BaitSetting b in f.m_baits)
            {
                if (b == null || b.m_bait == null)
                    continue;
                string token = b.m_bait.m_itemData.m_shared.m_name;
                string name = Localization.instance != null ? Localization.instance.Localize(token) : token;
                baits.Add(new BaitOption(b.m_bait.name, name, b.m_chance));
                string ammoType = b.m_bait.m_itemData.m_shared.m_ammoType;
                if (!string.IsNullOrEmpty(ammoType))
                    BaitAmmoTypes.Add(ammoType);
            }
            return new FishInfo
            {
                Prefab = prefab,
                NameToken = f.m_name,
                Baits = baits,
                Params = new FishParams
                {
                    StaminaUse = f.m_staminaUse,
                    EscapeStaminaUse = f.m_escapeStaminaUse,
                    EscapeMin = f.m_escapeMin,
                    EscapeMax = f.m_escapeMax,
                    EscapeMaxPerLevel = f.m_escapeMaxPerLevel,
                    EscapeWaitMin = f.m_escapeWaitMin,
                    EscapeWaitMax = f.m_escapeWaitMax
                }
            };
        }

        private static void ReadFloat(FishingFloat ff)
        {
            Rod = new RodParams
            {
                PullStaminaUse = ff.m_pullStaminaUse,
                PullStaminaUseMaxSkillMultiplier = ff.m_pullStaminaUseMaxSkillMultiplier,
                PullLineSpeed = ff.m_pullLineSpeed,
                PullLineSpeedMaxSkill = ff.m_pullLineSpeedMaxSkill,
                HookedStaminaPerSec = ff.m_hookedStaminaPerSec,
                HookedStaminaPerSecMaxSkill = ff.m_hookedStaminaPerSecMaxSkill
            };
            FloatRange = ff.m_range;
            MaxDistance = ff.m_maxDistance;
        }
    }
}
