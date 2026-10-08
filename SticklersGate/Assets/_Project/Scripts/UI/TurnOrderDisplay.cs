using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SticklersGate.Combat;

namespace SticklersGate.UI
{
    /// <summary>
    /// Displays the turn order as a horizontal list of unit names.
    /// Highlights the current unit.
    /// </summary>
    public class TurnOrderDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _entryContainer;
        [SerializeField] private GameObject _entryPrefab;

        [Header("Colors")]
        [SerializeField] private Color _activeColor = Color.yellow;
        [SerializeField] private Color _playerColor = Color.white;
        [SerializeField] private Color _enemyColor = new Color(1f, 0.6f, 0.6f);
        [SerializeField] private Color _deadColor = Color.gray;

        private readonly List<TurnOrderEntry> _entries = new List<TurnOrderEntry>();

        /// <summary>
        /// Build the turn order display from the initiative-sorted list.
        /// </summary>
        public void SetTurnOrder(IReadOnlyList<CombatUnit> turnOrder)
        {
            Clear();

            foreach (var unit in turnOrder)
            {
                if (_entryPrefab == null || _entryContainer == null) break;

                var go = Instantiate(_entryPrefab, _entryContainer);
                go.SetActive(true);
                var entry = new TurnOrderEntry
                {
                    Unit = unit,
                    Root = go,
                    Label = go.GetComponentInChildren<TMP_Text>()
                };

                if (entry.Label != null)
                {
                    entry.Label.text = unit.Name;
                    entry.Label.color = unit.IsPlayer ? _playerColor : _enemyColor;
                }

                _entries.Add(entry);
            }
        }

        /// <summary>
        /// Highlight the active unit in the turn order.
        /// </summary>
        public void SetActiveUnit(CombatUnit activeUnit)
        {
            foreach (var entry in _entries)
            {
                if (entry.Label == null) continue;

                if (!entry.Unit.IsAlive)
                {
                    entry.Label.color = _deadColor;
                }
                else if (entry.Unit == activeUnit)
                {
                    entry.Label.color = _activeColor;
                    entry.Label.fontStyle = FontStyles.Bold;
                }
                else
                {
                    entry.Label.color = entry.Unit.IsPlayer ? _playerColor : _enemyColor;
                    entry.Label.fontStyle = FontStyles.Normal;
                }
            }
        }

        private void Clear()
        {
            foreach (var entry in _entries)
            {
                if (entry.Root != null)
                    Destroy(entry.Root);
            }
            _entries.Clear();
        }

        private class TurnOrderEntry
        {
            public CombatUnit Unit;
            public GameObject Root;
            public TMP_Text Label;
        }
    }
}
