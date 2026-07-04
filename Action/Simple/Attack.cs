using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using WFollowBot.Managers;

namespace WFollowBot.Action.Simple;

public class Attack : IAction
{
    private static readonly System.Numerics.Vector2 ZeroVec = default;
    public ActionState State => ActionState.Attack;
    public string SkillSetName => "Simple";

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
        }

        if (target == null)
        {
            context.MovementController.Stop();
            return;
        }

        float distToTarget = (float)Math.Sqrt(closestDistSq);

        string skillName = settings.CombatSkillName;
        Xbox360Button attackButton = settings.AttackButton;

        if (!string.IsNullOrEmpty(skillName))
        {
            var playerEntity = context.PlayerInfo.GetEntity();
            if (playerEntity != null && playerEntity.TryGetComponent(out Actor actor))
            {
                if (actor.ActiveSkills.ContainsKey(skillName))
                {
                    if (!actor.IsSkillUsable.Contains(skillName))
                    {
                        context.MovementController.Stop();
                        return;
                    }
                }
            }
        }

        if (distToTarget < attackRange)
        {
            FaceTarget(context, playerPos, target);
            context.JoyStick.PressButton(attackButton, true);
        }
        //else
        //{
        //    context.JoyStick.PressButton(attackButton, false);
        //    var targetPoint = GetEntityPoint(target);
        //    context.MovementController.MoveToward(playerPos, targetPoint, targetPoint);
        //}
    }

    private static void FaceTarget(PlayerContext context, Point playerPos, Entity target)
    {
        var targetPos = GetEntityPos(target);
        float dx = targetPos.X - playerPos.X;
        float dy = targetPos.Y - playerPos.Y;

        float temp = dx;
        dx = dy;
        dy = temp;

        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.01f) return;

        float normX = dx / dist;
        float normY = dy / dist;

        context.JoyStick.SetAxis(Xbox360Axis.RightThumbX, (short)(normX * 32767));
        context.JoyStick.SetAxis(Xbox360Axis.RightThumbY, (short)(-normY * 32767));
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
