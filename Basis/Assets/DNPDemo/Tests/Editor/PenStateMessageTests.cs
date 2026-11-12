using DNPDemo.Interaction;
using DNPDemo.Pen;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class PenStateMessageTests
    {
        static PenState SampleState() => new()
        {
            Mode = PenMode.Voice,
            Radius = 0.2f,
            IsHeld = true,
            IsTriggerHeld = true,
            Position = new Vector3(1.5f, -2f, 3.25f),
            Rotation = Quaternion.Euler(10f, 20f, 30f),
        };

        [Test]
        public void RoundTripsState()
        {
            PenState state = SampleState();
            byte[] buffer = new PenStateMessage(0xBEEF, state).Serialize();
            Assert.AreEqual(PenStateMessage.Size, buffer.Length);

            Assert.IsTrue(PenStateMessage.TryParse(buffer, out PenStateMessage parsed));
            Assert.AreEqual(0xBEEF, parsed.Sequence);
            Assert.AreEqual(state.Mode, parsed.State.Mode);
            Assert.AreEqual(state.Radius, parsed.State.Radius);
            Assert.AreEqual(state.IsHeld, parsed.State.IsHeld);
            Assert.AreEqual(state.IsTriggerHeld, parsed.State.IsTriggerHeld);
            Assert.AreEqual(state.Position, parsed.State.Position);
            Assert.That(Quaternion.Angle(state.Rotation, parsed.State.Rotation), Is.LessThan(0.01f));
        }

        [Test]
        public void RejectsMalformedMessages()
        {
            Assert.IsFalse(PenStateMessage.TryParse(null, out _));
            Assert.IsFalse(PenStateMessage.TryParse(new byte[PenStateMessage.Size - 1], out _));

            byte[] wrongVersion = new PenStateMessage(1, SampleState()).Serialize();
            wrongVersion[0] = 99;
            Assert.IsFalse(PenStateMessage.TryParse(wrongVersion, out _));

            byte[] unknownMode = new PenStateMessage(1, SampleState()).Serialize();
            unknownMode[3] = 200;
            Assert.IsFalse(PenStateMessage.TryParse(unknownMode, out _));

            PenState zeroRotation = SampleState();
            zeroRotation.Rotation = new Quaternion(0f, 0f, 0f, 0f);
            Assert.IsFalse(PenStateMessage.TryParse(new PenStateMessage(1, zeroRotation).Serialize(), out _));
        }

        [Test]
        public void NewerSequenceHandlesWrapAround()
        {
            Assert.IsTrue(PenStateMessage.IsNewer(2, 1));
            Assert.IsFalse(PenStateMessage.IsNewer(1, 2));
            Assert.IsFalse(PenStateMessage.IsNewer(5, 5));
            Assert.IsTrue(PenStateMessage.IsNewer(1, ushort.MaxValue));
        }

        [Test]
        public void SmallChangesAreNotVisibleButFlagsAre()
        {
            PenState state = SampleState();
            PenState nudged = state;
            nudged.Position += new Vector3(0.0005f, 0f, 0f);
            Assert.IsFalse(nudged.DiffersVisibly(state));

            PenState moved = state;
            moved.Position += new Vector3(0.01f, 0f, 0f);
            Assert.IsTrue(moved.DiffersVisibly(state));
            Assert.IsFalse(moved.DiffersDiscretely(state));

            PenState released = state;
            released.IsTriggerHeld = false;
            Assert.IsTrue(released.DiffersDiscretely(state));
        }
    }
}
