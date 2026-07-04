using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace WFollowBot.Data;

public class IntermediatePoints
{
    public enum MasterIntermediatePoints
    {
        None,
        MainRegen,
        Loot,
        Monster,
        Interaction,
        PriorityInteraction,
        Door,
        Manual
    }

    private readonly object _listLock = new object();
    private LinkedList<Point> _intermediatePoints = [];
    private MasterIntermediatePoints _masterPoint = MasterIntermediatePoints.None;

    public int Count { get => _intermediatePoints.Count; }

    public Point CurrentPoint
    {
        get
        {
            lock (_listLock)
            {
                return _intermediatePoints.FirstOrDefault();
            }
        }
    }

    public bool IsValid
    {
        get
        {
            lock (_listLock)
            {
                return _intermediatePoints.Count > 0;
            }
        }
    }

    public MasterIntermediatePoints MasterPoint
    {
        get
        {
            lock (_listLock)
            {
                return _masterPoint;
            }
        }
        private set
        {
            lock (_listLock)
            {
                _masterPoint = value;
            }
        }
    }

    public void AddPointToFront(Point point, MasterIntermediatePoints master)
    {
        if (point == default) return;

        lock (_listLock)
        {
            if (master != _masterPoint)
                Clear();

            _intermediatePoints.AddFirst(point);
            _masterPoint = master;
        }
    }

    public void AddPointToLast(Point point, MasterIntermediatePoints master)
    {
        if (point == default) return;

        lock (_listLock)
        {
            if (master != _masterPoint)
                Clear();

            _intermediatePoints.AddLast(point);
            _masterPoint = master;
        }
    }

    public List<Point> GetPoints()
    {
        lock (_listLock)
        {
            return new(_intermediatePoints);
        }
    }

    public bool TryUpdateCurrentPoint()
    {
        lock (_listLock)
        {
            if (_intermediatePoints.Count > 0)
            {
                _intermediatePoints.RemoveFirst();
            }
            else
            {
                MasterPoint = MasterIntermediatePoints.None;
            }

            return IsValid;
        }
    }

    public void Clear()
    {
        lock (_listLock)
        {
            _intermediatePoints.Clear();
            _masterPoint = MasterIntermediatePoints.None;
        }
    }
}
