using GameHelper;
using GameHelper.RemoteObjects.Components;
using System;
using System.Numerics;
using WFollowBot.Action;
using WFollowBot.Controllers;
using WFollowBot.Data;
using WFollowBot.Leader;
using WFollowBot.PathFinding;

namespace WFollowBot.Managers;

public enum ActionState
{
    Combat,
    Idle,
    Moving,
    Looting,
    Interacting,
    InteractingAreaTransition,
    Equipment,
    ExitMap,
    AltarPick,
    Resurrect,
    SmallMoving,
    Attack,
    Dodge,
    FlaskUse,
    SpamFlaskUse,
}

public class StateManager
{
    private const float TransitionActivationRadius = 25f;

    private ActionState _currentState;

    public event Action<ActionState> OnStateChanged;

    private bool _hasStateChanged;
    private double _nextFlashUseTime;
    public bool HasStateChanged
    {
        get
        {
            lock (_lockObject)
            {
                bool state = _hasStateChanged;
                _hasStateChanged = false;
                return state;
            }
        }
        private set
        {
            lock (_lockObject)
            {
                _hasStateChanged = value;
            }
        }
    }

    private bool _stateIsLock = false;
    private double _lockEndTime;
    public bool StateIsLock { get => _stateIsLock; set => _stateIsLock = value; }

    private readonly object _lockObject = new();

    public ActionState CurrentState
    {
        get
        {
            lock (_lockObject)
            {
                return _currentState;
            }
        }
    }

    public StateManager()
    {
        _currentState = ActionState.Idle;
    }

    public void Update(
        IPathfindingService pathfinding,
        PlayerInfo playerInfo,
        ILeader leader,
        FollowController follow,
        Settings.FollowerSettings settings,
        EntityGridManager entityGridManager,
        ActionProvider actionProvider,
        PlayerContext playerContext)
    {
        if (_stateIsLock)
        {
            if (Environment.TickCount64 >= _lockEndTime)
                _stateIsLock = false;
            else
                return;
        }

        if (playerInfo.IsDead())
        {
            SetState(ActionState.Resurrect);
            return;
        }

        if (settings.FlaskUseEnabled)
        {
            if (settings.SpamFlaskUseEnabled && AllowSpamLifeFlask())
            {
                if (actionProvider.TryExecute(ActionState.SpamFlaskUse, playerContext))
                {
                }

                //SetState(ActionState.SpamFlaskUse);
                return;
            }

            var entity = playerInfo.GetEntity();
            if (entity != null && entity.IsValid && entity.TryGetComponent(out Life life))
            {
                double now = Environment.TickCount64;
                if (now >= _nextFlashUseTime)
                {
                    bool needFlash = false;
                    if (life.Health.Total > 0)
                    {
                        float hpPct = (float)life.Health.Current / life.Health.Total * 100f;
                        if (hpPct < settings.FlaskHpThreshold)
                            needFlash = true;
                    }
                    if (!needFlash && life.Mana.Total > 0)
                    {
                        float manaPct = (float)life.Mana.Current / life.Mana.Total * 100f;
                        if (manaPct < settings.FlaskManaThreshold)
                            needFlash = true;
                    }
                    if (needFlash)
                    {
                        _nextFlashUseTime = now + settings.FlaskCooldownMs;
                        SetState(ActionState.FlaskUse);
                        return;
                    }
                }
            }
        }

        var playerPos = playerInfo.PlayerGridPosition;
        var playerVec = new Vector2(playerPos.X, playerPos.Y);

        bool leaderGone = leader == null || !leader.IsValid;
        if (leaderGone && HasTransitionNearby(playerVec, TransitionActivationRadius))
        {
            SetState(ActionState.InteractingAreaTransition);
            return;
        }

        var status = pathfinding.Status;
        if (status == PathStatus.Full)
        {
            SetState(ActionState.Moving);
        }
        else if (status == PathStatus.Partial)
        {
            SetState(ActionState.SmallMoving);
        }
        else if (settings.AttackEnabled && entityGridManager.GetNearbyEntitiesCount(playerVec, settings.CombatRange) > 0)
        {
            SetState(ActionState.Combat);
            return;
        }
        else if (settings.LootingEnabled && HasLootNearby(playerVec, settings.LootRange))
        {
            SetState(ActionState.Looting);
            return;
        }
        else
        {
            SetState(ActionState.Idle);
        }
    }

    private static bool AllowSpamLifeFlask()
    {
        if (Core.States.InGameStateObject.CurrentWorldInstance.AreaDetails.IsHideout)
            return false;

        var flaskInventory = Core.States.InGameStateObject.CurrentAreaInstance.ServerDataObject.FlaskInventory;

        foreach (var flaskItem in flaskInventory.Items)
        {
            if (!flaskItem.Value.Path.Contains("Life"))
                continue;

            if (!flaskItem.Value.TryGetComponent(out Charges charges))
                continue;

            return charges.Current > charges.PerUseCharge;
        }

        return false;
    }

    private static bool HasTransitionNearby(Vector2 playerPos, float radius)
    {
        var areaInstance = GameHelper.Core.States.InGameStateObject.CurrentAreaInstance;
        if (areaInstance == null) return false;

        var awakeEntities = areaInstance.AwakeEntities;
        float radiusSq = radius * radius;

        foreach (var kvp in awakeEntities)
        {
            var e = kvp.Value;
            if (!e.IsValid) continue;
            if (!e.TryGetComponent(out AreaTransition _) &&
                !e.TryGetComponent(out Transitionable _)) continue;

            if (e.TryGetComponent(out Render render))
            {
                float dx = render.GridPosition.X - playerPos.X;
                float dy = render.GridPosition.Y - playerPos.Y;
                if (dx * dx + dy * dy <= radiusSq)
                    return true;
            }
        }
        return false;
    }

    private static bool HasLootNearby(Vector2 playerPos, float radius)
    {
        var areaInstance = GameHelper.Core.States.InGameStateObject.CurrentAreaInstance;
        if (areaInstance == null) return false;

        var awakeEntities = areaInstance.AwakeEntities;
        float radiusSq = radius * radius;

        foreach (var kvp in awakeEntities)
        {
            var e = kvp.Value;
            if (!e.IsValid) continue;
            if (!e.Path.StartsWith("Metadata/Items/")) continue;

            if (e.TryGetComponent(out Render render))
            {
                float dx = render.GridPosition.X - playerPos.X;
                float dy = render.GridPosition.Y - playerPos.Y;
                if (dx * dx + dy * dy <= radiusSq)
                    return true;
            }
        }
        return false;
    }

    public void InvokeOnStateChanged()
    {
        OnStateChanged?.Invoke(CurrentState);
    }

    public void SetState(ActionState newState)
    {
        bool stateChanged = false;
        lock (_lockObject)
        {
            if (_currentState != newState && !_stateIsLock)
            {
                _currentState = newState;
                stateChanged = true;
                HasStateChanged = true;
            }
        }

        if (stateChanged)
        {
            OnStateChanged?.Invoke(newState);
        }
    }

    public void SetStateAndLock(ActionState newState, int millisecond)
    {
        bool stateChanged = false;
        lock (_lockObject)
        {
            if (_currentState != newState)
            {
                _currentState = newState;
                stateChanged = true;
                HasStateChanged = true;
            }
            _stateIsLock = true;
            _lockEndTime = Environment.TickCount64 + millisecond;
        }

        if (stateChanged)
        {
            OnStateChanged?.Invoke(newState);
        }
    }

    public bool IsInState(ActionState state)
    {
        lock (_lockObject)
        {
            return _currentState == state;
        }
    }
}
