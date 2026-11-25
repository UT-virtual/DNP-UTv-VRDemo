using DNPDemo.Environment;
using UnityEngine;

namespace DNPDemo.Interaction.Background
{
    /// <summary>
    /// Runs <see cref="BackgroundPart"/> through the <see cref="BackgroundPanel"/>. While the part is current, its
    /// <see cref="BackgroundPart.TargetPreset"/> is listed on the panel on every device. On the actor's device the tool
    /// also opens the panel with it selected (<see cref="BackgroundPart.OpensPanel"/>) and enables「背景を適用」; applying
    /// it closes the panel and completes the part.
    /// <para>
    /// The background changes at the head of the next step (<see cref="Scenario.Cues.EnvironmentCue"/>), so the tool sends
    /// nothing besides the completion and never changes the environment itself.
    /// </para>
    /// </summary>
    public sealed class BackgroundTool : InteractionTool
    {
        [SerializeField] BackgroundPanel _panel;

        InteractionSession _session;
        BackgroundPart _part;
        bool _accepting;

        /// <summary>Status line after applying a background other than the target.</summary>
        public static string FormatChooseTarget(EnvironmentPreset target) =>
            target != null ? $"{target.DisplayName}を選んでください" : string.Empty;

        void Awake()
        {
            if (_panel == null)
            {
                Debug.LogError("[DNPDemo] BackgroundTool needs a BackgroundPanel reference.", this);
            }
        }

        void OnDestroy() => StopAccepting();

        public override bool CanRun(InteractionPart part) => part is BackgroundPart;

        public override void Begin(InteractionSession session)
        {
            StopAccepting();
            _session = session;
            _part = (BackgroundPart)session.Part;
            if (_panel == null)
            {
                return;
            }
            _panel.SetCandidate(_part.TargetPreset);
            if (session.IsLocalActor)
            {
                _panel.ApplyRequested += HandleApplyRequested;
                _accepting = true;
                _panel.SetApplyAllowed(true);
                if (_part.OpensPanel)
                {
                    _panel.Open(_part.TargetPreset);
                }
            }
        }

        public override void End()
        {
            StopAccepting();
            _session = null;
        }

        public override void Discard()
        {
            StopAccepting();
            _session = null;
            _part = null;
            if (_panel != null)
            {
                _panel.SetCandidate(null);
            }
        }

        void HandleApplyRequested(EnvironmentPreset preset)
        {
            InteractionSession session = _session;
            if (session == null || !session.IsLocalActor || !session.IsCurrent || _part == null || preset == null)
            {
                return;
            }
            if (preset == _part.TargetPreset)
            {
                _panel.Close();
                session.Complete();
            }
            else
            {
                _panel.SetStatus(FormatChooseTarget(_part.TargetPreset));
            }
        }

        void StopAccepting()
        {
            if (!_accepting)
            {
                return;
            }
            _accepting = false;
            if (_panel != null)
            {
                _panel.ApplyRequested -= HandleApplyRequested;
                _panel.SetApplyAllowed(false);
            }
        }
    }
}
