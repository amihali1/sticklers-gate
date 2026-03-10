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
    }
}
