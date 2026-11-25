using System;
using System.Collections.Generic;
using DNPDemo.Environment;
using DNPDemo.Presentation;
using DNPDemo.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Interaction.Background
{
    /// <summary>
    /// The background panel: opened from「背景」on the main menu (<see cref="Menu.BackgroundMenuProvider"/>), it is put in
    /// front of the view and left there to be pointed at with the laser. × closes it.
    /// <para>
    /// The left column lists the backgrounds as cards (<see cref="BackgroundCard"/>): the panorama and the name, with the
    /// background on show framed. Pressing a card selects it; the right side shows the selected panorama large, with
    /// 「背景を適用」under it.
    /// </para>
    /// <para>
    /// The list follows from the step index (<see cref="BackgroundList"/>): the baseline of the environment controller,
    /// the backgrounds the scenario has applied, and the background generated during the current
    /// <see cref="BackgroundPart"/> (<see cref="SetCandidate"/>). It is rebuilt whenever the step changes.
    /// </para>
    /// <para>
    /// Either player can open it at any time.「背景を適用」stays greyed out until the scenario allows it:
    /// <see cref="BackgroundTool"/> enables it on the actor's device during a <see cref="BackgroundPart"/> and handles
    /// <see cref="ApplyRequested"/>. The background on show cannot be applied again. The panel itself changes nothing on
    /// stage and sends nothing.
    /// </para>
    /// </summary>
    public sealed class BackgroundPanel : DemoWorldPanel
    {
        public const string ApplyLabel = "背景を適用";
        public const string CurrentSuffix = "（表示中）";

        [SerializeField] ScenarioDirector _director;

        [Tooltip("Looks up the environment controller, whose baseline heads the list.")]
        [SerializeField] ScenarioStage _stage;

        [Tooltip("Key of the environment controller whose backgrounds are listed.")]
        [SerializeField] string _environmentKey = EnvironmentController.EnvironmentKey;

        [Tooltip("Parent of the cards. Its layout group places them.")]
        [SerializeField] RectTransform _cardContainer;

        [Tooltip("Inactive, outside the container. Cloned for each background.")]
        [SerializeField] BackgroundCard _cardTemplate;

        [SerializeField] RawImage _preview;
        [SerializeField] TMP_Text _nameText;
        [SerializeField] TMP_Text _statusText;
        [SerializeField] Button _applyButton;

        readonly List<EnvironmentPreset> _presets = new();
        readonly List<BackgroundCard> _cards = new();
        EnvironmentPreset _current;
        EnvironmentPreset _selected;
        EnvironmentPreset _candidate;
        bool _applyAllowed;
        string _status = string.Empty;

        /// <summary>The panel in the loaded scene, for the main menu entry. Null when there is none.</summary>
        public static BackgroundPanel Active { get; private set; }

        /// <summary>The backgrounds listed, in order. Up to date while the panel is open.</summary>
        public IReadOnlyList<EnvironmentPreset> Presets => _presets;

        /// <summary>The background on show, according to the step index.</summary>
        public EnvironmentPreset Current => _current;

        /// <summary>The background selected on the panel.</summary>
        public EnvironmentPreset Selected => _selected;

        /// <summary>True while「背景を適用」is enabled by the scenario.</summary>
        public bool ApplyAllowed => _applyAllowed;

        /// <summary>The player pressed「背景を適用」while it could be pressed. Carries the selected background.</summary>
        public event Action<EnvironmentPreset> ApplyRequested;

        protected override void Awake()
        {
            base.Awake();
            AddListener(_applyButton, Apply);
            if (_cardTemplate != null)
            {
                _cardTemplate.gameObject.SetActive(false);
            }
        }

        void OnEnable()
        {
            Active = this;
            if (_director != null)
            {
                _director.StepChanged += HandleStepChanged;
            }
        }

        void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }
            if (_director != null)
            {
                _director.StepChanged -= HandleStepChanged;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RemoveListener(_applyButton, Apply);
            foreach (BackgroundCard card in _cards)
            {
                if (card != null)
                {
                    card.Pressed -= Select;
                }
            }
        }

        /// <summary>
        /// Puts the panel in front of the view with <paramref name="selection"/> selected, or the background on show when
        /// it is null or not listed. Opening it again brings it back in front.
        /// </summary>
        public void Open(EnvironmentPreset selection = null)
        {
            RebuildList();
            _selected = selection != null && _presets.Contains(selection) ? selection : _current;
            _status = string.Empty;
            ShowInFrontOfView();
            Refresh();
        }

        /// <summary>Selects <paramref name="preset"/>. Does nothing when it is not listed.</summary>
        public void Select(EnvironmentPreset preset)
        {
            if (preset != null && _presets.Contains(preset) && preset != _selected)
            {
                _selected = preset;
                _status = string.Empty;
                Refresh();
            }
        }

        /// <summary>Lists <paramref name="preset"/> after the backgrounds the scenario has applied. Null removes it.</summary>
        public void SetCandidate(EnvironmentPreset preset)
        {
            if (preset == _candidate)
            {
                return;
            }
            _candidate = preset;
            if (IsOpen)
            {
                RebuildList();
                Refresh();
            }
        }

        /// <summary>Enables or greys out「背景を適用」. Disallowing it also clears the status line.</summary>
        public void SetApplyAllowed(bool allowed)
        {
            _applyAllowed = allowed;
            if (!allowed)
            {
                _status = string.Empty;
            }
            Refresh();
        }

        /// <summary>A short line under「背景を適用」. Cleared when the selection changes.</summary>
        public void SetStatus(string status)
        {
            _status = status ?? string.Empty;
            Refresh();
        }

        void HandleStepChanged()
        {
            if (IsOpen)
            {
                RebuildList();
                Refresh();
            }
        }

        void RebuildList()
        {
            IReadOnlyList<ScenarioStep> steps = null;
            int stepCount = 0;
            if (_director != null && _director.IsRunning && _director.Scenario != null)
            {
                steps = _director.Scenario.Steps;
                // A step's environment cue plays at its start, so the current step's background is already on show.
                stepCount = _director.StepIndex + 1;
            }
            EnvironmentPreset baseline = FindBaseline();
            _presets.Clear();
            _presets.AddRange(BackgroundList.Collect(steps, stepCount, _environmentKey, baseline, _candidate));
            _current = BackgroundList.FindCurrent(steps, stepCount, _environmentKey, baseline);
            if (_selected == null || !_presets.Contains(_selected))
            {
                _selected = _current;
            }
        }

        EnvironmentPreset FindBaseline()
        {
            if (_stage != null
                && _stage.TryGetObject(_environmentKey, out GameObject target)
                && target.TryGetComponent(out EnvironmentController controller))
            {
                return controller.BaselinePreset;
            }
            return null;
        }

        void Apply()
        {
            if (IsOpen && BackgroundList.CanApply(_applyAllowed, _selected, _current))
            {
                ApplyRequested?.Invoke(_selected);
            }
        }

        void Refresh()
        {
            if (!IsOpen)
            {
                return;
            }
            ShowCards();
            if (_preview != null)
            {
                Texture2D panorama = _selected != null ? _selected.Panorama : null;
                _preview.texture = panorama;
                _preview.enabled = panorama != null;
            }
            string name = _selected != null ? _selected.DisplayName : string.Empty;
            SetText(_nameText, _selected != null && _selected == _current ? name + CurrentSuffix : name);
            SetText(_statusText, _status);
            SetInteractable(_applyButton, BackgroundList.CanApply(_applyAllowed, _selected, _current));
        }

        void ShowCards()
        {
            if (_cardTemplate == null || _cardContainer == null)
            {
                return;
            }
            while (_cards.Count < _presets.Count)
            {
                BackgroundCard card = Instantiate(_cardTemplate, _cardContainer, false);
                card.name = $"Card{_cards.Count}";
                card.Pressed += Select;
                _cards.Add(card);
            }
            for (int i = 0; i < _cards.Count; i++)
            {
                bool used = i < _presets.Count;
                _cards[i].gameObject.SetActive(used);
                if (used)
                {
                    EnvironmentPreset preset = _presets[i];
                    _cards[i].Show(preset, preset == _current, preset == _selected);
                }
            }
        }
    }
}
