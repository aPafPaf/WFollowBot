using System;
using System.Collections.Generic;
using System.Drawing;
using WFollowBot.PathFinder;

namespace WFollowBot;

public static class PathFinderHeuristic
{
    //Используется для ортогональных сеток (когда движение возможно только по горизонтали или вертикали). Рассчитывается как сумма абсолютных разностей по осям.
    public static float ManhattanDistance(Point a, Point b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    //Эвклидова эвристика лучше для более свободного перемещения, но медленнее.
    public static float EuclideanDistance(Point a, Point b)
    {
        return MathF.Sqrt(MathF.Pow(a.X - b.X, 2) + MathF.Pow(a.Y - b.Y, 2));
    }

    //Чебышевская эвристика эффективна для карт с диагональными движениями.
    public static float ChebyshevDistance(Point a, Point b)
    {
        float dx = Math.Abs(a.X - b.X);
        float dy = Math.Abs(a.Y - b.Y);
        return MathF.Max(dx, dy) + 0.5f * MathF.Min(dx, dy);
    }

    //Манхэттенская и диагональная эвристики просты и быстры, идеально подходят для игр с фиксированными сетками.
    public static float DiagonalDistance(Point a, Point b)
    {
        float dx = Math.Abs(a.X - b.X);
        float dy = Math.Abs(a.Y - b.Y);
        return Math.Max(dx, dy);
    }

    //Взвешенная и препятственно-осведомлённая эвристики дают возможность настраивать поведение алгоритма под конкретные нужды, например, для оптимизации скорости или учета сложных ландшафтов.
    public static float ObstacleAwareHeuristic(Point a, Point b, int obstacleCount)
    {
        float distance = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        return distance + obstacleCount * 10; // Чем больше препятствий, тем выше стоимость
    }

    //Функция для вычисления стоимости между текущей точкой и целью. Основанная на длине найденого пути.
    public static double FindPathCost(Point currentPoint, Point targetPoint, int[][] grid)
    {
        var path = Pathfinder.FindPath(grid, currentPoint, targetPoint);

        return path != null && path.Path.Count > 0 ? path.Path.Count : int.MaxValue;
    }

    public static int FastLimitedPathCost(int[][] grid, Point start, Point end, int maxDepth = 50)
    {
        int rows = grid.Length;
        int cols = grid[0].Length;

        if(start == end) return 0;

        var visited = new bool[rows, cols];
        var queue = new Queue<(Point point, int depth)>();

        queue.Enqueue((start, 0));
        visited[start.Y, start.X] = true;

        while(queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();

            if(depth > maxDepth)
                break;

            if(current == end)
                return depth;

            var neighbors = new List<Point>
            {
                new Point(current.X - 1, current.Y),
                new Point(current.X + 1, current.Y),
                new Point(current.X, current.Y - 1),
                new Point(current.X, current.Y + 1)
            };

            foreach(var n in neighbors)
            {
                if(n.X < 0 || n.X >= cols || n.Y < 0 || n.Y >= rows)
                    continue;

                if(visited[n.Y, n.X])
                    continue;

                if(grid[n.Y][n.X] == 0)
                    continue;

                visited[n.Y, n.X] = true;
                queue.Enqueue((n, depth + 1));
            }
        }

        return int.MaxValue;
    }

}
