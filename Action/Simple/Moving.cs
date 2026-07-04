using System.Drawing;
using WFollowBot.Managers;

namespace WFollowBot.Action.Simple;

public class Moving : IAction
{
    public ActionState State => ActionState.Moving;
    public string SkillSetName => "Simple";

    public void Execute(PlayerContext context)
    {
        var path = context.Pathfinding.CurrentPath;
        if (path.Count < 2)
        {
            context.MovementController.Stop();
            return;
        }

        var playerPos = context.PlayerInfo.PlayerGridPosition;
        var grid = TerrainInfo.ProcessedTerrainData;

        Point? nextPoint = MovementHelper.GetNextWaypointDirect(path, playerPos);

        if (!nextPoint.HasValue)
            return;

        context.MovementController.MoveToward(playerPos, nextPoint.Value);

        if (MovementHelper.DistanceSquared(playerPos, nextPoint.Value) < 2 * 2)
            context.Pathfinding.RemovePassedPoints(playerPos);
    }
}