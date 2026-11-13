using System.Collections.Generic;
using Basis;
using Basis.Network.Core;
using Basis.Scripts.Networking.NetworkedAvatar;
using DNPDemo.Roles;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Owns the two pens in the world, one per role, and keeps what Basis's pickup sync does not carry in step
    /// across devices: the mode, the selection radius, and the held and trigger flags.
    /// <list type="bullet">
    /// <item>The pen of the local role goes to <see cref="SpatialPenController"/>, which alone may grab it; the other
    /// one shows what its player's device sends. A pen is visible while its role is in the session.</item>
    /// <item>The local pen's state is sent at up to <see cref="_sendRate"/> per second while the radius changes
    /// (unreliable), mode, grip and trigger changes go out at once, and once the state has been still for
    /// <see cref="_settleSeconds"/> the exact state follows on the reliable channel, so the last packet is never lost.</item>
    /// <item>Joining players get the current state directly. A state that arrives before its sender's role is known is
    /// kept and shown once the role is announced.</item>
    /// </list>
    /// The pose of each pen is synced by the <c>BasisPickupSyncNetworking</c> on the pen; until anyone grabs it, a
    /// pen stays where it was placed in the prefab, which is its starting position.
    /// </summary>
    public sealed class SpatialPenSync : BasisNetworkBehaviour
    {
        [SerializeField] DemoRoleService _roles;
        [SerializeField] SpatialPenController _controller;
        [SerializeField] SpatialPenView _playerAPen;
        [SerializeField] SpatialPenView _playerBPen;

        [SerializeField, Min(1f)] float _sendRate = 20f;
        [SerializeField, Min(0.05f)] float _settleSeconds = 0.3f;

        readonly Dictionary<ushort, PenStateMessage> _latestByPlayer = new();
        readonly ushort[] _singleRecipient = new ushort[1];

        PenState _lastSent;
        bool _hasSent;
        bool _settlePending;
        float _lastSendTime = float.NegativeInfinity;
        ushort _sequence;
        bool _ready;

        public override void Start()
        {
            if (_roles == null || _controller == null || _playerAPen == null || _playerBPen == null)
            {
                Debug.LogError("[DNPDemo] SpatialPenSync needs DemoRoleService, SpatialPenController and both pens.", this);
                enabled = false;
                return;
            }
            _ready = true;
            _roles.LocalRoleChanged += HandleLocalRoleChanged;
            _roles.RolesChanged += HandleRolesChanged;
            HandleLocalRoleChanged(_roles.LocalRole);
            base.Start();
        }

        public override void OnDestroy()
        {
            if (_ready)
            {
                _roles.LocalRoleChanged -= HandleLocalRoleChanged;
                _roles.RolesChanged -= HandleRolesChanged;
            }
            base.OnDestroy();
        }

        public override void OnNetworkReady()
        {
            SendState(DeliveryMethod.ReliableOrdered, null);
        }

        public override void OnPlayerJoined(BasisNetworkPlayer player)
        {
            if (player != null && !player.IsLocal)
            {
                _singleRecipient[0] = player.playerId;
                SendState(DeliveryMethod.ReliableOrdered, _singleRecipient);
            }
        }

        public override void OnPlayerLeft(BasisNetworkPlayer player)
        {
            if (player != null)
            {
                _latestByPlayer.Remove(player.playerId);
            }
        }

        public override void OnNetworkMessage(ushort playerId, byte[] buffer, DeliveryMethod deliveryMethod)
        {
            if (!_ready || !PenStateMessage.TryParse(buffer, out PenStateMessage message))
            {
                return;
            }
            if (_latestByPlayer.TryGetValue(playerId, out PenStateMessage latest) && !PenStateMessage.IsNewer(message.Sequence, latest.Sequence))
            {
                return;
            }
            _latestByPlayer[playerId] = message;
            ShowRemote(playerId, message.State);
        }

        void LateUpdate()
        {
            if (!_ready || _controller.View == null || !HasNetworkID)
            {
                return;
            }
            PenState state = _controller.CurrentState;
            float now = Time.unscaledTime;
            bool discrete = !_hasSent || state.DiffersDiscretely(_lastSent);
            if (discrete || (state.DiffersVisibly(_lastSent) && now - _lastSendTime >= 1f / _sendRate))
            {
                Send(state, DeliveryMethod.Sequenced, null);
                _settlePending = true;
            }
            else if (_settlePending && now - _lastSendTime >= _settleSeconds)
            {
                Send(state, DeliveryMethod.ReliableOrdered, null);
                _settlePending = false;
            }
        }

        void SendState(DeliveryMethod deliveryMethod, ushort[] recipients)
        {
            if (_ready && _controller.View != null && HasNetworkID)
            {
                Send(_controller.CurrentState, deliveryMethod, recipients);
            }
        }

        void Send(in PenState state, DeliveryMethod deliveryMethod, ushort[] recipients)
        {
            _sequence++;
            SendCustomNetworkEvent(new PenStateMessage(_sequence, state).Serialize(), deliveryMethod, recipients);
            if (recipients == null)
            {
                _lastSent = state;
                _hasSent = true;
                _lastSendTime = Time.unscaledTime;
            }
        }

        void HandleLocalRoleChanged(DemoRole role)
        {
            _controller.Bind(PenFor(role));
            _hasSent = false;
            RefreshVisibility();
            ShowAllRemote();
        }

        void HandleRolesChanged()
        {
            RefreshVisibility();
            ShowAllRemote();
        }

        void ShowAllRemote()
        {
            foreach (KeyValuePair<ushort, PenStateMessage> pair in _latestByPlayer)
            {
                ShowRemote(pair.Key, pair.Value.State);
            }
        }

        void ShowRemote(ushort playerId, in PenState state)
        {
            DemoRole role = _roles.GetRole(playerId);
            SpatialPenView pen = PenFor(role);
            if (pen != null && role != _roles.LocalRole)
            {
                pen.ShowState(state);
            }
        }

        void RefreshVisibility()
        {
            _playerAPen.SetVisible(IsPresent(DemoRole.PlayerA));
            _playerBPen.SetVisible(IsPresent(DemoRole.PlayerB));
        }

        bool IsPresent(DemoRole role) => role == _roles.LocalRole || _roles.TryGetPlayerId(role, out _);

        SpatialPenView PenFor(DemoRole role) => role switch
        {
            DemoRole.PlayerA => _playerAPen,
            DemoRole.PlayerB => _playerBPen,
            _ => null,
        };
    }
}
