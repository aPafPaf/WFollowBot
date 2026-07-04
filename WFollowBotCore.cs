using GameHelper;
using GameHelper.Plugin;
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
using WFollowBot.Events;
using WFollowBot.Input;
using WFollowBot.Leader;
using WFollowBot.Managers;
using WFollowBot.Settings;

namespace WFollowBot
{
    public sealed class WFollowBotCore : PCore<WFollowBotSettings>
    {
        AreaChange AreaChange;
        EntityGridManager EntityGridManager;
        private MouseHelper MouseHelper;
        private PlayerContext Player1;
        private PlayerContext Player2;

        System.Drawing.Point lastPathStart = new(-1, -1);
        System.Drawing.Point lastPathEnd = new(-1, -1);
        List<System.Drawing.Point> cachedPath = new();

        public override void DrawSettings()
        {
            ImGui.Checkbox("Show TerrainInfo Debug Window", ref this.Settings.ShowTerrainInfoWindow);
            ImGui.Checkbox("Show Structure Debug", ref this.Settings.ShowStructureDebug);
            ImGui.Checkbox("Render Terrain", ref this.Settings.RenderTerrain);
            ImGui.Checkbox("Cursor Info", ref this.Settings.CursorInfo);
            ImGui.SliderInt("Action Delay", ref this.Settings.ActionDelay, 25, 400);

            ImGui.Separator();
            ImGui.Text("Followers:");

            var followersList = new[] {
                (ctx: Player1, cfg: Settings.Player1Settings),
                (ctx: Player2, cfg: Settings.Player2Settings)
            };
            for (int i = 0; i < followersList.Length; i++)
            {
                var (ctx, cfg) = followersList[i];
                ImGui.PushID(i);

                bool enabled = cfg.Enabled;
                ImGui.Checkbox("Enable", ref enabled);
                cfg.Enabled = enabled;

                ImGui.SameLine();
                ImGui.Text(cfg.Name);
                ImGui.SameLine();
                bool atk = cfg.AttackEnabled;
                if (ImGui.Checkbox("Atk", ref atk)) cfg.AttackEnabled = atk;
                ImGui.SameLine();
                bool loot = cfg.LootingEnabled;
                if (ImGui.Checkbox("Loot", ref loot)) cfg.LootingEnabled = loot;

                var allPlayers = GetAllPlayerNames();
                var playerIdx = Math.Max(0, Array.IndexOf(allPlayers, cfg.PlayerName));
                if (ImGui.Combo("Player", ref playerIdx, allPlayers, allPlayers.Length))
                {
                    cfg.PlayerName = playerIdx > 0 ? allPlayers[playerIdx] : "";
                    ctx?.ApplySettings(cfg);
                }
                var playerNames = GetPlayerNames();
                int currentIdx = Math.Max(0, Array.IndexOf(playerNames, cfg.LeaderName));
                if (ImGui.Combo("Follow Target", ref currentIdx, playerNames, playerNames.Length))
                {
                    cfg.LeaderName = currentIdx > 0 ? playerNames[currentIdx] : "";
                    ctx?.ApplySettings(cfg);  // ApplySettings now handles LeaderName changes
                }

                int modeIdx = (int)cfg.Mode;
                if (ImGui.Combo("Leader Mode", ref modeIdx, new[] { "Entity (Follow Player)", "Cursor (Fixed Point)" }, 2))
                {
                    cfg.Mode = (LeaderMode)modeIdx;
                    ctx?.ApplySettings(cfg);
                }

                if (cfg.Mode == LeaderMode.Cursor)
                {
                    ImGui.TextDisabled("RMB in game to set position");
                    ImGui.SameLine();
                    if (ctx?.Leader != null && ctx.Leader.IsValid)
                        ImGui.Text($"Pos: ({ctx.Leader.GridPosition.X}, {ctx.Leader.GridPosition.Y})");
                }

                ImGui.SameLine();
                if (ctx != null)
                {
                    string status = !ctx.Enabled ? "Disabled" :
                        ctx.Leader == null || !ctx.Leader.IsValid ? "No Leader" :
                        ctx.Pathfinding.IsStuck ? "Stuck!" :
                        ctx.StateManager.CurrentState.ToString();
                    ImGui.Text($"Status: {status}");
                }

                if (ImGui.SliderFloat("Hold Radius", ref cfg.HoldRadius, 10f, 60f))
                    ctx?.ApplySettings(cfg);

                int skillIdx = Math.Max(0, Array.IndexOf(FollowerSettings.SkillSetOptions, cfg.SkillSetName));
                if (ImGui.Combo("Skill Set", ref skillIdx, FollowerSettings.SkillSetOptions, FollowerSettings.SkillSetOptions.Length))
                {
                    cfg.SkillSetName = FollowerSettings.SkillSetOptions[skillIdx];
                    ctx?.ApplySettings(cfg);
                }

                if (ctx != null)
                {
                    ImGui.Text($"Dist: {MathF.Sqrt(ctx.FollowController.SquaredDistToLeader):F1}  State: {ctx.StateManager.CurrentState}");
                }

                if (ImGui.TreeNode("Keyboard"))
                {
                    bool kbEnabled = cfg.Keyboard.Enabled;
                    if (ImGui.Checkbox("Enable Keyboard", ref kbEnabled))
                    {
                        cfg.Keyboard.Enabled = kbEnabled;
                        if (ctx != null) ctx.JoyStick.KeyboardEnabled = kbEnabled;
                    }
                    if (kbEnabled)
                    {
                        if (ImGui.TreeNode("Bindings"))
                        {
                            var bindings = ctx?.JoyStick.GetBindingLabels();
                            if (bindings != null)
                                foreach (var b in bindings)
                                    ImGui.TextDisabled(b);
                            else
                                ImGui.TextDisabled("(not initialized)");
                            ImGui.TreePop();
                        }
                        ImGui.SameLine();
                        ImGui.TextDisabled("Key names: A-Z, 0-9, ArrowUp/Down/Left/Right, Tab, Enter, Space, LShift, LControl, LAlt, F1-F12, Home, End, PageUp, PageDown");
                    }
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Loot Settings"))
                {
                    int flags = cfg.LootFilter;
                    bool c = (flags & (int)LootFilterFlags.Currency) != 0;
                    if (ImGui.Checkbox("Currency", ref c)) { if (c) flags |= (int)LootFilterFlags.Currency; else flags &= ~(int)LootFilterFlags.Currency; cfg.LootFilter = flags; }
                    bool m = (flags & (int)LootFilterFlags.Map) != 0;
                    if (ImGui.Checkbox("Maps", ref m)) { if (m) flags |= (int)LootFilterFlags.Map; else flags &= ~(int)LootFilterFlags.Map; cfg.LootFilter = flags; }
                    bool g = (flags & (int)LootFilterFlags.Gem) != 0;
                    if (ImGui.Checkbox("Gems", ref g)) { if (g) flags |= (int)LootFilterFlags.Gem; else flags &= ~(int)LootFilterFlags.Gem; cfg.LootFilter = flags; }
                    bool fl = (flags & (int)LootFilterFlags.Flask) != 0;
                    if (ImGui.Checkbox("Flasks", ref fl)) { if (fl) flags |= (int)LootFilterFlags.Flask; else flags &= ~(int)LootFilterFlags.Flask; cfg.LootFilter = flags; }
                    bool j = (flags & (int)LootFilterFlags.Jewel) != 0;
                    if (ImGui.Checkbox("Jewels", ref j)) { if (j) flags |= (int)LootFilterFlags.Jewel; else flags &= ~(int)LootFilterFlags.Jewel; cfg.LootFilter = flags; }
                    bool u = (flags & (int)LootFilterFlags.Unique) != 0;
                    if (ImGui.Checkbox("Uniques", ref u)) { if (u) flags |= (int)LootFilterFlags.Unique; else flags &= ~(int)LootFilterFlags.Unique; cfg.LootFilter = flags; }
                    bool r = (flags & (int)LootFilterFlags.Rare) != 0;
                    if (ImGui.Checkbox("Rares", ref r)) { if (r) flags |= (int)LootFilterFlags.Rare; else flags &= ~(int)LootFilterFlags.Rare; cfg.LootFilter = flags; }
                    ImGui.SliderFloat("Loot Range", ref cfg.LootRange, 5f, 30f);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Combat Settings"))
                {
                    ImGui.SliderFloat("Detect Range", ref cfg.CombatRange, 5f, 40f);
                    ImGui.SliderFloat("Attack Range", ref cfg.AttackRange, 5f, 30f);
                    ImGui.InputText("Skill Name", ref cfg.CombatSkillName, 64);
                    int styleIdx = (int)cfg.Style;
                    if (ImGui.Combo("Style", ref styleIdx, new[] { "Default", "MoveOnly", "Ranged", "Totem" }, 4))
                        cfg.Style = (CombatStyle)styleIdx;

                    var buttonOptions = new[] { "A", "B", "X", "Y", "D-Up", "D-Down", "D-Left", "D-Right", "Start", "Back", "Guide", "LB", "RB", "L3", "R3" };
                    var buttonValues = new[] { Xbox360Button.A, Xbox360Button.B, Xbox360Button.X, Xbox360Button.Y, Xbox360Button.Up, Xbox360Button.Down, Xbox360Button.Left, Xbox360Button.Right, Xbox360Button.Start, Xbox360Button.Back, Xbox360Button.Guide, Xbox360Button.LeftShoulder, Xbox360Button.RightShoulder, Xbox360Button.LeftThumb, Xbox360Button.RightThumb };
                    int btnIdx = Math.Max(0, Array.IndexOf(buttonValues, cfg.AttackButton));
                    if (ImGui.Combo("Attack Button", ref btnIdx, buttonOptions, buttonOptions.Length))
                        cfg.AttackButton = buttonValues[btnIdx];
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Flask"))
                {
                    ImGui.Checkbox("Enable Auto Flask", ref cfg.FlaskUseEnabled);
                    ImGui.SliderInt("HP Threshold %", ref cfg.FlaskHpThreshold, 1, 99);
                    ImGui.SliderInt("Mana Threshold %", ref cfg.FlaskManaThreshold, 1, 99);
                    ImGui.SliderInt("Cooldown (ms)", ref cfg.FlaskCooldownMs, 500, 5000);
                    ImGui.TreePop();
                }

                ImGui.Separator();
                ImGui.PopID();
            }
        }

        public override void DrawUI()
        {
            MouseHelper.Update();
            HandleCursorLeaderClick();

            TerrainInfo.Update();

            EntityGridManager.Update();

            Player1?.Update();
            Player2?.Update();

            Player1?.Action();
            Player2?.Action();

            DrawRender();
        }

        public void DrawRender()
        {
            DrawTerrainOverlay();
            DrawFollowerPaths();
            DrawTerrainInfoWindow();
            DrawCursorInfo();
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

                // Draw next waypoint marker
                var nextWp = path[^1];
                float hp = 0;
                if (nextWp.Y < terrain.Length && nextWp.X < terrain[0].Length)
                    hp = terrain[nextWp.Y][nextWp.X];
                var wpScreen = worldInstance.WorldToScreen(
                    new Vector2(nextWp.X * gridToWorld, nextWp.Y * gridToWorld), hp);
                ImGui.GetForegroundDrawList().AddCircleFilled(wpScreen, 5f,
                    ImGuiHelper.Color(0, 255, 100, 220));

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
                            var playerPos = ctx.PlayerInfo.PlayerGridPosition;
                            float dx = r.GridPosition.X - playerPos.X;
                            float dy = r.GridPosition.Y - playerPos.Y;
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

        public override void OnDisable()
        {
            AreaChange.Disable();
            if (Player1?.JoyStick.IsRunning == true) Player1.JoyStick.Stop();
            if (Player2?.JoyStick.IsRunning == true) Player2.JoyStick.Stop();
        }

        public override void OnEnable(bool isGameOpened)
        {
            MouseHelper = new MouseHelper();
            AreaChange = new AreaChange();
            EntityGridManager = new EntityGridManager();

            Player1 = new PlayerContext(this, 0);
            Player1.EntityGridManager = EntityGridManager;
            Player1.ApplySettings(Settings.Player1Settings);
            Player1.JoyStick.Start();

            Player2 = new PlayerContext(this, 1);
            Player2.EntityGridManager = EntityGridManager;
            Player2.ApplySettings(Settings.Player2Settings);
            Player2.JoyStick.Start();
        }

        public override void SaveSettings()
        {
        }

        private string[] GetAllPlayerNames()
        {
            var areaInstance = Core.States.InGameStateObject.CurrentAreaInstance;
            if (areaInstance == null) return Array.Empty<string>();

            var names = new List<string> { "-" };
            foreach (var kvp in areaInstance.AwakeEntities)
            {
                var e = kvp.Value;
                if (!e.IsValid || e.EntityType != EntityTypes.Player) continue;
                if (!e.TryGetComponent(out Player playerComp)) continue;
                names.Add(playerComp.Name);
            }
            return names.Distinct().ToArray();
        }

        private string[] GetPlayerNames()
        {
            var areaInstance = Core.States.InGameStateObject.CurrentAreaInstance;
            if (areaInstance == null) return Array.Empty<string>();

            var playerSelf = areaInstance.Player;
            var names = new List<string> { "-" };
            foreach (var kvp in areaInstance.AwakeEntities)
            {
                var e = kvp.Value;
                if (!e.IsValid || e.EntityType != EntityTypes.Player) continue;
                if (!e.TryGetComponent(out Player playerComp)) continue;
                if (string.IsNullOrEmpty(playerComp.Name)) continue;
                // Keep the display consistent: exclude self from the
                // "Follow Target" dropdown since you can't follow yourself.
                // Use reference equality which may be unreliable in a memory-
                // reading framework; if it fails, self will appear in both
                // dropdowns but ComputeFollowTarget handles dist==0 gracefully.
                if (e == playerSelf) continue;
                names.Add(playerComp.Name);
            }
            return names.Distinct().ToArray();
        }

        private void HandleCursorLeaderClick()
        {
            if (!MouseHelper.IsRightClicked())
                return;

            if (ImGui.GetIO().WantCaptureMouse)
                return;

            var mouseGrid = ScreenToWorldHelper.MouseToGrid();
            if (!mouseGrid.HasValue)
                return;

            var pt = new System.Drawing.Point((int)mouseGrid.Value.X, (int)mouseGrid.Value.Y);

            foreach (var ctx in new[] { Player1, Player2 })
            {
                if (ctx == null || !ctx.Enabled)
                    continue;
                if (ctx.CurrentSettings.Mode != LeaderMode.Cursor)
                    continue;
                ctx.SetCursorPosition(pt);
            }
        }
    }
}
