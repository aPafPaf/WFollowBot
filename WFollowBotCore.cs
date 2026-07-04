using GameHelper.Plugin;
using Newtonsoft.Json;
using System;
using System.IO;
using WFollowBot.Events;
using WFollowBot.Input;
using WFollowBot.Leader;
using WFollowBot.Managers;

namespace WFollowBot
{
    public sealed partial class WFollowBotCore : PCore<WFollowBotSettings>
    {
        private AreaChange AreaChange;
        private EntityGridManager EntityGridManager;
        private MouseHelper MouseHelper;
        private PlayerContext Player1;
        private PlayerContext Player2;

        private string SettingPathname => Path.Join(this.DllDirectory, "config", "settings.txt");

        public override void DrawUI()
        {
            MouseHelper.Update();
            HandleCursorLeaderClick();

            //TerrainInfo.Update();

            EntityGridManager.Update();

            Player1?.Update();
            Player2?.Update();

            Player1?.Action();
            Player2?.Action();

            DrawRender();
        }

        public override void OnDisable()
        {
            AreaChange?.Disable();
            if (Player1?.JoyStick.IsRunning == true) Player1.JoyStick.Stop();
            if (Player2?.JoyStick.IsRunning == true) Player2.JoyStick.Stop();
        }

        public override void OnEnable(bool isGameOpened)
        {
            if (File.Exists(this.SettingPathname))
            {
                try
                {
                    var content = File.ReadAllText(this.SettingPathname);
                    this.Settings = JsonConvert.DeserializeObject<WFollowBotSettings>(content)
                        ?? new WFollowBotSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WFollowBot] Failed to load settings: {ex.Message}. Using defaults.");
                }
            }

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
            Directory.CreateDirectory(Path.GetDirectoryName(this.SettingPathname) ?? string.Empty);
            var settingsData = JsonConvert.SerializeObject(this.Settings, Formatting.Indented);
            File.WriteAllText(this.SettingPathname, settingsData);
        }

        private void HandleCursorLeaderClick()
        {
            if (!MouseHelper.IsRightClicked())
                return;

            if (ImGuiNET.ImGui.GetIO().WantCaptureMouse)
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
