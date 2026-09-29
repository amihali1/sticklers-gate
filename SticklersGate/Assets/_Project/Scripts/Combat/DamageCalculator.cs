using SticklersGate.Data;
using SticklersGate.Utils;

namespace SticklersGate.Combat
{
    /// <summary>
    /// Result of an attack roll resolution.
    /// </summary>
    public struct AttackResult
    {
        public int D20Roll;
        public int Modifier;
        public int TotalAttackRoll;
        public int TargetAC;
        public bool IsHit;
        public bool IsCriticalHit;
        public bool IsCriticalFail;
        public int DamageRolled;
        public int TotalDamage;
        public StatusEffectType AppliedEffect;
        public int EffectDuration;
        /// <summary>Whether the target was within the ability's grid range when resolved (informational; enforcement lives in movement/UI).</summary>
        public bool WasInRange;
    }

    /// <summary>
    /// Result of a healing action.
    /// </summary>
    public struct HealResult
    {
        public int DiceRolled;
        public int FlatBonus;
        public int TotalHealed;
    }

    /// <summary>
    /// Result of a saving throw.
    /// </summary>
    public struct SaveResult
    {
        public int D20Roll;
        public int Modifier;
        public int TotalRoll;
        public int DC;
        public bool Success;
    }

    /// <summary>
    /// Resolves combat actions: attacks, heals, and saving throws.
    /// Returns result structs with full roll details for UI display.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Resolve an attack: d20 + modifier vs AC, then roll damage on hit.
        /// </summary>
        /// <param name="attacker">The unit making the attack.</param>
        /// <param name="target">The unit being attacked.</param>
        /// <param name="ability">The ability being used for the attack.</param>
        /// <param name="hasAdvantage">Whether the attacker has advantage on the attack roll.</param>
        /// <param name="hasDisadvantage">Whether the attacker has disadvantage on the attack roll.</param>
        /// <returns>The result of the attack resolution.</returns>
        public static AttackResult ResolveAttack(CombatUnit attacker, CombatUnit target, AbilitySO ability, bool hasAdvantage = false, bool hasDisadvantage = false)
        {
            var result = new AttackResult();

            // Roll d20
            if (hasAdvantage && !hasDisadvantage)
                result.D20Roll = DiceRoller.RollWithAdvantage();
            else if (hasDisadvantage && !hasAdvantage)
                result.D20Roll = DiceRoller.RollWithDisadvantage();
            else
                result.D20Roll = DiceRoller.RollD20();

            result.IsCriticalHit = DiceRoller.IsCriticalHit(result.D20Roll);
            result.IsCriticalFail = DiceRoller.IsCriticalFail(result.D20Roll);

            // Calculate attack roll
            result.Modifier = attacker.Stats.GetModifier(ability.AbilityModifier);
            result.TotalAttackRoll = result.D20Roll + result.Modifier;
            result.TargetAC = target.EffectiveAC;
            result.WasInRange = IsInRange(attacker, target, ability);

            // Determine hit (nat 20 always hits, nat 1 always misses)
            if (result.IsCriticalHit)
                result.IsHit = true;
            else if (result.IsCriticalFail)
                result.IsHit = false;
            else
                result.IsHit = result.TotalAttackRoll >= result.TargetAC;

            // Roll damage on hit
            if (result.IsHit)
            {
                int diceCount = ability.DamageDiceCount;
                if (result.IsCriticalHit) diceCount *= 2; // Double dice on crit

                result.DamageRolled = DiceRoller.RollMultiple(diceCount, ability.DamageDiceSides);
                result.TotalDamage = result.DamageRolled + result.Modifier + ability.FlatDamageBonus;

                if (result.TotalDamage < 0) result.TotalDamage = 0;

                // Apply status effect if ability has one
                if (ability.AppliesStatusEffect)
                {
                    result.AppliedEffect = ability.AppliesEffect;
                    result.EffectDuration = ability.EffectDurationTurns;
                }
            }

            return result;
        }

        /// <summary>
        /// Resolve a healing action: roll heal dice + flat bonus.
        /// </summary>
        /// <param name="healer">The unit performing the heal.</param>
        /// <param name="ability">The ability being used to heal.</param>
        /// <returns>The result of the healing action.</returns>
        public static HealResult ResolveHeal(CombatUnit healer, AbilitySO ability)
        {
            // healer param reserved for future stat-scaled healing
            var result = new HealResult();

            result.DiceRolled = DiceRoller.RollMultiple(ability.HealDiceCount, ability.HealDiceSides);
            result.FlatBonus = ability.FlatHealBonus;
            result.TotalHealed = result.DiceRolled + result.FlatBonus;

            if (result.TotalHealed < 0) result.TotalHealed = 0;

            return result;
        }

        /// <summary>
        /// Resolve a saving throw: d20 + modifier vs DC.
        /// </summary>
        /// <param name="target">The unit making the saving throw.</param>
        /// <param name="savingAbility">The ability score used for the saving throw.</param>
        /// <param name="dc">The difficulty class required to succeed.</param>
        /// <returns>The result of the saving throw.</returns>
        public static SaveResult ResolveSavingThrow(CombatUnit target, AbilityScore savingAbility, int dc)
        {
            var result = new SaveResult();

            result.D20Roll = DiceRoller.RollD20();
            result.Modifier = target.Stats.GetModifier(savingAbility);
            result.TotalRoll = result.D20Roll + result.Modifier;
            result.DC = dc;

            // Nat 20 always succeeds, nat 1 always fails
            if (DiceRoller.IsCriticalHit(result.D20Roll))
                result.Success = true;
            else if (DiceRoller.IsCriticalFail(result.D20Roll))
                result.Success = false;
            else
                result.Success = result.TotalRoll >= dc;

            return result;
        }

        /// <summary>
        /// Whether the target is within the ability's grid range (Manhattan
        /// distance). Self and AoE (AllEnemies/AllAllies) abilities ignore range
        /// and always return true.
        /// </summary>
        /// <param name="attacker">The unit making the ability.</param>
        /// <param name="target">The unit being targeted.</param>
        /// <param name="ability">The ability determining range and target type.</param>
        /// <returns>True if the target is valid and in range, false otherwise.</returns>
        public static bool IsInRange(CombatUnit attacker, CombatUnit target, AbilitySO ability)
        {
            switch (ability.TargetType)
            {
                case AbilityTargetType.Self:
                case AbilityTargetType.AllEnemies:
                case AbilityTargetType.AllAllies:
                    return true;
                default:
                    if (attacker == null || target == null) return false;
                    return attacker.Position.ManhattanDistanceTo(target.Position) <= ability.Range;
            }
        }
    }
}
