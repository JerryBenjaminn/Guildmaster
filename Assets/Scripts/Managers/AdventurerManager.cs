using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Owns the roster of living adventurers (current.json slice). Operates on a
    /// <see cref="CurrentData"/> bound by GameManager; GameManager does file IO.
    ///
    /// Public API:
    ///   Bind(data)                       - attach the current data slice.
    ///   Roster                           - read-only list of adventurers.
    ///   GetById(id)
    ///   Add(adventurer) / Remove(id)
    ///   SetStatus(id, status)
    ///   CreateAdventurer(classId, gen)   - build a roster member from class data.
    ///   CreateStub(name)                 - throwaway member for wiring tests (no class asset needed).
    /// </summary>
    public class AdventurerManager : Singleton<AdventurerManager>
    {
        private CurrentData _data;

        public void Bind(CurrentData data)
        {
            _data = data;
        }

        public IReadOnlyList<Adventurer> Roster => _data.roster;

        public Adventurer GetById(string id)
        {
            return _data.roster.Find(a => a.id == id);
        }

        public void Add(Adventurer adventurer)
        {
            if (adventurer == null) return;
            _data.roster.Add(adventurer);
        }

        public bool Remove(string id)
        {
            int index = _data.roster.FindIndex(a => a.id == id);
            if (index < 0) return false;
            _data.roster.RemoveAt(index);
            return true;
        }

        public void SetStatus(string id, AdventurerStatus status)
        {
            var a = GetById(id);
            if (a != null) a.status = status;
        }

        public int CountHealthy()
        {
            if (_data == null) return 0;
            int n = 0;
            foreach (var a in _data.roster) if (a.status == AdventurerStatus.Healthy) n++;
            return n;
        }

        /// <summary>Create a roster member from an authored class asset.</summary>
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
                stats = classData != null ? classData.StatsAtLevel(1) : new StatBlock(100, 0, 10, 10, 5),
            };
            _data.roster.Add(adventurer);
            return adventurer;
        }

        /// <summary>
        /// Throwaway adventurer for proving wiring without any class asset
        /// (CLAUDE.md §9 allows 1-2 stubs for testing). Not for shipping content.
        /// </summary>
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
    }
}
