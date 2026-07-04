using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Numerics;
using WFollowBot.Data;
using WFollowBot.Managers;

namespace WFollowBot.Action.STD;

public class InteractingAreaTransition : IAction
{
    public ActionState State => ActionState.InteractingAreaTransition;
    public string SkillSetName => "STD";

    private const float SearchRadius = 20f;

    public void Execute(PlayerContext context)
    {
        if (InstanceWindowIsOpen())
            return;

        var playerPos = context.PlayerInfo.PlayerGridPosition;
        var playerVec = new Vector2(playerPos.X, playerPos.Y);

        var areaInstance = GameHelper.Core.States.InGameStateObject.CurrentAreaInstance;
        if (areaInstance == null) { context.MovementController.Stop(); return; }

        Entity nearestTransition = null;
        float closestDistSq = SearchRadius * SearchRadius;

        foreach (var kvp in areaInstance.AwakeEntities)
        {
            var e = kvp.Value;
            if (!e.IsValid) continue;
            if (!e.TryGetComponent(out AreaTransition _) &&
                !e.TryGetComponent(out Transitionable _)) continue;
            if (!e.TryGetComponent(out Render render)) continue;
            if (!e.TryGetComponent(out Targetable targetable)) continue;
            if (false && !targetable.IsTargetable) continue; //TODO: offset brokern

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

        context.JoyStick.PressButtonFor(Xbox360Button.A, 0.2f, 0.08f, "InteractingAreaTransition");
        //context.Pathfinding.ClearPath();
        //context.Storages.Path.AreaTransition();
    }

    public bool InstanceWindowIsOpen() => false;
}
