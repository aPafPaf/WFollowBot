using GameHelper.Extensions;
using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using System.Drawing;

namespace WFollowBot;

public class PlayerInfo
{
    Entity Entity { get; set; }
    public Point PlayerGridPosition { get; private set; }
    public Point PlayerPrevGridPosition { get; private set; }
    private string _playerName = "";

    public void SetPlayerName(string name)
    {
        if (_playerName == name) return;
        _playerName = name;
        Entity = null;
    }

    public void Update()
    {
        if (Entity == null || !Entity.IsValid)
        {
            Entity = FindPlayerEntity();
        }

        if (Entity == null) return;

        Point playerPos = Entity.GetGridPos();
        if (PlayerGridPosition != playerPos)
        {
            PlayerPrevGridPosition = PlayerGridPosition;
            PlayerGridPosition = playerPos;
        }
    }

    private Entity FindPlayerEntity()
    {
        var areaInstance = GameHelper.Core.States.InGameStateObject.CurrentAreaInstance;
        if (areaInstance == null) return null;

        if (string.IsNullOrEmpty(_playerName))
        {
            var player = areaInstance.Player;
            if (player != null && player.IsValid)
                return player;
            return null;
        }

        foreach (var kvp in areaInstance.AwakeEntities)
        {
            var e = kvp.Value;
            if (!e.IsValid || e.EntityType != EntityTypes.Player) continue;
            // Skip entities in unusable states (consistent with TryGetLeader)
            if (e.EntityState == EntityStates.Useless ||
                e.EntityState == EntityStates.PinnacleBossHidden) continue;
            if (!e.TryGetComponent(out Player playerComp)) continue;
            if (string.IsNullOrEmpty(playerComp.Name)) continue;
            if (_playerName.Equals(playerComp.Name, System.StringComparison.OrdinalIgnoreCase))
                return e;
        }

        // Fallback: if the named player wasn't found in AwakeEntities,
        // check if areaInstance.Player matches (handles the case where
        // the local player entity isn't in the awake entities dictionary).
        var self = areaInstance.Player;
        if (self != null && self.IsValid && self.TryGetComponent(out Player selfComp) &&
            _playerName.Equals(selfComp.Name, System.StringComparison.OrdinalIgnoreCase))
            return self;

        return null;
    }

    public void ForceEntityRefresh()
    {
        Entity = null;
    }

    public Entity GetEntity() => Entity;

    public bool IsDead()
    {

        if (Entity == null || !Entity.IsValid) return false;
        if (!Entity.TryGetComponent(out GameHelper.RemoteObjects.Components.Life life)) return false;
        if(life.Health.Total == 0) return false; // broken offsets

        return !life.IsAlive;
    }
}
