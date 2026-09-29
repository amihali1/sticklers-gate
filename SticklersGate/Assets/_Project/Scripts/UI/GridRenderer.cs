using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.UI
{
    /// <summary>
    /// Draws the combat grid as world-space tiles, overlays movement / ability
    /// range highlights, and reports tile clicks. Coordinate math lives in
    /// <see cref="GridCoordinateMapper"/>; this component only owns visuals and input.
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        /// <summary>Kinds of tile highlight the renderer can show.</summary>
        public enum HighlightKind
        {
            None,
            Movement,
            AbilityRange,
            ActiveUnit
        }

        [Header("Tile Look")]
        [SerializeField] private float _tileGap = 0.06f;
        [SerializeField] private Color _gridLineColor = new Color(0.55f, 0.52f, 0.46f);

        [Header("Terrain Colors")]
        [SerializeField] private Color _normalColor = new Color(0.9f, 0.88f, 0.82f);
        [SerializeField] private Color _difficultColor = new Color(0.72f, 0.62f, 0.45f);
        [SerializeField] private Color _elevatedColor = new Color(0.68f, 0.72f, 0.66f);
        [SerializeField] private Color _waterColor = new Color(0.55f, 0.72f, 0.9f);
        [SerializeField] private Color _lavaColor = new Color(0.95f, 0.45f, 0.25f);
        [SerializeField] private Color _abyssColor = new Color(0.15f, 0.15f, 0.18f);
        [SerializeField] private Color _coverColor = new Color(0.6f, 0.6f, 0.6f);

        [Header("Highlight Colors")]
        [SerializeField] private Color _movementColor = new Color(0.25f, 0.5f, 1f, 0.45f);
        [SerializeField] private Color _abilityRangeColor = new Color(1f, 0.3f, 0.25f, 0.35f);
        [SerializeField] private Color _activeUnitColor = new Color(1f, 0.85f, 0.2f, 0.5f);

        /// <summary>Fired when the player clicks/taps a tile inside the grid.</summary>
        public event Action<GridPosition> OnTileClicked;

        /// <summary>Coordinate mapping for the current grid (null before <see cref="Build"/>).</summary>
        public GridCoordinateMapper Mapper { get; private set; }

        private const int TILE_SORTING_ORDER = -10;
        private const int HIGHLIGHT_SORTING_ORDER = -9;

        private static Sprite _squareSprite;

        private readonly Dictionary<GridPosition, SpriteRenderer> _highlights = new Dictionary<GridPosition, SpriteRenderer>();
        private Camera _camera;

        /// <summary>
        /// Build tile visuals for the grid. Clears any previously built tiles.
        /// </summary>
        public void Build(CombatGrid grid, GridCoordinateMapper mapper, Camera camera)
        {
            Clear();
            Mapper = mapper;
            _camera = camera;

            // Background slab shows through tile gaps as grid lines
            var background = CreateSquare("GridBackground", transform, TILE_SORTING_ORDER - 1);
            background.color = _gridLineColor;
            background.transform.position = mapper.Center;
            background.transform.localScale = new Vector3(
                mapper.Width * mapper.TileSize + _tileGap,
                mapper.Height * mapper.TileSize + _tileGap, 1f);

            float innerSize = Mathf.Max(0.01f, mapper.TileSize - _tileGap);
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var pos = new GridPosition(x, y);
                    Vector3 world = mapper.GridToWorld(pos);

                    var tile = CreateSquare($"Tile_{x}_{y}", transform, TILE_SORTING_ORDER);
                    tile.color = GetTerrainColor(grid.GetTerrain(pos));
                    tile.transform.position = world;
                    tile.transform.localScale = new Vector3(innerSize, innerSize, 1f);

                    var highlight = CreateSquare($"Highlight_{x}_{y}", tile.transform, HIGHLIGHT_SORTING_ORDER);
                    highlight.transform.localPosition = Vector3.zero;
                    highlight.transform.localScale = Vector3.one;
                    highlight.enabled = false;
                    _highlights[pos] = highlight;
                }
            }
        }

        /// <summary>
        /// Highlight the given tiles with a kind, clearing all other highlights.
        /// </summary>
        public void ShowHighlights(IEnumerable<GridPosition> tiles, HighlightKind kind)
        {
            ClearHighlights();
            AddHighlights(tiles, kind);
        }

        /// <summary>Highlight the given tiles without clearing existing ones.</summary>
        public void AddHighlights(IEnumerable<GridPosition> tiles, HighlightKind kind)
        {
            if (tiles == null || kind == HighlightKind.None) return;

            Color color = GetHighlightColor(kind);
            foreach (var pos in tiles)
            {
                if (_highlights.TryGetValue(pos, out var sr))
                {
                    sr.color = color;
                    sr.enabled = true;
                }
            }
        }

        /// <summary>Remove every highlight.</summary>
        public void ClearHighlights()
        {
            foreach (var sr in _highlights.Values)
            {
                if (sr != null) sr.enabled = false;
            }
        }

        /// <summary>World-space center of a tile (zero if not built yet).</summary>
        public Vector3 GetWorldPosition(GridPosition pos)
        {
            return Mapper != null ? Mapper.GridToWorld(pos) : Vector3.zero;
        }

        private void Update()
        {
            if (Mapper == null || _camera == null) return;
            if (!Input.GetMouseButtonDown(0)) return;

            // Clicks on UI buttons must not also hit the grid underneath
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 world = _camera.ScreenToWorldPoint(Input.mousePosition);
            if (Mapper.TryWorldToGrid(world, out var pos))
            {
                OnTileClicked?.Invoke(pos);
            }
        }

        private void Clear()
        {
            _highlights.Clear();
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private Color GetTerrainColor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Difficult: return _difficultColor;
                case TerrainType.Elevated: return _elevatedColor;
                case TerrainType.Water: return _waterColor;
                case TerrainType.Lava: return _lavaColor;
                case TerrainType.Abyss: return _abyssColor;
                case TerrainType.Cover: return _coverColor;
                default: return _normalColor;
            }
        }

        private Color GetHighlightColor(HighlightKind kind)
        {
            switch (kind)
            {
                case HighlightKind.Movement: return _movementColor;
                case HighlightKind.AbilityRange: return _abilityRangeColor;
                case HighlightKind.ActiveUnit: return _activeUnitColor;
                default: return Color.clear;
            }
        }

        private static SpriteRenderer CreateSquare(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        /// <summary>Shared 1x1-world-unit white square generated at runtime (no art assets needed).</summary>
        private static Sprite GetSquareSprite()
        {
            if (_squareSprite == null)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                _squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return _squareSprite;
        }
    }
}
