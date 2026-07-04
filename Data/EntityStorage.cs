using GameHelper.RemoteObjects.States.InGameStateObjects;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WFollowBot.Data;

public class EntityStorage
{
    private List<Entity> _entities;
    private readonly object _lock = new object(); // минимальный lock

    public int Count => _entities.Count;
    public bool IsEmpty => _entities.Count == 0;

    public EntityStorage(int capacity = 512)
    {
        _entities = new List<Entity>(capacity); // сразу резервируем память
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Entity entity)
    {
        if (entity == null) return;
        lock (_lock)
        {
            _entities.Add(entity);
        }
    }

    // Быстрая массовая проверка без LINQ
    public bool Any(Predicate<Entity> predicate)
    {
        lock (_lock)
        {
            var span = CollectionsMarshal.AsSpan(_entities); // быстрый доступ к List<T> как Span<T>
            for (int i = 0; i < span.Length; i++)
            {
                if (predicate(span[i]))
                    return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(IEnumerable<Entity> entities)
    {
        if (entities == null) return;
        lock (_lock)
        {
            _entities.Clear();
            _entities.AddRange(entities);
        }
    }

    // Получить все сущности
    public List<Entity> GetAll()
    {
        return _entities;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        lock (_lock)
        {
            _entities.Clear();
        }
    }
    public Entity this[int index]
    {
        get
        {
            lock (_lock)
            {
                return CollectionsMarshal.AsSpan(_entities)[index];
            }
        }
        set
        {
            lock (_lock)
            {
                CollectionsMarshal.AsSpan(_entities)[index] = value;
            }
        }
    }

}
