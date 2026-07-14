using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TroopClassifier
{
    /// <summary>
    /// Stable tactical roles derived from the equipment brought to a battle.
    /// They describe a battlefield job, never troop tier, culture, or armour.
    /// </summary>
    public enum TroopRole
    {
        LightInfantry,
        ShieldInfantry,
        ShockInfantry,
        Skirmisher,
        FootArcher,
        Crossbowman,
        MeleeCavalry,
        HorseArcher,
        PikeInfantry
    }

    /// <summary>
    /// Shared role rules. Agent classification uses the actual rolled spawn kit;
    /// character classification is a stable best-effort fallback for campaign UI.
    /// </summary>
    public static class TroopRoleClassifier
    {
        private const string JavelinUsage = "Javelin";
        private const string ThrownPolearmUsage = "TwoHandedPolearm_Thrown";

        public static TroopRole Classify(Agent? agent)
        {
            if (agent == null)
                return TroopRole.LightInfantry;

            return Classify(agent.HasMount, slot => agent.SpawnEquipment[slot].Item);
        }

        public static TroopRole Classify(BasicCharacterObject? troop)
        {
            if (troop == null)
                return TroopRole.LightInfantry;

            var roles = new List<TroopRole>();
            bool yieldedAny = false;
            if (troop.BattleEquipments != null)
            {
                foreach (var equipment in troop.BattleEquipments)
                {
                    if (equipment == null)
                        continue;

                    yieldedAny = true;
                    roles.Add(Classify(troop.IsMounted, slot => equipment[slot].Item));
                }
            }

            if (!yieldedAny && troop.Equipment != null)
                roles.Add(Classify(troop.IsMounted, slot => troop.Equipment[slot].Item));

            if (roles.Count == 0)
                return TroopRole.LightInfantry;

            TroopRole best = TroopRole.LightInfantry;
            int bestCount = -1;
            foreach (TroopRole role in roles)
            {
                int count = 0;
                foreach (TroopRole candidate in roles)
                {
                    if (candidate == role)
                        count++;
                }

                if (count > bestCount || (count == bestCount && Priority(role) > Priority(best)))
                {
                    best = role;
                    bestCount = count;
                }
            }

            return best;
        }

        private static TroopRole Classify(bool isMounted, Func<int, ItemObject?> getItem)
        {
            Loadout loadout = Inspect(getItem);
            if (isMounted)
                return loadout.HasBow || loadout.HasCrossbow ? TroopRole.HorseArcher : TroopRole.MeleeCavalry;

            if (loadout.HasBow) return TroopRole.FootArcher;
            if (loadout.HasCrossbow) return TroopRole.Crossbowman;
            if (loadout.HasPike) return TroopRole.PikeInfantry;
            if (loadout.HasLargeSwingable) return TroopRole.ShockInfantry;

            // Pilum's thrown usage is TwoHandedPolearm_Thrown, even though the
            // engine's weapon class is Javelin. Only true Javelin usage counts.
            if (loadout.JavelinStacks >= 2 ||
                (loadout.JavelinStacks == 1 && (!loadout.HasShield || loadout.OccupiedWeaponSlots <= 3)))
                return TroopRole.Skirmisher;

            return loadout.HasShield ? TroopRole.ShieldInfantry : TroopRole.LightInfantry;
        }

        private static Loadout Inspect(Func<int, ItemObject?> getItem)
        {
            var loadout = new Loadout();
            for (int slot = (int)EquipmentIndex.WeaponItemBeginSlot; slot < (int)EquipmentIndex.NumPrimaryWeaponSlots; slot++)
            {
                ItemObject? item = getItem(slot);
                if (item == null)
                    continue;

                loadout.OccupiedWeaponSlots++;
                string itemId = item.StringId ?? string.Empty;
                string itemName = item.Name?.ToString() ?? string.Empty;
                bool hasNormalJavelinUsage = false;
                foreach (WeaponComponentData weapon in item.Weapons)
                {
                    if (weapon.IsShield) loadout.HasShield = true;
                    if (weapon.WeaponClass == WeaponClass.Bow) loadout.HasBow = true;
                    if (weapon.WeaponClass == WeaponClass.Crossbow) loadout.HasCrossbow = true;
                    if (IsNormalJavelin(weapon)) hasNormalJavelinUsage = true;
                    if (IsPike(itemId, itemName, weapon)) loadout.HasPike = true;
                    if (IsLargeSwingable(weapon)) loadout.HasLargeSwingable = true;
                }

                if (hasNormalJavelinUsage)
                    loadout.JavelinStacks++;
            }

            return loadout;
        }

        private static bool IsNormalJavelin(WeaponComponentData weapon)
            => weapon.WeaponClass == WeaponClass.Javelin &&
               string.Equals(weapon.ItemUsage, JavelinUsage, StringComparison.OrdinalIgnoreCase);

        private static bool IsPike(string itemId, string itemName, WeaponComponentData weapon)
            => weapon.IsPolearm &&
               (itemId.IndexOf("pike", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("pike", StringComparison.OrdinalIgnoreCase) >= 0);

        private static bool IsLargeSwingable(WeaponComponentData weapon)
        {
            if (string.Equals(weapon.ItemUsage, ThrownPolearmUsage, StringComparison.OrdinalIgnoreCase))
                return false;

            if (weapon.IsTwoHanded && weapon.SwingDamage > 0)
                return true;

            return weapon.WeaponClass == WeaponClass.TwoHandedSword ||
                   weapon.WeaponClass == WeaponClass.TwoHandedAxe ||
                   weapon.WeaponClass == WeaponClass.TwoHandedMace ||
                   ((weapon.WeaponClass == WeaponClass.TwoHandedPolearm || weapon.WeaponClass == WeaponClass.LowGripPolearm) && weapon.SwingDamage > 0);
        }

        private static int Priority(TroopRole role)
        {
            switch (role)
            {
                case TroopRole.HorseArcher: return 9;
                case TroopRole.MeleeCavalry: return 8;
                case TroopRole.FootArcher: return 7;
                case TroopRole.Crossbowman: return 6;
                case TroopRole.PikeInfantry: return 5;
                case TroopRole.ShockInfantry: return 4;
                case TroopRole.Skirmisher: return 3;
                case TroopRole.ShieldInfantry: return 2;
                default: return 1;
            }
        }

        private sealed class Loadout
        {
            public bool HasBow { get; set; }
            public bool HasCrossbow { get; set; }
            public bool HasShield { get; set; }
            public bool HasPike { get; set; }
            public bool HasLargeSwingable { get; set; }
            public int JavelinStacks { get; set; }
            public int OccupiedWeaponSlots { get; set; }
        }
    }
}
