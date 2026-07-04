using System.Collections.Generic;
using System.Drawing;
using WFollowBot.PathFinding;

namespace WFollowBot.PathFinder
{
    /// <summary>
    /// Wrapper class for pathfinding using the AStar algorithm.
    /// </summary>
    public class Pathfinder
    {
        private static IPathfinderAlgorithm _algorithm = new AStar();

        /// <summary>
        /// Sets a specific pathfinding algorithm implementation.
        /// </summary>
        /// <param name="algorithm">Implementation of the IPathfinderAlgorithm interface (e.g., AStar).</param>
        public static void SetAlgorithm(IPathfinderAlgorithm algorithm)
        {
            _algorithm = algorithm ?? new AStar();
        }

        /// <summary>
        /// Finds a path and returns the search result.
        /// </summary>
        public static PathFinderResult FindPath(
            int[][] grid,
            Point start,
            Point end)
        {
            return _algorithm.FindPath(grid, start, end);
        }

        /// <summary>
        /// Finds a path and returns a list of points.
        /// </summary>
        public static List<Point> FindPathList(
            int[][] grid,
            Point start,
            Point end)
        {
            return _algorithm.FindPathList(grid, start, end);
        }
    }
}
