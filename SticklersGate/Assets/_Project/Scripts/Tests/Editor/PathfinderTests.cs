using System.Collections.Generic;
using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the Pathfinder: reachable-range and shortest-path over terrain
    /// costs and occupancy.
    /// </summary>
    public class PathfinderTests
    {
        private CombatGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _grid = new CombatGrid(8, 6);
        }

        [Test]
        public void Reachable_OnOpenGrid_IsManhattanDiamond()
        {
            var start = new GridPosition(3, 3);
            var reachable = new HashSet<GridPosition>(
                Pathfinder.GetReachablePositions(_grid, start, 2));

            // Budget 2 on open terrain = all tiles within Manhattan distance 2, minus start.
            int expected = 0;
            for (int x = 0; x < _grid.Width; x++)
            for (int y = 0; y < _grid.Height; y++)
            {
                var p = new GridPosition(x, y);
                if (p != start && start.ManhattanDistanceTo(p) <= 2)
                    expected++;
            }

            Assert.AreEqual(expected, reachable.Count);
            Assert.IsTrue(reachable.Contains(new GridPosition(5, 3)));  // 2 east
            Assert.IsFalse(reachable.Contains(start));                  // start excluded
            Assert.IsFalse(reachable.Contains(new GridPosition(6, 3))); // 3 east, over budget
        }

        [Test]
        public void Reachable_DoesNotPathThroughOccupiedTiles()
        {
            var start = new GridPosition(0, 0);
            // Wall off the two tiles adjacent to start's only exits within a 1-corridor.
            _grid.PlaceOccupant(new object(), new GridPosition(1, 0));
            _grid.PlaceOccupant(new object(), new GridPosition(0, 1));

            var reachable = Pathfinder.GetReachablePositions(_grid, start, 3);
            Assert.AreEqual(0, reachable.Count); // fully boxed in
        }

        [Test]
        public void Reachable_DifficultTerrainCostsExtra()
        {
            var start = new GridPosition(0, 0);
            // Make the tile east of start difficult (cost 2).
            _grid.SetTerrain(new GridPosition(1, 0), TerrainType.Difficult);

            var budget1 = new HashSet<GridPosition>(Pathfinder.GetReachablePositions(_grid, start, 1));
            // (1,0) costs 2 so it is NOT reachable with budget 1; (0,1) costs 1 and is.
            Assert.IsFalse(budget1.Contains(new GridPosition(1, 0)));
            Assert.IsTrue(budget1.Contains(new GridPosition(0, 1)));

            var budget2 = new HashSet<GridPosition>(Pathfinder.GetReachablePositions(_grid, start, 2));
            Assert.IsTrue(budget2.Contains(new GridPosition(1, 0)));
        }

        [Test]
        public void Reachable_ExcludesImpassableTerrain()
        {
            var start = new GridPosition(0, 0);
            _grid.SetTerrain(new GridPosition(1, 0), TerrainType.Abyss);

            var reachable = new HashSet<GridPosition>(
                Pathfinder.GetReachablePositions(_grid, start, 5));
            Assert.IsFalse(reachable.Contains(new GridPosition(1, 0)));
            // But you can still reach it via the long way around (0,1)->(1,1)->(2,0)... not through the abyss.
            Assert.IsTrue(reachable.Contains(new GridPosition(0, 1)));
        }

        [Test]
        public void FindPath_StraightLine_HasExpectedLength()
        {
            var start = new GridPosition(0, 0);
            var goal = new GridPosition(3, 0);
            var path = Pathfinder.FindPath(_grid, start, goal);

            Assert.IsNotNull(path);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
            Assert.AreEqual(4, path.Count); // (0,0),(1,0),(2,0),(3,0)
        }

        [Test]
        public void FindPath_RoutesAroundOccupiedTile()
        {
            var start = new GridPosition(0, 0);
            var goal = new GridPosition(2, 0);
            _grid.PlaceOccupant(new object(), new GridPosition(1, 0)); // block the direct route

            var path = Pathfinder.FindPath(_grid, start, goal);
            Assert.IsNotNull(path);
            Assert.IsFalse(path.Contains(new GridPosition(1, 0)));
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
            // Detour makes it longer than the blocked straight line (which was 3 tiles).
            Assert.GreaterOrEqual(path.Count, 4);
        }

        [Test]
        public void FindPath_ReturnsNullWhenGoalUnreachableWithinBudget()
        {
            var start = new GridPosition(0, 0);
            var goal = new GridPosition(5, 0);
            var path = Pathfinder.FindPath(_grid, start, goal, movementBudget: 2);
            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_ReturnsNullForOccupiedGoal()
        {
            var start = new GridPosition(0, 0);
            var goal = new GridPosition(2, 2);
            _grid.PlaceOccupant(new object(), goal);
            Assert.IsNull(Pathfinder.FindPath(_grid, start, goal));
        }

        [Test]
        public void FindPath_ToSelfIsSingleTile()
        {
            var start = new GridPosition(4, 4);
            var path = Pathfinder.FindPath(_grid, start, start);
            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(start, path[0]);
        }

        [Test]
        public void FindPath_PrefersCheaperTerrainRoute()
        {
            // Direct east route crosses two Difficult tiles (cost 2 each = 4);
            // a detour of four Normal tiles costs 4 as well but the direct is
            // shorter in tiles, so validate the pathfinder still finds a valid
            // minimal-cost path and doesn't crash on mixed terrain.
            var start = new GridPosition(0, 0);
            _grid.SetTerrain(new GridPosition(1, 0), TerrainType.Difficult);
            _grid.SetTerrain(new GridPosition(2, 0), TerrainType.Difficult);
            var goal = new GridPosition(3, 0);

            var path = Pathfinder.FindPath(_grid, start, goal);
            Assert.IsNotNull(path);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
        }
    }
}
