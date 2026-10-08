using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SticklersGate.Combat;

namespace SticklersGate.UI
{
    /// <summary>
    /// Shown after the player's action while movement remains: prompts the
    /// player to keep moving on blue tiles or end the turn.
    /// </summary>
    public class MovementPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _promptText;
        [SerializeField] private Button _endTurnButton;

        /// <summary>Fired when the player ends their turn, leaving any movement unused.</summary>
        public event Action OnEndTurnSelected;

        /// <summary>Show the movement prompt for the given unit.</summary>
        public void Show(CombatUnit unit)
        {
            gameObject.SetActive(true);

            if (_promptText != null)
            {
                _promptText.text = $"{unit.Name}: {unit.RemainingMovement} movement left. Tap a blue tile to move, or end your turn.";
            }

            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.RemoveAllListeners();
                _endTurnButton.onClick.AddListener(() => OnEndTurnSelected?.Invoke());
            }
        }

        /// <summary>Hide the movement panel.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
