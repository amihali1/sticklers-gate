using System;
using UnityEngine;

namespace SticklersGate.Data
{
    /// <summary>
    /// A single terrain override for a tile. Tiles without an entry default to
    /// <see cref="TerrainType.Normal"/>.
    /// </summary>
    [Serializable]
    public struct TerrainCell
    {
        public GridPosition position;
        public TerrainType terrain;
    }

    /// <summary>
    /// Per-encounter grid configuration: dimensions, terrain overrides, and the
    /// spawn tiles for the player party and enemies. Pure data — the runtime
    /// <c>CombatGrid</c> is built from this via <c>CombatGrid.FromLayout</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGridLayout", menuName = "SticklersGate/GridLayout")]
    public class GridLayoutSO : ScriptableObject
    {
        [Header("Dimensions")]
        [SerializeField][Range(1, GameConstants.MAX_GRID_WIDTH)]
        private int _gridWidth = GameConstants.DEFAULT_GRID_WIDTH;
        [SerializeField][Range(1, GameConstants.MAX_GRID_HEIGHT)]
        private int _gridHeight = GameConstants.DEFAULT_GRID_HEIGHT;

        [Header("Terrain (tiles omitted default to Normal)")]
        [SerializeField] private TerrainCell[] _terrainOverrides = Array.Empty<TerrainCell>();

        [Header("Spawn Points")]
        [SerializeField] private GridPosition[] _playerSpawns = Array.Empty<GridPosition>();
        [SerializeField] private GridPosition[] _enemySpawns = Array.Empty<GridPosition>();

        // --- Public Properties ---

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public TerrainCell[] TerrainOverrides => _terrainOverrides;
        public GridPosition[] PlayerSpawns => _playerSpawns;
        public GridPosition[] EnemySpawns => _enemySpawns;
    }
}
