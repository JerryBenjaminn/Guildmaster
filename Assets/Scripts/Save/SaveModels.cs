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
        public int level = 1;
        public int xp = 0;
        public int generation = 1;
        public string familyName;
        public AdventurerStatus status = AdventurerStatus.Healthy;

        // Snapshot of computed stats (class + level + bloodline at creation).
        public StatBlock stats = new StatBlock();

        public List<string> equippedItemIds = new List<string>();
        public List<string> abilityIds = new List<string>();

        // For injury recovery timing (D2: recovery has NO offline cap).
        public long injuryHealAtTicksUtc = 0;
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
