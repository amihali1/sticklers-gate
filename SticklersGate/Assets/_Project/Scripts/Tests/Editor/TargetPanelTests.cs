using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;
using SticklersGate.UI;

namespace SticklersGate.Tests.Editor
{
    /// <summary>
    /// Tests for TargetPanel.GetValidTargets static method.
    /// </summary>
    [TestFixture]
    public class TargetPanelTests
    {
        private CharacterClassSO _testClass;
        private EnemySO _testEnemy;
        private AbilitySO _testAbility;

        [SetUp]
        public void SetUp()
        {
            _testClass = ScriptableObject.CreateInstance<CharacterClassSO>();
            SetPrivateField(_testClass, "_className", "TestClass");
            SetPrivateField(_testClass, "_baseAC", 10);
            SetPrivateField(_testClass, "_hitDiceSides", 10);
            SetPrivateField(_testClass, "_baseStats", new StatBlock());
            SetPrivateField(_testClass, "_level1Abilities", new AbilitySO[0]);

            _testEnemy = ScriptableObject.CreateInstance<EnemySO>();
            SetPrivateField(_testEnemy, "_enemyName", "TestEnemy");
            SetPrivateField(_testEnemy, "_armorClass", 12);
            SetPrivateField(_testEnemy, "_hitPoints", 20);
            SetPrivateField(_testEnemy, "_stats", new StatBlock());

            _testAbility = ScriptableObject.CreateInstance<AbilitySO>();
            SetPrivateField(_testAbility, "_abilityName", "TestAbility");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testClass);
            Object.DestroyImmediate(_testEnemy);
            Object.DestroyImmediate(_testAbility);
        }

        [Test]
        public void SingleEnemy_ReturnsAliveConsciousEnemies()
        {
            SetPrivateField(_testAbility, "_targetType", AbilityTargetType.SingleEnemy);

            var player = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            var enemy1 = CombatUnit.FromEnemy(_testEnemy, "Goblin A");
            var enemy2 = CombatUnit.FromEnemy(_testEnemy, "Goblin B");

            var players = new List<CombatUnit> { player };
            var enemies = new List<CombatUnit> { enemy1, enemy2 };

            var targets = TargetPanel.GetValidTargets(AbilityTargetType.SingleEnemy, player, players, enemies);

            Assert.AreEqual(2, targets.Count);
            Assert.Contains(enemy1, targets);
            Assert.Contains(enemy2, targets);
        }

        [Test]
        public void SingleEnemy_ExcludesDeadEnemies()
        {
            var player = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            var enemy1 = CombatUnit.FromEnemy(_testEnemy, "Goblin A");
            var enemy2 = CombatUnit.FromEnemy(_testEnemy, "Goblin B");
            enemy2.Die();

            var players = new List<CombatUnit> { player };
            var enemies = new List<CombatUnit> { enemy1, enemy2 };

            var targets = TargetPanel.GetValidTargets(AbilityTargetType.SingleEnemy, player, players, enemies);

            Assert.AreEqual(1, targets.Count);
            Assert.Contains(enemy1, targets);
        }

        [Test]
        public void SingleAlly_ReturnsAliveAllies()
        {
            var player1 = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            var player2 = CombatUnit.FromPlayerCharacter("Healer", _testClass, 1);
            var enemy = CombatUnit.FromEnemy(_testEnemy, "Goblin");

            var players = new List<CombatUnit> { player1, player2 };
            var enemies = new List<CombatUnit> { enemy };

            var targets = TargetPanel.GetValidTargets(AbilityTargetType.SingleAlly, player1, players, enemies);

            Assert.AreEqual(2, targets.Count);
            Assert.Contains(player1, targets);
            Assert.Contains(player2, targets);
        }

        [Test]
        public void Self_ReturnsOnlyActor()
        {
            var player = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            var enemy = CombatUnit.FromEnemy(_testEnemy, "Goblin");

            var players = new List<CombatUnit> { player };
            var enemies = new List<CombatUnit> { enemy };

            var targets = TargetPanel.GetValidTargets(AbilityTargetType.Self, player, players, enemies);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(player, targets[0]);
        }

        [Test]
        public void AllEnemies_ReturnsAliveConsciousEnemies()
        {
            var player = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            var enemy1 = CombatUnit.FromEnemy(_testEnemy, "Goblin A");
            var enemy2 = CombatUnit.FromEnemy(_testEnemy, "Goblin B");
            var enemy3 = CombatUnit.FromEnemy(_testEnemy, "Goblin C");
            enemy3.Die();

            var players = new List<CombatUnit> { player };
            var enemies = new List<CombatUnit> { enemy1, enemy2, enemy3 };

            var targets = TargetPanel.GetValidTargets(AbilityTargetType.AllEnemies, player, players, enemies);

            Assert.AreEqual(2, targets.Count);
        }

        [Test]
        public void AllAllies_ReturnsAliveAllies()
        {
            var player1 = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            var player2 = CombatUnit.FromPlayerCharacter("Healer", _testClass, 1);
            var enemy = CombatUnit.FromEnemy(_testEnemy, "Goblin");

            var players = new List<CombatUnit> { player1, player2 };
            var enemies = new List<CombatUnit> { enemy };

            var targets = TargetPanel.GetValidTargets(AbilityTargetType.AllAllies, player1, players, enemies);

            Assert.AreEqual(2, targets.Count);
        }

        [Test]
        public void EnemyActor_TreatsPlayersAsEnemies()
        {
            var player = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            var enemy = CombatUnit.FromEnemy(_testEnemy, "Goblin");

            var players = new List<CombatUnit> { player };
            var enemies = new List<CombatUnit> { enemy };

            // Enemy targeting SingleEnemy should see players as enemies
            var targets = TargetPanel.GetValidTargets(AbilityTargetType.SingleEnemy, enemy, players, enemies);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(player, targets[0]);
        }

        [Test]
        public void SingleEnemy_ExcludesUnconsciousEnemies()
        {
            var player = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            var enemy = CombatUnit.FromEnemy(_testEnemy, "Goblin");

            // Knock enemy unconscious
            enemy.TakeDamage(enemy.MaxHP);

            var players = new List<CombatUnit> { player };
            var enemies = new List<CombatUnit> { enemy };

            var targets = TargetPanel.GetValidTargets(AbilityTargetType.SingleEnemy, player, players, enemies);

            Assert.AreEqual(0, targets.Count);
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }
                type = type.BaseType;
            }
        }
    }
}
