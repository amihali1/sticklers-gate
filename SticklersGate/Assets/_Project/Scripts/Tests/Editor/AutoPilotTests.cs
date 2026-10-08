using System.Collections.Generic;
using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the autoplay policy and for enemy target selection respecting range.
    /// </summary>
    public class AutoPilotTests
    {
        private static AbilitySO Heal(int range)
        {
            var heal = TestUnits.Ability("Heal", AbilityCategory.Heal, AbilityTargetType.SingleAlly, 0, 0, range: range);
            TestUnits.Set(heal, "_healDiceCount", 2);
            TestUnits.Set(heal, "_healDiceSides", 8);
            return heal;
        }

        private static CombatUnit Hero(GridPosition pos, params AbilitySO[] abilities)
        {
            // Enemies are the easiest way to give a test unit abilities; flip sides via the lists
            return TestUnits.Enemy("Hero", pos, 3, 30, 12, abilities);
        }

        [Test]
        public void ChooseAction_HealsBadlyHurtAllyInRange()
        {
            var cleric = Hero(new GridPosition(0, 0), TestUnits.Ability(range: 1), Heal(4));
            var hurt = TestUnits.Enemy("Hurt", new GridPosition(2, 0));
            hurt.TakeDamage(25); // 5/30
            var foe = TestUnits.Player("Foe", new GridPosition(1, 0));

            var plan = AutoPilot.ChooseAction(cleric, new List<CombatUnit> { cleric, hurt }, new List<CombatUnit> { foe });

            Assert.AreEqual("Heal", plan.Ability.Data.AbilityName);
            Assert.AreEqual(hurt, plan.Target);
        }

        [Test]
        public void ChooseAction_AttacksOnlyTargetsInRange()
        {
            var archer = Hero(new GridPosition(0, 0), TestUnits.Ability("Bow", AbilityCategory.RangedAttack, range: 3));
            var near = TestUnits.Player("Near", new GridPosition(2, 0), hp: 30);
            var farButWeak = TestUnits.Player("Far", new GridPosition(7, 5), hp: 30);
            farButWeak.TakeDamage(25);

            var plan = AutoPilot.ChooseAction(archer, new List<CombatUnit> { archer }, new List<CombatUnit> { near, farButWeak });

            Assert.AreEqual(near, plan.Target);
        }

        [Test]
        public void ChooseAction_DefendsWhenNothingReaches()
        {
            var knight = Hero(new GridPosition(0, 0), TestUnits.Ability(range: 1));
            var far = TestUnits.Player("Far", new GridPosition(7, 5));

            var plan = AutoPilot.ChooseAction(knight, new List<CombatUnit> { knight }, new List<CombatUnit> { far });

            Assert.IsTrue(plan.Defend);
        }

        [Test]
        public void EnemyAI_PrefersReachableTargetOverOutOfRangeLowestHP()
        {
            // Regression: enemies picked the lowest-HP player anywhere, then forfeited when out of range
            var bandit = TestUnits.Enemy("Bandit", new GridPosition(5, 2), 2, 22, 13,
                TestUnits.Ability("Crossbow", AbilityCategory.RangedAttack, range: 4));
            var nearTank = TestUnits.Player("Tank", new GridPosition(3, 2), hp: 40);
            var farWeak = TestUnits.Player("Wizard", new GridPosition(0, 5), hp: 30);
            farWeak.TakeDamage(20);

            for (int i = 0; i < 20; i++) // AI has a random branch; it must never pick an unreachable target
            {
                var decision = EnemyAI.Decide(bandit, new List<CombatUnit> { nearTank, farWeak }, new List<CombatUnit> { bandit });
                Assert.AreEqual(nearTank, decision.Target);
            }
        }
    }
}
