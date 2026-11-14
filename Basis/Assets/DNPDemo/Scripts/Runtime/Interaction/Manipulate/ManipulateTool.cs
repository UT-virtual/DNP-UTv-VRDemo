using System;
using Basis.Network.Core;
using DNPDemo.Pen;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Manipulate
{
    /// <summary>
    /// Runs <see cref="ManipulatePart"/>.
    /// <list type="bullet">
    /// <item>Every device: resolves the target, its constraint (<see cref="ManipulateConstraint"/>) and the ghost of the
    /// pose to reach (<see cref="ManipulateGhostView"/>) when the part begins. The object's pose at that moment is the
    /// starting pose the values count from.</item>
    /// <item>Actor's device: while the pen is in manipulate mode, the trigger grabs the object when the tip touches its
    /// grab zone, and the object follows the tip within the constraint (<see cref="ManipulateGrabber"/>). The object
    /// lights up while it can be grabbed and while it is held. On release the constraint judges the pose: a success snaps
    /// to the goal (or stays, for a threshold) and completes the part; otherwise the object stays where it was released
    /// and can be grabbed again.</item>
    /// <item>Other devices: move the object to the values the actor sends, smoothing the drags, and light it up while the
    /// actor holds it.</item>
    /// </list>
    /// Drags are sent unreliably at up to <see cref="_sendRate"/> per second; releases and snaps reliably
    /// (<see cref="ManipulateMessage"/>). A device that enters the part late receives the current value
    /// (<see cref="CreateSnapshot"/>).
    /// <para>
    /// The tool never puts the object back: when the step moves on, <see cref="ScenarioStage.ResetToBaseline"/> returns
    /// every keyed object to its pose at scene load and the step's cues show the result. The runner begins a part after
    /// the stage has been rebuilt for the step (<see cref="ScenarioPlayer"/> subscribes to the step change in OnEnable,
    /// <see cref="InteractionRunner"/> in Start), so the starting pose is the one the step shows. The object keeps its
    /// final pose after the part ends; the ghost is hidden.
    /// </para>
    /// </summary>
    public sealed class ManipulateTool : InteractionTool
    {
        [SerializeField] SpatialPenController _pen;

        [Tooltip("Resolves the part's keys to scene objects.")]
        [SerializeField] ScenarioStage _stage;

        [SerializeField] ManipulateGhostView _ghost;

        [Tooltip("Most drag updates sent per second.")]
        [SerializeField, Range(1f, 60f)] float _sendRate = 20f;

        [Tooltip("Seconds the object takes to snap to the goal.")]
        [SerializeField, Min(0f)] float _snapSeconds = 0.25f;

        [Tooltip("How quickly the object catches up with received drags (per second; higher is tighter).")]
        [SerializeField, Min(1f)] float _followSharpness = 20f;

        readonly ManipulateHighlight _highlight = new();

        ManipulateGrabber _grabber;
        InteractionSession _session;
        ManipulatePart _part;

        // What the current part works on. Kept across a restart of the same part (a role change), dropped on Discard.
        ManipulatePart _resolvedPart;
        ManipulateConstraint _constraint;
        ManipulateMotion _motion;
        IDetectionVolume _grabZone;
        ManipulateValue _value;

        // Sending (actor) and receiving (observers).
        ushort _counter;
        ushort _latestReceived;
        bool _hasReceived;
        bool _dirty;
        float _lastSendTime;

        void Awake()
        {
            if (_pen == null || _stage == null || _ghost == null)
            {
                Debug.LogError("[DNPDemo] ManipulateTool needs SpatialPenController, ScenarioStage and ManipulateGhostView references.", this);
                return;
            }
            _grabber = new ManipulateGrabber(_pen);
            _grabber.Grabbed += HandleGrabbed;
            _grabber.Released += HandleReleased;
        }

        void OnDestroy()
        {
            if (_grabber != null)
            {
                _grabber.Grabbed -= HandleGrabbed;
                _grabber.Released -= HandleReleased;
                _grabber.Stop();
            }
            _highlight.Set(false);
        }

        void LateUpdate()
        {
            if (_grabber != null && _session != null && _session.IsLocalActor)
            {
                _grabber.Tick();
                if (_grabber.IsGrabbing && _motion != null)
                {
                    SetValue(_grabber.Value);
                    _motion.Place(_constraint.PoseOf(_value));
                    if (_dirty && Time.unscaledTime - _lastSendTime >= 1f / _sendRate)
                    {
                        Send(ManipulateMessageKind.Drag, DeliveryMethod.Unreliable);
                    }
                }
                _highlight.Set(_grabber.IsHovering || _grabber.IsGrabbing);
            }
            _motion?.Tick(Time.deltaTime, _followSharpness);
        }

        public override bool CanRun(InteractionPart part) => part is ManipulatePart;

        public override void Begin(InteractionSession session)
        {
            StopInput();
            _session = session;
            _part = (ManipulatePart)session.Part;
            if (_part != _resolvedPart || _constraint == null)
            {
                Resolve(_part);
            }
            if (_constraint == null)
            {
                return;
            }
            if (_ghost != null)
            {
                _ghost.Show(_motion.Target, _constraint.GuidePose);
            }
            if (session.IsLocalActor && _grabber != null)
            {
                _grabber.Listen(_part.Mode, _constraint, _grabZone, _value);
            }
        }

        public override void End()
        {
            StopInput();
            HideGhost();
        }

        public override void Discard()
        {
            StopInput();
            HideGhost();
            _motion?.Stop();
            _highlight.Bind(null);
            _resolvedPart = null;
            _constraint = null;
            _motion = null;
            _grabZone = null;
            _value = default;
            _hasReceived = false;
        }

        public override void OnRemoteProgress(InteractionSession session, ArraySegment<byte> payload)
        {
            if (_constraint == null || session != _session)
            {
                return;
            }
            if (!ManipulateMessage.TryParse(payload, out ManipulateMessage message))
            {
                Debug.LogWarning("[DNPDemo] ManipulateTool: 操作のデータを読み取れませんでした。", this);
                return;
            }
            if (_hasReceived && !ManipulateMessage.IsNewer(message.Counter, _latestReceived))
            {
                return;
            }
            _hasReceived = true;
            _latestReceived = message.Counter;
            _value = _constraint.Clamp(message.Value);
            Pose pose = _constraint.PoseOf(_value);
            switch (message.Kind)
            {
                case ManipulateMessageKind.Drag:
                case ManipulateMessageKind.Release:
                    _motion.Follow(pose);
                    break;
                case ManipulateMessageKind.Snap:
                    _motion.Animate(pose, _snapSeconds);
                    break;
                case ManipulateMessageKind.Snapshot:
                    _motion.Place(pose);
                    break;
            }
            _highlight.Set(message.IsGrabbing);
        }

        public override byte[] CreateSnapshot(InteractionSession session)
        {
            if (session != _session || _constraint == null)
            {
                return null;
            }
            bool grabbing = _grabber != null && _grabber.IsGrabbing;
            return new ManipulateMessage(ManipulateMessageKind.Snapshot, ++_counter, grabbing, _value).Serialize();
        }

        void HandleGrabbed()
        {
            if (IsActive())
            {
                _motion.Stop();
                Send(ManipulateMessageKind.Drag, DeliveryMethod.Unreliable);
            }
        }

        void HandleReleased(ManipulateValue value, bool intended)
        {
            if (!IsActive())
            {
                return;
            }
            SetValue(value);
            _motion.Place(_constraint.PoseOf(_value));
            if (!intended)
            {
                Send(ManipulateMessageKind.Release, DeliveryMethod.ReliableOrdered);
                return;
            }
            if (!_constraint.TryComplete(_value, out ManipulateValue result))
            {
                Debug.Log($"[DNPDemo] 操作はまだ完了していません。{_constraint.Describe(_value)}", this);
                Send(ManipulateMessageKind.Release, DeliveryMethod.ReliableOrdered);
                return;
            }
            Debug.Log($"[DNPDemo] 操作を確定しました。{_constraint.Describe(_value)}", this);
            SetValue(result);
            _grabber.SetValue(result);
            if (result.Snapped)
            {
                _motion.Animate(_constraint.PoseOf(result), _snapSeconds);
                Send(ManipulateMessageKind.Snap, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Send(ManipulateMessageKind.Release, DeliveryMethod.ReliableOrdered);
            }
            _session.Complete();
        }

        /// <summary>
        /// Looks up the part's objects and captures the target's current pose as the start. If any is missing the part
        /// cannot be operated and has to be skipped by the operator.
        /// </summary>
        void Resolve(ManipulatePart part)
        {
            _resolvedPart = part;
            _constraint = null;
            _motion?.Stop();
            _motion = null;
            _grabZone = null;
            _value = default;
            _hasReceived = false;
            _highlight.Bind(null);

            if (!TryGetTransform(part.TargetKey, out Transform target))
            {
                Fail("動かす対象");
                return;
            }
            Transform goal = null;
            Transform pivot = null;
            if (part.Constraint == ManipulateConstraintKind.Translate && !TryGetTransform(part.GoalKey, out goal))
            {
                Fail("平行移動の目標マーカー");
                return;
            }
            if (part.Constraint == ManipulateConstraintKind.Rotate && !TryGetTransform(part.PivotKey, out pivot))
            {
                Fail("回転の支点");
                return;
            }
            ManipulateConstraint constraint = ManipulateConstraint.FromPart(part, new Pose(target.position, target.rotation), goal, pivot);
            if (constraint == null)
            {
                Fail("制約のパラメータ");
                return;
            }
            if (!constraint.IsGoalReachable)
            {
                Debug.LogWarning($"[DNPDemo] 目標マーカー「{part.GoalKey}」が最大距離 {part.MaxDistance:0.##}m より遠いため、届きません。", this);
            }
            _grabZone = DetectionVolumes.Resolve(_stage, part.EffectiveGrabZoneKey);
            if (_grabZone == null)
            {
                Debug.LogError($"[DNPDemo] 操作の掴む範囲「{part.EffectiveGrabZoneKey}」が見つからないため、掴めません。運営スキップで進めてください。", this);
            }
            _constraint = constraint;
            _motion = new ManipulateMotion(target);
            _highlight.Bind(target);
        }

        bool TryGetTransform(string key, out Transform result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(key) || !_stage.TryGetObject(key, out GameObject target))
            {
                return false;
            }
            result = target.transform;
            return true;
        }

        void Fail(string what)
        {
            Debug.LogError($"[DNPDemo] {what}が見つからないため、この操作は行えません。運営スキップで進めてください。", this);
        }

        void SetValue(ManipulateValue value)
        {
            if (!value.Equals(_value))
            {
                _value = value;
                _dirty = true;
            }
        }

        void Send(ManipulateMessageKind kind, DeliveryMethod deliveryMethod)
        {
            _lastSendTime = Time.unscaledTime;
            _dirty = false;
            bool grabbing = _grabber != null && _grabber.IsGrabbing;
            _session.SendProgress(new ManipulateMessage(kind, ++_counter, grabbing, _value).Serialize(), deliveryMethod);
        }

        bool IsActive() => _session != null && _session.IsCurrent && _session.IsLocalActor && _constraint != null;

        void StopInput()
        {
            _grabber?.Stop();
            _highlight.Set(false);
            _session = null;
            _part = null;
        }

        void HideGhost()
        {
            if (_ghost != null)
            {
                _ghost.Hide();
            }
        }
    }
}
