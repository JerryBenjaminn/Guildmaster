using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Auto-resolve combat math (CLAUDE.md D5 "Option A"): sum team Power, compare
    /// to dungeon difficulty, roll. NO visual battler (deferred §9). All numbers
    /// come from <see cref="BalanceConfig"/> — nothing hardcoded here.
    ///
    /// This is the MVP STUB resolve: the success/band/injury/death MECHANISM is
    /// real and honest (and locked at send-time, D3), while the content feeding it
    /// is minimal. Pure & deterministic given a seeded RNG, so it is fully testable
    /// and so the locked outcome reproduces exactly.
    /// </summary>
    public static class CombatResolver
    {
        public static float Power(StatBlock s, BalanceConfig cfg)
        {
            return s.hp * cfg.powerPerHp
                 + s.mana * cfg.powerPerMana
                 + s.attack * cfg.powerPerAttack
                 + s.magicPower * cfg.powerPerMagicPower
                 + s.defense * cfg.powerPerDefense
                 + s.speed * cfg.powerPerSpeed
                 + s.crit * cfg.powerPerCrit;
        }

        public static float TeamPower(IEnumerable<StatBlock> members, BalanceConfig cfg)
        {
            float total = 0f;
            foreach (var m in members) total += Power(m, cfg);
            return total;
        }

        /// <summary>Success chance, always clamped to the §7 frame [min, max].</summary>
        public static float SuccessChance(float teamPower, float difficulty, BalanceConfig cfg)
        {
            float raw = 0.5f + (teamPower - difficulty) * cfg.powerToSuccessSensitivity;
            return Mathf.Clamp(raw, cfg.successChanceMin, cfg.successChanceMax);
        }

        public static OutcomeBand BandFor(float successChance, float roll, BalanceConfig cfg)
        {
            // roll in [0,1): success when roll < successChance.
            if (roll < successChance)
            {
                float margin = successChance - roll;            // how comfortably we passed
                return margin >= successChance * cfg.flawlessMargin
                    ? OutcomeBand.Flawless
                    : OutcomeBand.Success;
            }
            else
            {
                float miss = roll - successChance;               // how badly we failed
                if (miss <= cfg.closeCallMargin) return OutcomeBand.CloseCall;
                if (miss >= cfg.catastropheMargin) return OutcomeBand.Catastrophe;
                return OutcomeBand.Failure;
            }
        }

        /// <summary>
        /// Roll a complete, locked outcome for a team against a dungeon. Called
        /// once at send-time; the result is stored and revealed later unchanged.
        /// </summary>
        public static ExpeditionOutcome Resolve(
            float teamPower,
            DungeonData dungeon,
            ExpeditionTier tier,
            BalanceConfig cfg,
            IReadOnlyList<string> teamMemberIds,
            System.Random rng)
        {
            float difficulty = EffectiveDifficulty(dungeon);
            float chance = SuccessChance(teamPower, difficulty, cfg);
            float roll = (float)rng.NextDouble();
            bool success = roll < chance;
            OutcomeBand band = BandFor(chance, roll, cfg);

            var outcome = new ExpeditionOutcome
            {
                successChance = chance,
                roll = roll,
                success = success,
                band = band,
                lootSeed = rng.Next(),
            };

            if (success)
            {
                float hours = tier != null ? tier.durationMinutes / 60f : 0f;
                float goldPerHour = (tier != null ? tier.goldPerHour : 0f) * dungeon.rewardMultiplier;
                outcome.goldReward = Mathf.RoundToInt(goldPerHour * hours);
                outcome.xpReward = Mathf.RoundToInt(cfg.xpPerHour * hours);
            }

            // Injury / death rolls per member, gated by band. Permadeath only on a
            // failed death-save during a Catastrophe (D5 / §5).
            float injuryChance = band == OutcomeBand.CloseCall ? cfg.injuryChanceCloseCall
                               : band == OutcomeBand.Failure ? cfg.injuryChanceFailure
                               : 0f;

            foreach (var memberId in teamMemberIds)
            {
                if (band == OutcomeBand.Catastrophe)
                {
                    bool saved = rng.NextDouble() < cfg.baseDeathSaveChance;
                    if (saved) outcome.injuredMemberIds.Add(memberId);
                    else outcome.deadMemberIds.Add(memberId);
                }
                else if (injuryChance > 0f && rng.NextDouble() < injuryChance)
                {
                    outcome.injuredMemberIds.Add(memberId);
                }
            }

            return outcome;
        }

        /// <summary>Dungeon base difficulty plus the power its enemies contribute.</summary>
        public static float EffectiveDifficulty(DungeonData dungeon)
        {
            float total = dungeon.difficulty;
            if (dungeon.enemies != null)
            {
                foreach (var e in dungeon.enemies)
                {
                    if (e != null) total += e.powerContribution;
                }
            }
            return total;
        }
    }
}
