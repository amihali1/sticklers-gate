using System;
using NUnit.Framework;
using UnityEngine;
using SticklersGate.Data;
using SticklersGate.UI;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for GridCoordinateMapper: grid↔world conversion and tile fitting.
    /// </summary>
    public class GridCoordinateMapperTests
    {
        private const float EPSILON = 0.0001f;

        [Test]
        public void Constructor_ThrowsOnInvalidArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridCoordinateMapper(0, 6, 1f, Vector3.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridCoordinateMapper(8, 0, 1f, Vector3.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridCoordinateMapper(8, 6, 0f, Vector3.zero));
        }

        [Test]
        public void GridToWorld_TopLeftTile_IsUpperLeftOfCenter()
        {
            var mapper = new GridCoordinateMapper(8, 6, 1f, Vector3.zero);

            Vector3 world = mapper.GridToWorld(new GridPosition(0, 0));

            Assert.AreEqual(-3.5f, world.x, EPSILON);
            Assert.AreEqual(2.5f, world.y, EPSILON);
        }

        [Test]
        public void GridToWorld_BottomRightTile_IsLowerRightOfCenter()
        {
            var mapper = new GridCoordinateMapper(8, 6, 1f, Vector3.zero);

            Vector3 world = mapper.GridToWorld(new GridPosition(7, 5));

            Assert.AreEqual(3.5f, world.x, EPSILON);
            Assert.AreEqual(-2.5f, world.y, EPSILON);
        }

        [Test]
        public void GridToWorld_RespectsTileSizeAndCenter()
        {
            var mapper = new GridCoordinateMapper(4, 2, 2f, new Vector3(10f, -1f, 0f));

            Vector3 world = mapper.GridToWorld(new GridPosition(0, 0));

            // left edge = 10 - 4, top edge = -1 + 2; tile center half a tile in
            Assert.AreEqual(7f, world.x, EPSILON);
            Assert.AreEqual(0f, world.y, EPSILON);
        }

        [Test]
        public void TryWorldToGrid_RoundTripsEveryTile()
        {
            var mapper = new GridCoordinateMapper(8, 6, 0.9f, new Vector3(0.3f, -0.2f, 0f));

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    var pos = new GridPosition(x, y);
                    Assert.IsTrue(mapper.TryWorldToGrid(mapper.GridToWorld(pos), out var back));
                    Assert.AreEqual(pos, back);
                }
            }
        }

        [Test]
        public void TryWorldToGrid_OutsideGrid_ReturnsFalse()
        {
            var mapper = new GridCoordinateMapper(8, 6, 1f, Vector3.zero);

            Assert.IsFalse(mapper.TryWorldToGrid(new Vector3(-4.1f, 0f, 0f), out _));
            Assert.IsFalse(mapper.TryWorldToGrid(new Vector3(4.1f, 0f, 0f), out _));
            Assert.IsFalse(mapper.TryWorldToGrid(new Vector3(0f, 3.1f, 0f), out _));
            Assert.IsFalse(mapper.TryWorldToGrid(new Vector3(0f, -3.1f, 0f), out _));
        }

        [Test]
        public void TryWorldToGrid_PointNearTileEdge_MapsToContainingTile()
        {
            var mapper = new GridCoordinateMapper(8, 6, 1f, Vector3.zero);

            // Just right of the vertical line between columns 3 and 4, just below the top edge
            Assert.IsTrue(mapper.TryWorldToGrid(new Vector3(0.01f, 2.99f, 0f), out var pos));
            Assert.AreEqual(new GridPosition(4, 0), pos);
        }

        [Test]
        public void FitTileSize_LimitedByTighterDimension()
        {
            // Width allows 12/8 = 1.5, height allows 6/6 = 1.0
            Assert.AreEqual(1f, GridCoordinateMapper.FitTileSize(8, 6, 12f, 6f, 5f), EPSILON);
        }

        [Test]
        public void FitTileSize_CappedAtMax()
        {
            Assert.AreEqual(1.2f, GridCoordinateMapper.FitTileSize(2, 2, 20f, 20f, 1.2f), EPSILON);
        }
    }
}
