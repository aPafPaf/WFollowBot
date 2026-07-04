
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WFollowBot.Managers;

namespace WFollowBot.Action;

public class ActionProvider
{
    private Dictionary<string, List<IAction>> _allActions = new();

    private Dictionary<ActionState, IAction> _activeActions = new();

    public bool TryGetAction(ActionState actionState, out IAction action) =>
        _activeActions.TryGetValue(actionState, out action);

    public ActionProvider()
    {
        RegisterAllActions();
        ChangeActiveActions("Simple");
    }

    public void RegisterAllActions()
    {
        var actionType = typeof(IAction);
        var actions = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => actionType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .Select(t => (IAction)Activator.CreateInstance(t)!)
            .ToList();

        _allActions = actions
            .GroupBy(a => a.SkillSetName)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public void ChangeActiveActions(string skillSetName)
    {
        _activeActions.Clear();

        _allActions.TryGetValue(skillSetName, out var actions);
        _allActions.TryGetValue("STD", out var stdActions);

        var combined = (actions ?? Enumerable.Empty<IAction>())
            .Concat(stdActions ?? Enumerable.Empty<IAction>())
            .GroupBy(a => a.State)
            .Select(g => g.First());

        _activeActions = combined.ToDictionary(a => a.State, a => a);
    }

    public bool TryExecute(ActionState state, PlayerContext context)
    {
        try
        {
            if (_activeActions.TryGetValue(state, out var action))
            {
                action.Execute(context);
                return true;
            }
        }
        catch (Exception ex)
        {
        }

        return false;
    }
}
