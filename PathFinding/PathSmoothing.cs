using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using WFollowBot.Managers;

namespace WFollowBot.PathFinding;

public static class PathSmoothing
{
    public static List<Point> OptimizePathFromEnd(int[][] grid, List<Point> path)
    {
        if (path == null || path.Count < 2)
            return path;

        List<Point> optimizedPath = new List<Point>(path.Count / 2 + 1) { path[0] };
        int currentIndex = 0;
        int pathCount = path.Count;

        // Используем Span для избежания bounds checking
        var pathSpan = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(path);

        while (currentIndex < pathCount - 1)
        {
            // Экспоненциальный поиск для быстрого нахождения диапазона
            int step = 1;
            int candidate = currentIndex + step;
            int lastValid = currentIndex;

            // Фаза экспоненциального роста
            while (candidate < pathCount && IsValidDirectlyUnsafe(grid, pathSpan[currentIndex], pathSpan[candidate]))
            {
                lastValid = candidate;
                step <<= 1; // умножаем на 2
                candidate = currentIndex + step;
            }

            // Бинарный поиск в найденном диапазоне
            int left = lastValid + 1;
            int right = Math.Min(candidate, pathCount - 1);

            while (left <= right)
            {
                int mid = left + ((right - left) >> 1);
                if (IsValidDirectlyUnsafe(grid, pathSpan[currentIndex], pathSpan[mid]))
                {
                    lastValid = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            if (lastValid == currentIndex)
            {
                currentIndex++;
                if (currentIndex < pathCount)
                    optimizedPath.Add(pathSpan[currentIndex]);
            }
            else
            {
                // Добавляем промежуточные точки без создания списка
                if (lastValid > currentIndex + 1)
                {
                    AddLinePoints(pathSpan[currentIndex], pathSpan[lastValid], optimizedPath);
                }
                else
                {
                    optimizedPath.Add(pathSpan[lastValid]);
                }

                currentIndex = lastValid;
            }
        }

        return optimizedPath;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddLinePoints(Point start, Point end, List<Point> output)
    {
        int x = start.X, y = start.Y;
        int dx = end.X - start.X;
        int dy = end.Y - start.Y;
        int adx = Math.Abs(dx);
        int ady = Math.Abs(dy);
        int sx = (dx >> 31) | 1; // branchless: dx < 0 ? -1 : 1
        int sy = (dy >> 31) | 1;
        int err = adx - ady;

        while (true)
        {
            int e2 = err << 1;

            if (e2 > -ady)
            {
                err -= ady;
                x += sx;
            }
            if (e2 < adx)
            {
                err += adx;
                y += sy;
            }

            if (x == end.X && y == end.Y)
            {
                output.Add(end);
                break;
            }

            output.Add(new Point(x, y));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool IsValidDirectlyUnsafe(int[][] grid, Point p0, Point p1, int minGridValue = 1)
    {
        int x = p0.X, y = p0.Y;
        int dx = p1.X - p0.X;
        int dy = p1.Y - p0.Y;
        int adx = Math.Abs(dx);
        int ady = Math.Abs(dy);
        int sx = (dx >> 31) | 1; // branchless sign
        int sy = (dy >> 31) | 1;
        int err = adx - ady;

        int height = grid.Length;
        int width = grid[0].Length;

        while (true)
        {
            // Bounds check с unsigned comparison для обеих координат
            if ((uint)y >= (uint)height || (uint)x >= (uint)width)
                return false;

            // Прямой доступ к элементу массива
            int value = grid[y][x];
            if (value < minGridValue)
                return false;

            if (x == p1.X && y == p1.Y)
                return true;

            int e2 = err << 1;

            if (e2 > -ady)
            {
                err -= ady;
                x += sx;
            }
            if (e2 < adx)
            {
                err += adx;
                y += sy;
            }
        }
    }

    public static List<Point> SmoothPath(int[][] grid, List<Point> path)
    {
        if (path == null || path.Count < 3)
            return path ?? new List<Point>();

        var result = new List<Point>(path.Count);
        result.Add(path[0]);

        int currentIndex = 0;
        while (currentIndex < path.Count - 1)
        {
            int farthest = currentIndex + 1;
            for (int i = path.Count - 1; i > currentIndex; i--)
            {
                if (RegenManager.IsValidDirectly(grid, path[currentIndex], path[i], 1))
                {
                    farthest = i;
                    break;
                }
            }

            result.Add(path[farthest]);
            currentIndex = farthest;
        }

        return result;
    }
}