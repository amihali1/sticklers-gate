namespace SticklersGate.Data
{
    /// <summary>
    /// The six core ability scores, following D&D 5e conventions.
    /// </summary>
    public enum AbilityScore
    {
        Strength,
        Dexterity,
        Constitution,
        Intelligence,
        Wisdom,
        Charisma
    }

    /// <summary>
    /// Damage types that can be dealt by attacks and spells.
    /// </summary>
    public enum DamageType
    {
        Slashing,
        Piercing,
        Bludgeoning,
        Fire,
        Cold,
        Lightning,
        Radiant,
        Necrotic,
        Psychic,
        Force,
        Thunder
    }

    /// <summary>
    /// What a combat ability targets.
    /// </summary>
    public enum AbilityTargetType
    {
        SingleEnemy,
        SingleAlly,
        /// <summary>Items only. Abilities use grid areas (<see cref="Point"/> / emanation) instead.</summary>
        AllEnemies,
        /// <summary>Items only. Abilities use grid areas (<see cref="Point"/> / emanation) instead.</summary>
        AllAllies,
        Self,
        /// <summary>A tile within range: the center of a <see cref="AreaShape.Sphere"/>.</summary>
        Point
    }

    /// <summary>
    /// Shape of an ability's area of effect on the grid (D&amp;D 5e templates).
    /// </summary>
    public enum AreaShape
    {
        /// <summary>Single target, no area.</summary>
        None,
        /// <summary>Every tile within the radius of a chosen point (Fireball). Can include the caster.</summary>
        Sphere,
        /// <summary>Every tile within the radius of the caster. Harmful ones exclude the caster (Thunderwave); ally-only auras include them (Aura of Protection).</summary>
        Emanation
    }

    /// <summary>
    /// Which creatures inside an area are affected, relative to the caster.
    /// </summary>
    public enum AreaAffects
    {
        /// <summary>Everyone in the area, friend or foe (5e default for damaging areas).</summary>
        AllCreatures,
        AlliesOnly,
        EnemiesOnly
    }

    /// <summary>
    /// Category of a combat ability, determines UI presentation and logic branching.
    /// </summary>
    public enum AbilityCategory
    {
        MeleeAttack,
        RangedAttack,
        Spell,
        Heal,
        Buff,
        Debuff
    }

    /// <summary>
    /// Status effects that can be applied to combatants.
    /// </summary>
    public enum StatusEffectType
    {
        None,
        Poisoned,
        Stunned,
        Blessed,
        Shielded,
        Burning,
        /// <summary>Movement reduced (Ray of Frost).</summary>
        Slowed,
        Frightened,
        Invisible,
        /// <summary>Can't move; attacks against it have advantage, its attacks have disadvantage (Entangle).</summary>
        Restrained,
        /// <summary>Barbarian Rage: bonus melee damage, half damage from slashing/piercing/bludgeoning.</summary>
        Raging,
        /// <summary>Hunter's Mark / Hex: the marker's attacks deal bonus damage to this unit.</summary>
        Marked
    }

    /// <summary>
    /// Phases of the combat state machine.
    /// </summary>
    public enum CombatPhase
    {
        Setup,
        RollInitiative,
        TurnStart,
        PlayerChooseMovement,
        PlayerChooseAction,
        PlayerChooseTarget,
        ResolveAction,
        TurnEnd,
        CheckEndCondition,
        Victory,
        Defeat
    }

    /// <summary>
    /// Playable character class types.
    /// </summary>
    public enum CharacterClassType
    {
        Sticknight,
        Stickwizard,
        Stickrogue,
        Stickleric,
        Sticksorcerer,
        Stickwarlock,
        Stickdruid,
        Stickbard,
        Stickbarbarian,
        Stickmonk,
        Stickranger,
        Stickpaladin
    }

    /// <summary>
    /// Terrain of a single grid tile. Affects movement cost, line of fire, and
    /// combat modifiers. Only Normal is used in the initial encounters; the rest
    /// are wired up as content requires them.
    /// </summary>
    public enum TerrainType
    {
        /// <summary>Flat, walkable, movement cost 1.</summary>
        Normal,

        /// <summary>Walkable at 2× movement cost (rubble, mud, brush).</summary>
        Difficult,

        /// <summary>Walkable; grants +1 AC and +1 ranged range to occupants.</summary>
        Elevated,

        /// <summary>Walkable at 2× movement cost; halves fire damage taken.</summary>
        Water,

        /// <summary>Walkable but deals 2d6 fire on enter and at turn start.</summary>
        Lava,

        /// <summary>Impassable; entering (forced movement) is a lethal fall.</summary>
        Abyss,

        /// <summary>Walkable; grants +2 AC versus ranged attacks.</summary>
        Cover
    }

    /// <summary>
    /// Enemy tier for difficulty scaling and AI behavior.
    /// </summary>
    public enum EnemyTier
    {
        Minion,
        Standard,
        Elite,
        Boss
    }

    /// <summary>
    /// How the enemy AI selects its target.
    /// </summary>
    public enum AITargetPreference
    {
        Random,
        LowestHP,
        HighestThreat,
        Healers
    }

    /// <summary>
    /// Victory condition for an encounter.
    /// </summary>
    public enum VictoryCondition
    {
        DefeatAll,
        DefeatBoss,
        SurviveTurns
    }

    /// <summary>
    /// Type of item for inventory and UI categorization.
    /// </summary>
    public enum ItemType
    {
        Consumable,
        Weapon,
        Armor,
        Accessory
    }

    /// <summary>
    /// Rarity tier for items.
    /// </summary>
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }
}
