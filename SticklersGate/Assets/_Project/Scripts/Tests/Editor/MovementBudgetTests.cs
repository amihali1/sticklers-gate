using System.Collections.Generic;
using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for split movement (several moves per turn from one budget),
    /// path cost lookup, and duplicate-name lettering.
    /// </summary>
    public class MovementBudgetTests
    {
        [Test]
        public void ResetMovement_FillsBudgetFromEffectiveMovement()
        {
            var unit = TestUnits.Player("Hero", new GridPosition(0, 0), movement: 4);
            unit.ResetMovement();
            Assert.AreEqual(4, unit.RemainingMovement);

            unit.ApplyStatusEffect(StatusEffectType.Slowed, 1);
            unit.ResetMovement();
            Assert.AreEqual(4 - GameConstants.SLOWED_MOVEMENT_PENALTY, unit.RemainingMovement);
        }

        [Test]
        public void SpendMovement_AllowsSeveralMovesUntilEmpty()
        {
            var unit = TestUnits.Player("Hero", new GridPosition(0, 0), movement: 3);
            unit.ResetMovement();

            unit.SpendMovement(1);
            Assert.AreEqual(2, unit.RemainingMovement);
            unit.SpendMovement(2);
            Assert.AreEqual(0, unit.RemainingMovement);
            unit.SpendMovement(1);
            Assert.AreEqual(0, unit.RemainingMovement, "Never negative.");
        }

        [Test]
        public void ReachableCosts_ChargeTerrainCost()
        {
            var grid = new CombatGrid(6, 1);
            grid.SetTerrain(new GridPosition(1, 0), TerrainType.Difficult);

            var costs = Pathfinder.GetReachableCosts(grid, new GridPosition(0, 0), 3);

            Assert.AreEqual(2, costs[new GridPosition(1, 0)], "Difficult terrain costs 2.");
            Assert.AreEqual(3, costs[new GridPosition(2, 0)]);
            Assert.IsFalse(costs.ContainsKey(new GridPosition(3, 0)), "Beyond the budget.");
            Assert.IsFalse(costs.ContainsKey(new GridPosition(0, 0)), "Start tile excluded.");
        }

        [Test]
        public void ReachableCosts_MatchReachablePositions()
        {
            var grid = new CombatGrid(8, 6);
            var start = new GridPosition(3, 3);

            var costs = Pathfinder.GetReachableCosts(grid, start, 3);
            var tiles = Pathfinder.GetReachablePositions(grid, start, 3);

            CollectionAssert.AreEquivalent(tiles, costs.Keys);
        }

        [Test]
        public void DisambiguateNames_LettersOnlyDuplicates()
        {
            var names = UnitNaming.DisambiguateNames(new List<string>
            {
                "Stick Goblin", "Stick Spider", "Stick Goblin", "Stick Goblin"
            });

            CollectionAssert.AreEqual(new[]
            {
                "Stick Goblin A", "Stick Spider", "Stick Goblin B", "Stick Goblin C"
            }, names);
        }

        [Test]
        public void FromEnemy_BlankOverrideFallsBackToEnemyName()
        {
            var unit = TestUnits.Enemy("Stick Goblin", new GridPosition(0, 0));
            var so = unit.EnemyData;

            Assert.AreEqual("Stick Goblin", CombatUnit.FromEnemy(so, "").Name);
            Assert.AreEqual("Stick Goblin", CombatUnit.FromEnemy(so, "   ").Name);
            Assert.AreEqual("Bone Guard", CombatUnit.FromEnemy(so, "Bone Guard").Name);
        }
    }
}
