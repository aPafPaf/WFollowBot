using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Drawing;
using WFollowBotCore;

namespace WFollowBot.Controllers;

public class MovementController
{
    private readonly JoyStick _joystick;

    public float MaxDeflection { get; set; } = 32767f;
    public float MinDeflection { get; set; } = 32767f;
    public float StopDistance { get; set; } = 3f;
    public float SlowDownDistance { get; set; } = 8f;

    public MovementController(JoyStick joystick)
    {
        _joystick = joystick;
    }

    public void MoveToward(Point from, Point nextWaypoint)
    {
        float dx = nextWaypoint.X - from.X;
        float dy = nextWaypoint.Y - from.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.1f)
        {
            Stop();
            return;
        }

        float normX = dx / dist;
        float normY = dy / dist;

        float speedFactor = 1.0f;
        if (dist < SlowDownDistance)
        {
            speedFactor = MathF.Max(0.15f, (dist - StopDistance) / (SlowDownDistance - StopDistance));
        }

        if (dist <= StopDistance)
        {
            Stop();
            return;
        }

        float rawDeflection = MaxDeflection * speedFactor;
        if (rawDeflection > 0 && rawDeflection < MinDeflection)
            rawDeflection = MinDeflection;

        short deflection = (short)rawDeflection;
        short axisX = (short)(normX * deflection);
        short axisY = (short)(normY * deflection);

        _joystick.SetAxis(Xbox360Axis.LeftThumbX, axisX);
        _joystick.SetAxis(Xbox360Axis.LeftThumbY, axisY);
    }

    public void MoveToward(Point from, Point nextWaypoint, Point finalTarget)
    {
        float dx = nextWaypoint.X - from.X;
        float dy = nextWaypoint.Y - from.Y;

        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < 0.1f)
        {
            Stop();
            return;
        }

        float normX = dx / dist;
        float normY = dy / dist;

        float finalDist = MathF.Sqrt(
            MathF.Pow(finalTarget.X - from.X, 2) +
            MathF.Pow(finalTarget.Y - from.Y, 2));

        float controlDist = MathF.Max(dist, finalDist);
        float speedFactor = 1.0f;
        if (controlDist < SlowDownDistance)
        {
            speedFactor = MathF.Max(0.15f, (controlDist - StopDistance) / (SlowDownDistance - StopDistance));
        }

        if (finalDist <= StopDistance)
        {
            Stop();
            return;
        }

        float rawDeflection = MaxDeflection * speedFactor;
        if (rawDeflection > 0 && rawDeflection < MinDeflection)
            rawDeflection = MinDeflection;
        short deflection = (short)rawDeflection;

        short axisX = (short)(normX * deflection);
        short axisY = (short)(normY * deflection);

        _joystick.SetAxis(Xbox360Axis.LeftThumbX, axisX);
        _joystick.SetAxis(Xbox360Axis.LeftThumbY, axisY);
    }

    public void Stop()
    {
        _joystick.SetAxis(Xbox360Axis.LeftThumbX, 0);
        _joystick.SetAxis(Xbox360Axis.LeftThumbY, 0);
    }
}