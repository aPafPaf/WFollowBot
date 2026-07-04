using WFollowBot.Managers;

namespace WFollowBot.Action.STD;

public class Equipment : IAction
{
    public ActionState State => ActionState.Equipment;
    public string SkillSetName => "STD";

    public void Execute(PlayerContext context)
    {
    }
}
