using System;
using System.Collections.Generic;
using System.Globalization;
using Basis.Network.Core;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Networking;
using Basis.Scripts.Networking.NetworkedAvatar;
using DNPDemo.Roles;
using UnityEngine;

namespace DNPDemo.Session
{
    public enum DemoSessionState
    {
        /// <summary>Not connected and not searching.</summary>
        Idle,
        /// <summary>Listening for host beacons (Player B).</summary>
        Searching,
        /// <summary>Hosting or joining is in progress.</summary>
        Connecting,
        /// <summary>The local player is in the session.</summary>
        Connected,
        /// <summary>The link dropped and Basis is retrying.</summary>
        Reconnecting,
    }

    /// <summary>
    /// Starts, joins and tracks the LAN session. This is the only demo code that calls
    /// <see cref="BasisConnectionService"/>; connection health is read from Basis' watchdog every frame.
    /// </summary>
    public sealed class DemoSessionService : MonoBehaviour
    {
        const string LastHostAddressKey = "DNPDemo.LastHostAddress";
        const string HostAddress = "localhost";

        [SerializeField] DemoNetworkSettings _settings = new();

        [Tooltip("Join automatically when exactly one host is found.")]
        [SerializeField] bool _autoJoinSingleHost = true;

        readonly LanHostScanner _scanner = new();
        readonly LanHostBeacon _beacon = new();

        bool _isHosting;
        bool _connectRequested;
        bool _wasConnected;
        float _searchStartedAt;
        string _lastError;
        string _localLanAddress;

        public DemoNetworkSettings Settings => _settings;
        public DemoSessionState State { get; private set; } = DemoSessionState.Idle;

        /// <summary>User-facing reason for the last failure, or null.</summary>
        public string LastError
        {
            get => _lastError;
            private set
            {
                if (_lastError == value) return;
                _lastError = value;
                Changed?.Invoke();
            }
        }

        /// <summary>This device's LAN address while hosting (shown so Player B can type it in), or null.</summary>
        public string LocalLanAddress
        {
            get => _localLanAddress;
            private set
            {
                if (_localLanAddress == value) return;
                _localLanAddress = value;
                Changed?.Invoke();
            }
        }

        public IReadOnlyList<DiscoveredHost> DiscoveredHosts => _scanner.Hosts;

        public string LastHostAddress
        {
            get => PlayerPrefs.GetString(LastHostAddressKey, string.Empty);
            private set
            {
                PlayerPrefs.SetString(LastHostAddressKey, value);
                PlayerPrefs.Save();
            }
        }

        public event Action<DemoSessionState> StateChanged;
        public event Action DiscoveredHostsChanged;

        /// <summary>Raised when anything the lobby displays changes (state, hosts, error, local address).</summary>
        public event Action Changed;

        void OnEnable()
        {
            _scanner.HostsChanged += HandleHostsChanged;
            BasisNetworkPlayer.OnLocalPlayerJoined += HandleLocalPlayerJoined;
            BasisNetworkPlayer.OnLocalPlayerLeft += HandleLocalPlayerLeft;
        }

        void OnDisable()
        {
            _scanner.HostsChanged -= HandleHostsChanged;
            BasisNetworkPlayer.OnLocalPlayerJoined -= HandleLocalPlayerJoined;
            BasisNetworkPlayer.OnLocalPlayerLeft -= HandleLocalPlayerLeft;
            _scanner.Stop();
            _beacon.Stop();
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (_scanner.IsRunning)
            {
                _scanner.Tick(now, _settings.HostExpirySeconds);
                TryAutoJoin(now);
            }
            SetState(EvaluateState());
        }

        /// <summary>Player A: start the in-process Basis server and join it.</summary>
        public void StartHost()
        {
            if (!CanStartConnection())
            {
                return;
            }
            StopSearch();
            BasisNetworkManagement.HostPeerLimit = _settings.PeerLimit;
            BasisNetworkManagement.HostServerName = _settings.SessionName;
            _isHosting = true;
            Connect(HostAddress, _settings.ServerPort, isHostMode: true);
        }

        /// <summary>Player B: listen for host beacons.</summary>
        public void StartSearch()
        {
            if (!CanStartConnection() || _scanner.IsRunning)
            {
                return;
            }
            LastError = null;
            if (!_scanner.Start(_settings.DiscoveryPort))
            {
                LastError = "ホストの探索を開始できませんでした。IP アドレスを入力して接続してください。";
                return;
            }
            _searchStartedAt = Time.unscaledTime;
        }

        public void StopSearch() => _scanner.Stop();

        /// <summary>Player B: join a host by address.</summary>
        public void Join(string address, ushort port)
        {
            if (!CanStartConnection() || string.IsNullOrWhiteSpace(address))
            {
                return;
            }
            StopSearch();
            _isHosting = false;
            LastHostAddress = address.Trim();
            Connect(address.Trim(), port, isHostMode: false);
        }

        public void Join(DiscoveredHost host) => Join(host.Address, host.ServerPort);

        public void JoinWithDefaultPort(string address) => Join(address, _settings.ServerPort);

        bool CanStartConnection() => State == DemoSessionState.Idle || State == DemoSessionState.Searching;

        void Connect(string address, ushort port, bool isHostMode)
        {
            string portText = port.ToString(CultureInfo.InvariantCulture);
            var target = new ConnectionTarget(BasisNetworkStackRegistry.LiteNetLibId, address + ":" + portText);
            target.Set(ConnectionTarget.Keys.Address, address);
            target.Set(ConnectionTarget.Keys.Port, portText);
            var entry = new ServerDirectoryEntry
            {
                Id = "dnpdemo-lan",
                DisplayName = _settings.SessionName,
                Password = _settings.Password,
                HasPassword = true,
                Target = target,
            };
            LastError = null;
            _connectRequested = true;
            string displayName = DemoRoleService.ResolveRole(isHostMode).ToDisplayName();
            _ = BasisConnectionService.ConnectAsync(entry, displayName, isHostMode);
        }

        void TryAutoJoin(float now)
        {
            // Wait a little longer than one beacon interval so a second host on the same LAN gets a chance to show up.
            if (!_autoJoinSingleHost || State != DemoSessionState.Searching
                || now - _searchStartedAt < _settings.BeaconIntervalSeconds * 1.5f || _scanner.Hosts.Count != 1)
            {
                return;
            }
            Join(_scanner.Hosts[0]);
        }

        DemoSessionState EvaluateState()
        {
            if (BasisNetworkConnection.LocalPlayerIsConnected)
            {
                _connectRequested = false;
                _wasConnected = true;
                return DemoSessionState.Connected;
            }
            switch (BasisNetworkConnectionWatchdog.Health)
            {
                case BasisConnectionHealth.Connecting:
                    return DemoSessionState.Connecting;
                case BasisConnectionHealth.Stalled:
                case BasisConnectionHealth.Reconnecting:
                    return DemoSessionState.Reconnecting;
            }
            if (_connectRequested || _wasConnected)
            {
                // The attempt (or Basis' reconnect loop) ended without a live connection.
                LastError = _wasConnected
                    ? "接続が切れました。もう一度、役割を選んでください。"
                    : "接続できませんでした。ホストが起動しているか、同じネットワークにいるかを確認してください。";
                _connectRequested = false;
                _wasConnected = false;
                _isHosting = false;
            }
            return _scanner.IsRunning ? DemoSessionState.Searching : DemoSessionState.Idle;
        }

        void SetState(DemoSessionState state)
        {
            if (State == state)
            {
                return;
            }
            State = state;
            StateChanged?.Invoke(state);
            Changed?.Invoke();
        }

        void HandleHostsChanged()
        {
            DiscoveredHostsChanged?.Invoke();
            Changed?.Invoke();
        }

        void HandleLocalPlayerJoined(BasisNetworkPlayer networkPlayer, BasisLocalPlayer localPlayer)
        {
            if (!_isHosting)
            {
                return;
            }
            LocalLanAddress = LocalAddressResolver.TryGetLanAddress(out string address) ? address : null;
            _beacon.Start(new LanBeaconPacket(_settings.ServerPort, _settings.SessionName),
                _settings.DiscoveryPort, _settings.BeaconIntervalSeconds);
        }

        void HandleLocalPlayerLeft(BasisNetworkPlayer networkPlayer, BasisLocalPlayer localPlayer)
        {
            _beacon.Stop();
            LocalLanAddress = null;
        }
    }
}
