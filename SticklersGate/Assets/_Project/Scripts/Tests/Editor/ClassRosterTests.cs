using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Checks every CharacterClassSO asset against the roster conventions in
    /// CONTENT.md: one asset per class type, "Stick" naming, stat budget,
    /// D&amp;D hit dice, and a complete ability progression.
    /// </summary>
    public class ClassRosterTests
    {
        private const string DATA_ROOT = "Assets/_Project/Data";
        private const int MIN_STAT_TOTAL = 70;
        private const int MAX_STAT_TOTAL = 80;
        private const int MAX_STARTING_STAT = 18;

        private static readonly int[] DND_HIT_DICE = { 6, 8, 10, 12 };

        private static List<CharacterClassSO> Classes()
        {
            return AssetDatabase.FindAssets("t:CharacterClassSO", new[] { DATA_ROOT })
                .Select(g => AssetDatabase.LoadAssetAtPath<CharacterClassSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null)
                .ToList();
        }

        [Test]
        public void EachClassType_HasExactlyOneAsset()
        {
            var classes = Classes();
            foreach (var group in classes.GroupBy(c => c.ClassType))
            {
                Assert.AreEqual(1, group.Count(), $"{group.Key} has {group.Count()} class assets.");
            }
        }

        [Test]
        public void ClassName_MatchesClassType()
        {
            foreach (var cls in Classes())
            {
                Assert.AreEqual(cls.ClassType.ToString(), cls.ClassName, cls.name);
                StringAssert.StartsWith("Stick", cls.ClassName, "Keep the Stick pun prefix.");
            }
        }

        [Test]
        public void Stats_StayWithinTheRosterBudget()
        {
            foreach (var cls in Classes())
            {
                var s = cls.BaseStats;
                int[] scores = { s.strength, s.dexterity, s.constitution, s.intelligence, s.wisdom, s.charisma };
                Assert.That(scores.Sum(), Is.InRange(MIN_STAT_TOTAL, MAX_STAT_TOTAL), $"{cls.ClassName} stat total.");
                Assert.LessOrEqual(scores.Max(), MAX_STARTING_STAT, $"{cls.ClassName} has a stat above 18.");
            }
        }

        [Test]
        public void HitDie_IsADndHitDie()
        {
            foreach (var cls in Classes())
            {
                CollectionAssert.Contains(DND_HIT_DICE, cls.HitDiceSides, cls.ClassName);
            }
        }

        [Test]
        public void EveryClass_HasAbilitiesAtLevels1And5()
        {
            foreach (var cls in Classes())
            {
                var level1 = cls.GetAbilitiesAtLevel(1);
                var level5 = cls.GetAbilitiesAtLevel(5);

                Assert.IsNotEmpty(level1, $"{cls.ClassName} has no level 1 abilities.");
                Assert.Greater(level5.Length, level1.Length, $"{cls.ClassName} gains nothing by level 5.");
                Assert.IsFalse(level5.Any(a => a == null), $"{cls.ClassName} references a missing ability asset.");
            }
        }

        [Test]
        public void EveryClass_CanDealDamageAtLevel1()
        {
            foreach (var cls in Classes())
            {
                Assert.IsTrue(cls.GetAbilitiesAtLevel(1).Any(a => a.DealsDamage),
                    $"{cls.ClassName} can't attack at level 1.");
            }
        }

        [Test]
        public void EveryClass_BuildsAUsableCombatUnit()
        {
            foreach (var cls in Classes())
            {
                var unit = CombatUnit.FromPlayerCharacter(cls.ClassName, cls, 5);
                Assert.Greater(unit.MaxHP, 0, cls.ClassName);
                Assert.Greater(unit.EffectiveMovement, 0, cls.ClassName);
                Assert.AreEqual(cls.GetAbilitiesAtLevel(5).Length, unit.Abilities.Count, cls.ClassName);
            }
        }
    }
}
