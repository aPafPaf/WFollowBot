//using GameHelper;
//using GameHelper.RemoteObjects.Components;
//using GameHelper.RemoteObjects.States.InGameStateObjects;
//using GameHelper.Utils;
//using GameOffsets.Objects.States.InGameState;
//using ImGuiNET;
//using System;
//using System.Collections.Generic;
//using System.Numerics;
//using System.Text;
//using WFollowBot.Managers;

//namespace WFollowBot
//{
//    public partial class WFollowBotCore
//    {
//        public void DrawRender()
//        {
//            DrawTerrainOverlay();
//            DrawFollowerPaths();
//            DrawTerrainInfoWindow();
//            DrawCursorInfo();
//        }

//        private void DrawCursorInfo()
//        {
//            if (!this.Settings.CursorInfo)
//                return;

//            var mouseWorld = ScreenToWorldHelper.MouseToWorld();
//            var mouseGrid = ScreenToWorldHelper.MouseToGrid();
//            if (mouseGrid == null)
//                return;

//            ImGui.SetNextWindowBgAlpha(0.85f);
//            if (ImGui.Begin("Cursor Info", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
//            {
//                ImGui.Text($"Grid: {mouseGrid.Value.X:F1}, {mouseGrid.Value.Y:F1}");
//                if (mouseWorld.HasValue)
//                    ImGui.Text($"World: {mouseWorld.Value.X:F1}, {mouseWorld.Value.Y:F1}");

//                var player = Core.States.InGameStateObject.CurrentAreaInstance?.Player;
//                if (player != null && player.TryGetComponent<Render>(out var render))
//                {
//                    var startGrid = new System.Drawing.Point((int)render.GridPosition.X, (int)render.GridPosition.Y);
//                    var endGrid = new System.Drawing.Point((int)mouseGrid.Value.X, (int)mouseGrid.Value.Y);

//                    if (startGrid != lastPathStart || endGrid != lastPathEnd)
//                    {
//                        lastPathStart = startGrid;
//                        lastPathEnd = endGrid;
//                        cachedPath = PathFinder.Pathfinder.FindPathList(
//                            TerrainInfo.ProcessedTerrainData, startGrid, endGrid);
//                    }

//                    ImGui.Separator();
//                    if (cachedPath.Count > 0)
//                    {
//                        ImGui.Text($"Path: {cachedPath.Count} nodes");
//                        var first = cachedPath[0];
//                        var last = cachedPath[^1];
//                        ImGui.Text($"From: ({first.X}, {first.Y}) -> ({last.X}, {last.Y})");
//                    }
//                    else
//                    {
//                        ImGui.Text("Path: not found");
//                    }
//                }

//                ImGui.End();
//            }
//        }

//        private void DrawTerrainInfoWindow()
//        {
//            if (!this.Settings.ShowTerrainInfoWindow)
//                return;

//            ImGui.SetNextWindowBgAlpha(0.9f);
//            if (ImGui.Begin("TerrainInfo Debug", ref this.Settings.ShowTerrainInfoWindow))
//            {
//                var dataRows = TerrainInfo.GridHeightData.Length;
//                var dataCols = dataRows > 0 ? TerrainInfo.GridHeightData[0].Length : 0;
//                var walkDataLen = TerrainInfo.GridWalkableData.Length;
//                var processedRows = TerrainInfo.ProcessedTerrainData.Length;
//                var processedCols = processedRows > 0 ? TerrainInfo.ProcessedTerrainData[0].Length : 0;
//                var totalCells = TerrainInfo.TotalCellCount();
//                var walkableCells = TerrainInfo.WalkableCellCount();

//                ImGui.Text($"GridHeightData: {dataRows} x {dataCols}");
//                ImGui.Text($"GridWalkableData: {walkDataLen} bytes");
//                ImGui.Text($"BytesPerRow: {TerrainInfo.BytesPerRow}");
//                ImGui.Text($"ProcessedTerrainData: {processedRows} x {processedCols}");
//                ImGui.Text($"Total cells: {totalCells}");
//                ImGui.Text($"Walkable: {walkableCells}");
//                ImGui.Text($"Non-walkable: {totalCells - walkableCells}");
//                ImGui.Separator();
//                ImGui.Text($"IsBorder(0,0): {TerrainInfo.IsBorder(0, 0)}");

//                if (ImGui.CollapsingHeader("Grid Sample (first 5x5)"))
//                {
//                    if (ImGui.BeginTable("grid_sample", 6, ImGuiTableFlags.Borders))
//                    {
//                        ImGui.TableSetupColumn("y\\x");
//                        for (var x = 0; x < 5; x++)
//                            ImGui.TableSetupColumn($"{x}");
//                        ImGui.TableHeadersRow();

//                        for (var y = 0; y < 5 && y < processedRows; y++)
//                        {
//                            ImGui.TableNextRow();
//                            ImGui.TableSetColumnIndex(0);
//                            ImGui.Text($"{y}");
//                            for (var x = 0; x < 5 && x < processedCols; x++)
//                            {
//                                ImGui.TableSetColumnIndex(x + 1);
//                                var val = TerrainInfo.ProcessedTerrainData[y][x];
//                                ImGui.Text($"{val}");
//                            }
//                        }

//                        ImGui.EndTable();
//                    }
//                }
//            }

//            ImGui.End();
//        }

//        private void DrawTerrainOverlay()
//        {
//            if (!this.Settings.RenderTerrain)
//                return;

//            if (TerrainInfo.ProcessedTerrainData.Length == 0)
//                return;

//            var player = Core.States.InGameStateObject.CurrentAreaInstance?.Player;
//            if (player == null || !player.TryGetComponent<Render>(out var render))
//                return;

//            var radius = this.Settings.TerrainViewRadius;
//            var gridToWorld = TileStructure.TileToWorldConversion / TileStructure.TileToGridConversion;
//            var gX = render.GridPosition.X;
//            var gY = render.GridPosition.Y;
//            var cellSize = new Vector2(5, 5);

//            for (var dy = -radius; dy <= radius; dy++)
//            {
//                for (var dx = -radius; dx <= radius; dx++)
//                {
//                    var gridX = (int)(gX + dx);
//                    var gridY = (int)(gY + dy);
//                    if (gridY < 0 || gridY >= TerrainInfo.ProcessedTerrainData.Length ||
//                        gridX < 0 || gridX >= TerrainInfo.ProcessedTerrainData[0].Length)
//                        continue;

//                    var walkVal = TerrainInfo.ProcessedTerrainData[gridY][gridX];

//                    float height = 0;
//                    if (gridY < TerrainInfo.GridHeightData.Length &&
//                        gridX < TerrainInfo.GridHeightData[0].Length)
//                        height = TerrainInfo.GridHeightData[gridY][gridX];

//                    var screenPos = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
//                        new Vector2(gridX * gridToWorld, gridY * gridToWorld), height);

//                    var color = walkVal == 0
//                        ? ImGuiHelper.Color(255, 0, 0, 120)
//                        : ImGuiHelper.Color(0, 255, 0, 120);

//                    ImGui.GetForegroundDrawList().AddRectFilled(screenPos, screenPos + cellSize, color);
//                }
//            }

//            // Draw player cell in blue
//            {
//                float height = 0;
//                var pgX = (int)gX;
//                var pgY = (int)gY;
//                if (pgY < TerrainInfo.GridHeightData.Length &&
//                    pgX < TerrainInfo.GridHeightData[0].Length)
//                    height = TerrainInfo.GridHeightData[pgY][pgX];

//                var playerScreen = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
//                    new Vector2(pgX * gridToWorld, pgY * gridToWorld), height);
//                ImGui.GetForegroundDrawList().AddRectFilled(
//                    playerScreen, playerScreen + cellSize * 2,
//                    ImGuiHelper.Color(0, 120, 255, 200));
//            }

//            // Draw cached path
//            if (cachedPath.Count > 0)
//            {
//                for (var i = 1; i < cachedPath.Count; i++)
//                {
//                    float h0 = 0, h1 = 0;
//                    var p0Y = cachedPath[i - 1].Y;
//                    var p0X = cachedPath[i - 1].X;
//                    var p1Y = cachedPath[i].Y;
//                    var p1X = cachedPath[i].X;
//                    if (p0Y < TerrainInfo.GridHeightData.Length && p0X < TerrainInfo.GridHeightData[0].Length)
//                        h0 = TerrainInfo.GridHeightData[p0Y][p0X];
//                    if (p1Y < TerrainInfo.GridHeightData.Length && p1X < TerrainInfo.GridHeightData[0].Length)
//                        h1 = TerrainInfo.GridHeightData[p1Y][p1X];

//                    var screen0 = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
//                        new Vector2(p0X * gridToWorld, p0Y * gridToWorld), h0);
//                    var screen1 = Core.States.InGameStateObject.CurrentWorldInstance.WorldToScreen(
//                        new Vector2(p1X * gridToWorld, p1Y * gridToWorld), h1);

//                    ImGui.GetForegroundDrawList().AddLine(screen0, screen1,
//                        ImGuiHelper.Color(255, 255, 0, 220), 3f);
//                }
//            }

//            // Slider window
//            ImGui.Begin("Terrain View");
//            ImGui.SliderInt("Radius", ref this.Settings.TerrainViewRadius, 1, 150);
//            ImGui.Text($"Walkable: {GetCellCount(radius, gX, gY, true)}, Not walkable: {GetCellCount(radius, gX, gY, false)}");
//            ImGui.End();
//        }

//        private void DrawFollowerPaths()
//        {
//            if (_followers.Count == 0) return;

//            var worldInstance = Core.States.InGameStateObject.CurrentWorldInstance;
//            if (worldInstance == null) return;

//            var terrain = TerrainInfo.GridHeightData;
//            if (terrain.Length == 0) return;

//            var gridToWorld = TileStructure.TileToWorldConversion / TileStructure.TileToGridConversion;

//            foreach (var ctx in _followers)
//            {
//                if (!ctx.Enabled) continue;

//                var path = ctx.Storages.Path.GetCurrentPath();
//                if (path == null || path.Count < 2) continue;

//                for (var i = 1; i < path.Count; i++)
//                {
//                    var p0 = path[i - 1];
//                    var p1 = path[i];
//                    float h0 = 0, h1 = 0;
//                    if (p0.Y < terrain.Length && p0.X < terrain[0].Length) h0 = terrain[p0.Y][p0.X];
//                    if (p1.Y < terrain.Length && p1.X < terrain[0].Length) h1 = terrain[p1.Y][p1.X];

//                    var screen0 = worldInstance.WorldToScreen(
//                        new Vector2(p0.X * gridToWorld, p0.Y * gridToWorld), h0);
//                    var screen1 = worldInstance.WorldToScreen(
//                        new Vector2(p1.X * gridToWorld, p1.Y * gridToWorld), h1);

//                    var pathColor = ImGuiHelper.Color(0, 255, 0, 180);
//                    ImGui.GetForegroundDrawList().AddLine(screen0, screen1, pathColor, 2.5f);
//                }

//                // Draw next waypoint marker
//                var nextWp = path[^1];
//                float hp = 0;
//                if (nextWp.Y < terrain.Length && nextWp.X < terrain[0].Length)
//                    hp = terrain[nextWp.Y][nextWp.X];
//                var wpScreen = worldInstance.WorldToScreen(
//                    new Vector2(nextWp.X * gridToWorld, nextWp.Y * gridToWorld), hp);
//                ImGui.GetForegroundDrawList().AddCircleFilled(wpScreen, 5f,
//                    ImGuiHelper.Color(0, 255, 100, 220));

//                // Draw transition target if in transition state
//                if (ctx.StateManager.CurrentState == ActionState.InteractingAreaTransition)
//                {
//                    var areaInst = Core.States.InGameStateObject.CurrentAreaInstance;
//                    if (areaInst != null)
//                    {
//                        Entity nearestTransition = null;
//                        float nearestDistSq = float.MaxValue;
//                        foreach (var kvp in areaInst.AwakeEntities)
//                        {
//                            var e = kvp.Value;
//                            if (!e.IsValid || !e.TryGetComponent(out Transitionable _)) continue;
//                            if (!e.TryGetComponent(out Render r)) continue;
//                            var playerPos = ctx.PlayerInfo.PlayerGridPosition;
//                            float dx = r.GridPosition.X - playerPos.X;
//                            float dy = r.GridPosition.Y - playerPos.Y;
//                            float d = dx * dx + dy * dy;
//                            if (d < nearestDistSq) { nearestDistSq = d; nearestTransition = e; }
//                        }
//                        if (nearestTransition != null && nearestTransition.TryGetComponent(out Render tr1))
//                        {
//                            float th = 0;
//                            int tgy = (int)tr1.GridPosition.Y, tgx = (int)tr1.GridPosition.X;
//                            if (tgy < terrain.Length && tgx < terrain[0].Length) th = terrain[tgy][tgx];
//                            var transScreen = worldInstance.WorldToScreen(
//                                new Vector2(tgx * gridToWorld, tgy * gridToWorld), th);
//                            ImGui.GetForegroundDrawList().AddCircleFilled(transScreen, 8f,
//                                ImGuiHelper.Color(255, 165, 0, 200));
//                            ImGui.GetForegroundDrawList().AddCircle(transScreen, 12f,
//                                ImGuiHelper.Color(255, 100, 0, 255), 0, 2f);
//                        }
//                    }
//                }
//            }
//        }

//        private int GetCellCount(int radius, float gX, float gY, bool walkable)
//        {
//            var count = 0;
//            for (var dy = -radius; dy <= radius; dy++)
//            {
//                for (var dx = -radius; dx <= radius; dx++)
//                {
//                    var gridX = (int)(gX + dx);
//                    var gridY = (int)(gY + dy);
//                    if (gridY < 0 || gridY >= TerrainInfo.ProcessedTerrainData.Length ||
//                        gridX < 0 || gridX >= TerrainInfo.ProcessedTerrainData[0].Length)
//                        continue;

//                    if ((TerrainInfo.ProcessedTerrainData[gridY][gridX] != 0) == walkable)
//                        count++;
//                }
//            }

//            return count;
//        }
//    }
//}
