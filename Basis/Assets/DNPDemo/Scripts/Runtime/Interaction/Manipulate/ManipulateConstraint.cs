using System;
using UnityEngine;

namespace DNPDemo.Interaction.Manipulate
{
    /// <summary>
    /// How far a manipulated object has been moved from where the part started. Only the field of the constraint's kind
    /// is used. This is what travels between devices; each device turns it into a pose with
    /// <see cref="ManipulateConstraint.PoseOf"/>.
    /// </summary>
    public struct ManipulateValue : IEquatable<ManipulateValue>
    {
        /// <summary>Translate: world-space displacement from the starting position, in metres.</summary>
        public Vector3 Offset;

        /// <summary>Rotate: degrees around the axis from the starting pose.</summary>
        public float Angle;

        /// <summary>
        /// Released at the goal and snapped to it. A snapped translation also takes the goal marker's rotation, which the
        /// offset alone does not carry.
        /// </summary>
        public bool Snapped;

        public static ManipulateValue Translation(Vector3 offset, bool snapped = false) => new() { Offset = offset, Snapped = snapped };

        public static ManipulateValue Rotation(float angle, bool snapped = false) => new() { Angle = angle, Snapped = snapped };

        public readonly bool Equals(ManipulateValue other) =>
            Offset == other.Offset && Angle.Equals(other.Angle) && Snapped == other.Snapped;

        public override readonly bool Equals(object obj) => obj is ManipulateValue other && Equals(other);

        public override readonly int GetHashCode() => HashCode.Combine(Offset, Angle, Snapped);
    }

    /// <summary>
    /// The geometry of one manipulate part, resolved to world space when the part begins: where the object started, how
    /// it may move, and when a release completes the part. It turns a <see cref="ManipulateValue"/> into the object's
    /// pose, keeps values within range and judges releases. Plain C#, so it can be tested without a scene.
    /// <para>
    /// World space is shared by both devices (the statue stands at the same place in the same scene), so a value means
    /// the same pose everywhere.
    /// </para>
    /// </summary>
    public sealed class ManipulateConstraint
    {
        /// <summary>
        /// Closer than this to the rotation axis, the tip's direction around the axis is too unstable to turn the object;
        /// the rotation waits until the tip moves out again.
        /// </summary>
        public const float MinLeverRadius = 0.02f;

        readonly Pose _start;

        // Translate.
        readonly Pose _goal;
        readonly float _maxDistance;
        readonly float _snapDistance;

        // Rotate.
        readonly Vector3 _pivot;
        readonly Vector3 _axis;
        readonly float _minAngle;
        readonly float _maxAngle;
        readonly RotateCompletion _completion;
        readonly float _targetAngle;
        readonly float _snapAngle;
        readonly float _thresholdAngle;

        ManipulateConstraint(ManipulateConstraintKind kind, Pose start, Pose goal, float maxDistance, float snapDistance,
            Vector3 pivot, Vector3 axis, float minAngle, float maxAngle, RotateCompletion completion,
            float targetAngle, float snapAngle, float thresholdAngle)
        {
            Kind = kind;
            _start = start;
            _goal = goal;
            _maxDistance = maxDistance;
            _snapDistance = snapDistance;
            _pivot = pivot;
            _axis = axis;
            _minAngle = minAngle;
            _maxAngle = maxAngle;
            _completion = completion;
            _targetAngle = targetAngle;
            _snapAngle = snapAngle;
            _thresholdAngle = thresholdAngle;
        }

        public static ManipulateConstraint Translate(Pose start, Pose goal, float maxDistance, float snapDistance) =>
            new(ManipulateConstraintKind.Translate, start, goal, Mathf.Max(0f, maxDistance), Mathf.Max(0f, snapDistance),
                default, Vector3.up, 0f, 0f, default, 0f, 0f, 0f);

        /// <summary>
        /// Rotation around <paramref name="axis"/> (world space, any length but zero) through <paramref name="pivot"/>.
        /// The range always includes 0, the starting pose.
        /// </summary>
        public static ManipulateConstraint Rotate(Pose start, Vector3 pivot, Vector3 axis, float minAngle, float maxAngle,
            RotateCompletion completion, float targetAngle, float snapAngle, float thresholdAngle)
        {
            if (axis.sqrMagnitude < 1e-12f)
            {
                throw new ArgumentException("The rotation axis must not be zero.", nameof(axis));
            }
            return new ManipulateConstraint(ManipulateConstraintKind.Rotate, start, default, 0f, 0f,
                pivot, axis.normalized, Mathf.Min(minAngle, 0f), Mathf.Max(maxAngle, 0f), completion,
                targetAngle, Mathf.Max(0f, snapAngle), thresholdAngle);
        }

        /// <summary>
        /// The constraint of <paramref name="part"/> for an object that starts at <paramref name="start"/>.
        /// <paramref name="goal"/> (translate) or <paramref name="pivot"/> (rotate) must be given; null when the one the
        /// part needs is missing or its parameters cannot work (a zero axis).
        /// </summary>
        public static ManipulateConstraint FromPart(ManipulatePart part, Pose start, Transform goal, Transform pivot)
        {
            if (part == null)
            {
                return null;
            }
            switch (part.Constraint)
            {
                case ManipulateConstraintKind.Translate:
                    return goal == null ? null : Translate(start, new Pose(goal.position, goal.rotation), part.MaxDistance, part.SnapDistance);
                case ManipulateConstraintKind.Rotate:
                    if (pivot == null)
                    {
                        return null;
                    }
                    Vector3 axis = pivot.TransformDirection(part.Axis);
                    return axis.sqrMagnitude < 1e-12f
                        ? null
                        : Rotate(start, pivot.position, axis, part.MinAngle, part.MaxAngle, part.Completion,
                            part.TargetAngle, part.SnapAngle, part.ThresholdAngle);
                default:
                    return null;
            }
        }

        public ManipulateConstraintKind Kind { get; }

        public Pose Start => _start;

        /// <summary>Translate: the goal can be reached within the allowed distance.</summary>
        public bool IsGoalReachable => Kind != ManipulateConstraintKind.Translate
            || Vector3.Distance(_start.position, _goal.position) <= _maxDistance + _snapDistance;

        /// <summary>Where the ghost shows the object: the goal, the target angle, or the threshold angle.</summary>
        public Pose GuidePose => Kind == ManipulateConstraintKind.Translate
            ? _goal
            : RotatedPose(_completion == RotateCompletion.Snap ? _targetAngle : _thresholdAngle);

        /// <summary>The object's world pose for <paramref name="value"/>.</summary>
        public Pose PoseOf(ManipulateValue value)
        {
            if (Kind == ManipulateConstraintKind.Translate)
            {
                return value.Snapped ? _goal : new Pose(_start.position + value.Offset, _start.rotation);
            }
            return RotatedPose(value.Snapped && _completion == RotateCompletion.Snap ? _targetAngle : value.Angle);
        }

        /// <summary>Keeps <paramref name="value"/> within the allowed range (used on values from the network too).</summary>
        public ManipulateValue Clamp(ManipulateValue value)
        {
            if (Kind == ManipulateConstraintKind.Translate)
            {
                return value.Snapped
                    ? ManipulateValue.Translation(_goal.position - _start.position, snapped: true)
                    : ManipulateValue.Translation(Vector3.ClampMagnitude(value.Offset, _maxDistance));
            }
            if (value.Snapped && _completion == RotateCompletion.Snap)
            {
                return ManipulateValue.Rotation(_targetAngle, snapped: true);
            }
            return ManipulateValue.Rotation(ClampAngle(value.Angle));
        }

        public float ClampAngle(float angle) => Mathf.Clamp(angle, _minAngle, _maxAngle);

        /// <summary>
        /// Judges a release. True when it completes the part, with the value to end at: snapped to the goal or the target
        /// angle, or as released for a threshold.
        /// </summary>
        public bool TryComplete(ManipulateValue released, out ManipulateValue result)
        {
            result = released;
            if (Kind == ManipulateConstraintKind.Translate)
            {
                if (Vector3.Distance(_start.position + released.Offset, _goal.position) > _snapDistance)
                {
                    return false;
                }
                result = ManipulateValue.Translation(_goal.position - _start.position, snapped: true);
                return true;
            }
            switch (_completion)
            {
                case RotateCompletion.Snap:
                    if (Mathf.Abs(released.Angle - _targetAngle) > _snapAngle)
                    {
                        return false;
                    }
                    result = ManipulateValue.Rotation(_targetAngle, snapped: true);
                    return true;
                case RotateCompletion.Threshold:
                    return _thresholdAngle > 0f ? released.Angle >= _thresholdAngle : released.Angle <= _thresholdAngle;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Rotate: signed degrees the tip turned around the axis going from <paramref name="from"/> to
        /// <paramref name="to"/>. False when either point is within <see cref="MinLeverRadius"/> of the axis.
        /// </summary>
        public bool TryMeasureTurn(Vector3 from, Vector3 to, out float degrees)
        {
            degrees = 0f;
            Vector3 a = Vector3.ProjectOnPlane(from - _pivot, _axis);
            Vector3 b = Vector3.ProjectOnPlane(to - _pivot, _axis);
            if (a.sqrMagnitude < MinLeverRadius * MinLeverRadius || b.sqrMagnitude < MinLeverRadius * MinLeverRadius)
            {
                return false;
            }
            degrees = Vector3.SignedAngle(a, b, _axis);
            return true;
        }

        /// <summary>One-line description of <paramref name="value"/> against the goal, for logs.</summary>
        public string Describe(ManipulateValue value)
        {
            if (Kind == ManipulateConstraintKind.Translate)
            {
                float distance = Vector3.Distance(PoseOf(value).position, _goal.position);
                return $"目標まで {distance * 100f:0.#}cm（スナップ {_snapDistance * 100f:0.#}cm 以内）";
            }
            return _completion == RotateCompletion.Snap
                ? $"{value.Angle:0.#}°（目標 {_targetAngle:0.#}° ± {_snapAngle:0.#}°）"
                : $"{value.Angle:0.#}°（しきい値 {_thresholdAngle:0.#}°）";
        }

        Pose RotatedPose(float angle)
        {
            Quaternion turn = Quaternion.AngleAxis(angle, _axis);
            return new Pose(_pivot + turn * (_start.position - _pivot), turn * _start.rotation);
        }
    }

    /// <summary>
    /// One grab of a manipulated object: turns the pen tip's movement since the grab into a value.
    /// <list type="bullet">
    /// <item>Translate: the object moves by the tip's displacement since the grab, clamped to the allowed distance, so it
    /// stays on the edge toward the hand when the hand goes too far.</item>
    /// <item>Rotate: the tip's turn around the axis is added up frame by frame, so it keeps counting past ±180°. Each
    /// step is clamped to the range, so turning back from a stop moves the object at once.</item>
    /// </list>
    /// </summary>
    public sealed class ManipulateDrag
    {
        readonly ManipulateConstraint _constraint;
        readonly Vector3 _grabTip;
        readonly ManipulateValue _atGrab;
        Vector3 _lastTip;
        bool _hasLastTip;
        float _angle;

        public ManipulateDrag(ManipulateConstraint constraint, ManipulateValue atGrab, Vector3 tip)
        {
            _constraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
            _atGrab = constraint.Clamp(atGrab);
            _grabTip = tip;
            _angle = _atGrab.Angle;
            Value = _atGrab;
            TryKeepTip(tip);
        }

        public ManipulateValue Value { get; private set; }

        /// <summary>Moves the object with the tip now at <paramref name="tip"/>. Returns the new value.</summary>
        public ManipulateValue Update(Vector3 tip)
        {
            if (_constraint.Kind == ManipulateConstraintKind.Translate)
            {
                // A snapped object picked up again continues from the goal position (with the starting rotation).
                Value = _constraint.Clamp(ManipulateValue.Translation(_atGrab.Offset + (tip - _grabTip)));
                return Value;
            }
            if (!_hasLastTip)
            {
                TryKeepTip(tip);
                return Value;
            }
            if (_constraint.TryMeasureTurn(_lastTip, tip, out float degrees))
            {
                _angle = _constraint.ClampAngle(_angle + degrees);
                _lastTip = tip;
                Value = ManipulateValue.Rotation(_angle);
            }
            return Value;
        }

        void TryKeepTip(Vector3 tip)
        {
            // A starting point on the axis has no direction; the turn is measured from the first point off it.
            if (_constraint.Kind == ManipulateConstraintKind.Rotate && !_constraint.TryMeasureTurn(tip, tip, out _))
            {
                return;
            }
            _lastTip = tip;
            _hasLastTip = true;
        }
    }
}
