using System;
using System.Collections.Generic;
using System.Drawing;

namespace WFollowBot.PathFinding;

public enum PathStatus
{
    None,
    Partial,
    Full,
}

public interface IPathfindingService
{
    PathStatus Status { get; }
    IReadOnlyList<Point> CurrentPath { get; }
    Point? Destination { get; }
    Point PrevPosition { get; set; }
    bool IsStuck { get; }
    float RepathInterval { get; set; }
    float MaxStuckTime { get; set; }
    int StuckThreshold { get; set; }

    event System.Action OnPathUpdated;
    event System.Action OnPathCompleted;

    void RequestPath(Point destination);
    void ClearPath();
    void RemovePassedPoints(Point playerPos);
    void Update();
}
