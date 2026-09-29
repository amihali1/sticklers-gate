using System;
using UnityEngine;

namespace SticklersGate.Data
{
    /// <summary>
    /// Defines an enemy spawn within an encounter.
    /// </summary>
    [Serializable]
    public struct EnemySpawn
    {
        public EnemySO enemyData;
        public string nameOverride;
    }

    /// <summary>
    /// Defines a combat encounter: enemy lineup, grid layout, difficulty, victory condition, and narration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEncounter", menuName = "SticklersGate/EncounterData")]
    public class EncounterSO : ScriptableObject
    {
        [Header("Encounter Setup")]
        [SerializeField] private string _encounterName;
        [SerializeField] private EnemySpawn[] _enemies;
        [SerializeField] private VictoryCondition _victoryCondition;
        [SerializeField] private int _survivalTurns;

        [Header("Environment")]
        [SerializeField] private Sprite _background;
        [Tooltip("Battlefield grid (size, terrain, spawn tiles). Null uses the default grid and formation.")]
        [SerializeField] private GridLayoutSO _gridLayout;

        [Header("Narrator")]
        [SerializeField][TextArea] private string[] _introLines;
        [SerializeField][TextArea] private string[] _victoryLines;
        [SerializeField][TextArea] private string[] _defeatLines;

        // --- Public Properties ---

        public string EncounterName => _encounterName;
        public EnemySpawn[] Enemies => _enemies;
        public VictoryCondition VictoryCondition => _victoryCondition;
        public int SurvivalTurns => _survivalTurns;

        public Sprite Background => _background;
        public GridLayoutSO GridLayout => _gridLayout;

        public string[] IntroLines => _introLines;
        public string[] VictoryLines => _victoryLines;
        public string[] DefeatLines => _defeatLines;
    }
}
