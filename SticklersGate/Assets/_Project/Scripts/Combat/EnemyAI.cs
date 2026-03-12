using System.Collections.Generic;
using UnityEngine;
using SticklersGate.Data;

namespace SticklersGate.Combat
{
    /// <summary>
    /// Result of an AI decision: which ability to use and who to target.
    /// </summary>
    public struct AIDecision
    {
        public RuntimeAbility ChosenAbility;
        public CombatUnit Target;
    }

    /// <summary>
    /// Selects actions and targets for enemy combatants.
    /// Priority: self-preservation → heal → select target by preference → pick ability.
    /// 20% chance of suboptimal choice for flavor.
    /// </summary>
    public static class EnemyAI
    {
        private const float SUBOPTIMAL_CHANCE = 0.2f;

        /// <summary>
        /// Decide what action an enemy unit should take this turn.
        /// </summary>
        public static AIDecision Decide(CombatUnit enemy, List<CombatUnit> playerUnits, List<CombatUnit> enemyUnits)
        {
            // enemyUnits reserved for future group tactics (ally healing, coordinated attacks)
            var decision = new AIDecision();
            var enemyData = enemy.EnemyData;

            // Get available abilities (off cooldown)
            var readyAbilities = new List<RuntimeAbility>();
            foreach (var ability in enemy.Abilities)
            {
                if (ability.IsReady) readyAbilities.Add(ability);
            }

            if (readyAbilities.Count == 0)
            {
                // No abilities ready — shouldn't happen, but defend
                return decision;
            }

            // Self-preservation: if low HP and can heal, do it
            if (enemyData != null && enemyData.RetreatThreshold > 0)
            {
                float hpPercent = (float)enemy.CurrentHP / enemy.MaxHP;
                if (hpPercent <= enemyData.RetreatThreshold)
                {
                    var healAbility = FindHealAbility(readyAbilities);
                    if (healAbility != null)
                    {
                        decision.ChosenAbility = healAbility;
                        decision.Target = enemy;
                        return decision;
                    }
                }
            }

            // Get valid targets
            var validTargets = GetValidTargets(playerUnits);
            if (validTargets.Count == 0) return decision;

            // 20% chance of random suboptimal choice
            if (Random.value < SUBOPTIMAL_CHANCE)
            {
                decision.ChosenAbility = readyAbilities[Random.Range(0, readyAbilities.Count)];
                decision.Target = SelectTarget(validTargets, AITargetPreference.Random);
                return decision;
            }

            // Pick best damaging ability, weighing multi-target higher when multiple targets exist
            RuntimeAbility bestAbility = null;
            int bestScore = 0;

            foreach (var ability in readyAbilities)
            {
                if (ability.Data.DealsDamage)
                {
                    int baseDamage = ability.Data.DamageDiceCount * ability.Data.DamageDiceSides + ability.Data.FlatDamageBonus;
                    int score = baseDamage;

                    // Multi-target abilities score higher with more targets
                    if (ability.Data.TargetType == AbilityTargetType.AllEnemies)
                    {
                        score *= validTargets.Count;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAbility = ability;
                    }
                }
            }

            // Fall back to first ready ability if no damage ability found
            decision.ChosenAbility = bestAbility ?? readyAbilities[0];

            // Select target based on AI preference
            // For multi-target abilities, target is just a placeholder (CombatStateMachine gathers all)
            var preference = enemyData != null ? enemyData.TargetPreference : AITargetPreference.Random;
            decision.Target = SelectTarget(validTargets, preference);

            return decision;
        }

        private static CombatUnit SelectTarget(List<CombatUnit> targets, AITargetPreference preference)
        {
            if (targets.Count == 0) return null;

            switch (preference)
            {
                case AITargetPreference.LowestHP:
                    CombatUnit lowest = targets[0];
                    foreach (var t in targets)
                    {
                        if (t.CurrentHP < lowest.CurrentHP) lowest = t;
                    }
                    return lowest;

                case AITargetPreference.HighestThreat:
                    // Threat approximated by damage output (back row = casters = high threat)
                    CombatUnit highest = targets[0];
                    foreach (var t in targets)
                    {
                        if (t.Row == CombatRow.BackRow && highest.Row != CombatRow.BackRow)
                            highest = t;
                    }
                    return highest;

                case AITargetPreference.Healers:
                    // Target anyone in back row first (healers tend to be there)
                    foreach (var t in targets)
                    {
                        if (t.Row == CombatRow.BackRow) return t;
                    }
                    return targets[0];

                case AITargetPreference.Random:
                default:
                    return targets[Random.Range(0, targets.Count)];
            }
        }

        private static List<CombatUnit> GetValidTargets(List<CombatUnit> units)
        {
            var valid = new List<CombatUnit>();
            foreach (var unit in units)
            {
                if (unit.IsAlive && unit.IsConscious) valid.Add(unit);
            }
            return valid;
        }

        private static RuntimeAbility FindHealAbility(List<RuntimeAbility> abilities)
        {
            foreach (var ability in abilities)
            {
                if (ability.Data.Heals) return ability;
            }
            return null;
        }
    }
}
