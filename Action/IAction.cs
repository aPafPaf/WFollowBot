using WFollowBot.Managers;

namespace WFollowBot.Action
{
    public interface IAction
    {
        public string SkillSetName { get; }
        public ActionState State { get; }
        public void Execute(PlayerContext context);
    }
}
