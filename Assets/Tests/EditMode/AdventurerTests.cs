using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Guildmaster.Tests
{
    /// <summary>
    /// Task02 coverage: status-transition validity, stat derivation, roster cap,
    /// and recruit gold-spend. EditMode tests don't run Awake, so manager
    /// singletons are wired via a small reflection seam (SetInstance) — production
    /// code stays untouched.
    /// </summary>
    public class AdventurerTests
    {
        // ---- Status state machine (pure) ----

        [Test]
        public void CanDeploy_OnlyWhenHealthy()
        {
            Assert.IsTrue(AdventurerStateMachine.CanDeploy(AdventurerStatus.Healthy));
            Assert.IsFalse(AdventurerStateMachine.CanDeploy(AdventurerStatus.Injured));
            Assert.IsFalse(AdventurerStateMachine.CanDeploy(AdventurerStatus.OnExpedition));
            Assert.IsFalse(AdventurerStateMachine.CanDeploy(AdventurerStatus.Training));
            Assert.IsFalse(AdventurerStateMachine.CanDeploy(AdventurerStatus.Dead));
        }

        [Test]
        public void Transitions_EnforceValidGraph()
        {
            Assert.IsTrue(AdventurerStateMachine.IsValidTransition(AdventurerStatus.Healthy, AdventurerStatus.OnExpedition));
            Assert.IsTrue(AdventurerStateMachine.IsValidTransition(AdventurerStatus.OnExpedition, AdventurerStatus.DeadPending));
            Assert.IsTrue(AdventurerStateMachine.IsValidTransition(AdventurerStatus.OnExpedition, AdventurerStatus.Injured));
            Assert.IsTrue(AdventurerStateMachine.IsValidTransition(AdventurerStatus.Injured, AdventurerStatus.Healthy));
            Assert.IsTrue(AdventurerStateMachine.IsValidTransition(AdventurerStatus.DeadPending, AdventurerStatus.Dead));

            // Invalid: deploy from injured, revive the dead, leave retirement.
            Assert.IsFalse(AdventurerStateMachine.IsValidTransition(AdventurerStatus.Injured, AdventurerStatus.OnExpedition));
            Assert.IsFalse(AdventurerStateMachine.IsValidTransition(AdventurerStatus.Dead, AdventurerStatus.Healthy));
            Assert.IsFalse(AdventurerStateMachine.IsValidTransition(AdventurerStatus.Retired, AdventurerStatus.OnExpedition));
        }

        // ---- Stat derivation ----

        [Test]
        public void Derive_AppliesLevelGrowthAndBloodline()
        {
            var cls = ScriptableObject.CreateInstance<AdventurerClassData>();
            cls.baseStats = new StatBlock(100, 0, 10, 0, 10, 10, 5);
            cls.statsPerLevel = new StatBlock(10, 0, 2, 0, 2, 1, 1);

            var lvl1 = StatCalculator.Derive(cls, 1, null, default);
            Assert.AreEqual(100, lvl1.hp);
            Assert.AreEqual(10, lvl1.attack);

            var lvl3 = StatCalculator.Derive(cls, 3, null, default);
            Assert.AreEqual(120, lvl3.hp);     // 100 + 2*10
            Assert.AreEqual(14, lvl3.attack);  // 10 + 2*2

            var bloodline = new List<BloodlineBonus> { new BloodlineBonus { statType = StatType.Attack, fraction = 0.2f } };
            var buffed = StatCalculator.Derive(cls, 1, bloodline, default);
            Assert.AreEqual(12, buffed.attack); // round(10 * 1.2)
            Assert.AreEqual(100, buffed.hp);    // unaffected stat unchanged

            Object.DestroyImmediate(cls);
        }

        [Test]
        public void GearFlatStats_IsZeroHook_UntilTask05()
        {
            var a = new Adventurer();
            a.equipment.weaponItemId = "anything";
            var gear = StatCalculator.GearFlatStats(a);
            Assert.AreEqual(0, gear.attack);
            Assert.AreEqual(0, gear.hp);
        }

        // ---- Roster cap & recruit gold spend ----

        private GameObject _go;
        private AdventurerManager _adv;
        private GuildManager _guild;
        private AdventurerClassData _adventurerClass;

        private static void SetInstance<T>(T value) where T : Singleton<T>
        {
            var prop = typeof(Singleton<T>).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            prop.GetSetMethod(true).Invoke(null, new object[] { value });
        }

        private void WireManagers(BalanceConfig balance, CurrentData current, PersistentData persistent)
        {
            _go = new GameObject("Managers");
            _guild = _go.AddComponent<GuildManager>();
            _adv = _go.AddComponent<AdventurerManager>();
            SetInstance(_guild);
            SetInstance(_adv);
            _guild.Bind(persistent);
            _adv.Initialize(balance, current);

            _adventurerClass = ScriptableObject.CreateInstance<AdventurerClassData>();
            _adventurerClass.id = "adventurer";
            _adventurerClass.displayName = "Adventurer";
            ContentDatabase.RegisterClass(_adventurerClass);
        }

        [TearDown]
        public void TearDown()
        {
            SetInstance<AdventurerManager>(null);
            SetInstance<GuildManager>(null);
            if (_go != null) Object.DestroyImmediate(_go);
            if (_adventurerClass != null) Object.DestroyImmediate(_adventurerClass);
        }

        [Test]
        public void Recruit_SpendsGold_AndRespectsCap()
        {
            var balance = ScriptableObject.CreateInstance<BalanceConfig>();
            balance.recruitCost = 100;
            balance.baseRosterCap = 2;
            balance.rosterSlotsPerBarracksLevel = 0;

            var current = new CurrentData();
            var persistent = new PersistentData { atRiskGold = 250 };
            WireManagers(balance, current, persistent);

            Assert.AreEqual(2, _adv.RosterCap());

            Assert.IsNotNull(_adv.TryRecruit());
            Assert.AreEqual(150, _guild.TotalGold);
            Assert.AreEqual(1, _adv.LivingCount());

            Assert.IsNotNull(_adv.TryRecruit());
            Assert.AreEqual(50, _guild.TotalGold);
            Assert.AreEqual(2, _adv.LivingCount());

            // Cap reached -> blocked, no further spend.
            Assert.IsNull(_adv.TryRecruit());
            Assert.AreEqual(2, _adv.LivingCount());
            Assert.AreEqual(50, _guild.TotalGold);

            Object.DestroyImmediate(balance);
        }

        [Test]
        public void Recruit_BlockedWhenTooPoor()
        {
            var balance = ScriptableObject.CreateInstance<BalanceConfig>();
            balance.recruitCost = 100;
            balance.baseRosterCap = 5;
            balance.rosterSlotsPerBarracksLevel = 0;

            var current = new CurrentData();
            var persistent = new PersistentData { atRiskGold = 150 };
            WireManagers(balance, current, persistent);

            Assert.IsNotNull(_adv.TryRecruit());   // 150 -> 50
            Assert.AreEqual(50, _guild.TotalGold);

            Assert.IsNull(_adv.TryRecruit());      // 50 < 100, blocked
            Assert.AreEqual(1, _adv.LivingCount());
            Assert.AreEqual(50, _guild.TotalGold);

            Object.DestroyImmediate(balance);
        }

        [Test]
        public void InjuryRecovery_HealsWhenDue_IncludingOffline()
        {
            var balance = ScriptableObject.CreateInstance<BalanceConfig>();
            var current = new CurrentData();
            WireManagers(balance, current, new PersistentData());

            var a = _adv.CreateAdventurer("adventurer", 1, "Fam");
            long now = System.DateTime.UtcNow.Ticks;

            // Injury whose recovery elapsed while away (offline) -> heals on process.
            a.status = AdventurerStatus.Injured;
            a.injurySeverity = InjurySeverity.Major;
            a.injuryHealAtTicksUtc = now - System.TimeSpan.FromHours(1).Ticks;
            Assert.AreEqual(1, _adv.ProcessInjuryRecovery(now));
            Assert.AreEqual(AdventurerStatus.Healthy, a.status);
            Assert.AreEqual(InjurySeverity.None, a.injurySeverity);

            // Not-yet-due injury stays injured.
            a.status = AdventurerStatus.Injured;
            a.injurySeverity = InjurySeverity.Minor;
            a.injuryHealAtTicksUtc = now + System.TimeSpan.FromHours(1).Ticks;
            Assert.AreEqual(0, _adv.ProcessInjuryRecovery(now));
            Assert.AreEqual(AdventurerStatus.Injured, a.status);

            Object.DestroyImmediate(balance);
        }

        [Test]
        public void AddXp_LevelsUp_AndCapsAtMaxLevel()
        {
            var balance = ScriptableObject.CreateInstance<BalanceConfig>();
            balance.maxLevel = 3;
            balance.xpCurveBase = 100f;
            balance.xpCurveExponent = 1f; // XpToNext(L) = 100*L

            var current = new CurrentData();
            WireManagers(balance, current, new PersistentData());

            var a = _adv.CreateAdventurer("adventurer", 1, "Fam");
            // L1->L2 needs 100, L2->L3 needs 200. Give 100 -> level 2.
            _adv.AddXp(a, 100);
            Assert.AreEqual(2, a.level);

            // Give plenty -> should cap at maxLevel 3, xp zeroed.
            _adv.AddXp(a, 10000);
            Assert.AreEqual(3, a.level);
            Assert.AreEqual(0, a.xp);

            Object.DestroyImmediate(balance);
        }
    }
}
