using System.Drawing;

namespace WFollowBot.Leader;

public class CursorLeader : ILeader
{
    private Point _gridPosition;
    private string _name;

    public CursorLeader()
    {
        _gridPosition = Point.Empty;
        _name = "Cursor";
    }

    public CursorLeader(Point gridPosition, string name = "Cursor")
    {
        _gridPosition = gridPosition;
        _name = name;
    }

    public Point GridPosition => _gridPosition;
    public bool IsValid => _gridPosition != Point.Empty;
    public string Name => _name;

    public void SetPosition(Point gridPosition, string? name = null)
    {
        _gridPosition = gridPosition;
        if (name != null) _name = name;
    }

    public void Clear()
    {
        _gridPosition = Point.Empty;
        _name = "Cursor";
    }
}
