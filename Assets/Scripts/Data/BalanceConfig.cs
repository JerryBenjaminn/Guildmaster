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
        public float powerPerDefense = 1f;
        public float powerPerCrit = 1f;

        [Header("Success formula (SEED — feel)")]
        [Tooltip("SEED: success chance = 0.5 + (teamPower - difficulty) * sensitivity, then clamped to [min,max].")]
        public float powerToSuccessSensitivity = 0.01f;

        [Header("Outcome band thresholds (SEED — feel)")]
        [Tooltip("SEED: on a successful roll, margin above the success threshold beyond this fraction = Flawless, else Success.")]
        [Range(0f, 1f)] public float flawlessMargin = 0.5f;
        [Tooltip("SEED: on a failed roll, margin below the success threshold within this fraction = CloseCall, deeper = Failure; far below = Catastrophe.")]
        [Range(0f, 1f)] public float closeCallMargin = 0.15f;
        [Tooltip("SEED: failed roll this far (or more) below the threshold = Catastrophe.")]
        [Range(0f, 1f)] public float catastropheMargin = 0.6f;

        [Header("Injury / death-save (SEED — feel; permadeath logic is a FRAME)")]
        [Tooltip("SEED: base chance a member is injured on a CloseCall outcome.")]
        [Range(0f, 1f)] public float injuryChanceCloseCall = 0.5f;
        [Tooltip("SEED: base chance a member is injured on a Failure outcome.")]
        [Range(0f, 1f)] public float injuryChanceFailure = 0.75f;
        [Tooltip("SEED: base death-save success chance on a Catastrophe (failed save = permadeath).")]
        [Range(0f, 1f)] public float baseDeathSaveChance = 0.5f;

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

        public float OfflineCapSeconds => offlineCapHours * 3600f;
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
