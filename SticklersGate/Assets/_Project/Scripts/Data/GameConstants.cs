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

        /// <summary>
        /// XP thresholds for each level (index 0 = level 1).
        /// </summary>
        public static readonly int[] XP_THRESHOLDS = { 0, 300, 900, 2700, 6500 };
    }
}
