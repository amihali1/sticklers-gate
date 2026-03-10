using System.Collections.Generic;
using UnityEngine;
using SticklersGate.Data;

namespace SticklersGate.Utils
{
    /// <summary>
    /// Static utility for all dice rolling. Maintains a history of recent rolls for combat log.
    /// </summary>
    public static class DiceRoller
    {
        private const int MAX_HISTORY = 20;
        private static readonly List<int> _rollHistory = new List<int>();

        /// <summary>
        /// Roll a single die with the given number of sides.
        /// </summary>
        public static int Roll(int sides)
        {
            int result = Random.Range(1, sides + 1);
            AddToHistory(result);
            return result;
        }

        /// <summary>
        /// Roll multiple dice and return the total.
        /// </summary>
        public static int RollMultiple(int count, int sides)
        {
            int total = 0;
            for (int i = 0; i < count; i++)
            {
                total += Roll(sides);
            }
            return total;
        }

        /// <summary>
        /// Roll a d20.
        /// </summary>
        public static int RollD20()
        {
            return Roll(20);
        }

        /// <summary>
        /// Roll 2d20 and take the higher result (advantage).
        /// </summary>
        public static int RollWithAdvantage()
        {
            int a = Roll(20);
            int b = Roll(20);
            return Mathf.Max(a, b);
        }

        /// <summary>
        /// Roll 2d20 and take the lower result (disadvantage).
        /// </summary>
        public static int RollWithDisadvantage()
        {
            int a = Roll(20);
            int b = Roll(20);
            return Mathf.Min(a, b);
        }

        /// <summary>
        /// Returns true if the d20 roll is a natural 20.
        /// </summary>
        public static bool IsCriticalHit(int d20Roll)
        {
            return d20Roll >= GameConstants.CRIT_THRESHOLD;
        }

        /// <summary>
        /// Returns true if the d20 roll is a natural 1.
        /// </summary>
        public static bool IsCriticalFail(int d20Roll)
        {
            return d20Roll <= GameConstants.CRIT_FAIL_THRESHOLD;
        }

        /// <summary>
        /// Calculate the ability score modifier: floor((score - 10) / 2).
        /// </summary>
        public static int GetModifier(int abilityScore)
        {
            return Mathf.FloorToInt((abilityScore - 10) / 2f);
        }

        /// <summary>
        /// Returns a read-only view of the roll history (most recent last).
        /// </summary>
        public static IReadOnlyList<int> RollHistory => _rollHistory;

        /// <summary>
        /// Clears the roll history.
        /// </summary>
        public static void ClearHistory()
        {
            _rollHistory.Clear();
        }

        private static void AddToHistory(int result)
        {
            _rollHistory.Add(result);
            if (_rollHistory.Count > MAX_HISTORY)
            {
                _rollHistory.RemoveAt(0);
            }
        }
    }
}
