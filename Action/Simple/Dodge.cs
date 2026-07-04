using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using WFollowBot.Managers;

namespace WFollowBot.Action.Simple;

public class Dodge : IAction
{
    private static readonly System.Numerics.Vector2 ZeroVec = default;
    public ActionState State => ActionState.Dodge;
    public string SkillSetName => "Simple";

    public void Execute(PlayerContext context)
    {
        var settings = context.CurrentSettings;
        if (settings == null) return;

        var playerPos = context.PlayerInfo.PlayerGridPosition;
        var playerVec = new Vector2(playerPos.X, playerPos.Y);

        float dangerRange = 6f;

        var enemies = new List<Entity>();
        context.EntityGridManager.GetNearbyEntities(playerVec, dangerRange, enemies);

        Entity threat = null;
        float closestDistSq = dangerRange * dangerRange;

        foreach (var e in enemies)
        {
            if (e.EntityType != EntityTypes.Monster) continue;
            if (!e.TryGetComponent(out Life life) || !life.IsAlive) continue;

            var ePos = GetEntityPos(e);
            float distSq = Vector2.DistanceSquared(ePos, playerVec);
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                threat = e;
            }
        }

        if (threat == null)
        {
            context.MovementController.Stop();
            return;
        }

        var threatPos = GetEntityPos(threat);
        var awayDir = playerVec - threatPos;
        if (awayDir.LengthSquared() < 0.001f)
            awayDir = new Vector2(1, 0);
        awayDir = Vector2.Normalize(awayDir);

        var perpDir = new Vector2(-awayDir.Y, awayDir.X);
        var dodgeTarget = playerVec + perpDir * 8f;

        var dodgePoint = new Point((int)dodgeTarget.X, (int)dodgeTarget.Y);
        context.MovementController.MoveToward(playerPos, dodgePoint, dodgePoint);
    }

    private static Vector2 GetEntityPos(Entity e)
    {
        if (e.TryGetComponent(out Render render))
            return new Vector2(render.GridPosition.X, render.GridPosition.Y);
        return ZeroVec;
    }
}
