using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
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

        //    private List<Point> RegenPathUIClose(Point endPoint, int radius, int gridValueMin = 2)
        //    {
        //        if(Settings.DebugSettings.AllowRegenMessage.Value)
        //            LogMessage($"RegenPath - {endPoint.X}:{endPoint.Y}");
        //        if(endPoint == new Point(0, 0)) return [];

        //        List<(Point point, int value)> points = [];

        //        points = FindClosestAccessiblePoints(TerrainInfo.ProcessedTerrainData, endPoint, radius);

        //        if(!points.Any()) return [];

        //        endPoint = points[random.Next(0, points.Count)].point;

        //        var findPathResult = Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, endPoint);

        //        Storages.Path.LastRegenPoint = endPoint;

        //        return findPathResult.Path;
        //    }

        //    public List<Point> RegenPathUI(Point endPoint, int gridValueMin = 2)
        //    {
        //        if(Settings.DebugSettings.AllowRegenMessage.Value)
        //            LogMessage($"RegenPath - {endPoint.X}:{endPoint.Y}");
        //        if(endPoint == new Point(0, 0)) return [];

        //        List<(Point point, int value)> points = [];

        //        points = FindClosestAccessiblePoints(TerrainInfo.ProcessedTerrainData, endPoint, 10);

        //        if(!points.Any()) return [];

        //        endPoint = points[random.Next(0, points.Count)].point;

        //        var findPathResult = Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, endPoint);

        //        Storages.Path.LastRegenPoint = endPoint;

        //        return findPathResult.Path;
        //    }

        //    private bool FullRegenPathhNextTilePosition(int radius = 15)
        //    {
        //        Storages.Path.CurrentPathClear();

        //        if(!Storages.Path.PathFinderInfo.TryDequeueMetaTilePosition(out Point endPoint))
        //        {
        //            LogError("Error DequeueMetaTilePosition!!!");
        //            return false;
        //        }

        //        if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, endPoint, 2))
        //        {
        //            endPoint = FindClosestAccessiblePoint(TerrainInfo.ProcessedTerrainData, endPoint, radius);
        //        }

        //        if(endPoint == default)
        //        {
        //            LogError("Error EndPoint is Zero!!!");
        //            return false;
        //        }

        //        Storages.Path.SetEndPoint(endPoint);

        //        var finderResult = Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, endPoint);

        //        Storages.Path.PathSegments.SetPathSegments(GetSegmentDividers(finderResult.Path));

        //        if(Storages.Path.PathSegments.TryDequeuePathSegment(out Point newPoint))
        //        {
        //            Storages.Path.SetCurrentTargetPoint(newPoint);

        //            var finderResultNewPoint = Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, newPoint);

        //            Storages.Path.AssignCurrentPath(finderResultNewPoint.Path);
        //        }

        //        return true;
        //    }

        //    private List<Point> FullRegenPath(Point endPoint, int radius = 15)
        //    {
        //        if(Settings.DebugSettings.AllowRegenMessage.Value)
        //            LogMessage($"FullRegenPath");

        //        Storages.Path.CurrentPathClear();

        //        Storages.Path.PathFinderInfo.TilePositionClear();

        //        List<Point> points = [];

        //        if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, endPoint, 2))
        //        {
        //            endPoint = FindClosestAccessiblePoint(TerrainInfo.ProcessedTerrainData, endPoint, radius);
        //        }

        //        if(endPoint == default)
        //        {
        //            return [];
        //        }

        //        Storages.Path.SetEndPoint(endPoint);

        //        var finderResult = Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, endPoint);

        //        Storages.Path.PathSegments.SetPathSegments(GetSegmentDividers(finderResult.Path));

        //        if(Storages.Path.PathSegments.TryDequeuePathSegment(out Point newPoint))
        //        {
        //            Storages.Path.SetCurrentTargetPoint(newPoint);

        //            var finderResultNewPoint = Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, newPoint);

        //            Storages.Path.AssignCurrentPath(finderResultNewPoint.Path);
        //        }

        //        return finderResult.Path;
        //    }

        //    private bool InitRegenPathGenerateCase()
        //    {
        //        List<Point> points;

        //        //if(Settings.WorkingSettings.FullClearMode.Value)
        //        //{
        //        //    points = GenerateKeyPoints(TerrainInfo.ProcessedTerrainData, Constants.RangeGeneratePoints);
        //        //}
        //        //else
        //        //{
        //        points = GenerateKeyPoints(TerrainInfo.ProcessedTerrainData, Constants.RangeGeneratePoints / 2);
        //        //}

        //        Storages.Path.SetEndPoint(FindFurthestPointByPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, points));

        //        if(!points.Any())
        //        {
        //            points = GenerateKeyPoints(TerrainInfo.ProcessedTerrainData, Constants.RangeGeneratePoints).ToList();

        //            if(!points.Any()) return GlobalVariables.PathFinderResultFalse.PathFound;

        //            if(Storages.Path.EndPoint == default) Storages.Path.SetEndPoint(points.Last());
        //        }

        //        Storages.Path.PathSegments.SetPathSegments(GetVisitOrderWithCache(TerrainInfo.ProcessedTerrainData, points, PlayerInfo.PlayerGridPosition, Storages.Path.EndPoint));

        //        while(!Storages.Path.PathSegments.isLastSegment)
        //        {
        //            if(Storages.Path.PathSegments.TryDequeuePathSegment(out Point newPoint))
        //            {
        //                if(newPoint == default) return GlobalVariables.PathFinderResultFalse.PathFound;

        //                if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, newPoint, 2))
        //                {
        //                    var findPoint = FindClosestAccessiblePoint(TerrainInfo.ProcessedTerrainData, newPoint, 15);

        //                    if(findPoint == default) continue;

        //                    newPoint = findPoint;
        //                }
        //                if(!Storages.MapPoint.Contains(newPoint))
        //                {
        //                    Storages.Path.SetCurrentTargetPoint(newPoint);
        //                    break;
        //                }
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        return Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, Storages.Path.CurrentTargetPoint).PathFound;
        //    }

        //    private void InitRegenPathEndPointZero()
        //    {
        //        if(Storages.Path.EndPoint == Point.Zero)
        //        {
        //            LogError("InitRegenPath: EndPoint is zero!!");

        //            string tileMeta = Storages.Path.PathFinderInfo.GetCurrentTileMeta();
        //            //if(tileMeta == default) return GlobalVariables.PathFinderResultFalse;
        //            if(tileMeta == default)
        //            {
        //                LogError("InitRegenPathEndPointZero: TileMeta is default!");
        //                return;
        //            }

        //            //Init tileMeta for pathfinding and currentTile
        //            if(TerrainInfo._allTargetLocations.TryGetValue(tileMeta, out List<Point> TilePoints))
        //            {
        //                foreach(Point tilePoint in TilePoints)
        //                {
        //                    if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, tilePoint))
        //                    {
        //                        var newPoint = FindClosestAccessiblePointRange(TerrainInfo.ProcessedTerrainData, tilePoint, 5, 15);

        //                        Storages.Path.SetEndPoint(tilePoint);
        //                    }
        //                    else
        //                    {
        //                        Storages.Path.SetEndPoint(tilePoint);

        //                        break;
        //                    }
        //                }
        //            }

        //            //return GlobalVariables.PathFinderResultFalse;
        //        }
        //    }

        //    public bool InitRegenPathRushMode()
        //    {
        //        var pathFinderResult = Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, Storages.Path.EndPoint);
        //        if(!pathFinderResult.PathFound) return GlobalVariables.PathFinderResultFalse.PathFound;

        //        pathFinderResult.Path = OptimizePathFromEnd(TerrainInfo.ProcessedTerrainData, pathFinderResult.Path);

        //        var segments = GetSegmentDividers(pathFinderResult.Path);

        //        Storages.Path.PathSegments.SetPathSegments(segments);

        //        while(!Storages.Path.PathSegments.isLastSegment)
        //        {
        //            if(Storages.Path.PathSegments.TryDequeuePathSegment(out Point newPoint))
        //            {
        //                if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, newPoint, 2))
        //                {
        //                    var findPoint = FindClosestAccessiblePoint(TerrainInfo.ProcessedTerrainData, newPoint, 15);

        //                    if(findPoint == default) continue;

        //                    newPoint = findPoint;
        //                }

        //                if(!Storages.MapPoint.Contains(newPoint) || Storages.Path.PathSegments.isLastSegment)
        //                {
        //                    Storages.Path.SetCurrentTargetPoint(newPoint);
        //                    break;
        //                }
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        return pathFinderResult.PathFound;
        //    }

        //    public PathFinderResult InitRegenPathFullClearMode()
        //    {
        //        var points = GenerateKeyPoints(TerrainInfo.ProcessedTerrainData, Constants.RangeGeneratePoints);
        //        Storages.Path.SetEndPoint(FindFurthestPointByPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, points));

        //        if(!points.Any())
        //        {
        //            points = GenerateKeyPoints(TerrainInfo.ProcessedTerrainData, Constants.RangeGeneratePoints).ToList();

        //            if(!points.Any()) return GlobalVariables.PathFinderResultFalse;

        //            if(Storages.Path.EndPoint == default) Storages.Path.SetEndPoint(points.Last());
        //        }

        //        Storages.Path.PathSegments.SetPathSegments(GetVisitOrderWithCache(TerrainInfo.ProcessedTerrainData, points, PlayerInfo.PlayerGridPosition, Storages.Path.EndPoint));

        //        while(!Storages.Path.PathSegments.isLastSegment)
        //        {
        //            if(Storages.Path.PathSegments.TryDequeuePathSegment(out Point newPoint))
        //            {
        //                if(newPoint == default) return GlobalVariables.PathFinderResultFalse;

        //                if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, newPoint, 2))
        //                {
        //                    var findPoint = FindClosestAccessiblePoint(TerrainInfo.ProcessedTerrainData, newPoint, 15);

        //                    if(findPoint == default) continue;

        //                    newPoint = findPoint;
        //                }
        //                if(!Storages.MapPoint.Contains(newPoint))
        //                {
        //                    Storages.Path.SetCurrentTargetPoint(newPoint);
        //                    break;
        //                }
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        return Pathfinder.FindPath(TerrainInfo.ProcessedTerrainData, PlayerInfo.PlayerGridPosition, Storages.Path.CurrentTargetPoint);
        //    }

        //    private void InitRegenPath()
        //    {
        //        if(Settings.DebugSettings.AllowRegenMessage.Value)
        //            LogMessage("InitRegenPath");

        //        switch(Storages.Path.PathFinderInfo.GetCurrentTileMeta())
        //        {
        //            case "Generate":
        //                {
        //                    InitRegenPathGenerateCase();
        //                    return;
        //                }
        //            case "Stay":
        //                return;
        //            case "":
        //                break;
        //        }


        //        //if(Settings.WorkingSettings.FullClearMode)
        //        //{
        //        //    InitRegenPathFullClearMode();
        //        //}
        //        //else
        //        //{
        //        InitRegenPathRushMode();
        //        //}

        //        Storages.Path.LastRegenPoint = Storages.Path.EndPoint;
        //    }

        //    public static ConcurrentQueue<Point> GetSegmentDividers(List<Point> fullPath)
        //    {
        //        int segmentLength = 300;

        //        var queue = new ConcurrentQueue<Point>();

        //        if(fullPath == null || fullPath.Count == 0 || segmentLength <= 0)
        //        {
        //            return queue;
        //        }

        //        int totalPoints = fullPath.Count;
        //        queue.Enqueue(fullPath[0]); // Добавляем начальную точку

        //        int currentIndex = 0;
        //        double accumulatedDistance = 0;

        //        for(int i = 1; i < totalPoints; i++)
        //        {
        //            accumulatedDistance += PathFinderHeuristic.EuclideanDistance(fullPath[currentIndex], fullPath[i]);

        //            // Если накопленное расстояние больше или равно длине сегмента, добавляем точку в очередь
        //            if(accumulatedDistance >= segmentLength)
        //            {
        //                queue.Enqueue(fullPath[i]);
        //                currentIndex = i; // Обновляем текущий индекс
        //                accumulatedDistance = 0; // Сбрасываем накопленное расстояние
        //            }
        //        }

        //        // Добавляем последнюю точку пути, если она не была добавлена
        //        if(queue.IsEmpty || queue.TryPeek(out var lastPoint) && lastPoint != fullPath[^1])
        //        {
        //            queue.Enqueue(fullPath[^1]);
        //        }

        //        return queue;
        //    }

        //    public static List<Point> GetAllPointsInRadius(int[][] grid, Point center, int radius)
        //    {
        //        int estimatedCount = (int)(Math.PI * radius * radius) + 4 * radius;
        //        var points = new List<Point>(estimatedCount);

        //        int rows = grid.Length;
        //        int cols = grid[0].Length;

        //        int radiusSquared = radius * radius;

        //        int minX = Math.Max(0, center.X - radius);
        //        int maxX = Math.Min(rows - 1, center.X + radius);
        //        int minY = Math.Max(0, center.Y - radius);
        //        int maxY = Math.Min(cols - 1, center.Y + radius);

        //        for(int i = minX; i <= maxX; i++)
        //        {
        //            int dx = i - center.X;
        //            int dxSquared = dx * dx;

        //            for(int j = minY; j <= maxY; j++)
        //            {
        //                int dy = j - center.Y;

        //                if(dxSquared + dy * dy <= radiusSquared)
        //                {
        //                    points.Add(new Point(i, j));
        //                }
        //            }
        //        }

        //        return points;
        //    }

        //    public static bool GridIsEmpty(int[][] grid, Point center, int radius)
        //    {
        //        int er = 0;

        //        for(int i = center.X - radius; i <= center.X + radius; i++)
        //        {
        //            for(int j = center.Y - radius; j <= center.Y + radius; j++)
        //            {
        //                try
        //                {
        //                    if(Math.Sqrt(Math.Pow(i - center.X, 2) + Math.Pow(j - center.Y, 2)) <= radius &&
        //                        grid[j][i] > 0)
        //                    {
        //                        return false;
        //                    }
        //                }
        //                catch(Exception ex)
        //                {
        //                    er++;

        //                    if(er > 50)
        //                        return false;

        //                    Debug.WriteLine(ex);
        //                    continue;
        //                }
        //            }
        //        }

        //        return true;
        //    }

        //    public static List<Point> GetValidPointsInRadius(int[][] grid, Point center, int radius)
        //    {
        //        try
        //        {
        //            int rows = grid.Length;
        //            int cols = grid[0].Length;
        //            int radiusSq = radius * radius; // Считаем квадрат радиуса один раз

        //            var pointsInRadius = new List<Point>(radius * radius); // Предустанавливаем ёмкость списка

        //            for(int j = Math.Max(0, center.Y - radius); j <= Math.Min(rows - 1, center.Y + radius); j++)
        //            {
        //                for(int i = Math.Max(0, center.X - radius); i <= Math.Min(cols - 1, center.X + radius); i++)
        //                {
        //                    int dx = i - center.X;
        //                    int dy = j - center.Y;

        //                    // Проверяем расстояние без использования Math.Sqrt()
        //                    if(dx * dx + dy * dy <= radiusSq && grid[j][i] > 0)
        //                    {
        //                        pointsInRadius.Add(new Point(i, j));
        //                    }
        //                }
        //            }

        //            return pointsInRadius;
        //        }
        //        catch(Exception ex)
        //        {
        //            Debug.WriteLine("GetValidPointsInRadius " + ex);

        //            return [];
        //        }
        //    }

        //    [Obsolete]
        //    public static List<Point> GetPointsInRadius(int[][] grid, Point center, int radius)
        //    {
        //        var pointsInRadius = new List<Point>();
        //        int rows = grid.Length;
        //        int cols = grid[0].Length;

        //        // Перебираем все точки в квадрате, охватывающем радиус
        //        for(int i = center.X - radius; i <= center.X + radius; i++)
        //        {
        //            for(int j = center.Y - radius; j <= center.Y + radius; j++)
        //            {
        //                // Проверяем, что точка не выходит за границы сетки
        //                if(i >= 0 && i < rows && j >= 0 && j < cols)
        //                {
        //                    // Проверяем, находится ли точка в пределах круга
        //                    if(Math.Sqrt(Math.Pow(i - center.X, 2) + Math.Pow(j - center.Y, 2)) <= radius && grid[j][i] >= 1)
        //                    {
        //                        pointsInRadius.Add(new Point(i, j));
        //                    }
        //                }
        //            }
        //        }

        //        return pointsInRadius;
        //    }

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    public static bool IsNeighborsDirectly(int[][] grid, Point p0, Point p1)
        //    {
        //        int dx = p0.X - p1.X;
        //        int dy = p0.Y - p1.Y;

        //        if((dx != 1 || dy != 0) && (dx != -1 || dy != 0) &&
        //            (dx != 0 || dy != 1) && (dx != 0 || dy != -1))
        //            return false;

        //        if((uint)p1.Y >= (uint)grid.Length || (uint)p1.X >= (uint)grid[0].Length)
        //            return false;

        //        int val = grid[p1.Y][p1.X];
        //        return (uint)(val - 1) <= 5;
        //    }

        //    //public static bool IsNeighborsDirectly(int[][] grid, Point p0, Point p1)
        //    //{
        //    //    if(Math.Abs(p0.X - p1.X) + Math.Abs(p0.Y - p1.Y) != 1)
        //    //        return false;

        //    //    if(p1.X >= 0 && p1.X < grid[0].Length && p1.Y >= 0 && p1.Y < grid.Length)
        //    //        return grid[p1.Y][p1.X] is >= 1 and <= 6;

        //    //    return false;
        //    //}

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    public static bool IsValidNeighboringDirectly(int[][] grid, Point start, Point end, int minGridValue = 1)
        //    {
        //        Span<Point> neighbors = stackalloc Point[4];
        //        int neighborCount = GetNeighbors444(grid, start, neighbors, offset: 3);

        //        int height = grid.Length;
        //        int width = grid[0].Length;

        //        Span<Point> lineBuffer = stackalloc Point[Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)) + 10];

        //        for(int i = 0; i < neighborCount; i++)
        //        {
        //            Point neighbor = neighbors[i];
        //            int pointCount = DrawLine(neighbor, end, lineBuffer);

        //            for(int j = 0; j < pointCount; j++)
        //            {
        //                Point point = lineBuffer[j];

        //                // Проверка границ и значения
        //                if((uint)point.Y >= (uint)height || (uint)point.X >= (uint)width)
        //                    return false;

        //                if(grid[point.Y][point.X] < minGridValue)
        //                    return false;
        //            }
        //        }

        //        return true;
        //    }


        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    private static int DrawLine(Point start, Point end, Span<Point> output)
        //    {
        //        int x = start.X, y = start.Y;
        //        int x1 = end.X, y1 = end.Y;

        //        int dx = Math.Abs(x1 - x);
        //        int dy = Math.Abs(y1 - y);
        //        int sx = x < x1 ? 1 : -1;
        //        int sy = y < y1 ? 1 : -1;
        //        int err = dx - dy;

        //        int count = 0;

        //        while(true)
        //        {
        //            output[count++] = new Point(x, y);

        //            if(x == x1 && y == y1)
        //                break;

        //            int e2 = err << 1;

        //            if(e2 > -dy)
        //            {
        //                err -= dy;
        //                x += sx;
        //            }

        //            if(e2 < dx)
        //            {
        //                err += dx;
        //                y += sy;
        //            }
        //        }

        //        return count;
        //    }

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    public static List<Point> DrawLine(Point start, Point end)
        //    {
        //        int x = start.X, y = start.Y;
        //        int x1 = end.X, y1 = end.Y;

        //        int dx = Math.Abs(x1 - x);
        //        int dy = Math.Abs(y1 - y);
        //        int sx = x < x1 ? 1 : -1;
        //        int sy = y < y1 ? 1 : -1;
        //        int err = dx - dy;

        //        int capacity = Math.Max(dx, dy) + 1;
        //        List<Point> points = new List<Point>(capacity);

        //        while(true)
        //        {
        //            points.Add(new Point(x, y));

        //            if(x == x1 && y == y1)
        //                break;

        //            int e2 = err << 1;

        //            if(e2 > -dy)
        //            {
        //                err -= dy;
        //                x += sx;
        //            }

        //            if(e2 < dx)
        //            {
        //                err += dx;
        //                y += sy;
        //            }
        //        }

        //        return points;
        //    }

        //    private static List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
        //    {
        //        var path = new List<Point> { current };
        //        while(cameFrom.ContainsKey(current))
        //        {
        //            current = cameFrom[current];
        //            path.Insert(0, current);
        //        }
        //        return path;
        //    }

        //    public static List<Point> GetValidGridLine(int[][] grid, Point start, Point end)
        //    {
        //        List<Point> line = new List<Point>();

        //        // Предварительное вычисление размеров сетки
        //        int height = grid.Length;
        //        int width = (height > 0) ? grid[0].Length : 0;

        //        // Вычисляем разницу по координатам
        //        int dx = Math.Abs(end.X - start.X);
        //        int dy = Math.Abs(end.Y - start.Y);

        //        int sx = start.X < end.X ? 1 : -1;
        //        int sy = start.Y < end.Y ? 1 : -1;

        //        int err = dx - dy;
        //        int x = start.X;
        //        int y = start.Y;

        //        while(x != end.X || y != end.Y)
        //        {
        //            // Проверяем, что точка находится внутри сетки
        //            if(x < 0 || x >= width || y < 0 || y >= height)
        //                break;

        //            // Если значение в сетке равно 0, прекращаем обработку
        //            if(grid[y][x] == 0)
        //                break;

        //            // Добавляем точку в линию
        //            line.Add(new Point(x, y));

        //            int e2 = 2 * err;
        //            if(e2 > -dy)
        //            {
        //                err -= dy;
        //                x += sx;
        //            }
        //            if(e2 < dx)
        //            {
        //                err += dx;
        //                y += sy;
        //            }
        //        }

        //        return line;
        //    }

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    private static int GetNeighbors444(int[][] grid, Point current, Span<Point> output, int offset = 1)
        //    {
        //        int x = current.X;
        //        int y = current.Y;
        //        int width = grid[0].Length;
        //        int height = grid.Length;
        //        int count = 0;

        //        if((uint)(x - 1) < (uint)width && (uint)(grid[y][x - 1] - 1) <= 5)
        //            output[count++] = new Point(x - offset, y);

        //        if((uint)(x + 1) < (uint)width && (uint)(grid[y][x + 1] - 1) <= 5)
        //            output[count++] = new Point(x + offset, y);

        //        if((uint)(y - 1) < (uint)height && (uint)(grid[y - 1][x] - 1) <= 5)
        //            output[count++] = new Point(x, y - offset);

        //        if((uint)(y + 1) < (uint)height && (uint)(grid[y + 1][x] - 1) <= 5)
        //            output[count++] = new Point(x, y + offset);

        //        return count;
        //    }

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    private static List<Point> GetNeighbors4440(int[][] grid, Point current, int offset = 1)
        //    {
        //        try
        //        {
        //            var neighbors = new List<Point>();
        //            int x = current.X;
        //            int y = current.Y;

        //            if(x > 0 && grid[y][x - 1] is >= 1 and <= 6)
        //                neighbors.Add(new Point(x - offset, y));
        //            if(x < grid[0].Length - 1 && grid[y][x + 1] is >= 1 and <= 6)
        //                neighbors.Add(new Point(x + offset, y));
        //            if(y > 0 && grid[y - 1][x] is >= 1 and <= 6)
        //                neighbors.Add(new Point(x, y - offset));
        //            if(y < grid.Length - 1 && grid[y + offset][x] is >= 1 and <= 6)
        //                neighbors.Add(new Point(x, y + offset));

        //            return neighbors;
        //        }
        //        catch(Exception ex)
        //        {
        //            Debug.WriteLine(ex);
        //            return [];
        //        }
        //    }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static List<Point> GetNeighbors44(int[][] grid, Point current)
        //{
        //    try
        //    {
        //        var neighbors = new List<Point>();
        //        int x = current.X;
        //        int y = current.Y;

        //        if (x > 0 && grid[y][x - 1] is >= 1 and <= 6)
        //            neighbors.Add(new Point(x - 1, y));
        //        if (x < grid[0].Length - 1 && grid[y][x + 1] is >= 1 and <= 6)
        //            neighbors.Add(new Point(x + 1, y));
        //        if (y > 0 && grid[y - 1][x] is >= 1 and <= 6)
        //            neighbors.Add(new Point(x, y - 1));
        //        if (y < grid.Length - 1 && grid[y + 1][x] is >= 1 and <= 6)
        //            neighbors.Add(new Point(x, y + 1));

        //        return neighbors;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex);
        //        return [];
        //    }
        //}


        //    private static List<(Point point, int value)> FindClosestAccessiblePoints(int[][] grid, Point point, int radius, int gridValue = 5)
        //    {
        //        int rows = grid.Length;
        //        int cols = grid[0].Length;

        //        List<(Point, int value)> points = [];

        //        for(int dy = -radius; dy <= radius; dy++)
        //        {
        //            for(int dx = -radius; dx <= radius; dx++)
        //            {
        //                int newX = point.X + dx;
        //                int newY = point.Y + dy;

        //                if(newX >= 0 && newX < cols && newY >= 0 && newY < rows && grid[newY][newX] >= gridValue)
        //                {
        //                    //return new Point(newX, newY);
        //                    points.Add(new(new Point(newX, newY), grid[newY][newX]));
        //                }
        //            }
        //        }

        //        return points;
        //    }

        //    public static List<Point> GetVisitOrderWithCache(
        //    int[][] grid,
        //    List<Point> points,
        //    Point start,
        //    Point end)
        //    {
        //        return GetVisitOrderWithCache(grid, points, start);
        //    }

        //    public static List<Point> GetVisitOrderWithCache(
        //    int[][] grid,
        //    List<Point> points,
        //    Point start)
        //    {
        //        var result = new List<Point> { start };
        //        var remaining = new HashSet<Point>(points);
        //        remaining.Remove(start);

        //        var costCache = new ConcurrentDictionary<long, double>();

        //        static long Key(Point a, Point b)
        //        {
        //            long v1 = ((long)a.X << 32) | (uint)a.Y;
        //            long v2 = ((long)b.X << 32) | (uint)b.Y;
        //            return v1 < v2 ? (v1 << 32) ^ v2 : (v2 << 32) ^ v1;
        //        }

        //        double GetCost(Point a, Point b, double currentBest)
        //        {
        //            long key = Key(a, b);

        //            if(costCache.TryGetValue(key, out double val))
        //                return val;

        //            // дешевая, но строгая нижняя оценка
        //            double heuristic = PathFinderHeuristic.DiagonalDistance(a, b);

        //            // уже хуже — нет смысла считать real path
        //            if(heuristic > currentBest)
        //                return double.MaxValue;

        //            // полный поиск
        //            double cost = PathFinderHeuristic.FindPathCost(a, b, grid);
        //            cost += heuristic;

        //            costCache.TryAdd(key, cost);
        //            return cost;
        //        }

        //        Point current = start;

        //        while(remaining.Count > 0)
        //        {
        //            Point bestPoint = default;
        //            double bestCost = double.MaxValue;

        //            var snapshot = remaining.ToList();
        //            object sync = new object();

        //            Parallel.ForEach(
        //                snapshot,
        //                () => (localBest: default(Point), localCost: double.MaxValue),
        //                (p, _, local) =>
        //                {
        //                    double c = GetCost(current, p, local.localCost);
        //                    if(c < local.localCost)
        //                    {
        //                        local.localBest = p;
        //                        local.localCost = c;
        //                    }
        //                    return local;
        //                },
        //                local =>
        //                {
        //                    lock(sync)
        //                    {
        //                        if(local.localCost < bestCost)
        //                        {
        //                            bestCost = local.localCost;
        //                            bestPoint = local.localBest;
        //                        }
        //                    }
        //                });

        //            if(bestPoint == default)
        //                break;

        //            result.Add(bestPoint);
        //            remaining.Remove(bestPoint);
        //            current = bestPoint;
        //        }

        //        return result;
        //    }

        //    public static Point FindFurthestPointByPath(int[][] grid, Point startPoint, List<Point> points)
        //    {
        //        Point furthestPoint = startPoint;
        //        int maxPathLength = 0;

        //        int сurrentIteration = 0;

        //        foreach(var point in points)
        //        {
        //            DebugWindow.LogMsg($"Find Furthest Point By Path:  {сurrentIteration++} / {points.Count} ... ");

        //            // Находим путь от стартовой точки до текущей точки
        //            var pathFinderResult = Pathfinder.FindPath(grid, startPoint, point);

        //            // Проверяем, является ли точка доступной и если путь не пустой
        //            if(pathFinderResult.PathFound)
        //            {
        //                // Сравниваем длину текущего пути с максимальной длиной
        //                if(pathFinderResult.Path.Count > maxPathLength)
        //                {
        //                    maxPathLength = pathFinderResult.Path.Count;
        //                    furthestPoint = point;
        //                }
        //            }
        //        }

        //        return furthestPoint;
        //    }

        //    /// <summary>

        //    public static List<Point> OptimizePathFromEnd(int[][] grid, List<Point> path)
        //    {
        //        if(path == null || path.Count < 2)
        //            return path;

        //        List<Point> optimizedPath = new List<Point>(path.Count / 2 + 1) { path[0] };
        //        int currentIndex = 0;
        //        int pathCount = path.Count;

        //        // Используем Span для избежания bounds checking
        //        var pathSpan = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(path);

        //        while(currentIndex < pathCount - 1)
        //        {
        //            // Экспоненциальный поиск для быстрого нахождения диапазона
        //            int step = 1;
        //            int candidate = currentIndex + step;
        //            int lastValid = currentIndex;

        //            // Фаза экспоненциального роста
        //            while(candidate < pathCount && IsValidDirectlyUnsafe(grid, pathSpan[currentIndex], pathSpan[candidate]))
        //            {
        //                lastValid = candidate;
        //                step <<= 1; // умножаем на 2
        //                candidate = currentIndex + step;
        //            }

        //            // Бинарный поиск в найденном диапазоне
        //            int left = lastValid + 1;
        //            int right = Math.Min(candidate, pathCount - 1);

        //            while(left <= right)
        //            {
        //                int mid = left + ((right - left) >> 1);
        //                if(IsValidDirectlyUnsafe(grid, pathSpan[currentIndex], pathSpan[mid]))
        //                {
        //                    lastValid = mid;
        //                    left = mid + 1;
        //                }
        //                else
        //                {
        //                    right = mid - 1;
        //                }
        //            }

        //            if(lastValid == currentIndex)
        //            {
        //                currentIndex++;
        //                if(currentIndex < pathCount)
        //                    optimizedPath.Add(pathSpan[currentIndex]);
        //            }
        //            else
        //            {
        //                // Добавляем промежуточные точки без создания списка
        //                if(lastValid > currentIndex + 1)
        //                {
        //                    AddLinePoints(pathSpan[currentIndex], pathSpan[lastValid], optimizedPath);
        //                }
        //                else
        //                {
        //                    optimizedPath.Add(pathSpan[lastValid]);
        //                }

        //                currentIndex = lastValid;
        //            }
        //        }

        //        return optimizedPath;
        //    }

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    private static void AddLinePoints(Point start, Point end, List<Point> output)
        //    {
        //        int x = start.X, y = start.Y;
        //        int dx = end.X - start.X;
        //        int dy = end.Y - start.Y;
        //        int adx = Math.Abs(dx);
        //        int ady = Math.Abs(dy);
        //        int sx = (dx >> 31) | 1; // branchless: dx < 0 ? -1 : 1
        //        int sy = (dy >> 31) | 1;
        //        int err = adx - ady;

        //        while(true)
        //        {
        //            int e2 = err << 1;

        //            if(e2 > -ady)
        //            {
        //                err -= ady;
        //                x += sx;
        //            }
        //            if(e2 < adx)
        //            {
        //                err += adx;
        //                y += sy;
        //            }

        //            if(x == end.X && y == end.Y)
        //            {
        //                output.Add(end);
        //                break;
        //            }

        //            output.Add(new Point(x, y));
        //        }
        //    }

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    private static unsafe bool IsValidDirectlyUnsafe(int[][] grid, Point p0, Point p1, int minGridValue = 1)
        //    {
        //        int x = p0.X, y = p0.Y;
        //        int dx = p1.X - p0.X;
        //        int dy = p1.Y - p0.Y;
        //        int adx = Math.Abs(dx);
        //        int ady = Math.Abs(dy);
        //        int sx = (dx >> 31) | 1; // branchless sign
        //        int sy = (dy >> 31) | 1;
        //        int err = adx - ady;

        //        int height = grid.Length;
        //        int width = grid[0].Length;

        //        while(true)
        //        {
        //            // Bounds check с unsigned comparison для обеих координат
        //            if((uint)y >= (uint)height || (uint)x >= (uint)width)
        //                return false;

        //            // Прямой доступ к элементу массива
        //            int value = grid[y][x];
        //            if(value < minGridValue)
        //                return false;

        //            if(x == p1.X && y == p1.Y)
        //                return true;

        //            int e2 = err << 1;

        //            if(e2 > -ady)
        //            {
        //                err -= ady;
        //                x += sx;
        //            }
        //            if(e2 < adx)
        //            {
        //                err += adx;
        //                y += sy;
        //            }
        //        }
        //    }

        //    ////////////////

        //    public static List<Point> OptimizePathFromEnd1(int[][] grid, List<Point> path)
        //    {
        //        if(path == null || path.Count < 2)
        //            return path;

        //        List<Point> optimizedPath = new List<Point> { path[0] };
        //        int currentIndex = 0;

        //        while(currentIndex < path.Count - 1)
        //        {
        //            int farthestValidIndex = currentIndex;

        //            for(int i = path.Count - 1; i > currentIndex; i--)
        //            {
        //                if(IsValidDirectly(grid, path[currentIndex], path[i]))
        //                {
        //                    farthestValidIndex = i;
        //                    break;
        //                }
        //            }

        //            if(farthestValidIndex == currentIndex)
        //            {
        //                break;
        //            }

        //            optimizedPath.Add(path[farthestValidIndex]);

        //            if(farthestValidIndex > currentIndex + 1)
        //            {
        //                List<Point> linePoints = DrawLine(path[currentIndex], path[farthestValidIndex]);

        //                for(int j = 1; j < linePoints.Count - 1; j++)
        //                {
        //                    optimizedPath.Add(linePoints[j]);
        //                }
        //            }

        //            currentIndex = farthestValidIndex;
        //        }

        //        return optimizedPath;
        //    }

        //    public void InitAfterTransition()
        //    {
        //        //GlobalVariables.gameController = GameController;

        //        PlayerInfo.playerBeforeTransitionPosition = new();
        //        PlayerInfo.PlayerGridPosition = GameController.Player.GridPos.ToPoint();
        //        PlayerInfo.playerPrevGridPosition = GameController.Player.GridPos;

        //        Storages.InteractiveEntity.Reset();
        //        TerrainInfo.Reset();
        //    }

        //    public void InitTilesPosition(string[] tileMetas)
        //    {
        //        bool specificInit = tileMetas.Length == 1 &&
        //                            (tileMetas[0] == "Generate" || tileMetas[0] == "Stay" || tileMetas[0] == string.Empty);

        //        if(specificInit)
        //        {
        //            Storages.Path.PathFinderInfo.SetCurrentTileMeta(tileMetas[0]);
        //            return;
        //        }

        //        foreach(string tileMetaName in tileMetas)
        //        {
        //            if(TerrainInfo._allTargetLocations.TryGetValue(tileMetaName, out List<Point> points) &&
        //                points.Count > 0)
        //            {
        //                Storages.Path.PathFinderInfo.SetMetaTilesPosition(points);
        //                Storages.Path.PathFinderInfo.SetCurrentTileMeta(tileMetaName);
        //                break;
        //            }
        //        }
        //    }

        //    public void InitTileMetas(string areaName)
        //    {
        //        //Get All TilePosition
        //        List<Point> tilePoints = [];

        //        TileDescription tileDescription = TileData.GetDescription(areaName);
        //        //Это удалить наверно надо 
        //        if(tileDescription is not null)
        //        {
        //            List<string> tileMetas = [.. tileDescription.TileMetaName.Split(',')];

        //            //Init tileMeta for pathfinding and currentTile
        //            if(tileMetas.Count is 1 && (tileMetas[0] is "Generate" || tileMetas[0] is "Stay" || tileMetas[0] is ""))
        //            {
        //                Storages.Path.PathFinderInfo.SetCurrentTileMeta(tileMetas[0]);
        //            }
        //            else
        //            {
        //                foreach(string tileMetaName in tileMetas)
        //                {
        //                    if(TerrainInfo._allTargetLocations.TryGetValue(tileMetaName, out tilePoints))
        //                    {
        //                        if(tilePoints.Count == 0) continue;

        //                        Storages.Path.PathFinderInfo.SetMetaTilesPosition(tilePoints);
        //                        Storages.Path.PathFinderInfo.SetCurrentTileMeta(tileMetaName);

        //                        break;
        //                    }
        //                }
        //            }

        //            //init target Entity (kill,Interacting,etc)
        //            if(tileDescription.EntityDescription is not null)
        //            {
        //                Storages.Path.PathFinderInfo.SetCurrentEntity(tileDescription.EntityDescription);
        //            }
        //        }
        //    }

        //    public void InitVisitPointSum(int radius, float threshold)
        //    {
        //        if(GameController.Area.CurrentArea.IsHideout || GameController.Area.CurrentArea.IsTown) return;

        //        int radiusSavePoints = 100;
        //        var startPosition = GetPointsInRadius(
        //            TerrainInfo._areaStartPos,
        //            radiusSavePoints,
        //            TerrainInfo.AreaDimensions.Value.X,
        //            TerrainInfo.AreaDimensions.Value.Y);

        //        //Point weight calculation
        //        float[,] distanceMapFloat = ComputeDistanceToWalls(TerrainInfo.ProcessedTerrainData);
        //        //Calculation of the weight of the area on the terrane
        //        var areasNotExceedingTheThresholds = FindPointsWithSumBelowOrEqualThreshold(distanceMapFloat, radius, threshold);

        //        int[][] tempTerrain = TerrainInfo.ProcessedTerrainData.DeepCopy();

        //        foreach(var p in areasNotExceedingTheThresholds)
        //        {
        //            if(startPosition.Contains(p)) continue;

        //            tempTerrain[p.Y][p.X] = 0;
        //        }

        //        var newTerrain = TerrainInfo.GetCurrentLocation(tempTerrain, TerrainInfo._areaStartPos, out _);

        //        var pointsOnLocation = GenerateKeyPoints(newTerrain, (int)(Constants.RangeGeneratePoints * 1.5));

        //        //TargetDebugVar.debugListPoint.AddRange(pointsOnLocation);


        //        var end = FindFurthestPointByPath(newTerrain, TerrainInfo._areaStartPos, pointsOnLocation);
        //        var order = GetVisitOrderWithCache(newTerrain, pointsOnLocation, TerrainInfo._areaStartPos, end);


        //        Storages.Path.PathSegments.SetPathSegments(order);

        //        var endPoint = order.LastOrDefault();
        //        if(endPoint == default)
        //        {
        //            LogError("InitRegen: EndPoint is zero!", 10);
        //        }

        //        while(!Storages.Path.PathSegments.isLastSegment)
        //        {
        //            if(!Storages.Path.PathSegments.TryDequeuePathSegment(out Point newPoint) || newPoint == default) break;

        //            if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, newPoint, 2)) continue;

        //            if(!Storages.MapPoint.Contains(newPoint))
        //            {
        //                Storages.Path.SetCurrentTargetPoint(newPoint);
        //                break;
        //            }
        //        }
        //    }

        //    public void InitEndPoint(string meta)
        //    {
        //        if(GameController.Area.CurrentArea.IsHideout || GameController.Area.CurrentArea.IsTown) return;

        //        Point endPoint = default;
        //        List<Point> path = new List<Point>();

        //        if(meta != null && meta != "Generate" && meta != "Stay" && meta != "")
        //        {
        //            if(TerrainInfo._allTargetLocations.TryGetValue(meta, out List<Point> TilePoints))
        //            {
        //                foreach(var point in TilePoints)
        //                {
        //                    Point currentEnd = point;
        //                    if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, point, 2))
        //                    {
        //                        currentEnd = FindClosestAccessiblePointRange(TerrainInfo.ProcessedTerrainData, point, 10, 50, 2);
        //                        if(currentEnd == default) continue;
        //                    }

        //                    var pathFinderResult = Pathfinder.FindPath(
        //                        grid: TerrainInfo.ProcessedTerrainData,
        //                        start: PlayerInfo.PlayerGridPosition,
        //                        end: currentEnd
        //                        );

        //                    if(pathFinderResult.PathFound)
        //                    {
        //                        endPoint = currentEnd;
        //                        path = OptimizePathFromEnd(TerrainInfo.ProcessedTerrainData, pathFinderResult.Path);
        //                        break;
        //                    }
        //                }
        //            }
        //        }

        //        if(endPoint == default)
        //        {
        //            LogError("InitRegen: EndPoint is zero!", 10);
        //        }
        //        else if(path.Count > 0)
        //        {
        //            Storages.Path.SetEndPoint(endPoint);
        //        }
        //        else
        //        {
        //            LogError("InitRegen: No path found to endpoint!", 10);
        //        }
        //    }

        //    public bool ShouldSearchForNewPath
        //    {
        //        get
        //        {
        //            if(Storages.Watch.MainRegenTimerWatch.IsActionAllowed ||
        //                Storages.Path.PrevPosition != PlayerInfo.PlayerGridPosition)
        //            {
        //                if(GlobalVariables.AggressiveMode)
        //                {
        //                    if(Storages.InteractiveEntity.entitiesToAttack.IsEmpty && Storages.InteractiveEntity.entitiesMonster.IsEmpty &&
        //                        Storages.Loot.IsEmpty && !LootAroundOnGroundAny())
        //                    {
        //                        return true;
        //                    }
        //                }
        //                else
        //                {
        //                    var attackRange = Settings.ControlSettings.RangeSettings.AttackRange.Value;

        //                    var haveAttackMonster = Storages.InteractiveEntity.entitiesToAttack
        //                        .Any(e => Vector3.Distance(e.Pos, PlayerInfo.playerWorldPosition) < attackRange);

        //                    var HaveMosters = Storages.InteractiveEntity.entitiesMonster
        //                        .Any(e => Vector3.Distance(e.Pos, PlayerInfo.playerWorldPosition) < attackRange);

        //                    if(!HaveMosters && !haveAttackMonster &&
        //                        Storages.Loot.IsEmpty && !LootAroundOnGroundAny())
        //                    {
        //                        return true;
        //                    }
        //                }

        //            }

        //            return false;
        //        }
        //    }

        //    public void GetAnotherEndPointOnArea()
        //    {
        //        try
        //        {
        //            if(!Storages.InteractiveEntity.entitiesAreaTransitionOnScreen.IsEmpty) return;
        //            //Cleat Data Disable MainRegen Segment 
        //            Storages.Path.CurrentPathClear();

        //            //View entities that have already been seen. Search for important entities.
        //            List<EntityDataOnMap> allFoundEntities = Storages.StaticEntity.GetAllNotIgnored();

        //            if(allFoundEntities.Count != 0)
        //            {
        //                allFoundEntities = [.. allFoundEntities.WhereF(x =>
        //                {
        //                    return EntityData.currentEntities.Any(entity =>
        //                        x.RenderName == entity.RenderName &&
        //                        x.Metadata == entity.Metadata &&
        //                        x.Type == entity.EntityType);
        //                })];
        //            }

        //            if(allFoundEntities.Count == 0)
        //            {
        //                LogError("Error Change Entity: GetAnotherEndPointOnArea,allFoundEntities.Count: 0");
        //                return;
        //            }

        //            //Add Around Entities to notvalid
        //            foreach(var entity in allFoundEntities)
        //            {
        //                if(WHelper.GridToWorldDistance(PlayerInfo.PlayerGridPosition, entity.GridPos.ToPoint()) > Constants.RangeDetectNoValidEntities) continue;

        //                Storages.StaticEntity.AddNotValid(entity);
        //            }

        //            //Find Another Valid Entities On Storage 
        //            var nearestEntity = allFoundEntities.Where(x =>
        //            {
        //                if(x.Type != EntityType.AreaTransition) return false;

        //                Point endPoint = x.GridPos.ToPoint();

        //                if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, x.GridPos.ToPoint(), 4))
        //                {
        //                    endPoint = FindClosestAccessiblePoint(TerrainInfo.ProcessedTerrainData, x.GridPos.ToPoint(), 15);
        //                }

        //                if(!Storages.StaticEntity.ContainsNotValid(x) && endPoint != default)
        //                {
        //                    return true;
        //                }

        //                return false;
        //            }).OrderBy(x => WHelper.GridToWorldDistance(PlayerInfo.PlayerGridPosition, x.GridPos.ToPoint()));

        //            if(!nearestEntity.Any())
        //            {//changing to another tile if not found entities
        //                if(Storages.Path.PathFinderInfo.TryDequeueMetaTilePosition(out Point TilePosition))
        //                {
        //                    FullRegenPath(TilePosition);
        //                }

        //                return;
        //            }

        //            DebugWindow.LogMsg($"Found Entities From Data, FullRegenPath: {nearestEntity.First().GridPos.ToPoint()}");
        //            FullRegenPath(nearestEntity.First().GridPos.ToPoint());
        //        }
        //        catch(Exception ex)
        //        {
        //            LogError("GetAnotherEndPointOnArea" + ex);
        //            Debug.WriteLine("GetAnotherEndPointOnArea" + ex);
        //        }
        //    }

        //    public bool TryChangeCurrentPathSegment()
        //    {
        //        while(!Storages.Path.PathSegments.isLastSegment)
        //        {
        //            if(!Storages.Path.PathSegments.TryDequeuePathSegment(out Point newPoint)) continue;

        //            if(!IsValidPoint(TerrainInfo.ProcessedTerrainData, newPoint, 2))
        //            {
        //                var findPoint = FindClosestAccessiblePoint(TerrainInfo.ProcessedTerrainData, newPoint, 15);

        //                if(findPoint == default) continue;

        //                newPoint = findPoint;
        //            }

        //            if(!Storages.MapPoint.Contains(newPoint) || Storages.Path.PathSegments.isLastSegment)
        //            {
        //                Storages.Path.SetCurrentTargetPoint(newPoint);

        //                return true;
        //            }
        //        }

        //        return false;
        //    }

        //    public void UpdateGrid()
        //    {
        //        var entityF = GameController.EntityListWrapper.OnlyValidEntities.WhereF(x => (x.Type != EntityType.None || x.Type != EntityType.Error) &&
        //            StorageVar.impenetrableEntityType.Contains(x.Type)).ToList();

        //        UpdateGridRadius(TerrainInfo.ProcessedTerrainData, entityF, StorageVar.impenetrableEntityType, StorageVar.noBlockingEntityMeta, PlayerInfo.PlayerGridPosition);

        //        //Update Door Grid Position 
        //        var entityDoor = GameController.EntityListWrapper.OnlyValidEntities.WhereF(x => (x.Type != EntityType.None || x.Type != EntityType.Error) &&
        //            x.TryGetComponent(out TriggerableBlockage componentBlock) && x.TryGetComponent(out Targetable componentTargetable));

        //        UpdateGridRadius(TerrainInfo.ProcessedTerrainData, entityDoor, 1);
        //    }

        //    public static void UpdateGridRadius(int[][] grid, List<Entity> entities, ConcurrentBag<EntityType> impenetrableEntity, ConcurrentBag<string> impenetrableEntityMeta, Point playerPosition, int radius = (int)Math.PI)
        //    {
        //        if(grid == null || entities == null || impenetrableEntity == null)
        //            throw new ArgumentNullException("UpdateGridRadius: Error input data");

        //        if(grid[playerPosition.Y][playerPosition.X] == 0)
        //        {
        //            var points = GetValidPointsInRadius(grid, PlayerInfo.PlayerGridPosition, 2);

        //            foreach(var p in points)
        //            {
        //                grid[p.Y][p.X] = 5;
        //            }
        //        }

        //        foreach(var entity in entities)
        //        {
        //            if(impenetrableEntityMeta.Contains(entity.Metadata)) continue;

        //            if(!entity.TryGetComponent(out Render render)) continue;
        //            if(!entity.TryGetComponent(out Positioned positioned)) continue;

        //            if(grid[(int)entity.GridPos.Y][(int)entity.GridPos.X] == 0) continue;

        //            int centerX = (int)entity.GridPos.X;
        //            int centerY = (int)entity.GridPos.Y;

        //            int rows = grid.Length;
        //            int cols = grid[0].Length;

        //            for(int y = centerY - radius; y <= centerY + radius; y++)
        //            {
        //                for(int x = centerX - radius; x <= centerX + radius; x++)
        //                {
        //                    if(x >= 0 && x < cols && y >= 0 && y < rows)
        //                    {
        //                        if(IsPointInsideCircle(centerX, centerY, x, y, radius))
        //                        {
        //                            grid[y][x] = 0;
        //                            TerrainInfo.ProcessedTerrainData[y][x] = 0;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    private static float[,] ComputePrefixSum(float[,] data)
        //    {
        //        int rows = data.GetLength(0);
        //        int cols = data.GetLength(1);
        //        float[,] prefixSum = new float[rows, cols];

        //        for(int y = 0; y < rows; y++)
        //        {
        //            float rowSum = 0;
        //            for(int x = 0; x < cols; x++)
        //            {
        //                rowSum += data[y, x];
        //                prefixSum[y, x] = rowSum + (y > 0 ? prefixSum[y - 1, x] : 0);
        //            }
        //        }

        //        return prefixSum;
        //    }

        //    private static float GetSumInRect(float[,] prefixSum, int y1, int x1, int y2, int x2)
        //    {
        //        float total = prefixSum[y2, x2];
        //        if(y1 > 0) total -= prefixSum[y1 - 1, x2];
        //        if(x1 > 0) total -= prefixSum[y2, x1 - 1];
        //        if(y1 > 0 && x1 > 0) total += prefixSum[y1 - 1, x1 - 1];
        //        return total;
        //    }

        //    public static List<Point> FindPointsWithSumBelowOrEqualThreshold(float[,] distanceMapFloat, int radius, float threshold)
        //    {
        //        float minThreshold = (threshold - (threshold / 100 * 95));

        //        int rows = distanceMapFloat.GetLength(0);
        //        int cols = distanceMapFloat.GetLength(1);

        //        float[,] prefixSum = ComputePrefixSum(distanceMapFloat);
        //        var resultPoints = new List<Point>();

        //        for(int y = 0; y < rows; y++)
        //        {
        //            for(int x = 0; x < cols; x++)
        //            {
        //                int y1 = Math.Max(0, y - radius);
        //                int y2 = Math.Min(rows - 1, y + radius);
        //                int x1 = Math.Max(0, x - radius);
        //                int x2 = Math.Min(cols - 1, x + radius);

        //                float sum = GetSumInRect(prefixSum, y1, x1, y2, x2);

        //                if(sum <= threshold && sum >= minThreshold)
        //                {
        //                    resultPoints.Add(new Point(x, y));
        //                }
        //            }
        //        }

        //        return resultPoints;
        //    }

        //    public static List<Point> FindPointsWithSumAboveThreshold(float[,] distanceMapFloat, int radius, float threshold)
        //    {
        //        int rows = distanceMapFloat.GetLength(0);
        //        int cols = distanceMapFloat.GetLength(1);

        //        var prefixSum = ComputePrefixSum(distanceMapFloat);
        //        var resultPoints = new List<Point>();

        //        for(int y = 0; y < rows; y++)
        //        {
        //            for(int x = 0; x < cols; x++)
        //            {
        //                int y1 = Math.Max(0, y - radius);
        //                int y2 = Math.Min(rows - 1, y + radius);
        //                int x1 = Math.Max(0, x - radius);
        //                int x2 = Math.Min(cols - 1, x + radius);

        //                float sum = GetSumInRect(prefixSum, y1, x1, y2, x2);

        //                if(sum > threshold)
        //                {
        //                    resultPoints.Add(new Point(x, y));
        //                }
        //            }
        //        }

        //        return resultPoints;
        //    }

        //    public static float[,] ComputeDistanceToWalls(int[][] terrain)
        //    {
        //        int rows = terrain.Length;
        //        int cols = terrain[0].Length;
        //        float[,] dist = new float[rows, cols];
        //        bool[,] visited = new bool[rows, cols];

        //        Queue<(int y, int x)> queue = new();

        //        for(int y = 0; y < rows; y++)
        //        {
        //            for(int x = 0; x < cols; x++)
        //            {
        //                if(terrain[y][x] == 0)
        //                {
        //                    dist[y, x] = 0;
        //                    visited[y, x] = true;
        //                    queue.Enqueue((y, x));
        //                }
        //                else
        //                {
        //                    dist[y, x] = float.MaxValue;
        //                }
        //            }
        //        }

        //        int[] dy = { -1, 1, 0, 0 };
        //        int[] dx = { 0, 0, -1, 1 };

        //        while(queue.Count > 0)
        //        {
        //            var (y, x) = queue.Dequeue();
        //            float currentDist = dist[y, x];

        //            for(int i = 0; i < 4; i++)
        //            {
        //                int ny = y + dy[i];
        //                int nx = x + dx[i];

        //                if(ny < 0 || ny >= rows || nx < 0 || nx >= cols)
        //                    continue;

        //                if(!visited[ny, nx] && terrain[ny][nx] > 0)
        //                {
        //                    dist[ny, nx] = currentDist + 1;
        //                    visited[ny, nx] = true;
        //                    queue.Enqueue((ny, nx));
        //                }
        //            }
        //        }

        //        return dist;
        //    }

        //    public static List<Point> GetPointsInRadius(Point center, int radius, int rows, int cols)
        //    {
        //        var points = new List<Point>();

        //        int startY = Math.Max(0, center.Y - radius);
        //        int endY = Math.Min(rows - 1, center.Y + radius);
        //        int startX = Math.Max(0, center.X - radius);
        //        int endX = Math.Min(cols - 1, center.X + radius);

        //        for(int y = startY; y <= endY; y++)
        //        {
        //            for(int x = startX; x <= endX; x++)
        //            {
        //                points.Add(new Point(x, y));
        //            }
        //        }

        //        return points;
        //    }
        //}
    }
}