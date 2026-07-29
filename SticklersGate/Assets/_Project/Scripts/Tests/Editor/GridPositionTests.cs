using System.Collections.Generic;
using NUnit.Framework;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the GridPosition value type.
    /// </summary>
    public class GridPositionTests
    {
        [Test]
        public void ManhattanDistance_IsSumOfAxisDeltas()
        {
            var a = new GridPosition(1, 1);
            var b = new GridPosition(4, 3);
            Assert.AreEqual(5, a.ManhattanDistanceTo(b));
            Assert.AreEqual(5, b.ManhattanDistanceTo(a)); // symmetric
        }

        [Test]
        public void ManhattanDistance_ToSelfIsZero()
        {
            var a = new GridPosition(2, 7);
            Assert.AreEqual(0, a.ManhattanDistanceTo(a));
        }

        [Test]
        public void OrthogonalNeighbors_ReturnsFourAdjacentTiles()
        {
            var center = new GridPosition(3, 3);
            var neighbors = new HashSet<GridPosition>(center.OrthogonalNeighbors());

            Assert.AreEqual(4, neighbors.Count);
            Assert.IsTrue(neighbors.Contains(new GridPosition(4, 3)));
            Assert.IsTrue(neighbors.Contains(new GridPosition(2, 3)));
            Assert.IsTrue(neighbors.Contains(new GridPosition(3, 4)));
            Assert.IsTrue(neighbors.Contains(new GridPosition(3, 2)));
            // Diagonals are not neighbors.
            Assert.IsFalse(neighbors.Contains(new GridPosition(4, 4)));
        }

        [Test]
        public void Equality_MatchesOnBothAxes()
        {
            var a = new GridPosition(2, 5);
            var b = new GridPosition(2, 5);
            var c = new GridPosition(2, 6);

            Assert.IsTrue(a == b);
            Assert.IsTrue(a.Equals(b));
            Assert.IsFalse(a == c);
            Assert.IsTrue(a != c);
        }

        [Test]
        public void Equality_EqualPositionsShareHashCode()
        {
            var a = new GridPosition(7, 2);
            var b = new GridPosition(7, 2);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Addition_AndSubtraction_AreComponentwise()
        {
            var a = new GridPosition(3, 4);
            var b = new GridPosition(1, 2);
            Assert.AreEqual(new GridPosition(4, 6), a + b);
            Assert.AreEqual(new GridPosition(2, 2), a - b);
        }

        [Test]
        public void UsableAsDictionaryKey()
        {
            var map = new Dictionary<GridPosition, string>
            {
                [new GridPosition(1, 1)] = "a",
                [new GridPosition(2, 2)] = "b"
            };
            Assert.AreEqual("a", map[new GridPosition(1, 1)]);
            Assert.AreEqual("b", map[new GridPosition(2, 2)]);
        }
    }
}
