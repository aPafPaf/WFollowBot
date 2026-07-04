using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using Newtonsoft.Json;
using WFollowBot.Leader;

namespace WFollowBot.Settings;

[Flags]
public enum LootFilterFlags
{
    Currency = 1 << 0,
    Map = 1 << 1,
    Gem = 1 << 2,
    Flask = 1 << 3,
    Jewel = 1 << 4,
    Unique = 1 << 5,
    Rare = 1 << 6,
}

public enum CombatStyle
{
    Default,
    MoveOnly,
    Ranged,
    Totem,
}

[Serializable]
public class FollowerSettings
{
    public static readonly string[] SkillSetOptions = ["Simple", "STD"];
    public string Name = "Follower";
    public string PlayerName = "";
    public string LeaderName = "";
    public LeaderMode Mode = LeaderMode.Entity;
    public bool Enabled = false;
    public string SkillSetName = "Simple";
    public float HoldRadius = 15f;
    public int ActionDelay = 75;
    public float RepathInterval = 0.5f;
    public float MaxStuckTime = 2.0f;
    public int StuckThreshold = 3;

    public bool AttackEnabled = true;
    public bool LootingEnabled = true;

    public KeyboardLayout Keyboard { get; set; } = new();

    public int LootFilter = (int)(LootFilterFlags.Currency | LootFilterFlags.Map | LootFilterFlags.Unique | LootFilterFlags.Rare);
    public float LootRange = 15f;
    public float CombatRange = 20f;
    public float AttackRange = 15f;
    public string CombatSkillName = "";
    public CombatStyle Style = CombatStyle.Default;
    public Xbox360Button AttackButton = Xbox360Button.RightShoulder;

    public bool FlaskUseEnabled = false;
    public bool EsFlaskUseEnabled = false;
    public bool SpamFlaskUseEnabled = false;
    [JsonProperty("FlashHpThreshold")]
    public int FlaskHpThreshold = 90;
    [JsonProperty("FlashManaThreshold")]
    public int FlaskManaThreshold = 30;
    [JsonProperty("FlashCooldownMs")]
    public int FlaskCooldownMs = 1000;
}