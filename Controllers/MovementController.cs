using System;
using System.Drawing;
using System.Numerics;
using GameHelper;
using GameOffsets.Objects.States.InGameState;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using WFollowBot.Managers;
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
        MoveTowardScreen(from, nextWaypoint, nextWaypoint);
    }

    public void MoveToward(Point from, Point nextWaypoint, Point finalTarget)
    {
        MoveTowardScreen(from, nextWaypoint, finalTarget);
    }

    public void MoveTowardScreen(Point fromGrid, Point nextGrid, Point finalGrid)
    {
        var worldInstance = Core.States.InGameStateObject.CurrentWorldInstance;
        if (worldInstance == null) { Stop(); return; }

        var gridToWorld = TileStructure.TileToWorldConversion / TileStructure.TileToGridConversion;
        var terrain = TerrainInfo.GridHeightData;

        var fromScreen = worldInstance.WorldToScreen(
            new Vector2(fromGrid.X * gridToWorld, fromGrid.Y * gridToWorld),
            GetHeight(terrain, fromGrid));
        var nextScreen = worldInstance.WorldToScreen(
            new Vector2(nextGrid.X * gridToWorld, nextGrid.Y * gridToWorld),
            GetHeight(terrain, nextGrid));
        var finalScreen = worldInstance.WorldToScreen(
            new Vector2(finalGrid.X * gridToWorld, finalGrid.Y * gridToWorld),
            GetHeight(terrain, finalGrid));

        float dx = nextScreen.X - fromScreen.X;
        float dy = nextScreen.Y - fromScreen.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.1f)
        {
            Stop();
            return;
        }

        float normX = dx / dist;
        float normY = dy / dist;

        float finalDx = finalGrid.X - fromGrid.X;
        float finalDy = finalGrid.Y - fromGrid.Y;
        float finalDist = MathF.Sqrt(finalDx * finalDx + finalDy * finalDy);

        float speedFactor = 1.0f;
        if (finalDist < SlowDownDistance)
        {
            speedFactor = MathF.Max(0.15f, (finalDist - StopDistance) / (SlowDownDistance - StopDistance));
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
        short axisY = (short)(-normY * deflection);

        _joystick.SetAxis(Xbox360Axis.LeftThumbX, axisX);
        _joystick.SetAxis(Xbox360Axis.LeftThumbY, axisY);
    }

    public void Stop()
    {
        _joystick.SetAxis(Xbox360Axis.LeftThumbX, 0);
        _joystick.SetAxis(Xbox360Axis.LeftThumbY, 0);
    }

    private static float GetHeight(float[][] terrain, Point grid)
    {
        if (terrain.Length == 0) return 0f;
        if (grid.Y < 0 || grid.Y >= terrain.Length) return 0f;
        if (grid.X < 0 || grid.X >= terrain[0].Length) return 0f;
        return terrain[grid.Y][grid.X];
    }
}
