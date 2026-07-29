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
        public const int BACK_ROW_AC_BONUS = 2;
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

        public const int ELEVATED_AC_BONUS = 1;
        public const int ELEVATED_RANGE_BONUS = 1;
        public const int COVER_AC_BONUS = 2;

        /// <summary>
        /// XP thresholds for each level (index 0 = level 1).
        /// </summary>
        public static readonly int[] XP_THRESHOLDS = { 0, 300, 900, 2700, 6500 };
    }
}
