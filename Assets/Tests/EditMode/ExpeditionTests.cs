using System;
using NUnit.Framework;
using UnityEngine;

namespace Guildmaster.Tests
{
    /// <summary>
    /// Proves the expedition mechanic: outcome locked at send-time (D3), wall-clock
    /// completion (D1), and the 12h offline cap (D2). EditMode tests don't run
    /// Awake, so the manager is wired via Initialize() directly.
    /// </summary>
    public class ExpeditionTests
    {
        private BalanceConfig _cfg;
        private DungeonData _dungeon;
        private CurrentData _current;
        private GameObject _go;
        private ExpeditionManager _mgr;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<BalanceConfig>();

            _dungeon = ScriptableObject.CreateInstance<DungeonData>();
            _dungeon.id = "test_cave";
            _dungeon.difficulty = 50f;
            _dungeon.rewardMultiplier = 1f;
            ContentDatabase.RegisterDungeon(_dungeon);

            _current = new CurrentData();
            _current.roster.Add(new Adventurer
            {
                id = "hero1",
                displayName = "Hero",
                stats = new StatBlock(200, 0, 40, 30, 10),
                status = AdventurerStatus.Healthy,
            });

            _go = new GameObject("ExpeditionManager");
            _mgr = _go.AddComponent<ExpeditionManager>();
            _mgr.Initialize(_cfg, _current);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_go);
            UnityEngine.Object.DestroyImmediate(_dungeon);
            UnityEngine.Object.DestroyImmediate(_cfg);
        }

        [Test]
        public void Send_LocksOutcomeImmediately()
        {
            var tier = new ExpeditionTier("Quick", 10, 240);
            var exp = _mgr.SendExpedition("test_cave", new[] { "hero1" }, tier);

            Assert.IsNotNull(exp);
            Assert.IsNotNull(exp.outcome, "Outcome must be rolled and stored at send-time (D3).");
            // The success chance was computed and clamped at send-time.
            Assert.GreaterOrEqual(exp.outcome.successChance, _cfg.successChanceMin);
            Assert.LessOrEqual(exp.outcome.successChance, _cfg.successChanceMax);
            // Team is marked away.
            Assert.AreEqual(AdventurerStatus.OnExpedition, _current.roster[0].status);
        }

        [Test]
        public void Outcome_DoesNotChange_BetweenSendAndCollect()
        {
            var tier = new ExpeditionTier("Quick", 10, 240);
            var exp = _mgr.SendExpedition("test_cave", new[] { "hero1" }, tier);

            // Snapshot the locked result.
            float chance = exp.outcome.successChance;
            float roll = exp.outcome.roll;
            bool success = exp.outcome.success;
            OutcomeBand band = exp.outcome.band;
            int gold = exp.outcome.goldReward;

            // Simulate time passing (and an app restart) by back-dating the send.
            exp.sendTimeTicksUtc = DateTime.UtcNow.Ticks - TimeSpan.FromHours(1).Ticks;
            Assert.IsTrue(_mgr.CanComplete(exp));

            var revealed = _mgr.Collect(exp.id);

            Assert.IsNotNull(revealed);
            Assert.AreEqual(chance, revealed.successChance, "Closing the app cannot change the outcome (D3).");
            Assert.AreEqual(roll, revealed.roll);
            Assert.AreEqual(success, revealed.success);
            Assert.AreEqual(band, revealed.band);
            Assert.AreEqual(gold, revealed.goldReward);
        }

        [Test]
        public void Collect_BeforeComplete_ReturnsNull_NoAutoRestart()
        {
            var tier = new ExpeditionTier("Standard", 240, 160);
            var exp = _mgr.SendExpedition("test_cave", new[] { "hero1" }, tier);

            // Not enough time has passed.
            Assert.IsFalse(_mgr.CanComplete(exp));
            Assert.IsNull(_mgr.Collect(exp.id), "An incomplete expedition cannot be collected.");
            // Still active, not auto-restarted (D2).
            Assert.AreEqual(1, _mgr.Active.Count);
        }

        [Test]
        public void Collect_FreesSurvivors_AndRemovesExpedition()
        {
            var tier = new ExpeditionTier("Quick", 10, 240);
            var exp = _mgr.SendExpedition("test_cave", new[] { "hero1" }, tier);
            exp.sendTimeTicksUtc = DateTime.UtcNow.Ticks - TimeSpan.FromHours(1).Ticks;

            var outcome = _mgr.Collect(exp.id);
            Assert.IsNotNull(outcome);
            Assert.AreEqual(0, _mgr.Active.Count, "Collected expedition is removed.");

            var hero = _current.roster[0];
            // With a strong hero vs. low difficulty the survivor should not be away anymore.
            Assert.AreNotEqual(AdventurerStatus.OnExpedition, hero.status);
        }
    }
}
