using System;
using NUnit.Framework;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the StatBlock struct.
    /// </summary>
    public class StatBlockTests
    {
        private StatBlock _stats;

        [SetUp]
        public void SetUp()
        {
            _stats = new StatBlock
            {
                strength = 16,
                dexterity = 14,
                constitution = 12,
                intelligence = 8,
                wisdom = 10,
                charisma = 18
            };
        }

        [Test]
        public void GetScore_ReturnsCorrectValues()
        {
            Assert.AreEqual(16, _stats.GetScore(AbilityScore.Strength));
            Assert.AreEqual(14, _stats.GetScore(AbilityScore.Dexterity));
            Assert.AreEqual(12, _stats.GetScore(AbilityScore.Constitution));
            Assert.AreEqual(8, _stats.GetScore(AbilityScore.Intelligence));
            Assert.AreEqual(10, _stats.GetScore(AbilityScore.Wisdom));
            Assert.AreEqual(18, _stats.GetScore(AbilityScore.Charisma));
        }

        [Test]
        public void GetScore_ThrowsOnInvalidAbility()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _stats.GetScore((AbilityScore)99);
            });
        }

        [Test]
        public void GetModifier_CalculatesCorrectly()
        {
            Assert.AreEqual(3, _stats.GetModifier(AbilityScore.Strength));   // 16 → +3
            Assert.AreEqual(2, _stats.GetModifier(AbilityScore.Dexterity));  // 14 → +2
            Assert.AreEqual(1, _stats.GetModifier(AbilityScore.Constitution)); // 12 → +1
            Assert.AreEqual(-1, _stats.GetModifier(AbilityScore.Intelligence)); // 8 → -1
            Assert.AreEqual(0, _stats.GetModifier(AbilityScore.Wisdom));     // 10 → 0
            Assert.AreEqual(4, _stats.GetModifier(AbilityScore.Charisma));   // 18 → +4
        }

        [Test]
        public void GetModifier_FloorsNegativeCorrectly()
        {
            var stats = new StatBlock { strength = 9 }; // (9 - 10) / 2 = -0.5 → should be -1
            Assert.AreEqual(-1, stats.GetModifier(AbilityScore.Strength));

            stats.strength = 7; // (7 - 10) / 2 = -1.5 → should be -2
            Assert.AreEqual(-2, stats.GetModifier(AbilityScore.Strength));
        }
    }
}
