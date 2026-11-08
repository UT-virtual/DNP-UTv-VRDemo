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
        public void RejectsMalformedBuffers()
        {
            Assert.IsFalse(ScenarioMessage.TryParse(null, out _));
            Assert.IsFalse(ScenarioMessage.TryParse(new byte[ScenarioMessage.Size - 1], out _));
            Assert.IsFalse(ScenarioMessage.TryParse(new byte[ScenarioMessage.Size], out _), "Type 0 is not a message.");
            Assert.IsFalse(ScenarioMessage.TryParse(new byte[] { 99, 0, 0, 0, 0, 0 }, out _));
            Assert.IsFalse(ScenarioMessage.TryParse(new byte[] { (byte)ScenarioMessageType.Command, 0, 0, 0, 0, 0 }, out _),
                "A command message needs a command.");
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
        }
    }
}
