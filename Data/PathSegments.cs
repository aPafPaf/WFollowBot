using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace WFollowBot.Data
{
    public class PathSegments
    {
        private readonly object _pathSegmentsLock = new object();
        private LinkedList<Point> _pathSegmentsList = new();

        public bool isLastSegment { get => Count == 0; }

        public int Count
        {
            get
            {
                lock (_pathSegmentsLock)
                {
                    return _pathSegmentsList.Count;
                }
            }
        }

        public void SetPathSegments(IEnumerable<Point> newSegments)
        {
            lock (_pathSegmentsLock)
            {
                _pathSegmentsList = new(newSegments);
            }
        }

        public void AddPathSegmentToFront(Point segment)
        {
            if (segment == default)
            {
                return;
            }

            lock (_pathSegmentsLock)
            {
                _pathSegmentsList.AddFirst(segment);
            }
        }

        public void AddPathSegmentsToFront(IEnumerable<Point> segments)
        {
            if (!segments.Any())
            {
                return;
            }

            lock (_pathSegmentsLock)
            {
                foreach (Point segment in segments)
                {
                    _pathSegmentsList.AddFirst(segment);
                }
            }
        }

        public List<Point> GetPathSegments()
        {
            lock (_pathSegmentsLock)
            {
                return new List<Point>(_pathSegmentsList);
            }
        }

        public bool TryDequeuePathSegment(out Point segment)
        {
            lock (_pathSegmentsLock)
            {
                if (_pathSegmentsList.Count == 0)
                {
                    segment = new();
                    return false;
                }

                segment = _pathSegmentsList.First();
                _pathSegmentsList.RemoveFirst();
            }

            return segment != default;
        }

        public void Clear()
        {
            _pathSegmentsList.Clear();
        }
    }
}
