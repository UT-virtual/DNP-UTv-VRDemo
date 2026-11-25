using DNPDemo.Interaction;
using DNPDemo.Pen;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class PenStateMessageTests
    {
        static PenState SampleState() => new()
        {
            Mode = PenMode.Voice,
            Radius = 0.2f,
            Extension = 0.6f,
            IsHeld = true,
            IsTriggerHeld = true,
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
            Assert.AreEqual(state.Extension, parsed.State.Extension);
            Assert.AreEqual(state.IsHeld, parsed.State.IsHeld);
            Assert.AreEqual(state.IsTriggerHeld, parsed.State.IsTriggerHeld);
        }

        [Test]
        public void ClampsRadiusToTheAllowedRange()
        {
            PenState state = SampleState();
            state.Radius = 5f;
            Assert.IsTrue(PenStateMessage.TryParse(new PenStateMessage(1, state).Serialize(), out PenStateMessage parsed));
            Assert.AreEqual(PenGeometry.MaxSelectRadius, parsed.State.Radius);
        }

        [Test]
        public void ClampsExtensionToZeroAndOne()
        {
            PenState state = SampleState();
            state.Extension = 3f;
            Assert.IsTrue(PenStateMessage.TryParse(new PenStateMessage(1, state).Serialize(), out PenStateMessage parsed));
            Assert.AreEqual(1f, parsed.State.Extension);

            state.Extension = -1f;
            Assert.IsTrue(PenStateMessage.TryParse(new PenStateMessage(1, state).Serialize(), out parsed));
            Assert.AreEqual(0f, parsed.State.Extension);
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

            PenState notANumber = SampleState();
            notANumber.Radius = float.NaN;
            Assert.IsFalse(PenStateMessage.TryParse(new PenStateMessage(1, notANumber).Serialize(), out _));

            PenState infiniteExtension = SampleState();
            infiniteExtension.Extension = float.PositiveInfinity;
            Assert.IsFalse(PenStateMessage.TryParse(new PenStateMessage(1, infiniteExtension).Serialize(), out _));
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
        public void SmallRadiusChangesAreNotVisibleButFlagsAre()
        {
            PenState state = SampleState();
            PenState nudged = state;
            nudged.Radius += 0.0005f;
            Assert.IsFalse(nudged.DiffersVisibly(state));

            PenState resized = state;
            resized.Radius += 0.01f;
            Assert.IsTrue(resized.DiffersVisibly(state));
            Assert.IsFalse(resized.DiffersDiscretely(state));

            PenState released = state;
            released.IsTriggerHeld = false;
            Assert.IsTrue(released.DiffersDiscretely(state));
        }

        [Test]
        public void ExtensionChangesAreVisibleButNotDiscrete()
        {
            PenState state = SampleState();
            PenState nudged = state;
            nudged.Extension += 0.0005f;
            Assert.IsFalse(nudged.DiffersVisibly(state));

            PenState extended = state;
            extended.Extension += 0.01f;
            Assert.IsTrue(extended.DiffersVisibly(state));
            Assert.IsFalse(extended.DiffersDiscretely(state));
        }

        [Test]
        public void SelectModeTakesTheLengthFromTheRadiusAndOtherModesFromTheExtension()
        {
            PenState state = SampleState();
            state.Radius = PenGeometry.MaxSelectRadius;
            state.Extension = 0.25f;

            state.Mode = PenMode.Select;
            Assert.AreEqual(1f, state.BodyExtension, 1e-5f);

            foreach (PenMode mode in new[] { PenMode.Manipulate, PenMode.Voice, PenMode.Draw })
            {
                state.Mode = mode;
                Assert.AreEqual(0.25f, state.BodyExtension, 1e-5f, mode.ToString());
            }
        }
    }
}
