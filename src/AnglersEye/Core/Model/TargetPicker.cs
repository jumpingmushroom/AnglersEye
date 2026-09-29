using System;
using System.Collections.Generic;

namespace AnglersEye.Core.Model
{
    public sealed class FishSighting
    {
        /// <summary>The fish's prefab name.</summary>
        public readonly string Species;
        public readonly int Quality;
        public readonly Vec3 Position;

        public FishSighting(string species, int quality, Vec3 position)
        {
            Species = species;
            Quality = quality;
            Position = position;
        }
    }

    /// <summary>
    /// Which fish the player means (PLAN §2.2 smart bait): the one under the crosshair, else the
    /// nearest within a horizontal cone along the aim, else the species most common in range.
    /// </summary>
    public static class TargetPicker
    {
        public static FishSighting Pick(FishSighting crosshair, Vec3 origin, Vec3 aim, float coneDegrees, float range,
            IReadOnlyList<FishSighting> fish)
        {
            if (crosshair != null)
                return crosshair;
            if (fish == null || fish.Count == 0)
                return null;

            FishSighting inCone = InCone(origin, aim, coneDegrees, range, fish);
            return inCone ?? MostCommon(origin, range, fish);
        }

        private static FishSighting InCone(Vec3 origin, Vec3 aim, float coneDegrees, float range, IReadOnlyList<FishSighting> fish)
        {
            float aimLen = (float)Math.Sqrt(aim.X * aim.X + aim.Z * aim.Z);
            if (aimLen < 1e-4f)
                return null; // looking straight up or down: no horizontal direction to cone around

            double cosCone = Math.Cos(coneDegrees * Math.PI / 180.0);
            FishSighting best = null;
            float bestDist = float.MaxValue;
            foreach (FishSighting f in fish)
            {
                Vec3 d = f.Position - origin;
                float dist = d.Length;
                if (dist > range)
                    continue;
                float h = d.LengthXZ;
                double cos = h < 1e-4f ? 1.0 : (d.X * aim.X + d.Z * aim.Z) / (h * aimLen);
                if (cos >= cosCone && dist < bestDist)
                {
                    best = f;
                    bestDist = dist;
                }
            }
            return best;
        }

        private static FishSighting MostCommon(Vec3 origin, float range, IReadOnlyList<FishSighting> fish)
        {
            var count = new Dictionary<string, int>();
            var nearest = new Dictionary<string, FishSighting>();
            var nearestDist = new Dictionary<string, float>();
            foreach (FishSighting f in fish)
            {
                float dist = (f.Position - origin).Length;
                if (dist > range)
                    continue;
                int n;
                count.TryGetValue(f.Species, out n);
                count[f.Species] = n + 1;
                float nd;
                if (!nearestDist.TryGetValue(f.Species, out nd) || dist < nd)
                {
                    nearestDist[f.Species] = dist;
                    nearest[f.Species] = f;
                }
            }

            FishSighting best = null;
            int bestCount = 0;
            float bestDist = 0f;
            foreach (KeyValuePair<string, int> kv in count)
            {
                float nd = nearestDist[kv.Key];
                if (best == null || kv.Value > bestCount || (kv.Value == bestCount && nd < bestDist))
                {
                    best = nearest[kv.Key];
                    bestCount = kv.Value;
                    bestDist = nd;
                }
            }
            return best;
        }
    }
}
