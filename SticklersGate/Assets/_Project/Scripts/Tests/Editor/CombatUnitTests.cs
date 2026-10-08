using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the CombatUnit runtime model.
    /// </summary>
    public class CombatUnitTests
    {
        private CombatUnit _unit;

        [SetUp]
        public void SetUp()
        {
            // Create a unit directly via enemy factory for testing
            var enemySO = UnityEngine.ScriptableObject.CreateInstance<EnemySO>();

            // Use reflection to set private fields since they're [SerializeField]
            SetPrivateField(enemySO, "_enemyName", "Test Goblin");
            SetPrivateField(enemySO, "_hitPoints", 20);
            SetPrivateField(enemySO, "_armorClass", 12);
            SetPrivateField(enemySO, "_stats", new StatBlock
            {
                strength = 10, dexterity = 14, constitution = 10,
                intelligence = 8, wisdom = 8, charisma = 6
            });
            SetPrivateField(enemySO, "_abilities", new AbilitySO[0]);

            _unit = CombatUnit.FromEnemy(enemySO);
        }

        [Test]
        public void FromEnemy_SetsCorrectValues()
        {
            Assert.AreEqual("Test Goblin", _unit.Name);
            Assert.AreEqual(20, _unit.MaxHP);
            Assert.AreEqual(20, _unit.CurrentHP);
            Assert.AreEqual(12, _unit.BaseAC);
            Assert.AreEqual(GameConstants.DEFAULT_MELEE_MOVEMENT, _unit.MovementSpeed);
            Assert.AreEqual(new GridPosition(0, 0), _unit.Position);
            Assert.IsFalse(_unit.IsPlayer);
            Assert.IsTrue(_unit.IsAlive);
            Assert.IsTrue(_unit.IsConscious);
        }

        [Test]
        public void TakeDamage_ReducesHP()
        {
            _unit.TakeDamage(5);
            Assert.AreEqual(15, _unit.CurrentHP);
        }

        [Test]
        public void TakeDamage_CannotGoBelowZero()
        {
            _unit.TakeDamage(100);
            Assert.AreEqual(0, _unit.CurrentHP);
        }

        [Test]
        public void TakeDamage_DownsAtZeroHP()
        {
            _unit.TakeDamage(20);
            Assert.AreEqual(0, _unit.CurrentHP);
            Assert.IsFalse(_unit.IsConscious);
            Assert.IsTrue(_unit.IsAlive); // Downed, not dead
        }

        [Test]
        public void TakeDamage_FiresEvent()
        {
            int eventDamage = -1;
            _unit.OnDamageTaken += (d) => eventDamage = d;

            _unit.TakeDamage(7);
            Assert.AreEqual(7, eventDamage);
        }

        [Test]
        public void TakeDamage_IgnoredWhenDead()
        {
            _unit.Die();
            _unit.TakeDamage(5);
            Assert.AreEqual(0, _unit.CurrentHP);
        }

        [Test]
        public void Heal_RestoresHP()
        {
            _unit.TakeDamage(10);
            _unit.Heal(5);
            Assert.AreEqual(15, _unit.CurrentHP);
        }

        [Test]
        public void Heal_CannotExceedMaxHP()
        {
            _unit.TakeDamage(5);
            _unit.Heal(100);
            Assert.AreEqual(20, _unit.CurrentHP);
        }

        [Test]
        public void Heal_RevivesDownedUnit()
        {
            _unit.TakeDamage(20);
            Assert.IsFalse(_unit.IsConscious);

            _unit.Heal(5);
            Assert.IsTrue(_unit.IsConscious);
            Assert.AreEqual(5, _unit.CurrentHP);
        }

        [Test]
        public void Heal_IgnoredWhenDead()
        {
            _unit.Die();
            _unit.Heal(10);
            Assert.AreEqual(0, _unit.CurrentHP);
            Assert.IsFalse(_unit.IsAlive);
        }

        [Test]
        public void Die_SetsFlags()
        {
            _unit.Die();
            Assert.IsFalse(_unit.IsAlive);
            Assert.IsFalse(_unit.IsConscious);
            Assert.AreEqual(0, _unit.CurrentHP);
        }

        [Test]
        public void Die_FiresEvent()
        {
            bool eventFired = false;
            _unit.OnDeath += () => eventFired = true;

            _unit.Die();
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void RecordDeathSave_ThreeSuccessesStabilizes()
        {
            _unit.TakeDamage(20); // Down the unit

            _unit.RecordDeathSave(true);
            _unit.RecordDeathSave(true);
            bool resolved = _unit.RecordDeathSave(true);

            Assert.IsTrue(resolved);
            Assert.IsTrue(_unit.IsStabilized);
            Assert.IsTrue(_unit.IsAlive);
            Assert.IsFalse(_unit.IsConscious);
        }

        [Test]
        public void RecordDeathSave_ThreeFailuresKills()
        {
            _unit.TakeDamage(20);

            _unit.RecordDeathSave(false);
            _unit.RecordDeathSave(false);
            bool resolved = _unit.RecordDeathSave(false);

            Assert.IsTrue(resolved);
            Assert.IsFalse(_unit.IsAlive);
        }

        [Test]
        public void ReviveFromDeathSave_RestoresWithOneHP()
        {
            _unit.TakeDamage(20);
            _unit.RecordDeathSave(true);

            _unit.ReviveFromDeathSave();

            Assert.AreEqual(1, _unit.CurrentHP);
            Assert.IsTrue(_unit.IsConscious);
            Assert.IsFalse(_unit.IsStabilized);
            Assert.AreEqual(0, _unit.DeathSaveSuccesses);
            Assert.AreEqual(0, _unit.DeathSaveFailures);
        }

        [Test]
        public void StatusEffect_ApplyAndCheck()
        {
            _unit.ApplyStatusEffect(StatusEffectType.Poisoned, 3);

            Assert.IsTrue(_unit.HasStatusEffect(StatusEffectType.Poisoned));
            Assert.IsFalse(_unit.HasStatusEffect(StatusEffectType.Burning));
        }

        [Test]
        public void StatusEffect_NoneIsIgnored()
        {
            _unit.ApplyStatusEffect(StatusEffectType.None, 3);
            Assert.AreEqual(0, _unit.StatusEffects.Count);
        }

        [Test]
        public void StatusEffect_Remove()
        {
            _unit.ApplyStatusEffect(StatusEffectType.Stunned, 2);
            _unit.RemoveStatusEffect(StatusEffectType.Stunned);

            Assert.IsFalse(_unit.HasStatusEffect(StatusEffectType.Stunned));
        }

        [Test]
        public void StatusEffect_ReplacesSameType()
        {
            _unit.ApplyStatusEffect(StatusEffectType.Poisoned, 3);
            _unit.ApplyStatusEffect(StatusEffectType.Poisoned, 5);

            Assert.AreEqual(1, _unit.StatusEffects.Count);
            Assert.AreEqual(5, _unit.StatusEffects[0].RemainingTurns);
        }

        [Test]
        public void TickTurnEnd_ExpiresEffects()
        {
            _unit.ApplyStatusEffect(StatusEffectType.Blessed, 1);

            _unit.TickTurnStart();
            Assert.IsTrue(_unit.HasStatusEffect(StatusEffectType.Blessed), "Effects no longer tick at turn start.");

            _unit.TickTurnEnd();
            Assert.IsFalse(_unit.HasStatusEffect(StatusEffectType.Blessed));
        }

        [Test]
        public void TickTurnStart_ResetsBonusAC()
        {
            _unit.BonusAC = 4;
            _unit.TickTurnStart();
            Assert.AreEqual(0, _unit.BonusAC);
        }

        [Test]
        public void EffectiveAC_IncludesBonusAC()
        {
            Assert.AreEqual(12, _unit.EffectiveAC);
            _unit.BonusAC = 2;
            Assert.AreEqual(14, _unit.EffectiveAC);
        }

        [Test]
        public void FromEnemy_NameOverride()
        {
            var enemySO = UnityEngine.ScriptableObject.CreateInstance<EnemySO>();
            SetPrivateField(enemySO, "_enemyName", "Goblin");
            SetPrivateField(enemySO, "_hitPoints", 10);
            SetPrivateField(enemySO, "_armorClass", 10);
            SetPrivateField(enemySO, "_abilities", new AbilitySO[0]);

            var unit = CombatUnit.FromEnemy(enemySO, "Goblin A");
            Assert.AreEqual("Goblin A", unit.Name);
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(obj, value);
        }
    }
}
