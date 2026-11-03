using System.Collections.Generic;
using System.Text;
using DNPDemo.Roles;
using DNPDemo.Session;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Lobby
{
    /// <summary>
    /// World-space lobby: choose a role, find or type the host, then show the connection status.
    /// The visible page is derived from <see cref="DemoSessionService.State"/> on every change,
    /// so the panel never holds connection state of its own beyond "the keypad is open".
    /// </summary>
    public sealed class LobbyPanel : MonoBehaviour
    {
        [SerializeField] DemoSessionService _session;
        [SerializeField] DemoRoleService _roleService;

        [Header("Pages")]
        [SerializeField] GameObject _rolePage;
        [SerializeField] GameObject _searchPage;
        [SerializeField] GameObject _manualPage;
        [SerializeField] GameObject _statusPage;

        [Header("Shared")]
        [SerializeField] TMP_Text _errorText;

        [Header("Role page")]
        [SerializeField] Button _hostButton;
        [SerializeField] Button _joinButton;

        [Header("Search page")]
        [SerializeField] TMP_Text _searchStatusText;
        [SerializeField] RectTransform _hostListRoot;
        [SerializeField] Button _hostItemTemplate;
        [SerializeField] Button _manualEntryButton;
        [SerializeField] Button _searchBackButton;

        [Header("Manual entry page")]
        [SerializeField] IpKeypad _keypad;
        [SerializeField] Button _connectButton;
        [SerializeField] Button _manualBackButton;

        [Header("Status page")]
        [SerializeField] TMP_Text _statusText;

        readonly List<Button> _hostItems = new();
        bool _manualEntryOpen;

        void Awake()
        {
            if (_hostItemTemplate != null)
            {
                _hostItemTemplate.gameObject.SetActive(false);
            }
            _hostButton.onClick.AddListener(OnHostClicked);
            _joinButton.onClick.AddListener(OnJoinClicked);
            _manualEntryButton.onClick.AddListener(OpenManualEntry);
            _searchBackButton.onClick.AddListener(OnSearchBackClicked);
            _connectButton.onClick.AddListener(OnConnectClicked);
            _manualBackButton.onClick.AddListener(CloseManualEntry);
            _keypad.ValueChanged += _ => Refresh();
        }

        void OnEnable()
        {
            if (_session == null || _roleService == null)
            {
                Debug.LogError("[DNPDemo] LobbyPanel needs DemoSessionService and DemoRoleService references.", this);
                enabled = false;
                return;
            }
            _session.Changed += Refresh;
            _roleService.RolesChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (_session != null)
            {
                _session.Changed -= Refresh;
            }
            if (_roleService != null)
            {
                _roleService.RolesChanged -= Refresh;
            }
        }

        void OnHostClicked() => _session.StartHost();

        void OnJoinClicked() => _session.StartSearch();

        void OnSearchBackClicked() => _session.StopSearch();

        void OpenManualEntry()
        {
            _session.StopSearch();
            _manualEntryOpen = true;
            _keypad.SetValue(_session.LastHostAddress);
            Refresh();
        }

        void CloseManualEntry()
        {
            _manualEntryOpen = false;
            Refresh();
        }

        void OnConnectClicked()
        {
            if (!_keypad.IsValidIPv4)
            {
                return;
            }
            _manualEntryOpen = false;
            _session.JoinWithDefaultPort(_keypad.Value);
        }

        void Refresh()
        {
            DemoSessionState state = _session.State;
            bool busy = state == DemoSessionState.Connecting || state == DemoSessionState.Connected
                || state == DemoSessionState.Reconnecting;
            if (busy)
            {
                _manualEntryOpen = false;
            }

            _statusPage.SetActive(busy);
            _manualPage.SetActive(!busy && _manualEntryOpen);
            _searchPage.SetActive(!busy && !_manualEntryOpen && state == DemoSessionState.Searching);
            _rolePage.SetActive(!busy && !_manualEntryOpen && state == DemoSessionState.Idle);

            string error = busy ? null : _session.LastError;
            _errorText.text = error ?? string.Empty;
            _errorText.gameObject.SetActive(!string.IsNullOrEmpty(error));

            if (_searchPage.activeSelf)
            {
                RefreshHostList();
            }
            if (_manualPage.activeSelf)
            {
                _connectButton.interactable = _keypad.IsValidIPv4;
            }
            if (_statusPage.activeSelf)
            {
                _statusText.text = BuildStatusText(state);
            }
        }

        void RefreshHostList()
        {
            IReadOnlyList<DiscoveredHost> hosts = _session.DiscoveredHosts;
            _searchStatusText.text = hosts.Count == 0
                ? "プレイヤーA の端末を探しています…"
                : "接続するホストを選んでください。";

            while (_hostItems.Count < hosts.Count)
            {
                Button item = Instantiate(_hostItemTemplate, _hostListRoot);
                _hostItems.Add(item);
            }
            for (int i = 0; i < _hostItems.Count; i++)
            {
                Button item = _hostItems[i];
                bool used = i < hosts.Count;
                item.gameObject.SetActive(used);
                item.onClick.RemoveAllListeners();
                if (!used)
                {
                    continue;
                }
                DiscoveredHost host = hosts[i];
                item.GetComponentInChildren<TMP_Text>().text = $"{host.SessionName}（{host.Address}）";
                item.onClick.AddListener(() => _session.Join(host));
            }
        }

        string BuildStatusText(DemoSessionState state)
        {
            switch (state)
            {
                case DemoSessionState.Connecting:
                    return "接続しています…";
                case DemoSessionState.Reconnecting:
                    return "接続が途切れました。再接続しています…";
            }
            var text = new StringBuilder();
            DemoRole localRole = _roleService.LocalRole;
            if (localRole != DemoRole.None)
            {
                text.Append(localRole.ToDisplayName()).Append(" として参加しました。\n");
            }
            text.Append(_roleService.AreAllRolesPresent ? "2人そろいました。" : "もう1人の参加を待っています。");
            if (!string.IsNullOrEmpty(_session.LocalLanAddress))
            {
                text.Append("\n\nこの端末の IP アドレス：").Append(_session.LocalLanAddress);
            }
            return text.ToString();
        }
    }
}
