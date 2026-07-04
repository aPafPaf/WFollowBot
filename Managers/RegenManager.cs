using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using WFollowBot.Data;
using WFollowBot.PathFinder;
using WFollowBot.PathFinding;

namespace WFollowBot.Managers
{
    public class RegenManager
    {
        private PlayerInfo PlayerInfo;

        public RegenManager(PlayerInfo playerInfo)
        {
            PlayerInfo = playerInfo;
        }

        public PathFinderResult RegenPathNew(Point endPoint, int radius = 15)
        {
            if (endPoint == default)
            {
                return new PathFinderResult();
            }

            var grid = TerrainInfo.ProcessedTerrainData;
            if (grid.Length == 0)
            {
                return new PathFinderResult();
            }

            if (!IsValidPoint(grid, endPoint, 1))
            {
                endPoint = FindClosestWalkable(grid, endPoint, radius);
                if (!IsValidPoint(grid, endPoint, 1))
                    return new PathFinderResult();
            }

            if (IsValidDirectly(grid, PlayerInfo.PlayerGridPosition, endPoint, 2))
            {
                return new() { Path = DrawLine(PlayerInfo.PlayerGridPosition, endPoint) };
            }

            var findPathResult = Pathfinder.FindPath(grid, PlayerInfo.PlayerGridPosition, endPoint);

            return findPathResult;
        }
        public static bool IsValidPoint(int[][] grid, Point point, int gridValue = 0)
        {
            if (grid.Length == 0)
                return false;

            int rows = grid.Length;
            int cols = grid[0].Length;

            return point.X >= 0 && point.X < cols && point.Y >= 0 && point.Y < rows && grid[point.Y][point.X] > gridValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidDirectly(int[][] grid, Point p0, Point p1, int minGridValue = 1)
        {
            if (grid.Length == 0)
                return false;

            int x = p0.X, y = p0.Y;
            int x1 = p1.X, y1 = p1.Y;

            int dx = Math.Abs(x1 - x);
            int dy = Math.Abs(y1 - y);
            int sx = x < x1 ? 1 : -1;
            int sy = y < y1 ? 1 : -1;
            int err = dx - dy;

            int height = grid.Length;
            int width = grid[0].Length;

            while (true)
            {
                // Проверка границ и значения
                if ((uint)y >= (uint)height || (uint)x >= (uint)width || grid[y][x] < minGridValue)
                    return false;

                // Достигли конечной точки
                if (x == x1 && y == y1)
                    break;

                int e2 = err << 1;

                // Используем if/else если оба условия истинны (диагональный шаг),
                // чтобы не пропускать промежуточные клетки — каждая итерация
                // делает ровно один шаг и проверяет ровно одну клетку.
                if (e2 > -dy && e2 < dx)
                {
                    // Диагональный шаг: выбираем направление по большей оси
                    if (dx >= dy)
                    {
                        err -= dy;
                        x += sx;
                    }
                    else
                    {
                        err += dx;
                        y += sy;
                    }
                }
                else if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                else
                {
                    err += dx;
                    y += sy;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<Point> DrawLine(Point start, Point end)
        {
            int x = start.X, y = start.Y;
            int x1 = end.X, y1 = end.Y;

            int dx = Math.Abs(x1 - x);
            int dy = Math.Abs(y1 - y);
            int sx = x < x1 ? 1 : -1;
            int sy = y < y1 ? 1 : -1;
            int err = dx - dy;

            int capacity = Math.Max(dx, dy) + 1;
            List<Point> points = new List<Point>(capacity);

            while (true)
            {
                points.Add(new Point(x, y));

                if (x == x1 && y == y1)
                    break;

                int e2 = err << 1;

                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return points;
        }

        public static Point FindClosestWalkable(int[][] grid, Point target, int searchRadius = 15)
        {
            int rows = grid.Length;
            int cols = grid[0].Length;

            for (int r = 1; r <= searchRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;

                        int nx = target.X + dx;
                        int ny = target.Y + dy;

                        if ((uint)ny >= (uint)rows || (uint)nx >= (uint)cols) continue;
                        if (grid[ny][nx] > 0)
                            return new Point(nx, ny);
                    }
                }
            }
            return target;
        }
    }
}