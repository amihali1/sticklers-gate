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

            // Back row gets AC bonus vs melee attacks
            if (target.Row == CombatRow.BackRow &&
                (ability.Category == AbilityCategory.MeleeAttack))
            {
                result.TargetAC += GameConstants.BACK_ROW_AC_BONUS;
            }

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
    }
}
