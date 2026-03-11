using UnityEngine;
using UnityEngine.UI;

namespace SticklersGate.UI
{
    /// <summary>
    /// Displays the victory or defeat screen at the end of combat.
    /// </summary>
    public class CombatResultPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _subtitleText;
        [SerializeField] private Image _backgroundImage;

        [Header("Colors")]
        [SerializeField] private Color _victoryColor = new Color(0.2f, 0.6f, 0.2f, 0.9f);
        [SerializeField] private Color _defeatColor = new Color(0.6f, 0.1f, 0.1f, 0.9f);

        /// <summary>
        /// Show the result panel with victory or defeat state.
        /// </summary>
        public void Show(bool victory)
        {
            gameObject.SetActive(true);

            if (_titleText != null)
            {
                _titleText.text = victory ? "VICTORY" : "DEFEAT";
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = victory
                    ? "The stick figures live to fight another day."
                    : "Your stick figures have been erased.";
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = victory ? _victoryColor : _defeatColor;
            }
        }

        /// <summary>
        /// Hide the result panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
