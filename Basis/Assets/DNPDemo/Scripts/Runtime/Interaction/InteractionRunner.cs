using System;
using System.Collections.Generic;
using Basis;
using Basis.Network.Core;
using DNPDemo.Roles;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// Runs the interaction parts of the current step (<see cref="ScenarioStep.Interaction"/>) on every device.
    /// <para>
    /// It hands each part to the tool that can run it (<see cref="InteractionTool"/>), tells the tool whether this device
    /// is the actor, and states the pen mode the part needs. On the actor's device a completed part moves on to the next one,
    /// and the last one reports the step done through <see cref="ScenarioDirector.ReportInteractionComplete"/>.
    /// </para>
    /// <para>
    /// It also carries all tool traffic: every message is pinned to (sequence, step, part) and dropped when that is no
    /// longer current, so tools never see progress from another step. Part changes come from the actor; a device that
    /// enters the step asks the actor where it is, which covers late joins and reconnects. The actor answers with the part
    /// index and the current tool's snapshot (<see cref="InteractionTool.CreateSnapshot"/>).
    /// </para>
    /// <para>
    /// In-progress state is discarded when the step changes; what remains visible afterwards derives from the step
    /// index (see <see cref="ScenarioPlayer"/>).
    /// </para>
    /// </summary>
    public sealed class InteractionRunner : BasisNetworkBehaviour
    {
        [SerializeField] ScenarioDirector _director;
        [SerializeField] DemoRoleService _roles;

        [Tooltip("Tools available for running parts. The first one whose CanRun accepts a part runs it.")]
        [SerializeField] List<InteractionTool> _tools = new();

        readonly InteractionProgress _progress = new();
        readonly List<InteractionTool> _startedTools = new();
        readonly HashSet<string> _reportedMissingTools = new();
        readonly ushort[] _singleRecipient = new ushort[1];

        InteractionSession _session;
        InteractionTool _currentTool;
        bool _ready;

        /// <summary>Raised when the current part, the actor flag or the required pen mode may have changed.</summary>
        public event Action StateChanged;

        /// <summary>The step whose parts are running, or null when nothing is waiting (same lifetime as <see cref="CurrentPart"/>).</summary>
        public ScenarioStep CurrentStep => _session?.Step;

        /// <summary>The current part, or null when nothing is waiting (no interaction step, or every part is done).</summary>
        public InteractionPart CurrentPart => _session?.Part;

        public int PartIndex => _progress.PartIndex;
        public int PartCount => _progress.PartCount;

        /// <summary>True on the device of the role that performs the current step.</summary>
        public bool IsLocalActor => _session != null && _session.IsLocalActor;

        /// <summary>Role that performs the current step, or None.</summary>
        public DemoRole Actor => _session?.Actor ?? DemoRole.None;

        /// <summary>
        /// The pen mode the local player needs for the current part: its mode on the actor's device, otherwise None.
        /// Tools take pen input only while the player's chosen mode (<see cref="Pen.PenModeSelection"/>) matches it.
        /// </summary>
        public PenMode RequiredPenMode => IsLocalActor ? _session.Part.Mode : PenMode.None;

        public override void Start()
        {
            if (_director == null || _roles == null)
            {
                Debug.LogError("[DNPDemo] InteractionRunner needs ScenarioDirector and DemoRoleService references.", this);
                enabled = false;
                return;
            }
            _ready = true;
            _director.StepChanged += Rebuild;
            _roles.LocalRoleChanged += HandleLocalRoleChanged;
            base.Start();
            Rebuild();
        }

        public override void OnDestroy()
        {
            if (_ready)
            {
                _director.StepChanged -= Rebuild;
                _roles.LocalRoleChanged -= HandleLocalRoleChanged;
                DiscardAll();
            }
            base.OnDestroy();
        }

        public override void OnNetworkReady()
        {
            if (_ready && _progress.StepIndex >= 0 && !IsActorForCurrentStep())
            {
                RequestPartState();
            }
        }

        public override void OnNetworkMessage(ushort playerId, byte[] buffer, DeliveryMethod deliveryMethod)
        {
            if (!_ready || !InteractionMessage.TryParse(buffer, out InteractionMessage message, out ArraySegment<byte> payload)
                || !_progress.IsCurrentStep(message.Sequence, message.StepIndex))
            {
                return;
            }
            ScenarioStep step = _director.CurrentStep;
            bool fromActor = step != null && _roles.GetRole(playerId) == step.Actor;
            switch (message.Type)
            {
                case InteractionMessageType.Progress:
                    if (fromActor && message.Part == _progress.PartIndex && _session != null && !_session.IsLocalActor && _currentTool != null)
                    {
                        Invoke(_currentTool, tool => tool.OnRemoteProgress(_session, payload));
                    }
                    break;
                case InteractionMessageType.PartAdvanced:
                    if (fromActor && !IsActorForCurrentStep()
                        && _progress.ApplyRemote(message.Sequence, message.StepIndex, message.Part))
                    {
                        StartCurrentPart();
                    }
                    break;
                case InteractionMessageType.RequestPartState:
                    if (IsActorForCurrentStep())
                    {
                        _singleRecipient[0] = playerId;
                        if (_progress.PartIndex > 0)
                        {
                            Send(new InteractionMessage(InteractionMessageType.PartAdvanced, _progress.Sequence, _progress.StepIndex, _progress.PartIndex),
                                null, DeliveryMethod.ReliableOrdered, _singleRecipient);
                        }
                        // After the part index, on the same ordered channel, so the device has begun this part when it arrives.
                        SendSnapshot(_singleRecipient);
                    }
                    break;
            }
        }

        internal bool IsCurrent(InteractionSession session) => session != null && session == _session;

        internal void Complete(InteractionSession session)
        {
            if (!IsCurrent(session) || !session.IsLocalActor || !_progress.CompletePart(session.PartIndex))
            {
                return;
            }
            Send(new InteractionMessage(InteractionMessageType.PartAdvanced, _progress.Sequence, _progress.StepIndex, _progress.PartIndex),
                null, DeliveryMethod.ReliableOrdered, null);
            StartCurrentPart();
            if (_progress.IsFinished)
            {
                _director.ReportInteractionComplete();
            }
        }

        internal void SendProgress(InteractionSession session, byte[] payload, DeliveryMethod deliveryMethod)
        {
            if (IsCurrent(session) && session.IsLocalActor)
            {
                Send(new InteractionMessage(InteractionMessageType.Progress, _progress.Sequence, _progress.StepIndex, session.PartIndex),
                    payload, deliveryMethod, null);
            }
        }

        /// <summary>Sends the current tool's snapshot (<see cref="InteractionTool.CreateSnapshot"/>) to <paramref name="recipients"/>.</summary>
        void SendSnapshot(ushort[] recipients)
        {
            if (_session == null || !_session.IsLocalActor || _currentTool == null)
            {
                return;
            }
            byte[] snapshot = null;
            InteractionSession session = _session;
            Invoke(_currentTool, tool => snapshot = tool.CreateSnapshot(session));
            if (snapshot != null)
            {
                Send(new InteractionMessage(InteractionMessageType.Progress, _progress.Sequence, _progress.StepIndex, session.PartIndex),
                    snapshot, DeliveryMethod.ReliableOrdered, recipients);
            }
        }

        void HandleLocalRoleChanged(DemoRole role) => Rebuild();

        /// <summary>
        /// Brings the tools in line with the current step. A new scenario state discards everything and starts from
        /// the first part; the same state (for example after the local role changed) restarts the current part.
        /// </summary>
        void Rebuild()
        {
            ScenarioStep step = _director.CurrentStep;
            int partCount = step?.Interaction?.Count ?? 0;
            if (step == null || step.Completion != StepCompletion.Interaction || partCount == 0)
            {
                DiscardAll();
                _progress.Clear();
                RaiseStateChanged();
                return;
            }
            if (!_progress.IsCurrentStep(_director.Sequence, _director.StepIndex))
            {
                DiscardAll();
                _progress.Begin(_director.Sequence, _director.StepIndex, partCount);
                if (!IsActorForCurrentStep())
                {
                    RequestPartState();
                }
            }
            StartCurrentPart();
        }

        /// <summary>Ends the running part and begins the current one, if any is left.</summary>
        void StartCurrentPart()
        {
            if (_currentTool != null)
            {
                Invoke(_currentTool, tool => tool.End());
            }
            _session = null;
            _currentTool = null;

            ScenarioStep step = _director.CurrentStep;
            if (!_progress.IsActive || step == null)
            {
                RaiseStateChanged();
                return;
            }
            InteractionPart part = step.Interaction[_progress.PartIndex];
            _session = new InteractionSession(this, step, part, _progress.PartIndex, _progress.PartCount, step.Actor, IsActorForCurrentStep());
            _currentTool = FindTool(part);
            if (_currentTool == null)
            {
                if (_reportedMissingTools.Add(step.Id + "/" + _progress.PartIndex))
                {
                    Debug.LogWarning($"[DNPDemo] ステップ {step.Id} の部品 {_progress.PartIndex}（{part}）を実行できるツールがありません。運営スキップで進めてください。", this);
                }
            }
            else
            {
                if (!_startedTools.Contains(_currentTool))
                {
                    _startedTools.Add(_currentTool);
                }
                InteractionSession session = _session;
                Invoke(_currentTool, tool => tool.Begin(session));
            }
            RaiseStateChanged();
        }

        void DiscardAll()
        {
            if (_currentTool != null)
            {
                Invoke(_currentTool, tool => tool.End());
            }
            _session = null;
            _currentTool = null;
            foreach (InteractionTool started in _startedTools)
            {
                if (started != null)
                {
                    Invoke(started, tool => tool.Discard());
                }
            }
            _startedTools.Clear();
        }

        InteractionTool FindTool(InteractionPart part)
        {
            if (part == null)
            {
                return null;
            }
            foreach (InteractionTool tool in _tools)
            {
                if (tool != null && tool.CanRun(part))
                {
                    return tool;
                }
            }
            return null;
        }

        bool IsActorForCurrentStep()
        {
            ScenarioStep step = _director.CurrentStep;
            return step != null && step.Actor != DemoRole.None && step.Actor == _roles.LocalRole;
        }

        void RequestPartState()
        {
            Send(new InteractionMessage(InteractionMessageType.RequestPartState, _progress.Sequence, _progress.StepIndex, 0),
                null, DeliveryMethod.ReliableOrdered, null);
        }

        void Send(InteractionMessage message, byte[] payload, DeliveryMethod deliveryMethod, ushort[] recipients)
        {
            if (HasNetworkID)
            {
                SendCustomNetworkEvent(message.Serialize(payload), deliveryMethod, recipients);
            }
        }

        /// <summary>Calls into a tool so that a failing tool cannot break the runner or the other tools.</summary>
        void Invoke(InteractionTool tool, Action<InteractionTool> call)
        {
            try
            {
                call(tool);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, tool);
            }
        }

        void RaiseStateChanged() => StateChanged?.Invoke();
    }
}
