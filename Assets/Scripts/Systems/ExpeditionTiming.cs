using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// D8 — Speed reduces an expedition's real-time duration, hard-capped. Pure and
    /// testable so the resolve, the debug hook, and (later) the Expedition Prep
    /// duration preview all share one formula. Speed affects TIME ONLY, never the
    /// combat outcome (powerPerSpeed stays 0).
    /// </summary>
    public static class ExpeditionTiming
    {
        /// <summary>Fraction of duration removed for a given summed team Speed, clamped to the cap.</summary>
        public static float SpeedReduction(int totalSpeed, BalanceConfig cfg)
        {
            if (totalSpeed <= 0) return 0f;
            float raw = totalSpeed * cfg.speedTimeReductionPerPoint;
            return Mathf.Clamp(raw, 0f, cfg.speedTimeReductionCap);
        }

        /// <summary>Effective duration after the Speed reduction (never below zero).</summary>
        public static long EffectiveDurationSeconds(long baseDurationSeconds, int totalSpeed, BalanceConfig cfg)
        {
            float reduction = SpeedReduction(totalSpeed, cfg);
            long result = (long)Mathf.Round(baseDurationSeconds * (1f - reduction));
            return result < 0 ? 0 : result;
        }
    }
}
