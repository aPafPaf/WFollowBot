using System;
using System.Drawing;
using WFollowBot.Managers;

namespace WFollowBot.Controllers;

public class FollowController
{
    public float HoldRadius { get; set; } = 10f;
    public Point LastLeaderPosition { get; set; }
    public float SquaredDistToLeader { get; private set; }

    public void SetLeaderDistance(Point leaderPos, Point myPos)
    {
        LastLeaderPosition = leaderPos;
        SquaredDistToLeader = DistanceSquared(myPos, leaderPos);
    }

    public Point ComputeFollowTarget(Point leaderPos, Point myPos)
    {
        float dist = MathF.Sqrt(DistanceSquared(myPos, leaderPos));
        if (dist <= HoldRadius)
            return myPos;

        float dx = myPos.X - leaderPos.X;
        float dy = myPos.Y - leaderPos.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return leaderPos;

        int targetX = leaderPos.X + (int)(dx / len * HoldRadius);
        int targetY = leaderPos.Y + (int)(dy / len * HoldRadius);

        var grid = TerrainInfo.ProcessedTerrainData;
        if (grid.Length > 0 && RegenManager.IsValidPoint(grid, new Point(targetX, targetY), 1))
            return new Point(targetX, targetY);

        return leaderPos;
    }

    private static float DistanceSquared(Point a, Point b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}