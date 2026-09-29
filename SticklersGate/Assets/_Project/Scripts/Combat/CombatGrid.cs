using System;
using System.Collections.Generic;
using SticklersGate.Data;

namespace SticklersGate.Combat
{
    /// <summary>
    /// A rectangular tactical combat grid. Owns the terrain map and tile
    /// occupancy. Occupants are stored as <see cref="object"/> so this class has
    /// no dependency on combat runtime types; callers pass their own unit
    /// references (e.g. CombatUnit in PR B) and cast on the way out.
    /// </summary>
    public class CombatGrid
    {
        private readonly TerrainType[,] _terrain;
        private readonly Dictionary<GridPosition, object> _occupantByPosition = new();
        private readonly Dictionary<object, GridPosition> _positionByOccupant = new();

        public int Width { get; }
        public int Height { get; }

        /// <summary>
        /// Creates a grid of the given size with every tile set to Normal terrain.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If width or height is &lt; 1.</exception>
        public CombatGrid(int width, int height)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Grid width must be at least 1.");
            if (height < 1)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Grid height must be at least 1.");

            Width = width;
            Height = height;
            _terrain = new TerrainType[width, height];
            // TerrainType.Normal is the enum's zero value, so the array is already Normal-filled.
        }

        /// <summary>
        /// Builds a grid from a <see cref="GridLayoutSO"/>, applying its terrain
        /// overrides. Spawn points are exposed on the layout for the caller to
        /// place units; this method does not place occupants.
        /// </summary>
        /// <exception cref="ArgumentNullException">If layout is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If a terrain override is out of bounds.</exception>
        public static CombatGrid FromLayout(GridLayoutSO layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            var grid = new CombatGrid(layout.GridWidth, layout.GridHeight);
            foreach (var cell in layout.TerrainOverrides)
                grid.SetTerrain(cell.position, cell.terrain);

            return grid;
        }

        /// <summary>True if the position lies inside the grid bounds.</summary>
        public bool IsInBounds(GridPosition pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }

        /// <summary>Returns the terrain at a position.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If the position is out of bounds.</exception>
        public TerrainType GetTerrain(GridPosition pos)
        {
            RequireInBounds(pos);
            return _terrain[pos.x, pos.y];
        }

        /// <summary>Sets the terrain at a position.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If the position is out of bounds.</exception>
        public void SetTerrain(GridPosition pos, TerrainType terrain)
        {
            RequireInBounds(pos);
            _terrain[pos.x, pos.y] = terrain;
        }

        /// <summary>
        /// True if the terrain itself can be stood on (everything except Abyss).
        /// Ignores occupancy — use <see cref="IsWalkable"/> for pathing.
        /// </summary>
        public bool IsPassableTerrain(GridPosition pos)
        {
            return IsInBounds(pos) && _terrain[pos.x, pos.y] != TerrainType.Abyss;
        }

        /// <summary>
        /// True if a unit could occupy this tile right now: in bounds, passable
        /// terrain, and not already occupied.
        /// </summary>
        public bool IsWalkable(GridPosition pos)
        {
            return IsPassableTerrain(pos) && !IsOccupied(pos);
        }

        /// <summary>
        /// Movement cost to enter a tile. Difficult and Water cost 2, everything
        /// else costs 1. Impassable terrain returns <see cref="int.MaxValue"/>;
        /// callers should gate on <see cref="IsWalkable"/> first.
        /// </summary>
        public int GetMovementCost(GridPosition pos)
        {
            if (!IsPassableTerrain(pos))
                return int.MaxValue;

            var terrain = _terrain[pos.x, pos.y];
            return terrain == TerrainType.Difficult || terrain == TerrainType.Water
                ? GameConstants.DIFFICULT_MOVEMENT_COST
                : GameConstants.NORMAL_MOVEMENT_COST;
        }

        /// <summary>True if any occupant stands on this tile.</summary>
        public bool IsOccupied(GridPosition pos) => _occupantByPosition.ContainsKey(pos);

        /// <summary>Returns the occupant at a position, or null if the tile is empty.</summary>
        public object GetOccupant(GridPosition pos)
        {
            return _occupantByPosition.TryGetValue(pos, out var occupant) ? occupant : null;
        }

        /// <summary>Looks up where an occupant currently stands.</summary>
        public bool TryGetPosition(object occupant, out GridPosition pos)
        {
            return _positionByOccupant.TryGetValue(occupant, out pos);
        }

        /// <summary>
        /// Places an occupant on a tile. The tile must be walkable and the
        /// occupant must not already be on the grid.
        /// </summary>
        /// <exception cref="ArgumentNullException">If occupant is null.</exception>
        /// <exception cref="ArgumentException">If the tile is not walkable or the occupant is already placed.</exception>
        public void PlaceOccupant(object occupant, GridPosition pos)
        {
            if (occupant == null)
                throw new ArgumentNullException(nameof(occupant));
            if (_positionByOccupant.ContainsKey(occupant))
                throw new ArgumentException("Occupant is already on the grid; use MoveOccupant instead.", nameof(occupant));
            if (!IsWalkable(pos))
                throw new ArgumentException($"Tile {pos} is not walkable (out of bounds, impassable, or occupied).", nameof(pos));

            _occupantByPosition[pos] = occupant;
            _positionByOccupant[occupant] = pos;
        }

        /// <summary>
        /// Moves an already-placed occupant to a new tile. The destination must be
        /// walkable (unless it is the occupant's current tile, which is a no-op).
        /// </summary>
        /// <exception cref="ArgumentException">If the occupant is not on the grid or the destination is not walkable.</exception>
        public void MoveOccupant(object occupant, GridPosition to)
        {
            if (!_positionByOccupant.TryGetValue(occupant, out var from))
                throw new ArgumentException("Occupant is not on the grid; use PlaceOccupant first.", nameof(occupant));
            if (from == to)
                return;
            if (!IsWalkable(to))
                throw new ArgumentException($"Tile {to} is not walkable (out of bounds, impassable, or occupied).", nameof(to));

            _occupantByPosition.Remove(from);
            _occupantByPosition[to] = occupant;
            _positionByOccupant[occupant] = to;
        }

        /// <summary>
        /// All in-bounds tiles within <paramref name="distance"/> (Manhattan) of
        /// <paramref name="origin"/>, excluding the origin. Ignores terrain and
        /// occupancy — this is ability reach, not movement.
        /// </summary>
        public List<GridPosition> GetPositionsWithinDistance(GridPosition origin, int distance)
        {
            var result = new List<GridPosition>();
            for (int dx = -distance; dx <= distance; dx++)
            {
                int remaining = distance - Math.Abs(dx);
                for (int dy = -remaining; dy <= remaining; dy++)
                {
                    var pos = new GridPosition(origin.x + dx, origin.y + dy);
                    if (pos != origin && IsInBounds(pos))
                        result.Add(pos);
                }
            }
            return result;
        }

        /// <summary>Removes an occupant from the grid. No-op if not present.</summary>
        public void RemoveOccupant(object occupant)
        {
            if (occupant != null && _positionByOccupant.TryGetValue(occupant, out var pos))
            {
                _positionByOccupant.Remove(occupant);
                _occupantByPosition.Remove(pos);
            }
        }

        private void RequireInBounds(GridPosition pos)
        {
            if (!IsInBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), pos, $"Position is outside the {Width}x{Height} grid.");
        }
    }
}
