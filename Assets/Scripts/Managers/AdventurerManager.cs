using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Owns the roster of adventurers (current.json slice). Recruit/legacy creation,
    /// leveling, status transitions (enforced via <see cref="AdventurerStateMachine"/>),
    /// stat derivation (via <see cref="StatCalculator"/>), and roster-cap enforcement.
    /// Operates on a <see cref="CurrentData"/> bound by GameManager; GameManager does
    /// file IO.
    ///
    /// Public API:
    ///   Initialize(balance, current)         - inject deps (also used by tests).
    ///   Roster / GetById / Remove
    ///   LivingCount / CountByStatus / CountHealthy
    ///   RosterCap / HasFreeSlot
    ///   TryRecruit()                          - gold-cost + cap-checked Gen-1 recruit.
    ///   CreateAdventurer(classId, gen, family)/ CreateHeir(...) - creation paths.
    ///   AddXp(adv, amount)                    - XP -> level-up -> recalc -> slot unlocks.
    ///   TrySetStatus(id, to) / SetInjured / Retire / CanDeploy / Rename
    ///   AbilitySlotCount(adv) / RecalculateStats(adv) / RecalculateAll()
    /// </summary>
    public class AdventurerManager : Singleton<AdventurerManager>
    {
        private BalanceConfig _balance;
        private CurrentData _data;

        public void Initialize(BalanceConfig balance, CurrentData data)
        {
            _balance = balance;
            _data = data;
        }

        public IReadOnlyList<Adventurer> Roster => _data.roster;

        public Adventurer GetById(string id) => _data.roster.Find(a => a.id == id);

        public bool Remove(string id)
        {
            int index = _data.roster.FindIndex(a => a.id == id);
            if (index < 0) return false;
            _data.roster.RemoveAt(index);
            return true;
        }

        // ---- Counts & capacity ----

        /// <summary>Adventurers occupying a roster slot (excludes Dead and Retired).</summary>
        public int LivingCount()
        {
            if (_data == null) return 0;
            int n = 0;
            foreach (var a in _data.roster)
            {
                if (a.status != AdventurerStatus.Dead && a.status != AdventurerStatus.Retired) n++;
            }
            return n;
        }

        public int CountByStatus(AdventurerStatus status)
        {
            if (_data == null) return 0;
            int n = 0;
            foreach (var a in _data.roster) if (a.status == status) n++;
            return n;
        }

        public int CountHealthy() => CountByStatus(AdventurerStatus.Healthy);

        /// <summary>Roster cap = base + Barracks level * per-level slots (BalanceConfig).</summary>
        public int RosterCap()
        {
            int cap = _balance != null ? _balance.baseRosterCap : 0;
            var guild = GuildManager.Instance;
            if (guild != null && _balance != null)
            {
                int barracks = guild.GetFacilityLevel(_balance.barracksFacilityId);
                cap += barracks * _balance.rosterSlotsPerBarracksLevel;
            }
            return cap;
        }

        public bool HasFreeSlot() => LivingCount() < RosterCap();

        // ---- Creation paths ----

        /// <summary>Gold-cost, cap-checked recruitment of a Gen-1 blank-slate "Adventurer".</summary>
        public Adventurer TryRecruit()
        {
            if (!HasFreeSlot())
            {
                Debug.LogWarning("[AdventurerManager] Roster is full; recruit blocked.");
                return null;
            }

            var guild = GuildManager.Instance;
            if (guild != null && _balance != null && !guild.TrySpendGold(_balance.recruitCost))
            {
                Debug.LogWarning("[AdventurerManager] Not enough gold to recruit.");
                return null;
            }

            int n = _data.roster.Count + 1;
            return CreateAdventurer("adventurer", 1, $"Recruit {n}");
        }

        /// <summary>Create a roster member from an authored class asset and recalc its stats.</summary>
        public Adventurer CreateAdventurer(string classId, int generation, string familyName)
        {
            var classData = ContentDatabase.GetClass(classId);
            var adventurer = new Adventurer
            {
                id = System.Guid.NewGuid().ToString("N"),
                classId = classId,
                displayName = classData != null ? classData.displayName : classId,
                level = 1,
                generation = generation,
                familyName = familyName,
                status = AdventurerStatus.Healthy,
            };
            RecalculateStats(adventurer);
            _data.roster.Add(adventurer);
            return adventurer;
        }

        /// <summary>
        /// Legacy creation path (heir). Stub-callable now; task 04 wires the full
        /// legacy/class-choice flow. Bloodline bonuses (if any) apply via recalc.
        /// </summary>
        public Adventurer CreateHeir(string classId, string familyName)
        {
            int gen = LegacyManager.Instance != null ? LegacyManager.Instance.CurrentGeneration : 1;
            return CreateAdventurer(classId, gen, familyName);
        }

        /// <summary>Throwaway adventurer for wiring tests (no class asset needed).</summary>
        public Adventurer CreateStub(string displayName, StatBlock stats)
        {
            var adventurer = new Adventurer
            {
                id = System.Guid.NewGuid().ToString("N"),
                classId = "stub",
                displayName = displayName,
                level = 1,
                generation = 1,
                familyName = "Testfolk",
                status = AdventurerStatus.Healthy,
                stats = stats,
            };
            _data.roster.Add(adventurer);
            return adventurer;
        }

        // ---- Leveling ----

        /// <summary>
        /// Grant XP and roll up any level-ups (capped at BalanceConfig.maxLevel).
        /// Stats are recomputed after; ability-slot capacity follows from the new
        /// level automatically (see AbilitySlotCount).
        /// </summary>
        public void AddXp(Adventurer adventurer, int amount)
        {
            if (adventurer == null || amount <= 0 || _balance == null) return;

            adventurer.xp += amount;

            bool leveled = false;
            while (adventurer.level < _balance.maxLevel)
            {
                int needed = _balance.XpToNext(adventurer.level);
                if (adventurer.xp < needed) break;
                adventurer.xp -= needed;
                adventurer.level++;
                leveled = true;
            }

            // At max level, XP no longer banks toward a next level.
            if (adventurer.level >= _balance.maxLevel) adventurer.xp = 0;

            if (leveled) RecalculateStats(adventurer);
        }

        // ---- Status transitions (enforced) ----

        public bool CanDeploy(string id)
        {
            var a = GetById(id);
            return a != null && AdventurerStateMachine.CanDeploy(a.status);
        }

        public bool TrySetStatus(string id, AdventurerStatus to)
        {
            var a = GetById(id);
            if (a == null) return false;
            if (!AdventurerStateMachine.IsValidTransition(a.status, to)) return false;
            a.status = to;
            if (to != AdventurerStatus.Injured) a.injurySeverity = InjurySeverity.None;
            return true;
        }

        public bool SetInjured(string id, InjurySeverity severity)
        {
            var a = GetById(id);
            if (a == null) return false;
            if (!AdventurerStateMachine.IsValidTransition(a.status, AdventurerStatus.Injured)) return false;
            a.status = AdventurerStatus.Injured;
            a.injurySeverity = severity == InjurySeverity.None ? InjurySeverity.Minor : severity;
            return true;
        }

        public bool Retire(string id) => TrySetStatus(id, AdventurerStatus.Retired);

        public void Rename(string id, string newName)
        {
            var a = GetById(id);
            if (a != null && !string.IsNullOrWhiteSpace(newName)) a.displayName = newName.Trim();
        }

        // ---- Stats & slots ----

        public int AbilitySlotCount(Adventurer adventurer)
        {
            var cls = ContentDatabase.GetClass(adventurer.classId);
            return cls != null ? cls.AbilitySlotsAtLevel(adventurer.level) : 0;
        }

        /// <summary>Recompute derived stats for one adventurer (class@level + bloodline + gear).</summary>
        public void RecalculateStats(Adventurer adventurer)
        {
            if (adventurer == null) return;
            var cls = ContentDatabase.GetClass(adventurer.classId);

            IReadOnlyList<BloodlineBonus> bloodline = null;
            var legacy = LegacyManager.Instance;
            if (legacy != null) bloodline = legacy.BloodlineBonuses;

            adventurer.stats = StatCalculator.Derive(
                cls, adventurer.level, bloodline, StatCalculator.GearFlatStats(adventurer));
        }

        public void RecalculateAll()
        {
            if (_data == null) return;
            foreach (var a in _data.roster) RecalculateStats(a);
        }

        // ---- Injury recovery (real-time, no offline cap — D2) ----

        /// <summary>
        /// Heal any injured adventurer whose recovery deadline has passed (wall-clock,
        /// so offline recoveries apply on load). Returns the number recovered so the
        /// caller can refresh UI only when something changed. Poll-light: a linear
        /// scan with no allocations.
        /// </summary>
        public int ProcessInjuryRecovery(long nowTicksUtc)
        {
            if (_data == null) return 0;
            int recovered = 0;
            foreach (var a in _data.roster)
            {
                if (a.status != AdventurerStatus.Injured) continue;
                if (a.injuryHealAtTicksUtc <= 0 || nowTicksUtc < a.injuryHealAtTicksUtc) continue;

                a.status = AdventurerStatus.Healthy;
                a.injurySeverity = InjurySeverity.None;
                a.injuryHealAtTicksUtc = 0;
                recovered++;
            }
            return recovered;
        }

        /// <summary>Seconds until an injured adventurer recovers (0 if not injured / already due).</summary>
        public double RecoverySecondsRemaining(Adventurer a, long nowTicksUtc)
        {
            if (a == null || a.status != AdventurerStatus.Injured || a.injuryHealAtTicksUtc <= 0) return 0;
            long remaining = a.injuryHealAtTicksUtc - nowTicksUtc;
            return remaining <= 0 ? 0 : System.TimeSpan.FromTicks(remaining).TotalSeconds;
        }
    }
}
