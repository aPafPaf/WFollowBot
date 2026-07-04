using GameHelper;
using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using ImGuiNET;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WFollowBot.Leader;
using WFollowBot.Settings;

namespace WFollowBot
{
    public sealed partial class WFollowBotCore
    {
        private string _newPresetName = "";
        private int _selectedPresetIdx = -1;
        private string[] _presetNames = Array.Empty<string>();

        private string PresetsDir => Path.Join(this.DllDirectory, "config", "presets");

        private void RefreshPresetList()
        {
            if (!Directory.Exists(PresetsDir))
                _presetNames = Array.Empty<string>();
            else
                _presetNames = Directory.GetFiles(PresetsDir, "*.txt")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .OrderBy(n => n)
                    .ToArray();
            if (_selectedPresetIdx >= _presetNames.Length)
                _selectedPresetIdx = _presetNames.Length - 1;
        }

        private string PresetPath(string name) => Path.Join(PresetsDir, $"{name}.txt");

        public override void DrawSettings()
        {
            ImGui.Checkbox("Draw Render Plugin", ref this.Settings.DrawRenderPlugin);
            ImGui.Checkbox("Show TerrainInfo Debug Window", ref this.Settings.ShowTerrainInfoWindow);
            ImGui.Checkbox("Show Structure Debug", ref this.Settings.ShowStructureDebug);
            ImGui.Checkbox("Render Terrain", ref this.Settings.RenderTerrain);
            ImGui.Checkbox("Cursor Info", ref this.Settings.CursorInfo);
            ImGui.Checkbox("Show Button State Window", ref this.Settings.ShowButtonStateWindow);
            ImGui.SameLine();
            ImGui.TextDisabled("(rendered in game overlay, top-right)");
            DrawButtonStateSummary();
            ImGui.SliderInt("Action Delay", ref this.Settings.ActionDelay, 25, 400);

            ImGui.Separator();
            ImGui.Text("Presets:");

            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##PresetName", ref _newPresetName, 64))
            { }
            ImGui.SameLine();
            if (ImGui.Button("Save") && _newPresetName.Length > 0)
            {
                Directory.CreateDirectory(PresetsDir);
                var data = JsonConvert.SerializeObject(this.Settings, Formatting.Indented);
                File.WriteAllText(PresetPath(_newPresetName), data);
                RefreshPresetList();
                _selectedPresetIdx = Array.IndexOf(_presetNames, _newPresetName);
            }

            RefreshPresetList();
            int sel = _selectedPresetIdx;
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("##Presets", ref sel, _presetNames, _presetNames.Length))
                _selectedPresetIdx = sel;
            ImGui.SameLine();
            if (ImGui.Button("Load") && _selectedPresetIdx >= 0 && _selectedPresetIdx < _presetNames.Length)
            {
                var path = PresetPath(_presetNames[_selectedPresetIdx]);
                if (File.Exists(path))
                {
                    try
                    {
                        var content = File.ReadAllText(path);
                        this.Settings = JsonConvert.DeserializeObject<WFollowBotSettings>(content)
                            ?? new WFollowBotSettings();
                        Player1?.ApplySettings(Settings.Player1Settings);
                        Player2?.ApplySettings(Settings.Player2Settings);
                        _newPresetName = _presetNames[_selectedPresetIdx];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WFollowBot] Failed to load preset: {ex.Message}");
                    }
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Delete") && _selectedPresetIdx >= 0 && _selectedPresetIdx < _presetNames.Length)
            {
                var path = PresetPath(_presetNames[_selectedPresetIdx]);
                if (File.Exists(path))
                    File.Delete(path);
                RefreshPresetList();
                _selectedPresetIdx = -1;
            }

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
                    ImGui.SliderFloat("Detect Range", ref cfg.CombatRange, 5f, 400f);
                    ImGui.SliderFloat("Attack Range", ref cfg.AttackRange, 5f, 300f);
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
                    ImGui.Checkbox("Enable Spam Flask", ref cfg.SpamFlaskUseEnabled);
                    ImGui.Checkbox("Enable Auto ES Flask", ref cfg.EsFlaskUseEnabled);
                    ImGui.SliderInt("HP Threshold %", ref cfg.FlaskHpThreshold, 1, 99);
                    ImGui.SliderInt("Mana Threshold %", ref cfg.FlaskManaThreshold, 1, 99);
                    ImGui.SliderInt("Cooldown (ms)", ref cfg.FlaskCooldownMs, 500, 5000);
                    ImGui.TreePop();
                }

                ImGui.Separator();
                ImGui.PopID();
            }
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
    }
}
