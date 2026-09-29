using System;
using NUnit.Framework;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for the CombatGrid: bounds, terrain, occupancy, and movement cost.
    /// </summary>
    public class CombatGridTests
    {
        private CombatGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _grid = new CombatGrid(8, 6);
        }

        [Test]
        public void Constructor_ThrowsOnNonPositiveDimensions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatGrid(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatGrid(5, -1));
        }

        [Test]
        public void IsInBounds_RespectsGridEdges()
        {
            Assert.IsTrue(_grid.IsInBounds(new GridPosition(0, 0)));
            Assert.IsTrue(_grid.IsInBounds(new GridPosition(7, 5)));
            Assert.IsFalse(_grid.IsInBounds(new GridPosition(8, 5)));
            Assert.IsFalse(_grid.IsInBounds(new GridPosition(0, 6)));
            Assert.IsFalse(_grid.IsInBounds(new GridPosition(-1, 0)));
        }

        [Test]
        public void Terrain_DefaultsToNormal()
        {
            Assert.AreEqual(TerrainType.Normal, _grid.GetTerrain(new GridPosition(3, 3)));
        }

        [Test]
        public void SetTerrain_PersistsAndOutOfBoundsThrows()
        {
            var pos = new GridPosition(2, 2);
            _grid.SetTerrain(pos, TerrainType.Lava);
            Assert.AreEqual(TerrainType.Lava, _grid.GetTerrain(pos));
            Assert.Throws<ArgumentOutOfRangeException>(() => _grid.GetTerrain(new GridPosition(99, 99)));
        }

        [Test]
        public void Abyss_IsImpassableTerrain()
        {
            var pos = new GridPosition(4, 4);
            _grid.SetTerrain(pos, TerrainType.Abyss);
            Assert.IsFalse(_grid.IsPassableTerrain(pos));
            Assert.IsFalse(_grid.IsWalkable(pos));
            Assert.AreEqual(int.MaxValue, _grid.GetMovementCost(pos));
        }

        [Test]
        public void MovementCost_DifficultAndWaterCostTwo()
        {
            _grid.SetTerrain(new GridPosition(1, 1), TerrainType.Difficult);
            _grid.SetTerrain(new GridPosition(1, 2), TerrainType.Water);
            Assert.AreEqual(GameConstants.NORMAL_MOVEMENT_COST, _grid.GetMovementCost(new GridPosition(0, 0)));
            Assert.AreEqual(GameConstants.DIFFICULT_MOVEMENT_COST, _grid.GetMovementCost(new GridPosition(1, 1)));
            Assert.AreEqual(GameConstants.DIFFICULT_MOVEMENT_COST, _grid.GetMovementCost(new GridPosition(1, 2)));
        }

        [Test]
        public void PlaceOccupant_MarksTileOccupiedAndTracksPosition()
        {
            var unit = new object();
            var pos = new GridPosition(3, 3);
            _grid.PlaceOccupant(unit, pos);

            Assert.IsTrue(_grid.IsOccupied(pos));
            Assert.IsFalse(_grid.IsWalkable(pos));
            Assert.AreSame(unit, _grid.GetOccupant(pos));
            Assert.IsTrue(_grid.TryGetPosition(unit, out var found));
            Assert.AreEqual(pos, found);
        }

        [Test]
        public void PlaceOccupant_RejectsOccupiedTile()
        {
            var pos = new GridPosition(3, 3);
            _grid.PlaceOccupant(new object(), pos);
            Assert.Throws<ArgumentException>(() => _grid.PlaceOccupant(new object(), pos));
        }

        [Test]
        public void PlaceOccupant_RejectsImpassableAndNull()
        {
            _grid.SetTerrain(new GridPosition(2, 2), TerrainType.Abyss);
            Assert.Throws<ArgumentException>(() => _grid.PlaceOccupant(new object(), new GridPosition(2, 2)));
            Assert.Throws<ArgumentNullException>(() => _grid.PlaceOccupant(null, new GridPosition(0, 0)));
        }

        [Test]
        public void PlaceOccupant_RejectsDoublePlacementOfSameOccupant()
        {
            var unit = new object();
            _grid.PlaceOccupant(unit, new GridPosition(1, 1));
            Assert.Throws<ArgumentException>(() => _grid.PlaceOccupant(unit, new GridPosition(2, 2)));
        }

        [Test]
        public void MoveOccupant_UpdatesBothTiles()
        {
            var unit = new object();
            var from = new GridPosition(1, 1);
            var to = new GridPosition(1, 2);
            _grid.PlaceOccupant(unit, from);
            _grid.MoveOccupant(unit, to);

            Assert.IsFalse(_grid.IsOccupied(from));
            Assert.IsTrue(_grid.IsOccupied(to));
            Assert.IsTrue(_grid.TryGetPosition(unit, out var found));
            Assert.AreEqual(to, found);
        }

        [Test]
        public void MoveOccupant_ToSameTileIsNoOp()
        {
            var unit = new object();
            var pos = new GridPosition(4, 4);
            _grid.PlaceOccupant(unit, pos);
            Assert.DoesNotThrow(() => _grid.MoveOccupant(unit, pos));
            Assert.IsTrue(_grid.IsOccupied(pos));
        }

        [Test]
        public void MoveOccupant_RejectsUnplacedUnitAndOccupiedDestination()
        {
            Assert.Throws<ArgumentException>(() => _grid.MoveOccupant(new object(), new GridPosition(0, 0)));

            var a = new object();
            var b = new object();
            _grid.PlaceOccupant(a, new GridPosition(1, 1));
            _grid.PlaceOccupant(b, new GridPosition(2, 2));
            Assert.Throws<ArgumentException>(() => _grid.MoveOccupant(a, new GridPosition(2, 2)));
        }

        [Test]
        public void RemoveOccupant_FreesTile()
        {
            var unit = new object();
            var pos = new GridPosition(3, 3);
            _grid.PlaceOccupant(unit, pos);
            _grid.RemoveOccupant(unit);

            Assert.IsFalse(_grid.IsOccupied(pos));
            Assert.IsFalse(_grid.TryGetPosition(unit, out _));
            Assert.DoesNotThrow(() => _grid.RemoveOccupant(new object())); // no-op for unknown
        }

        [Test]
        public void FromLayout_AppliesDimensionsAndTerrain()
        {
            var layout = ScriptableObject.CreateInstance<GridLayoutSO>();
            SetPrivate(layout, "_gridWidth", 10);
            SetPrivate(layout, "_gridHeight", 7);
            SetPrivate(layout, "_terrainOverrides", new[]
            {
                new TerrainCell { position = new GridPosition(2, 3), terrain = TerrainType.Cover }
            });

            var grid = CombatGrid.FromLayout(layout);
            Assert.AreEqual(10, grid.Width);
            Assert.AreEqual(7, grid.Height);
            Assert.AreEqual(TerrainType.Cover, grid.GetTerrain(new GridPosition(2, 3)));
            Assert.AreEqual(TerrainType.Normal, grid.GetTerrain(new GridPosition(0, 0)));
        }

        [Test]
        public void FromLayout_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => CombatGrid.FromLayout(null));
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var info = target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(info, $"Field {field} not found.");
            info.SetValue(target, value);
        }
    }
}
