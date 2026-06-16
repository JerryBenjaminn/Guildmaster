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

        /// <summary>
        /// Map a (successChance, roll) pair to an outcome band (task-03 semantics).
        /// A successful roll lands in one of the three VICTORY bands (by margin of
        /// victory); a failed roll in one of the two DEFEAT bands (by margin of
        /// failure). See the task-03 doc for the exact junction.
        /// </summary>
        public static OutcomeBand BandFor(float successChance, float roll, BalanceConfig cfg)
        {
            if (roll < successChance) // VICTORY
            {
                float margin = successChance - roll;            // how comfortably we passed
                if (margin >= cfg.flawlessMargin) return OutcomeBand.Flawless;
                if (margin <= cfg.closeCallMargin) return OutcomeBand.CloseCall;
                return OutcomeBand.Success;
            }
            else // DEFEAT
            {
                float miss = roll - successChance;               // how badly we failed
                if (miss >= cfg.catastropheMargin) return OutcomeBand.Catastrophe;
                return OutcomeBand.Failure;
            }
        }

        /// <summary>The injury severity a given band inflicts on an injured member (a RULE).</summary>
        public static InjurySeverity SeverityForBand(OutcomeBand band)
        {
            switch (band)
            {
                case OutcomeBand.Success: return InjurySeverity.Minor;     // occasional, see chance
                case OutcomeBand.CloseCall: return InjurySeverity.Minor;
                case OutcomeBand.Failure: return InjurySeverity.Major;
                case OutcomeBand.Catastrophe: return InjurySeverity.Critical;
                default: return InjurySeverity.None;                       // Flawless
            }
        }

        /// <summary>Pure death-save check: survives when the roll beats the (clamped) chance.</summary>
        public static bool SurvivesDeathSave(double rollValue, float baseChance, float survivalModifier)
        {
            float chance = Mathf.Clamp01(baseChance + survivalModifier);
            return rollValue < chance;
        }

        /// <summary>Per-band chance that an injured member is hurt (Flawless never injures).</summary>
        private static float InjuryChanceForBand(OutcomeBand band, BalanceConfig cfg)
        {
            switch (band)
            {
                case OutcomeBand.Success: return cfg.successInjuryChance;
                case OutcomeBand.CloseCall: return cfg.closeCallInjuryChance;
                case OutcomeBand.Failure: return cfg.failureInjuryChance;
                default: return 0f; // Flawless none; Catastrophe handled via death-save
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
            System.Random rng,
            float deathSaveModifier = 0f)
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

            // Rewards scale by band (gold/xp). Gold also takes the dungeon's
            // reward multiplier. Item/loot drops are deferred to task 05.
            float hours = tier != null ? tier.durationMinutes / 60f : 0f;
            float baseGold = (tier != null ? tier.goldPerHour : 0f) * dungeon.rewardMultiplier * hours;
            float baseXp = cfg.xpPerHour * hours;
            outcome.goldReward = Mathf.RoundToInt(baseGold * cfg.GoldBandMultiplier(band));
            outcome.xpReward = Mathf.RoundToInt(baseXp * cfg.XpBandMultiplier(band));

            // Injury / death per member. Catastrophe rolls a death-save (failed =
            // permadeath, D5/§5; survivor takes a Critical injury). Other bands roll
            // a per-band injury chance; severity is fixed by band (a rule).
            InjurySeverity severity = SeverityForBand(band);
            float injuryChance = InjuryChanceForBand(band, cfg);

            foreach (var memberId in teamMemberIds)
            {
                if (band == OutcomeBand.Catastrophe)
                {
                    if (SurvivesDeathSave(rng.NextDouble(), cfg.baseDeathSaveChance, deathSaveModifier))
                        AddInjury(outcome, memberId, InjurySeverity.Critical);
                    else
                        outcome.deadMemberIds.Add(memberId);
                }
                else if (severity != InjurySeverity.None && injuryChance > 0f && rng.NextDouble() < injuryChance)
                {
                    AddInjury(outcome, memberId, severity);
                }
            }

            return outcome;
        }

        private static void AddInjury(ExpeditionOutcome outcome, string memberId, InjurySeverity severity)
        {
            outcome.injuries.Add(new InjuredMember { memberId = memberId, severity = severity });
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
