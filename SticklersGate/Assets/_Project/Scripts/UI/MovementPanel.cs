using System;
using UnityEngine;
using UnityEngine.UI;
using SticklersGate.Combat;

namespace SticklersGate.UI
{
    /// <summary>
    /// Shown during the movement phase: prompts the player to tap a highlighted
    /// tile and offers a Skip button to stay put.
    /// </summary>
    public class MovementPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text _promptText;
        [SerializeField] private Button _skipButton;

        /// <summary>Fired when the player chooses not to move this turn.</summary>
        public event Action OnSkipSelected;

        /// <summary>Show the movement prompt for the given unit.</summary>
        public void Show(CombatUnit unit)
        {
            gameObject.SetActive(true);

            if (_promptText != null)
            {
                _promptText.text = $"{unit.Name}: tap a blue tile to move (up to {unit.MovementSpeed})";
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveAllListeners();
                _skipButton.onClick.AddListener(() => OnSkipSelected?.Invoke());
            }
        }

        /// <summary>Hide the movement panel.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
