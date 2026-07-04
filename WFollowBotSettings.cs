using System.Collections.Generic;
using GameHelper.Plugin;
using WFollowBot.Settings;

namespace WFollowBot
{
    public sealed class WFollowBotSettings : IPSettings
    {
        public bool DrawRenderPlugin = true;
        public int TerrainViewRadius = 12;
        public bool ShowTerrainInfoWindow = false;
        public bool RenderTerrain = false;
        public bool CursorInfo = false;
        public bool ShowStructureDebug = false;
        public bool ShowButtonStateWindow = false;
        public int ActionDelay = 75;

        public FollowerSettings Player1Settings { get; set; } = new() { Name = "Player1", LeaderName = "", Enabled = false };
        public FollowerSettings Player2Settings { get; set; } = new() { Name = "Player2", LeaderName = "", Enabled = false };
    }
}
