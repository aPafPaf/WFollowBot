using GameHelper.Plugin;
using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using System.Drawing;
using WFollowBot.Action;
using WFollowBot.Controllers;
using WFollowBot.Data;
using WFollowBot.Leader;
using WFollowBot.Managers;
using WFollowBot.PathFinding;
using WFollowBotCore;

namespace WFollowBot;

public class PlayerContext
{
    public bool Enabled => _cachedSettings?.Enabled ?? false;
    public string LeaderName { get; set; }
    public string PlayerName { get; set; }
    public ILeader Leader { get; private set; }
    public int FollowerIndex { get; }

    public PlayerInfo PlayerInfo { get; private set; }

    private PCore<WFollowBotSettings> Plugin { get; }
    public JoyStick JoyStick { get; }
    public Storages Storages { get; }
    public StateManager StateManager { get; }
    public ActionProvider ActionProvider { get; }
    public ActionsController ActionsController { get; }
    public RegenManager RegenManager { get; }
    public IPathfindingService Pathfinding { get; private set; }
    public FollowController FollowController { get; }
    public MovementController MovementController { get; }
    public EntityGridManager EntityGridManager { get; set; }

    private Settings.FollowerSettings _cachedSettings;
    public Settings.FollowerSettings CurrentSettings => _cachedSettings;

    private string _lastAreaHash = string.Empty;
    private Point? _cursorGridPosition;
    private CursorLeader _cursorLeader;
    private LeaderMode _lastAppliedMode = (LeaderMode)(-1);

    public PlayerContext(PCore<WFollowBotSettings> plugin, int index)
    {
        FollowerIndex = index;
        Plugin = plugin;
        JoyStick = new JoyStick(index);
        Storages = new Storages();
        StateManager = new StateManager();
        PlayerInfo = new PlayerInfo();
        ActionProvider = new ActionProvider();
        ActionsController = new ActionsController();
        RegenManager = new RegenManager(PlayerInfo);
        Pathfinding = new PathfindingService(PlayerInfo, RegenManager);
        FollowController = new FollowController();
        MovementController = new MovementController(JoyStick);
    }

    public void Update()
    {
        if (!Enabled) return;
        CheckAreaChange();
        UpdateLeader();
        PlayerInfo.Update();
        Pathfinding.PrevPosition = PlayerInfo.PlayerGridPosition;
    }

    private void CheckAreaChange()
    {
        var area = GameHelper.Core.States.InGameStateObject.CurrentAreaInstance;
        if (area == null) return;
        if (area.AreaHash != _lastAreaHash)
        {
            _lastAreaHash = area.AreaHash;
            Leader = null;
            _cursorGridPosition = null;
            _cursorLeader?.Clear();
            PlayerInfo.ForceEntityRefresh();
            Pathfinding.ClearPath();
            Storages.Path.AreaChange();
            TerrainInfo.Update();
        }
    }

    public void Action()
    {
        if (!Enabled) return;
        if (!ActionsController.AllowAction) return;

        if (Leader != null && Leader.IsValid)
        {
            var leaderPos = Leader.GridPosition;
            var myPos = PlayerInfo.PlayerGridPosition;
            FollowController.SetLeaderDistance(leaderPos, myPos);

            Pathfinding.RequestPath(leaderPos);
        }

        Pathfinding.Update();

        StateManager.Update(Pathfinding, PlayerInfo, Leader, FollowController, _cachedSettings);

        if (StateManager.HasStateChanged)
            JoyStick.Reset();

        if (ActionProvider.TryExecute(StateManager.CurrentState, this))
        {
            ActionsController.SetNextActionTime(Plugin.Settings.ActionDelay);
        }
    }

    public void ApplySettings(Settings.FollowerSettings cfg)
    {
        _cachedSettings = cfg;
        ActionProvider.ChangeActiveActions(cfg.SkillSetName);
        PlayerName = cfg.PlayerName;
        PlayerInfo.SetPlayerName(cfg.PlayerName);
        FollowController.HoldRadius = cfg.HoldRadius;
        Pathfinding.RepathInterval = cfg.RepathInterval;
        Pathfinding.MaxStuckTime = cfg.MaxStuckTime;
        Pathfinding.StuckThreshold = cfg.StuckThreshold;
        JoyStick.ApplyLayout(cfg.Keyboard);
        JoyStick.KeyboardEnabled = cfg.Keyboard.Enabled;

        if (_lastAppliedMode != cfg.Mode)
        {
            SwitchMode(cfg.Mode, cfg.LeaderName);
        }
        else if (cfg.Mode == LeaderMode.Entity &&
                 !string.Equals(LeaderName, cfg.LeaderName, System.StringComparison.Ordinal))
        {
            LeaderName = cfg.LeaderName;
            Leader = null;
        }
    }

    public void SetCursorPosition(Point gridPosition)
    {
        _cursorGridPosition = gridPosition;
        SwitchMode(LeaderMode.Cursor);
    }

    private void SwitchMode(LeaderMode newMode, string leaderName = null)
    {
        if (_lastAppliedMode == newMode)
            return;

        _lastAppliedMode = newMode;
        _cachedSettings.Mode = newMode;
        Leader = null;

        if (newMode == LeaderMode.Entity)
        {
            LeaderName = leaderName ?? string.Empty;
            _cursorGridPosition = null;
            _cursorLeader = null;
        }
        else
        {
            LeaderName = string.Empty;
        }
    }

    public void SetLeaderByName(string name)
    {
        LeaderName = name;
        Leader = null;
    }

    private void UpdateLeader()
    {
        if (_cachedSettings.Mode == LeaderMode.Entity)
            UpdateEntityLeader();
        else
            UpdateCursorLeader();
    }

    private void UpdateEntityLeader()
    {
        if (Leader is EntityLeader el && el.IsValid)
            return;

        if (TryGetLeader(out Entity entity))
            Leader = new EntityLeader(entity);
        else
            Leader = null;
    }

    private void UpdateCursorLeader()
    {
        if (_cursorGridPosition.HasValue)
        {
            if (_cursorLeader == null)
                _cursorLeader = new CursorLeader();
            _cursorLeader.SetPosition(_cursorGridPosition.Value);
            Leader = _cursorLeader;
        }
        else
        {
            Leader = null;
        }
    }

    private bool TryGetLeader(out Entity leaderEntity)
    {
        leaderEntity = null;
        if (string.IsNullOrEmpty(LeaderName))
            return false;

        var areaInstance = GameHelper.Core.States.InGameStateObject.CurrentAreaInstance;
        if (areaInstance == null) return false;

        foreach (var entityD in areaInstance.AwakeEntities)
        {
            Entity entity = entityD.Value;

            if (!entity.IsValid || entity.EntityState == EntityStates.Useless ||
                entity.EntityType == EntityTypes.Renderable ||
                entity.EntityState == EntityStates.PinnacleBossHidden)
                continue;

            if (entity.EntityType is not EntityTypes.Player)
                continue;

            if (!entity.TryGetComponent(out Player playerComponent))
                continue;

            if (string.IsNullOrEmpty(playerComponent.Name))
                continue;

            if (!LeaderName.Equals(playerComponent.Name, System.StringComparison.OrdinalIgnoreCase))
                continue;

            leaderEntity = entity;
            return true;
        }

        return false;
    }
}
