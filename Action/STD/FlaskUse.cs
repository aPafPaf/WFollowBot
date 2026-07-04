using GameHelper.RemoteObjects.Components;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using WFollowBot.Managers;

namespace WFollowBot.Action.STD;

public class FlaskUse : IAction
{
    public string SkillSetName => "STD";
    public ActionState State => ActionState.FlaskUse;

    public void Execute(PlayerContext context)
    {
        var settings = context.CurrentSettings;
        if (settings == null || !settings.FlaskUseEnabled)
            return;

        if (settings.SpamFlaskUseEnabled)
            return;

        var entity = context.PlayerInfo.GetEntity();
        if (entity == null || !entity.IsValid)
            return;

        if (!entity.TryGetComponent(out Life life))
            return;

        if (context.CurrentSettings.EsFlaskUseEnabled)
        {

            if (life.Health.Total > 0)
            {
                float hpPercent = (float)life.Health.Current / life.Health.Total * 100f;
                if (hpPercent < settings.FlaskHpThreshold)
                {
                    context.JoyStick.PressButtonFor(Xbox360Button.Left, 0.1f, 0.1f, "FlaskUse.HP");
                    return;
                }
            }

        }
        else
        {
            if (life.EnergyShield.Total > 0)
            {
                float hpPercent = (float)life.Health.Current / life.Health.Total * 100f;
                if (hpPercent < settings.FlaskHpThreshold)
                {
                    context.JoyStick.PressButtonFor(Xbox360Button.Left, 0.1f, 0.1f, "FlaskUse.HP");
                    return;
                }
            }
        }

        if (life.Mana.Total > 0)
        {
            float manaPercent = (float)life.Mana.Current / life.Mana.Total * 100f;
            if (manaPercent < settings.FlaskManaThreshold)
            {
                context.JoyStick.PressButtonFor(Xbox360Button.Right, 0.1f, 0.1f, "FlaskUse.MP");
            }
        }
    }
}
