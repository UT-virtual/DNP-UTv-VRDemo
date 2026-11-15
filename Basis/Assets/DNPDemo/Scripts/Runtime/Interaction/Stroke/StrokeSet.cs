using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction.Stroke
{
    /// <summary>One line drawn in the air: world-space points in drawing order, under an id unique within its set.</summary>
    public sealed class StrokeLine
    {
        readonly List<Vector3> _points = new();
        Vector3 _min;
        Vector3 _max;

        internal StrokeLine(ushort id)
        {
            Id = id;
        }

        public ushort Id { get; }

        public IReadOnlyList<Vector3> Points => _points;

        public int Count => _points.Count;

        /// <summary>The trigger was released: the line will not grow any more, and it can be judged and erased.</summary>
        public bool IsFinished { get; internal set; }

        /// <summary>Axis-aligned box around the points. Zero-sized at the origin while the line is empty.</summary>
        public Bounds Bounds => _points.Count == 0 ? default : new Bounds((_min + _max) * 0.5f, _max - _min);

        public Vector3 Last => _points[_points.Count - 1];

        internal void Add(Vector3 point)
        {
            if (_points.Count == 0)
            {
                _min = point;
                _max = point;
            }
            else
            {
                _min = Vector3.Min(_min, point);
                _max = Vector3.Max(_max, point);
            }
            _points.Add(point);
        }

        /// <summary>The sphere touches the line: some segment (or the single point) lies within <paramref name="reach"/>.</summary>
        public bool Touches(Vector3 center, float reach)
        {
            if (_points.Count == 0)
            {
                return false;
            }
            Vector3 margin = Vector3.one * reach;
            Vector3 min = _min - margin;
            Vector3 max = _max + margin;
            if (center.x < min.x || center.y < min.y || center.z < min.z || center.x > max.x || center.y > max.y || center.z > max.z)
            {
                return false;
            }
            float reachSqr = reach * reach;
            if (_points.Count == 1)
            {
                return (center - _points[0]).sqrMagnitude <= reachSqr;
            }
            for (int i = 1; i < _points.Count; i++)
            {
                if (SegmentDistanceSqr(center, _points[i - 1], _points[i]) <= reachSqr)
                {
                    return true;
                }
            }
            return false;
        }

        static float SegmentDistanceSqr(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lengthSqr = ab.sqrMagnitude;
            float t = lengthSqr > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / lengthSqr) : 0f;
            return (p - (a + ab * t)).sqrMagnitude;
        }
    }

    /// <summary>
    /// The lines of one stroke part, on the actor's device as drawn and on the others as received. Holds no Unity
    /// objects; <see cref="StrokeView"/> draws it.
    /// <para>
    /// Sizes are bounded so the network messages and the drawing stay small: <see cref="MaxPointsPerLine"/> per line and
    /// <see cref="MaxTotalPoints"/> over all lines. Points past either limit are not added; the line simply stops growing.
    /// </para>
    /// </summary>
    public sealed class StrokeSet
    {
        public const int MaxPointsPerLine = 1024;
        public const int MaxTotalPoints = 4096;

        /// <summary>Most lines a set holds at once (the network format counts them in a byte).</summary>
        public const int MaxLines = 255;

        /// <summary>
        /// Distance the tip must move before the next point is recorded. Dense enough for a smooth line, sparse enough
        /// that a 1024-point line is about 5 m long.
        /// </summary>
        public const float MinPointSpacing = 0.005f;

        readonly List<StrokeLine> _lines = new();
        ushort _nextId;

        public IReadOnlyList<StrokeLine> Lines => _lines;

        public int TotalPoints { get; private set; }

        public void Clear()
        {
            _lines.Clear();
            TotalPoints = 0;
            _nextId = 0;
        }

        /// <summary>Actor: starts a new line at <paramref name="point"/>. Null when the set is full.</summary>
        public StrokeLine Begin(Vector3 point)
        {
            if (_lines.Count >= MaxLines || TotalPoints >= MaxTotalPoints)
            {
                return null;
            }
            // Ids only need to differ from the lines still in the set; skip any still in use after wrapping around.
            while (Find(_nextId) != null)
            {
                _nextId++;
            }
            StrokeLine line = Create(_nextId++);
            Append(line, point);
            return line;
        }

        /// <summary>
        /// Actor: adds <paramref name="point"/> if the tip moved at least <see cref="MinPointSpacing"/> since the last
        /// point and the limits allow. True when the point was added.
        /// </summary>
        public bool Extend(StrokeLine line, Vector3 point)
        {
            if (line == null || line.IsFinished || line.Count == 0
                || (point - line.Last).sqrMagnitude < MinPointSpacing * MinPointSpacing)
            {
                return false;
            }
            return Append(line, point);
        }

        /// <summary>
        /// Observer: the line with <paramref name="id"/>, created when it is new. Null when a new line would exceed
        /// <see cref="MaxLines"/>.
        /// </summary>
        public StrokeLine GetOrCreate(ushort id)
        {
            StrokeLine line = Find(id);
            if (line != null)
            {
                return line;
            }
            return _lines.Count < MaxLines ? Create(id) : null;
        }

        /// <summary>Adds a point without the spacing rule (received points). False when a limit is reached.</summary>
        public bool Append(StrokeLine line, Vector3 point)
        {
            if (line == null || line.Count >= MaxPointsPerLine || TotalPoints >= MaxTotalPoints)
            {
                return false;
            }
            line.Add(point);
            TotalPoints++;
            return true;
        }

        public void Finish(StrokeLine line)
        {
            if (line != null)
            {
                line.IsFinished = true;
            }
        }

        public StrokeLine Find(ushort id)
        {
            foreach (StrokeLine line in _lines)
            {
                if (line.Id == id)
                {
                    return line;
                }
            }
            return null;
        }

        public bool Remove(ushort id)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Id == id)
                {
                    TotalPoints -= _lines[i].Count;
                    _lines.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// A finished line that the sphere at <paramref name="center"/> touches, oldest first. The line being drawn is
        /// never returned.
        /// </summary>
        public bool TryFindTouching(Vector3 center, float reach, out StrokeLine touched)
        {
            foreach (StrokeLine line in _lines)
            {
                if (line.IsFinished && line.Touches(center, reach))
                {
                    touched = line;
                    return true;
                }
            }
            touched = null;
            return false;
        }

        /// <summary>Box around every point of every line. False when there are no points.</summary>
        public bool TryGetBounds(out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (StrokeLine line in _lines)
            {
                if (line.Count == 0)
                {
                    continue;
                }
                if (any)
                {
                    bounds.Encapsulate(line.Bounds);
                }
                else
                {
                    bounds = line.Bounds;
                    any = true;
                }
            }
            return any;
        }

        StrokeLine Create(ushort id)
        {
            var line = new StrokeLine(id);
            _lines.Add(line);
            return line;
        }
    }
}
