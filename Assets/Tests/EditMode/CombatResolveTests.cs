using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Guildmaster.Tests
{
    /// <summary>
    /// Task03 coverage: outcome-band junction (victory/defeat), band→severity,
    /// death-save, band-scaled rewards, Resolve wiring, and D8 timing.
    /// </summary>
    public class CombatResolveTests
    {
        private BalanceConfig Cfg() => ScriptableObject.CreateInstance<BalanceConfig>();

        // ---- Band junction ----

        [Test]
        public void BandFor_VictoryAndDefeatSplit()
        {
            var cfg = Cfg(); // flawless 0.30, closeCall 0.10, catastrophe 0.40
            // Victory (roll < chance), by margin of victory.
            Assert.AreEqual(OutcomeBand.Flawless, CombatResolver.BandFor(0.8f, 0.10f, cfg)); // margin .70
            Assert.AreEqual(OutcomeBand.Success, CombatResolver.BandFor(0.8f, 0.55f, cfg));  // margin .25
            Assert.AreEqual(OutcomeBand.CloseCall, CombatResolver.BandFor(0.8f, 0.75f, cfg)); // margin .05
            // Defeat (roll >= chance), by margin of failure.
            Assert.AreEqual(OutcomeBand.Failure, CombatResolver.BandFor(0.5f, 0.70f, cfg));     // miss .20
            Assert.AreEqual(OutcomeBand.Catastrophe, CombatResolver.BandFor(0.5f, 0.95f, cfg)); // miss .45
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void SeverityForBand_MatchesApprovedMapping()
        {
            Assert.AreEqual(InjurySeverity.None, CombatResolver.SeverityForBand(OutcomeBand.Flawless));
            Assert.AreEqual(InjurySeverity.Minor, CombatResolver.SeverityForBand(OutcomeBand.Success));
            Assert.AreEqual(InjurySeverity.Minor, CombatResolver.SeverityForBand(OutcomeBand.CloseCall));
            Assert.AreEqual(InjurySeverity.Major, CombatResolver.SeverityForBand(OutcomeBand.Failure));
            Assert.AreEqual(InjurySeverity.Critical, CombatResolver.SeverityForBand(OutcomeBand.Catastrophe));
        }

        [Test]
        public void SurvivesDeathSave_RespectsChanceAndModifier()
        {
            Assert.IsTrue(CombatResolver.SurvivesDeathSave(0.40, 0.5f, 0f));
            Assert.IsFalse(CombatResolver.SurvivesDeathSave(0.60, 0.5f, 0f));
            // Survival modifier (Infirmary/Priest hook) raises the bar.
            Assert.IsTrue(CombatResolver.SurvivesDeathSave(0.70, 0.5f, 0.3f)); // chance 0.8
            // Clamped to [0,1].
            Assert.IsTrue(CombatResolver.SurvivesDeathSave(0.99, 0.9f, 0.5f));  // clamps to 1.0
            Assert.IsFalse(CombatResolver.SurvivesDeathSave(0.01, 0.0f, -1f));  // clamps to 0.0
        }

        // ---- Rewards ----

        [Test]
        public void BandRewardMultipliers_SeedValues()
        {
            var cfg = Cfg();
            Assert.AreEqual(1.25f, cfg.GoldBandMultiplier(OutcomeBand.Flawless), 0.0001f);
            Assert.AreEqual(1.0f, cfg.GoldBandMultiplier(OutcomeBand.Success), 0.0001f);
            Assert.AreEqual(0.6f, cfg.GoldBandMultiplier(OutcomeBand.CloseCall), 0.0001f);
            Assert.AreEqual(0.0f, cfg.GoldBandMultiplier(OutcomeBand.Failure), 0.0001f);
            Assert.AreEqual(0.0f, cfg.GoldBandMultiplier(OutcomeBand.Catastrophe), 0.0001f);
            Assert.AreEqual(0.25f, cfg.XpBandMultiplier(OutcomeBand.Failure), 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        // ---- Resolve wiring (band/success consistent with the junction) ----

        [Test]
        public void Resolve_BandAndSuccessAlwaysConsistentWithRoll()
        {
            var cfg = Cfg();
            var dungeon = ScriptableObject.CreateInstance<DungeonData>();
            dungeon.id = "d";
            dungeon.difficulty = 100f;
            var tier = new ExpeditionTier("Quick", 10, 240);
            var team = new List<string> { "m1", "m2" };

            for (int seed = 0; seed < 50; seed++)
            {
                var rng = new System.Random(seed);
                // Vary team power so chance spans the clamp range.
                float teamPower = (seed % 5) * 60f;
                var o = CombatResolver.Resolve(teamPower, dungeon, tier, cfg, team, rng);

                Assert.AreEqual(o.roll < o.successChance, o.success, $"seed {seed}");
                Assert.AreEqual(CombatResolver.BandFor(o.successChance, o.roll, cfg), o.band, $"seed {seed}");
                Assert.GreaterOrEqual(o.successChance, cfg.successChanceMin);
                Assert.LessOrEqual(o.successChance, cfg.successChanceMax);
                // Flawless/Success never carry a death; defeat bands give no gold.
                if (o.band == OutcomeBand.Failure || o.band == OutcomeBand.Catastrophe)
                    Assert.AreEqual(0, o.goldReward);
            }

            Object.DestroyImmediate(dungeon);
            Object.DestroyImmediate(cfg);
        }

        // ---- D8 timing ----

        [Test]
        public void SpeedReduction_ScalesAndClampsAtCap()
        {
            var cfg = Cfg(); // factor 0.0025, cap 0.30
            Assert.AreEqual(0f, ExpeditionTiming.SpeedReduction(0, cfg), 0.0001f);
            Assert.AreEqual(0.15f, ExpeditionTiming.SpeedReduction(60, cfg), 0.0001f);   // fast 4-team
            Assert.AreEqual(cfg.speedTimeReductionCap, ExpeditionTiming.SpeedReduction(100000, cfg), 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void EffectiveDuration_AppliesReduction_AndCap()
        {
            var cfg = Cfg();
            Assert.AreEqual(600, ExpeditionTiming.EffectiveDurationSeconds(600, 0, cfg));   // no speed
            Assert.AreEqual(510, ExpeditionTiming.EffectiveDurationSeconds(600, 60, cfg));  // 15% off
            Assert.AreEqual(700, ExpeditionTiming.EffectiveDurationSeconds(1000, 100000, cfg)); // capped 30%
            Object.DestroyImmediate(cfg);
        }
    }
}
