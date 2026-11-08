using System.Collections.Generic;
using DNPDemo.Editor;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class ScenarioDataTests
    {
        static List<ScenarioStep> DefaultSteps => ScenarioDataBuilder.CreateDefaultSteps();

        [Test]
        public void DefaultScenarioIsValid()
        {
            CollectionAssert.IsEmpty(ScenarioData.Validate(DefaultSteps));
        }

        [Test]
        public void DefaultScenarioFollowsThePlan()
        {
            List<ScenarioStep> steps = DefaultSteps;
            Assert.AreEqual(23, steps.Count, "docs/PLAN.md §3 defines steps 0 to 22.");
            Assert.AreEqual(StepCompletion.PlayersReady, steps[0].Completion, "Step 0 is the lobby.");
            Assert.AreEqual(StepCompletion.Manual, steps[steps.Count - 1].Completion, "The closing step waits for the operator to restart.");

            int interactions = 0;
            foreach (ScenarioStep step in steps)
            {
                if (step.Completion == StepCompletion.Interaction)
                {
                    interactions++;
                    Assert.IsFalse(string.IsNullOrEmpty(step.TaskText), $"{step.Id} needs a task instruction.");
                }
            }
            Assert.AreEqual(11, interactions, "Steps 3, 6, 7, 9, 11, 13, 14, 15, 17, 19 and 21 wait for a player.");
        }

        [Test]
        public void ValidateReportsBrokenSteps()
        {
            var steps = new List<ScenarioStep>
            {
                new() { Id = "a" },
                new() { Id = "a" },
                new() { Id = "", Completion = StepCompletion.Interaction },
                new() { Id = "c", Cues = new List<ScenarioCue> { null } },
                null,
            };
            List<string> problems = ScenarioData.Validate(steps);
            Assert.AreEqual(5, problems.Count, string.Join("\n", problems));
        }

        [Test]
        public void ValidateRejectsEmptyScenario()
        {
            Assert.IsNotEmpty(ScenarioData.Validate(new List<ScenarioStep>()));
        }

        [Test]
        public void OnlyTheActorCanCompleteAnInteraction()
        {
            var step = new ScenarioStep { Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerB };
            Assert.IsTrue(step.AcceptsInteractionFrom(DemoRole.PlayerB));
            Assert.IsFalse(step.AcceptsInteractionFrom(DemoRole.PlayerA));
            Assert.IsFalse(step.AcceptsInteractionFrom(DemoRole.None));

            var automatic = new ScenarioStep { Completion = StepCompletion.CuesFinished, Actor = DemoRole.PlayerB };
            Assert.IsFalse(automatic.AcceptsInteractionFrom(DemoRole.PlayerB));
        }

        [Test]
        public void LinesWithoutVoiceUseEstimatedDuration()
        {
            var timing = new LineTiming { SecondsPerCharacter = 0.1f, MinimumSeconds = 2f };
            Assert.AreEqual(2f, timing.EstimateSeconds(null));
            Assert.AreEqual(2f, timing.EstimateSeconds("短い"));
            Assert.AreEqual(5f, timing.EstimateSeconds(new string('あ', 50)), 1e-4f);
        }
    }
}
