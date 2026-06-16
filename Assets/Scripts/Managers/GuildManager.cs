using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Owns guild-level progress backed by persistent.json: gold (split into the
    /// at-risk pool, D4), facility levels, rank, talents, hybrid unlocks.
    /// Operates on a <see cref="PersistentData"/> slice bound by GameManager;
    /// GameManager performs the file IO.
    ///
    /// Public API:
    ///   Bind(data)                       - attach the persistent data slice.
    ///   TotalGold                        - protected + at-risk gold.
    ///   AddGold(amount)                  - earned gold flows into the at-risk pool (D4).
    ///   TrySpendGold(amount)             - spend (at-risk first, then protected); false if too poor.
    ///   ApplyDeathGoldLoss()             - D4: lose a % of at-risk, bank the rest; returns lost amount.
    ///   GetFacilityLevel / SetFacilityLevel
    ///   AddTalentPoints / TrySpendTalentPoints
    /// </summary>
    public class GuildManager : Singleton<GuildManager>
    {
        private PersistentData _data;
        public PersistentData Data => _data;

        public void Bind(PersistentData data)
        {
            _data = data;
        }

        public long TotalGold => _data.protectedGold + _data.atRiskGold;

        /// <summary>Earned gold goes into the at-risk pool (the slice exposed to death loss, D4).</summary>
        public void AddGold(long amount)
        {
            if (amount <= 0) return;
            _data.atRiskGold += amount;
        }

        public bool TrySpendGold(long amount)
        {
            if (amount <= 0) return true;
            if (TotalGold < amount) return false;

            // Draw from at-risk first so savings (protected) are spent last.
            long fromAtRisk = System.Math.Min(_data.atRiskGold, amount);
            _data.atRiskGold -= fromAtRisk;
            long remainder = amount - fromAtRisk;
            _data.protectedGold -= remainder;
            return true;
        }

        /// <summary>
        /// D4: on death, lose a configured % of the at-risk pool; whatever survives
        /// is banked into protected savings and the at-risk pool resets to 0.
        /// Long-term savings are never touched. Returns the gold lost.
        /// </summary>
        public long ApplyDeathGoldLoss(BalanceConfig cfg)
        {
            long lost = LegacyMath.GoldLostOnDeath(_data.atRiskGold, cfg.goldAtRiskLossPercent);
            long survived = _data.atRiskGold - lost;
            _data.protectedGold += survived;
            _data.atRiskGold = 0;
            return lost;
        }

        // ---- Facilities ----

        public int GetFacilityLevel(string facilityId)
        {
            var state = _data.facilities.Find(f => f.facilityId == facilityId);
            return state != null ? state.level : 0;
        }

        public void SetFacilityLevel(string facilityId, int level)
        {
            var state = _data.facilities.Find(f => f.facilityId == facilityId);
            if (state == null)
            {
                state = new FacilityState { facilityId = facilityId, level = 0 };
                _data.facilities.Add(state);
            }
            state.level = level;
        }

        // ---- Talents (point economy — flat list MVP) ----

        public int TalentPoints => _data.talentPoints;
        public void AddTalentPoints(int points) { if (points > 0) _data.talentPoints += points; }

        public bool TrySpendTalentPoints(int points)
        {
            if (points <= 0) return true;
            if (_data.talentPoints < points) return false;
            _data.talentPoints -= points;
            return true;
        }

        // ---- Rank ----

        public int Rank => _data.guildRank;
        public string GuildName => _data.guildName;
    }
}
