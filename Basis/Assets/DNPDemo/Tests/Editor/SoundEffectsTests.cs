using System.Collections.Generic;
using DNPDemo.Audio;
using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class SoundEffectsTests
    {
        const string InstallMenu = "DNP Demo/Setup/Install Sound Effects";

        [Test]
        public void CatalogEntryWithoutClipIsSilent()
        {
            var catalog = ScriptableObject.CreateInstance<SeCatalog>();
            try
            {
                catalog.GetOrAdd(SeEvent.Fail);
                Assert.IsFalse(catalog.TryGet(SeEvent.Fail, out AudioClip clip, out _));
                Assert.IsNull(clip);
                Assert.IsFalse(catalog.TryGet(SeEvent.Complete, out _, out _), "An event without an entry is silent too.");
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlayerWithoutCatalogOrSourceDoesNothing()
        {
            var go = new GameObject("SePlayer");
            try
            {
                var player = go.AddComponent<SePlayer>();
                Assert.DoesNotThrow(() => player.Play(SeEvent.Complete));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SoundWaitsForTheLineAndPlaysWhenItEnds()
        {
            var hold = new SeHold();
            Assert.IsFalse(hold.Request(SeEvent.SelectConfirm, now: 10f, linePlaying: true), "Held while a line plays.");
            Assert.IsFalse(hold.TryRelease(10.5f, linePlaying: true, out _), "Still held while the line plays.");
            Assert.IsTrue(hold.TryRelease(11f, linePlaying: false, out SeEvent released), "Released once the line ended.");
            Assert.AreEqual(SeEvent.SelectConfirm, released);
            Assert.IsFalse(hold.TryRelease(11.1f, linePlaying: false, out _), "Released only once.");
        }

        [Test]
        public void SoundPlaysAtOnceWithoutALine()
        {
            var hold = new SeHold();
            Assert.IsTrue(hold.Request(SeEvent.Snap, now: 0f, linePlaying: false));
            Assert.IsFalse(hold.IsHolding);
        }

        [Test]
        public void NewerSoundReplacesTheHeldOne()
        {
            var hold = new SeHold();
            hold.Request(SeEvent.VoiceStart, now: 0f, linePlaying: true);
            hold.Request(SeEvent.Fail, now: 0.5f, linePlaying: true);
            Assert.IsTrue(hold.TryRelease(1f, linePlaying: false, out SeEvent released));
            Assert.AreEqual(SeEvent.Fail, released);
        }

        [Test]
        public void SoundHeldTooLongIsDropped()
        {
            var hold = new SeHold();
            hold.Request(SeEvent.Complete, now: 0f, linePlaying: true);
            Assert.IsFalse(hold.TryRelease(SeHold.MaxWaitSeconds + 0.1f, linePlaying: false, out _));
            Assert.IsFalse(hold.IsHolding, "A dropped sound is not played later.");
        }

        [Test]
        public void EffectsAreLoweredWhileALinePlays()
        {
            var ducking = new SeDucking();
            ducking.Step(linePlaying: true, deltaTime: SeDucking.FadeSeconds * 2f);
            Assert.AreEqual(SeDucking.LineGain, ducking.Gain, 1e-5f);
            ducking.Step(linePlaying: true, deltaTime: 1f);
            Assert.AreEqual(SeDucking.LineGain, ducking.Gain, 1e-5f, "Stays down while the line goes on.");
        }

        [Test]
        public void EffectsComeBackWhenTheLineEnds()
        {
            var ducking = new SeDucking();
            ducking.Step(linePlaying: true, deltaTime: 1f);
            ducking.Step(linePlaying: false, deltaTime: SeDucking.FadeSeconds * 2f);
            Assert.AreEqual(1f, ducking.Gain, 1e-5f);
        }

        [Test]
        public void EffectsFadeRatherThanJump()
        {
            var ducking = new SeDucking();
            float down = ducking.Step(linePlaying: true, deltaTime: SeDucking.FadeSeconds * 0.5f);
            Assert.Greater(down, SeDucking.LineGain, "Half way down after half the fade.");
            Assert.Less(down, 1f);
            ducking.Step(linePlaying: true, deltaTime: 1f);
            float up = ducking.Step(linePlaying: false, deltaTime: SeDucking.FadeSeconds * 0.5f);
            Assert.Greater(up, SeDucking.LineGain, "Half way up after half the fade.");
            Assert.Less(up, 1f);
        }

        [Test]
        public void EveryEventHasAFileName()
        {
            foreach (SeEvent seEvent in System.Enum.GetValues(typeof(SeEvent)))
            {
                Assert.IsTrue(SoundEffectsSetup.FileNames.ContainsKey(seEvent), $"{seEvent} has no file in {SoundEffectsSetup.Folder}.");
            }
        }

        [Test]
        public void DefaultStepsPlayTheVersionSoundAfterTheFirstLine()
        {
            AssertVersionSounds(ScenarioDataBuilder.CreateDefaultSteps(), "CreateDefaultSteps");
        }

        [Test]
        public void EnsuringTheVersionSoundsTwiceAddsNothing()
        {
            List<ScenarioStep> steps = ScenarioDataBuilder.CreateDefaultSteps();
            Assert.IsFalse(ScenarioDataBuilder.EnsureVersionSounds(steps));
            AssertVersionSounds(steps, "CreateDefaultSteps after EnsureVersionSounds");
        }

        [Test]
        public void ScenarioAssetPlaysTheVersionSounds()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data, ScenarioDataBuilder.AssetPath);
            AssertVersionSounds(data.Steps, $"{ScenarioDataBuilder.AssetPath} (run {InstallMenu})");
        }

        [Test]
        public void DemoSystemsRunnerHasASoundPlayer()
        {
            var systems = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(systems, ScenarioInstaller.DemoSystemsPrefabPath);
            var runner = systems.GetComponentInChildren<InteractionRunner>(true);
            Assert.IsNotNull(runner);
            var player = new SerializedObject(runner).FindProperty("_sounds").objectReferenceValue as SePlayer;
            Assert.IsNotNull(player, $"InteractionRunner._sounds is not set (run {InstallMenu}).");
            var so = new SerializedObject(player);
            Assert.IsNotNull(so.FindProperty("_catalog").objectReferenceValue, "SePlayer._catalog is not set.");
            Assert.IsNotNull(so.FindProperty("_stage").objectReferenceValue, $"SePlayer._stage is not set, so sounds do not wait for lines (run {InstallMenu}).");
            var source = so.FindProperty("_source").objectReferenceValue as AudioSource;
            Assert.IsNotNull(source, "SePlayer._source is not set.");
            Assert.AreEqual(0f, source.spatialBlend, "Interaction sounds are not positional.");
        }

        /// <summary>
        /// Each step of <see cref="ScenarioDataBuilder.VersionSounds"/> has its sound cue right after its first line (before
        /// it when it is the only line), never last and never holding the step, with the clip of the event when that file is
        /// placed; no other step has a sound cue.
        /// </summary>
        static void AssertVersionSounds(IReadOnlyList<ScenarioStep> steps, string source)
        {
            var expected = new Dictionary<string, SeEvent>();
            foreach ((string stepId, SeEvent sound) in ScenarioDataBuilder.VersionSounds)
            {
                expected.Add(stepId, sound);
            }
            int found = 0;
            foreach (ScenarioStep step in steps)
            {
                List<ScenarioCue> cues = step.Cues ?? new List<ScenarioCue>();
                int soundCount = cues.FindAll(cue => cue is SoundCue).Count;
                if (!expected.TryGetValue(step.Id, out SeEvent sound))
                {
                    Assert.AreEqual(0, soundCount, $"{source}: {step.Id} must have no sound cue.");
                    continue;
                }
                found++;
                Assert.AreEqual(1, soundCount, $"{source}: {step.Id} must have one sound cue.");
                Assert.GreaterOrEqual(cues.FindIndex(cue => cue is LineCue), 0, $"{source}: {step.Id} has no line.");
                int expectedIndex = ScenarioDataBuilder.VersionSoundIndex(cues.FindAll(cue => cue is not SoundCue));
                var soundCue = cues[expectedIndex] as SoundCue;
                Assert.IsNotNull(soundCue,
                    $"{source}: the sound of {step.Id} must come right after its first line (before it when it is the only line).");
                Assert.Less(expectedIndex, cues.Count - 1,
                    $"{source}: the sound of {step.Id} must not be the last cue, or the step change stops it at once.");
                Assert.IsFalse(soundCue.WaitForEnd, $"{source}: the sound of {step.Id} must not hold the step.");
                AudioClip clip = SoundEffectsSetup.LoadClip(sound);
                if (clip != null)
                {
                    Assert.AreSame(clip, soundCue.Clip, $"{source}: {step.Id} plays {SoundEffectsSetup.FileNames[sound]}.");
                }
            }
            Assert.AreEqual(expected.Count, found, $"{source}: a step of VersionSounds is missing.");
        }
    }
}
