using UnityEngine;

namespace AnglersEye.Core
{
    internal sealed class FishingSnapshot
    {
        public Player Player;
        public ItemDrop.ItemData Rod;
        public bool RodEquipped;
        public FishingFloat Float;
        public Fish Catch;
        public bool Escaping;
        public float RemainingEscape;
        public float LineLength;
        /// <summary>The fish nibbling or heading for the float, else the nearest in its range.</summary>
        public Fish Subject;
    }

    /// <summary>The local player's fishing, read once per frame (PLAN §1.2–1.4).</summary>
    internal static class FishingState
    {
        public static readonly FishingSnapshot Current = new FishingSnapshot();

        /// <summary>The float's m_range is 50 m (PLAN §1.7); the label is about the fish at your float, not one across the pond.</summary>
        private const float NearbyRadius = 10f;

        /// <summary>The float belongs to the local player (ZDO rodOwner, as FishingFloat.GetOwner reads it).</summary>
        public static bool IsLocal(FishingFloat ff)
        {
            Player p = Player.m_localPlayer;
            if (p == null || ff == null || ff.m_nview == null || !ff.m_nview.IsValid())
                return false;
            return ff.m_nview.GetZDO().GetLong(ZDOVars.s_rodOwner, 0L) == p.GetZDOID().UserID;
        }

        public static FishingFloat LocalFloat()
        {
            foreach (FishingFloat ff in FishingFloat.GetAllInstances())
                if (IsLocal(ff))
                    return ff;
            return null;
        }

        public static void Refresh()
        {
            FishingSnapshot s = Current;
            Player p = Player.m_localPlayer;
            s.Player = p;
            s.Rod = p != null ? p.GetCurrentWeapon() : null;
            s.RodEquipped = Tackle.IsRod(s.Rod);
            s.Float = p != null ? LocalFloat() : null;
            s.Catch = s.Float != null ? s.Float.GetCatch() : null;
            // The hooker owns the fish (Fish.OnHooked claims ownership), so its escape timer is live here.
            s.Escaping = s.Catch != null && s.Catch.IsEscaping();
            s.RemainingEscape = s.Catch != null ? Mathf.Max(0f, s.Catch.m_escapeTime) : 0f;
            s.LineLength = s.Float != null ? s.Float.m_lineLength : 0f;
            s.Subject = s.Float != null && s.Catch == null ? SubjectFor(s.Float) : null;
        }

        private static Fish SubjectFor(FishingFloat ff)
        {
            if (ff.m_nibbler != null && Time.time - ff.m_nibbleTime < 2f)
                return ff.m_nibbler;
            Fish nearest = null;
            float best = Mathf.Min(ff.m_range, NearbyRadius);
            Vector3 at = ff.transform.position;
            foreach (IMonoUpdater u in Fish.Instances)
            {
                Fish f = u as Fish;
                if (f == null || f.IsOutOfWater())
                    continue;
                // Only meaningful when we own the fish (its AI runs here); otherwise null (PLAN §1.1).
                if (f.m_waypointFF == ff)
                    return f;
                float d = Vector3.Distance(f.transform.position, at);
                if (d <= best)
                {
                    best = d;
                    nearest = f;
                }
            }
            return nearest;
        }
    }
}
