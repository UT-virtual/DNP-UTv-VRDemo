using System.Collections.Generic;
using DNPDemo.Scenario;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class ScenarioCheckpointTests
    {
        static readonly List<ScenarioStep> Steps = new()
        {
            new ScenarioStep { Id = "lobby" },
            new ScenarioStep { Id = "opening" },
            new ScenarioStep { Id = "scene1" },
            new ScenarioStep { Id = "closing" },
        };

        [TestCase("opening", 1)]
        [TestCase("scene1", 2)]
        public void AStepInTheMiddleIsResumable(string stepId, int expected)
        {
            Assert.AreEqual(expected, ScenarioCheckpoint.ResolveResumeIndex(Steps, stepId));
        }

        [TestCase("lobby")]
        [TestCase("closing")]
        public void TheLobbyAndTheClosingAreNotInterruptions(string stepId)
        {
            Assert.AreEqual(-1, ScenarioCheckpoint.ResolveResumeIndex(Steps, stepId));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("removed-step")]
        public void NothingOrAnUnknownStepIsNotResumable(string stepId)
        {
            Assert.AreEqual(-1, ScenarioCheckpoint.ResolveResumeIndex(Steps, stepId));
        }
    }
}
