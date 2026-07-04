using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WFollowBot.Managers;

namespace WFollowBot.Action;

internal static class MovementHelper
{
    public const float DefaultLookaheadRadius = 5f;

    public static Point? GetNextWaypointDirect(IReadOnlyList<Point> path, Point playerPos)
    {
        if (RegenManager.IsValidDirectly(TerrainInfo.ProcessedTerrainData, playerPos, path.LastOrDefault()))
            return path.LastOrDefault();

        Point targetPoint = new();

        for (int i = 1; i < path.Count; i++)
        {
            Point point = path[i];
            if (!RegenManager.IsValidDirectly(TerrainInfo.ProcessedTerrainData, playerPos, point))
            {
                return path[i - 1];
            }
        }

        return null;
    }

    public static Point GetLookaheadPoint(
        IReadOnlyList<Point> path,
        Point playerPos,
        float radius = DefaultLookaheadRadius)
    {
        if (path.Count == 0) return playerPos;
        if (path.Count == 1) return path[0];

        var grid = TerrainInfo.ProcessedTerrainData;

        int closestIdx = 0;
        float closestDistSq = float.MaxValue;
        for (int i = 0; i < path.Count; i++)
        {
            float distSq = DistanceSquared(playerPos, path[i]);
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closestIdx = i;
            }
        }

        float radiusSq = radius * radius;
        int resultIdx = closestIdx;
        for (int i = closestIdx + 1; i < path.Count; i++)
        {
            if (!RegenManager.IsValidDirectly(grid, playerPos, path[i]))
                break;
            if (DistanceSquared(playerPos, path[i]) >= radiusSq)
                return path[i];
            resultIdx = i;
        }

        return path[resultIdx];
    }

    public static float DistanceSquared(Point a, Point b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
