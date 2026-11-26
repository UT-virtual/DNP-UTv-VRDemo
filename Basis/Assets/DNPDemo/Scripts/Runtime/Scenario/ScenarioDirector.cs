using System;
using System.Collections.Generic;
using Basis;
using Basis.Network.Core;
using DNPDemo.Roles;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// Owns scenario progress and keeps it in step across devices.
    /// <para>
    /// The device with scenario authority (decided by <see cref="DemoRoleService"/>) holds the true
    /// (sequence, step) and broadcasts every change reliably. Other devices only copy it, report completed
    /// interactions, and forward operator commands. A device that joins or reconnects asks for the state when
    /// its network id is ready, rather than relying on a push at join time that could arrive before it listens.
    /// </para>
    /// <para>
    /// The run is meant for both players. While one of them is missing, a step whose cues have finished does not move
    /// on; when the missing player comes back, the current step plays again from the start for both.
    /// </para>
    /// <para>
    /// The authority stores the step it is at (<see cref="ScenarioCheckpoint"/>). When it starts a run and the stored
    /// step shows that the previous run was cut short, it stays in the lobby and offers that step as
    /// <see cref="ResumeStepIndex"/> until the operator resumes there or starts anew. While that offer stands, the stored
    /// step is left as it is, so the offer survives another restart or the host leaving and rejoining the session.
    /// </para>
    /// <para>
    /// Everything visible derives from the step index (see <see cref="ScenarioPlayer"/>), so this class never
    /// sends presentation state.
    /// </para>
    /// </summary>
    public sealed class ScenarioDirector : BasisNetworkBehaviour
    {
        /// <summary>The first step is the lobby: the run starts there and <see cref="OperatorCommand.Restart"/> returns there.</summary>
        public const int LobbyStepIndex = 0;

        [SerializeField] ScenarioData _scenario;
        [SerializeField] DemoRoleService _roles;

        ScenarioProgress _progress;
        int _resumeStepIndex = -1;
        bool _allRolesPresent;
        readonly ushort[] _singleRecipient = new ushort[1];

        public ScenarioData Scenario => _scenario;
        public bool IsRunning => _progress != null && _progress.IsRunning;
        public int StepIndex => _progress?.StepIndex ?? -1;
        public int StepCount => _progress?.StepCount ?? 0;
        public ushort Sequence => _progress?.Sequence ?? 0;
        public ScenarioStep CurrentStep => IsRunning ? _scenario.Steps[_progress.StepIndex] : null;

        /// <summary>True while the run is at the lobby step.</summary>
        public bool IsAtLobby => IsRunning && _progress.StepIndex == LobbyStepIndex;

        /// <summary>True on the device that decides progress.</summary>
        public bool HasAuthority => _roles != null && _roles.HasScenarioAuthority;

        /// <summary>
        /// The step where the previous run was interrupted, offered while this run is still in the lobby; -1 when there is
        /// nothing to resume. Known on every device (the authority sends it with the state).
        /// </summary>
        public int ResumeStepIndex => _resumeStepIndex;

        /// <summary>
        /// True on the authority while a player is missing: the current step does not move on when its cues finish, and it
        /// plays again from the start when the player comes back.
        /// </summary>
        public bool IsWaitingForPartner => HasAuthority && IsRunning && !IsAtLobby && !_allRolesPresent;

        /// <summary>Raised after the current step changes, and when the run stops (<see cref="IsRunning"/> false).</summary>
        public event Action StepChanged;

        /// <summary>
        /// Raised when something the operator panel shows changes without a step change: <see cref="ResumeStepIndex"/>
        /// or <see cref="IsWaitingForPartner"/>.
        /// </summary>
        public event Action StatusChanged;

        void Awake()
        {
            if (_scenario == null || _roles == null)
            {
                Debug.LogError("[DNPDemo] ScenarioDirector needs ScenarioData and DemoRoleService references.", this);
                enabled = false;
                return;
            }
            List<string> problems = _scenario.Validate();
            foreach (string problem in problems)
            {
                Debug.LogError($"[DNPDemo] {_scenario.name}: {problem}", _scenario);
            }
            _progress = new ScenarioProgress(_scenario.Steps.Count);
        }

        public override void Start()
        {
            if (_progress == null)
            {
                return;
            }
            _roles.LocalRoleChanged += HandleLocalRoleChanged;
            _roles.RolesChanged += HandleRolesChanged;
            base.Start();
        }

        public override void OnDestroy()
        {
            if (_roles != null)
            {
                _roles.LocalRoleChanged -= HandleLocalRoleChanged;
                _roles.RolesChanged -= HandleRolesChanged;
            }
            base.OnDestroy();
        }

        public override void OnNetworkReady()
        {
            if (HasAuthority)
            {
                BeginAsAuthority();
            }
            else
            {
                Send(ScenarioMessage.RequestState(), null);
            }
        }

        public override void OnNetworkMessage(ushort playerId, byte[] buffer, DeliveryMethod deliveryMethod)
        {
            if (_progress == null || !ScenarioMessage.TryParse(buffer, out ScenarioMessage message))
            {
                return;
            }
            switch (message.Type)
            {
                case ScenarioMessageType.State:
                    if (!HasAuthority)
                    {
                        SetResumeStep(ValidResumeIndex(message.ResumeStepIndex));
                        if (_progress.Apply(message.Sequence, message.StepIndex))
                        {
                            RaiseStepChanged();
                        }
                    }
                    break;
                case ScenarioMessageType.RequestState:
                    if (HasAuthority && IsRunning)
                    {
                        _singleRecipient[0] = playerId;
                        Send(CreateStateMessage(), _singleRecipient);
                    }
                    break;
                case ScenarioMessageType.ReportComplete:
                    if (HasAuthority)
                    {
                        TryCompleteInteraction(message.Sequence, message.StepIndex, _roles.GetRole(playerId));
                    }
                    break;
                case ScenarioMessageType.Command:
                    if (HasAuthority)
                    {
                        Execute(message.Command, message.Sequence, message.StepIndex, message.TargetStepIndex);
                    }
                    break;
            }
        }

        /// <summary>Operator command (next / back / restart) from any device, applied to the step currently shown here.</summary>
        public void RequestCommand(OperatorCommand command)
        {
            if (command == OperatorCommand.JumpTo)
            {
                Debug.LogError("[DNPDemo] Use RequestJumpTo to jump to a step.", this);
                return;
            }
            RequestCommand(command, -1);
        }

        /// <summary>Operator command from any device: go to <paramref name="stepIndex"/>, or play it again if it is the current step.</summary>
        public void RequestJumpTo(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= StepCount)
            {
                return;
            }
            RequestCommand(OperatorCommand.JumpTo, stepIndex);
        }

        /// <summary>Called by the interaction tools when the local player finished the current step's operation.</summary>
        public void ReportInteractionComplete()
        {
            if (!IsRunning)
            {
                return;
            }
            if (HasAuthority)
            {
                TryCompleteInteraction(_progress.Sequence, _progress.StepIndex, _roles.LocalRole);
            }
            else
            {
                Send(ScenarioMessage.ReportComplete(_progress.Sequence, _progress.StepIndex), null);
            }
        }

        /// <summary>Called by <see cref="ScenarioPlayer"/> when the cues of (<paramref name="sequence"/>, <paramref name="stepIndex"/>) have finished locally.</summary>
        public void NotifyCuesFinished(ushort sequence, int stepIndex)
        {
            if (!HasAuthority || !_progress.IsCurrent(sequence, stepIndex)
                || CurrentStep.Completion != StepCompletion.CuesFinished)
            {
                return;
            }
            if (!_allRolesPresent)
            {
                Debug.Log($"[DNPDemo] Scenario step {stepIndex} ({CurrentStep.Id}) holds until both players are present.");
                return;
            }
            Advance(_progress.Next());
        }

        void RequestCommand(OperatorCommand command, int targetStepIndex)
        {
            if (!IsRunning)
            {
                return;
            }
            if (HasAuthority)
            {
                Execute(command, _progress.Sequence, _progress.StepIndex, targetStepIndex);
            }
            else
            {
                Send(ScenarioMessage.CommandFor(command, _progress.Sequence, _progress.StepIndex, targetStepIndex), null);
            }
        }

        void Execute(OperatorCommand command, ushort sequence, int stepIndex, int targetStepIndex)
        {
            if (!_progress.IsCurrent(sequence, stepIndex))
            {
                return;
            }
            switch (command)
            {
                case OperatorCommand.Next:
                    Advance(_progress.Next());
                    break;
                case OperatorCommand.Back:
                    Advance(_progress.Back());
                    break;
                case OperatorCommand.Restart:
                    Advance(_progress.Restart());
                    break;
                case OperatorCommand.JumpTo:
                    Advance(_progress.JumpTo(targetStepIndex));
                    break;
            }
        }

        void TryCompleteInteraction(ushort sequence, int stepIndex, DemoRole reporter)
        {
            if (_progress.IsCurrent(sequence, stepIndex) && CurrentStep.AcceptsInteractionFrom(reporter))
            {
                Advance(_progress.Next());
            }
        }

        void BeginAsAuthority()
        {
            if (!HasNetworkID || IsRunning)
            {
                return;
            }
            // Read before the start overwrites the checkpoint with the lobby.
            SetResumeStep(ScenarioCheckpoint.ResolveResumeIndex(_scenario.Steps, ScenarioCheckpoint.Load()));
            if (_resumeStepIndex >= 0)
            {
                Debug.Log($"[DNPDemo] The previous run stopped at step {_resumeStepIndex} ({_scenario.Steps[_resumeStepIndex].Id}); it can be resumed from the operator panel.");
            }
            Advance(_progress.Start());
            if (_allRolesPresent && _resumeStepIndex < 0 && CurrentStep.Completion == StepCompletion.PlayersReady)
            {
                // Both players were already here before the run started, so the arrival has happened.
                Advance(_progress.Next());
            }
        }

        void Advance(bool changed)
        {
            if (!changed)
            {
                return;
            }
            if (!IsAtLobby)
            {
                // Leaving the lobby by any route starts a run, so the interrupted one is no longer on offer.
                SetResumeStep(-1);
            }
            Debug.Log($"[DNPDemo] Scenario step {_progress.StepIndex} ({CurrentStep.Id}), seq {_progress.Sequence}");
            if (_resumeStepIndex < 0)
            {
                // While an interrupted run is on offer (only ever in the lobby), the stored step is still that run's.
                // Overwriting it with the lobby would lose it if the app restarts again before the operator resumes.
                ScenarioCheckpoint.Save(CurrentStep.Id);
            }
            Send(CreateStateMessage(), null);
            RaiseStepChanged();
        }

        ScenarioMessage CreateStateMessage() =>
            ScenarioMessage.State(_progress.Sequence, _progress.StepIndex, _resumeStepIndex);

        int ValidResumeIndex(int stepIndex) =>
            stepIndex > LobbyStepIndex && stepIndex < StepCount ? stepIndex : -1;

        void SetResumeStep(int stepIndex)
        {
            if (_resumeStepIndex == stepIndex)
            {
                return;
            }
            _resumeStepIndex = stepIndex;
            StatusChanged?.Invoke();
        }

        void HandleLocalRoleChanged(DemoRole role)
        {
            if (role == DemoRole.None)
            {
                SetResumeStep(-1);
                if (IsRunning)
                {
                    _progress.Stop();
                    RaiseStepChanged();
                }
                return;
            }
            if (HasAuthority)
            {
                BeginAsAuthority();
            }
        }

        void HandleRolesChanged()
        {
            bool wasPresent = _allRolesPresent;
            _allRolesPresent = _roles.AreAllRolesPresent;
            if (wasPresent == _allRolesPresent)
            {
                return;
            }
            StatusChanged?.Invoke();
            if (!_allRolesPresent || !HasAuthority || !IsRunning)
            {
                return;
            }
            if (CurrentStep.Completion == StepCompletion.PlayersReady)
            {
                // With an interrupted run on offer, the operator decides whether to resume it or start anew.
                if (_resumeStepIndex < 0)
                {
                    Advance(_progress.Next());
                }
            }
            else
            {
                // The returning player missed part of this step; play it again from the start for both.
                Advance(_progress.JumpTo(_progress.StepIndex));
            }
        }

        void Send(ScenarioMessage message, ushort[] recipients)
        {
            if (HasNetworkID)
            {
                SendCustomNetworkEvent(message.Serialize(), DeliveryMethod.ReliableOrdered, recipients);
            }
        }

        void RaiseStepChanged() => StepChanged?.Invoke();
    }
}
