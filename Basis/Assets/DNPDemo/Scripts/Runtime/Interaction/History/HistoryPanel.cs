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
    /// front of the view and left there to be pointed at with the laser. × closes it. It has two pages:
    /// <list type="bullet">
    /// <item><see cref="HistoryPanelPage.Tree"/>, shown on opening: every version of the main statue's history as a card
    /// in a branch tree (<see cref="HistoryTreeView"/>). Pressing a card shows it on the detail page.</item>
    /// <item><see cref="HistoryPanelPage.Detail"/>: one version at a time as a turning miniature (<see cref="HistoryPreview"/>);
    /// ◀ ▶ step through the versions in the order they were reached, and「一覧へ」goes back to the tree.</item>
    /// </list>
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
    public sealed class HistoryPanel : DemoWorldPanel
    {
        public const string RestoreLabel = "モデルを復元";
        public const string EmptyMessage = "まだ履歴がありません";
        public const string BackLabel = "一覧へ";

        [SerializeField] ScenarioDirector _director;

        [Tooltip("Key of the statue whose versions are listed.")]
        [SerializeField] string _statueKey = VenusStatueKeys.Main;

        [Tooltip("Keep it outside the canvas object and active at scene load, so its statue builds the models then rather than on the first open.")]
        [SerializeField] HistoryPreview _preview;

        [Tooltip("Shown on the tree page.")]
        [SerializeField] GameObject _treePage;
        [SerializeField] HistoryTreeView _treeView;

        [Tooltip("Shown on the detail page, with the preview.")]
        [SerializeField] GameObject _detailPage;
        [SerializeField] Button _backButton;
        [SerializeField] TMP_Text _versionText;
        [SerializeField] TMP_Text _pageText;
        [SerializeField] TMP_Text _emptyText;
        [SerializeField] TMP_Text _statusText;
        [SerializeField] Button _previousButton;
        [SerializeField] Button _nextButton;
        [SerializeField] Button _restoreButton;

        readonly HistoryBrowser _browser = new();
        HistoryPanelPage _page = HistoryPanelPage.Tree;
        bool _restoreAllowed;
        string _status = string.Empty;

        /// <summary>The panel in the loaded scene, for the main menu entry. Null when there is none.</summary>
        public static HistoryPanel Active { get; private set; }

        public HistoryPanelPage Page => _page;

        /// <summary>The version on show, or null when there is no history yet.</summary>
        public VenusVersion Selected => _browser.Selected;

        /// <summary>True while the「モデルを復元」button is enabled by the scenario.</summary>
        public bool RestoreAllowed => _restoreAllowed;

        /// <summary>The player pressed「モデルを復元」while it was allowed. Carries the version on show.</summary>
        public event Action<VenusVersion> RestoreRequested;

        protected override void Awake()
        {
            base.Awake();
            AddListener(_previousButton, GoBack);
            AddListener(_nextButton, GoForward);
            AddListener(_restoreButton, Restore);
            AddListener(_backButton, ShowTree);
            if (_treeView != null)
            {
                _treeView.VersionPressed += HandleVersionPressed;
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
            RemoveListener(_previousButton, GoBack);
            RemoveListener(_nextButton, GoForward);
            RemoveListener(_restoreButton, Restore);
            RemoveListener(_backButton, ShowTree);
            if (_treeView != null)
            {
                _treeView.VersionPressed -= HandleVersionPressed;
            }
        }

        /// <summary>
        /// Puts the panel in front of the view on the tree page, with the newest version selected for the detail page.
        /// Opening it again brings it back in front.
        /// </summary>
        public void Open()
        {
            _browser.SetVersions(CollectVersions());
            _browser.SelectLatest();
            _page = HistoryPanelPage.Tree;
            _status = string.Empty;
            ShowInFrontOfView();
            Refresh();
        }

        /// <summary>Shows <paramref name="version"/> on the detail page. Does nothing when it is not in the history.</summary>
        public void ShowDetail(VenusVersion version)
        {
            if (IsOpen && _browser.Select(version))
            {
                _page = HistoryPanelPage.Detail;
                _status = string.Empty;
                Refresh();
            }
        }

        /// <summary>Goes back to the tree page.</summary>
        public void ShowTree()
        {
            if (IsOpen && _page != HistoryPanelPage.Tree)
            {
                _page = HistoryPanelPage.Tree;
                _status = string.Empty;
                Refresh();
            }
        }

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

        void HandleVersionPressed(VenusVersion version) => ShowDetail(version);

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

        protected override void OnVisibilityChanged(bool visible)
        {
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
            if (!hasHistory)
            {
                // The operator moved the scenario back before the first version while the detail page was open.
                _page = HistoryPanelPage.Tree;
            }
            bool detail = _page == HistoryPanelPage.Detail;
            if (_treePage != null)
            {
                _treePage.SetActive(!detail);
            }
            if (_detailPage != null)
            {
                _detailPage.SetActive(detail);
            }
            if (!detail && _treeView != null)
            {
                _treeView.Show(_browser.Versions);
            }
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
                _preview.Show(detail ? selected : null);
            }
        }
    }

    /// <summary>The two pages of <see cref="HistoryPanel"/>.</summary>
    public enum HistoryPanelPage
    {
        /// <summary>Every version as a branch tree.</summary>
        Tree,

        /// <summary>One version at a time, with「モデルを復元」.</summary>
        Detail,
    }
}
