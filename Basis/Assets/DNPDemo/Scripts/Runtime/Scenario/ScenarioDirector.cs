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
    /// Everything visible derives from the step index (see <see cref="ScenarioPlayer"/>), so this class never
    /// sends presentation state.
    /// </para>
    /// </summary>
    public sealed class ScenarioDirector : BasisNetworkBehaviour
    {
        [SerializeField] ScenarioData _scenario;
        [SerializeField] DemoRoleService _roles;

        ScenarioProgress _progress;
        readonly ushort[] _singleRecipient = new ushort[1];

        public ScenarioData Scenario => _scenario;
        public bool IsRunning => _progress != null && _progress.IsRunning;
        public int StepIndex => _progress?.StepIndex ?? -1;
        public int StepCount => _progress?.StepCount ?? 0;
        public ushort Sequence => _progress?.Sequence ?? 0;
        public ScenarioStep CurrentStep => IsRunning ? _scenario.Steps[_progress.StepIndex] : null;

        /// <summary>True on the device that decides progress.</summary>
        public bool HasAuthority => _roles != null && _roles.HasScenarioAuthority;

        /// <summary>Raised after the current step changes, and when the run stops (<see cref="IsRunning"/> false).</summary>
        public event Action StepChanged;

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
                    if (!HasAuthority && _progress.Apply(message.Sequence, message.StepIndex))
                    {
                        RaiseStepChanged();
                    }
                    break;
                case ScenarioMessageType.RequestState:
                    if (HasAuthority && IsRunning)
                    {
                        _singleRecipient[0] = playerId;
                        Send(ScenarioMessage.State(_progress.Sequence, _progress.StepIndex), _singleRecipient);
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
                        Execute(message.Command, message.Sequence, message.StepIndex);
                    }
                    break;
            }
        }

        /// <summary>Operator command (next / back / restart) from any device, applied to the step currently shown here.</summary>
        public void RequestCommand(OperatorCommand command)
        {
            if (!IsRunning)
            {
                return;
            }
            if (HasAuthority)
            {
                Execute(command, _progress.Sequence, _progress.StepIndex);
            }
            else
            {
                Send(ScenarioMessage.CommandFor(command, _progress.Sequence, _progress.StepIndex), null);
            }
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
            if (HasAuthority && _progress.IsCurrent(sequence, stepIndex)
                && CurrentStep.Completion == StepCompletion.CuesFinished)
            {
                Advance(_progress.Next());
            }
        }

        void Execute(OperatorCommand command, ushort sequence, int stepIndex)
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
            Advance(_progress.Start());
        }

        void Advance(bool changed)
        {
            if (!changed)
            {
                return;
            }
            Debug.Log($"[DNPDemo] Scenario step {_progress.StepIndex} ({CurrentStep.Id}), seq {_progress.Sequence}");
            Send(ScenarioMessage.State(_progress.Sequence, _progress.StepIndex), null);
            RaiseStepChanged();
        }

        void HandleLocalRoleChanged(DemoRole role)
        {
            if (role == DemoRole.None)
            {
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
            if (HasAuthority && IsRunning && CurrentStep.Completion == StepCompletion.PlayersReady
                && _roles.AreAllRolesPresent)
            {
                Advance(_progress.Next());
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
