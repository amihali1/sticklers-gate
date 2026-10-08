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

        /// <summary>Unit that applied the effect (null for items/environment). Marked uses it.</summary>
        public CombatUnit Source { get; }

        /// <summary>
        /// Set when a unit applies an effect to itself during its own turn, so the
        /// end of that same turn doesn't consume a turn of duration.
        /// </summary>
        public bool SkipNextTick { get; private set; }

        public ActiveStatusEffect(StatusEffectType type, int duration, CombatUnit source = null, bool skipNextTick = false)
        {
            Type = type;
            RemainingTurns = duration;
            Source = source;
            SkipNextTick = skipNextTick;
        }

        /// <summary>
        /// Reduce duration by one turn. Returns true if the effect has expired.
        /// </summary>
        public bool Tick()
        {
            if (SkipNextTick)
            {
                SkipNextTick = false;
                return false;
            }

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

        /// <summary>Base tiles this unit can move per turn (from its SO).</summary>
        public int MovementSpeed { get; }

        /// <summary>
        /// Tiles this unit can move this turn after status effects: 0 while
        /// Restrained, reduced while Slowed. Use this for pathing.
        /// </summary>
        public int EffectiveMovement
        {
            get
            {
                if (HasStatusEffect(StatusEffectType.Restrained)) return 0;
                int penalty = HasStatusEffect(StatusEffectType.Slowed) ? GameConstants.SLOWED_MOVEMENT_PENALTY : 0;
                return Mathf.Max(0, MovementSpeed - penalty);
            }
        }

        // --- Mutable State ---
        public int CurrentHP { get; private set; }
        public int Initiative { get; set; }

        /// <summary>Current tile on the combat grid. Assigned at spawn, updated on movement.</summary>
        public GridPosition Position { get; set; }
        public bool IsAlive { get; private set; } = true;
        public bool IsConscious { get; private set; } = true;
        public int DeathSaveSuccesses { get; private set; }
        public int DeathSaveFailures { get; private set; }
        public bool IsStabilized { get; private set; }
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
            int movementSpeed, List<RuntimeAbility> abilities,
            CharacterClassSO classData, EnemySO enemyData)
        {
            Name = name;
            IsPlayer = isPlayer;
            Stats = stats;
            MaxHP = maxHP;
            BaseAC = baseAC;
            MovementSpeed = movementSpeed;
            CurrentHP = maxHP;
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
                movementSpeed: classData.MovementSpeed,
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
                movementSpeed: enemyData.MovementSpeed,
                abilities: abilities,
                classData: null,
                enemyData: enemyData
            );
        }

        /// <summary>
        /// Current effective AC including temporary bonuses (defend, Shielded).
        /// </summary>
        public int EffectiveAC =>
            BaseAC + BonusAC + (HasStatusEffect(StatusEffectType.Shielded) ? GameConstants.SHIELDED_AC_BONUS : 0);

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
                IsStabilized = false;
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
                    IsStabilized = true;
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
            IsStabilized = false;
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
        /// Duration counts this unit's turns; effects tick at the end of each of
        /// its turns. A unit applying an effect to itself (on its own turn) keeps
        /// the full duration through its following turns.
        /// </summary>
        /// <param name="source">Unit that applied it (used by Marked); null for items/environment.</param>
        public void ApplyStatusEffect(StatusEffectType type, int duration, CombatUnit source = null)
        {
            if (type == StatusEffectType.None) return;

            RemoveStatusEffect(type);
            StatusEffects.Add(new ActiveStatusEffect(type, duration, source, skipNextTick: source == this));
            OnStatusEffectApplied?.Invoke(type);
        }

        /// <summary>Returns the active effect of a type, or null.</summary>
        public ActiveStatusEffect GetStatusEffect(StatusEffectType type)
        {
            foreach (var effect in StatusEffects)
            {
                if (effect.Type == type) return effect;
            }
            return null;
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
        /// Tick ability cooldowns and reset per-turn bonuses at turn start.
        /// Status effects tick at turn end (<see cref="TickTurnEnd"/>).
        /// </summary>
        public void TickTurnStart()
        {
            // Tick ability cooldowns
            foreach (var ability in Abilities)
            {
                ability.TickCooldown();
            }

            // Reset per-turn bonuses
            BonusAC = 0;
        }

        /// <summary>
        /// Tick status effects at the end of this unit's turn (5e "until the end
        /// of its next turn"), so a 1-turn Stun skips exactly one turn and a
        /// 1-turn debuff lasts through the target's next turn. Removes expired effects.
        /// </summary>
        public void TickTurnEnd()
        {
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                if (StatusEffects[i].Tick())
                {
                    var type = StatusEffects[i].Type;
                    StatusEffects.RemoveAt(i);
                    OnStatusEffectRemoved?.Invoke(type);
                }
            }
        }
    }
}
