using System;
using NUnit.Framework;
using UnityEngine;

namespace Guildmaster.Tests
{
    /// <summary>
    /// Guards the §7 / D4 / D7 numeric FRAMES: success clamp, item-inherit scaling,
    /// bloodline cap, at-risk gold loss, and the offline cap. These are the limits
    /// the test agent is authorised to enforce as bugs (CLAUDE.md §11).
    /// </summary>
    public class BalanceFramesTests
    {
        private BalanceConfig Cfg()
        {
            return ScriptableObject.CreateInstance<BalanceConfig>();
        }

        [Test]
        public void SuccessChance_NeverExceedsClamp()
        {
            var cfg = Cfg();
            // Absurdly strong team vs trivial difficulty -> must clamp to max (<1).
            float high = CombatResolver.SuccessChance(100000f, 1f, cfg);
            Assert.LessOrEqual(high, cfg.successChanceMax);
            Assert.Less(high, 1f, "Success can never be 100% (D-rule).");

            // Hopeless team vs huge difficulty -> must clamp to min (>0).
            float low = CombatResolver.SuccessChance(1f, 100000f, cfg);
            Assert.GreaterOrEqual(low, cfg.successChanceMin);
            Assert.Greater(low, 0f);

            UnityEngine.Object.DestroyImmediate(cfg);
        }

        [Test]
        public void InheritedItemLevel_ScalesByConfig_AtLeastOne()
        {
            // D7: ~0.3 scaling. 100 -> 30.
            Assert.AreEqual(30, LegacyMath.ScaleInheritedItemLevel(100, 0.3f));
            // Floor at 1 so a low-level item still inherits something.
            Assert.AreEqual(1, LegacyMath.ScaleInheritedItemLevel(1, 0.3f));
        }

        [Test]
        public void BloodlineBonus_NeverExceedsCap()
        {
            float cap = 0.20f;
            float v = 0f;
            for (int i = 0; i < 100; i++)
            {
                v = LegacyMath.AccumulateBloodlineBonus(v, 0.05f, cap);
            }
            Assert.LessOrEqual(v, cap, "Bloodline bonus must cap at +20% per stat (D7).");
            Assert.AreEqual(cap, v, 0.0001f);
        }

        [Test]
        public void GoldLoss_OnlyTouchesAtRiskPool()
        {
            // D4: lose a % of the at-risk pool only.
            long lost = LegacyMath.GoldLostOnDeath(1000, 0.25f);
            Assert.AreEqual(250, lost);

            // Nothing at risk -> nothing lost (protected savings untouched).
            Assert.AreEqual(0, LegacyMath.GoldLostOnDeath(0, 0.25f));
        }

        [Test]
        public void OfflineAccrual_CapsAt12h()
        {
            var cfg = Cfg();
            long now = DateTime.UtcNow.Ticks;
            long longAgo = now - TimeSpan.FromHours(48).Ticks;

            double capped = OfflineCalculator.CappedElapsedSeconds(longAgo, now, cfg.OfflineCapSeconds);
            Assert.AreEqual(cfg.OfflineCapSeconds, capped, 1.0, "Offline accrual caps at 12h (D2).");

            // Under the cap, full elapsed time is returned.
            long oneHourAgo = now - TimeSpan.FromHours(1).Ticks;
            double under = OfflineCalculator.CappedElapsedSeconds(oneHourAgo, now, cfg.OfflineCapSeconds);
            Assert.AreEqual(3600.0, under, 1.0);

            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }
}
