using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for status effect mechanics (Shielded, Slowed, Restrained, Raging,
    /// Marked), effect timing, and the special ability features (maximize,
    /// lifesteal, save-for-half).
    /// </summary>
    public class StatusEffectMechanicsTests
    {
        private CombatUnit _unit;
        private CombatUnit _other;

        [SetUp]
        public void SetUp()
        {
            _unit = TestUnits.Enemy("Unit", new GridPosition(0, 0), movement: 4, ac: 12);
            _other = TestUnits.Player("Other", new GridPosition(1, 0));
        }

        // --- Timing ---

        [Test]
        public void OneTurnEffect_OnAnotherUnit_LastsThroughItsNextTurn()
        {
            // Shield Bash on my turn: the target is stunned for its next turn, then it wears off
            _unit.ApplyStatusEffect(StatusEffectType.Stunned, 1, _other);

            _unit.TickTurnStart();
            Assert.IsTrue(_unit.HasStatusEffect(StatusEffectType.Stunned), "Still stunned when its turn starts.");

            _unit.TickTurnEnd();
            Assert.IsFalse(_unit.HasStatusEffect(StatusEffectType.Stunned));
        }

        [Test]
        public void SelfAppliedEffect_SurvivesTheTurnItWasCast()
        {
            // Cunning Dodge on my turn: must still be up during the enemies' turns
            _unit.ApplyStatusEffect(StatusEffectType.Shielded, 1, _unit);

            _unit.TickTurnEnd();
            Assert.IsTrue(_unit.HasStatusEffect(StatusEffectType.Shielded), "Casting turn doesn't consume duration.");

            _unit.TickTurnStart();
            _unit.TickTurnEnd();
            Assert.IsFalse(_unit.HasStatusEffect(StatusEffectType.Shielded));
        }

        // --- Defensive / movement effects ---

        [Test]
        public void Shielded_AddsFourAC()
        {
            _unit.ApplyStatusEffect(StatusEffectType.Shielded, 1);
            Assert.AreEqual(12 + GameConstants.SHIELDED_AC_BONUS, _unit.EffectiveAC);
        }

        [Test]
        public void Slowed_ReducesMovement()
        {
            _unit.ApplyStatusEffect(StatusEffectType.Slowed, 1);
            Assert.AreEqual(4 - GameConstants.SLOWED_MOVEMENT_PENALTY, _unit.EffectiveMovement);
        }

        [Test]
        public void Slowed_NeverGoesBelowZero()
        {
            var slow = TestUnits.Enemy("Slow", new GridPosition(0, 0), movement: 1);
            slow.ApplyStatusEffect(StatusEffectType.Slowed, 1);
            Assert.AreEqual(0, slow.EffectiveMovement);
        }

        [Test]
        public void Restrained_StopsMovement()
        {
            _unit.ApplyStatusEffect(StatusEffectType.Restrained, 1);
            Assert.AreEqual(0, _unit.EffectiveMovement);
        }

        // --- Damage modifiers ---

        [Test]
        public void Raging_AddsMeleeDamage()
        {
            var melee = TestUnits.Ability(category: AbilityCategory.MeleeAttack);
            _unit.ApplyStatusEffect(StatusEffectType.Raging, 3, _unit);

            Assert.AreEqual(10 + GameConstants.RAGE_DAMAGE_BONUS,
                DamageCalculator.ApplyDamageModifiers(_unit, _other, melee, 10));
        }

        [Test]
        public void Raging_DoesNotBoostSpells()
        {
            var spell = TestUnits.Ability(category: AbilityCategory.Spell, damageType: DamageType.Fire);
            _unit.ApplyStatusEffect(StatusEffectType.Raging, 3, _unit);

            Assert.AreEqual(10, DamageCalculator.ApplyDamageModifiers(_unit, _other, spell, 10));
        }

        [Test]
        public void RagingTarget_HalvesPhysicalDamageOnly()
        {
            _other.ApplyStatusEffect(StatusEffectType.Raging, 3, _other);

            var slash = TestUnits.Ability(category: AbilityCategory.Spell, damageType: DamageType.Slashing);
            var fire = TestUnits.Ability(category: AbilityCategory.Spell, damageType: DamageType.Fire);

            Assert.AreEqual(5, DamageCalculator.ApplyDamageModifiers(_unit, _other, slash, 10));
            Assert.AreEqual(10, DamageCalculator.ApplyDamageModifiers(_unit, _other, fire, 10));
        }

        [Test]
        public void Marked_BonusOnlyFromTheMarker()
        {
            var bow = TestUnits.Ability(category: AbilityCategory.RangedAttack, damageType: DamageType.Piercing);
            var stranger = TestUnits.Enemy("Stranger", new GridPosition(5, 5));
            _other.ApplyStatusEffect(StatusEffectType.Marked, 3, _unit);

            int fromMarker = DamageCalculator.ApplyDamageModifiers(_unit, _other, bow, 10);
            Assert.That(fromMarker, Is.InRange(11, 10 + GameConstants.MARKED_BONUS_DIE));

            Assert.AreEqual(10, DamageCalculator.ApplyDamageModifiers(stranger, _other, bow, 10));
        }

        // --- Ability features ---

        [Test]
        public void MaximizeDamage_RollsMaxOnEveryDie()
        {
            var ability = TestUnits.Ability(dice: 6, sides: 10);
            TestUnits.Set(ability, "_maximizeDamage", true);

            Assert.AreEqual(60, DamageCalculator.RollDamageDice(ability, isCritical: false));
            Assert.AreEqual(120, DamageCalculator.RollDamageDice(ability, isCritical: true));
        }

        [Test]
        public void Lifesteal_HealsPercentOfDamage()
        {
            Assert.AreEqual(7, DamageCalculator.LifestealAmount(15, 50));
            Assert.AreEqual(0, DamageCalculator.LifestealAmount(0, 50));
            Assert.AreEqual(0, DamageCalculator.LifestealAmount(15, 0));
        }

        [Test]
        public void SaveForHalf_FullOnFailHalfOnSuccess()
        {
            var fireball = TestUnits.Ability("Fireball", AbilityCategory.Spell, AbilityTargetType.Point, 4, 6, DamageType.Fire, 6);
            TestUnits.Set(fireball, "_maximizeDamage", true); // 24, so only the save varies
            TestUnits.Set(fireball, "_saveForHalfDamage", true);
            TestUnits.Set(fireball, "_savingThrowAbility", AbilityScore.Dexterity);
            TestUnits.Set(fireball, "_savingThrowDC", 11);

            for (int i = 0; i < 50; i++)
            {
                var result = DamageCalculator.ResolveSaveDamage(_other, _unit, fireball);
                Assert.AreEqual(result.Save.Success ? 12 : 24, result.TotalDamage);
            }
        }

        [Test]
        public void SaveForHalf_EffectOnlyOnFailedSave()
        {
            var spit = TestUnits.Ability("Spit", AbilityCategory.Spell, AbilityTargetType.Point, 1, 4, DamageType.Piercing, 3);
            TestUnits.Set(spit, "_saveForHalfDamage", true);
            TestUnits.Set(spit, "_savingThrowAbility", AbilityScore.Constitution);
            TestUnits.Set(spit, "_savingThrowDC", 11);
            TestUnits.Set(spit, "_appliesEffect", StatusEffectType.Poisoned);
            TestUnits.Set(spit, "_effectDurationTurns", 2);

            for (int i = 0; i < 50; i++)
            {
                var result = DamageCalculator.ResolveSaveDamage(_other, _unit, spit);
                Assert.AreEqual(!result.Save.Success, result.EffectApplies);
            }
        }
    }
}
