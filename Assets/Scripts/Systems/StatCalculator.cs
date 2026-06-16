using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Derives an adventurer's live stats (Task02 §2). The pipeline is:
    ///
    ///   class base @ level   (content)
    ///   + gear flat stats    [hook — ZERO until task 05]
    ///   then * (1 + bloodline%) per stat   [hook — empty/ZERO until task 04]
    ///
    /// The bloodline and gear contributions are real entry points that sum to
    /// zero now, so tasks 04/05 plug in without reworking the model. Pure and
    /// testable.
    /// </summary>
    public static class StatCalculator
    {
        public static StatBlock Derive(
            AdventurerClassData cls,
            int level,
            IReadOnlyList<BloodlineBonus> bloodline,
            StatBlock gearFlat)
        {
            StatBlock baseStats = cls != null ? cls.StatsAtLevel(level) : default;

            // Gear flat stats (task 05 will populate; zero for now).
            StatBlock withGear = baseStats + gearFlat;

            // Bloodline multiplicative bonus per stat (task 04 will populate; empty = identity).
            return ApplyBloodline(withGear, bloodline);
        }

        /// <summary>Gear flat-stat total — ZERO hook until task 05 (effects on stats).</summary>
        public static StatBlock GearFlatStats(Adventurer adventurer)
        {
            return default; // intentionally zero: gear assignment is real, stat effects are task 05
        }

        private static StatBlock ApplyBloodline(StatBlock s, IReadOnlyList<BloodlineBonus> bloodline)
        {
            if (bloodline == null || bloodline.Count == 0) return s;

            var result = s;
            foreach (var b in bloodline)
            {
                float mult = 1f + b.fraction;
                switch (b.statType)
                {
                    case StatType.HP: result.hp = Mathf.RoundToInt(result.hp * mult); break;
                    case StatType.Mana: result.mana = Mathf.RoundToInt(result.mana * mult); break;
                    case StatType.Attack: result.attack = Mathf.RoundToInt(result.attack * mult); break;
                    case StatType.MagicPower: result.magicPower = Mathf.RoundToInt(result.magicPower * mult); break;
                    case StatType.Defense: result.defense = Mathf.RoundToInt(result.defense * mult); break;
                    case StatType.Speed: result.speed = Mathf.RoundToInt(result.speed * mult); break;
                    case StatType.Crit: result.crit = Mathf.RoundToInt(result.crit * mult); break;
                }
            }
            return result;
        }
    }
}
