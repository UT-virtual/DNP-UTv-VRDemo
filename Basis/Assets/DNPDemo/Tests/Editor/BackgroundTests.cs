using System.Collections.Generic;
using DNPDemo.Editor;
using DNPDemo.Environment;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Background;
using DNPDemo.Interaction.Voice;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class BackgroundTests
    {
        readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }
            _created.Clear();
        }

        EnvironmentPreset CreatePreset(string name)
        {
            var preset = ScriptableObject.CreateInstance<EnvironmentPreset>();
            preset.name = name;
            _created.Add(preset);
            return preset;
        }

        static ScenarioStep StepWith(params ScenarioCue[] cues) => new() { Id = "s", Cues = new List<ScenarioCue>(cues) };

        [Test]
        public void ListHasTheBaselineThenTheAppliedBackgroundsThenTheCandidateEachOnce()
        {
            EnvironmentPreset room = CreatePreset("Room");
            EnvironmentPreset temple = CreatePreset("Temple");
            EnvironmentPreset sea = CreatePreset("Sea");
            var steps = new List<ScenarioStep>
            {
                StepWith(new EnvironmentCue(temple)),
                StepWith(new EnvironmentCue(room), new EnvironmentCue(temple)),
                StepWith(new EnvironmentCue(sea)),
            };
            const string key = EnvironmentController.EnvironmentKey;

            CollectionAssert.AreEqual(new[] { room, temple }, BackgroundList.Collect(steps, 2, key, room, null));
            CollectionAssert.AreEqual(new[] { room, temple, sea }, BackgroundList.Collect(steps, 2, key, room, sea),
                "The candidate comes after the backgrounds already applied.");
            CollectionAssert.AreEqual(new[] { room, temple, sea }, BackgroundList.Collect(steps, 3, key, room, temple),
                "A candidate already listed is not listed twice.");
            CollectionAssert.AreEqual(new[] { temple }, BackgroundList.Collect(steps, 0, key, null, temple),
                "Without a baseline or applied backgrounds, the list is the candidate.");
            CollectionAssert.AreEqual(new[] { room }, BackgroundList.Collect(steps, 3, "Other", room, null),
                "Cues for another controller are not counted.");
            CollectionAssert.AreEqual(new[] { room }, BackgroundList.Collect(null, 3, key, room, null));
        }

        [Test]
        public void CurrentIsTheLastAppliedBackgroundOrTheBaseline()
        {
            EnvironmentPreset room = CreatePreset("Room");
            EnvironmentPreset temple = CreatePreset("Temple");
            var steps = new List<ScenarioStep> { StepWith(), StepWith(new EnvironmentCue(temple)), StepWith(new EnvironmentCue(room)) };
            const string key = EnvironmentController.EnvironmentKey;

            Assert.AreSame(room, BackgroundList.FindCurrent(steps, 1, key, room));
            Assert.AreSame(temple, BackgroundList.FindCurrent(steps, 2, key, room));
            Assert.AreSame(room, BackgroundList.FindCurrent(steps, 3, key, room));
            Assert.AreSame(room, BackgroundList.FindCurrent(steps, 10, "Other", room));
        }

        [Test]
        public void ApplyNeedsPermissionAndAnotherBackgroundThanTheOneOnShow()
        {
            EnvironmentPreset room = CreatePreset("Room");
            EnvironmentPreset temple = CreatePreset("Temple");

            Assert.IsTrue(BackgroundList.CanApply(true, temple, room));
            Assert.IsFalse(BackgroundList.CanApply(false, temple, room), "Only while the scenario allows it.");
            Assert.IsFalse(BackgroundList.CanApply(true, room, room), "The background on show cannot be applied again.");
            Assert.IsFalse(BackgroundList.CanApply(true, null, room));
        }

        [Test]
        public void PartIsAPanelOperationThatNeedsATarget()
        {
            var part = new BackgroundPart("x", null);
            Assert.AreEqual(PenMode.Panel, part.Mode);
            Assert.IsTrue(part.OpensPanel);
            var problems = new List<string>();
            part.Validate(problems);
            Assert.AreEqual(1, problems.Count);

            problems.Clear();
            new BackgroundPart("x", CreatePreset("Temple")).Validate(problems);
            CollectionAssert.IsEmpty(problems);
        }

        [Test]
        public void ChooseTargetStatusNamesTheTarget()
        {
            EnvironmentPreset temple = CreatePreset("EnvironmentPreset_Temple");
            StringAssert.Contains("EnvironmentPreset_Temple", BackgroundTool.FormatChooseTarget(temple),
                "Without a display name, the asset name stands in.");
            Assert.IsEmpty(BackgroundTool.FormatChooseTarget(null));
        }

        [Test]
        public void DefaultScenarioGeneratesTheTempleByVoiceAndAppliesIt()
        {
            AssertTempleStep(ScenarioDataBuilder.CreateDefaultSteps(), "default scenario");
        }

        [Test]
        public void ScenarioAssetGeneratesTheTempleByVoiceAndAppliesIt()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data, ScenarioDataBuilder.AssetPath);
            AssertTempleStep(data.Steps, ScenarioDataBuilder.AssetPath);
        }

        /// <summary>
        /// Step 21 generates the background by voice and applies it from the panel; the background it applies has to be
        /// the one step 22 switches to, or the panel would promise one background and the stage show another.
        /// </summary>
        static void AssertTempleStep(IReadOnlyList<ScenarioStep> steps, string source)
        {
            int index = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].Id == "p3-temple")
                {
                    index = i;
                }
            }
            Assert.AreEqual(21, index, source);
            List<InteractionPart> parts = steps[index].Interaction;
            Assert.AreEqual(2, parts.Count, source);
            var voice = parts[0] as VoicePart;
            Assert.IsNotNull(voice, $"{source}: step 21 starts with the voice command.");
            Assert.AreEqual(ScenarioDataBuilder.TempleTranscript, voice.Transcript, source);
            var background = parts[1] as BackgroundPart;
            Assert.IsNotNull(background, $"{source}: step 21 applies the background from the panel.");
            Assert.IsTrue(background.OpensPanel, $"{source}: the panel opens once the voice command is recognised.");

            var temple = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.TemplePresetPath);
            Assert.AreSame(temple, background.TargetPreset, source);
            EnvironmentCue switchCue = null;
            foreach (ScenarioCue cue in steps[index + 1].Cues)
            {
                switchCue ??= cue as EnvironmentCue;
            }
            Assert.IsNotNull(switchCue, $"{source}: step 22 switches the background.");
            Assert.AreSame(background.TargetPreset, switchCue.Preset, $"{source}: step 22 has to switch to the background step 21 applies.");
        }

        [Test]
        public void PresetsHaveDisplayNames()
        {
            var exhibition = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.ExhibitionPresetPath);
            var temple = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.TemplePresetPath);
            Assert.IsNotNull(exhibition);
            Assert.IsNotNull(temple);
            Assert.AreEqual(EnvironmentPresetBuilder.ExhibitionDisplayName, exhibition.DisplayName);
            Assert.AreEqual(EnvironmentPresetBuilder.TempleDisplayName, temple.DisplayName);
        }
    }
}
