using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DNPDemo.Session
{
    /// <summary>Finds this device's IPv4 address on the LAN, for display on the lobby panel.</summary>
    public static class LocalAddressResolver
    {
        public static bool TryGetLanAddress(out string address)
        {
            return TryFromRoutingTable(out address) || TryFromInterfaces(out address);
        }

        /// <summary>
        /// Asks the OS which local address it would route an outbound packet from.
        /// Connecting a UDP socket sends nothing; it only selects the route.
        /// </summary>
        static bool TryFromRoutingTable(out string address)
        {
            address = null;
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 65530));
                if (socket.LocalEndPoint is IPEndPoint endPoint && !IPAddress.IsLoopback(endPoint.Address)
                    && !endPoint.Address.Equals(IPAddress.Any))
                {
                    address = endPoint.Address.ToString();
                    return true;
                }
            }
            catch (SocketException)
            {
                // No default route (e.g. an isolated router without a gateway); fall back to the interface list.
            }
            return false;
        }

        static bool TryFromInterfaces(out string address)
        {
            address = null;
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }
                    foreach (UnicastIPAddressInformation info in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (info.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(info.Address))
                        {
                            address = info.Address.ToString();
                            return true;
                        }
                    }
                }
            }
            catch (NetworkInformationException)
            {
            }
            return false;
        }
    }
}
