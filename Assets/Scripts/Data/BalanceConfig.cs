using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Central tunables (CLAUDE.md §7). THIS ASSET IS THE SOURCE OF TRUTH for
    /// balance — never hardcode these numbers in logic (.cs) files. Field
    /// defaults below are SEED values; they exist so the game runs before an
    /// asset is authored, and are overridden by the BalanceConfig.asset created
    /// in the Inspector (placed under a Resources folder so the bootstrap can
    /// load it).
    ///
    /// Frames (hard limits from §7 / D1-D7) vs. feel (proposed seeds, tune freely)
    /// are noted per field. See the bootstrap report for the seed summary.
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Guildmaster/Balance Config", order = 0)]
    public class BalanceConfig : ScriptableObject
    {
        [Header("Offline accrual (D2 — FRAME)")]
        [Tooltip("FRAME: expeditions/training accrue offline up to this many hours, then hold. Injury recovery is NOT capped.")]
        public float offlineCapHours = 12f;

        [Header("Success chance clamp (§7 — FRAME)")]
        [Tooltip("FRAME: success chance can never go below this.")]
        [Range(0f, 1f)] public float successChanceMin = 0.05f;
        [Tooltip("FRAME: success chance can never reach 100% (D-rule: never 100%).")]
        [Range(0f, 1f)] public float successChanceMax = 0.95f;

        [Header("Inheritance anti-snowball (D7 — FRAME)")]
        [Tooltip("FRAME: inherited items scale to this fraction of original level (~0.3). The main power-creep dial.")]
        [Range(0f, 1f)] public float itemInheritScale = 0.3f;
        [Tooltip("FRAME: bloodline bonus caps at this fraction (+0.20 = +20%) per stat type.")]
        [Range(0f, 1f)] public float bloodlineCapPerStat = 0.20f;

        [Header("Gold at-risk pool (D4 — SEED %, fraction is a feel decision)")]
        [Tooltip("SEED (feel): on death, lose this fraction of gold earned since the last death. Surviving at-risk gold is banked into protected savings. Propose 0.25 — tune.")]
        [Range(0f, 1f)] public float goldAtRiskLossPercent = 0.25f;

        [Header("Power weighting (SEED — structural, how stats become Power)")]
        [Tooltip("SEED: each point of these stats contributes this much to Power. Tune for feel.")]
        public float powerPerHp = 0.1f;
        public float powerPerMana = 0.1f;
        public float powerPerAttack = 1f;
        public float powerPerMagicPower = 1f;
        public float powerPerDefense = 1f;
        [Tooltip("SEED: Speed has no auto-resolve role yet (reserved for future turn-order); weight 0 so it doesn't distort combat. Tune when a use exists.")]
        public float powerPerSpeed = 0f;
        public float powerPerCrit = 1f;

        [Header("Success formula (SEED — feel)")]
        [Tooltip("SEED: success chance = 0.5 + (teamPower - difficulty) * sensitivity, then clamped to [min,max].")]
        public float powerToSuccessSensitivity = 0.01f;

        [Header("Outcome band thresholds (SEED — feel; victory/defeat split, see task-03 doc)")]
        [Tooltip("SEED: VICTORY margin (successChance - roll) >= this => Flawless.")]
        [Range(0f, 1f)] public float flawlessMargin = 0.30f;
        [Tooltip("SEED: VICTORY margin <= this => CloseCall (barely won); otherwise Success.")]
        [Range(0f, 1f)] public float closeCallMargin = 0.10f;
        [Tooltip("SEED: DEFEAT margin (roll - successChance) >= this => Catastrophe; otherwise Failure.")]
        [Range(0f, 1f)] public float catastropheMargin = 0.40f;

        [Header("Band reward multipliers (SEED). Index order: Flawless, Success, CloseCall, Failure, Catastrophe")]
        public float[] goldBandMultipliers = { 1.25f, 1.0f, 0.6f, 0.0f, 0.0f };
        public float[] xpBandMultipliers = { 1.25f, 1.0f, 0.75f, 0.25f, 0.0f };

        [Header("Injury chances per band (SEED — feel; severity-per-band is a RULE)")]
        [Tooltip("SEED: chance a clean Success victory still causes a Minor injury. Keep low/occasional so Success != CloseCall.")]
        [Range(0f, 1f)] public float successInjuryChance = 0.10f;
        [Tooltip("SEED: chance a CloseCall victory causes a Minor injury.")]
        [Range(0f, 1f)] public float closeCallInjuryChance = 0.75f;
        [Tooltip("SEED: chance a Failure causes a Major injury.")]
        [Range(0f, 1f)] public float failureInjuryChance = 1.0f;

        [Header("Death-save (FRAME: permadeath honest; SEED: chance)")]
        [Tooltip("SEED: base death-save success chance on Catastrophe (failed save = permadeath). Infirmary/Priest modifiers ADD to this later via a zero-sum hook.")]
        [Range(0f, 1f)] public float baseDeathSaveChance = 0.5f;

        [Header("Injury recovery (SEED — CANONICAL; supersedes the old 2h/6h/12h). No offline cap (D2).")]
        public float minorRecoveryMinutes = 30f;
        public float majorRecoveryMinutes = 120f;
        public float criticalRecoveryMinutes = 360f;

        [Header("D8 — Speed reduces expedition time (capped)")]
        [Tooltip("SEED: fraction of duration removed per point of summed team Speed.")]
        public float speedTimeReductionPerPoint = 0.0025f;
        [Tooltip("FRAME (D8 ~25-30%): hard cap on total time reduction regardless of Speed.")]
        [Range(0f, 1f)] public float speedTimeReductionCap = 0.30f;

        [Header("Reward model (SEED — feel; §7 says ~240/hr@10m -> ~120/hr@8h)")]
        [Tooltip("SEED: XP granted per hour of expedition duration on success.")]
        public float xpPerHour = 60f;
        [Tooltip("Expedition duration tiers (UI Quick/Short/Standard/Long/Overnight). gold/hr DECREASES with duration so active play is slightly favoured (§7).")]
        public List<ExpeditionTier> expeditionTiers = new List<ExpeditionTier>
        {
            new ExpeditionTier("Quick",     10,   240),
            new ExpeditionTier("Short",     60,   200),
            new ExpeditionTier("Standard", 240,   160),
            new ExpeditionTier("Long",     480,   120),
            new ExpeditionTier("Overnight",720,   100),
        };

        [Header("Roster (SEED — feel; cap is a frame-ish design number)")]
        [Tooltip("SEED: one-time gold granted to a brand-new guild so the first recruits are affordable.")]
        public long startingGold = 300;
        [Tooltip("SEED: gold cost to recruit a new blank-slate Gen-1 adventurer.")]
        public int recruitCost = 100;
        [Tooltip("SEED/FRAME: roster slots before any Barracks upgrades.")]
        public int baseRosterCap = 5;
        [Tooltip("SEED: extra roster slots granted per Barracks facility level.")]
        public int rosterSlotsPerBarracksLevel = 2;
        [Tooltip("Facility id whose level expands the roster cap.")]
        public string barracksFacilityId = "barracks";

        [Header("Leveling (SEED — feel; max level is a design FRAME)")]
        [Tooltip("FRAME: adventurers level 1..maxLevel (Task02 = 20).")]
        public int maxLevel = 20;
        [Tooltip("SEED: XP needed for level L = round(xpCurveBase * L^xpCurveExponent).")]
        public float xpCurveBase = 100f;
        public float xpCurveExponent = 1.5f;

        public float OfflineCapSeconds => offlineCapHours * 3600f;

        /// <summary>SEED curve: XP required to advance FROM <paramref name="level"/> to the next.</summary>
        public int XpToNext(int level)
        {
            if (level < 1) level = 1;
            return Mathf.RoundToInt(xpCurveBase * Mathf.Pow(level, xpCurveExponent));
        }

        public float GoldBandMultiplier(OutcomeBand band) => BandMul(goldBandMultipliers, band);
        public float XpBandMultiplier(OutcomeBand band) => BandMul(xpBandMultipliers, band);

        private static float BandMul(float[] arr, OutcomeBand band)
        {
            int i = (int)band;
            return (arr != null && i >= 0 && i < arr.Length) ? arr[i] : 0f;
        }

        public float RecoveryMinutes(InjurySeverity severity)
        {
            switch (severity)
            {
                case InjurySeverity.Minor: return minorRecoveryMinutes;
                case InjurySeverity.Major: return majorRecoveryMinutes;
                case InjurySeverity.Critical: return criticalRecoveryMinutes;
                default: return 0f;
            }
        }

        public long RecoverySeconds(InjurySeverity severity) => (long)(RecoveryMinutes(severity) * 60f);
    }

    /// <summary>One idle duration option and its gold/hr (CLAUDE.md §7, UI_SPEC §4.2).</summary>
    [Serializable]
    public class ExpeditionTier
    {
        public string displayName;
        public int durationMinutes;
        public float goldPerHour;

        public ExpeditionTier() { }

        public ExpeditionTier(string displayName, int durationMinutes, float goldPerHour)
        {
            this.displayName = displayName;
            this.durationMinutes = durationMinutes;
            this.goldPerHour = goldPerHour;
        }

        public long DurationSeconds => (long)durationMinutes * 60L;
    }
}
