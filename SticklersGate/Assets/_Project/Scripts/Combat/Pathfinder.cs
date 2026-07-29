using System.Collections.Generic;
using SticklersGate.Data;

namespace SticklersGate.Combat
{
    /// <summary>
    /// Grid pathfinding for tactical movement. Uses uniform-cost search
    /// (Dijkstra) because terrain gives tiles a variable entry cost. Movement is
    /// 4-directional (orthogonal, no diagonals). Occupied tiles and impassable
    /// terrain block movement; the mover's own starting tile is always a valid
    /// origin even though it is occupied by the mover.
    /// </summary>
    public static class Pathfinder
    {
        /// <summary>
        /// All tiles reachable from <paramref name="start"/> within
        /// <paramref name="movementBudget"/> total movement cost. Excludes the
        /// start tile. Only empty, passable tiles are returned.
        /// </summary>
        public static List<GridPosition> GetReachablePositions(
            CombatGrid grid, GridPosition start, int movementBudget)
        {
            var costs = Dijkstra(grid, start, movementBudget, null, out _);
            var result = new List<GridPosition>();
            foreach (var pair in costs)
            {
                if (pair.Key != start && pair.Value <= movementBudget)
                    result.Add(pair.Key);
            }
            return result;
        }

        /// <summary>
        /// Shortest path from <paramref name="start"/> to <paramref name="goal"/>
        /// as an ordered list including both endpoints, or null if the goal is
        /// unreachable. If a movement budget is given, returns null when the
        /// cheapest path exceeds it.
        /// </summary>
        public static List<GridPosition> FindPath(
            CombatGrid grid, GridPosition start, GridPosition goal, int movementBudget = int.MaxValue)
        {
            if (start == goal)
                return new List<GridPosition> { start };
            if (!grid.IsWalkable(goal))
                return null;

            var costs = Dijkstra(grid, start, movementBudget, goal, out var cameFrom);
            if (!costs.TryGetValue(goal, out var goalCost) || goalCost > movementBudget)
                return null;

            var path = new List<GridPosition>();
            var current = goal;
            path.Add(current);
            while (current != start)
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Core uniform-cost search. Returns the cheapest cost to reach each
        /// settled tile and the predecessor map for path reconstruction. If
        /// <paramref name="target"/> is set, the search stops once the target is
        /// settled. Tiles cost their entry cost; the start costs 0.
        /// </summary>
        private static Dictionary<GridPosition, int> Dijkstra(
            CombatGrid grid, GridPosition start, int movementBudget,
            GridPosition? target, out Dictionary<GridPosition, GridPosition> cameFrom)
        {
            var bestCost = new Dictionary<GridPosition, int> { [start] = 0 };
            cameFrom = new Dictionary<GridPosition, GridPosition>();
            var settled = new HashSet<GridPosition>();
            // Frontier of discovered-but-not-settled tiles; small grids make a
            // linear min-scan cheaper than a heap's overhead.
            var frontier = new List<GridPosition> { start };

            while (frontier.Count > 0)
            {
                int minIndex = 0;
                for (int i = 1; i < frontier.Count; i++)
                {
                    if (bestCost[frontier[i]] < bestCost[frontier[minIndex]])
                        minIndex = i;
                }

                var current = frontier[minIndex];
                frontier.RemoveAt(minIndex);
                if (!settled.Add(current))
                    continue;

                if (target.HasValue && current == target.Value)
                    break;

                int currentCost = bestCost[current];
                foreach (var neighbor in current.OrthogonalNeighbors())
                {
                    if (settled.Contains(neighbor) || !grid.IsWalkable(neighbor))
                        continue;

                    int stepCost = grid.GetMovementCost(neighbor);
                    int newCost = currentCost + stepCost;
                    if (newCost > movementBudget)
                        continue;

                    if (!bestCost.TryGetValue(neighbor, out var known) || newCost < known)
                    {
                        bestCost[neighbor] = newCost;
                        cameFrom[neighbor] = current;
                        frontier.Add(neighbor);
                    }
                }
            }

            return bestCost;
        }
    }
}
