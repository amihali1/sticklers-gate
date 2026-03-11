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
        public void ResolveAttack_BackRowBonusApplied()
        {
            _target.Row = CombatRow.BackRow;

            var result = DamageCalculator.ResolveAttack(_attacker, _target, _meleeAbility);

            // Target AC should include back row bonus for melee
            Assert.AreEqual(12 + GameConstants.BACK_ROW_AC_BONUS, result.TargetAC);
        }

        [Test]
        public void ResolveAttack_BackRowBonusNotAppliedForRanged()
        {
            _target.Row = CombatRow.BackRow;

            var rangedAbility = ScriptableObject.CreateInstance<AbilitySO>();
            SetField(rangedAbility, "_category", AbilityCategory.RangedAttack);
            SetField(rangedAbility, "_damageDiceCount", 1);
            SetField(rangedAbility, "_damageDiceSides", 8);
            SetField(rangedAbility, "_abilityModifier", AbilityScore.Dexterity);

            var result = DamageCalculator.ResolveAttack(_attacker, _target, rangedAbility);

            Assert.AreEqual(12, result.TargetAC); // No back row bonus for ranged
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
