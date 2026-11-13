using System.Collections.Generic;
using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Selection;
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
            // duplicate id, empty id, interaction without actor, interaction without parts, unset cue, null step
            Assert.AreEqual(6, problems.Count, string.Join("\n", problems));
        }

        [Test]
        public void ValidateChecksInteractionParts()
        {
            var steps = new List<ScenarioStep>
            {
                new()
                {
                    Id = "unset-part", Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerA,
                    Interaction = new List<InteractionPart> { null },
                },
                new()
                {
                    Id = "parts-on-auto-step", Completion = StepCompletion.CuesFinished,
                    Interaction = new List<InteractionPart> { new DummyInteractionPart(PenMode.Voice, "x") },
                },
                new()
                {
                    Id = "ok", Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerB,
                    Interaction = new List<InteractionPart> { new DummyInteractionPart(PenMode.Select, "x") },
                },
            };
            List<string> problems = ScenarioData.Validate(steps);
            Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
        }

        [Test]
        public void ValidateChecksPartParameters()
        {
            var steps = new List<ScenarioStep>
            {
                new()
                {
                    Id = "no-zone", Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerA,
                    Interaction = new List<InteractionPart> { new SelectionPart("x") },
                },
                new()
                {
                    Id = "bad-keys", Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerA,
                    Interaction = new List<InteractionPart> { new SelectionPart("x", "Zone_A", "", "Zone_A") },
                },
                new()
                {
                    Id = "ok", Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerB,
                    Interaction = new List<InteractionPart> { new SelectionPart("x", "Zone_A", "Zone_B") },
                },
            };
            List<string> problems = ScenarioData.Validate(steps);
            // no target zone, empty key, duplicate key
            Assert.AreEqual(3, problems.Count, string.Join("\n", problems));
        }

        [Test]
        public void RequirementsIncludeTheZonesOfInteractionParts()
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<ScenarioData>();
            try
            {
                data.SetSteps(new List<ScenarioStep>
                {
                    new()
                    {
                        Id = "select", Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerA,
                        Interaction = new List<InteractionPart> { new SelectionPart("x", "Zone_A", "Zone_B") },
                    },
                });
                CollectionAssert.AreEquivalent(new[] { "Zone_A", "Zone_B" }, data.CollectRequirements().ObjectKeys);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void DefaultInteractionPartsFollowThePlan()
        {
            var expected = new Dictionary<string, PenMode[]>
            {
                ["s1-voice-left-arm"] = new[] { PenMode.Voice },
                ["s2-move-apple"] = new[] { PenMode.Select, PenMode.Manipulate },
                ["s2-regrip-apple"] = new[] { PenMode.Select, PenMode.Voice },
                ["s2-slide-arm"] = new[] { PenMode.Select, PenMode.Manipulate },
                ["s2-guide-curve"] = new[] { PenMode.Draw },
                ["s3-draw-mirror"] = new[] { PenMode.Draw, PenMode.Voice },
                ["s3-place-mirror"] = new[] { PenMode.Manipulate },
                ["s3-grip-mirror"] = new[] { PenMode.Select, PenMode.Voice },
                ["s3-adjust-wrist"] = new[] { PenMode.Select, PenMode.Manipulate },
                ["p2-compare"] = new[] { PenMode.Panel },
                ["p3-temple"] = new[] { PenMode.Select, PenMode.Voice },
            };
            foreach (ScenarioStep step in DefaultSteps)
            {
                if (step.Completion != StepCompletion.Interaction)
                {
                    continue;
                }
                Assert.IsTrue(expected.TryGetValue(step.Id, out PenMode[] modes), $"{step.Id} is not an operation of docs/PLAN.md §3.");
                Assert.AreEqual(modes.Length, step.Interaction.Count, step.Id);
                for (int i = 0; i < modes.Length; i++)
                {
                    Assert.AreEqual(modes[i], step.Interaction[i].Mode, $"{step.Id} part {i}");
                    Assert.IsFalse(string.IsNullOrEmpty(step.Interaction[i].Instruction), $"{step.Id} part {i} needs an instruction.");
                }
            }
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
