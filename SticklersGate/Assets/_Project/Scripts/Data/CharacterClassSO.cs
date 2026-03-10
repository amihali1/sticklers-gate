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
            int count = _level1Abilities.Length;
            if (level >= 3) count += _level3Abilities.Length;
            if (level >= 5) count += _level5Abilities.Length;

            var abilities = new AbilitySO[count];
            int index = 0;

            foreach (var a in _level1Abilities) abilities[index++] = a;
            if (level >= 3) foreach (var a in _level3Abilities) abilities[index++] = a;
            if (level >= 5) foreach (var a in _level5Abilities) abilities[index++] = a;

            return abilities;
        }
    }
}
