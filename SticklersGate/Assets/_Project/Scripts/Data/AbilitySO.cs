using UnityEngine;

namespace SticklersGate.Data
{
    /// <summary>
    /// Defines a combat ability: attack, spell, heal, buff, or debuff.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "SticklersGate/AbilityData")]
    public class AbilitySO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _abilityName;
        [SerializeField] private AbilityCategory _category;
        [SerializeField] private AbilityTargetType _targetType;
        [SerializeField][TextArea] private string _description;

        [Header("Attack / Damage")]
        [SerializeField] private int _damageDiceCount;
        [SerializeField] private int _damageDiceSides;
        [SerializeField] private DamageType _damageType;
        [SerializeField] private AbilityScore _abilityModifier;
        [SerializeField] private int _flatDamageBonus;

        [Header("Healing")]
        [SerializeField] private int _healDiceCount;
        [SerializeField] private int _healDiceSides;
        [SerializeField] private int _flatHealBonus;

        [Header("Status Effect")]
        [SerializeField] private StatusEffectType _appliesEffect;
        [SerializeField] private int _effectDurationTurns;
        [SerializeField] private AbilityScore _savingThrowAbility;
        [SerializeField] private int _savingThrowDC;

        [Tooltip("Resolve damage with a saving throw instead of an attack roll: full damage on a failed save, half on a success (5e Fireball). Uses the saving throw ability/DC above; any status effect only lands on a failed save.")]
        [SerializeField] private bool _saveForHalfDamage;

        [Header("Range")]
        [Tooltip("Grid range in tiles (Manhattan). Melee = 1, ranged 3-6. For Point abilities this is how far away the area can be centered. Self abilities ignore range.")]
        [SerializeField] private int _range = GameConstants.DEFAULT_MELEE_RANGE;

        [Header("Area of Effect")]
        [SerializeField] private AreaShape _areaShape;
        [Tooltip("Area radius in tiles (Manhattan).")]
        [SerializeField] private int _areaRadius;
        [SerializeField] private AreaAffects _areaAffects;

        [Header("Special")]
        [Tooltip("Status effect applied to the user after the ability resolves (Shadow Step: Invisible).")]
        [SerializeField] private StatusEffectType _selfEffect;
        [SerializeField] private int _selfEffectDurationTurns;
        [Tooltip("Percent of damage dealt that heals the user (Vampiric Touch: 50).")]
        [SerializeField][Range(0, 100)] private int _lifestealPercent;
        [Tooltip("Separate attack rolls against the target (Flurry of Blows: 2). Single-target damage only.")]
        [SerializeField][Min(1)] private int _hitCount = 1;
        [Tooltip("Additional enemies within range also hit (Twinned Spell: 1). Single-target enemy abilities only.")]
        [SerializeField][Min(0)] private int _extraTargets;
        [Tooltip("Damage dice deal maximum instead of rolling (Empowered Spell).")]
        [SerializeField] private bool _maximizeDamage;

        [Header("Cooldown")]
        [SerializeField] private int _cooldownTurns;
        [SerializeField] private int _unlockLevel = 1;

        [Header("Narrator Quips")]
        [SerializeField][TextArea] private string[] _hitQuips;
        [SerializeField][TextArea] private string[] _missQuips;
        [SerializeField][TextArea] private string[] _critQuips;

        // --- Public Properties ---

        public string AbilityName => _abilityName;
        public AbilityCategory Category => _category;
        public AbilityTargetType TargetType => _targetType;
        public string Description => _description;

        public int DamageDiceCount => _damageDiceCount;
        public int DamageDiceSides => _damageDiceSides;
        public DamageType DamageType => _damageType;
        public AbilityScore AbilityModifier => _abilityModifier;
        public int FlatDamageBonus => _flatDamageBonus;

        public int HealDiceCount => _healDiceCount;
        public int HealDiceSides => _healDiceSides;
        public int FlatHealBonus => _flatHealBonus;

        public StatusEffectType AppliesEffect => _appliesEffect;
        public int EffectDurationTurns => _effectDurationTurns;
        public AbilityScore SavingThrowAbility => _savingThrowAbility;
        public int SavingThrowDC => _savingThrowDC;
        public bool SaveForHalfDamage => _saveForHalfDamage;

        public AreaShape AreaShape => _areaShape;
        public int AreaRadius => _areaRadius;
        public AreaAffects AreaAffects => _areaAffects;

        public StatusEffectType SelfEffect => _selfEffect;
        public int SelfEffectDurationTurns => _selfEffectDurationTurns;
        public int LifestealPercent => _lifestealPercent;
        public int HitCount => Mathf.Max(1, _hitCount);
        public int ExtraTargets => Mathf.Max(0, _extraTargets);
        public bool MaximizeDamage => _maximizeDamage;

        /// <summary>
        /// Range in grid tiles (Manhattan distance). AoE abilities ignore this.
        /// </summary>
        public int Range => _range;

        public int CooldownTurns => _cooldownTurns;
        public int UnlockLevel => _unlockLevel;

        public string[] HitQuips => _hitQuips;
        public string[] MissQuips => _missQuips;
        public string[] CritQuips => _critQuips;

        /// <summary>
        /// Whether this ability deals damage.
        /// </summary>
        public bool DealsDamage => (_damageDiceCount > 0 && _damageDiceSides > 0) || _flatDamageBonus > 0;

        /// <summary>
        /// Whether this ability heals.
        /// </summary>
        public bool Heals => (_healDiceCount > 0 && _healDiceSides > 0) || _flatHealBonus > 0;

        /// <summary>
        /// Whether this ability applies a status effect.
        /// </summary>
        public bool AppliesStatusEffect => _appliesEffect != StatusEffectType.None && _effectDurationTurns > 0;

        /// <summary>
        /// Whether this ability has a cooldown.
        /// </summary>
        public bool HasCooldown => _cooldownTurns > 0;

        /// <summary>Whether this ability affects an area on the grid.</summary>
        public bool IsArea => _areaShape != AreaShape.None;

        /// <summary>Whether this ability applies a status effect to its user.</summary>
        public bool AppliesSelfEffect => _selfEffect != StatusEffectType.None && _selfEffectDurationTurns > 0;
    }
}
