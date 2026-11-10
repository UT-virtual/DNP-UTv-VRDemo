using System;
using DNPDemo.Interaction;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class InteractionMessageTests
    {
        [Test]
        public void RoundTripsHeaderAndPayload()
        {
            var message = new InteractionMessage(InteractionMessageType.Progress, 0xBEEF, 300, 2);
            byte[] buffer = message.Serialize(new byte[] { 7, 8, 9 });
            Assert.AreEqual(InteractionMessage.HeaderSize + 3, buffer.Length);

            Assert.IsTrue(InteractionMessage.TryParse(buffer, out InteractionMessage parsed, out ArraySegment<byte> payload));
            Assert.AreEqual(InteractionMessageType.Progress, parsed.Type);
            Assert.AreEqual(0xBEEF, parsed.Sequence);
            Assert.AreEqual(300, parsed.StepIndex);
            Assert.AreEqual(2, parsed.Part);
            CollectionAssert.AreEqual(new byte[] { 7, 8, 9 }, payload);
        }

        [Test]
        public void HeaderOnlyMessageHasEmptyPayload()
        {
            byte[] buffer = new InteractionMessage(InteractionMessageType.RequestPartState, 1, 2, 0).Serialize();
            Assert.AreEqual(InteractionMessage.HeaderSize, buffer.Length);
            Assert.IsTrue(InteractionMessage.TryParse(buffer, out _, out ArraySegment<byte> payload));
            Assert.AreEqual(0, payload.Count);
        }

        [Test]
        public void RejectsShortAndUnknownMessages()
        {
            Assert.IsFalse(InteractionMessage.TryParse(null, out _, out _));
            Assert.IsFalse(InteractionMessage.TryParse(new byte[InteractionMessage.HeaderSize - 1], out _, out _));
            byte[] unknown = new byte[InteractionMessage.HeaderSize];
            unknown[0] = 99;
            Assert.IsFalse(InteractionMessage.TryParse(unknown, out _, out _));
        }
    }
}
