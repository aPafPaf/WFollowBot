using WFollowBot.Managers;

namespace WFollowBot.Action.Simple;

public class Idle : IAction
{
    public ActionState State => ActionState.Idle;
    public string SkillSetName => "Simple";

    public void Execute(PlayerContext context)
    {
        context.MovementController.Stop();
    }
}