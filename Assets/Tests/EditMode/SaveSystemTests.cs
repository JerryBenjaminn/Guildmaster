using NUnit.Framework;

namespace Guildmaster.Tests
{
    /// <summary>
    /// Proves the save system round-trips through the filesystem (CLAUDE.md §3.5).
    /// Writing then reading from Application.persistentDataPath is exactly what
    /// happens across an app restart, so a passing round-trip demonstrates the
    /// save survives an app kill. Uses test-only filenames so real saves are safe.
    /// </summary>
    public class SaveSystemTests
    {
        private const string PFile = "test_persistent.json";
        private const string LFile = "test_legacy.json";
        private const string CFile = "test_current.json";

        [TearDown]
        public void Cleanup()
        {
            SaveSystem.Delete(PFile);
            SaveSystem.Delete(LFile);
            SaveSystem.Delete(CFile);
        }

        [Test]
        public void Persistent_RoundTrips()
        {
            var data = new PersistentData
            {
                guildName = "Ironhold",
                guildRank = 3,
                protectedGold = 5000,
                atRiskGold = 1200,
                talentPoints = 7,
            };
            data.facilities.Add(new FacilityState { facilityId = "infirmary", level = 2 });
            data.unlockedHybridClassIds.Add("battlemage");

            SaveSystem.Save(PFile, data);
            var loaded = SaveSystem.Load<PersistentData>(PFile);

            Assert.AreEqual("Ironhold", loaded.guildName);
            Assert.AreEqual(3, loaded.guildRank);
            Assert.AreEqual(5000, loaded.protectedGold);
            Assert.AreEqual(1200, loaded.atRiskGold);
            Assert.AreEqual(7, loaded.talentPoints);
            Assert.AreEqual(1, loaded.facilities.Count);
            Assert.AreEqual("infirmary", loaded.facilities[0].facilityId);
            Assert.AreEqual(2, loaded.facilities[0].level);
            Assert.Contains("battlemage", loaded.unlockedHybridClassIds);
        }

        [Test]
        public void Current_RoundTrips_WithExpeditionOutcome()
        {
            var data = new CurrentData { lastSeenTicksUtc = 123456789 };
            data.roster.Add(new Adventurer
            {
                id = "a1",
                displayName = "Bron",
                classId = "warrior",
                level = 4,
                stats = new StatBlock(140, 0, 18, 0, 16, 0, 8),
                status = AdventurerStatus.OnExpedition,
            });
            var exp = new Expedition
            {
                id = "e1",
                dungeonId = "cave",
                durationSeconds = 600,
                sendTimeTicksUtc = 999,
            };
            exp.teamMemberIds.Add("a1");
            exp.outcome.success = true;
            exp.outcome.band = OutcomeBand.Flawless;
            exp.outcome.goldReward = 240;
            data.activeExpeditions.Add(exp);

            SaveSystem.Save(CFile, data);
            var loaded = SaveSystem.Load<CurrentData>(CFile);

            Assert.AreEqual(123456789, loaded.lastSeenTicksUtc);
            Assert.AreEqual(1, loaded.roster.Count);
            Assert.AreEqual("Bron", loaded.roster[0].displayName);
            Assert.AreEqual(18, loaded.roster[0].stats.attack);
            Assert.AreEqual(1, loaded.activeExpeditions.Count);
            Assert.AreEqual(OutcomeBand.Flawless, loaded.activeExpeditions[0].outcome.band);
            Assert.AreEqual(240, loaded.activeExpeditions[0].outcome.goldReward);
            Assert.AreEqual("a1", loaded.activeExpeditions[0].teamMemberIds[0]);
        }

        [Test]
        public void Load_Missing_ReturnsFreshDefault()
        {
            SaveSystem.Delete(LFile);
            var loaded = SaveSystem.Load<LegacyData>(LFile);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.currentGeneration);
            Assert.IsNotNull(loaded.lineage);
        }
    }
}
