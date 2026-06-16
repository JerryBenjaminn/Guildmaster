using System;

namespace Guildmaster
{
    /// <summary>
    /// Offline-time math (CLAUDE.md D1/D2). Time is tracked in absolute UTC ticks
    /// so it is honest across app kills and clock-independent of Time.time.
    ///
    /// Two distinct rules:
    ///  - Expeditions resolve by HONEST WALL-CLOCK (sendTime + duration). The
    ///    outcome is already locked (D3), so no save-scum is possible; completed
    ///    expeditions simply wait for collection (they do NOT auto-restart).
    ///  - CONTINUOUS accrual (training XP, future systems) is capped at the 12h
    ///    offline cap (D2). Injury recovery has NO cap.
    /// </summary>
    public static class OfflineCalculator
    {
        /// <summary>Elapsed seconds between two UTC tick stamps, capped (for continuous accrual).</summary>
        public static double CappedElapsedSeconds(long lastSeenTicksUtc, long nowTicksUtc, float capSeconds)
        {
            double elapsed = UncappedElapsedSeconds(lastSeenTicksUtc, nowTicksUtc);
            return Math.Min(elapsed, capSeconds);
        }

        public static double UncappedElapsedSeconds(long lastSeenTicksUtc, long nowTicksUtc)
        {
            if (lastSeenTicksUtc <= 0) return 0;
            long deltaTicks = nowTicksUtc - lastSeenTicksUtc;
            if (deltaTicks <= 0) return 0;
            return TimeSpan.FromTicks(deltaTicks).TotalSeconds;
        }

        public static bool IsComplete(Expedition exp, long nowTicksUtc)
        {
            return nowTicksUtc >= exp.CompleteTimeTicksUtc;
        }

        /// <summary>Seconds left until completion (0 if already complete).</summary>
        public static double SecondsRemaining(Expedition exp, long nowTicksUtc)
        {
            long remainingTicks = exp.CompleteTimeTicksUtc - nowTicksUtc;
            if (remainingTicks <= 0) return 0;
            return TimeSpan.FromTicks(remainingTicks).TotalSeconds;
        }
    }
}
