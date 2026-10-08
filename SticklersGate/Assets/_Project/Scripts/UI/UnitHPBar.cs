using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SticklersGate.Combat;

namespace SticklersGate.UI
{
    /// <summary>
    /// Displays an HP bar and name label for a CombatUnit.
    /// Subscribes to unit events for live updates.
    /// </summary>
    public class UnitHPBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private Image _backgroundImage;

        [Header("Colors")]
        [SerializeField] private Color _healthyColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _woundedColor = new Color(0.9f, 0.9f, 0.1f);
        [SerializeField] private Color _criticalColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color _playerBgColor = new Color(0.15f, 0.15f, 0.4f, 0.8f);
        [SerializeField] private Color _enemyBgColor = new Color(0.4f, 0.15f, 0.15f, 0.8f);

        private CombatUnit _unit;
        private Color _defaultNameColor = Color.white;

        /// <summary>
        /// Bind this HP bar to a CombatUnit and subscribe to its events.
        /// </summary>
        public void Bind(CombatUnit unit)
        {
            Unbind();
            _unit = unit;

            if (_nameText != null)
            {
                _nameText.text = unit.Name;
                _defaultNameColor = _nameText.color;
            }
            if (_backgroundImage != null)
                _backgroundImage.color = unit.IsPlayer ? _playerBgColor : _enemyBgColor;

            unit.OnDamageTaken += OnHPChanged;
            unit.OnHealed += OnHPChanged;
            unit.OnDeath += OnDeath;

            Refresh();
        }

        /// <summary>
        /// Unbind from the current unit.
        /// </summary>
        public void Unbind()
        {
            if (_unit != null)
            {
                _unit.OnDamageTaken -= OnHPChanged;
                _unit.OnHealed -= OnHPChanged;
                _unit.OnDeath -= OnDeath;
                _unit = null;
            }
        }

        /// <summary>
        /// Refresh the HP bar display to match the current unit state.
        /// </summary>
        public void Refresh()
        {
            if (_unit == null) return;

            float ratio = _unit.MaxHP > 0 ? (float)_unit.CurrentHP / _unit.MaxHP : 0f;

            if (_fillImage != null)
            {
                _fillImage.fillAmount = ratio;
                _fillImage.color = ratio > 0.5f ? _healthyColor :
                                   ratio > 0.25f ? _woundedColor : _criticalColor;
            }

            if (_hpText != null)
            {
                _hpText.text = $"{_unit.CurrentHP}/{_unit.MaxHP}";
            }

            if (_nameText != null)
            {
                if (!_unit.IsAlive)
                    _nameText.color = Color.gray;
                else if (!_unit.IsConscious)
                    _nameText.color = new Color(1f, 0.5f, 0.5f);
                else
                    _nameText.color = _defaultNameColor;
            }
        }

        private void OnHPChanged(int amount) => Refresh();
        private void OnDeath() => Refresh();

        private void OnDestroy() => Unbind();
    }
}
