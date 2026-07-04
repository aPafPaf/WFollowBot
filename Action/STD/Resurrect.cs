using WFollowBot.Managers;

namespace WFollowBot.Action.STD;

public class Resurrect : IAction
{
    public ActionState State => ActionState.Resurrect;
    public string SkillSetName => "STD";

    public void Execute(PlayerContext context)
    {
        //context.JoyStick.PressButton(Xbox360Button.A, true);
        //context.ActionsController.AddTimeNextAction(1000);
    }
}
