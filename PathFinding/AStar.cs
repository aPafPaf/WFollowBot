using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace WFollowBot.PathFinding;

public class AStar : IPathfinderAlgorithm
{
    // ThreadStatic - каждый поток получает свою копию
    [ThreadStatic]
    private static Dictionary<Point, Point>? _cameFromPool;

    [ThreadStatic]
    private static HashSet<Point>? _visitedPool;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public PathFinderResult FindPath(int[][] grid, Point start, Point end)
    {
        var openSet = new PriorityQueue<Point, int>();

        // Инициализируем пулы для текущего потока, если их ещё нет
        _cameFromPool ??= new Dictionary<Point, Point>(512);
        _visitedPool ??= new HashSet<Point>(512);

        _cameFromPool.Clear();
        _visitedPool.Clear();

        var cameFrom = _cameFromPool;
        var visited = _visitedPool;

        long timeoutTicks = Stopwatch.GetTimestamp() + (Stopwatch.Frequency * 3);

        openSet.Enqueue(start, EuclideanDistanceSquared(start, end));
        visited.Add(start);

        int width = grid[0].Length;
        int height = grid.Length;
        int endX = end.X, endY = end.Y;

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            int cx = current.X;
            int cy = current.Y;

            // Early exit
            if (cx == endX && cy == endY)
                return new PathFinderResult() { Path = ReconstructPathFast(cameFrom, current) };

            // Inline GetNeighbors для максимальной скорости
            if ((uint)(cx - 1) < (uint)width && (uint)(grid[cy][cx - 1] - 1) <= 5)
            {
                Point n = new Point(cx - 1, cy);
                if (visited.Add(n))
                {
                    cameFrom[n] = current;
                    openSet.Enqueue(n, EuclideanDistanceSquared(n, end));
                }
            }

            if ((uint)(cx + 1) < (uint)width && (uint)(grid[cy][cx + 1] - 1) <= 5)
            {
                Point n = new Point(cx + 1, cy);
                if (visited.Add(n))
                {
                    cameFrom[n] = current;
                    openSet.Enqueue(n, EuclideanDistanceSquared(n, end));
                }
            }

            if ((uint)(cy - 1) < (uint)height && (uint)(grid[cy - 1][cx] - 1) <= 5)
            {
                Point n = new Point(cx, cy - 1);
                if (visited.Add(n))
                {
                    cameFrom[n] = current;
                    openSet.Enqueue(n, EuclideanDistanceSquared(n, end));
                }
            }

            if ((uint)(cy + 1) < (uint)height && (uint)(grid[cy + 1][cx] - 1) <= 5)
            {
                Point n = new Point(cx, cy + 1);
                if (visited.Add(n))
                {
                    cameFrom[n] = current;
                    openSet.Enqueue(n, EuclideanDistanceSquared(n, end));
                }
            }

            // Проверка таймаута раз в N итераций (не каждый раз!)
            if ((visited.Count & 0x3F) == 0 && Stopwatch.GetTimestamp() > timeoutTicks)
                return new PathFinderResult(isTimeout: true);
        }

        return new PathFinderResult();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public List<Point> FindPathList(int[][] grid, Point start, Point end)
    {
        var openSet = new PriorityQueue<Point, int>();

        // Инициализируем пулы для текущего потока, если их ещё нет
        _cameFromPool ??= new Dictionary<Point, Point>(512);
        _visitedPool ??= new HashSet<Point>(512);

        _cameFromPool.Clear();
        _visitedPool.Clear();

        var cameFrom = _cameFromPool;
        var visited = _visitedPool;

        long timeoutTicks = Stopwatch.GetTimestamp() + (Stopwatch.Frequency * 3);

        openSet.Enqueue(start, EuclideanDistanceSquared(start, end));
        visited.Add(start);

        int width = grid[0].Length;
        int height = grid.Length;
        int endX = end.X, endY = end.Y;

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            int cx = current.X;
            int cy = current.Y;

            if (cx == endX && cy == endY)
                return ReconstructPathFast(cameFrom, current);

            if ((uint)(cx - 1) < (uint)width && (uint)(grid[cy][cx - 1] - 1) <= 5)
            {
                Point n = new Point(cx - 1, cy);
                if (visited.Add(n))
                {
                    cameFrom[n] = current;
                    openSet.Enqueue(n, EuclideanDistanceSquared(n, end));
                }
            }

            if ((uint)(cx + 1) < (uint)width && (uint)(grid[cy][cx + 1] - 1) <= 5)
            {
                Point n = new Point(cx + 1, cy);
                if (visited.Add(n))
                {
                    cameFrom[n] = current;
                    openSet.Enqueue(n, EuclideanDistanceSquared(n, end));
                }
            }

            if ((uint)(cy - 1) < (uint)height && (uint)(grid[cy - 1][cx] - 1) <= 5)
            {
                Point n = new Point(cx, cy - 1);
                if (visited.Add(n))
                {
                    cameFrom[n] = current;
                    openSet.Enqueue(n, EuclideanDistanceSquared(n, end));
                }
            }

            if ((uint)(cy + 1) < (uint)height && (uint)(grid[cy + 1][cx] - 1) <= 5)
            {
                Point n = new Point(cx, cy + 1);
                if (visited.Add(n))
                {
                    cameFrom[n] = current;
                    openSet.Enqueue(n, EuclideanDistanceSquared(n, end));
                }
            }

            if ((visited.Count & 0x3F) == 0 && Stopwatch.GetTimestamp() > timeoutTicks)
                return [];
        }

        return [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static List<Point> ReconstructPathFast(Dictionary<Point, Point> cameFrom, Point current)
    {
        var path = new List<Point>(64);

        // Добавляем в конец, потом реверсим (быстрее чем Insert(0))
        path.Add(current);
        while (cameFrom.TryGetValue(current, out Point prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EuclideanDistanceSquared(Point a, Point b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;

        // Возвращаем квадрат расстояния (избегаем sqrt)
        return dx * dx + dy * dy;
    }
}