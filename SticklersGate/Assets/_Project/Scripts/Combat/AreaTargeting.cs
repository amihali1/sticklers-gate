using System.Collections.Generic;
using SticklersGate.Data;

namespace SticklersGate.Combat
{
    /// <summary>
    /// Grid area-of-effect rules for D&amp;D-style templates. A Sphere is centered
    /// on a chosen tile within the ability's range and can include the caster.
    /// An Emanation surrounds the caster: harmful ones (Thunderwave) exclude the
    /// caster, ally-only auras (Aura of Protection) include them.
    /// Areas use Manhattan distance, like movement and range.
    /// </summary>
    public static class AreaTargeting
    {
        /// <summary>
        /// The tile an area is centered on: the chosen point for a Sphere, the
        /// caster's own tile for an Emanation.
        /// </summary>
        public static GridPosition GetCenter(AbilitySO ability, CombatUnit caster, GridPosition chosenPoint)
        {
            return ability.AreaShape == AreaShape.Emanation ? caster.Position : chosenPoint;
        }

        /// <summary>
        /// True if a Sphere may be centered on <paramref name="point"/>: inside the
        /// grid and within the ability's range of the caster. Emanations are always
        /// valid (they are centered on the caster).
        /// </summary>
        public static bool IsValidCenter(CombatGrid grid, AbilitySO ability, CombatUnit caster, GridPosition point)
        {
            if (ability.AreaShape == AreaShape.Emanation) return true;
            if (grid != null && !grid.IsInBounds(point)) return false;
            return caster.Position.ManhattanDistanceTo(point) <= ability.Range;
        }

        /// <summary>
        /// Every in-bounds tile covered by the area. Includes the center for a
        /// Sphere or an ally-only aura; excludes the caster's tile otherwise.
        /// </summary>
        public static List<GridPosition> GetAreaTiles(CombatGrid grid, AbilitySO ability, CombatUnit caster, GridPosition chosenPoint)
        {
            GridPosition center = GetCenter(ability, caster, chosenPoint);
            var tiles = grid.GetPositionsWithinDistance(center, ability.AreaRadius);
            if ((ability.AreaShape == AreaShape.Sphere || IncludesCaster(ability)) && grid.IsInBounds(center))
            {
                tiles.Add(center);
            }
            return tiles;
        }

        /// <summary>
        /// Units inside the area that the ability affects, filtered by
        /// <see cref="AbilitySO.AreaAffects"/> relative to the caster. Damaging
        /// and status abilities skip downed units; healing reaches downed allies.
        /// </summary>
        public static List<CombatUnit> GetAffectedUnits(AbilitySO ability, CombatUnit caster,
            GridPosition chosenPoint, IEnumerable<CombatUnit> allUnits)
        {
            GridPosition center = GetCenter(ability, caster, chosenPoint);
            var result = new List<CombatUnit>();

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive) continue;
                if (!unit.IsConscious && !ability.Heals) continue;
                if (ability.AreaShape == AreaShape.Emanation && unit == caster && !IncludesCaster(ability)) continue;
                if (unit.Position.ManhattanDistanceTo(center) > ability.AreaRadius) continue;
                if (!Affects(ability.AreaAffects, caster, unit)) continue;

                result.Add(unit);
            }

            return result;
        }

        /// <summary>Ally-only emanations (auras) cover the caster too, as in 5e.</summary>
        private static bool IncludesCaster(AbilitySO ability)
        {
            return ability.AreaShape == AreaShape.Emanation && ability.AreaAffects == AreaAffects.AlliesOnly;
        }

        private static bool Affects(AreaAffects affects, CombatUnit caster, CombatUnit unit)
        {
            bool isAlly = unit.IsPlayer == caster.IsPlayer;
            switch (affects)
            {
                case AreaAffects.AlliesOnly: return isAlly;
                case AreaAffects.EnemiesOnly: return !isAlly;
                default: return true;
            }
        }
    }
}
