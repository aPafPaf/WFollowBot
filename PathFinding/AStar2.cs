using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using WFollowBot.PathFinding;

namespace WFollowBot.PathFinder;

public class AStar2 : IPathfinderAlgorithm
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public PathFinderResult FindPath(int[][] grid, Point start, Point end)
    {
        var openSet = new PriorityQueue<Point, float>();
        var cameFrom = new Dictionary<Point, Point>();
        var fScore = new Dictionary<Point, float>();

        Stopwatch findingTime = new();
        findingTime.Restart();

        openSet.Enqueue(start, 0);
        fScore[start] = PathFinderHeuristic.DiagonalDistance(start, end);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current.Equals(end))
            {
                return new PathFinderResult() { Path = ReconstructPath(cameFrom, current) };
            }

            Point[] nArray = GetNeighbors44(grid, current, out int arrayCount);
            for (int i = 0; i < arrayCount; i++)
            {
                if (findingTime.Elapsed.Seconds > 3)
                {
                    findingTime.Stop();
                    return new PathFinderResult(true);
                }

                if (!fScore.TryGetValue(nArray[i], out float score) || PathFinderHeuristic.DiagonalDistance(nArray[i], end) < fScore[nArray[i]])
                {
                    cameFrom[nArray[i]] = current;
                    fScore[nArray[i]] = PathFinderHeuristic.DiagonalDistance(nArray[i], end);
                    openSet.Enqueue(nArray[i], fScore[nArray[i]]);
                }
            }
        }

        return new PathFinderResult();
    }

    public List<Point> FindPathList(int[][] grid, Point start, Point end)
    {
        var openSet = new PriorityQueue<Point, float>();
        var cameFrom = new Dictionary<Point, Point>();
        var fScore = new Dictionary<Point, float>();

        Stopwatch findingTime = new();
        findingTime.Restart();

        openSet.Enqueue(start, 0);
        fScore[start] = PathFinderHeuristic.DiagonalDistance(start, end);

        while (openSet.Count > 0)
        {

            var current = openSet.Dequeue();

            if (current.Equals(end))
                return ReconstructPath(cameFrom, current);

            Point[] nArray = GetNeighbors44(grid, current, out int arrayCount);
            for (int i = 0; i < arrayCount; i++)
            {
                if (findingTime.Elapsed.Seconds > 3) return [];

                if (!fScore.ContainsKey(nArray[i]) || PathFinderHeuristic.DiagonalDistance(nArray[i], end) < fScore[nArray[i]])
                {
                    cameFrom[nArray[i]] = current;
                    fScore[nArray[i]] = PathFinderHeuristic.DiagonalDistance(nArray[i], end);
                    openSet.Enqueue(nArray[i], fScore[nArray[i]]);
                }
            }
        }

        return [];
    }

    private List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
    {
        var path = new List<Point> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Point[] GetNeighbors44(int[][] grid, Point current, out int count)
    {
        // максимум 4 соседа
        Point[] neighbors = new Point[4];
        count = 0;

        int x = current.X;
        int y = current.Y;

        if (x > 0 && grid[y][x - 1] is >= 1 and <= 6)
            neighbors[count++] = new Point(x - 1, y);
        if (x < grid[0].Length - 1 && grid[y][x + 1] is >= 1 and <= 6)
            neighbors[count++] = new Point(x + 1, y);
        if (y > 0 && grid[y - 1][x] is >= 1 and <= 6)
            neighbors[count++] = new Point(x, y - 1);
        if (y < grid.Length - 1 && grid[y + 1][x] is >= 1 and <= 6)
            neighbors[count++] = new Point(x, y + 1);

        return neighbors;
    }
}