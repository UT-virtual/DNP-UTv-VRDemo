using System;
using System.Text;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// Progress payload of <see cref="HistoryTool"/>: the <see cref="Venus.VenusVersion.Id"/> of the version the actor
    /// restored to the comparison table, as one length byte followed by UTF-8.
    /// </summary>
    public static class HistoryMessage
    {
        /// <summary>Longest id in UTF-8 bytes. Version ids are short ("armless", "v1.0").</summary>
        public const int MaxIdBytes = byte.MaxValue;

        /// <summary>The payload for <paramref name="versionId"/>, or null when it is empty or too long to send.</summary>
        public static byte[] Serialize(string versionId)
        {
            if (string.IsNullOrEmpty(versionId))
            {
                return null;
            }
            byte[] id = Encoding.UTF8.GetBytes(versionId);
            if (id.Length > MaxIdBytes)
            {
                return null;
            }
            var payload = new byte[1 + id.Length];
            payload[0] = (byte)id.Length;
            Buffer.BlockCopy(id, 0, payload, 1, id.Length);
            return payload;
        }

        public static bool TryParse(ArraySegment<byte> payload, out string versionId)
        {
            versionId = null;
            if (payload.Array == null || payload.Count < 1)
            {
                return false;
            }
            int length = payload.Array[payload.Offset];
            if (length == 0 || payload.Count != 1 + length)
            {
                return false;
            }
            try
            {
                versionId = new UTF8Encoding(false, true).GetString(payload.Array, payload.Offset + 1, length);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }
}
