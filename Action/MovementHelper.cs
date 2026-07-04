using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WFollowBot.Managers;

namespace WFollowBot.Action;

internal static class MovementHelper
{

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

    public static Point GetNextWaypoint(IReadOnlyList<Point> path, Point playerPos)
    {
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

        for (int i = closestIdx + 1; i < path.Count; i++)
        {
            if (DistanceSquared(playerPos, path[i]) > 3 * 3)
                return path[i];
        }

        return path[^1];
    }

    public static float DistanceSquared(Point a, Point b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
