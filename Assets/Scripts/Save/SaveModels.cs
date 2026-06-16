using System;
using System.Collections.Generic;

namespace Guildmaster
{
    /// <summary>
    /// Plain serializable data containers written to the three JSON save files
    /// (CLAUDE.md §3.5). Unity's JsonUtility serializes [Serializable] classes,
    /// lists and enums — but NOT dictionaries, so all maps are modelled as lists
    /// of entry structs (e.g. lineage class->count).
    ///
    ///   persistent.json -> PersistentData (guild progress, never wiped)
    ///   legacy.json     -> LegacyData     (lineage / inheritance)
    ///   current.json    -> CurrentData    (active adventurers, wipeable)
    /// </summary>

    // ---- persistent.json ------------------------------------------------------

    [Serializable]
    public class PersistentData
    {
        public int saveVersion = 1;
        public string guildName = "New Guild";
        public int guildRank = 0;

        // Gold split for the at-risk pool (D4): protected savings vs. gold earned
        // since the last death (the slice that's at risk on death).
        public long protectedGold = 0;
        public long atRiskGold = 0;

        // One-time starting-gold grant flag. Defaults false, so an existing save
        // that predates the field also receives the grant once on next load.
        public bool startingGrantGiven = false;

        public List<FacilityState> facilities = new List<FacilityState>();
        public List<TalentState> talents = new List<TalentState>();
        public int talentPoints = 0;

        public List<string> unlockedHybridClassIds = new List<string>();
        public List<string> firstClearedDungeonIds = new List<string>();
    }

    [Serializable]
    public class FacilityState
    {
        public string facilityId;
        public int level;
    }

    [Serializable]
    public class TalentState
    {
        public string talentId;
        public int rank;
    }

    // ---- legacy.json ----------------------------------------------------------

    [Serializable]
    public class LegacyData
    {
        public int saveVersion = 1;
        public int currentGeneration = 1;

        // class id -> count of ancestors of that class (dictionary as list).
        public List<LineageEntry> lineage = new List<LineageEntry>();

        // accumulated bloodline bonus per stat type, capped (D7).
        public List<BloodlineBonus> bloodlineBonuses = new List<BloodlineBonus>();

        public List<HallOfFameEntry> hallOfFame = new List<HallOfFameEntry>();

        // items available to carry to an heir (already level-scaled per D7).
        public List<string> inheritedItemPoolIds = new List<string>();
    }

    [Serializable]
    public class LineageEntry
    {
        public string classId;
        public int count;
    }

    [Serializable]
    public class BloodlineBonus
    {
        public StatType statType;
        public float fraction; // e.g. 0.20 == +20%, capped by BalanceConfig.
    }

    [Serializable]
    public class HallOfFameEntry
    {
        public string adventurerName;
        public string classId;
        public int generation;
        public int rankReached;
        public string causeOfDeath;
        public long diedAtTicksUtc;
    }

    // ---- current.json ---------------------------------------------------------

    [Serializable]
    public class CurrentData
    {
        public int saveVersion = 1;

        public List<Adventurer> roster = new List<Adventurer>();
        public List<Expedition> activeExpeditions = new List<Expedition>();

        // Wall-clock of the last save, for offline-progress calculation.
        public long lastSeenTicksUtc = 0;
    }

    [Serializable]
    public class Adventurer
    {
        public string id;
        public string displayName;
        public string classId;
        public string portraitId;        // art ref; no art authored in MVP
        public int level = 1;
        public int xp = 0;
        public int generation = 1;
        public string familyName;
        public AdventurerStatus status = AdventurerStatus.Healthy;
        public InjurySeverity injurySeverity = InjurySeverity.None;

        // Cached DERIVED stats (class@level + bloodline + gear). Recomputed by
        // StatCalculator via AdventurerManager whenever an input changes; not a
        // per-frame cost. Read by ExpeditionManager for team power.
        public StatBlock stats = new StatBlock();

        // Real, assignable equipment slots (gear effects on stats land in task 05).
        public EquipmentSlots equipment = new EquipmentSlots();

        // Assigned abilities (capacity = class.AbilitySlotsAtLevel(level)).
        public List<string> abilityIds = new List<string>();

        // For injury recovery timing (D2: recovery has NO offline cap).
        public long injuryHealAtTicksUtc = 0;
    }

    /// <summary>
    /// The four real equipment slots (GEAR_TALENT_SPEC B1). Stores item ids per
    /// slot; assignment is real now, stat effects arrive in task 05.
    /// </summary>
    [Serializable]
    public class EquipmentSlots
    {
        public string weaponItemId;
        public string armorItemId;
        public string accessory1ItemId;
        public string accessory2ItemId;

        public string Get(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Weapon: return weaponItemId;
                case ItemSlot.Armor: return armorItemId;
                case ItemSlot.Accessory1: return accessory1ItemId;
                case ItemSlot.Accessory2: return accessory2ItemId;
                default: return null;
            }
        }

        public void Set(ItemSlot slot, string itemId)
        {
            switch (slot)
            {
                case ItemSlot.Weapon: weaponItemId = itemId; break;
                case ItemSlot.Armor: armorItemId = itemId; break;
                case ItemSlot.Accessory1: accessory1ItemId = itemId; break;
                case ItemSlot.Accessory2: accessory2ItemId = itemId; break;
            }
        }

        /// <summary>Non-empty equipped item ids (for inheritance, etc.).</summary>
        public List<string> AllItemIds()
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(weaponItemId)) list.Add(weaponItemId);
            if (!string.IsNullOrEmpty(armorItemId)) list.Add(armorItemId);
            if (!string.IsNullOrEmpty(accessory1ItemId)) list.Add(accessory1ItemId);
            if (!string.IsNullOrEmpty(accessory2ItemId)) list.Add(accessory2ItemId);
            return list;
        }
    }

    /// <summary>
    /// An active or completed expedition. The OUTCOME is rolled and stored the
    /// moment the team is sent (D3) — closing the app cannot change it. The timer
    /// only reveals it; it does not decide it.
    /// </summary>
    [Serializable]
    public class Expedition
    {
        public string id;
        public string dungeonId;
        public List<string> teamMemberIds = new List<string>();

        public long sendTimeTicksUtc;
        public long durationSeconds;
        public bool collected = false;

        // Locked-at-send outcome (D3). Hidden from UI until completion.
        public ExpeditionOutcome outcome = new ExpeditionOutcome();

        public long CompleteTimeTicksUtc =>
            sendTimeTicksUtc + TimeSpan.FromSeconds(durationSeconds).Ticks;
    }

    [Serializable]
    public class ExpeditionOutcome
    {
        public float successChance;
        public float roll;
        public bool success;
        public OutcomeBand band;

        public int goldReward;
        public int xpReward;
        public int lootSeed;

        public List<string> injuredMemberIds = new List<string>();
        public List<string> deadMemberIds = new List<string>();
    }
}
