using System;
using System.Text;

namespace DNPDemo.Session
{
    /// <summary>
    /// Payload of the UDP broadcast Player A's device sends so Player B can find it.
    /// Layout: "DNPD" (4) | version (1) | server port, little endian (2) | name length (1) | name, UTF-8.
    /// </summary>
    public readonly struct LanBeaconPacket : IEquatable<LanBeaconPacket>
    {
        public const byte CurrentVersion = 1;
        public const int MaxSessionNameBytes = 64;

        static readonly byte[] Magic = { (byte)'D', (byte)'N', (byte)'P', (byte)'D' };
        const int HeaderLength = 4 + 1 + 2 + 1;

        public readonly ushort ServerPort;
        public readonly string SessionName;

        public LanBeaconPacket(ushort serverPort, string sessionName)
        {
            ServerPort = serverPort;
            SessionName = sessionName ?? string.Empty;
        }

        public byte[] Encode()
        {
            byte[] name = TruncateUtf8(SessionName, MaxSessionNameBytes);
            byte[] buffer = new byte[HeaderLength + name.Length];
            Buffer.BlockCopy(Magic, 0, buffer, 0, Magic.Length);
            buffer[4] = CurrentVersion;
            buffer[5] = (byte)(ServerPort & 0xFF);
            buffer[6] = (byte)(ServerPort >> 8);
            buffer[7] = (byte)name.Length;
            Buffer.BlockCopy(name, 0, buffer, HeaderLength, name.Length);
            return buffer;
        }

        public static bool TryDecode(byte[] data, int length, out LanBeaconPacket packet)
        {
            packet = default;
            if (data == null || length < HeaderLength || length > data.Length)
            {
                return false;
            }
            for (int i = 0; i < Magic.Length; i++)
            {
                if (data[i] != Magic[i])
                {
                    return false;
                }
            }
            if (data[4] != CurrentVersion)
            {
                return false;
            }
            ushort port = (ushort)(data[5] | (data[6] << 8));
            int nameLength = data[7];
            if (port == 0 || nameLength > MaxSessionNameBytes || HeaderLength + nameLength != length)
            {
                return false;
            }
            string name;
            try
            {
                name = new UTF8Encoding(false, true).GetString(data, HeaderLength, nameLength);
            }
            catch (ArgumentException)
            {
                return false;
            }
            packet = new LanBeaconPacket(port, name);
            return true;
        }

        /// <summary>Cuts the UTF-8 encoding at a character boundary so it never exceeds <paramref name="maxBytes"/>.</summary>
        static byte[] TruncateUtf8(string value, int maxBytes)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length <= maxBytes)
            {
                return bytes;
            }
            int cut = maxBytes;
            while (cut > 0 && (bytes[cut] & 0xC0) == 0x80)
            {
                cut--;
            }
            byte[] truncated = new byte[cut];
            Buffer.BlockCopy(bytes, 0, truncated, 0, cut);
            return truncated;
        }

        public bool Equals(LanBeaconPacket other) => ServerPort == other.ServerPort && SessionName == other.SessionName;
        public override bool Equals(object obj) => obj is LanBeaconPacket other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ServerPort, SessionName);
    }
}
