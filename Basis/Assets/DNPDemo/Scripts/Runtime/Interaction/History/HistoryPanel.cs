using System;
using System.Collections.Generic;
using DNPDemo.Presentation;
using DNPDemo.Scenario;
using DNPDemo.Venus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// The history panel: opened from「モデルの履歴」on the main menu (<see cref="Menu.HistoryMenuProvider"/>), it is put in
    /// front of the view and left there to be pointed at with the laser. It shows one version of the main statue's history
    /// at a time as a turning miniature (<see cref="HistoryPreview"/>); ◀ ▶ step through the versions and × closes it.
    /// <para>
    /// Either player can open it at any time. The「モデルを復元」button stays greyed out until the scenario allows it:
    /// <see cref="HistoryTool"/> enables it on the actor's device during a <see cref="HistoryPart"/> and handles
    /// <see cref="RestoreRequested"/>. The panel itself changes nothing on stage and sends nothing.
    /// </para>
    /// <para>
    /// The history follows from the step index like every other lasting state (<see cref="VenusVersionHistory"/>), so it is
    /// the same on both devices and is rebuilt whenever the step changes.
    /// </para>
    /// </summary>
    public sealed class HistoryPanel : MonoBehaviour
    {
        public const string RestoreLabel = "モデルを復元";
        public const string EmptyMessage = "まだ履歴がありません";

        /// <summary>Opacity of the label or icon on a disabled button.</summary>
        const float DisabledContentAlpha = 0.35f;

        [SerializeField] ScenarioDirector _director;

        [Tooltip("Key of the statue whose versions are listed.")]
        [SerializeField] string _statueKey = VenusStatueKeys.Main;

        [SerializeField, Min(0.2f)] float _openDistance = 0.8f;
        [SerializeField] float _openHeightOffset = -0.15f;

        [Tooltip("The canvas, shown while the panel is open.")]
        [SerializeField] GameObject _content;

        [Tooltip("Keep it outside the canvas object and active at scene load, so its statue builds the models then rather than on the first open.")]
        [SerializeField] HistoryPreview _preview;
        [SerializeField] TMP_Text _versionText;
        [SerializeField] TMP_Text _pageText;
        [SerializeField] TMP_Text _emptyText;
        [SerializeField] TMP_Text _statusText;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _previousButton;
        [SerializeField] Button _nextButton;
        [SerializeField] Button _restoreButton;

        readonly HistoryBrowser _browser = new();
        bool _restoreAllowed;
        string _status = string.Empty;

        /// <summary>The panel in the loaded scene, for the main menu entry. Null when there is none.</summary>
        public static HistoryPanel Active { get; private set; }

        public bool IsOpen => _content != null && _content.activeSelf;

        /// <summary>The version on show, or null when there is no history yet.</summary>
        public VenusVersion Selected => _browser.Selected;

        /// <summary>True while the「モデルを復元」button is enabled by the scenario.</summary>
        public bool RestoreAllowed => _restoreAllowed;

        /// <summary>The player pressed「モデルを復元」while it was allowed. Carries the version on show.</summary>
        public event Action<VenusVersion> RestoreRequested;

        void Awake()
        {
            AddListener(_closeButton, Close);
            AddListener(_previousButton, GoBack);
            AddListener(_nextButton, GoForward);
            AddListener(_restoreButton, Restore);
            SetContentVisible(false);
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

        void OnDestroy()
        {
            RemoveListener(_closeButton, Close);
            RemoveListener(_previousButton, GoBack);
            RemoveListener(_nextButton, GoForward);
            RemoveListener(_restoreButton, Restore);
        }

        /// <summary>Puts the panel in front of the view showing the newest version. Opening it again brings it back in front.</summary>
        public void Open()
        {
            if (HeadPose.IsAvailable)
            {
                HeadPose.Place(transform, HeadPose.GetYaw(transform.eulerAngles.y), _openDistance, _openHeightOffset);
            }
            _browser.SetVersions(CollectVersions());
            _browser.SelectLatest();
            _status = string.Empty;
            SetContentVisible(true);
            Refresh();
        }

        public void Close() => SetContentVisible(false);

        /// <summary>Enables or greys out「モデルを復元」. Disallowing it also clears the status line.</summary>
        public void SetRestoreAllowed(bool allowed)
        {
            _restoreAllowed = allowed;
            if (!allowed)
            {
                _status = string.Empty;
            }
            Refresh();
        }

        /// <summary>A short line under「モデルを復元」, such as what the last restore did. Cleared when the version on show changes.</summary>
        public void SetStatus(string status)
        {
            _status = status ?? string.Empty;
            Refresh();
        }

        void HandleStepChanged()
        {
            if (IsOpen)
            {
                _browser.SetVersions(CollectVersions());
                Refresh();
            }
        }

        List<VenusVersion> CollectVersions()
        {
            if (_director == null || !_director.IsRunning || _director.Scenario == null)
            {
                return new List<VenusVersion>();
            }
            // A step's version cue plays at its start, so the version of the current step is already on the statue.
            return VenusVersionHistory.Collect(_director.Scenario.Steps, _director.StepIndex + 1, _statueKey);
        }

        void GoBack()
        {
            if (_browser.GoBack())
            {
                _status = string.Empty;
                Refresh();
            }
        }

        void GoForward()
        {
            if (_browser.GoForward())
            {
                _status = string.Empty;
                Refresh();
            }
        }

        void Restore()
        {
            VenusVersion selected = _browser.Selected;
            if (IsOpen && _restoreAllowed && selected != null)
            {
                RestoreRequested?.Invoke(selected);
            }
        }

        void SetContentVisible(bool visible)
        {
            if (_content != null)
            {
                _content.SetActive(visible);
            }
            if (!visible && _preview != null)
            {
                _preview.Show(null);
            }
        }

        void Refresh()
        {
            if (!IsOpen)
            {
                return;
            }
            VenusVersion selected = _browser.Selected;
            bool hasHistory = selected != null;
            SetText(_versionText, hasHistory ? selected.ToString() : string.Empty);
            SetText(_pageText, hasHistory ? $"{_browser.Index + 1} / {_browser.Versions.Count}" : string.Empty);
            SetText(_statusText, _status);
            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(!hasHistory);
            }
            SetInteractable(_previousButton, _browser.CanGoBack);
            SetInteractable(_nextButton, _browser.CanGoForward);
            SetInteractable(_restoreButton, _restoreAllowed && hasHistory);
            if (_preview != null)
            {
                _preview.Show(selected);
            }
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        /// <summary>
        /// Sets <paramref name="button"/>'s state. Its colour tint greys out only the target graphic, so the label or icon
        /// on it is faded here as well.
        /// </summary>
        static void SetInteractable(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }
            button.interactable = interactable;
            foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != button.targetGraphic)
                {
                    graphic.canvasRenderer.SetAlpha(interactable ? 1f : DisabledContentAlpha);
                }
            }
        }

        static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
