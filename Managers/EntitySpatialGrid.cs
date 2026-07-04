using GameHelper.RemoteObjects.States.InGameStateObjects;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace WFollowBot.Managers
{
public class EntitySpatialGrid
{
    private readonly int _cellSize;
    private volatile Dictionary<(int, int), Entity[]> _snapshot = new();
    private Dictionary<(int, int), List<Entity>> _build = new();

    public EntitySpatialGrid(int cellSize = 8)
    {
        _cellSize = cellSize;
    }

    public void BeginRebuild()
    {
        _build.Clear();
    }

    public void Add(Entity entity)
    {
        var pos = GetEntityGridPos(entity);
        int gx = (int)(pos.X / _cellSize);
        int gy = (int)(pos.Y / _cellSize);

        var key = (gx, gy);
        if (!_build.TryGetValue(key, out var list))
            _build[key] = list = new List<Entity>(4);

        list.Add(entity);
    }

    public void EndRebuild()
    {
        var next = new Dictionary<(int, int), Entity[]>(_build.Count);
        foreach(var kv in _build)
        {
            next[kv.Key] = kv.Value.ToArray();
        }

        _snapshot = next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity[] GetEntitiesInCell(Vector2 pos)
    {
        var snap = _snapshot;
        int gx = (int)(pos.X / _cellSize);
        int gy = (int)(pos.Y / _cellSize);

        if(snap.TryGetValue((gx, gy), out var arr))
            return arr;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetEntitiesInRadius(Vector2 pos, float radiusSq, List<Entity> results)
    {
        var snap = _snapshot;
        float radius = MathF.Sqrt(radiusSq);
        int minX = (int)((pos.X - radius) / _cellSize) - 1;
        int minY = (int)((pos.Y - radius) / _cellSize) - 1;
        int maxX = (int)((pos.X + radius) / _cellSize) + 1;
        int maxY = (int)((pos.Y + radius) / _cellSize) + 1;

        for (int gx = minX; gx <= maxX; gx++)
        {
            for (int gy = minY; gy <= maxY; gy++)
            {
                if (snap.TryGetValue((gx, gy), out var arr))
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var e = arr[i];
                        float distSq = Vector2.DistanceSquared(GetEntityGridPos(e), pos);
                        if (distSq <= radiusSq)
                            results.Add(e);
                    }
                }
            }
        }
    }

    private static Vector2 GetEntityGridPos(Entity e)
    {
        if (e.TryGetComponent(out GameHelper.RemoteObjects.Components.Render r))
            return new Vector2(r.GridPosition.X, r.GridPosition.Y);
        return Vector2.Zero;
    }

    public int Count
    {
        get
        {
            int count = 0;
            foreach(var arr in _snapshot.Values)
                count += arr.Length;
            return count;
        }
    }

    public IReadOnlyDictionary<(int, int), Entity[]> GetAllCells() => _snapshot;
}
}
