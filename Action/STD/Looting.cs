using GameHelper.RemoteEnums;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using WFollowBot.Managers;
using WFollowBot.Settings;

namespace WFollowBot.Action.STD;

public class Looting : IAction
{
    private static readonly System.Numerics.Vector2 ZeroVec = default;
    public ActionState State => ActionState.Looting;
    public string SkillSetName => "STD";

    public void Execute(PlayerContext context)
    {
        var playerPos = context.PlayerInfo.PlayerGridPosition;
        var playerVec = new Vector2(playerPos.X, playerPos.Y);

        var settings = context.CurrentSettings;
        if (settings == null) return;

        float lootRange = settings.LootRange;
        var lootFilter = (LootFilterFlags)settings.LootFilter;
        if (lootFilter == 0)
        {
            context.MovementController.Stop();
            return;
        }

        var lootItems = new List<Entity>();
        context.EntityGridManager.GetNearbyEntities(playerVec, lootRange, lootItems);

        Entity nearestLoot = null;
        float closestDistSq = lootRange * lootRange;

        foreach (var e in lootItems)
        {
            if (!IsGroundItem(e)) continue;
            if (!MatchesFilter(e, lootFilter)) continue;
            if (e.TryGetComponent(out Chest chest) && chest.IsOpened) continue;

            var ePos = GetEntityPos(e);
            float distSq = Vector2.DistanceSquared(ePos, playerVec);
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                nearestLoot = e;
            }
        }

        if (nearestLoot == null)
        {
            context.MovementController.Stop();
            return;
        }

        float dist = (float)Math.Sqrt(closestDistSq);
        if (dist < 5f)
        {
            context.JoyStick.PressButton(Xbox360Button.A, true);
        }
        else
        {
            context.JoyStick.PressButton(Xbox360Button.A, false);
            var targetPoint = GetEntityPoint(nearestLoot);
            context.MovementController.MoveToward(playerPos, targetPoint, targetPoint);
        }
    }

    private static bool IsGroundItem(Entity e)
    {
        return e.Path.StartsWith("Metadata/Items/");
    }

    private static bool MatchesFilter(Entity e, LootFilterFlags filter)
    {
        var path = e.Path;

        if ((filter & LootFilterFlags.Currency) != 0 && path.Contains("/Currency/"))
            return true;
        if ((filter & LootFilterFlags.Map) != 0 && path.Contains("/Maps/"))
            return true;
        if ((filter & LootFilterFlags.Gem) != 0 && (path.Contains("/Gem") || path.Contains("/Gems/")))
            return true;
        if ((filter & LootFilterFlags.Flask) != 0 && path.Contains("/Flask/"))
            return true;
        if ((filter & LootFilterFlags.Jewel) != 0 && (path.Contains("/Jewel/") || path.Contains("/Jewels/")))
            return true;

        if ((filter & (LootFilterFlags.Unique | LootFilterFlags.Rare)) != 0 &&
            e.TryGetComponent(out ObjectMagicProperties omp))
        {
            if ((filter & LootFilterFlags.Unique) != 0 && omp.Rarity == Rarity.Unique)
                return true;
            if ((filter & LootFilterFlags.Rare) != 0 && omp.Rarity == Rarity.Rare)
                return true;
        }

        return false;
    }

    private static Vector2 GetEntityPos(Entity e)
    {
        if (e.TryGetComponent(out Render render))
            return new Vector2(render.GridPosition.X, render.GridPosition.Y);
        return ZeroVec;
    }

    private static Point GetEntityPoint(Entity e)
    {
        if (e.TryGetComponent(out Render render))
            return new Point((int)render.GridPosition.X, (int)render.GridPosition.Y);
        return Point.Empty;
    }
}
