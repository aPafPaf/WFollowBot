using GameHelper;
using GameHelper.Extensions;
using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using GameHelper.Utils;
using GameOffsets.Objects.States.InGameState;
using ImGuiNET;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WFollowBot.Action;
using WFollowBot.Leader;
using WFollowBot.Managers;
using WFollowBot.Settings;
using WFollowBotCore;

namespace WFollowBot
{
    public sealed partial class WFollowBotCore
    {
        System.Drawing.Point lastPathStart = new(-1, -1);
        System.Drawing.Point lastPathEnd = new(-1, -1);
        List<System.Drawing.Point> cachedPath = new();

        private static readonly Vector4 HeldColor     = new(0.40f, 1.00f, 0.40f, 1.0f);
        private static readonly Vector4 PendingColor  = new(1.00f, 0.85f, 0.20f, 1.0f);
        private static readonly Vector4 CooldownColor = new(0.30f, 0.85f, 1.00f, 1.0f);
        private static readonly Vector4 IdleColor     = new(0.55f, 0.55f, 0.55f, 1.0f);

        private enum ChipMode { Pressed, Pending, Cooldown }

        public void DrawRender()
        {
            DrawTerrainOverlay();
            DrawFollowerPaths();
            DrawTerrainInfoWindow();
            DrawCursorInfo();
            DrawButtonStateWindow();
        }

        private void DrawCursorInfo()
        {
            if (!this.Settings.CursorInfo)
                return;

            var mouseWorld = ScreenToWorldHelper.MouseToWorld();
            var mouseGrid = ScreenToWorldHelper.MouseToGrid();
            if (mouseGrid == null)
                return;

            ImGui.SetNextWindowBgAlpha(0.85f);
            if (ImGui.Begin("Cursor Info", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.Text($"Grid: {mouseGrid.Value.X:F1}, {mouseGrid.Value.Y:F1}");
                if (mouseWorld.HasValue)
                    ImGui.Text($"World: {mouseWorld.Value.X:F1}, {mouseWorld.Value.Y:F1}");

                var player = Core.States.InGameStateObject.CurrentAreaInstance?.Player;
                if (player != null && player.TryGetComponent<Render>(out var render))
                {
                    var startGrid = new System.Drawing.Point((int)render.GridPosition.X, (int)render.GridPosition.Y);
                    var endGrid = new System.Drawing.Point((int)mouseGrid.Value.X, (int)mouseGrid.Value.Y);

                    if (startGrid != lastPathStart || endGrid != lastPathEnd)
                    {
                        lastPathStart = startGrid;
                        lastPathEnd = endGrid;
                        cachedPath = PathFinder.Pathfinder.FindPathList(
                            TerrainInfo.ProcessedTerrainData, startGrid, endGrid);
                    }

                    ImGui.Separator();
                    if (cachedPath.Count > 0)
                    {
                        ImGui.Text($"Path: {cachedPath.Count} nodes");
                        var first = cachedPath[0];
                        var last = cachedPath[^1];
                        ImGui.Text($"From: ({first.X}, {first.Y}) -> ({last.X}, {last.Y})");
                    }
                    else
                    {
                        ImGui.Text("Path: not found");
                    }
                }

                ImGui.End();
            }
        }

        private void DrawButtonStateWindow()
        {
            if (!this.Settings.ShowButtonStateWindow) return;

            // Pin a sensible default position the first time the window is
            // shown (top-right of the game overlay). Subsequent moves by the
            // user are remembered by ImGui per-window-ID, so this is a
            // one-shot hint, not an every-frame override.
            var displaySize = ImGui.GetIO().DisplaySize;
            ImGui.SetNextWindowPos(
                new Vector2(Math.Max(20f, displaySize.X - 360f), 60f),
                ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowBgAlpha(0.9f);
            if (ImGui.Begin("Button State", ref this.Settings.ShowButtonStateWindow,
                ImGuiWindowFlags.AlwaysAutoResize))
            {
                var followers = new[]
                {
                    (name: "Player1", ctx: Player1),
                    (name: "Player2", ctx: Player2),
                };

                for (int i = 0; i < followers.Length; i++)
                {
                    var (name, ctx) = followers[i];
                    if (ctx == null) continue;
                    ImGui.PushID(i);

                    if (ImGui.CollapsingHeader($"{name}  ({(ctx.Enabled ? "ON" : "OFF")})",
                        ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        var js = ctx.JoyStick;
                        ImGui.TextDisabled($"Controller: {(js.IsRunning ? "running" : "stopped")}");
                        ImGui.Separator();

                        var held = js.GetActiveButtons();
                        ImGui.Text($"Pressed ({held.Count}):");
                        DrawButtonChips(held, ChipMode.Pressed, js: js);

                        ImGui.Separator();

                        var pending = js.GetPendingHoldButtons();
                        ImGui.Text($"Auto-Release Pending ({pending.Count}):");
                        DrawButtonChips(pending, ChipMode.Pending, js: js);

                        ImGui.Separator();

                        var cooldown = js.GetCooldownButtons();
                        ImGui.Text($"Cooldown ({cooldown.Count}):");
                        DrawButtonChips(cooldown, ChipMode.Cooldown, js: js);

                        ImGui.Separator();
                        ImGui.TextDisabled("Axes / Triggers:");
                        ImGui.TextDisabled(
                            $"  L: ({js.GetAxis(Xbox360Axis.LeftThumbX)}, {js.GetAxis(Xbox360Axis.LeftThumbY)})" +
                            $"  R: ({js.GetAxis(Xbox360Axis.RightThumbX)}, {js.GetAxis(Xbox360Axis.RightThumbY)})");
                        ImGui.TextDisabled(
                            $"  LT: {js.GetSlider(Xbox360Slider.LeftTrigger)}" +
                            $"  RT: {js.GetSlider(Xbox360Slider.RightTrigger)}");
                    }

                    ImGui.PopID();
                }
            }
            ImGui.End();
        }

        /// <summary>
        /// Inline live summary of button state, drawn in the settings panel
        /// itself so the user gets immediate feedback even if the game
        /// overlay window is hidden behind something or off-screen.
        /// </summary>
        private void DrawButtonStateSummary()
        {
            var followers = new[]
            {
                (name: "P1", ctx: Player1),
                (name: "P2", ctx: Player2),
            };

            for (int i = 0; i < followers.Length; i++)
            {
                var (name, ctx) = followers[i];
                if (ctx == null) continue;
                ImGui.PushID($"btnsummary_{i}");

                var js = ctx.JoyStick;
                var held     = js.GetActiveButtons();
                var pending  = js.GetPendingHoldButtons();
                var cooldown = js.GetCooldownButtons();
                if (held.Count == 0 && pending.Count == 0 && cooldown.Count == 0)
                {
                    ImGui.TextDisabled($"{name}: (idle)");
                }
                else
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"{name}: held=[");
                    sb.Append(string.Join(",", held.Select(JoyStick.FormatButtonLabel)));
                    sb.Append("]");

                    if (pending.Count > 0)
                    {
                        sb.Append("  pending=[");
                        sb.Append(string.Join(",", pending.Select(JoyStick.FormatButtonLabel)));
                        sb.Append("]");
                    }

                    if (cooldown.Count > 0)
                    {
                        sb.Append("  cd=[");
                        var first = true;
                        foreach (var btn in cooldown.OrderBy(b => b.ToString()))
                        {
                            if (!first) sb.Append(",");
                            sb.Append($"{JoyStick.FormatButtonLabel(btn)}");
                            var rem = js.GetCooldownRemainingSeconds(btn);
                            if (rem > 0.0) sb.Append($" {(int)Math.Ceiling(rem * 1000.0)}ms");
                            var src = js.GetLastPressSource(btn);
                            if (!string.IsNullOrEmpty(src))
                            {
                                var disp = src.Length > 12 ? src.Substring(0, 12) + "…" : src;
                                sb.Append($" ← {disp}");
                            }
                            first = false;
                        }
                        sb.Append("]");
                    }

                    ImGui.TextDisabled(sb.ToString());
                    if (ImGui.IsItemHovered())
                    {
                        var tooltip = new System.Text.StringBuilder();
                        tooltip.AppendLine("Last press source per button:");
                        foreach (var btn in held.Concat(pending).Concat(cooldown).Distinct()
                                     .OrderBy(b => b.ToString()))
                        {
                            var src = js.GetLastPressSource(btn);
                            tooltip.AppendLine($"  [{JoyStick.FormatButtonLabel(btn)}] {src ?? "(none)"}");
                        }
                        ImGui.SetTooltip(tooltip.ToString());
                    }
                }

                ImGui.PopID();
            }
        }

        private static void DrawButtonChips(IEnumerable<Xbox360Button> buttons, ChipMode mode, JoyStick js)
        {
            var ordered = buttons.OrderBy(b => b.ToString()).ToList();
            if (ordered.Count == 0)
            {
                ImGui.TextColored(IdleColor, "    (none)");
                return;
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                if (i > 0) ImGui.SameLine();
                var btn = ordered[i];
                var label = JoyStick.FormatButtonLabel(btn);
                switch (mode)
                {
                    case ChipMode.Pending:
                    {
                        double age = js.GetHoldAgeSeconds(btn);
                        var ageStr = age >= 0 ? $" {age:F2}s" : "";
                        ImGui.TextColored(PendingColor, $"[{label}]{ageStr}");
                        if (ImGui.IsItemHovered())
                        {
                            var src = js.GetLastPressSource(btn);
                            if (!string.IsNullOrEmpty(src))
                                ImGui.SetTooltip($"source: {src}");
                        }
                        break;
                    }
                    case ChipMode.Cooldown:
                    {
                        var rem = js.GetCooldownRemainingSeconds(btn);
                        var remStr = rem > 0.0 ? $" {(int)Math.Ceiling(rem * 1000.0)}ms" : "";
                        ImGui.TextColored(CooldownColor, $"[{label}]{remStr}");
                        if (ImGui.IsItemHovered())
                        {
                            var src = js.GetLastPressSource(btn);
                            if (!string.IsNullOrEmpty(src))
                                ImGui.SetTooltip($"source: {src}");
                        }
                        break;
                    }
                    default:
                    {
                        ImGui.TextColored(HeldColor, $"[{label}]");
                        if (ImGui.IsItemHovered())
                        {
                            var src = js.GetLastPressSource(btn);
                            if (!string.IsNullOrEmpty(src))
                                ImGui.SetTooltip($"source: {src}");
                        }
                        break;
                    }
                }
            }
        }

        private void DrawTerrainInfoWindow()
        {
            if (!this.Settings.ShowTerrainInfoWindow)
                return;

            ImGui.SetNextWindowBgAlpha(0.9f);
            if (ImGui.Begin("TerrainInfo Debug", ref this.Settings.ShowTerrainInfoWindow))
            {
                var dataRows = TerrainInfo.GridHeightData.Length;
                var dataCols = dataRows > 0 ? TerrainInfo.GridHeightData[0].Length : 0;
                var walkDataLen = TerrainInfo.GridWalkableData.Length;
                var processedRows = TerrainInfo.ProcessedTerrainData.Length;
                var processedCols = processedRows > 0 ? TerrainInfo.ProcessedTerrainData[0].Length : 0;
                var totalCells = TerrainInfo.TotalCellCount();
                var walkableCells = TerrainInfo.WalkableCellCount();

                ImGui.Text($"GridHeightData: {dataRows} x {dataCols}");
                ImGui.Text($"GridWalkableData: {walkDataLen} bytes");
                ImGui.Text($"BytesPerRow: {TerrainInfo.BytesPerRow}");
                ImGui.Text($"ProcessedTerrainData: {processedRows} x {processedCols}");
                ImGui.Text($"Total cells: {totalCells}");
                ImGui.Text($"Walkable: {walkableCells}");
                ImGui.Text($"Non-walkable: {totalCells - walkableCells}");
                ImGui.Separator();
                ImGui.Text($"IsBorder(0,0): {TerrainInfo.IsBorder(0, 0)}");

                if (ImGui.CollapsingHeader("Grid Sample (first 5x5)"))
                {
                    if (ImGui.BeginTable("grid_sample", 6, ImGuiTableFlags.Borders))
                    {
                        ImGui.TableSetupColumn("y\\x");
                        for (var x = 0; x < 5; x++)
                            ImGui.TableSetupColumn($"{x}");
                        ImGui.TableHeadersRow();

                        for (var y = 0; y < 5 && y < processedRows; y++)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text($"{y}");
                            for (var x = 0; x < 5 && x < processedCols; x++)
                            {
                                ImGui.TableSetColumnIndex(x + 1);
                                var val = TerrainInfo.ProcessedTerrainData[y][x];
                                ImGui.Text($"{val}");
                            }
                        }

                        ImGui.EndTable();
                    }
                }
            }

            ImGui.End();
        }

        private void DrawTerrainOverlay()
        {
            if (!this.Settings.RenderTerrain)
                return;

            if (TerrainInfo.ProcessedTerrainData.Length == 0)
                return;

            var player = Core.States.InGameStateObject.CurrentAreaInstance?.Player;
            if (player == null || !player.TryGetComponent<Render>(out var render))
                return;

            var radius = this.Settings.TerrainViewRadius;
            var gridToWorld = TileStructure.TileToWorldConversion / TileStructure.TileToGridConversion;
            var gX = render.GridPosition.X;
            var gY = render.GridPosition.Y;
            var cellSize = new Vector2(5, 5);

            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var gridX = (int)(gX + dx);
                    var gridY = (int)(gY + dy);
                    if (gridY < 0 || gridY >= TerrainInfo.ProcessedTerrainData.Length ||
                        gridX < 0 || gridX >= TerrainInfo.ProcessedTerrainData[0].Length)
                        continue;

                    var walkVal = TerrainInfo.ProcessedTerrainData[gridY][gridX];

                    float height = 0;
                    if (gridY < TerrainInfo.GridHeightData.Length &&
                        gridX < TerrainInfo.GridHeightData[0].Length)
                        height = TerrainInfo.GridHeightData[gridY][gridX];

                    var screenPos = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
                        new Vector2(gridX * gridToWorld, gridY * gridToWorld), height);

                    var color = walkVal == 0
                        ? ImGuiHelper.Color(255, 0, 0, 120)
                        : ImGuiHelper.Color(0, 255, 0, 120);

                    ImGui.GetForegroundDrawList().AddRectFilled(screenPos, screenPos + cellSize, color);
                }
            }

            // Draw player cell in blue
            {
                float height = 0;
                var pgX = (int)gX;
                var pgY = (int)gY;
                if (pgY < TerrainInfo.GridHeightData.Length &&
                    pgX < TerrainInfo.GridHeightData[0].Length)
                    height = TerrainInfo.GridHeightData[pgY][pgX];

                var playerScreen = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
                    new Vector2(pgX * gridToWorld, pgY * gridToWorld), height);
                ImGui.GetForegroundDrawList().AddRectFilled(
                    playerScreen, playerScreen + cellSize * 2,
                    ImGuiHelper.Color(0, 120, 255, 200));
            }

            // Draw cached path
            if (cachedPath.Count > 0)
            {
                for (var i = 1; i < cachedPath.Count; i++)
                {
                    float h0 = 0, h1 = 0;
                    var p0Y = cachedPath[i - 1].Y;
                    var p0X = cachedPath[i - 1].X;
                    var p1Y = cachedPath[i].Y;
                    var p1X = cachedPath[i].X;
                    if (p0Y < TerrainInfo.GridHeightData.Length && p0X < TerrainInfo.GridHeightData[0].Length)
                        h0 = TerrainInfo.GridHeightData[p0Y][p0X];
                    if (p1Y < TerrainInfo.GridHeightData.Length && p1X < TerrainInfo.GridHeightData[0].Length)
                        h1 = TerrainInfo.GridHeightData[p1Y][p1X];

                    var screen0 = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
                        new Vector2(p0X * gridToWorld, p0Y * gridToWorld), h0);
                    var screen1 = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
                        new Vector2(p1X * gridToWorld, p1Y * gridToWorld), h1);

                    ImGui.GetForegroundDrawList().AddLine(screen0, screen1,
                        ImGuiHelper.Color(255, 255, 0, 220), 3f);
                }
            }

            // Slider window
            ImGui.Begin("Terrain View");
            ImGui.SliderInt("Radius", ref this.Settings.TerrainViewRadius, 1, 150);
            ImGui.Text($"Walkable: {GetCellCount(radius, gX, gY, true)}, Not walkable: {GetCellCount(radius, gX, gY, false)}");
            ImGui.End();
        }

        private void DrawFollowerPaths()
        {
            if (Player1 == null && Player2 == null) return;

            var worldInstance = Core.States.InGameStateObject.CurrentWorldInstance;
            if (worldInstance == null) return;

            var terrain = TerrainInfo.GridHeightData;
            if (terrain.Length == 0) return;

            var gridToWorld = TileStructure.TileToWorldConversion / TileStructure.TileToGridConversion;

            foreach (var ctx in new[] { Player1, Player2 })
            {
                if (ctx == null || !ctx.Enabled) continue;

                // Draw HoldRadius circle around leader
                if (ctx.Leader != null && ctx.Leader.IsValid)
                {
                    var leaderPos = ctx.Leader.GridPosition;

                    float lh = 0;
                    if (leaderPos.Y < terrain.Length && leaderPos.X < terrain[0].Length)
                        lh = terrain[leaderPos.Y][leaderPos.X];

                    var leaderScreen = worldInstance.WorldToScreen(
                        new Vector2(leaderPos.X * gridToWorld, leaderPos.Y * gridToWorld), lh);

                    var hr = ctx.FollowController.HoldRadius;
                    var rightScreen = worldInstance.WorldToScreen(
                        new Vector2((leaderPos.X + hr) * gridToWorld, leaderPos.Y * gridToWorld), lh);

                    float pixelRadius = MathF.Abs(rightScreen.X - leaderScreen.X);

                    ImGui.GetForegroundDrawList().AddCircle(leaderScreen, pixelRadius,
                        ImGuiHelper.Color(0, 255, 200, 80), 0, 2f);
                    ImGui.GetForegroundDrawList().AddCircle(leaderScreen, pixelRadius - 2f,
                        ImGuiHelper.Color(0, 255, 200, 20), 0, 8f);

                    var namePos = leaderScreen;
                    namePos.Y -= pixelRadius + 18;
                    ImGui.GetForegroundDrawList().AddText(namePos,
                        ImGuiHelper.Color(0, 255, 200, 255),
                        $"[{ctx.Leader.Name}] HR={hr:F0}");
                }

                var path = ctx.Pathfinding.CurrentPath.ToList();
                if (path == null || path.Count < 2) continue;

                for (var i = 1; i < path.Count; i++)
                {
                    var p0 = path[i - 1];
                    var p1 = path[i];
                    float h0 = 0, h1 = 0;
                    if (p0.Y < terrain.Length && p0.X < terrain[0].Length) h0 = terrain[p0.Y][p0.X];
                    if (p1.Y < terrain.Length && p1.X < terrain[0].Length) h1 = terrain[p1.Y][p1.X];

                    var screen0 = worldInstance.WorldToScreen(
                        new Vector2(p0.X * gridToWorld, p0.Y * gridToWorld), h0);
                    var screen1 = worldInstance.WorldToScreen(
                        new Vector2(p1.X * gridToWorld, p1.Y * gridToWorld), h1);

                    var pathColor = ImGuiHelper.Color(0, 255, 0, 180);
                    ImGui.GetForegroundDrawList().AddLine(screen0, screen1, pathColor, 2.5f);
                }

                // Draw next waypoint marker (lookahead point the bot is actually targeting)
                var playerPos = ctx.PlayerInfo.PlayerGridPosition;
                var nextWp = MovementHelper.GetLookaheadPoint(path, playerPos);
                float hp = 0;
                if (nextWp.Y < terrain.Length && nextWp.X < terrain[0].Length)
                    hp = terrain[nextWp.Y][nextWp.X];
                var wpScreen = worldInstance.WorldToScreen(
                    new Vector2(nextWp.X * gridToWorld, nextWp.Y * gridToWorld), hp);
                ImGui.GetForegroundDrawList().AddCircleFilled(wpScreen, 5f,
                    ImGuiHelper.Color(0, 255, 100, 220));

                // Draw joystick direction lines (left = movement, right = aim)
                float ph = 0f;
                if (playerPos.Y >= 0 && playerPos.Y < terrain.Length &&
                    playerPos.X >= 0 && playerPos.X < terrain[0].Length)
                    ph = terrain[playerPos.Y][playerPos.X];
                var playerScreen2 = worldInstance.WorldToScreen(
                    new Vector2(playerPos.X * gridToWorld, playerPos.Y * gridToWorld), ph);

                const float stickScale = 60f / 32767f;
                const float stickThreshold = 0.05f * 32767f;

                short lx = ctx.JoyStick.GetAxis(Xbox360Axis.LeftThumbX);
                short ly = ctx.JoyStick.GetAxis(Xbox360Axis.LeftThumbY);
                if (MathF.Abs(lx) > stickThreshold || MathF.Abs(ly) > stickThreshold)
                {
                    var leftEnd = new Vector2(
                        playerScreen2.X + lx * stickScale,
                        playerScreen2.Y + ly * stickScale);
                    ImGui.GetForegroundDrawList().AddLine(playerScreen2, leftEnd,
                        ImGuiHelper.Color(100, 150, 255, 220), 2.5f);
                    ImGui.GetForegroundDrawList().AddCircleFilled(leftEnd, 4f,
                        ImGuiHelper.Color(100, 150, 255, 220));
                }

                short rx = ctx.JoyStick.GetAxis(Xbox360Axis.RightThumbX);
                short ry = ctx.JoyStick.GetAxis(Xbox360Axis.RightThumbY);
                if (MathF.Abs(rx) > stickThreshold || MathF.Abs(ry) > stickThreshold)
                {
                    var rightEnd = new Vector2(
                        playerScreen2.X + rx * stickScale,
                        playerScreen2.Y + ry * stickScale);
                    ImGui.GetForegroundDrawList().AddLine(playerScreen2, rightEnd,
                        ImGuiHelper.Color(255, 100, 100, 220), 2.5f);
                    ImGui.GetForegroundDrawList().AddCircleFilled(rightEnd, 4f,
                        ImGuiHelper.Color(255, 100, 100, 220));
                }

                // Draw transition target if in transition state
                if (ctx.StateManager.CurrentState == ActionState.InteractingAreaTransition)
                {
                    var areaInst = Core.States.InGameStateObject.CurrentAreaInstance;
                    if (areaInst != null)
                    {
                        Entity nearestTransition = null;
                        float nearestDistSq = float.MaxValue;
                        foreach (var kvp in areaInst.AwakeEntities)
                        {
                            var e = kvp.Value;
                            if (!e.IsValid || !e.TryGetComponent(out Transitionable _)) continue;
                            if (!e.TryGetComponent(out Render r)) continue;
                            var playerPos1 = ctx.PlayerInfo.PlayerGridPosition;
                            float dx = r.GridPosition.X - playerPos1.X;
                            float dy = r.GridPosition.Y - playerPos1.Y;
                            float d = dx * dx + dy * dy;
                            if (d < nearestDistSq) { nearestDistSq = d; nearestTransition = e; }
                        }
                        if (nearestTransition != null && nearestTransition.TryGetComponent(out Render tr1))
                        {
                            float th = 0;
                            int tgy = (int)tr1.GridPosition.Y, tgx = (int)tr1.GridPosition.X;
                            if (tgy < terrain.Length && tgx < terrain[0].Length) th = terrain[tgy][tgx];
                            var transScreen = worldInstance.WorldToScreen(
                                new Vector2(tgx * gridToWorld, tgy * gridToWorld), th);
                            ImGui.GetForegroundDrawList().AddCircleFilled(transScreen, 8f,
                                ImGuiHelper.Color(255, 165, 0, 200));
                            ImGui.GetForegroundDrawList().AddCircle(transScreen, 12f,
                                ImGuiHelper.Color(255, 100, 0, 255), 0, 2f);
                        }
                    }
                }
            }
        }

        private int GetCellCount(int radius, float gX, float gY, bool walkable)
        {
            var count = 0;
            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var gridX = (int)(gX + dx);
                    var gridY = (int)(gY + dy);
                    if (gridY < 0 || gridY >= TerrainInfo.ProcessedTerrainData.Length ||
                        gridX < 0 || gridX >= TerrainInfo.ProcessedTerrainData[0].Length)
                        continue;

                    if ((TerrainInfo.ProcessedTerrainData[gridY][gridX] != 0) == walkable)
                        count++;
                }
            }

            return count;
        }
    }
}
