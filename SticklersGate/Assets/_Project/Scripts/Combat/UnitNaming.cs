using System.Collections.Generic;

namespace SticklersGate.Combat
{
    /// <summary>
    /// Display-name rules for spawned units.
    /// </summary>
    public static class UnitNaming
    {
        /// <summary>
        /// Appends A, B, C… to names that appear more than once so duplicates can
        /// be told apart ("Stick Goblin A", "Stick Goblin B"). Unique names are
        /// left alone. Order is preserved.
        /// </summary>
        public static List<string> DisambiguateNames(IList<string> baseNames)
        {
            var totals = new Dictionary<string, int>();
            foreach (var name in baseNames)
            {
                totals[name] = totals.TryGetValue(name, out int n) ? n + 1 : 1;
            }

            var seen = new Dictionary<string, int>();
            var result = new List<string>(baseNames.Count);
            foreach (var name in baseNames)
            {
                if (totals[name] == 1)
                {
                    result.Add(name);
                    continue;
                }

                int index = seen.TryGetValue(name, out int i) ? i : 0;
                seen[name] = index + 1;
                result.Add($"{name} {(char)('A' + index)}");
            }

            return result;
        }
    }
}
