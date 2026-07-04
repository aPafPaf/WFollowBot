using WFollowBot.Managers;

namespace WFollowBot.Action.STD;
public class SmallMoving : IAction
{
    public ActionState State => ActionState.SmallMoving;
    public string SkillSetName => "STD";

    public void Execute(PlayerContext context)
    {
        var path = context.Pathfinding.CurrentPath;
        if (path.Count == 0)
        {
            context.MovementController.Stop();
            return;
        }

        var playerPos = context.PlayerInfo.PlayerGridPosition;
        var nextPoint = MovementHelper.GetLookaheadPoint(path, playerPos);
        var finalPoint = path[^1];

        context.MovementController.MoveToward(playerPos, nextPoint, finalPoint);

        if (MovementHelper.DistanceSquared(playerPos, nextPoint) < 2 * 2)
            context.Pathfinding.RemovePassedPoints(playerPos);
    }
}