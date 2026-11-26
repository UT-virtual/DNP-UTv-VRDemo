using System;
using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Hint
{
    /// <summary>
    /// Shows the hint of the running interaction part (<see cref="InteractionPart.Hint"/>) when the part is still not done
    /// <see cref="InteractionHint.DelaySeconds"/> after it began: its zones light up (<see cref="HintZoneView"/>) and its
    /// text goes to the task HUD (<see cref="Text"/>, read by <see cref="TaskHud.TaskHudView"/>).
    /// <para>
    /// It runs on both devices, the actor's and the observer's, so the observer can help. Each device counts from when the
    /// part began there; nothing is sent over the network. The count starts again whenever the runner starts a part
    /// (<see cref="InteractionRunner.CurrentSession"/> changes), and the hint disappears when the part ends or the step
    /// moves on.
    /// </para>
    /// </summary>
    public sealed class InteractionHintPresenter : MonoBehaviour
    {
        [SerializeField] InteractionRunner _runner;

        [Tooltip("Resolves the hint's zone keys to scene objects.")]
        [SerializeField] ScenarioStage _stage;

        [SerializeField] HintZoneView _zoneView;

        readonly InteractionHintTimer _timer = new();
        readonly List<string> _keys = new();
        readonly List<IDetectionVolume> _volumes = new();

        InteractionSession _session;
        bool _visible;

        /// <summary>Raised when <see cref="Text"/> or <see cref="IsShown"/> changed.</summary>
        public event Action Changed;

        /// <summary>The hint of the running part is out.</summary>
        public bool IsShown => _visible;

        /// <summary>The hint's text while it is out; empty otherwise or when the hint has no text.</summary>
        public string Text { get; private set; } = string.Empty;

        void OnEnable()
        {
            if (_runner == null)
            {
                Debug.LogError("[DNPDemo] InteractionHintPresenter needs an InteractionRunner reference.", this);
                return;
            }
            _runner.StateChanged += Sync;
            Sync();
        }

        void OnDisable()
        {
            if (_runner != null)
            {
                _runner.StateChanged -= Sync;
            }
            _session = null;
            _timer.Clear();
            Hide();
        }

        void Update()
        {
            if (_timer.Tick(Time.unscaledTime))
            {
                Show();
            }
        }

        /// <summary>Restarts the count when the runner started another part (or none).</summary>
        void Sync()
        {
            InteractionSession session = _runner.CurrentSession;
            if (session == _session)
            {
                return;
            }
            _session = session;
            Hide();
            if (session != null)
            {
                _timer.Start(session.Part?.Hint, Time.unscaledTime);
            }
            else
            {
                _timer.Clear();
            }
        }

        void Show()
        {
            InteractionPart part = _session?.Part;
            if (part == null)
            {
                return;
            }
            Text = part.Hint?.Text?.Trim() ?? string.Empty;

            _keys.Clear();
            part.CollectHintZoneKeys(_keys);
            _volumes.Clear();
            foreach (string key in _keys)
            {
                IDetectionVolume volume = DetectionVolumes.Resolve(_stage, key);
                if (volume != null)
                {
                    _volumes.Add(volume);
                }
            }
            if (_zoneView != null)
            {
                _zoneView.Show(_volumes);
            }
            _visible = true;
            Debug.Log($"[DNPDemo] ステップ {_session.Step?.Id} の部品 {_session.PartIndex} のヒントを表示します（ゾーン {_volumes.Count} 個）。", this);
            Changed?.Invoke();
        }

        void Hide()
        {
            if (_zoneView != null)
            {
                _zoneView.Hide();
            }
            if (!_visible)
            {
                return;
            }
            _visible = false;
            Text = string.Empty;
            Changed?.Invoke();
        }
    }
}
