using System.Collections.Generic;
using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;
using UnityEngine;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the EnemyAI decision system.
    /// </summary>
    public class EnemyAITests
    {
        private CombatUnit _enemy;
        private List<CombatUnit> _playerUnits;
        private List<CombatUnit> _enemyUnits;

        [SetUp]
        public void SetUp()
        {
            // Create an attack ability
            var attackAbility = ScriptableObject.CreateInstance<AbilitySO>();
            SetField(attackAbility, "_abilityName", "Slash");
            SetField(attackAbility, "_category", AbilityCategory.MeleeAttack);
            SetField(attackAbility, "_damageDiceCount", 1);
            SetField(attackAbility, "_damageDiceSides", 6);

            // Create enemy
            var enemySO = ScriptableObject.CreateInstance<EnemySO>();
            SetField(enemySO, "_enemyName", "Test Enemy");
            SetField(enemySO, "_hitPoints", 30);
            SetField(enemySO, "_armorClass", 12);
            SetField(enemySO, "_stats", new StatBlock
            {
                strength = 14, dexterity = 10, constitution = 12,
                intelligence = 8, wisdom = 8, charisma = 6
            });
            SetField(enemySO, "_abilities", new AbilitySO[] { attackAbility });
            SetField(enemySO, "_targetPreference", AITargetPreference.LowestHP);
            SetField(enemySO, "_aggression", 0.8f);
            _enemy = CombatUnit.FromEnemy(enemySO);

            // Create player units
            _playerUnits = new List<CombatUnit>();
            for (int i = 0; i < 2; i++)
            {
                var playerSO = ScriptableObject.CreateInstance<EnemySO>();
                SetField(playerSO, "_enemyName", $"Player {i}");
                SetField(playerSO, "_hitPoints", 20);
                SetField(playerSO, "_armorClass", 10);
                SetField(playerSO, "_abilities", new AbilitySO[0]);
                _playerUnits.Add(CombatUnit.FromEnemy(playerSO));
            }

            _enemyUnits = new List<CombatUnit> { _enemy };
        }

        [Test]
        public void Decide_ReturnsAbilityAndTarget()
        {
            var decision = EnemyAI.Decide(_enemy, _playerUnits, _enemyUnits);

            Assert.IsNotNull(decision.ChosenAbility);
            Assert.IsNotNull(decision.Target);
        }

        [Test]
        public void Decide_TargetsLowestHP()
        {
            // Damage one player to make them lowest HP
            _playerUnits[1].TakeDamage(10);

            // Run many times — with LowestHP preference, should mostly target player 1
            int targetedPlayer1 = 0;
            for (int i = 0; i < 100; i++)
            {
                var decision = EnemyAI.Decide(_enemy, _playerUnits, _enemyUnits);
                if (decision.Target == _playerUnits[1]) targetedPlayer1++;
            }

            // Should target lowest HP most of the time (80% non-suboptimal)
            Assert.Greater(targetedPlayer1, 50, "Should target lowest HP player majority of the time");
        }

        [Test]
        public void Decide_SkipsDeadTargets()
        {
            _playerUnits[0].Die();

            var decision = EnemyAI.Decide(_enemy, _playerUnits, _enemyUnits);

            Assert.AreEqual(_playerUnits[1], decision.Target);
        }

        [Test]
        public void Decide_SkipsUnconsciousTargets()
        {
            _playerUnits[0].TakeDamage(20); // Down them

            var decision = EnemyAI.Decide(_enemy, _playerUnits, _enemyUnits);

            Assert.AreEqual(_playerUnits[1], decision.Target);
        }

        [Test]
        public void Decide_ReturnsNullWhenNoValidTargets()
        {
            foreach (var p in _playerUnits) p.Die();

            var decision = EnemyAI.Decide(_enemy, _playerUnits, _enemyUnits);

            Assert.IsNull(decision.Target);
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(obj, value);
        }
    }
}
