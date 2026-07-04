using System.Drawing;

namespace WFollowBot.Leader;

public interface ILeader
{
    Point GridPosition { get; }
    bool IsValid { get; }
    string Name { get; }
}
