using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using WFollowBot.Managers;

namespace WFollowBot.PathFinding;

public class PathfindingService : IPathfindingService
{
    private readonly PlayerInfo _playerInfo;
    private readonly RegenManager _regenManager;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly List<Point> _currentPath = [];

    private double _lastRepathTime;
    private double _lastUpdateTime;
    private Point _lastStuckCheckPosition;
    private double _stuckTimer;
    private Point _prevPosition;
    private Point? _destination;

    public float RepathInterval { get; set; } = 0.5f;
    public float MaxStuckTime { get; set; } = 2.0f;
    public int StuckThreshold { get; set; } = 3;

    public PathStatus Status { get; private set; } = PathStatus.None;
    public IReadOnlyList<Point> CurrentPath => _currentPath;
    public Point? Destination => _destination;
    public bool IsStuck { get; private set; }

    public Point PrevPosition
    {
        get => _prevPosition;
        set => _prevPosition = value;
    }

    public event System.Action OnPathUpdated;
    public event System.Action OnPathCompleted;

    public PathfindingService(PlayerInfo playerInfo, RegenManager regenManager)
    {
        _playerInfo = playerInfo;
        _regenManager = regenManager;
    }

    public void Update()
    {
        var myPos = _playerInfo.PlayerGridPosition;
        var elapsed = _stopwatch.Elapsed.TotalSeconds;
        float deltaTime = (float)(elapsed - _lastUpdateTime);
        _lastUpdateTime = elapsed;

        if (_destination == null)
            return;

        if (DistanceSquared(myPos, _lastStuckCheckPosition) < StuckThreshold * StuckThreshold)
        {
            _stuckTimer += deltaTime;
        }
        else
        {
            _lastStuckCheckPosition = myPos;
            _stuckTimer = 0;
        }
        IsStuck = _stuckTimer > MaxStuckTime;

        bool needRepath = false;

        if (_currentPath.Count == 0)
            needRepath = true;

        if (IsStuck)
            needRepath = true;

        if (_lastRepathTime + RepathInterval < elapsed)
            needRepath = true;

        if (needRepath)
        {
            var result = _regenManager.RegenPathNew(_destination.Value, 15);
            if (result.PathFound)
            {
                var smoothed = PathSmoothing.OptimizePathFromEnd(TerrainInfo.ProcessedTerrainData, result.Path);
                AssignPath(smoothed);
                _lastRepathTime = elapsed;
                IsStuck = false;
                _stuckTimer = 0;
            }
        }
    }

    public void RequestPath(Point destination)
    {
        bool destinationChanged = !_destination.HasValue ||
            _destination.Value.X != destination.X ||
            _destination.Value.Y != destination.Y;
        _destination = destination;

        if (!destinationChanged)
            return;

        var result = _regenManager.RegenPathNew(destination, 15);
        if (result.PathFound)
        {
            var smoothed = PathSmoothing.OptimizePathFromEnd(TerrainInfo.ProcessedTerrainData, result.Path);
            AssignPath(smoothed);
            _lastRepathTime = _stopwatch.Elapsed.TotalSeconds;
        }
        else
        {
            _currentPath.Clear();
            Status = PathStatus.None;
            OnPathCompleted?.Invoke();
        }
    }

    public void ClearPath()
    {
        _currentPath.Clear();
        _destination = null;
        Status = PathStatus.None;
        OnPathCompleted?.Invoke();
    }

    public void RemovePassedPoints(Point playerPos)
    {
        int removeUpTo = 0;
        for (int i = 0; i < _currentPath.Count; i++)
        {
            if (DistanceSquared(playerPos, _currentPath[i]) < 2 * 2)
                removeUpTo = i + 1;
        }
        if (removeUpTo > 0)
        {
            _currentPath.RemoveRange(0, removeUpTo);
            UpdateStatus();
        }
    }

    private void AssignPath(List<Point> path)
    {
        _currentPath.Clear();
        _currentPath.AddRange(path);
        UpdateStatus();
        OnPathUpdated?.Invoke();
    }

    private void UpdateStatus()
    {
        int count = _currentPath.Count;
        if (count > 20)
            Status = PathStatus.Full;
        else if (count > 10)
            Status = PathStatus.Partial;
        else
            Status = PathStatus.None;
    }

    private static float DistanceSquared(Point a, Point b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
