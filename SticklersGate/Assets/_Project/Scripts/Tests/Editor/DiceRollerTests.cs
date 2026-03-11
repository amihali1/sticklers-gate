using NUnit.Framework;
using SticklersGate.Data;
using SticklersGate.Utils;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the DiceRoller utility.
    /// </summary>
    public class DiceRollerTests
    {
        [SetUp]
        public void SetUp()
        {
            DiceRoller.ClearHistory();
        }

        [Test]
        public void Roll_ReturnsWithinRange()
        {
            for (int i = 0; i < 100; i++)
            {
                int result = DiceRoller.Roll(6);
                Assert.GreaterOrEqual(result, 1);
                Assert.LessOrEqual(result, 6);
            }
        }

        [Test]
        public void Roll_ZeroSides_ReturnsZero()
        {
            Assert.AreEqual(0, DiceRoller.Roll(0));
        }

        [Test]
        public void Roll_NegativeSides_ReturnsZero()
        {
            Assert.AreEqual(0, DiceRoller.Roll(-5));
        }

        [Test]
        public void RollMultiple_ReturnsSumInRange()
        {
            for (int i = 0; i < 50; i++)
            {
                int result = DiceRoller.RollMultiple(3, 6);
                Assert.GreaterOrEqual(result, 3);
                Assert.LessOrEqual(result, 18);
            }
        }

        [Test]
        public void RollD20_ReturnsWithinRange()
        {
            for (int i = 0; i < 100; i++)
            {
                int result = DiceRoller.RollD20();
                Assert.GreaterOrEqual(result, 1);
                Assert.LessOrEqual(result, 20);
            }
        }

        [Test]
        public void IsCriticalHit_TrueOnNat20()
        {
            Assert.IsTrue(DiceRoller.IsCriticalHit(20));
        }

        [Test]
        public void IsCriticalHit_FalseOnOtherRolls()
        {
            Assert.IsFalse(DiceRoller.IsCriticalHit(19));
            Assert.IsFalse(DiceRoller.IsCriticalHit(1));
        }

        [Test]
        public void IsCriticalFail_TrueOnNat1()
        {
            Assert.IsTrue(DiceRoller.IsCriticalFail(1));
        }

        [Test]
        public void IsCriticalFail_FalseOnOtherRolls()
        {
            Assert.IsFalse(DiceRoller.IsCriticalFail(2));
            Assert.IsFalse(DiceRoller.IsCriticalFail(20));
        }

        [Test]
        public void GetModifier_CalculatesCorrectly()
        {
            Assert.AreEqual(0, DiceRoller.GetModifier(10));
            Assert.AreEqual(0, DiceRoller.GetModifier(11));
            Assert.AreEqual(1, DiceRoller.GetModifier(12));
            Assert.AreEqual(4, DiceRoller.GetModifier(18));
            Assert.AreEqual(-1, DiceRoller.GetModifier(9));
            Assert.AreEqual(-1, DiceRoller.GetModifier(8));
            Assert.AreEqual(-2, DiceRoller.GetModifier(7));
            Assert.AreEqual(-5, DiceRoller.GetModifier(1));
        }

        [Test]
        public void RollHistory_TracksRolls()
        {
            DiceRoller.Roll(6);
            DiceRoller.Roll(6);
            DiceRoller.Roll(6);

            Assert.AreEqual(3, DiceRoller.RollHistory.Count);
        }

        [Test]
        public void RollHistory_CapsAt20()
        {
            for (int i = 0; i < 25; i++)
            {
                DiceRoller.Roll(6);
            }

            Assert.AreEqual(20, DiceRoller.RollHistory.Count);
        }

        [Test]
        public void ClearHistory_EmptiesHistory()
        {
            DiceRoller.Roll(6);
            DiceRoller.Roll(6);
            DiceRoller.ClearHistory();

            Assert.AreEqual(0, DiceRoller.RollHistory.Count);
        }
    }
}
