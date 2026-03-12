using System.Collections.Generic;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.UI
{
    /// <summary>
    /// Main combat UI controller. Subscribes to CombatStateMachine events and
    /// coordinates sub-components: HP bars, action panel, target panel,
    /// turn order display, narrator, damage popups, and result panel.
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

        [Header("HP Bars")]
        [SerializeField] private Transform _playerHPBarContainer;
        [SerializeField] private Transform _enemyHPBarContainer;
        [SerializeField] private GameObject _hpBarPrefab;

        [Header("Damage Popups")]
        [SerializeField] private GameObject _damagePopupPrefab;
        [SerializeField] private Canvas _popupCanvas;
        [SerializeField] private Camera _mainCamera;

        [Header("Unit Positions")]
        [SerializeField] private Transform[] _playerPositions;
        [SerializeField] private Transform[] _enemyPositions;

        private readonly List<UnitHPBar> _playerHPBars = new List<UnitHPBar>();
        private readonly List<UnitHPBar> _enemyHPBars = new List<UnitHPBar>();
        private readonly Dictionary<CombatUnit, Transform> _unitPositions = new Dictionary<CombatUnit, Transform>();

        private RuntimeAbility _selectedAbility;

        private void Awake()
        {
            if (_actionPanel != null) _actionPanel.Hide();
            if (_targetPanel != null) _targetPanel.Hide();
            if (_resultPanel != null) _resultPanel.Hide();
        }

        private void OnEnable()
        {
            if (_combatStateMachine == null) return;

            _combatStateMachine.OnCombatStarted += HandleCombatStarted;
            _combatStateMachine.OnInitiativeRolled += HandleInitiativeRolled;
            _combatStateMachine.OnTurnStarted += HandleTurnStarted;
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
            }

            if (_targetPanel != null)
            {
                _targetPanel.OnTargetSelected += HandleTargetSelected;
                _targetPanel.OnCancelled += HandleTargetCancelled;
            }
        }

        private void OnDisable()
        {
            if (_combatStateMachine == null) return;

            _combatStateMachine.OnCombatStarted -= HandleCombatStarted;
            _combatStateMachine.OnInitiativeRolled -= HandleInitiativeRolled;
            _combatStateMachine.OnTurnStarted -= HandleTurnStarted;
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
            }

            if (_targetPanel != null)
            {
                _targetPanel.OnTargetSelected -= HandleTargetSelected;
                _targetPanel.OnCancelled -= HandleTargetCancelled;
            }
        }

        private void HandleCombatStarted()
        {
            SetupHPBars();
            AssignUnitPositions();
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
        }

        private void HandlePlayerActionNeeded(CombatUnit unit)
        {
            if (_actionPanel != null)
            {
                _actionPanel.ShowActions(unit);
            }
        }

        private void HandleAbilitySelected(RuntimeAbility ability)
        {
            _selectedAbility = ability;
            if (_actionPanel != null) _actionPanel.Hide();

            // Self-target abilities auto-submit
            if (ability.Data.TargetType == AbilityTargetType.Self)
            {
                SubmitAction(ability, _combatStateMachine.CurrentUnit);
                return;
            }

            if (_targetPanel != null)
            {
                _targetPanel.ShowTargets(ability, _combatStateMachine.CurrentUnit,
                    _combatStateMachine.PlayerUnits, _combatStateMachine.EnemyUnits);
            }
        }

        private void HandleDefendSelected()
        {
            if (_actionPanel != null) _actionPanel.Hide();
            if (_targetPanel != null) _targetPanel.Hide();

            _combatStateMachine.SubmitDefend();
        }

        private void HandleTargetSelected(CombatUnit target)
        {
            if (_targetPanel != null) _targetPanel.Hide();

            if (_selectedAbility != null)
            {
                SubmitAction(_selectedAbility, target);
            }
        }

        private void HandleTargetCancelled()
        {
            if (_targetPanel != null) _targetPanel.Hide();
            _selectedAbility = null;

            // Re-show action panel
            if (_combatStateMachine.CurrentUnit != null && _combatStateMachine.CurrentUnit.IsPlayer)
            {
                if (_actionPanel != null)
                {
                    _actionPanel.ShowActions(_combatStateMachine.CurrentUnit);
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
            if (_resultPanel != null) _resultPanel.Show(victory);
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

        private void AssignUnitPositions()
        {
            _unitPositions.Clear();

            if (_playerPositions == null)
            {
                Debug.LogWarning("CombatUIController: _playerPositions array is not assigned.", this);
            }
            else
            {
                for (int i = 0; i < _combatStateMachine.PlayerUnits.Count && i < _playerPositions.Length; i++)
                {
                    _unitPositions[_combatStateMachine.PlayerUnits[i]] = _playerPositions[i];
                }
            }

            if (_enemyPositions == null)
            {
                Debug.LogWarning("CombatUIController: _enemyPositions array is not assigned.", this);
            }
            else
            {
                for (int i = 0; i < _combatStateMachine.EnemyUnits.Count && i < _enemyPositions.Length; i++)
                {
                    _unitPositions[_combatStateMachine.EnemyUnits[i]] = _enemyPositions[i];
                }
            }
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
                if (_unitPositions.TryGetValue(unit, out var transform))
                {
                    worldPos = transform.position + Vector3.up * 1.5f;
                }

                popup.Setup(message, color, worldPos, _mainCamera);
            }
        }
    }
}
