using System;
using System.Collections.Generic;
using Basis.Network.Core;
using DNPDemo.Pen;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>
    /// Runs <see cref="SelectionPart"/>.
    /// <list type="bullet">
    /// <item>Actor's device: while the pen is in select mode and its trigger is held, the swept tip sphere is recorded
    /// and shown (<see cref="SelectionRecorder"/>). On release the selection is judged against the part's zones
    /// (<see cref="SelectionJudge"/>). A success stays on screen, is sent to the other devices and completes the part;
    /// a failure fades out and the actor tries again.</item>
    /// <item>Other devices: show the confirmed selection the actor sent. While it is being drawn they see only the
    /// actor's pen.</item>
    /// </list>
    /// The selection stays visible after the part ends (during a following voice command, for example) and is removed
    /// when the step moves on.
    /// </summary>
    public sealed class SelectionTool : InteractionTool
    {
        [SerializeField] SpatialPenController _pen;

        [Tooltip("Resolves the part's zone keys to scene objects.")]
        [SerializeField] ScenarioStage _stage;

        [SerializeField] SelectionVolumeView _view;

        [Tooltip("Distance between recorded spheres, as a fraction of the radius.")]
        [SerializeField, Range(0.1f, 2f)] float _spacingFactor = SelectionSampler.DefaultSpacingFactor;

        [Tooltip("Most spheres one selection records. Bounds the network message and the draw cost.")]
        [SerializeField, Range(1, 1023)] int _maxSamples = SelectionSampler.DefaultMaxSamples;

        [Tooltip("Seconds a failed selection takes to fade out.")]
        [SerializeField, Min(0f)] float _rejectFadeSeconds = 0.4f;

        readonly List<IDetectionVolume> _targets = new();
        readonly List<SelectionSample> _received = new();

        SelectionRecorder _recorder;
        InteractionSession _session;
        SelectionPart _part;
        bool _targetsComplete;

        void Awake()
        {
            if (_pen == null || _stage == null || _view == null)
            {
                Debug.LogError("[DNPDemo] SelectionTool needs SpatialPenController, ScenarioStage and SelectionVolumeView references.", this);
                return;
            }
            _recorder = new SelectionRecorder(_pen, _view, new SelectionSampler(_spacingFactor, _maxSamples));
            _recorder.Finished += HandleFinished;
        }

        void OnDestroy()
        {
            if (_recorder != null)
            {
                _recorder.Finished -= HandleFinished;
                _recorder.Stop();
            }
        }

        void LateUpdate()
        {
            _recorder?.Tick();
        }

        public override bool CanRun(InteractionPart part) => part is SelectionPart;

        public override void Begin(InteractionSession session)
        {
            StopInput();
            _session = session;
            _part = (SelectionPart)session.Part;
            ClearView();
            if (session.IsLocalActor && _recorder != null)
            {
                ResolveTargets();
                _recorder.Listen(_part.Mode);
            }
        }

        public override void End() => StopInput();

        public override void Discard()
        {
            StopInput();
            ClearView();
        }

        public override void OnRemoteProgress(InteractionSession session, ArraySegment<byte> payload)
        {
            if (_view == null)
            {
                return;
            }
            if (SelectionMessage.TryParse(payload, _received))
            {
                _view.Show(_received);
            }
            else
            {
                Debug.LogWarning("[DNPDemo] SelectionTool: 範囲選択のデータを読み取れませんでした。", this);
            }
        }

        void HandleFinished(IReadOnlyList<SelectionSample> samples)
        {
            if (_session == null || !_session.IsCurrent || _part == null)
            {
                return;
            }
            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, _targets, _part.Criteria);
            if (!_targetsComplete || !verdict.Succeeded)
            {
                Debug.Log($"[DNPDemo] 範囲選択をやり直します。{verdict}", this);
                _view.Clear(_rejectFadeSeconds);
                return;
            }
            Debug.Log($"[DNPDemo] 範囲選択を確定しました。{verdict}", this);
            InteractionSession session = _session;
            session.SendProgress(SelectionMessage.Serialize(samples), DeliveryMethod.ReliableOrdered);
            session.Complete();
        }

        /// <summary>
        /// Looks up the part's zones. If any key cannot be resolved, every selection fails: judging against the zones
        /// that were found would accept a selection that misses the missing one.
        /// </summary>
        void ResolveTargets()
        {
            _targets.Clear();
            _targetsComplete = _part.TargetZoneKeys != null && _part.TargetZoneKeys.Count > 0;
            if (_part.TargetZoneKeys != null)
            {
                foreach (string key in _part.TargetZoneKeys)
                {
                    IDetectionVolume volume = null;
                    if (_stage.TryGetObject(key, out GameObject target))
                    {
                        volume = DetectionVolumes.Find(target);
                        if (volume == null)
                        {
                            Debug.LogError($"[DNPDemo] キー「{key}」のオブジェクトに DetectionZoneGroup も DetectionZone もありません。", target);
                        }
                    }
                    if (volume == null)
                    {
                        _targetsComplete = false;
                        continue;
                    }
                    _targets.Add(volume);
                }
            }
            if (!_targetsComplete)
            {
                Debug.LogError("[DNPDemo] 範囲選択の対象ゾーンがそろっていないため、判定は常に失敗します。運営スキップで進めてください。", this);
            }
        }

        void StopInput()
        {
            _recorder?.Stop();
            _session = null;
            _part = null;
        }

        void ClearView()
        {
            if (_view != null)
            {
                _view.Clear();
            }
        }
    }
}
