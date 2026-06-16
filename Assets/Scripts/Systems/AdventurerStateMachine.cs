namespace Guildmaster
{
    /// <summary>
    /// Pure rules for valid adventurer status transitions (Task02 §2). Kept
    /// separate from AdventurerManager so the state machine is unit-testable in
    /// isolation. The key invariant the task calls out: an injured/dead/away
    /// adventurer cannot be deployed — only a Healthy one can (see CanDeploy).
    /// </summary>
    public static class AdventurerStateMachine
    {
        public static bool CanDeploy(AdventurerStatus status) => status == AdventurerStatus.Healthy;

        /// <summary>True if moving from <paramref name="from"/> to <paramref name="to"/> is allowed.</summary>
        public static bool IsValidTransition(AdventurerStatus from, AdventurerStatus to)
        {
            if (from == to) return true; // idempotent no-op (e.g. re-flagging severity)

            switch (from)
            {
                case AdventurerStatus.Healthy:
                    return to == AdventurerStatus.OnExpedition
                        || to == AdventurerStatus.Training
                        || to == AdventurerStatus.Injured
                        || to == AdventurerStatus.Retired
                        || to == AdventurerStatus.DeadPending;

                case AdventurerStatus.OnExpedition:
                    // Resolved when the expedition is collected.
                    return to == AdventurerStatus.Healthy
                        || to == AdventurerStatus.Injured
                        || to == AdventurerStatus.DeadPending;

                case AdventurerStatus.Training:
                    return to == AdventurerStatus.Healthy
                        || to == AdventurerStatus.Retired;

                case AdventurerStatus.Injured:
                    return to == AdventurerStatus.Healthy   // recovered
                        || to == AdventurerStatus.Retired
                        || to == AdventurerStatus.DeadPending;

                case AdventurerStatus.DeadPending:
                    return to == AdventurerStatus.Dead;     // after legacy is processed

                case AdventurerStatus.Retired:
                case AdventurerStatus.Dead:
                    return false;                            // terminal

                default:
                    return false;
            }
        }
    }
}
