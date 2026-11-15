using System;
using DNPDemo.Interaction.Manipulate;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class ManipulateMessageTests
    {
        [Test]
        public void RoundTripKeepsEverything()
        {
            var value = new ManipulateValue { Offset = new Vector3(0.1f, -0.2f, 0.3f), Angle = -42.5f, Snapped = true };
            byte[] bytes = new ManipulateMessage(ManipulateMessageKind.Snap, 513, true, value).Serialize();
            Assert.AreEqual(ManipulateMessage.Size, bytes.Length);

            Assert.IsTrue(ManipulateMessage.TryParse(new ArraySegment<byte>(bytes), out ManipulateMessage parsed));
            Assert.AreEqual(ManipulateMessageKind.Snap, parsed.Kind);
            Assert.AreEqual(513, parsed.Counter);
            Assert.IsTrue(parsed.IsGrabbing);
            Assert.AreEqual(value, parsed.Value);
        }

        [Test]
        public void ParsesFromTheMiddleOfABuffer()
        {
            byte[] message = new ManipulateMessage(ManipulateMessageKind.Drag, 7, false, ManipulateValue.Rotation(12f)).Serialize();
            var buffer = new byte[message.Length + 6];
            Buffer.BlockCopy(message, 0, buffer, 6, message.Length);
            Assert.IsTrue(ManipulateMessage.TryParse(new ArraySegment<byte>(buffer, 6, message.Length), out ManipulateMessage parsed));
            Assert.AreEqual(12f, parsed.Value.Angle);
            Assert.IsFalse(parsed.IsGrabbing);
            Assert.IsFalse(parsed.Value.Snapped);
        }

        [Test]
        public void RejectsMalformedPayloads()
        {
            byte[] valid = new ManipulateMessage(ManipulateMessageKind.Release, 1, false, default).Serialize();
            Assert.IsFalse(ManipulateMessage.TryParse(new ArraySegment<byte>(valid, 0, valid.Length - 1), out _));
            Assert.IsFalse(ManipulateMessage.TryParse(default, out _));

            byte[] unknownKind = (byte[])valid.Clone();
            unknownKind[0] = 99;
            Assert.IsFalse(ManipulateMessage.TryParse(new ArraySegment<byte>(unknownKind), out _));

            byte[] notFinite = new ManipulateMessage(ManipulateMessageKind.Drag, 1, false, ManipulateValue.Rotation(float.NaN)).Serialize();
            Assert.IsFalse(ManipulateMessage.TryParse(new ArraySegment<byte>(notFinite), out _));
        }

        [Test]
        public void NewerCountersAreRecognisedAcrossWrapAround()
        {
            Assert.IsTrue(ManipulateMessage.IsNewer(5, 4));
            Assert.IsFalse(ManipulateMessage.IsNewer(4, 5));
            Assert.IsFalse(ManipulateMessage.IsNewer(5, 5));
            Assert.IsTrue(ManipulateMessage.IsNewer(2, ushort.MaxValue));
            Assert.IsFalse(ManipulateMessage.IsNewer(ushort.MaxValue, 2));
        }
    }
}
