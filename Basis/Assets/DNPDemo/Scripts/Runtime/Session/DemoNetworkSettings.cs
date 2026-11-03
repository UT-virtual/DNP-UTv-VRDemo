using System;
using UnityEngine;

namespace DNPDemo.Session
{
    /// <summary>
    /// LAN session parameters shared by the host and the joining client.
    /// Both devices must run a build with identical values.
    /// </summary>
    [Serializable]
    public sealed class DemoNetworkSettings
    {
        [Tooltip("UDP port of the Basis server hosted by Player A.")]
        public ushort ServerPort = 4296;

        [Tooltip("Password of the hosted Basis server. Player B joins with the same value.")]
        public string Password = "dnputv-demo";

        [Tooltip("UDP port used for the host beacon broadcast.")]
        public ushort DiscoveryPort = 4297;

        [Tooltip("Name carried in the beacon, shown in the host list when several hosts are found.")]
        public string SessionName = "DNP UTv Demo";

        [Min(0.2f)]
        public float BeaconIntervalSeconds = 1f;

        [Tooltip("A discovered host is dropped from the list after this many seconds without a beacon.")]
        [Min(1f)]
        public float HostExpirySeconds = 3.5f;

        [Tooltip("Maximum number of players accepted by the host.")]
        [Min(1)]
        public int PeerLimit = 2;
    }
}
