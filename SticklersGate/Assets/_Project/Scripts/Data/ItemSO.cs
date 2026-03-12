using UnityEngine;

namespace SticklersGate.Data
{
    /// <summary>
    /// Defines an item: consumable, weapon, armor, or accessory.
    /// Consumables have a use effect (heal, damage, buff). Equipment modifies stats.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "SticklersGate/ItemData")]
    public class ItemSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _itemName;
        [SerializeField] private ItemType _itemType;
        [SerializeField] private ItemRarity _rarity;
        [SerializeField][TextArea] private string _description;
        [SerializeField] private int _goldValue;

        [Header("Consumable Effect")]
        [SerializeField] private int _healAmount;
        [SerializeField] private int _damageAmount;
        [SerializeField] private DamageType _damageType;
        [SerializeField] private AbilityTargetType _targetType;
        [SerializeField] private StatusEffectType _removesEffect;
        [SerializeField] private StatusEffectType _appliesEffect;
        [SerializeField] private int _effectDuration;

        [Header("Equipment Stats")]
        [SerializeField] private int _acBonus;
        [SerializeField] private int _damageBonus;

        [Header("Narrator")]
        [SerializeField][TextArea] private string[] _useQuips;

        // --- Public Properties ---

        public string ItemName => _itemName;
        public ItemType ItemType => _itemType;
        public ItemRarity Rarity => _rarity;
        public string Description => _description;
        public int GoldValue => _goldValue;

        public int HealAmount => _healAmount;
        public int DamageAmount => _damageAmount;
        public DamageType DamageType => _damageType;
        public AbilityTargetType TargetType => _targetType;
        public StatusEffectType RemovesEffect => _removesEffect;
        public StatusEffectType AppliesEffect => _appliesEffect;
        public int EffectDuration => _effectDuration;

        public int ACBonus => _acBonus;
        public int DamageBonus => _damageBonus;

        public string[] UseQuips => _useQuips;

        /// <summary>
        /// Whether this item is a consumable that can be used in combat.
        /// </summary>
        public bool IsConsumable => _itemType == ItemType.Consumable;

        /// <summary>
        /// Whether this consumable heals.
        /// </summary>
        public bool Heals => _healAmount > 0;

        /// <summary>
        /// Whether this consumable deals damage.
        /// </summary>
        public bool DealsDamage => _damageAmount > 0;

        /// <summary>
        /// Whether this consumable removes a status effect.
        /// </summary>
        public bool RemovesStatus => _removesEffect != StatusEffectType.None;
    }
}
