using System.Drawing;

namespace WFollowBot.Data;

public class StoragePath
{
    public StoragePath()
    {
        PathSegments = new PathSegments();
        IntermediatePoints = new IntermediatePoints();
        AreaChange();
    }

    public PathSegments PathSegments { get; private set; }
    public IntermediatePoints IntermediatePoints { get; private set; }

    public void AreaChange(string areaName = "")
    {
        IntermediatePoints.Clear();
    }

    public void AreaTransition()
    {
        IntermediatePoints.Clear();
    }
}
