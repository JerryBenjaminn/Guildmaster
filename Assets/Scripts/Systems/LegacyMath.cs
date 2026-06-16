using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Pure inheritance math (CLAUDE.md D4, D7). Separated from LegacyManager so
    /// the anti-snowball frames are unit-testable in isolation. These are FRAMES
    /// (clamps/caps Jerry set) — enforced, not feel decisions.
    /// </summary>
    public static class LegacyMath
    {
        /// <summary>D7: inherited item scales to ~itemInheritScale of original level (min 1).</summary>
        public static int ScaleInheritedItemLevel(int originalLevel, float inheritScale)
        {
            int scaled = Mathf.RoundToInt(originalLevel * inheritScale);
            return Mathf.Max(1, scaled);
        }

        /// <summary>D7: accumulate a bloodline bonus, capped per stat (never exceeds the cap).</summary>
        public static float AccumulateBloodlineBonus(float current, float add, float capPerStat)
        {
            return Mathf.Min(current + add, capPerStat);
        }

        /// <summary>
        /// D4: amount lost from the at-risk pool on death (a % of gold earned
        /// since the last death). Returns the gold lost; clamped to the pool.
        /// </summary>
        public static long GoldLostOnDeath(long atRiskGold, float lossPercent)
        {
            if (atRiskGold <= 0) return 0;
            long loss = (long)System.Math.Round(atRiskGold * (double)Mathf.Clamp01(lossPercent));
            return System.Math.Min(loss, atRiskGold);
        }
    }
}
