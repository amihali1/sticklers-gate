using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests.Editor
{
    /// <summary>
    /// Tests for the defend action mechanics.
    /// </summary>
    [TestFixture]
    public class CombatDefendTests
    {
        private CharacterClassSO _testClass;

        [SetUp]
        public void SetUp()
        {
            _testClass = ScriptableObject.CreateInstance<CharacterClassSO>();
            SetPrivateField(_testClass, "_className", "TestClass");
            SetPrivateField(_testClass, "_baseAC", 12);
            SetPrivateField(_testClass, "_hitDiceSides", 10);
            SetPrivateField(_testClass, "_baseStats", new StatBlock());
            SetPrivateField(_testClass, "_level1Abilities", new AbilitySO[0]);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testClass);
        }

        [Test]
        public void DefendAction_AddsBonusAC()
        {
            var unit = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            Assert.AreEqual(0, unit.BonusAC);

            unit.BonusAC = GameConstants.DEFEND_AC_BONUS;

            Assert.AreEqual(GameConstants.DEFEND_AC_BONUS, unit.BonusAC);
            Assert.AreEqual(12 + GameConstants.DEFEND_AC_BONUS, unit.EffectiveAC);
        }

        [Test]
        public void DefendBonus_ResetsOnTickTurnStart()
        {
            var unit = CombatUnit.FromPlayerCharacter("Hero", _testClass, 1);
            unit.BonusAC = GameConstants.DEFEND_AC_BONUS;

            Assert.AreEqual(GameConstants.DEFEND_AC_BONUS, unit.BonusAC);

            unit.TickTurnStart();

            Assert.AreEqual(0, unit.BonusAC);
            Assert.AreEqual(12, unit.EffectiveAC);
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
            Assert.Fail($"Field '{fieldName}' was not found on type '{obj.GetType().FullName}' or its base types.");
        }
    }
}
