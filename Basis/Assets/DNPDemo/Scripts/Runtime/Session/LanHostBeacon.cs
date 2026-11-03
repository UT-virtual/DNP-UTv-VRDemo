using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DNPDemo.Session
{
    /// <summary>
    /// Periodically broadcasts a <see cref="LanBeaconPacket"/> on the local network while Player A hosts.
    /// Sending broadcasts needs no Android multicast lock; only the receiving side does.
    /// </summary>
    public sealed class LanHostBeacon : IDisposable
    {
        UdpClient _client;
        CancellationTokenSource _cts;

        public bool IsRunning => _cts != null;

        public void Start(LanBeaconPacket packet, ushort discoveryPort, float intervalSeconds)
        {
            Stop();
            try
            {
                _client = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[DNPDemo] Host beacon could not open a socket: {e.Message}");
                return;
            }
            _cts = new CancellationTokenSource();
            _ = SendLoopAsync(_client, packet.Encode(), new IPEndPoint(IPAddress.Broadcast, discoveryPort),
                TimeSpan.FromSeconds(intervalSeconds), _cts.Token);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _client?.Dispose();
            _client = null;
        }

        public void Dispose() => Stop();

        static async Task SendLoopAsync(UdpClient client, byte[] payload, IPEndPoint target, TimeSpan interval, CancellationToken token)
        {
            bool warned = false;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await client.SendAsync(payload, payload.Length, target).ConfigureAwait(false);
                    warned = false;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException e)
                {
                    // Typically "network unreachable" while Wi-Fi reconnects; keep trying, but log once per outage.
                    if (!warned)
                    {
                        Debug.LogWarning($"[DNPDemo] Host beacon send failed: {e.Message}");
                        warned = true;
                    }
                }
                try
                {
                    await Task.Delay(interval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
