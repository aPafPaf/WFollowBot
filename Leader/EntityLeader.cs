using GameHelper.Extensions;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using System;
using System.Drawing;

namespace WFollowBot.Leader;

public class EntityLeader : ILeader
{
    private readonly Entity _entity;
    private string _cachedName;

    public EntityLeader(Entity entity)
    {
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    public Point GridPosition => _entity.GetGridPos();

    public bool IsValid => _entity != null && _entity.IsValid;

    public string Name
    {
        get
        {
            if (_cachedName != null) return _cachedName;
            if (_entity.TryGetComponent(out Player playerComp))
                _cachedName = playerComp.Name ?? "Unknown";
            else
                _cachedName = "Entity";
            return _cachedName;
        }
    }
}
