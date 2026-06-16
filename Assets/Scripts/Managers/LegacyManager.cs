using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// The dynasty engine (CLAUDE.md §5, D4, D7) backed by legacy.json. Handles the
    /// death -> legacy step: at-risk gold loss (D4), item level-scaling (D7),
    /// bloodline bonus accumulation (capped, D7), lineage tracking, and Hall of
    /// Fame records. The actual CLASS CHOICE for the heir is an active player
    /// decision in the UI (UI_SPEC §4.3) — this manager prepares the data and
    /// applies the inheritance once confirmed; it never auto-resolves the moment.
    ///
    /// Public API:
    ///   Bind(legacy)                         - attach legacy data slice.
    ///   CurrentGeneration
    ///   GetBloodlineBonus(stat)
    ///   GetLineageCount(classId)
    ///   ProcessDeath(dead, cause, balance)   - applies inheritance math, returns a summary.
    ///   InheritedItemLevel(level, balance)   - D7 helper.
    /// </summary>
    public class LegacyManager : Singleton<LegacyManager>
    {
        private LegacyData _data;

        public void Bind(LegacyData data)
        {
            _data = data;
        }

        public int CurrentGeneration => _data.currentGeneration;

        public float GetBloodlineBonus(StatType stat)
        {
            var b = _data.bloodlineBonuses.Find(x => x.statType == stat);
            return b != null ? b.fraction : 0f;
        }

        public int GetLineageCount(string classId)
        {
            var e = _data.lineage.Find(x => x.classId == classId);
            return e != null ? e.count : 0;
        }

        /// <summary>D7: level an inherited item scales to.</summary>
        public int InheritedItemLevel(int originalLevel, BalanceConfig balance)
        {
            return LegacyMath.ScaleInheritedItemLevel(originalLevel, balance.itemInheritScale);
        }

        /// <summary>
        /// Apply the consequences of a death and prepare the legacy. Updates lineage,
        /// accumulates a (capped) bloodline bonus, records the Hall of Fame entry,
        /// scales the dead member's items into the inheritable pool, and applies the
        /// at-risk gold loss via GuildManager (D4). Returns a summary for the UI.
        /// </summary>
        public LegacyResult ProcessDeath(Adventurer dead, string causeOfDeath, BalanceConfig balance)
        {
            var result = new LegacyResult();

            // Lineage: increment count for the dead member's class.
            IncrementLineage(dead.classId);

            // Bloodline bonus: contribute a small, capped per-stat bonus (D7). The
            // contribution magnitude is a feel value sourced from balance; here we
            // grant a fraction toward each of the member's strongest axes. For the
            // bootstrap stub we contribute toward Attack as a representative axis.
            float contribution = balance.bloodlineCapPerStat * 0.1f; // small step; capped below
            AccumulateBloodline(StatType.Attack, contribution, balance.bloodlineCapPerStat);
            result.bloodlineStat = StatType.Attack;
            result.bloodlineBonusAfter = GetBloodlineBonus(StatType.Attack);

            // Items: scale to inheritable pool (D7).
            foreach (var itemId in dead.equippedItemIds)
            {
                _data.inheritedItemPoolIds.Add(itemId);
                result.inheritedItemIds.Add(itemId);
            }

            // Gold at-risk loss (D4) via GuildManager.
            if (GuildManager.Instance != null)
            {
                result.goldLost = GuildManager.Instance.ApplyDeathGoldLoss(balance);
            }

            // Hall of Fame record (UI_SPEC §3 Hall).
            _data.hallOfFame.Add(new HallOfFameEntry
            {
                adventurerName = dead.displayName,
                classId = dead.classId,
                generation = dead.generation,
                rankReached = dead.level,
                causeOfDeath = causeOfDeath,
                diedAtTicksUtc = DateTime.UtcNow.Ticks,
            });

            // Advance the dynasty generation for the heir.
            _data.currentGeneration++;
            result.newGeneration = _data.currentGeneration;

            return result;
        }

        private void IncrementLineage(string classId)
        {
            if (string.IsNullOrEmpty(classId)) return;
            var e = _data.lineage.Find(x => x.classId == classId);
            if (e == null)
            {
                e = new LineageEntry { classId = classId, count = 0 };
                _data.lineage.Add(e);
            }
            e.count++;
        }

        private void AccumulateBloodline(StatType stat, float add, float cap)
        {
            var b = _data.bloodlineBonuses.Find(x => x.statType == stat);
            if (b == null)
            {
                b = new BloodlineBonus { statType = stat, fraction = 0f };
                _data.bloodlineBonuses.Add(b);
            }
            b.fraction = LegacyMath.AccumulateBloodlineBonus(b.fraction, add, cap);
        }
    }

    /// <summary>Summary of a processed death, handed to the UI for the legacy moment.</summary>
    public class LegacyResult
    {
        public long goldLost;
        public int newGeneration;
        public StatType bloodlineStat;
        public float bloodlineBonusAfter;
        public List<string> inheritedItemIds = new List<string>();
    }
}
