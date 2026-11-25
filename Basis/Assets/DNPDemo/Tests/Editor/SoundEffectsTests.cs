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
        public void EveryEventHasAFileName()
        {
            foreach (SeEvent seEvent in System.Enum.GetValues(typeof(SeEvent)))
            {
                Assert.IsTrue(SoundEffectsSetup.FileNames.ContainsKey(seEvent), $"{seEvent} has no file in {SoundEffectsSetup.Folder}.");
            }
        }

        [Test]
        public void DefaultStepsPlayTheVersionSoundBeforeTheStatueChanges()
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
            var source = so.FindProperty("_source").objectReferenceValue as AudioSource;
            Assert.IsNotNull(source, "SePlayer._source is not set.");
            Assert.AreEqual(0f, source.spatialBlend, "Interaction sounds are not positional.");
        }

        /// <summary>
        /// Each step of <see cref="ScenarioDataBuilder.VersionSounds"/> has its sound cue right before its first statue cue,
        /// with the clip of the event when that file is placed, and no other step has a sound cue.
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
                int index = cues.FindIndex(cue => cue is StatueVersionCue);
                Assert.Greater(index, 0, $"{source}: {step.Id} has no statue cue after its sound.");
                Assert.IsInstanceOf<SoundCue>(cues[index - 1], $"{source}: the sound of {step.Id} must come right before the statue cue.");
                AudioClip clip = SoundEffectsSetup.LoadClip(sound);
                if (clip != null)
                {
                    Assert.AreSame(clip, ((SoundCue)cues[index - 1]).Clip, $"{source}: {step.Id} plays {SoundEffectsSetup.FileNames[sound]}.");
                }
            }
            Assert.AreEqual(expected.Count, found, $"{source}: a step of VersionSounds is missing.");
        }
    }
}
