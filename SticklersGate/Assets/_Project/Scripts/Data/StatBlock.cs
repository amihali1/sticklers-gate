using System;
using UnityEngine;

namespace SticklersGate.Data
{
    /// <summary>
    /// A set of six ability scores. Used by both CharacterClassSO and EnemySO.
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        [Range(1, 30)] public int strength;
        [Range(1, 30)] public int dexterity;
        [Range(1, 30)] public int constitution;
        [Range(1, 30)] public int intelligence;
        [Range(1, 30)] public int wisdom;
        [Range(1, 30)] public int charisma;

        /// <summary>
        /// Returns the ability score value for the given ability.
        /// </summary>
        public int GetScore(AbilityScore ability)
        {
            return ability switch
            {
                AbilityScore.Strength => strength,
                AbilityScore.Dexterity => dexterity,
                AbilityScore.Constitution => constitution,
                AbilityScore.Intelligence => intelligence,
                AbilityScore.Wisdom => wisdom,
                AbilityScore.Charisma => charisma,
                _ => throw new System.ArgumentOutOfRangeException(nameof(ability), ability, "Unhandled AbilityScore value.")
            };
        }

        /// <summary>
        /// Returns the modifier for the given ability: floor((score - 10) / 2).
        /// </summary>
        public int GetModifier(AbilityScore ability)
        {
            return Mathf.FloorToInt((GetScore(ability) - 10) / 2f);
        }
    }
}
