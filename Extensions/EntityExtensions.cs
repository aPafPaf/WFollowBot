using GameHelper.RemoteObjects.Components;
using GameHelper.RemoteObjects.States.InGameStateObjects;
using System.Drawing;

namespace GameHelper.Extensions
{
    public static class EntityExtensions
    {
        public static Point GetGridPos(this Entity entity)
        {
            if (entity == null)
                return Point.Empty;

            if (!entity.TryGetComponent(out Render componentRender))
                return Point.Empty;

            return componentRender.GetGridPos();
        }
    }
}
