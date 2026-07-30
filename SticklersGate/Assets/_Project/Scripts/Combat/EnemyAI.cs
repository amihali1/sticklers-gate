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

        /// <summary>
        /// Chooses a tile for an enemy to move to this turn. Melee units close to
        /// adjacency; ranged units hold at their weapon range (kite). Returns the
        /// enemy's current position when no move improves its footing or there are
        /// no living targets. Only considers reachable, unoccupied tiles.
        /// </summary>
        public static GridPosition DecideMovement(CombatUnit enemy, CombatGrid grid, List<CombatUnit> playerUnits)
        {
            if (grid == null) return enemy.Position;

            var target = NearestLivingTarget(enemy, playerUnits);
            if (target == null) return enemy.Position;

            int desiredRange = MaxAbilityRange(enemy);

            // Staying put is always an option.
            var candidates = Pathfinder.GetReachablePositions(grid, enemy.Position, enemy.MovementSpeed);
            candidates.Add(enemy.Position);

            GridPosition best = enemy.Position;
            int bestOffset = int.MaxValue; // |distance - desiredRange|
            int bestDistance = int.MaxValue;

            foreach (var tile in candidates)
            {
                int dist = tile.ManhattanDistanceTo(target.Position);
                int offset = Mathf.Abs(dist - desiredRange);
                if (offset < bestOffset || (offset == bestOffset && dist < bestDistance))
                {
                    bestOffset = offset;
                    bestDistance = dist;
                    best = tile;
                }
            }

            return best;
        }

        /// <summary>Living, conscious target nearest to the enemy by tile distance.</summary>
        private static CombatUnit NearestLivingTarget(CombatUnit enemy, List<CombatUnit> playerUnits)
        {
            CombatUnit nearest = null;
            int nearestDist = int.MaxValue;
            foreach (var unit in playerUnits)
            {
                if (!unit.IsAlive || !unit.IsConscious) continue;
                int dist = enemy.Position.ManhattanDistanceTo(unit.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = unit;
                }
            }
            return nearest;
        }

        /// <summary>Largest range among the enemy's abilities, floored at melee (1).</summary>
        private static int MaxAbilityRange(CombatUnit enemy)
        {
            int max = GameConstants.DEFAULT_MELEE_RANGE;
            foreach (var ability in enemy.Abilities)
            {
                if (ability.Data.Range > max) max = ability.Data.Range;
            }
            return max;
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
                    // Threat approximated by peak ability damage output (casters/DPS).
                    CombatUnit highest = targets[0];
                    int highestThreat = MaxAbilityDamage(highest);
                    foreach (var t in targets)
                    {
                        int threat = MaxAbilityDamage(t);
                        if (threat > highestThreat)
                        {
                            highestThreat = threat;
                            highest = t;
                        }
                    }
                    return highest;

                case AITargetPreference.Healers:
                    // Prefer units that can heal (support), else fall back to first.
                    foreach (var t in targets)
                    {
                        if (HasHealAbility(t)) return t;
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

        /// <summary>
        /// Peak single-hit damage a unit's abilities can roll (dice max + flat).
        /// Used to approximate how threatening a target is.
        /// </summary>
        private static int MaxAbilityDamage(CombatUnit unit)
        {
            int max = 0;
            foreach (var ability in unit.Abilities)
            {
                var data = ability.Data;
                if (!data.DealsDamage) continue;
                int potential = data.DamageDiceCount * data.DamageDiceSides + data.FlatDamageBonus;
                if (potential > max) max = potential;
            }
            return max;
        }

        /// <summary>True if any of the unit's abilities can heal.</summary>
        private static bool HasHealAbility(CombatUnit unit)
        {
            foreach (var ability in unit.Abilities)
            {
                if (ability.Data.Heals) return true;
            }
            return false;
        }
    }
}
