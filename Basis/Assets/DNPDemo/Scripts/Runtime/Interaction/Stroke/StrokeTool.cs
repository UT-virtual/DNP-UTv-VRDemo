using System;
using Basis.Network.Core;
using DNPDemo.Pen;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Stroke
{
    /// <summary>
    /// Runs <see cref="StrokePart"/>.
    /// <list type="bullet">
    /// <item>Actor's device: while the pen is in draw mode and its trigger is held, the tip draws a line
    /// (<see cref="StrokeRecorder"/>); with the trigger up, the eraser at the butt removes the lines it touches. Each
    /// released line is judged (<see cref="StrokeJudge"/>). A guide line that misses fades out; a sketch that is still
    /// too small stays so the actor can add to it. Success completes the part.</item>
    /// <item>Other devices: show the actor's lines as they are drawn, finished and erased.</item>
    /// </list>
    /// Lines being drawn are sent at most <see cref="_sendInterval"/> apart, as the points added since the last send.
    /// Everything goes reliably and in order, so the other devices end up with the same lines without a final resend.
    /// A device that enters the part late receives every line at once (<see cref="CreateSnapshot"/>).
    /// <para>
    /// The lines stay visible after the part ends (during a following voice command, for example) and are removed when
    /// the step moves on.
    /// </para>
    /// </summary>
    public sealed class StrokeTool : InteractionTool
    {
        [SerializeField] SpatialPenController _pen;

        [Tooltip("Resolves the guide's zone keys to scene objects.")]
        [SerializeField] ScenarioStage _stage;

        [SerializeField] StrokeView _view;

        [Tooltip("Seconds between sends of a line being drawn.")]
        [SerializeField, Range(0.02f, 0.5f)] float _sendInterval = 0.1f;

        [Tooltip("Seconds a guide line that missed takes to fade out.")]
        [SerializeField, Min(0f)] float _rejectFadeSeconds = 0.4f;

        readonly StrokeSet _set = new();

        StrokeRecorder _recorder;
        InteractionSession _session;
        StrokePart _part;
        IDetectionVolume _start;
        IDetectionVolume _end;

        // Points of the current line already sent, and when they were last sent.
        int _sentCount;
        float _lastSendTime;

        void Awake()
        {
            if (_pen == null || _stage == null || _view == null)
            {
                Debug.LogError("[DNPDemo] StrokeTool needs SpatialPenController, ScenarioStage and StrokeView references.", this);
                return;
            }
            _recorder = new StrokeRecorder(_pen, _view, _set);
            _recorder.Started += HandleStarted;
            _recorder.Finished += HandleFinished;
            _recorder.Abandoned += HandleRemoved;
            _recorder.Erased += HandleRemoved;
        }

        void OnDestroy()
        {
            if (_recorder != null)
            {
                _recorder.Started -= HandleStarted;
                _recorder.Finished -= HandleFinished;
                _recorder.Abandoned -= HandleRemoved;
                _recorder.Erased -= HandleRemoved;
                _recorder.Stop();
            }
        }

        void LateUpdate()
        {
            if (_recorder == null)
            {
                return;
            }
            _recorder.Tick();
            StrokeLine current = _recorder.Current;
            if (current != null && Time.unscaledTime - _lastSendTime >= _sendInterval)
            {
                SendPending(current);
            }
        }

        public override bool CanRun(InteractionPart part) => part is StrokePart;

        public override void Begin(InteractionSession session)
        {
            StopInput();
            _session = session;
            _part = (StrokePart)session.Part;
            ClearAll();
            if (session.IsLocalActor && _recorder != null)
            {
                ResolveZones();
                _recorder.Listen(_part.Mode);
            }
        }

        public override void End() => StopInput();

        public override void Discard()
        {
            StopInput();
            ClearAll();
        }

        public override void OnRemoteProgress(InteractionSession session, ArraySegment<byte> payload)
        {
            if (_view == null)
            {
                return;
            }
            if (!StrokeMessage.TryApply(payload, _set, out StrokeUpdate update))
            {
                Debug.LogWarning("[DNPDemo] StrokeTool: 線のデータを読み取れませんでした。", this);
                return;
            }
            switch (update.Kind)
            {
                case StrokeMessageKind.Append:
                    _view.Show(_set.Find(update.LineId));
                    break;
                case StrokeMessageKind.Erase:
                    _view.Hide(update.LineId, update.Fade ? _rejectFadeSeconds : 0f);
                    break;
                case StrokeMessageKind.Snapshot:
                    _view.Clear();
                    _view.Sync(_set);
                    break;
            }
        }

        public override byte[] CreateSnapshot(InteractionSession session)
        {
            if (session != _session || _set.Lines.Count == 0)
            {
                return null;
            }
            // The line being drawn goes in as it is now. The next Append may repeat some of its points; the receiver
            // skips those (StrokeMessage.TryApply).
            return StrokeMessage.Snapshot(_set);
        }

        void HandleStarted(StrokeLine line)
        {
            _sentCount = 0;
            SendPending(line);
        }

        void HandleFinished(StrokeLine line)
        {
            if (!IsActive())
            {
                return;
            }
            SendPending(line);
            Send(StrokeMessage.End(line.Id));

            StrokeVerdict verdict = _part.Goal == StrokeGoal.Guide
                ? StrokeJudge.Guide(line.Points, _start, _end, _view.Width * 0.5f)
                : StrokeJudge.Sketch(_set, _part.MinExtent);
            if (verdict.Succeeded)
            {
                Debug.Log($"[DNPDemo] 線画を確定しました。{verdict}", this);
                _session.Complete();
                return;
            }
            Debug.Log($"[DNPDemo] 線画はまだ完了していません。{verdict}", this);
            if (_part.Goal == StrokeGoal.Guide)
            {
                _set.Remove(line.Id);
                _view.Hide(line.Id, _rejectFadeSeconds);
                Send(StrokeMessage.Erase(line.Id, fade: true));
            }
        }

        void HandleRemoved(StrokeLine line)
        {
            if (IsActive())
            {
                Send(StrokeMessage.Erase(line.Id, fade: false));
            }
        }

        /// <summary>Sends the points of <paramref name="line"/> added since the last send, if any.</summary>
        void SendPending(StrokeLine line)
        {
            _lastSendTime = Time.unscaledTime;
            if (!IsActive() || line.Count <= _sentCount)
            {
                return;
            }
            Send(StrokeMessage.Append(line, _sentCount));
            _sentCount = line.Count;
        }

        void Send(byte[] payload) => _session.SendProgress(payload, DeliveryMethod.ReliableOrdered);

        bool IsActive() => _session != null && _session.IsCurrent && _session.IsLocalActor && _part != null;

        /// <summary>
        /// Looks up the guide's zones. If either cannot be resolved, every guide line fails: the line could not be shown
        /// to reach a zone that is not there.
        /// </summary>
        void ResolveZones()
        {
            _start = null;
            _end = null;
            if (_part.Goal != StrokeGoal.Guide)
            {
                return;
            }
            _start = DetectionVolumes.Resolve(_stage, _part.StartZoneKey);
            _end = DetectionVolumes.Resolve(_stage, _part.EndZoneKey);
            if (_start == null || _end == null)
            {
                Debug.LogError("[DNPDemo] ガイドの始点・終点ゾーンがそろっていないため、判定は常に失敗します。運営スキップで進めてください。", this);
            }
        }

        void StopInput()
        {
            _recorder?.Stop();
            _session = null;
            _part = null;
            _start = null;
            _end = null;
        }

        void ClearAll()
        {
            _set.Clear();
            _sentCount = 0;
            if (_view != null)
            {
                _view.Clear();
            }
        }
    }
}
