using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.Core
{
    /// <summary>
    /// Before the rod resolves its ammo (Attack.StartDraw / Attack.Start both call HaveAmmo then
    /// EquipAmmoItem, PLAN §1.6), equip the carried bait that works best on the aimed fish. Only a
    /// normal equip of an item you own; bait rules are untouched.
    /// </summary>
    internal static class SmartBait
    {
        // The rod is drawn like a bow: StartDraw fires on draw, Start fires on release, so one cast
        // can run the "needs bait" check twice. The panel already shows the same "needs ..." line,
        // so suppressing an identical repeat within this window loses nothing.
        private const float RepeatMessageAfter = 15f;
        private static string _lastMessageText;
        private static float _lastMessageTime = -RepeatMessageAfter;

        public static void BeforeCast(Humanoid character, ItemDrop.ItemData weapon)
        {
            Player p = character as Player;
            if (p == null || p != Player.m_localPlayer || !Features.On(Feature.SmartBait) || !Tackle.IsRod(weapon))
                return;

            FishSighting target = Targeting.Aim(p);
            FishInfo info;
            if (target == null || !FishCatalog.All.TryGetValue(target.Species, out info))
                return;
            BaitAdvice advice = Tackle.Advise(p, info);
            if (advice == null)
                return;

            if (!advice.Carried)
            {
                string text = Labels.NeedsBait(info.Name, advice.Best.BaitName);
                if (text != _lastMessageText || Time.time - _lastMessageTime > RepeatMessageAfter)
                {
                    _lastMessageText = text;
                    _lastMessageTime = Time.time;
                    p.Message(MessageHud.MessageType.Center, text);
                }
                return;
            }

            string type = weapon.m_shared.m_ammoType;
            ItemDrop.ItemData current = VanillaChoice(p, type);
            if (current != null && current.m_dropPrefab != null && current.m_dropPrefab.name == advice.Best.BaitId)
                return;
            ItemDrop.ItemData pick = p.GetInventory().GetAmmoItem(type, advice.Best.BaitId);
            if (pick == null)
                return;
            bool ok = p.EquipItem(pick, false);
            if (PluginConfig.Verbose.Value)
                AnglersEyePlugin.Log.LogInfo("Angler's Eye: smart bait " + info.Prefab + " -> " + advice.Best.BaitId + (ok ? "" : " (equip refused)"));
        }

        /// <summary>What Attack.FindAmmo would use: the equipped ammo if valid, else the lowest grid slot.</summary>
        private static ItemDrop.ItemData VanillaChoice(Player p, string type)
        {
            ItemDrop.ItemData eq = p.GetAmmoItem();
            if (eq != null && p.GetInventory().ContainsItem(eq) && eq.m_shared.m_ammoType == type)
                return eq;
            return p.GetInventory().GetAmmoItem(type);
        }
    }
}
