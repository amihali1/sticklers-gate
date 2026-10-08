namespace SticklersGate.Data
{
    /// <summary>
    /// Global game constants. No magic numbers in code.
    /// </summary>
    public static class GameConstants
    {
        public const int BASE_AC = 10;
        public const int CRIT_THRESHOLD = 20;
        public const int CRIT_FAIL_THRESHOLD = 1;
        public const int MAX_PARTY_SIZE = 4;
        public const int MAX_LEVEL = 5;
        public const int DEATH_SAVE_SUCCESS_NEEDED = 3;
        public const int DEATH_SAVE_FAIL_NEEDED = 3;
        public const int DEFEND_AC_BONUS = 2;
        public const int FLEE_DC = 12;

        // --- Grid system (Phase 9) ---
        public const int DEFAULT_GRID_WIDTH = 8;
        public const int DEFAULT_GRID_HEIGHT = 6;
        public const int MAX_GRID_WIDTH = 12;
        public const int MAX_GRID_HEIGHT = 8;

        /// <summary>Tiles a melee ability reaches (orthogonally adjacent).</summary>
        public const int DEFAULT_MELEE_RANGE = 1;

        /// <summary>Tiles a standard ranged ability reaches.</summary>
        public const int DEFAULT_RANGED_RANGE = 4;

        /// <summary>Tiles a melee-focused unit can move per turn.</summary>
        public const int DEFAULT_MELEE_MOVEMENT = 3;

        /// <summary>Tiles a ranged/caster unit can move per turn.</summary>
        public const int DEFAULT_RANGED_MOVEMENT = 2;

        /// <summary>Movement cost of entering a Normal tile.</summary>
        public const int NORMAL_MOVEMENT_COST = 1;

        /// <summary>Movement cost of entering Difficult or Water terrain.</summary>
        public const int DIFFICULT_MOVEMENT_COST = 2;

        // --- Status effect mechanics ---

        /// <summary>AC bonus while Shielded (Cunning Dodge, Patient Defense, Aura of Protection).</summary>
        public const int SHIELDED_AC_BONUS = 4;

        /// <summary>Die added to attack rolls and saving throws while Blessed (5e Bless / Bardic Inspiration).</summary>
        public const int BLESSED_BONUS_DIE = 4;

        /// <summary>Tiles of movement lost while Slowed (5e Ray of Frost: -10 ft).</summary>
        public const int SLOWED_MOVEMENT_PENALTY = 2;

        /// <summary>Bonus damage on melee attacks while Raging.</summary>
        public const int RAGE_DAMAGE_BONUS = 2;

        /// <summary>Die of bonus damage the marker deals to a Marked target (5e Hunter's Mark / Hex: 1d6).</summary>
        public const int MARKED_BONUS_DIE = 6;

        public const int ELEVATED_AC_BONUS = 1;
        public const int ELEVATED_RANGE_BONUS = 1;
        public const int COVER_AC_BONUS = 2;

        /// <summary>
        /// XP thresholds for each level (index 0 = level 1).
        /// </summary>
        public static readonly int[] XP_THRESHOLDS = { 0, 300, 900, 2700, 6500 };
    }
}
