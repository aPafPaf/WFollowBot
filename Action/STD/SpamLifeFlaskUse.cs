using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using WFollowBot.Managers;

namespace WFollowBot.Action.STD;

public class SpamFlaskUse : IAction
{
    public string SkillSetName => "STD";
    public ActionState State => ActionState.SpamFlaskUse;

    private const float MinIntervalSec = 0.5f;
    private const float RateWindowSec = 5.0f;
    private const int MaxExecutionsPerWindow = 7;
    private long _lastExecuteTimestamp;
    private readonly List<long> _executionTimestamps = new();

    public void Execute(PlayerContext context)
    {
        var settings = context.CurrentSettings;
        if (settings == null || !settings.SpamFlaskUseEnabled)
            return;

        long nowMs = Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;
        float elapsed = (nowMs - _lastExecuteTimestamp) / 1000f;
        if (elapsed < MinIntervalSec)
            return;

        _executionTimestamps.RemoveAll(t => (nowMs - t) / 1000f > RateWindowSec);

        if (_executionTimestamps.Count >= MaxExecutionsPerWindow)
            return;

        _lastExecuteTimestamp = nowMs;
        _executionTimestamps.Add(nowMs);

        float delay = (float)Random.Shared.NextSingle() * 0.2f + 0.1f;
        context.JoyStick.PressButtonFor(Xbox360Button.Left, delay, 0.3f, "SpamFlaskUse.HP");
    }
}
