using System;
using System.Collections.Generic;
using Basis;
using Basis.Network.Core;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Networking;
using Basis.Scripts.Networking.NetworkedAvatar;

namespace DNPDemo.Roles
{
    /// <summary>
    /// The single place that decides which role each device plays and whether it drives the scenario.
    /// Other code asks this service instead of reading <see cref="BasisNetworkManagement.IsHostMode"/>,
    /// so moving to a dedicated server (role picked in the lobby, authority via ownership) only touches this class.
    /// <para>
    /// Each device announces its own role; a device that hears from an unknown player replies with its role,
    /// so both sides end up with the full player-to-role table regardless of join order.
    /// </para>
    /// </summary>
    public sealed class DemoRoleService : BasisNetworkBehaviour
    {
        enum MessageType : byte
        {
            Announce = 1,
            Reply = 2,
        }

        readonly Dictionary<ushort, DemoRole> _rolesByPlayer = new();
        readonly ushort[] _singleRecipient = new ushort[1];

        public DemoRole LocalRole { get; private set; } = DemoRole.None;

        /// <summary>True on the device whose state is authoritative for scenario progress.</summary>
        public bool HasScenarioAuthority => LocalRole != DemoRole.None && BasisNetworkManagement.IsHostMode;

        /// <summary>Raised when the local role is assigned on join, or reset to <see cref="DemoRole.None"/> on leave.</summary>
        public event Action<DemoRole> LocalRoleChanged;

        /// <summary>Raised when the role table changes (a player announced a role or left).</summary>
        public event Action RolesChanged;

        /// <summary>Role a device takes when it hosts (or joins) a LAN session.</summary>
        public static DemoRole ResolveRole(bool isHostMode) => isHostMode ? DemoRole.PlayerA : DemoRole.PlayerB;

        public DemoRole GetLocalRole() => LocalRole;

        public DemoRole GetRole(ushort playerId) =>
            _rolesByPlayer.TryGetValue(playerId, out DemoRole role) ? role : DemoRole.None;

        public bool TryGetPlayerId(DemoRole role, out ushort playerId)
        {
            foreach (KeyValuePair<ushort, DemoRole> pair in _rolesByPlayer)
            {
                if (pair.Value == role)
                {
                    playerId = pair.Key;
                    return true;
                }
            }
            playerId = 0;
            return false;
        }

        /// <summary>True when both roles are present in the session.</summary>
        public bool AreAllRolesPresent => TryGetPlayerId(DemoRole.PlayerA, out _) && TryGetPlayerId(DemoRole.PlayerB, out _);

        public override void Start()
        {
            BasisNetworkPlayer.OnLocalPlayerJoined += HandleLocalPlayerJoined;
            BasisNetworkPlayer.OnLocalPlayerLeft += HandleLocalPlayerLeft;
            // Subscribed before base.Start so the local role is set before the base class resolves
            // the network id and calls OnNetworkReady (which announces the role).
            if (BasisNetworkConnection.LocalPlayerIsConnected)
            {
                AssignLocalRole();
            }
            base.Start();
        }

        public override void OnDestroy()
        {
            BasisNetworkPlayer.OnLocalPlayerJoined -= HandleLocalPlayerJoined;
            BasisNetworkPlayer.OnLocalPlayerLeft -= HandleLocalPlayerLeft;
            base.OnDestroy();
        }

        public override void OnNetworkReady()
        {
            if (LocalRole == DemoRole.None)
            {
                AssignLocalRole();
            }
            Send(MessageType.Announce, null);
        }

        public override void OnNetworkMessage(ushort playerId, byte[] buffer, DeliveryMethod deliveryMethod)
        {
            if (buffer == null || buffer.Length < 2)
            {
                return;
            }
            var type = (MessageType)buffer[0];
            var role = (DemoRole)buffer[1];
            if (role != DemoRole.PlayerA && role != DemoRole.PlayerB)
            {
                return;
            }
            bool changed = !_rolesByPlayer.TryGetValue(playerId, out DemoRole previous) || previous != role;
            _rolesByPlayer[playerId] = role;
            if (type == MessageType.Announce)
            {
                _singleRecipient[0] = playerId;
                Send(MessageType.Reply, _singleRecipient);
            }
            if (changed)
            {
                RolesChanged?.Invoke();
            }
        }

        public override void OnPlayerLeft(BasisNetworkPlayer player)
        {
            if (player != null && _rolesByPlayer.Remove(player.playerId))
            {
                RolesChanged?.Invoke();
            }
        }

        void HandleLocalPlayerJoined(BasisNetworkPlayer networkPlayer, BasisLocalPlayer localPlayer) => AssignLocalRole();

        void HandleLocalPlayerLeft(BasisNetworkPlayer networkPlayer, BasisLocalPlayer localPlayer)
        {
            bool hadRoles = _rolesByPlayer.Count > 0;
            _rolesByPlayer.Clear();
            if (LocalRole != DemoRole.None)
            {
                LocalRole = DemoRole.None;
                LocalRoleChanged?.Invoke(LocalRole);
            }
            if (hadRoles)
            {
                RolesChanged?.Invoke();
            }
        }

        void AssignLocalRole()
        {
            DemoRole role = ResolveRole(BasisNetworkManagement.IsHostMode);
            if (BasisNetworkConnection.TryGetLocalPlayerID(out ushort localId))
            {
                _rolesByPlayer[localId] = role;
            }
            if (LocalRole == role)
            {
                return;
            }
            LocalRole = role;
            LocalRoleChanged?.Invoke(role);
            RolesChanged?.Invoke();
        }

        void Send(MessageType type, ushort[] recipients)
        {
            if (!HasNetworkID || LocalRole == DemoRole.None)
            {
                return;
            }
            SendCustomNetworkEvent(new[] { (byte)type, (byte)LocalRole }, DeliveryMethod.ReliableOrdered, recipients);
        }
    }
}
