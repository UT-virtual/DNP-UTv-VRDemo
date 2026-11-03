using DNPDemo.Lobby;
using DNPDemo.Session;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class LanBeaconPacketTests
    {
        [Test]
        public void EncodeThenDecode_RoundTrips()
        {
            var packet = new LanBeaconPacket(4296, "DNP UTv デモ");
            byte[] data = packet.Encode();

            Assert.IsTrue(LanBeaconPacket.TryDecode(data, data.Length, out LanBeaconPacket decoded));
            Assert.AreEqual(packet, decoded);
        }

        [Test]
        public void Encode_TruncatesLongNameAtCharacterBoundary()
        {
            var packet = new LanBeaconPacket(4296, new string('あ', 40)); // 120 bytes in UTF-8
            byte[] data = packet.Encode();

            Assert.IsTrue(LanBeaconPacket.TryDecode(data, data.Length, out LanBeaconPacket decoded));
            Assert.AreEqual(new string('あ', LanBeaconPacket.MaxSessionNameBytes / 3), decoded.SessionName);
        }

        [Test]
        public void Decode_RejectsForeignOrBrokenPackets()
        {
            byte[] valid = new LanBeaconPacket(4296, "demo").Encode();

            byte[] wrongMagic = (byte[])valid.Clone();
            wrongMagic[0] = (byte)'X';
            Assert.IsFalse(LanBeaconPacket.TryDecode(wrongMagic, wrongMagic.Length, out _));

            byte[] wrongVersion = (byte[])valid.Clone();
            wrongVersion[4] = LanBeaconPacket.CurrentVersion + 1;
            Assert.IsFalse(LanBeaconPacket.TryDecode(wrongVersion, wrongVersion.Length, out _));

            Assert.IsFalse(LanBeaconPacket.TryDecode(valid, valid.Length - 1, out _), "truncated");
            Assert.IsFalse(LanBeaconPacket.TryDecode(new byte[3], 3, out _), "shorter than header");
            Assert.IsFalse(LanBeaconPacket.TryDecode(null, 0, out _), "null");

            byte[] zeroPort = new LanBeaconPacket(0, "demo").Encode();
            Assert.IsFalse(LanBeaconPacket.TryDecode(zeroPort, zeroPort.Length, out _), "port 0");
        }

        [TestCase("192.168.0.10", true)]
        [TestCase("10.0.0.1", true)]
        [TestCase("192.168.0", false)]
        [TestCase("192.168.0.256", false)]
        [TestCase("1", false)]
        [TestCase("", false)]
        public void IpKeypad_IsDottedQuad(string text, bool expected)
        {
            Assert.AreEqual(expected, IpKeypad.IsDottedQuad(text));
        }
    }
}
