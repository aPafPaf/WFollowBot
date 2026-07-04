using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace WFollowBot.PathFinding
{
    public class PathFinderResult
    {
        public PathFinderResult() { }
        public PathFinderResult(bool isTimeout)
        {
            IsTimeout = isTimeout;
        }

        public List<Point> Path { get; set; } = new();
        public bool IsTimeout { get; set; }
        public bool PathFound => Path.Count > 0;
    }
}
