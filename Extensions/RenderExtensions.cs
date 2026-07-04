namespace GameHelper.Extensions
{
    using GameHelper.RemoteObjects.Components;
    using GameOffsets.Natives;
    using System;
    using System.Drawing;

    public static class RenderExtensions
    {
        public static Point GetGridPos(this Render render)
        {
            var pos = render.GridPosition;
            return new Point((int)pos.X, (int)pos.Y);
        }

        public static float DistanceTo(this Render render, StdTuple3D<float> target)
        {
            var pos = render.GridPosition;
            var dx = pos.X - target.X;
            var dy = pos.Y - target.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}