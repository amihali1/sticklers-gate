using System.Collections.Generic;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.UI
{
    /// <summary>
    /// Bootstraps the combat scene. Creates unit visuals and starts the encounter.
    /// Attach to a root GameObject in the Combat scene.
    /// </summary>
    public class CombatSceneSetup : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private CombatStateMachine _combatStateMachine;
        [SerializeField] private EncounterSO _testEncounter;

        [Header("Player Party")]
        [SerializeField] private CharacterClassSO _playerClass;
        [SerializeField] private string _playerName = "Hero";
        [SerializeField] private int _playerLevel = 1;

        [Header("Unit Positions")]
        [SerializeField] private Transform[] _playerSpawnPoints;
        [SerializeField] private Transform[] _enemySpawnPoints;

        [Header("Stick Figure Settings")]
        [SerializeField] private float _stickFigureHeight = 2f;

        private readonly List<StickFigureRenderer> _stickFigures = new List<StickFigureRenderer>();

        private void Start()
        {
            if (_combatStateMachine == null || _testEncounter == null || _playerClass == null)
            {
                Debug.LogError("CombatSceneSetup: Missing required references!");
                return;
            }

            // Create player party
            var playerUnits = new List<CombatUnit>
            {
                CombatUnit.FromPlayerCharacter(_playerName, _playerClass, _playerLevel)
            };

            // Subscribe to events for stick figure animation
            _combatStateMachine.OnCombatStarted += () => SpawnStickFigures(playerUnits);
            _combatStateMachine.OnAttackResolved += HandleAttackAnimation;
            _combatStateMachine.OnUnitDied += HandleDeathAnimation;

            // Start the encounter
            _combatStateMachine.StartCombat(playerUnits, _testEncounter);
        }

        private void SpawnStickFigures(List<CombatUnit> playerUnits)
        {
            // Spawn player stick figures
            for (int i = 0; i < playerUnits.Count && i < _playerSpawnPoints.Length; i++)
            {
                var figure = CreateStickFigure(playerUnits[i], _playerSpawnPoints[i], true);
                _stickFigures.Add(figure);
            }

            // Spawn enemy stick figures
            var enemyUnits = _combatStateMachine.EnemyUnits;
            for (int i = 0; i < enemyUnits.Count && i < _enemySpawnPoints.Length; i++)
            {
                var figure = CreateStickFigure(enemyUnits[i], _enemySpawnPoints[i], false);
                _stickFigures.Add(figure);
            }
        }

        private StickFigureRenderer CreateStickFigure(CombatUnit unit, Transform spawnPoint, bool facingRight)
        {
            var go = new GameObject($"StickFigure_{unit.Name}");
            go.transform.SetParent(spawnPoint, false);
            go.transform.localPosition = Vector3.zero;

            var renderer = go.AddComponent<StickFigureRenderer>();
            renderer.Bind(unit, facingRight);

            return renderer;
        }

        private void HandleAttackAnimation(CombatUnit attacker, AttackResult result)
        {
            foreach (var figure in _stickFigures)
            {
                // We'll need to track which figure belongs to which unit
                // For now, this is a placeholder — will be improved with
                // a dictionary mapping in the next iteration
            }
        }

        private void HandleDeathAnimation(CombatUnit unit)
        {
            foreach (var figure in _stickFigures)
            {
                // Same as above — placeholder for death animation
            }
        }
    }
}
