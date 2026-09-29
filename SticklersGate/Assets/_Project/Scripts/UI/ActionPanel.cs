using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.UI
{
    /// <summary>
    /// Displays available actions for the current player's turn.
    /// Shows ability buttons and a Defend button. Abilities on cooldown or
    /// with no target in grid range are shown disabled.
    /// </summary>
    public class ActionPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _buttonContainer;
        [SerializeField] private GameObject _abilityButtonPrefab;
        [SerializeField] private Button _defendButton;

        [Header("Colors")]
        [SerializeField] private Color _readyColor = Color.white;
        [SerializeField] private Color _cooldownColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _outOfRangeColor = new Color(0.45f, 0.3f, 0.3f);

        /// <summary>
        /// Fired when the player selects an ability. Passes the RuntimeAbility.
        /// </summary>
        public event Action<RuntimeAbility> OnAbilitySelected;

        /// <summary>
        /// Fired when the player chooses to defend.
        /// </summary>
        public event Action OnDefendSelected;

        /// <summary>
        /// Fired when the player selects an item. Passes the ItemSO.
        /// </summary>
        public event Action<ItemSO> OnItemSelected;

        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();
        private Inventory _inventory;

        /// <summary>
        /// Set the inventory to show item buttons.
        /// </summary>
        public void SetInventory(Inventory inventory)
        {
            _inventory = inventory;
        }

        /// <summary>
        /// Show action buttons for the given unit's available abilities. The unit
        /// lists are used to disable abilities with no target in range.
        /// </summary>
        public void ShowActions(CombatUnit unit,
            IReadOnlyList<CombatUnit> playerUnits, IReadOnlyList<CombatUnit> enemyUnits)
        {
            ClearButtons();
            gameObject.SetActive(true);

            foreach (var ability in unit.Abilities)
            {
                if (_abilityButtonPrefab == null || _buttonContainer == null) break;

                var go = Instantiate(_abilityButtonPrefab, _buttonContainer);
                go.SetActive(true);
                _spawnedButtons.Add(go);

                var buttonText = go.GetComponentInChildren<Text>();
                var button = go.GetComponent<Button>();

                bool isReady = ability.IsReady;
                bool inRange = TargetPanel.HasTargetInRange(ability.Data, unit, playerUnits, enemyUnits);

                if (buttonText != null)
                {
                    string label = ability.Data.AbilityName;
                    if (!isReady)
                    {
                        label += $" ({ability.CurrentCooldown})";
                    }
                    else if (!inRange)
                    {
                        label += " (out of range)";
                    }
                    buttonText.text = label;
                }

                if (button != null)
                {
                    button.interactable = isReady && inRange;

                    var colors = button.colors;
                    colors.normalColor = !isReady ? _cooldownColor : inRange ? _readyColor : _outOfRangeColor;
                    colors.disabledColor = colors.normalColor;
                    button.colors = colors;

                    var capturedAbility = ability;
                    button.onClick.AddListener(() => OnAbilitySelected?.Invoke(capturedAbility));
                }
            }

            // Show consumable item buttons
            if (_inventory != null && _abilityButtonPrefab != null && _buttonContainer != null)
            {
                foreach (var stack in _inventory.GetConsumables())
                {
                    var go = Instantiate(_abilityButtonPrefab, _buttonContainer);
                    go.SetActive(true);
                    _spawnedButtons.Add(go);

                    var buttonText = go.GetComponentInChildren<Text>();
                    var button = go.GetComponent<Button>();
                    var img = go.GetComponent<Image>();

                    if (buttonText != null)
                    {
                        buttonText.text = $"{stack.Item.ItemName} x{stack.Quantity}";
                    }

                    if (img != null)
                    {
                        img.color = new Color(0.2f, 0.4f, 0.2f); // Green tint for items
                    }

                    if (button != null)
                    {
                        var capturedItem = stack.Item;
                        button.onClick.AddListener(() => OnItemSelected?.Invoke(capturedItem));
                    }
                }
            }

            if (_defendButton != null)
            {
                _defendButton.gameObject.SetActive(true);
                _defendButton.onClick.RemoveAllListeners();
                _defendButton.onClick.AddListener(() => OnDefendSelected?.Invoke());
            }
        }

        /// <summary>
        /// Hide the action panel.
        /// </summary>
        public void Hide()
        {
            ClearButtons();
            gameObject.SetActive(false);
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
