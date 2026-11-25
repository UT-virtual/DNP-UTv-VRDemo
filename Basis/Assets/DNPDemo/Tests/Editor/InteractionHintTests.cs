using System.Collections.Generic;
using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Hint;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Interaction.Selection;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Interaction.Voice;
using DNPDemo.Scenario;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class InteractionHintTests
    {
        [Test]
        public void HintAppearsOnceWhenTheDelayHasPassed()
        {
            var timer = new InteractionHintTimer();
            timer.Start(new InteractionHint("x", 20f), now: 100f);

            Assert.IsFalse(timer.Tick(119.9f));
            Assert.IsFalse(timer.IsShown);
            Assert.IsTrue(timer.Tick(120f));
            Assert.IsTrue(timer.IsShown);
            Assert.IsFalse(timer.Tick(130f), "The hint appears only once per part.");
            Assert.IsTrue(timer.IsShown);
        }

        [Test]
        public void ZeroDelayMeansNoHint()
        {
            var timer = new InteractionHintTimer();
            timer.Start(new InteractionHint("x", 0f), now: 0f);
            Assert.IsFalse(timer.Tick(1000f));

            timer.Start(null, now: 0f);
            Assert.IsFalse(timer.Tick(1000f));
        }

        [Test]
        public void StartingAgainCountsFromTheNewStart()
        {
            var timer = new InteractionHintTimer();
            timer.Start(new InteractionHint("x", 10f), now: 0f);
            Assert.IsTrue(timer.Tick(10f));

            timer.Start(new InteractionHint("y", 10f), now: 50f);
            Assert.IsFalse(timer.IsShown);
            Assert.IsFalse(timer.Tick(59f));
            Assert.IsTrue(timer.Tick(60f));

            timer.Clear();
            Assert.IsFalse(timer.IsShown);
            Assert.IsFalse(timer.Tick(1000f));
        }

        [Test]
        public void NewPartsShowAHintAfterTheDefaultDelay()
        {
            Assert.AreEqual(InteractionHint.DefaultDelaySeconds, new SelectionPart().Hint.DelaySeconds);
            Assert.AreEqual(InteractionHint.DefaultDelaySeconds, new VoicePart().Hint.DelaySeconds);
        }

        [Test]
        public void HintLightsUpThePartsOwnZones()
        {
            CollectionAssert.AreEqual(new[] { "Zone_A", "Zone_B" }, HintZones(new SelectionPart("x", "Zone_A", "Zone_B")));
            CollectionAssert.AreEqual(new[] { "Start", "End" }, HintZones(StrokePart.Guide("x", "Start", "End")));
            CollectionAssert.IsEmpty(HintZones(StrokePart.Sketch("x")));
            CollectionAssert.AreEqual(new[] { "Apple" }, HintZones(ManipulatePart.Translate("x", "Apple", "Goal")),
                "Without a grab zone the target itself is grabbed; the goal is shown by the ghost.");
            ManipulatePart withGrabZone = ManipulatePart.Translate("x", "Apple", "Goal");
            withGrabZone.GrabZoneKey = "Zone_Grab";
            CollectionAssert.AreEqual(new[] { "Zone_Grab" }, HintZones(withGrabZone));
            CollectionAssert.IsEmpty(HintZones(new VoicePart("x", "y")));
        }

        [Test]
        public void HintZoneKeysReplaceThePartsOwnZones()
        {
            var part = new SelectionPart("x", "Zone_A");
            part.Hint.ZoneKeys = new List<string> { "Zone_Hint", "", "Zone_Hint" };
            CollectionAssert.AreEqual(new[] { "Zone_Hint" }, HintZones(part));

            part.Hint.ZoneKeys = new List<string> { " " };
            CollectionAssert.AreEqual(new[] { "Zone_A" }, HintZones(part), "Blank keys leave the part's own zones.");
        }

        [Test]
        public void ValidateChecksHintZoneKeys()
        {
            var part = new SelectionPart("x", "Zone_A");
            part.Hint.ZoneKeys = new List<string> { "Zone_Hint", "", "Zone_Hint" };
            var steps = new List<ScenarioStep>
            {
                new()
                {
                    Id = "hint", Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerA,
                    Interaction = new List<InteractionPart> { part },
                },
            };
            List<string> problems = ScenarioData.Validate(steps);
            // empty key, duplicate key
            Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
        }

        [Test]
        public void RequirementsIncludeTheHintZones()
        {
            var part = new VoicePart("x", "y");
            part.Hint.ZoneKeys = new List<string> { "Zone_Hint" };
            var data = ScriptableObject.CreateInstance<ScenarioData>();
            try
            {
                data.SetSteps(new List<ScenarioStep>
                {
                    new()
                    {
                        Id = "voice", Completion = StepCompletion.Interaction, Actor = DemoRole.PlayerA,
                        Interaction = new List<InteractionPart> { part },
                    },
                });
                CollectionAssert.AreEquivalent(new[] { "Zone_Hint" }, data.CollectRequirements().ObjectKeys);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void EveryDefaultOperationHasAHint()
        {
            AssertEveryPartHasAHint(ScenarioDataBuilder.CreateDefaultSteps(), "default scenario");
        }

        [Test]
        public void EveryOperationOfTheScenarioAssetHasAHint()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data, ScenarioDataBuilder.AssetPath);
            AssertEveryPartHasAHint(data.Steps, ScenarioDataBuilder.AssetPath);
        }

        [Test]
        public void ZoneMeshesFaceOutwards()
        {
            AssertFacesOutwards(ZoneShapeMeshes.CreateBox());
            AssertFacesOutwards(ZoneShapeMeshes.CreateCapsule(0.1f, 0.4f));
        }

        [Test]
        public void CapsuleMeshHasTheZonesSize()
        {
            Mesh capsule = ZoneShapeMeshes.CreateCapsule(0.1f, 0.4f);
            try
            {
                Assert.AreEqual(0.2f, capsule.bounds.size.x, 1e-4f);
                Assert.AreEqual(0.4f, capsule.bounds.size.y, 1e-4f);
                Assert.AreEqual(0f, capsule.bounds.center.y, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(capsule);
            }
        }

        static List<string> HintZones(InteractionPart part)
        {
            var keys = new List<string>();
            part.CollectHintZoneKeys(keys);
            return keys;
        }

        static void AssertEveryPartHasAHint(IReadOnlyList<ScenarioStep> steps, string source)
        {
            foreach (ScenarioStep step in steps)
            {
                if (step?.Interaction == null)
                {
                    continue;
                }
                for (int i = 0; i < step.Interaction.Count; i++)
                {
                    InteractionHint hint = step.Interaction[i]?.Hint;
                    Assert.IsNotNull(hint, $"{source}: {step.Id} part {i}");
                    Assert.IsTrue(hint.IsEnabled, $"{source}: {step.Id} part {i} shows no hint.");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(hint.Text), $"{source}: {step.Id} part {i} needs a hint text.");
                }
            }
        }

        /// <summary>Unity draws the side where the cross product of a triangle's edges points; on a convex shape around the origin that is away from it.</summary>
        static void AssertFacesOutwards(Mesh mesh)
        {
            try
            {
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Assert.Greater(triangles.Length, 0);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]];
                    Vector3 b = vertices[triangles[i + 1]];
                    Vector3 c = vertices[triangles[i + 2]];
                    Assert.GreaterOrEqual(Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c), 0f, $"{mesh.name} triangle {i / 3}");
                }
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
