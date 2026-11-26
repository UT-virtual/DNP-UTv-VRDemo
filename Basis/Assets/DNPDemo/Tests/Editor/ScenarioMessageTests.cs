using DNPDemo.Scenario;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class ScenarioMessageTests
    {
        [Test]
        public void StateRoundTrips()
        {
            AssertRoundTrip(ScenarioMessage.State(0xBEEF, 21));
        }

        [Test]
        public void StateCarriesTheResumeStep()
        {
            Assert.IsTrue(ScenarioMessage.TryParse(ScenarioMessage.State(4, 0, 12).Serialize(), out ScenarioMessage withResume));
            Assert.AreEqual(12, withResume.ResumeStepIndex);
            Assert.IsTrue(ScenarioMessage.TryParse(ScenarioMessage.State(4, 0).Serialize(), out ScenarioMessage withoutResume));
            Assert.AreEqual(-1, withoutResume.ResumeStepIndex);
        }

        [Test]
        public void RequestStateRoundTrips()
        {
            AssertRoundTrip(ScenarioMessage.RequestState());
        }

        [Test]
        public void ReportCompleteRoundTrips()
        {
            AssertRoundTrip(ScenarioMessage.ReportComplete(7, 3));
        }

        [TestCase(OperatorCommand.Next)]
        [TestCase(OperatorCommand.Back)]
        [TestCase(OperatorCommand.Restart)]
        public void CommandRoundTrips(OperatorCommand command)
        {
            AssertRoundTrip(ScenarioMessage.CommandFor(command, 300, 12));
        }

        [Test]
        public void JumpToCarriesItsTarget()
        {
            ScenarioMessage message = ScenarioMessage.CommandFor(OperatorCommand.JumpTo, 300, 12, 17);
            AssertRoundTrip(message);
            Assert.IsTrue(ScenarioMessage.TryParse(message.Serialize(), out ScenarioMessage parsed));
            Assert.AreEqual(17, parsed.TargetStepIndex);
        }

        [Test]
        public void RejectsMalformedBuffers()
        {
            Assert.IsFalse(ScenarioMessage.TryParse(null, out _));
            Assert.IsFalse(ScenarioMessage.TryParse(new byte[ScenarioMessage.Size - 1], out _));
            Assert.IsFalse(ScenarioMessage.TryParse(new byte[ScenarioMessage.Size], out _), "Type 0 is not a message.");
            Assert.IsFalse(ScenarioMessage.TryParse(Buffer(99, OperatorCommand.None, 0), out _));
            Assert.IsFalse(ScenarioMessage.TryParse(Buffer((byte)ScenarioMessageType.Command, OperatorCommand.None, 0), out _),
                "A command message needs a command.");
            Assert.IsFalse(ScenarioMessage.TryParse(Buffer((byte)ScenarioMessageType.Command, OperatorCommand.JumpTo, ScenarioMessage.NoStep), out _),
                "A jump needs a target step.");
        }

        static byte[] Buffer(byte type, OperatorCommand command, ushort targetStep)
        {
            var buffer = new byte[ScenarioMessage.Size];
            buffer[0] = type;
            buffer[5] = (byte)command;
            buffer[6] = (byte)targetStep;
            buffer[7] = (byte)(targetStep >> 8);
            return buffer;
        }

        static void AssertRoundTrip(ScenarioMessage message)
        {
            byte[] buffer = message.Serialize();
            Assert.AreEqual(ScenarioMessage.Size, buffer.Length);
            Assert.IsTrue(ScenarioMessage.TryParse(buffer, out ScenarioMessage parsed));
            Assert.AreEqual(message.Type, parsed.Type);
            Assert.AreEqual(message.Sequence, parsed.Sequence);
            Assert.AreEqual(message.StepIndex, parsed.StepIndex);
            Assert.AreEqual(message.Command, parsed.Command);
            Assert.AreEqual(message.TargetStep, parsed.TargetStep);
            Assert.AreEqual(message.ResumeStep, parsed.ResumeStep);
        }
    }
}
