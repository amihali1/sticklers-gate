using System;
using System.Collections.Generic;
using UnityEngine;
using SticklersGate.Data;

namespace SticklersGate.Combat
{
    /// <summary>
    /// Tracks the cooldown state of an ability at runtime.
    /// </summary>
    [Serializable]
    public class RuntimeAbility
    {
        public AbilitySO Data { get; }
        public int CurrentCooldown { get; private set; }
        public bool IsReady => CurrentCooldown <= 0;

        public RuntimeAbility(AbilitySO data)
        {
            Data = data;
            CurrentCooldown = 0;
        }

        /// <summary>
        /// Put this ability on cooldown.
        /// </summary>
        public void StartCooldown()
        {
            CurrentCooldown = Data.CooldownTurns;
        }

        /// <summary>
        /// Reduce cooldown by one turn.
        /// </summary>
        public void TickCooldown()
        {
            if (CurrentCooldown > 0) CurrentCooldown--;
        }
    }

    /// <summary>
    /// Tracks an active status effect on a combatant.
    /// </summary>
    [Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffectType Type { get; }
        public int RemainingTurns { get; private set; }

        public ActiveStatusEffect(StatusEffectType type, int duration)
        {
            Type = type;
            RemainingTurns = duration;
        }

        /// <summary>
        /// Reduce duration by one turn. Returns true if the effect has expired.
        /// </summary>
        public bool Tick()
        {
            RemainingTurns--;
            return RemainingTurns <= 0;
        }
    }

    /// <summary>
    /// Mutable runtime state of a combatant. Created from CharacterClassSO or EnemySO at combat start.
    /// SOs are never mutated — this class holds all per-combat state.
    /// </summary>
    public class CombatUnit
    {
        // --- Identity ---
        public string Name { get; }
        public bool IsPlayer { get; }

        // --- Stats (copied from SO) ---
        public StatBlock Stats { get; }
        public int MaxHP { get; }
        public int BaseAC { get; }

        // --- Mutable State ---
        public int CurrentHP { get; private set; }
        public int Initiative { get; set; }
        public CombatRow Row { get; set; }
        public bool IsAlive { get; private set; } = true;
        public bool IsConscious { get; private set; } = true;
        public int DeathSaveSuccesses { get; private set; }
        public int DeathSaveFailures { get; private set; }
        public int BonusAC { get; set; }

        // --- Abilities & Effects ---
        public List<RuntimeAbility> Abilities { get; }
        public List<ActiveStatusEffect> StatusEffects { get; } = new List<ActiveStatusEffect>();

        // --- Source Data (for sprites, quips, etc.) ---
        public CharacterClassSO ClassData { get; }
        public EnemySO EnemyData { get; }

        // --- Events ---
        public event Action<int> OnDamageTaken;
        public event Action<int> OnHealed;
        public event Action OnDeath;
        public event Action<StatusEffectType> OnStatusEffectApplied;
        public event Action<StatusEffectType> OnStatusEffectRemoved;

        private CombatUnit(string name, bool isPlayer, StatBlock stats, int maxHP, int baseAC,
            CombatRow row, List<RuntimeAbility> abilities,
            CharacterClassSO classData, EnemySO enemyData)
        {
            Name = name;
            IsPlayer = isPlayer;
            Stats = stats;
            MaxHP = maxHP;
            BaseAC = baseAC;
            CurrentHP = maxHP;
            Row = row;
            Abilities = abilities;
            ClassData = classData;
            EnemyData = enemyData;
        }

        /// <summary>
        /// Create a CombatUnit from a player character's class data.
        /// </summary>
        public static CombatUnit FromPlayerCharacter(string name, CharacterClassSO classData, int level)
        {
            var abilities = new List<RuntimeAbility>();
            foreach (var a in classData.GetAbilitiesAtLevel(level))
            {
                abilities.Add(new RuntimeAbility(a));
            }

            return new CombatUnit(
                name: name,
                isPlayer: true,
                stats: classData.BaseStats,
                maxHP: classData.GetMaxHPAtLevel(level),
                baseAC: classData.BaseAC,
                row: classData.PreferredRow,
                abilities: abilities,
                classData: classData,
                enemyData: null
            );
        }

        /// <summary>
        /// Create a CombatUnit from an enemy data definition.
        /// </summary>
        public static CombatUnit FromEnemy(EnemySO enemyData, string nameOverride = null)
        {
            var abilities = new List<RuntimeAbility>();
            var enemyAbilities = enemyData.Abilities ?? System.Array.Empty<AbilitySO>();
            foreach (var a in enemyAbilities)
            {
                abilities.Add(new RuntimeAbility(a));
            }

            return new CombatUnit(
                name: nameOverride ?? enemyData.EnemyName,
                isPlayer: false,
                stats: enemyData.Stats,
                maxHP: enemyData.HitPoints,
                baseAC: enemyData.ArmorClass,
                row: enemyData.PreferredRow,
                abilities: abilities,
                classData: null,
                enemyData: enemyData
            );
        }

        /// <summary>
        /// Current effective AC including bonuses and row modifiers.
        /// </summary>
        public int EffectiveAC => BaseAC + BonusAC;

        /// <summary>
        /// Apply damage to this unit. Handles downing and death.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;

            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnDamageTaken?.Invoke(amount);

            if (CurrentHP <= 0 && IsConscious)
            {
                IsConscious = false;
            }
        }

        /// <summary>
        /// Heal this unit. Cannot exceed max HP. Cannot heal dead units.
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive) return;

            int actual = Mathf.Min(amount, MaxHP - CurrentHP);
            CurrentHP += actual;
            OnHealed?.Invoke(actual);

            if (!IsConscious && CurrentHP > 0)
            {
                IsConscious = true;
                DeathSaveSuccesses = 0;
                DeathSaveFailures = 0;
            }
        }

        /// <summary>
        /// Record a death saving throw result. Returns true if the unit's fate is decided.
        /// </summary>
        public bool RecordDeathSave(bool success)
        {
            if (success)
            {
                DeathSaveSuccesses++;
                if (DeathSaveSuccesses >= GameConstants.DEATH_SAVE_SUCCESS_NEEDED)
                {
                    // Stabilized — still at 0 HP but no longer dying
                    return true;
                }
            }
            else
            {
                DeathSaveFailures++;
                if (DeathSaveFailures >= GameConstants.DEATH_SAVE_FAIL_NEEDED)
                {
                    Die();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Revive from death saves (nat 20 on death save).
        /// </summary>
        public void ReviveFromDeathSave()
        {
            CurrentHP = 1;
            IsConscious = true;
            DeathSaveSuccesses = 0;
            DeathSaveFailures = 0;
        }

        /// <summary>
        /// Permanently kill this unit.
        /// </summary>
        public void Die()
        {
            IsAlive = false;
            IsConscious = false;
            CurrentHP = 0;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Apply a status effect. Replaces existing effect of the same type.
        /// </summary>
        public void ApplyStatusEffect(StatusEffectType type, int duration)
        {
            if (type == StatusEffectType.None) return;

            RemoveStatusEffect(type);
            StatusEffects.Add(new ActiveStatusEffect(type, duration));
            OnStatusEffectApplied?.Invoke(type);
        }

        /// <summary>
        /// Remove a status effect by type.
        /// </summary>
        public void RemoveStatusEffect(StatusEffectType type)
        {
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                if (StatusEffects[i].Type == type)
                {
                    StatusEffects.RemoveAt(i);
                    OnStatusEffectRemoved?.Invoke(type);
                }
            }
        }

        /// <summary>
        /// Returns true if the unit has the given status effect.
        /// </summary>
        public bool HasStatusEffect(StatusEffectType type)
        {
            foreach (var effect in StatusEffects)
            {
                if (effect.Type == type) return true;
            }
            return false;
        }

        /// <summary>
        /// Tick all status effects and cooldowns at turn start. Removes expired effects.
        /// </summary>
        public void TickTurnStart()
        {
            // Tick status effects
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                if (StatusEffects[i].Tick())
                {
                    var type = StatusEffects[i].Type;
                    StatusEffects.RemoveAt(i);
                    OnStatusEffectRemoved?.Invoke(type);
                }
            }

            // Tick ability cooldowns
            foreach (var ability in Abilities)
            {
                ability.TickCooldown();
            }

            // Reset per-turn bonuses
            BonusAC = 0;
        }
    }
}
