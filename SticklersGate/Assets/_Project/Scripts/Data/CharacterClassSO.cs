using UnityEngine;

namespace SticklersGate.Data
{
    /// <summary>
    /// Defines a playable character class with base stats, abilities, and flavor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewClass", menuName = "SticklersGate/CharacterClassData")]
    public class CharacterClassSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _className;
        [SerializeField] private CharacterClassType _classType;
        [SerializeField][TextArea] private string _flavorText;

        [Header("Base Stats")]
        [SerializeField] private StatBlock _baseStats;
        [SerializeField] private int _baseAC;
        [SerializeField] private int _hitDiceSides;
        [SerializeField] private CombatRow _preferredRow;

        [Header("Abilities")]
        [SerializeField] private AbilitySO[] _level1Abilities;
        [SerializeField] private AbilitySO[] _level3Abilities;
        [SerializeField] private AbilitySO[] _level5Abilities;

        [Header("Sprites")]
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Sprite _attackSprite;
        [SerializeField] private Sprite _hitSprite;
        [SerializeField] private Sprite _deathSprite;

        // --- Public Properties ---

        public string ClassName => _className;
        public CharacterClassType ClassType => _classType;
        public string FlavorText => _flavorText;

        public StatBlock BaseStats => _baseStats;
        public int BaseAC => _baseAC;
        public int HitDiceSides => _hitDiceSides;
        public CombatRow PreferredRow => _preferredRow;

        public Sprite IdleSprite => _idleSprite;
        public Sprite AttackSprite => _attackSprite;
        public Sprite HitSprite => _hitSprite;
        public Sprite DeathSprite => _deathSprite;

        /// <summary>
        /// Calculates max HP at a given level.
        /// Level 1: max hit die + CON modifier.
        /// Level 2+: add hit die average (rounded up) + CON modifier per level.
        /// </summary>
        public int GetMaxHPAtLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > GameConstants.MAX_LEVEL) level = GameConstants.MAX_LEVEL;

            int conMod = _baseStats.GetModifier(AbilityScore.Constitution);
            int hp = _hitDiceSides + conMod;

            for (int i = 1; i < level; i++)
            {
                hp += (_hitDiceSides / 2 + 1) + conMod;
            }

            return hp;
        }

        /// <summary>
        /// Returns all abilities available at the given level.
        /// </summary>
        public AbilitySO[] GetAbilitiesAtLevel(int level)
        {
            if (level < 1) level = 1;

            var l1 = _level1Abilities ?? System.Array.Empty<AbilitySO>();
            var l3 = _level3Abilities ?? System.Array.Empty<AbilitySO>();
            var l5 = _level5Abilities ?? System.Array.Empty<AbilitySO>();

            int count = l1.Length;
            if (level >= 3) count += l3.Length;
            if (level >= 5) count += l5.Length;

            var abilities = new AbilitySO[count];
            int index = 0;

            foreach (var a in l1) abilities[index++] = a;
            if (level >= 3) foreach (var a in l3) abilities[index++] = a;
            if (level >= 5) foreach (var a in l5) abilities[index++] = a;

            return abilities;
        }
    }
}
