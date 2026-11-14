using System;
using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Manipulate
{
    /// <summary>How a manipulated object may move.</summary>
    public enum ManipulateConstraintKind
    {
        /// <summary>Moves with the pen's tip without turning, within a distance of where it started.</summary>
        Translate = 0,
        /// <summary>Turns around one axis through a pivot, within an angle range.</summary>
        Rotate = 1,
    }

    /// <summary>When a rotation counts as done.</summary>
    public enum RotateCompletion
    {
        /// <summary>Released near <see cref="ManipulatePart.TargetAngle"/>; the object snaps to it.</summary>
        Snap = 0,
        /// <summary>Released past <see cref="ManipulatePart.ThresholdAngle"/>; the object stays where it was released.</summary>
        Threshold = 1,
    }

    /// <summary>
    /// Grabbing a part of the statue with the pen's tip and moving it within a constraint. While the pen is in manipulate
    /// mode, holding the trigger with the tip in the grab zone grabs the object; it follows the tip until the trigger is
    /// released (see <see cref="ManipulateTool"/>).
    /// <list type="bullet">
    /// <item><see cref="ManipulateConstraintKind.Translate"/>: the object keeps its rotation and stays within
    /// <see cref="MaxDistance"/> of where it started. Released within <see cref="SnapDistance"/> of the goal marker
    /// (<see cref="GoalKey"/>), it snaps to the marker's position and rotation.</item>
    /// <item><see cref="ManipulateConstraintKind.Rotate"/>: the object turns around <see cref="Axis"/> through the pivot
    /// (<see cref="PivotKey"/>), between <see cref="MinAngle"/> and <see cref="MaxAngle"/>. Angles are in degrees from the
    /// pose at the start of the part, positive by the left-hand rule around the axis (Unity's rotation direction).</item>
    /// </list>
    /// A ghost of the object shows where it should go: the goal, the target angle, or the threshold angle.
    /// </summary>
    [Serializable]
    public sealed class ManipulatePart : InteractionPart
    {
        public const float DefaultMaxDistance = 0.5f;
        public const float DefaultSnapDistance = 0.05f;
        public const float DefaultSnapAngle = 10f;

        [Tooltip("Key (ScenarioObjectBinding) of the object that moves.")]
        public string TargetKey;

        [Tooltip("Key of the zone the pen's tip must touch to grab. The object carries a DetectionZoneGroup or a DetectionZone. Empty: the zone on the target itself.")]
        public string GrabZoneKey;

        public ManipulateConstraintKind Constraint;

        [Tooltip("Translate: key of the marker whose position and rotation the object snaps to.")]
        public string GoalKey;

        [Tooltip("Translate: farthest the object may move from where it started, in metres.")]
        [Min(0.01f)] public float MaxDistance = DefaultMaxDistance;

        [Tooltip("Translate: released within this distance of the goal, in metres, the object snaps to it.")]
        [Min(0.001f)] public float SnapDistance = DefaultSnapDistance;

        [Tooltip("Rotate: key of the object whose position is the pivot and whose local space holds the axis.")]
        public string PivotKey;

        [Tooltip("Rotate: axis of rotation in the pivot's local space.")]
        public Vector3 Axis = Vector3.up;

        [Tooltip("Rotate: smallest angle, in degrees from the starting pose. 0 or less.")]
        public float MinAngle = -90f;

        [Tooltip("Rotate: largest angle, in degrees from the starting pose. 0 or more.")]
        public float MaxAngle = 90f;

        public RotateCompletion Completion;

        [Tooltip("Rotate (snap): angle the object snaps to, in degrees.")]
        public float TargetAngle = 45f;

        [Tooltip("Rotate (snap): released within this many degrees of the target angle, the object snaps to it.")]
        [Min(0.1f)] public float SnapAngle = DefaultSnapAngle;

        [Tooltip("Rotate (threshold): released past this angle (on the same side of 0), the part is done. Not 0.")]
        public float ThresholdAngle = 30f;

        public ManipulatePart()
        {
        }

        public static ManipulatePart Translate(string instruction, string targetKey, string goalKey,
            float maxDistance = DefaultMaxDistance, float snapDistance = DefaultSnapDistance) => new()
        {
            Instruction = instruction,
            TargetKey = targetKey,
            Constraint = ManipulateConstraintKind.Translate,
            GoalKey = goalKey,
            MaxDistance = maxDistance,
            SnapDistance = snapDistance,
        };

        public static ManipulatePart RotateToTarget(string instruction, string targetKey, string pivotKey, Vector3 axis,
            float minAngle, float maxAngle, float targetAngle, float snapAngle = DefaultSnapAngle) => new()
        {
            Instruction = instruction,
            TargetKey = targetKey,
            Constraint = ManipulateConstraintKind.Rotate,
            PivotKey = pivotKey,
            Axis = axis,
            MinAngle = minAngle,
            MaxAngle = maxAngle,
            Completion = RotateCompletion.Snap,
            TargetAngle = targetAngle,
            SnapAngle = snapAngle,
        };

        public static ManipulatePart RotatePastThreshold(string instruction, string targetKey, string pivotKey, Vector3 axis,
            float minAngle, float maxAngle, float thresholdAngle) => new()
        {
            Instruction = instruction,
            TargetKey = targetKey,
            Constraint = ManipulateConstraintKind.Rotate,
            PivotKey = pivotKey,
            Axis = axis,
            MinAngle = minAngle,
            MaxAngle = maxAngle,
            Completion = RotateCompletion.Threshold,
            ThresholdAngle = thresholdAngle,
        };

        public override PenMode Mode => PenMode.Manipulate;

        /// <summary>Key of the zone that is grabbed: <see cref="GrabZoneKey"/>, or the target when it is empty.</summary>
        public string EffectiveGrabZoneKey => string.IsNullOrWhiteSpace(GrabZoneKey) ? TargetKey : GrabZoneKey;

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            Add(requirements, TargetKey);
            Add(requirements, GrabZoneKey);
            Add(requirements, Constraint == ManipulateConstraintKind.Translate ? GoalKey : PivotKey);
        }

        public override void Validate(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(TargetKey))
            {
                problems.Add("動かす対象のキーが空です。");
            }
            switch (Constraint)
            {
                case ManipulateConstraintKind.Translate:
                    if (string.IsNullOrWhiteSpace(GoalKey))
                    {
                        problems.Add("平行移動の目標マーカーのキーが空です。");
                    }
                    if (!(MaxDistance > 0f))
                    {
                        problems.Add("平行移動の最大距離は 0 より大きくしてください。");
                    }
                    if (!(SnapDistance > 0f))
                    {
                        problems.Add("平行移動のスナップ距離は 0 より大きくしてください。");
                    }
                    break;
                case ManipulateConstraintKind.Rotate:
                    ValidateRotation(problems);
                    break;
                default:
                    problems.Add($"操作の制約 {(int)Constraint} は定義されていません。");
                    break;
            }
        }

        void ValidateRotation(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(PivotKey))
            {
                problems.Add("回転の支点のキーが空です。");
            }
            if (Axis.sqrMagnitude < 1e-6f)
            {
                problems.Add("回転軸の長さが 0 です。");
            }
            if (MinAngle > 0f || MaxAngle < 0f)
            {
                problems.Add($"回転の範囲（{MinAngle:0.#}〜{MaxAngle:0.#}°）は、開始時の 0° を含めてください。");
            }
            switch (Completion)
            {
                case RotateCompletion.Snap:
                    if (TargetAngle < MinAngle || TargetAngle > MaxAngle)
                    {
                        problems.Add($"回転の目標角度 {TargetAngle:0.#}° が範囲（{MinAngle:0.#}〜{MaxAngle:0.#}°）の外にあります。");
                    }
                    if (!(SnapAngle > 0f))
                    {
                        problems.Add("回転のスナップ角度は 0 より大きくしてください。");
                    }
                    break;
                case RotateCompletion.Threshold:
                    if (ThresholdAngle == 0f)
                    {
                        problems.Add("回転のしきい値は 0 以外にしてください。");
                    }
                    else if (ThresholdAngle < MinAngle || ThresholdAngle > MaxAngle)
                    {
                        problems.Add($"回転のしきい値 {ThresholdAngle:0.#}° が範囲（{MinAngle:0.#}〜{MaxAngle:0.#}°）の外にあります。");
                    }
                    break;
                default:
                    problems.Add($"回転の完了条件 {(int)Completion} は定義されていません。");
                    break;
            }
        }

        public override string ToString() => Constraint switch
        {
            ManipulateConstraintKind.Translate => $"平行移動（{Label(TargetKey)} → {Label(GoalKey)}）{Instruction}",
            ManipulateConstraintKind.Rotate when Completion == RotateCompletion.Threshold =>
                $"回転（{Label(TargetKey)}、{Label(PivotKey)} を支点に {ThresholdAngle:0.#}° を超える）{Instruction}",
            ManipulateConstraintKind.Rotate => $"回転（{Label(TargetKey)}、{Label(PivotKey)} を支点に {TargetAngle:0.#}°）{Instruction}",
            _ => $"操作（不明な制約）{Instruction}",
        };

        static void Add(ScenarioRequirements requirements, string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                requirements.ObjectKeys.Add(key);
            }
        }

        static string Label(string key) => string.IsNullOrWhiteSpace(key) ? "未設定" : key;
    }
}
