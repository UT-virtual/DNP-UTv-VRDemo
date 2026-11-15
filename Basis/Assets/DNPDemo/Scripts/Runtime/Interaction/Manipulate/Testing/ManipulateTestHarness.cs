using System;
using System.Collections.Generic;
using DNPDemo.Pen;
using UnityEngine;

namespace DNPDemo.Interaction.Manipulate.Testing
{
    /// <summary>
    /// Lets manipulation be tried in a test scene without a session, roles or scenario. Each setup is one object with its
    /// constraint, taken from a <see cref="ManipulatePart"/> whose keys are ignored: the objects are referenced directly.
    /// In manipulate mode the pen grabs whichever object its tip touches, with the same rules as
    /// <see cref="ManipulateTool"/>: the ghost shows the pose to reach, the object lights up while it can be grabbed, and
    /// a release is judged and the verdict is logged. A success snaps (or stays, for a threshold) and the object returns
    /// to its start after <see cref="_resetDelay"/> seconds, so it can be tried again.
    /// <para>
    /// Every setup listens to the pen at once, so the objects must stand apart: a tip touching two grab zones grabs both.
    /// </para>
    /// Network sync is not covered. Not for the demo scenes, where <see cref="ManipulateTool"/> runs the manipulate parts.
    /// </summary>
    public sealed class ManipulateTestHarness : MonoBehaviour
    {
        [Serializable]
        sealed class Setup
        {
            public string Label;

            [Tooltip("The object that moves.")]
            public Transform Target;

            [Tooltip("Object with a DetectionZoneGroup or DetectionZone the tip must touch. Empty: the target itself.")]
            public GameObject GrabZone;

            [Tooltip("Translate: the marker the object snaps to.")]
            public Transform Goal;

            [Tooltip("Rotate: the pivot, whose local space holds the axis.")]
            public Transform Pivot;

            public ManipulateGhostView Ghost;

            [Tooltip("Constraint and completion. The keys are not used here.")]
            public ManipulatePart Part = new();

            [NonSerialized] public ManipulateGrabber Grabber;
            [NonSerialized] public ManipulateConstraint Constraint;
            [NonSerialized] public ManipulateMotion Motion;
            [NonSerialized] public readonly ManipulateHighlight Highlight = new();
            [NonSerialized] public float ResetAt = -1f;
        }

        [SerializeField] SpatialPenController _pen;
        [SerializeField] List<Setup> _setups = new();

        [Tooltip("Seconds the object takes to snap to the goal.")]
        [SerializeField, Min(0f)] float _snapSeconds = 0.25f;

        [Tooltip("Seconds after a success before the object returns to its start.")]
        [SerializeField, Min(0f)] float _resetDelay = 1.5f;

        void Start()
        {
            if (_pen == null)
            {
                Debug.LogError("[DNPDemo] ManipulateTestHarness: SpatialPenController を設定してください。", this);
                return;
            }
            foreach (Setup setup in _setups)
            {
                Prepare(setup);
            }
        }

        void OnDestroy()
        {
            foreach (Setup setup in _setups)
            {
                setup.Grabber?.Stop();
                setup.Highlight.Set(false);
            }
        }

        void LateUpdate()
        {
            foreach (Setup setup in _setups)
            {
                if (setup.Grabber == null)
                {
                    continue;
                }
                setup.Grabber.Tick();
                if (setup.Grabber.IsGrabbing)
                {
                    setup.Motion.Place(setup.Constraint.PoseOf(setup.Grabber.Value));
                }
                else if (setup.ResetAt >= 0f && Time.time >= setup.ResetAt)
                {
                    setup.ResetAt = -1f;
                    setup.Grabber.SetValue(default);
                    setup.Motion.Animate(setup.Constraint.Start, _snapSeconds);
                }
                setup.Highlight.Set(setup.Grabber.IsHovering || setup.Grabber.IsGrabbing);
                setup.Motion.Tick(Time.deltaTime, 20f);
            }
        }

        void Prepare(Setup setup)
        {
            string label = string.IsNullOrEmpty(setup.Label) ? "（名前なし）" : setup.Label;
            if (setup.Target == null || setup.Part == null)
            {
                Debug.LogError($"[DNPDemo] 操作のテスト「{label}」: 対象と部品を設定してください。", this);
                return;
            }
            var start = new Pose(setup.Target.position, setup.Target.rotation);
            setup.Constraint = ManipulateConstraint.FromPart(setup.Part, start, setup.Goal, setup.Pivot);
            if (setup.Constraint == null)
            {
                Debug.LogError($"[DNPDemo] 操作のテスト「{label}」: 目標マーカーか支点が足りないか、回転軸が 0 です。", this);
                return;
            }
            IDetectionVolume grabZone = DetectionVolumes.Find(setup.GrabZone != null ? setup.GrabZone : setup.Target.gameObject);
            if (grabZone == null)
            {
                Debug.LogError($"[DNPDemo] 操作のテスト「{label}」: 掴む範囲に DetectionZoneGroup か DetectionZone がありません。", this);
                return;
            }
            setup.Motion = new ManipulateMotion(setup.Target);
            setup.Highlight.Bind(setup.Target);
            setup.Grabber = new ManipulateGrabber(_pen);
            setup.Grabber.Grabbed += () => setup.ResetAt = -1f;
            setup.Grabber.Released += (value, intended) => HandleReleased(setup, label, value, intended);
            setup.Grabber.Listen(setup.Part.Mode, setup.Constraint, grabZone, default);
            if (setup.Ghost != null)
            {
                setup.Ghost.Show(setup.Target, setup.Constraint.GuidePose);
            }
        }

        void HandleReleased(Setup setup, string label, ManipulateValue value, bool intended)
        {
            setup.Motion.Place(setup.Constraint.PoseOf(value));
            if (!intended)
            {
                return;
            }
            string measured = setup.Constraint.Describe(value);
            bool succeeded = setup.Constraint.TryComplete(value, out ManipulateValue result);
            Debug.Log($"[DNPDemo] 操作のテスト「{label}」：{(succeeded ? "成功" : "未達")}：{measured}", this);
            if (succeeded)
            {
                setup.Grabber.SetValue(result);
                setup.Motion.Animate(setup.Constraint.PoseOf(result), _snapSeconds);
                setup.ResetAt = Time.time + _resetDelay;
            }
        }
    }
}
