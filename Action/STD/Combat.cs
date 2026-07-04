using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using WFollowBot.Managers;
using WFollowBot.Settings;

namespace WFollowBot.Action.STD;

public class Combat : IAction
{
    private static readonly System.Numerics.Vector2 ZeroVec = default;
    public ActionState State => ActionState.Combat;
    public string SkillSetName => "STD";

    public void Execute(PlayerContext context)
    {
        var settings = context.CurrentSettings;
        if (settings == null) return;

        var playerPos = context.PlayerInfo.PlayerGridPosition;
        var playerVec = new Vector2(playerPos.X, playerPos.Y);

        float detectRange = settings.CombatRange;
        float attackRange = settings.AttackRange;

        var enemies = new List<Entity>();
        context.EntityGridManager.GetNearbyEntities(playerVec, detectRange, enemies);

        Entity target = null;
        float closestDistSq = detectRange * detectRange;
        float lowestHpPercent = float.MaxValue;

        foreach (var e in enemies)
        {
            if (e.EntityType != EntityTypes.Monster) continue;
            if (!e.TryGetComponent(out Life life) || !life.IsAlive) continue;

            var ePos = GetEntityPos(e);
            float distSq = Vector2.DistanceSquared(ePos, playerVec);
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                target = e;
            }

            float hpPercent = life.Health.Total > 0 ? life.Health.Current / (float)life.Health.Total : 1f;
            if (hpPercent < lowestHpPercent)
                lowestHpPercent = hpPercent;
        }

        if (target == null)
        {
            context.MovementController.Stop();
            return;
        }

        float distToTarget = (float)Math.Sqrt(closestDistSq);

        if (settings.Style == CombatStyle.MoveOnly)
        {
            if (distToTarget > attackRange)
            {
                var targetPoint = GetEntityPoint(target);
                context.MovementController.MoveToward(playerPos, targetPoint, targetPoint);
            }
            else
            {
                context.MovementController.Stop();
            }
            return;
        }

        // delegate to Attack action from the active skill set (e.g. Simple/Attack)
        if (distToTarget < attackRange)
        {
            if (context.ActionProvider.TryGetAction(ActionState.Attack, out var attackAction))
                attackAction.Execute(context);
        }
        else
        {
            var targetPoint = GetEntityPoint(target);
            context.MovementController.MoveToward(playerPos, targetPoint, targetPoint);
        }

        // delegate to Dodge action from the active skill set (e.g. Simple/Dodge)
        if (context.ActionProvider.TryGetAction(ActionState.Dodge, out var dodgeAction))
            dodgeAction.Execute(context);
    }

    private static Vector2 GetEntityPos(Entity e)
    {
        if (e.TryGetComponent(out Render render))
            return new Vector2(render.GridPosition.X, render.GridPosition.Y);
        return ZeroVec;
    }

    private static Point GetEntityPoint(Entity e)
    {
        if (e.TryGetComponent(out Render render))
            return new Point((int)render.GridPosition.X, (int)render.GridPosition.Y);
        return Point.Empty;
    }
}
