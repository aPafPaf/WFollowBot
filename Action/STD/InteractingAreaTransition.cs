using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Drawing;
using System.Numerics;
using WFollowBot.Managers;

namespace WFollowBot.Action.STD;

public class InteractingAreaTransition : IAction
{
    public ActionState State => ActionState.InteractingAreaTransition;
    public string SkillSetName => "STD";

    public void Execute(PlayerContext context)
    {
        var playerPos = context.PlayerInfo.PlayerGridPosition;
        var playerVec = new Vector2(playerPos.X, playerPos.Y);

        var areaInstance = GameHelper.Core.States.InGameStateObject.CurrentAreaInstance;
        if (areaInstance == null) { context.MovementController.Stop(); return; }

        Entity nearestTransition = null;
        float closestDistSq = 20f * 20f;

        foreach (var kvp in areaInstance.AwakeEntities)
        {
            var e = kvp.Value;
            if (!e.IsValid) continue;
            if (!e.TryGetComponent(out Transitionable _)) continue;
            if (!e.TryGetComponent(out Render render)) continue;

            float dx = render.GridPosition.X - playerVec.X;
            float dy = render.GridPosition.Y - playerVec.Y;
            float distSq = dx * dx + dy * dy;
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                nearestTransition = e;
            }
        }

        if (nearestTransition == null)
        {
            context.MovementController.Stop();
            return;
        }

        float dist = (float)Math.Sqrt(closestDistSq);
        if (dist < 4f)
        {
            context.JoyStick.PressButton(Xbox360Button.A, true);
            context.Pathfinding.ClearPath();
            context.Storages.Path.AreaTransition();
        }
        else
        {
            context.JoyStick.PressButton(Xbox360Button.A, false);
            if (nearestTransition.TryGetComponent(out Render targetRender))
            {
                var targetPoint = new Point(
                    (int)targetRender.GridPosition.X,
                    (int)targetRender.GridPosition.Y);
                context.MovementController.MoveToward(playerPos, targetPoint, targetPoint);
            }
        }
    }
}
