using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace WFollowBot.PathFinding
{
    public interface IPathfinderAlgorithm
    {
        /// <summary>
        /// Finds a path from start to end on the given grid and returns a PathFinderResult.
        /// </summary>
        PathFinderResult FindPath(
            int[][] grid,
            Point start,
            Point end
        );

        /// <summary>
        /// Finds a path from start to end on the given grid and returns a list of points.
        /// </summary>
        List<Point> FindPathList(
            int[][] grid,
            Point start,
            Point end
        );
    }
}
