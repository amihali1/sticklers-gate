using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.UI
{
    /// <summary>
    /// Displays target selection buttons after the player picks an ability.
    /// Shows valid targets based on AbilityTargetType.
    /// </summary>
    public class TargetPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _buttonContainer;
        [SerializeField] private GameObject _targetButtonPrefab;
        [SerializeField] private Button _cancelButton;

        /// <summary>
        /// Fired when the player selects a target.
        /// </summary>
        public event Action<CombatUnit> OnTargetSelected;

        /// <summary>
        /// Fired when the player cancels target selection.
        /// </summary>
        public event Action OnCancelled;

        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

        /// <summary>
        /// Show valid targets for a given target type (used by items).
        /// </summary>
        public void ShowTargets(AbilityTargetType targetType, CombatUnit actor,
            IReadOnlyList<CombatUnit> playerUnits, IReadOnlyList<CombatUnit> enemyUnits)
        {
            ClearButtons();
            gameObject.SetActive(true);

            var targets = GetValidTargets(targetType, actor, playerUnits, enemyUnits);

            foreach (var target in targets)
            {
                if (_targetButtonPrefab == null || _buttonContainer == null) break;

                var go = Instantiate(_targetButtonPrefab, _buttonContainer);
                go.SetActive(true);
                _spawnedButtons.Add(go);

                var buttonText = go.GetComponentInChildren<Text>();
                var button = go.GetComponent<Button>();

                if (buttonText != null)
                {
                    buttonText.text = $"{target.Name} ({target.CurrentHP}/{target.MaxHP})";
                }

                if (button != null)
                {
                    var capturedTarget = target;
                    button.onClick.AddListener(() => OnTargetSelected?.Invoke(capturedTarget));
                }
            }

            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(true);
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());
            }
        }

        /// <summary>
        /// Show valid targets for the selected ability.
        /// </summary>
        public void ShowTargets(RuntimeAbility ability, CombatUnit actor,
            IReadOnlyList<CombatUnit> playerUnits, IReadOnlyList<CombatUnit> enemyUnits)
        {
            ClearButtons();
            gameObject.SetActive(true);

            var targets = GetTargetsInRange(ability.Data, actor, playerUnits, enemyUnits);

            // Self-target auto-selects
            if (ability.Data.TargetType == AbilityTargetType.Self)
            {
                OnTargetSelected?.Invoke(actor);
                Hide();
                return;
            }

            foreach (var target in targets)
            {
                if (_targetButtonPrefab == null || _buttonContainer == null) break;

                var go = Instantiate(_targetButtonPrefab, _buttonContainer);
                go.SetActive(true);
                _spawnedButtons.Add(go);

                var buttonText = go.GetComponentInChildren<Text>();
                var button = go.GetComponent<Button>();

                if (buttonText != null)
                {
                    buttonText.text = $"{target.Name} ({target.CurrentHP}/{target.MaxHP})";
                }

                if (button != null)
                {
                    var capturedTarget = target;
                    button.onClick.AddListener(() => OnTargetSelected?.Invoke(capturedTarget));
                }
            }

            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(true);
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());
            }
        }

        /// <summary>
        /// Show the aiming prompt for a Point (sphere) ability: no unit buttons,
        /// the player taps a tile on the grid instead. Cancel stays available.
        /// </summary>
        public void ShowAreaPrompt(RuntimeAbility ability)
        {
            ClearButtons();
            gameObject.SetActive(true);

            if (_targetButtonPrefab != null && _buttonContainer != null)
            {
                var go = Instantiate(_targetButtonPrefab, _buttonContainer);
                go.SetActive(true);
                _spawnedButtons.Add(go);

                var buttonText = go.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"Tap a tile to aim {ability.Data.AbilityName}, tap again to cast";
                }

                var button = go.GetComponent<Button>();
                if (button != null) button.interactable = false;
            }

            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(true);
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());
            }
        }

        /// <summary>
        /// Hide the target panel.
        /// </summary>
        public void Hide()
        {
            ClearButtons();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Get valid targets for a given target type.
        /// </summary>
        public static List<CombatUnit> GetValidTargets(AbilityTargetType targetType, CombatUnit actor,
            IReadOnlyList<CombatUnit> playerUnits, IReadOnlyList<CombatUnit> enemyUnits)
        {
            var targets = new List<CombatUnit>();

            IReadOnlyList<CombatUnit> allies = actor.IsPlayer ? playerUnits : enemyUnits;
            IReadOnlyList<CombatUnit> enemies = actor.IsPlayer ? enemyUnits : playerUnits;

            switch (targetType)
            {
                case AbilityTargetType.SingleEnemy:
                    foreach (var u in enemies)
                    {
                        if (u.IsAlive && u.IsConscious) targets.Add(u);
                    }
                    break;

                case AbilityTargetType.SingleAlly:
                    foreach (var u in allies)
                    {
                        if (u.IsAlive) targets.Add(u);
                    }
                    break;

                case AbilityTargetType.AllEnemies:
                    foreach (var u in enemies)
                    {
                        if (u.IsAlive && u.IsConscious) targets.Add(u);
                    }
                    break;

                case AbilityTargetType.AllAllies:
                    foreach (var u in allies)
                    {
                        if (u.IsAlive) targets.Add(u);
                    }
                    break;

                case AbilityTargetType.Self:
                    targets.Add(actor);
                    break;
            }

            return targets;
        }

        /// <summary>
        /// Valid targets for an ability that are also within its grid range.
        /// AoE and self abilities ignore range.
        /// </summary>
        public static List<CombatUnit> GetTargetsInRange(AbilitySO ability, CombatUnit actor,
            IReadOnlyList<CombatUnit> playerUnits, IReadOnlyList<CombatUnit> enemyUnits)
        {
            var targets = GetValidTargets(ability.TargetType, actor, playerUnits, enemyUnits);
            targets.RemoveAll(t => !DamageCalculator.IsInRange(actor, t, ability));
            return targets;
        }

        /// <summary>
        /// True if the actor could use the ability on at least one target right
        /// now (self, emanation and point-aimed area abilities always qualify).
        /// </summary>
        public static bool HasTargetInRange(AbilitySO ability, CombatUnit actor,
            IReadOnlyList<CombatUnit> playerUnits, IReadOnlyList<CombatUnit> enemyUnits)
        {
            switch (ability.TargetType)
            {
                case AbilityTargetType.Self:
                case AbilityTargetType.Point:
                case AbilityTargetType.AllEnemies:
                case AbilityTargetType.AllAllies:
                    return true;
                default:
                    return GetTargetsInRange(ability, actor, playerUnits, enemyUnits).Count > 0;
            }
        }

        private void ClearButtons()
        {
            foreach (var go in _spawnedButtons)
            {
                if (go != null) Destroy(go);
            }
            _spawnedButtons.Clear();
        }
    }
}
