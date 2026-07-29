using System;

namespace SticklersGate.Data
{
    /// <summary>
    /// An integer tile coordinate on the combat grid. Immutable value type.
    /// Movement and range use Manhattan distance (4-directional grid, no diagonals),
    /// matching Final Fantasy Tactics-style tactical movement.
    /// </summary>
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int x;
        public int y;

        public GridPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Manhattan (taxicab) distance to another position. This is the tile
        /// distance used for movement cost and ability range.
        /// </summary>
        public int ManhattanDistanceTo(GridPosition other)
        {
            return Math.Abs(x - other.x) + Math.Abs(y - other.y);
        }

        /// <summary>
        /// The four orthogonally adjacent positions (no diagonals). Bounds are
        /// not checked here — callers should validate against the grid.
        /// </summary>
        public GridPosition[] OrthogonalNeighbors()
        {
            return new[]
            {
                new GridPosition(x + 1, y),
                new GridPosition(x - 1, y),
                new GridPosition(x, y + 1),
                new GridPosition(x, y - 1)
            };
        }

        public bool Equals(GridPosition other) => x == other.x && y == other.y;

        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);

        public override int GetHashCode() => unchecked((x * 397) ^ y);

        public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);

        public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);

        public static GridPosition operator +(GridPosition a, GridPosition b)
            => new GridPosition(a.x + b.x, a.y + b.y);

        public static GridPosition operator -(GridPosition a, GridPosition b)
            => new GridPosition(a.x - b.x, a.y - b.y);

        public override string ToString() => $"({x}, {y})";
    }
}
