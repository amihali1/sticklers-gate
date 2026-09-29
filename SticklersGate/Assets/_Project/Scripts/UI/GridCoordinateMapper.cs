using System;
using UnityEngine;
using SticklersGate.Data;

namespace SticklersGate.UI
{
    /// <summary>
    /// Converts between grid tiles and world-space positions for the combat
    /// grid. Plain C# so it can be unit tested. Row 0 is the top row on screen
    /// and column 0 is the left-most column; the grid is centered on
    /// <see cref="Center"/>.
    /// </summary>
    public class GridCoordinateMapper
    {
        public int Width { get; }
        public int Height { get; }
        public float TileSize { get; }
        public Vector3 Center { get; }

        /// <summary>
        /// Creates a mapper for a grid of the given size.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If width, height, or tileSize is not positive.</exception>
        public GridCoordinateMapper(int width, int height, float tileSize, Vector3 center)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Grid width must be at least 1.");
            if (height < 1)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Grid height must be at least 1.");
            if (tileSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Tile size must be positive.");

            Width = width;
            Height = height;
            TileSize = tileSize;
            Center = center;
        }

        /// <summary>
        /// Largest tile size that fits a grid of the given dimensions inside a
        /// world-space area, capped at <paramref name="maxTileSize"/>.
        /// </summary>
        public static float FitTileSize(int width, int height, float areaWidth, float areaHeight, float maxTileSize)
        {
            float fit = Mathf.Min(areaWidth / Mathf.Max(1, width), areaHeight / Mathf.Max(1, height));
            return Mathf.Min(fit, maxTileSize);
        }

        /// <summary>World-space center of a tile. Does not bounds-check.</summary>
        public Vector3 GridToWorld(GridPosition pos)
        {
            float left = Center.x - Width * TileSize / 2f;
            float top = Center.y + Height * TileSize / 2f;
            return new Vector3(
                left + (pos.x + 0.5f) * TileSize,
                top - (pos.y + 0.5f) * TileSize,
                Center.z);
        }

        /// <summary>
        /// Tile containing a world-space point. Returns false if the point is
        /// outside the grid.
        /// </summary>
        public bool TryWorldToGrid(Vector3 world, out GridPosition pos)
        {
            float left = Center.x - Width * TileSize / 2f;
            float top = Center.y + Height * TileSize / 2f;
            int x = Mathf.FloorToInt((world.x - left) / TileSize);
            int y = Mathf.FloorToInt((top - world.y) / TileSize);

            pos = new GridPosition(x, y);
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}
