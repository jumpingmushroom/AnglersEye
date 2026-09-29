using System.Collections.Generic;
using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.Core
{
    /// <summary>The fish the player is aiming at, for smart bait and the pre-cast strip line.</summary>
    internal static class Targeting
    {
        private static readonly List<FishSighting> Buffer = new List<FishSighting>();

        public static FishSighting Aim(Player p)
        {
            GameCamera cam = GameCamera.instance;
            if (p == null || cam == null)
                return null;

            FishSighting cross = null;
            GameObject hover = p.GetHoverObject();
            Fish hovered = hover != null ? hover.GetComponentInParent<Fish>() : null;
            if (hovered != null && !hovered.IsOutOfWater())
                cross = Sight(hovered);

            Buffer.Clear();
            foreach (IMonoUpdater u in Fish.Instances)
            {
                Fish f = u as Fish;
                if (f == null || f.IsOutOfWater() || f.IsHooked())
                    continue;
                Buffer.Add(Sight(f));
            }
            Transform t = cam.transform;
            return TargetPicker.Pick(cross, V(t.position), V(t.forward), PluginConfig.AimConeDegrees.Value,
                FishCatalog.MaxDistance, Buffer);
        }

        public static FishSighting Sight(Fish f)
        {
            return new FishSighting(Utils.GetPrefabName(f.gameObject), FishCatalog.Quality(f), V(f.transform.position));
        }

        private static Vec3 V(Vector3 v)
        {
            return new Vec3(v.x, v.y, v.z);
        }
    }
}
