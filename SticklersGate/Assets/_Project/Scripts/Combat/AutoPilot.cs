using System.Collections.Generic;
using SticklersGate.Data;

namespace SticklersGate.Combat
{
    /// <summary>
    /// What an automated player intends to do this turn.
    /// </summary>
    public class AutoPlan
    {
        /// <summary>Ability to use, or null to Defend.</summary>
        public RuntimeAbility Ability;
        /// <summary>Unit target for single-target abilities (also the AI's aim for areas).</summary>
        public CombatUnit Target;
        /// <summary>Center tile for Point (sphere) abilities.</summary>
        public GridPosition? Point;
        /// <summary>Short human-readable reason, for the autoplay log.</summary>
        public string Reason;

        public bool Defend => Ability == null;
    }

    /// <summary>
    /// Simple, deterministic-enough policy that plays the player side for
    /// automated play-testing (autoplay harness, balance sims). Movement reuses
    /// <see cref="EnemyAI.DecideMovement"/>; areas reuse
    /// <see cref="EnemyAI.BestAreaValue"/>. Priorities: heal a badly hurt ally,
    /// then the best damage, then a useful buff/debuff, else Defend.
    /// </summary>
    public static class AutoPilot
    {
        /// <summary>Allies at or below this fraction of max HP get healed first.</summary>
        public const float HEAL_THRESHOLD = 0.4f;

        /// <summary>Tile to move to this turn (may be the current tile).</summary>
        public static GridPosition ChooseMove(CombatUnit unit, CombatGrid grid, List<CombatUnit> foes)
        {
            return EnemyAI.DecideMovement(unit, grid, foes);
        }

        /// <summary>Choose this turn's action from the unit's ready abilities.</summary>
        public static AutoPlan ChooseAction(CombatUnit unit, List<CombatUnit> friends, List<CombatUnit> foes)
        {
            var ready = new List<RuntimeAbility>();
            foreach (var a in unit.Abilities)
            {
                if (a.IsReady) ready.Add(a);
            }

            return TryHeal(unit, ready, friends)
                ?? TryDamage(unit, ready, friends, foes)
                ?? TrySupport(unit, ready, friends, foes)
                ?? new AutoPlan { Reason = "nothing useful in range: defend" };
        }

        private static AutoPlan TryHeal(CombatUnit unit, List<RuntimeAbility> ready, List<CombatUnit> friends)
        {
            CombatUnit hurt = null;
            foreach (var f in friends)
            {
                if (!f.IsAlive) continue;
                if ((float)f.CurrentHP / f.MaxHP > HEAL_THRESHOLD) continue;
                if (hurt == null || f.CurrentHP < hurt.CurrentHP) hurt = f;
            }
            if (hurt == null) return null;

            foreach (var a in ready)
            {
                var data = a.Data;
                if (!data.Heals) continue;

                if (data.TargetType == AbilityTargetType.Point &&
                    unit.Position.ManhattanDistanceTo(hurt.Position) <= data.Range)
                {
                    return new AutoPlan { Ability = a, Target = hurt, Point = hurt.Position, Reason = $"area heal on {hurt.Name}" };
                }

                if (data.TargetType == AbilityTargetType.SingleAlly && DamageCalculator.IsInRange(unit, hurt, data))
                {
                    return new AutoPlan { Ability = a, Target = hurt, Reason = $"heal {hurt.Name}" };
                }

                if (data.TargetType == AbilityTargetType.Self && hurt == unit)
                {
                    return new AutoPlan { Ability = a, Target = unit, Reason = "heal self" };
                }
            }
            return null;
        }

        private static AutoPlan TryDamage(CombatUnit unit, List<RuntimeAbility> ready,
            List<CombatUnit> friends, List<CombatUnit> foes)
        {
            var living = new List<CombatUnit>();
            foreach (var f in foes)
            {
                if (f.IsAlive && f.IsConscious) living.Add(f);
            }
            if (living.Count == 0) return null;

            AutoPlan best = null;
            int bestScore = 0;

            foreach (var a in ready)
            {
                var data = a.Data;
                if (!data.DealsDamage) continue;

                int perTarget = data.DamageDiceCount * data.DamageDiceSides + data.FlatDamageBonus;
                AutoPlan plan;
                int score;

                if (data.IsArea)
                {
                    int value = EnemyAI.BestAreaValue(unit, data, living, friends, out var aim);
                    if (value <= 0 || aim == null) continue;
                    score = perTarget * value;
                    plan = new AutoPlan
                    {
                        Ability = a, Target = aim,
                        Point = data.TargetType == AbilityTargetType.Point ? aim.Position : (GridPosition?)null,
                        Reason = $"{data.AbilityName} catching {value} net foe(s)"
                    };
                }
                else
                {
                    CombatUnit target = null;
                    foreach (var foe in living)
                    {
                        if (!DamageCalculator.IsInRange(unit, foe, data)) continue;
                        if (target == null || foe.CurrentHP < target.CurrentHP) target = foe;
                    }
                    if (target == null) continue;

                    score = perTarget * data.HitCount * (1 + data.ExtraTargets);
                    plan = new AutoPlan { Ability = a, Target = target, Reason = $"{data.AbilityName} on {target.Name}" };
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = plan;
                }
            }

            return best;
        }

        private static AutoPlan TrySupport(CombatUnit unit, List<RuntimeAbility> ready,
            List<CombatUnit> friends, List<CombatUnit> foes)
        {
            foreach (var a in ready)
            {
                var data = a.Data;
                if (data.DealsDamage || data.Heals || !data.AppliesStatusEffect) continue;

                switch (data.TargetType)
                {
                    case AbilityTargetType.Self:
                        if (data.IsArea)
                        {
                            // Ally aura: worth it if it covers someone besides the caster
                            var covered = AreaTargeting.GetAffectedUnits(data, unit, unit.Position, friends);
                            if (covered.Count > 1)
                                return new AutoPlan { Ability = a, Target = unit, Reason = $"{data.AbilityName} covering {covered.Count}" };
                        }
                        else if (!unit.HasStatusEffect(data.AppliesEffect))
                        {
                            return new AutoPlan { Ability = a, Target = unit, Reason = $"{data.AbilityName} on self" };
                        }
                        break;

                    case AbilityTargetType.SingleAlly:
                        foreach (var f in friends)
                        {
                            if (f.IsAlive && f.IsConscious && f != unit && !f.HasStatusEffect(data.AppliesEffect) &&
                                DamageCalculator.IsInRange(unit, f, data))
                                return new AutoPlan { Ability = a, Target = f, Reason = $"{data.AbilityName} on {f.Name}" };
                        }
                        break;

                    case AbilityTargetType.SingleEnemy:
                        foreach (var foe in foes)
                        {
                            if (foe.IsAlive && foe.IsConscious && !foe.HasStatusEffect(data.AppliesEffect) &&
                                DamageCalculator.IsInRange(unit, foe, data))
                                return new AutoPlan { Ability = a, Target = foe, Reason = $"{data.AbilityName} on {foe.Name}" };
                        }
                        break;

                    case AbilityTargetType.Point:
                        int value = EnemyAI.BestAreaValue(unit, data, foes, friends, out var aim);
                        if (value > 0 && aim != null)
                            return new AutoPlan { Ability = a, Target = aim, Point = aim.Position, Reason = $"{data.AbilityName} catching {value}" };
                        break;
                }
            }
            return null;
        }
    }
}
