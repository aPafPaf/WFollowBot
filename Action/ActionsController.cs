using System.Diagnostics;

namespace WFollowBot.Action;

public class ActionsController
{
    private int _additionalTime;
    private double _nextAction;

    public Stopwatch ActionWatch = Stopwatch.StartNew();
    public bool AllowAction => ActionWatch.Elapsed.TotalMilliseconds > _nextAction;


    public void SetNextActionTime(int time)
    {
        _nextAction = ActionWatch.Elapsed.TotalMilliseconds + time + _additionalTime;
        _additionalTime = 0;
    }

    public void AddTimeNextAction(int time)
    {
        _additionalTime = time;
    }

    public void Reset()
    {
        ActionWatch.Restart();
        _nextAction = 0;
        _additionalTime = 0;
    }
}
