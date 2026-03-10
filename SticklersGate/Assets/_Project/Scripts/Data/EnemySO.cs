using UnityEngine;

namespace SticklersGate.Data
{
    /// <summary>
    /// Defines an enemy type with stats, abilities, AI behavior, and rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "SticklersGate/EnemyData")]
    public class EnemySO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _enemyName;
        [SerializeField] private EnemyTier _tier;
        [SerializeField][TextArea] private string _description;

        [Header("Stats")]
        [SerializeField] private StatBlock _stats;
        [SerializeField] private int _hitPoints;
        [SerializeField] private int _armorClass;

        [Header("Combat")]
        [SerializeField] private AbilitySO[] _abilities;
        [SerializeField] private CombatRow _preferredRow;

        [Header("AI Behavior")]
        [SerializeField][Range(0f, 1f)] private float _aggression = 0.8f;
        [SerializeField] private AITargetPreference _targetPreference;
        [SerializeField][Range(0f, 1f)] private float _retreatThreshold;
        [SerializeField] private bool _canFlee;

        [Header("Rewards")]
        [SerializeField] private int _xpReward;
        [SerializeField] private int _goldMin;
        [SerializeField] private int _goldMax;

        [Header("Sprites")]
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Sprite _attackSprite;
        [SerializeField] private Sprite _hitSprite;
        [SerializeField] private Sprite _deathSprite;

        [Header("Narrator")]
        [SerializeField][TextArea] private string[] _introQuips;
        [SerializeField][TextArea] private string[] _deathQuips;

        // --- Public Properties ---

        public string EnemyName => _enemyName;
        public EnemyTier Tier => _tier;
        public string Description => _description;

        public StatBlock Stats => _stats;
        public int HitPoints => _hitPoints;
        public int ArmorClass => _armorClass;

        public AbilitySO[] Abilities => _abilities;
        public CombatRow PreferredRow => _preferredRow;

        public float Aggression => _aggression;
        public AITargetPreference TargetPreference => _targetPreference;
        public float RetreatThreshold => _retreatThreshold;
        public bool CanFlee => _canFlee;

        public int XPReward => _xpReward;
        public int GoldMin => _goldMin;
        public int GoldMax => _goldMax;

        public Sprite IdleSprite => _idleSprite;
        public Sprite AttackSprite => _attackSprite;
        public Sprite HitSprite => _hitSprite;
        public Sprite DeathSprite => _deathSprite;

        public string[] IntroQuips => _introQuips;
        public string[] DeathQuips => _deathQuips;
    }
}
