using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace DNPDemo.Session
{
    /// <summary>A host found through its beacon.</summary>
    public sealed class DiscoveredHost
    {
        public string Address { get; }
        public ushort ServerPort { get; }
        public string SessionName { get; internal set; }
        public float LastSeenTime { get; internal set; }

        public DiscoveredHost(string address, ushort serverPort, string sessionName, float lastSeenTime)
        {
            Address = address;
            ServerPort = serverPort;
            SessionName = sessionName;
            LastSeenTime = lastSeenTime;
        }

        internal string Key => MakeKey(Address, ServerPort);
        internal static string MakeKey(string address, ushort port) => address + ":" + port;
    }

    /// <summary>
    /// Listens for <see cref="LanHostBeacon"/> broadcasts. Sockets are read on a background task;
    /// the host list is only touched from <see cref="Tick"/>, which must run on the main thread.
    /// </summary>
    public sealed class LanHostScanner : IDisposable
    {
        readonly ConcurrentQueue<(string Address, LanBeaconPacket Packet)> _received = new();
        readonly Dictionary<string, DiscoveredHost> _hostsByKey = new();
        readonly List<DiscoveredHost> _hosts = new();
        readonly AndroidMulticastLock _multicastLock = new();
        UdpClient _client;

        public IReadOnlyList<DiscoveredHost> Hosts => _hosts;
        public bool IsRunning => _client != null;

        /// <summary>Raised from <see cref="Tick"/> when a host appears, disappears or changes name.</summary>
        public event Action HostsChanged;

        public bool Start(ushort discoveryPort)
        {
            Stop();
            _multicastLock.Acquire();
            try
            {
                var client = new UdpClient(AddressFamily.InterNetwork);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));
                client.EnableBroadcast = true;
                _client = client;
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[DNPDemo] Host discovery could not listen on UDP {discoveryPort}: {e.Message}");
                _multicastLock.Release();
                return false;
            }
            _ = ReceiveLoopAsync(_client);
            return true;
        }

        public void Stop()
        {
            _client?.Dispose();
            _client = null;
            _multicastLock.Release();
            while (_received.TryDequeue(out _)) { }
            if (_hosts.Count > 0)
            {
                _hosts.Clear();
                _hostsByKey.Clear();
                HostsChanged?.Invoke();
            }
        }

        public void Dispose() => Stop();

        public void Tick(float now, float expirySeconds)
        {
            bool changed = false;
            while (_received.TryDequeue(out var item))
            {
                string key = DiscoveredHost.MakeKey(item.Address, item.Packet.ServerPort);
                if (_hostsByKey.TryGetValue(key, out DiscoveredHost host))
                {
                    host.LastSeenTime = now;
                    if (host.SessionName != item.Packet.SessionName)
                    {
                        host.SessionName = item.Packet.SessionName;
                        changed = true;
                    }
                }
                else
                {
                    host = new DiscoveredHost(item.Address, item.Packet.ServerPort, item.Packet.SessionName, now);
                    _hostsByKey.Add(key, host);
                    _hosts.Add(host);
                    changed = true;
                }
            }
            for (int i = _hosts.Count - 1; i >= 0; i--)
            {
                if (now - _hosts[i].LastSeenTime > expirySeconds)
                {
                    _hostsByKey.Remove(_hosts[i].Key);
                    _hosts.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed)
            {
                HostsChanged?.Invoke();
            }
        }

        async Task ReceiveLoopAsync(UdpClient client)
        {
            while (true)
            {
                UdpReceiveResult result;
                try
                {
                    result = await client.ReceiveAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException e)
                {
                    if (_client != client)
                    {
                        return;
                    }
                    Debug.LogWarning($"[DNPDemo] Host discovery receive failed: {e.Message}");
                    continue;
                }
                if (LanBeaconPacket.TryDecode(result.Buffer, result.Buffer.Length, out LanBeaconPacket packet))
                {
                    _received.Enqueue((result.RemoteEndPoint.Address.ToString(), packet));
                }
            }
        }
    }
}
