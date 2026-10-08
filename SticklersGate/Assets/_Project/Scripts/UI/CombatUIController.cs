using System.Collections.Generic;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.UI
{
    /// <summary>
    /// Main combat UI controller. Subscribes to CombatStateMachine events and
    /// coordinates sub-components: grid, movement panel, HP bars, action panel,
    /// target panel, turn order display, narrator, damage popups, and result panel.
    /// </summary>
    public class CombatUIController : MonoBehaviour
    {
        [Header("Combat Reference")]
        [SerializeField] private CombatStateMachine _combatStateMachine;

        [Header("UI Components")]
        [SerializeField] private NarratorDisplay _narratorDisplay;
        [SerializeField] private TurnOrderDisplay _turnOrderDisplay;
        [SerializeField] private ActionPanel _actionPanel;
        [SerializeField] private TargetPanel _targetPanel;
        [SerializeField] private CombatResultPanel _resultPanel;
        [SerializeField] private MovementPanel _movementPanel;
        [SerializeField] private GridRenderer _gridRenderer;

        [Header("HP Bars")]
        [SerializeField] private Transform _playerHPBarContainer;
        [SerializeField] private Transform _enemyHPBarContainer;
        [SerializeField] private GameObject _hpBarPrefab;

        [Header("Damage Popups")]
        [SerializeField] private GameObject _damagePopupPrefab;
        [SerializeField] private Canvas _popupCanvas;
        [SerializeField] private Camera _mainCamera;

        private const float POPUP_HEIGHT_OFFSET = 0.9f;

        private readonly List<UnitHPBar> _playerHPBars = new List<UnitHPBar>();
        private readonly List<UnitHPBar> _enemyHPBars = new List<UnitHPBar>();
        private readonly HashSet<GridPosition> _reachableTiles = new HashSet<GridPosition>();
        private readonly List<CombatUnit> _selectableTargets = new List<CombatUnit>();

        private RuntimeAbility _selectedAbility;
        private ItemSO _selectedItem;

        // Point (sphere) aiming: first tap previews the area, second tap on the same tile casts
        private RuntimeAbility _aimingAbility;
        private GridPosition? _pendingAreaCenter;

        private void Awake()
        {
            if (_actionPanel != null) _actionPanel.Hide();
            if (_targetPanel != null) _targetPanel.Hide();
            if (_resultPanel != null) _resultPanel.Hide();
            if (_movementPanel != null) _movementPanel.Hide();
        }

        private void OnEnable()
        {
            if (_combatStateMachine == null) return;

            _combatStateMachine.OnCombatStarted += HandleCombatStarted;
            _combatStateMachine.OnInitiativeRolled += HandleInitiativeRolled;
            _combatStateMachine.OnTurnStarted += HandleTurnStarted;
            _combatStateMachine.OnMovementPhaseStarted += HandleMovementPhaseStarted;
            _combatStateMachine.OnUnitMoved += HandleUnitMoved;
            _combatStateMachine.OnPlayerActionNeeded += HandlePlayerActionNeeded;
            _combatStateMachine.OnAttackResolved += HandleAttackResolved;
            _combatStateMachine.OnHealResolved += HandleHealResolved;
            _combatStateMachine.OnUnitDowned += HandleUnitDowned;
            _combatStateMachine.OnUnitDied += HandleUnitDied;
            _combatStateMachine.OnNarratorQuip += HandleNarratorQuip;
            _combatStateMachine.OnCombatResult += HandleCombatResult;

            if (_actionPanel != null)
            {
                _actionPanel.OnAbilitySelected += HandleAbilitySelected;
                _actionPanel.OnDefendSelected += HandleDefendSelected;
                _actionPanel.OnItemSelected += HandleItemSelected;
            }

            if (_targetPanel != null)
            {
                _targetPanel.OnTargetSelected += HandleTargetSelected;
                _targetPanel.OnCancelled += HandleTargetCancelled;
            }

            if (_movementPanel != null) _movementPanel.OnEndTurnSelected += HandleEndTurn;
            if (_gridRenderer != null) _gridRenderer.OnTileClicked += HandleTileClicked;
        }

        private void OnDisable()
        {
            if (_combatStateMachine == null) return;

            _combatStateMachine.OnCombatStarted -= HandleCombatStarted;
            _combatStateMachine.OnInitiativeRolled -= HandleInitiativeRolled;
            _combatStateMachine.OnTurnStarted -= HandleTurnStarted;
            _combatStateMachine.OnMovementPhaseStarted -= HandleMovementPhaseStarted;
            _combatStateMachine.OnUnitMoved -= HandleUnitMoved;
            _combatStateMachine.OnPlayerActionNeeded -= HandlePlayerActionNeeded;
            _combatStateMachine.OnAttackResolved -= HandleAttackResolved;
            _combatStateMachine.OnHealResolved -= HandleHealResolved;
            _combatStateMachine.OnUnitDowned -= HandleUnitDowned;
            _combatStateMachine.OnUnitDied -= HandleUnitDied;
            _combatStateMachine.OnNarratorQuip -= HandleNarratorQuip;
            _combatStateMachine.OnCombatResult -= HandleCombatResult;

            if (_actionPanel != null)
            {
                _actionPanel.OnAbilitySelected -= HandleAbilitySelected;
                _actionPanel.OnDefendSelected -= HandleDefendSelected;
                _actionPanel.OnItemSelected -= HandleItemSelected;
            }

            if (_targetPanel != null)
            {
                _targetPanel.OnTargetSelected -= HandleTargetSelected;
                _targetPanel.OnCancelled -= HandleTargetCancelled;
            }

            if (_movementPanel != null) _movementPanel.OnEndTurnSelected -= HandleEndTurn;
            if (_gridRenderer != null) _gridRenderer.OnTileClicked -= HandleTileClicked;
        }

        private void HandleCombatStarted()
        {
            SetupHPBars();

            // Set inventory after StartCombat has run (Inventory is null at OnEnable time)
            if (_actionPanel != null && _combatStateMachine.Inventory != null)
            {
                _actionPanel.SetInventory(_combatStateMachine.Inventory);
            }
        }

        private void HandleInitiativeRolled(List<CombatUnit> turnOrder)
        {
            if (_turnOrderDisplay != null)
            {
                _turnOrderDisplay.SetTurnOrder(turnOrder);
            }
        }

        private void HandleTurnStarted(CombatUnit unit)
        {
            if (_turnOrderDisplay != null)
            {
                _turnOrderDisplay.SetActiveUnit(unit);
            }

            ShowActiveUnitHighlight(unit);
        }

        /// <summary>Post-action movement: the action is spent but movement remains.</summary>
        private void HandleMovementPhaseStarted(CombatUnit unit)
        {
            if (_actionPanel != null) _actionPanel.Hide();
            ShowMovementOptions(unit);
            if (_movementPanel != null) _movementPanel.Show(unit);
        }

        private void HandleEndTurn()
        {
            if (_movementPanel != null) _movementPanel.Hide();
            if (_gridRenderer != null) _gridRenderer.ClearHighlights();
            _combatStateMachine.EndTurn();
        }

        /// <summary>
        /// Refresh after the active player moves: new reachable tiles from the
        /// remaining movement, and ability availability from the new position.
        /// </summary>
        private void HandleUnitMoved(CombatUnit unit, GridPosition from, GridPosition to)
        {
            if (unit != _combatStateMachine.CurrentUnit || !unit.IsPlayer) return;

            switch (_combatStateMachine.CurrentPhase)
            {
                case CombatPhase.PlayerChooseAction:
                    ShowMovementOptions(unit);
                    if (_actionPanel != null)
                        _actionPanel.ShowActions(unit, _combatStateMachine.PlayerUnits, _combatStateMachine.EnemyUnits);
                    break;
                case CombatPhase.PlayerChooseMovement:
                    ShowMovementOptions(unit);
                    if (_movementPanel != null) _movementPanel.Show(unit);
                    break;
                default:
                    // Out of movement after acting: the turn is over
                    _reachableTiles.Clear();
                    if (_movementPanel != null) _movementPanel.Hide();
                    if (_gridRenderer != null) _gridRenderer.ClearHighlights();
                    break;
            }
        }

        /// <summary>Highlight tiles reachable with the unit's remaining movement, plus its own tile.</summary>
        private void ShowMovementOptions(CombatUnit unit)
        {
            _reachableTiles.Clear();
            var grid = _combatStateMachine.Grid;
            if (grid != null && unit.RemainingMovement > 0)
            {
                _reachableTiles.UnionWith(Pathfinder.GetReachablePositions(grid, unit.Position, unit.RemainingMovement));
            }

            if (_gridRenderer != null)
            {
                _gridRenderer.ShowHighlights(_reachableTiles, GridRenderer.HighlightKind.Movement);
                _gridRenderer.AddHighlights(new[] { unit.Position }, GridRenderer.HighlightKind.ActiveUnit);
            }
        }

        private void HandleTileClicked(GridPosition tile)
        {
            var current = _combatStateMachine.CurrentUnit;
            if (current == null || !current.IsPlayer) return;

            if (_aimingAbility != null)
            {
                HandleAreaAimTap(current, tile);
                return;
            }

            // Tap a unit on the grid as an alternative to the target buttons
            if (_selectableTargets.Count > 0)
            {
                if (_combatStateMachine.Grid?.GetOccupant(tile) is CombatUnit occupant &&
                    _selectableTargets.Contains(occupant))
                {
                    HandleTargetSelected(occupant);
                }
                return;
            }

            // Otherwise a blue tile moves the unit (before or after its action)
            var phase = _combatStateMachine.CurrentPhase;
            bool canMove = phase == CombatPhase.PlayerChooseMovement ||
                           (phase == CombatPhase.PlayerChooseAction && _selectedAbility == null && _selectedItem == null);
            if (canMove && _reachableTiles.Contains(tile))
            {
                _combatStateMachine.SubmitPlayerMovement(tile);
            }
        }

        private void HandleAreaAimTap(CombatUnit caster, GridPosition tile)
        {
            var grid = _combatStateMachine.Grid;
            var ability = _aimingAbility.Data;
            if (grid == null || !AreaTargeting.IsValidCenter(grid, ability, caster, tile)) return;

            if (_pendingAreaCenter.HasValue && _pendingAreaCenter.Value == tile)
            {
                var toCast = _aimingAbility;
                ClearAreaAim();
                if (_targetPanel != null) _targetPanel.Hide();
                if (_gridRenderer != null) _gridRenderer.ClearHighlights();
                _selectedAbility = null;
                _combatStateMachine.SubmitPlayerAreaAction(toCast, tile);
                return;
            }

            _pendingAreaCenter = tile;
            ShowAreaRange(caster, ability);
            if (_gridRenderer != null)
            {
                _gridRenderer.AddHighlights(AreaTargeting.GetAreaTiles(grid, ability, caster, tile),
                    GridRenderer.HighlightKind.AreaPreview);
            }
        }

        private void ShowAreaRange(CombatUnit caster, AbilitySO ability)
        {
            var grid = _combatStateMachine.Grid;
            if (_gridRenderer == null || grid == null) return;

            // A sphere may be centered anywhere within range, including the caster's own tile
            var tiles = grid.GetPositionsWithinDistance(caster.Position, ability.Range);
            tiles.Add(caster.Position);
            _gridRenderer.ShowHighlights(tiles, GridRenderer.HighlightKind.AbilityRange);
        }

        private void ClearAreaAim()
        {
            _aimingAbility = null;
            _pendingAreaCenter = null;
        }

        private void HandlePlayerActionNeeded(CombatUnit unit)
        {
            if (_movementPanel != null) _movementPanel.Hide();
            ShowMovementOptions(unit);

            if (_actionPanel != null)
            {
                _actionPanel.ShowActions(unit, _combatStateMachine.PlayerUnits, _combatStateMachine.EnemyUnits);
            }
        }

        private void HandleAbilitySelected(RuntimeAbility ability)
        {
            _selectedAbility = ability;
            if (_actionPanel != null) _actionPanel.Hide();

            // Auto-submit abilities that don't need target selection
            if (ability.Data.TargetType == AbilityTargetType.Self)
            {
                SubmitAction(ability, _combatStateMachine.CurrentUnit);
                return;
            }
            if (ability.Data.TargetType == AbilityTargetType.AllEnemies ||
                ability.Data.TargetType == AbilityTargetType.AllAllies)
            {
                // Multi-target: CombatStateMachine gathers all targets internally
                SubmitAction(ability, _combatStateMachine.CurrentUnit);
                return;
            }

            var actor = _combatStateMachine.CurrentUnit;

            // Spheres: aim at a tile on the grid instead of picking a unit
            if (ability.Data.TargetType == AbilityTargetType.Point)
            {
                _aimingAbility = ability;
                _pendingAreaCenter = null;
                _selectableTargets.Clear();
                ShowAreaRange(actor, ability.Data);
                if (_targetPanel != null) _targetPanel.ShowAreaPrompt(ability);
                return;
            }

            _selectableTargets.Clear();
            _selectableTargets.AddRange(TargetPanel.GetTargetsInRange(ability.Data, actor,
                _combatStateMachine.PlayerUnits, _combatStateMachine.EnemyUnits));

            if (_gridRenderer != null && _combatStateMachine.Grid != null)
            {
                _gridRenderer.ShowHighlights(
                    _combatStateMachine.Grid.GetPositionsWithinDistance(actor.Position, ability.Data.Range),
                    GridRenderer.HighlightKind.AbilityRange);
                _gridRenderer.AddHighlights(new[] { actor.Position }, GridRenderer.HighlightKind.ActiveUnit);
            }

            if (_targetPanel != null)
            {
                _targetPanel.ShowTargets(ability, actor,
                    _combatStateMachine.PlayerUnits, _combatStateMachine.EnemyUnits);
            }
        }

        private void HandleDefendSelected()
        {
            if (_actionPanel != null) _actionPanel.Hide();
            if (_targetPanel != null) _targetPanel.Hide();

            _combatStateMachine.SubmitDefend();
        }

        private void HandleItemSelected(ItemSO item)
        {
            _selectedItem = item;
            _selectedAbility = null;
            if (_actionPanel != null) _actionPanel.Hide();

            // Self-targeting items (heals on self) or AllAllies auto-submit
            if (item.TargetType == AbilityTargetType.Self ||
                item.TargetType == AbilityTargetType.AllAllies ||
                item.TargetType == AbilityTargetType.AllEnemies)
            {
                _combatStateMachine.SubmitUseItem(item, _combatStateMachine.CurrentUnit);
                _selectedItem = null;
                return;
            }

            // Show target selection for single-target items
            _selectableTargets.Clear();
            _selectableTargets.AddRange(TargetPanel.GetValidTargets(item.TargetType,
                _combatStateMachine.CurrentUnit, _combatStateMachine.PlayerUnits, _combatStateMachine.EnemyUnits));

            if (_targetPanel != null)
            {
                // Create a temporary AbilitySO-like target type for the panel
                _targetPanel.ShowTargets(item.TargetType, _combatStateMachine.CurrentUnit,
                    _combatStateMachine.PlayerUnits, _combatStateMachine.EnemyUnits);
            }
        }

        private void HandleTargetSelected(CombatUnit target)
        {
            if (_targetPanel != null) _targetPanel.Hide();
            _selectableTargets.Clear();
            if (_gridRenderer != null) _gridRenderer.ClearHighlights();

            if (_selectedItem != null)
            {
                _combatStateMachine.SubmitUseItem(_selectedItem, target);
                _selectedItem = null;
                return;
            }

            if (_selectedAbility != null)
            {
                SubmitAction(_selectedAbility, target);
            }
        }

        private void HandleTargetCancelled()
        {
            if (_targetPanel != null) _targetPanel.Hide();
            _selectedAbility = null;
            _selectedItem = null;
            _selectableTargets.Clear();
            ClearAreaAim();

            // Re-show action panel
            var current = _combatStateMachine.CurrentUnit;
            if (current != null && current.IsPlayer)
            {
                ShowMovementOptions(current);
                if (_actionPanel != null)
                {
                    _actionPanel.ShowActions(current, _combatStateMachine.PlayerUnits, _combatStateMachine.EnemyUnits);
                }
            }
        }

        private void HandleAttackResolved(CombatUnit attacker, CombatUnit target, AttackResult result)
        {
            string message;
            Color color;

            if (result.IsCriticalHit)
            {
                message = $"CRIT! {result.TotalDamage}";
                color = new Color(1f, 0.8f, 0f); // Gold
            }
            else if (result.IsCriticalFail)
            {
                message = "MISS! (Nat 1)";
                color = Color.gray;
            }
            else if (result.IsHit)
            {
                message = result.TotalDamage.ToString();
                color = Color.red;
            }
            else
            {
                message = "MISS";
                color = Color.gray;
            }

            // Show popup over the target, not the attacker
            SpawnDamagePopup(message, color, result.IsHit ? target : attacker);
        }

        private void HandleHealResolved(CombatUnit healer, CombatUnit target, HealResult result)
        {
            SpawnDamagePopup($"+{result.TotalHealed}", Color.green, target);
        }

        private void HandleUnitDowned(CombatUnit unit)
        {
            HandleNarratorQuip($"{unit.Name} is down!");
        }

        private void HandleUnitDied(CombatUnit unit)
        {
            // HP bars will auto-update via events
        }

        private void HandleNarratorQuip(string message)
        {
            if (_narratorDisplay != null)
            {
                _narratorDisplay.ShowMessage(message);
            }
        }

        private void HandleCombatResult(bool victory)
        {
            if (_actionPanel != null) _actionPanel.Hide();
            if (_targetPanel != null) _targetPanel.Hide();
            if (_movementPanel != null) _movementPanel.Hide();
            if (_gridRenderer != null) _gridRenderer.ClearHighlights();
            if (_resultPanel != null) _resultPanel.Show(victory);
        }

        private void ShowActiveUnitHighlight(CombatUnit unit)
        {
            if (_gridRenderer == null) return;
            _gridRenderer.ShowHighlights(new[] { unit.Position }, GridRenderer.HighlightKind.ActiveUnit);
        }

        private void SubmitAction(RuntimeAbility ability, CombatUnit target)
        {
            _selectedAbility = null;
            _combatStateMachine.SubmitPlayerAction(ability, target);
        }

        private void SetupHPBars()
        {
            ClearHPBars();

            foreach (var unit in _combatStateMachine.PlayerUnits)
            {
                CreateHPBar(unit, _playerHPBarContainer, _playerHPBars);
            }

            foreach (var unit in _combatStateMachine.EnemyUnits)
            {
                CreateHPBar(unit, _enemyHPBarContainer, _enemyHPBars);
            }
        }

        private void CreateHPBar(CombatUnit unit, Transform container, List<UnitHPBar> list)
        {
            if (_hpBarPrefab == null || container == null) return;

            var go = Instantiate(_hpBarPrefab, container);
            go.SetActive(true);
            var hpBar = go.GetComponent<UnitHPBar>();
            if (hpBar != null)
            {
                hpBar.Bind(unit);
                list.Add(hpBar);
            }
        }

        private void ClearHPBars()
        {
            foreach (var bar in _playerHPBars)
            {
                if (bar != null) Destroy(bar.gameObject);
            }
            _playerHPBars.Clear();

            foreach (var bar in _enemyHPBars)
            {
                if (bar != null) Destroy(bar.gameObject);
            }
            _enemyHPBars.Clear();
        }

        private void SpawnDamagePopup(string message, Color color, CombatUnit unit)
        {
            if (_damagePopupPrefab == null || _popupCanvas == null) return;

            var go = Instantiate(_damagePopupPrefab, _popupCanvas.transform);
            go.SetActive(true);
            var popup = go.GetComponent<DamagePopup>();
            if (popup != null)
            {
                Vector3 worldPos = Vector3.zero;
                if (_gridRenderer != null)
                {
                    worldPos = _gridRenderer.GetWorldPosition(unit.Position) + Vector3.up * POPUP_HEIGHT_OFFSET;
                }

                popup.Setup(message, color, worldPos, _mainCamera);
            }
        }
    }
}
