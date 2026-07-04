using GameHelper;
using GameHelper.RemoteEnums.Entity;
using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace WFollowBot.Managers
{
    public class EntityGridManager
    {
        private readonly EntitySpatialGrid _grid;
        private readonly int _cellSize = 8;

        private const int MaxEntities = 200;

        public EntityGridManager()
        {
            _grid = new EntitySpatialGrid(_cellSize);
        }

        public EntitySpatialGrid Grid => _grid;

        public void Update()
        {
            var awakeEntities = Core.States.InGameStateObject.CurrentAreaInstance.AwakeEntities;

            _grid.BeginRebuild();
            int count = 0;

            foreach (var kvp in awakeEntities)
            {
                if (count >= MaxEntities)
                    break;

                var entity = kvp.Value;
                if (!entity.IsValid /*|| entity.EntityState == EntityStates.Useless*/)
                    continue;

                if (entity.EntityType is not EntityTypes.Monster)
                    continue;

                if (!entity.TryGetComponent(out Life life))
                    continue;
                if (life.Health.Current == 0)
                    continue;

                if (!entity.TryGetComponent(out Targetable targetable))
                    continue;
                //if (!targetable.IsTargetable)
                //    continue;

                if (!entity.TryGetComponent(out Positioned positioned))
                    continue;
                if (positioned.IsFriendly)
                    continue;

                _grid.Add(entity);
                count++;
            }

            _grid.EndRebuild();
        }

        public void GetNearbyEntities(Vector2 gridPos, float radius, List<Entity> results)
        {
            _grid.GetEntitiesInRadius(gridPos, radius * radius, results);
        }

        public int GetNearbyEntitiesCount(Vector2 gridPos, float radius)
        {
            List<Entity> results = new List<Entity>();
            _grid.GetEntitiesInRadius(gridPos, radius * radius, results);

            return results.Count;
        }

        public List<Entity> GetEntitiesAroundPoint(Vector2 pos, float radius)
        {
            var results = new List<Entity>();
            _grid.GetEntitiesInRadius(pos, radius * radius, results);
            return results;
        }

        public int GetEntityCountAroundPoint(Vector2 pos, float radius)
        {
            var results = new List<Entity>();
            _grid.GetEntitiesInRadius(pos, radius * radius, results);
            return results.Count;
        }

        public Entity GetClosestEntity(Vector2 gridPos, float maxDistance)
        {
            var results = new List<Entity>();
            GetNearbyEntities(gridPos, maxDistance, results);

            Entity closest = null;
            float closestDistSq = float.MaxValue;

            foreach (var e in results)
            {
                var ePos = GetEntityPos(e);
                float distSq = Vector2.DistanceSquared(ePos, gridPos);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closest = e;
                }
            }

            return closest;
        }

        private static Vector2 GetEntityPos(Entity e)
        {
            if (e.TryGetComponent(out Render render))
                return new Vector2(render.GridPosition.X, render.GridPosition.Y);
            return Vector2.Zero;
        }

        public static Point GetEntityGridPoint(Entity e)
        {
            if (e.TryGetComponent(out Render render))
                return new Point((int)render.GridPosition.X, (int)render.GridPosition.Y);
            return Point.Empty;
        }
    }
}
