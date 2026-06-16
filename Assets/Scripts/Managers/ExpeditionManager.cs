using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// The heart of the loop (CLAUDE.md §2, §5). Sends timed expeditions whose
    /// OUTCOME IS ROLLED AND LOCKED AT SEND-TIME (D3), stored hidden, and revealed
    /// only when the real-time timer completes. Progress is wall-clock based, so it
    /// accrues while the app is closed (D1) and cannot be save-scummed (D3).
    /// Completed expeditions wait for collection; they do NOT auto-restart (D2).
    ///
    /// Public API:
    ///   Initialize(balance, current)              - inject deps (also used by tests).
    ///   Active                                     - read-only active expeditions.
    ///   CanComplete(exp) / SecondsRemaining(exp)
    ///   SendExpedition(dungeonId, teamIds, tier)   - rolls & locks outcome, returns the Expedition.
    ///   Collect(expeditionId)                      - reveals outcome, applies rewards/injuries/deaths.
    ///   OnExpeditionCollected                      - event fired on collect (UI/legacy hook).
    /// </summary>
    public partial class ExpeditionManager : Singleton<ExpeditionManager>
    {
        private BalanceConfig _balance;
        private CurrentData _current;

        /// <summary>Fired when an expedition is collected, with its revealed outcome.</summary>
        public event Action<Expedition, ExpeditionOutcome> OnExpeditionCollected;

        public void Initialize(BalanceConfig balance, CurrentData current)
        {
            _balance = balance;
            _current = current;
        }

        public IReadOnlyList<Expedition> Active => _current.activeExpeditions;

        public Expedition GetById(string id) => _current.activeExpeditions.Find(e => e.id == id);

        public bool CanComplete(Expedition exp) =>
            OfflineCalculator.IsComplete(exp, DateTime.UtcNow.Ticks);

        public double SecondsRemaining(Expedition exp) =>
            OfflineCalculator.SecondsRemaining(exp, DateTime.UtcNow.Ticks);

        /// <summary>
        /// Send a team on an expedition. The outcome is computed and stored NOW
        /// (D3) using a seeded RNG; the same stored result is what's revealed later.
        /// </summary>
        public Expedition SendExpedition(string dungeonId, IReadOnlyList<string> teamMemberIds, ExpeditionTier tier)
        {
            var dungeon = ContentDatabase.GetDungeon(dungeonId);
            if (dungeon == null)
            {
                Debug.LogError($"[ExpeditionManager] Unknown dungeon '{dungeonId}'.");
                return null;
            }
            if (teamMemberIds == null || teamMemberIds.Count == 0)
            {
                Debug.LogError("[ExpeditionManager] Cannot send an empty team.");
                return null;
            }

            // Deploy guard (Task02 §2): only Healthy adventurers can be sent.
            foreach (var id in teamMemberIds)
            {
                var a = _current.roster.Find(r => r.id == id);
                if (a == null)
                {
                    Debug.LogError($"[ExpeditionManager] Team member '{id}' not in roster.");
                    return null;
                }
                if (!AdventurerStateMachine.CanDeploy(a.status))
                {
                    Debug.LogError($"[ExpeditionManager] '{a.displayName}' cannot deploy (status {a.status}).");
                    return null;
                }
            }

            float teamPower = ComputeTeamPower(teamMemberIds);

            // Seeded RNG so the locked roll is reproducible and self-contained.
            int seed = unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode());
            var rng = new System.Random(seed);

            // Death-save survival modifier (Infirmary/Priest) is a zero-sum hook
            // until its task; passes 0 today.
            var outcome = CombatResolver.Resolve(teamPower, dungeon, tier, _balance, teamMemberIds, rng, 0f);

            // D8: team Speed shortens the real-time duration, hard-capped. Computed
            // at send-time; the locked outcome (D3) is unaffected by Speed.
            long baseDuration = tier != null ? tier.DurationSeconds : 0;
            int teamSpeed = ComputeTeamSpeed(teamMemberIds);
            long effectiveDuration = ExpeditionTiming.EffectiveDurationSeconds(baseDuration, teamSpeed, _balance);

            var expedition = new Expedition
            {
                id = Guid.NewGuid().ToString("N"),
                dungeonId = dungeonId,
                teamMemberIds = new List<string>(teamMemberIds),
                sendTimeTicksUtc = DateTime.UtcNow.Ticks,
                durationSeconds = effectiveDuration,
                collected = false,
                outcome = outcome,
            };

            _current.activeExpeditions.Add(expedition);

            // Mark the team as away.
            foreach (var id in teamMemberIds)
            {
                var a = _current.roster.Find(r => r.id == id);
                if (a != null) a.status = AdventurerStatus.OnExpedition;
            }

            return expedition;
        }

        /// <summary>
        /// Reveal and apply a completed expedition. Returns null if it isn't ready
        /// or was already collected. Applies gold/XP, sets injured/dead statuses
        /// (dead -> DeadPending for the legacy/class-choice moment, never auto-
        /// resolved per UI_SPEC §4.3), then removes the expedition.
        /// </summary>
        public ExpeditionOutcome Collect(string expeditionId)
        {
            var exp = GetById(expeditionId);
            if (exp == null || exp.collected) return null;
            if (!CanComplete(exp)) return null;

            var outcome = exp.outcome; // already locked at send-time
            exp.collected = true;

            if (outcome.success && GuildManager.Instance != null)
            {
                GuildManager.Instance.AddGold(outcome.goldReward);
            }

            var adv = AdventurerManager.Instance;
            foreach (var id in exp.teamMemberIds)
            {
                var a = _current.roster.Find(r => r.id == id);
                if (a == null) continue;

                if (outcome.deadMemberIds.Contains(id))
                {
                    a.status = AdventurerStatus.DeadPending; // awaits legacy/class choice
                    continue;                                // the dead don't gain XP
                }

                var injury = outcome.injuries.Find(m => m.memberId == id);
                if (injury != null) ApplyInjury(a, injury.severity);
                else
                {
                    a.status = AdventurerStatus.Healthy;
                    a.injurySeverity = InjurySeverity.None;
                    a.injuryHealAtTicksUtc = 0;
                }

                // XP is band-scaled (even a Failure grants a little); grant to all
                // survivors, injured or not.
                GrantXp(adv, a, outcome.xpReward);
            }

            _current.activeExpeditions.Remove(exp);
            OnExpeditionCollected?.Invoke(exp, outcome);
            return outcome;
        }

        // Route XP through AdventurerManager so level-ups happen; fall back to a
        // direct add when no manager is present (e.g. isolated unit tests).
        private static void GrantXp(AdventurerManager adv, Adventurer a, int xp)
        {
            if (xp <= 0) return;
            if (adv != null) adv.AddXp(a, xp);
            else a.xp += xp;
        }

        private float ComputeTeamPower(IReadOnlyList<string> teamMemberIds)
        {
            float total = 0f;
            foreach (var id in teamMemberIds)
            {
                var a = _current.roster.Find(r => r.id == id);
                if (a != null) total += CombatResolver.Power(a.stats, _balance);
            }
            return total;
        }

        private int ComputeTeamSpeed(IReadOnlyList<string> teamMemberIds)
        {
            int total = 0;
            foreach (var id in teamMemberIds)
            {
                var a = _current.roster.Find(r => r.id == id);
                if (a != null) total += a.stats.speed;
            }
            return total;
        }

        // Apply an injury with its severity and a real-time recovery deadline
        // (no offline cap, D2). Recovery is processed by AdventurerManager.
        private void ApplyInjury(Adventurer a, InjurySeverity severity)
        {
            a.status = AdventurerStatus.Injured;
            a.injurySeverity = severity;
            long recoverySeconds = _balance != null ? _balance.RecoverySeconds(severity) : 0;
            a.injuryHealAtTicksUtc = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(recoverySeconds).Ticks;
        }
    }
}
