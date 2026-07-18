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
        PikeInfantry,
        SpearInfantry,
        MountedSkirmisher
    }

    /// <summary>
    /// Shared role rules. Agent classification uses the actual rolled spawn kit;
    /// character classification is a stable best-effort fallback for campaign UI.
    /// </summary>
    public static class TroopRoleClassifier
    {
        private const string JavelinUsage = "Javelin";
        private const string JavelinAlternativeUsage = "OneHandedPolearm_JavelinAlternative";
        private const string ThrownPolearmUsage = "TwoHandedPolearm_Thrown";

        public static TroopRole Classify(Agent? agent)
        {
            if (agent == null)
                return TroopRole.LightInfantry;

            Loadout loadout = Inspect(slot => agent.SpawnEquipment[slot].Item);
            TroopRole role = Classify(agent.HasMount, loadout);
            TroopClassifierLogger.Log(agent, role, loadout);
            return role;
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
                    roles.Add(Classify(troop.IsMounted, Inspect(slot => equipment[slot].Item)));
                }
            }

            if (!yieldedAny && troop.Equipment != null)
                roles.Add(Classify(troop.IsMounted, Inspect(slot => troop.Equipment[slot].Item)));

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

        private static TroopRole Classify(bool isMounted, Loadout loadout)
        {
            if (isMounted)
            {
                if (loadout.HasBow || loadout.HasCrossbow || loadout.HasSling)
                    return TroopRole.HorseArcher;

                if (loadout.HasThrowing)
                    return TroopRole.MountedSkirmisher;

                return TroopRole.MeleeCavalry;
            }

            if (loadout.HasBow || loadout.HasSling) return TroopRole.FootArcher;
            if (loadout.HasCrossbow) return TroopRole.Crossbowman;
            if (loadout.HasPike) return TroopRole.PikeInfantry;
            if (loadout.HasLargeSwingable) return TroopRole.ShockInfantry;

            // Pilum's thrown usage is TwoHandedPolearm_Thrown, even though the
            // engine's weapon class is Javelin. Only true Javelin usage counts.
            if (loadout.JavelinStacks >= 2 ||
                (loadout.JavelinStacks == 1 && (!loadout.HasShield || loadout.OccupiedWeaponSlots <= 3)))
                return TroopRole.Skirmisher;

            if (loadout.HasShield && loadout.HasSpear) return TroopRole.SpearInfantry;

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
                loadout.WeaponItemIds.Add(itemId);
                foreach (WeaponComponentData weapon in item.Weapons)
                {
                    if (weapon.IsShield) loadout.HasShield = true;
                    if (weapon.WeaponClass == WeaponClass.Bow) loadout.HasBow = true;
                    if (weapon.WeaponClass == WeaponClass.Crossbow) loadout.HasCrossbow = true;
                    if (weapon.WeaponClass == WeaponClass.Sling) loadout.HasSling = true;
                    if (IsPike(itemId, itemName, weapon)) loadout.HasPike = true;
                    if (IsLargeSwingable(weapon)) loadout.HasLargeSwingable = true;
                    if (IsSpear(itemId, itemName, weapon)) loadout.HasSpear = true;
                    if (weapon.IsRangedWeapon && weapon.WeaponClass != WeaponClass.Bow && weapon.WeaponClass != WeaponClass.Crossbow && weapon.WeaponClass != WeaponClass.Sling)
                        loadout.HasThrowing = true;
                }

                if (IsJavelin(item))
                    loadout.JavelinStacks++;
            }

            return loadout;
        }

        private static bool IsSpear(string itemId, string itemName, WeaponComponentData weapon)
            => weapon.IsPolearm &&
               !IsPike(itemId, itemName, weapon) &&
               !string.Equals(weapon.ItemUsage, ThrownPolearmUsage, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Identifies a normal javelin item stack, including generated items whose
        /// runtime primary usage is the one-handed alternative. A Pilum is still
        /// excluded because its item exposes the thrown-polearm usage.
        /// </summary>
        private static bool IsJavelin(ItemObject item)
        {
            bool hasNormalJavelinUsage = false;
            foreach (WeaponComponentData weapon in item.Weapons)
            {
                if (string.Equals(weapon.ItemUsage, ThrownPolearmUsage, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (string.Equals(weapon.ItemUsage, JavelinUsage, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(weapon.ItemUsage, JavelinAlternativeUsage, StringComparison.OrdinalIgnoreCase))
                    hasNormalJavelinUsage = true;
            }

            if (hasNormalJavelinUsage)
                return true;

            // Bannerlord's crafted javelins can expose no usable description in
            // ItemObject.Weapons. Their generated IDs remain stable and do not
            // overlap Pilum/throwable-polearm IDs.
            return (item.StringId?.IndexOf("javelin", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

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
                case TroopRole.HorseArcher: return 11;
                case TroopRole.MountedSkirmisher: return 10;
                case TroopRole.MeleeCavalry: return 9;
                case TroopRole.FootArcher: return 8;
                case TroopRole.Crossbowman: return 7;
                case TroopRole.PikeInfantry: return 6;
                case TroopRole.ShockInfantry: return 5;
                case TroopRole.Skirmisher: return 4;
                case TroopRole.SpearInfantry: return 3;
                case TroopRole.ShieldInfantry: return 2;
                default: return 1;
            }
        }

        private sealed class Loadout
        {
            public bool HasBow { get; set; }
            public bool HasCrossbow { get; set; }
            public bool HasSling { get; set; }
            public bool HasShield { get; set; }
            public bool HasPike { get; set; }
            public bool HasLargeSwingable { get; set; }
            public bool HasSpear { get; set; }
            public bool HasThrowing { get; set; }
            public int JavelinStacks { get; set; }
            public int OccupiedWeaponSlots { get; set; }
            public List<string> WeaponItemIds { get; } = new List<string>();

            public string Describe()
                => string.Join(", ", WeaponItemIds);
        }

        private static class TroopClassifierLogger
        {
            private static readonly object Sync = new object();
            private static readonly HashSet<string> LoggedLoadouts = new HashSet<string>(StringComparer.Ordinal);
            private static Mission? _mission;
            private static string? _logPath;

            public static void Log(Agent agent, TroopRole role, Loadout loadout)
            {
                try
                {
                    string troopId = agent.Character?.StringId ?? "unknown";
                    string troopName = agent.Character?.Name?.ToString() ?? troopId;
                    string weaponKit = loadout.Describe();
                    string key = string.Concat(troopId, "|", agent.HasMount, "|", weaponKit);

                    lock (Sync)
                    {
                        if (!ReferenceEquals(_mission, agent.Mission))
                        {
                            _mission = agent.Mission;
                            LoggedLoadouts.Clear();
                            Append($"--- New mission: {agent.Mission?.GetType().Name ?? "unknown"} ---");
                        }

                        if (!LoggedLoadouts.Add(key))
                            return;

                        Append($"{troopId} ({troopName}) -> {role} | mounted={agent.HasMount} | " +
                               $"javelinStacks={loadout.JavelinStacks}, shield={loadout.HasShield}, " +
                               $"pike={loadout.HasPike}, swingable={loadout.HasLargeSwingable}, sling={loadout.HasSling} | weapons=[{weaponKit}]");
                    }
                }
                catch
                {
                    // Diagnostics must never interfere with a mission.
                }
            }

            public static void Reset()
            {
                lock (Sync)
                {
                    _mission = null;
                    LoggedLoadouts.Clear();
                    _logPath = null;
                    try
                    {
                        string path = GetLogPath();
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }
                    catch
                    {
                        // Diagnostics must never interfere with module loading.
                    }
                }
            }

            private static void Append(string message)
            {
                System.IO.File.AppendAllText(
                    GetLogPath(),
                    $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }

            private static string GetLogPath()
            {
                if (!string.IsNullOrEmpty(_logPath))
                    return _logPath!;

                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string directory = System.IO.Path.Combine(documents, "Mount and Blade II Bannerlord", "Configs");
                System.IO.Directory.CreateDirectory(directory);
                _logPath = System.IO.Path.Combine(directory, "TroopClassifier_Log.txt");
                return _logPath!;
            }
        }

        internal static void ResetLog()
            => TroopClassifierLogger.Reset();
    }
}
