using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;
using SticklersGate.Utils;
using UnityEngine;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the DamageCalculator.
    /// </summary>
    public class DamageCalculatorTests
    {
        private CombatUnit _attacker;
        private CombatUnit _target;
        private AbilitySO _meleeAbility;
        private AbilitySO _healAbility;

        [SetUp]
        public void SetUp()
        {
            DiceRoller.ClearHistory();

            // Create attacker
            var attackerSO = ScriptableObject.CreateInstance<EnemySO>();
            SetField(attackerSO, "_enemyName", "Attacker");
            SetField(attackerSO, "_hitPoints", 30);
            SetField(attackerSO, "_armorClass", 14);
            SetField(attackerSO, "_stats", new StatBlock
            {
                strength = 16, dexterity = 12, constitution = 14,
                intelligence = 10, wisdom = 10, charisma = 10
            });
            SetField(attackerSO, "_abilities", new AbilitySO[0]);
            _attacker = CombatUnit.FromEnemy(attackerSO);

            // Create target
            var targetSO = ScriptableObject.CreateInstance<EnemySO>();
            SetField(targetSO, "_enemyName", "Target");
            SetField(targetSO, "_hitPoints", 20);
            SetField(targetSO, "_armorClass", 12);
            SetField(targetSO, "_stats", new StatBlock
            {
                strength = 10, dexterity = 10, constitution = 10,
                intelligence = 10, wisdom = 10, charisma = 10
            });
            SetField(targetSO, "_abilities", new AbilitySO[0]);
            _target = CombatUnit.FromEnemy(targetSO);

            // Create melee ability: 2d6 slashing + STR
            _meleeAbility = ScriptableObject.CreateInstance<AbilitySO>();
            SetField(_meleeAbility, "_abilityName", "Sword Slash");
            SetField(_meleeAbility, "_category", AbilityCategory.MeleeAttack);
            SetField(_meleeAbility, "_damageDiceCount", 2);
            SetField(_meleeAbility, "_damageDiceSides", 6);
            SetField(_meleeAbility, "_damageType", DamageType.Slashing);
            SetField(_meleeAbility, "_abilityModifier", AbilityScore.Strength);

            // Create heal ability: 2d8 + 4
            _healAbility = ScriptableObject.CreateInstance<AbilitySO>();
            SetField(_healAbility, "_abilityName", "Cure Wounds");
            SetField(_healAbility, "_category", AbilityCategory.Heal);
            SetField(_healAbility, "_healDiceCount", 2);
            SetField(_healAbility, "_healDiceSides", 8);
            SetField(_healAbility, "_flatHealBonus", 4);
        }

        [Test]
        public void ResolveAttack_ReturnsValidResult()
        {
            var result = DamageCalculator.ResolveAttack(_attacker, _target, _meleeAbility);

            Assert.GreaterOrEqual(result.D20Roll, 1);
            Assert.LessOrEqual(result.D20Roll, 20);
            Assert.AreEqual(3, result.Modifier); // STR 16 → +3
            Assert.AreEqual(12, result.TargetAC);
        }

        [Test]
        public void ResolveAttack_HitDealsDamage()
        {
            // Run many times to get at least one hit
            bool gotHit = false;
            for (int i = 0; i < 100; i++)
            {
                var result = DamageCalculator.ResolveAttack(_attacker, _target, _meleeAbility);
                if (result.IsHit)
                {
                    gotHit = true;
                    Assert.Greater(result.TotalDamage, 0);
                    // 2d6 + 3 (STR mod): min 5, max 15
                    Assert.GreaterOrEqual(result.TotalDamage, 5);
                    Assert.LessOrEqual(result.TotalDamage, 15);
                    break;
                }
            }
            Assert.IsTrue(gotHit, "Should have gotten at least one hit in 100 rolls");
        }

        [Test]
        public void ResolveAttack_MissDealzZeroDamage()
        {
            // Run many times to get at least one miss
            bool gotMiss = false;
            for (int i = 0; i < 100; i++)
            {
                var result = DamageCalculator.ResolveAttack(_attacker, _target, _meleeAbility);
                if (!result.IsHit && !result.IsCriticalFail)
                {
                    gotMiss = true;
                    Assert.AreEqual(0, result.TotalDamage);
                    break;
                }
            }
            Assert.IsTrue(gotMiss, "Should have gotten at least one miss in 100 rolls");
        }

        [Test]
        public void IsInRange_MeleeAdjacentTrue_FarFalse()
        {
            // _meleeAbility has default range 1. Same tile (distance 0) is in range.
            _attacker.Position = new GridPosition(0, 0);
            _target.Position = new GridPosition(0, 0);
            Assert.IsTrue(DamageCalculator.IsInRange(_attacker, _target, _meleeAbility));

            // Distance 3 is out of a range-1 melee reach.
            _target.Position = new GridPosition(3, 0);
            Assert.IsFalse(DamageCalculator.IsInRange(_attacker, _target, _meleeAbility));
        }

        [Test]
        public void IsInRange_RangedReachesFartherThanMelee()
        {
            var rangedAbility = ScriptableObject.CreateInstance<AbilitySO>();
            SetField(rangedAbility, "_category", AbilityCategory.RangedAttack);
            SetField(rangedAbility, "_targetType", AbilityTargetType.SingleEnemy);
            SetField(rangedAbility, "_range", 4);

            _attacker.Position = new GridPosition(0, 0);
            _target.Position = new GridPosition(4, 0); // exactly at range
            Assert.IsTrue(DamageCalculator.IsInRange(_attacker, _target, rangedAbility));

            _target.Position = new GridPosition(5, 0); // one past
            Assert.IsFalse(DamageCalculator.IsInRange(_attacker, _target, rangedAbility));
        }

        [Test]
        public void IsInRange_AoEAndSelfIgnoreRange()
        {
            var aoe = ScriptableObject.CreateInstance<AbilitySO>();
            SetField(aoe, "_targetType", AbilityTargetType.AllEnemies);
            SetField(aoe, "_range", 1);

            _attacker.Position = new GridPosition(0, 0);
            _target.Position = new GridPosition(7, 5); // far away
            Assert.IsTrue(DamageCalculator.IsInRange(_attacker, _target, aoe));
        }

        [Test]
        public void ResolveAttack_PopulatesWasInRange()
        {
            _attacker.Position = new GridPosition(0, 0);
            _target.Position = new GridPosition(0, 0); // adjacent (range 1 melee)
            var inRange = DamageCalculator.ResolveAttack(_attacker, _target, _meleeAbility);
            Assert.IsTrue(inRange.WasInRange);

            _target.Position = new GridPosition(4, 4); // out of melee range
            var outOfRange = DamageCalculator.ResolveAttack(_attacker, _target, _meleeAbility);
            Assert.IsFalse(outOfRange.WasInRange);
        }

        [Test]
        public void ResolveHeal_ReturnsValidResult()
        {
            var result = DamageCalculator.ResolveHeal(_attacker, _healAbility);

            Assert.AreEqual(4, result.FlatBonus);
            // 2d8 + 4: min 6, max 20
            Assert.GreaterOrEqual(result.TotalHealed, 6);
            Assert.LessOrEqual(result.TotalHealed, 20);
        }

        [Test]
        public void ResolveSavingThrow_ReturnsValidResult()
        {
            var result = DamageCalculator.ResolveSavingThrow(_target, AbilityScore.Dexterity, 15);

            Assert.GreaterOrEqual(result.D20Roll, 1);
            Assert.LessOrEqual(result.D20Roll, 20);
            Assert.AreEqual(0, result.Modifier); // DEX 10 → +0
            Assert.AreEqual(15, result.DC);
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(obj, value);
        }
    }
}
