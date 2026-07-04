using System;
using System.Numerics;
using GameHelper;
using GameHelper.RemoteObjects.Components;
using GameOffsets.Objects.States.InGameState;
using ImGuiNET;

namespace WFollowBot;

public static class ScreenToWorldHelper
{
    private static readonly float GridToWorld = TileStructure.TileToWorldConversion / TileStructure.TileToGridConversion;

    public static Vector2? MouseToWorld()
    {
        if (ScreenToWorldAndGrid(ImGui.GetMousePos(), out var w, out _, out _))
            return w;
        return null;
    }

    public static Vector2? MouseToGrid()
    {
        if (ScreenToWorldAndGrid(ImGui.GetMousePos(), out _, out var g, out _))
            return g;
        return null;
    }

    public static bool ScreenToWorldAndGrid(Vector2 screenPos, out Vector2 worldPos, out Vector2 gridPos, out float height)
    {
        worldPos = default;
        gridPos = default;
        height = 0;

        var worldInstance = Core.States.InGameStateObject.CurrentWorldInstance;
        if (worldInstance == null || worldInstance.Address == IntPtr.Zero)
            return false;

        var areaInstance = Core.States.InGameStateObject.CurrentAreaInstance;
        if (areaInstance == null || areaInstance.GridHeightData == null || areaInstance.GridHeightData.Length == 0)
            return false;

        var window = Core.Process.WindowArea;
        if (window.Width <= 0 || window.Height <= 0)
            return false;

        var player = areaInstance.Player;
        float wx, wy, h;
        if (player?.TryGetComponent<Render>(out var render) == true)
        {
            wx = render.WorldPosition.X;
            wy = render.WorldPosition.Y;
            h = render.TerrainHeight;
        }
        else if (areaInstance.GridHeightData.Length > 0 && areaInstance.GridHeightData[0]?.Length > 0)
        {
            var cols = areaInstance.GridHeightData[0]!.Length;
            wx = cols / 2f * GridToWorld;
            wy = areaInstance.GridHeightData.Length / 2f * GridToWorld;
            h = areaInstance.GridHeightData[areaInstance.GridHeightData.Length / 2][cols / 2];
        }
        else
        {
            return false;
        }

        for (var iter = 0; iter < 20; iter++)
        {
            var currentScreen = worldInstance.WorldToScreen(new Vector2(wx, wy), h);
            var errorX = screenPos.X - currentScreen.X;
            var errorY = screenPos.Y - currentScreen.Y;

            if (errorX * errorX + errorY * errorY < 0.5f)
            {
                height = h;
                worldPos = new Vector2(wx, wy);
                gridPos = new Vector2(wx / GridToWorld, wy / GridToWorld);
                return true;
            }

            var eps = 0.5f;
            var screenXeps = worldInstance.WorldToScreen(new Vector2(wx + eps, wy), h);
            var screenYeps = worldInstance.WorldToScreen(new Vector2(wx, wy + eps), h);

            var Jxx = (screenXeps.X - currentScreen.X) / eps;
            var Jxy = (screenYeps.X - currentScreen.X) / eps;
            var Jyx = (screenXeps.Y - currentScreen.Y) / eps;
            var Jyy = (screenYeps.Y - currentScreen.Y) / eps;

            var det = Jxx * Jyy - Jxy * Jyx;
            if (Math.Abs(det) < 1e-10f)
                break;

            var invDet = 1f / det;
            var dx = (Jyy * errorX - Jxy * errorY) * invDet;
            var dy = (-Jyx * errorX + Jxx * errorY) * invDet;

            var maxStep = 500f;
            if (Math.Abs(dx) > maxStep) dx = Math.Sign(dx) * maxStep;
            if (Math.Abs(dy) > maxStep) dy = Math.Sign(dy) * maxStep;

            wx += dx;
            wy += dy;

            var gx = (int)Math.Round(wx / GridToWorld);
            var gy = (int)Math.Round(wy / GridToWorld);
            if (gy >= 0 && gy < areaInstance.GridHeightData.Length &&
                gx >= 0 && gx < areaInstance.GridHeightData[0].Length)
                h = areaInstance.GridHeightData[gy][gx];
        }

        height = h;
        worldPos = new Vector2(wx, wy);
        gridPos = new Vector2(wx / GridToWorld, wy / GridToWorld);
        return false;
    }
}
