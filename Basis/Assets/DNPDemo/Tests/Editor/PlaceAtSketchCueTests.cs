using System.Collections.Generic;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Presentation;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    /// <summary>
    /// <see cref="PlaceAtSketchCue"/> puts an object where a sketch was drawn, within reach of its marker, and
    /// <see cref="ScaleInEffect"/> grows it in without leaving it small when the step changes.
    /// </summary>
    public class PlaceAtSketchCueTests
    {
        const string SketchStepId = "sketch";

        // Unlikely to clash with a binding of whatever scene the test runner has open.
        const string Key = "PlaceAtSketchCueTests_Target";
        const string MarkerKey = "PlaceAtSketchCueTests_Marker";

        readonly List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _created.Clear();
        }

        GameObject Create(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        /// <summary>Lines drawn in the step <paramref name="stepId"/>, as <see cref="StrokeTool.LastResult"/> keeps them.</summary>
        static StrokeResult Sketch(string stepId, params Vector3[][] lines)
        {
            var set = new StrokeSet();
            foreach (Vector3[] points in lines)
            {
                StrokeLine line = set.Begin(points[0]);
                for (int i = 1; i < points.Length; i++)
                {
                    set.Extend(line, points[i]);
                }
                set.Finish(line);
            }
            return StrokeResult.Capture(new ScenarioStep { Id = stepId }, StrokePart.Sketch("x"), set);
        }

        [Test]
        public void BoundsCoverEveryLine()
        {
            StrokeResult sketch = Sketch(SketchStepId,
                new[] { new Vector3(0f, 1f, 0f), new Vector3(0.2f, 1f, 0f) },
                new[] { new Vector3(0.1f, 1.3f, -0.1f), new Vector3(0.1f, 0.9f, 0.1f) });

            Assert.IsTrue(sketch.TryGetBounds(out Bounds bounds));
            Assert.Less(Vector3.Distance(new Vector3(0.1f, 1.1f, 0f), bounds.center), 1e-5f);
            Assert.Less(Vector3.Distance(new Vector3(0.2f, 0.4f, 0.2f), bounds.size), 1e-5f);
            Assert.IsFalse(Sketch(SketchStepId).TryGetBounds(out _), "Nothing drawn has no bounds.");
        }

        [Test]
        public void ObjectGoesToTheCentreOfTheSketch()
        {
            StrokeResult sketch = Sketch(SketchStepId,
                new[] { new Vector3(0f, 1f, -1f), new Vector3(0.2f, 1.2f, -1f) });

            Vector3 position = PlaceAtSketchCue.ResolvePosition(sketch, SketchStepId, new Vector3(0f, 1f, -1.2f), 0.8f);

            Assert.Less(Vector3.Distance(new Vector3(0.1f, 1.1f, -1f), position), 1e-5f);
        }

        [Test]
        public void SketchFarFromTheMarkerIsPulledWithinReach()
        {
            StrokeResult sketch = Sketch(SketchStepId,
                new[] { new Vector3(3f, 1f, 0f), new Vector3(3f, 1.1f, 0f) });
            var marker = new Vector3(0f, 1.05f, 0f);

            Vector3 position = PlaceAtSketchCue.ResolvePosition(sketch, SketchStepId, marker, 0.8f);

            Assert.Less(Vector3.Distance(new Vector3(0.8f, 1.05f, 0f), position), 1e-5f, "Kept 0.8 m from the marker, towards the sketch.");
        }

        [Test]
        public void WithoutTheSketchTheObjectGoesToTheMarker()
        {
            var marker = new Vector3(0.3f, 1.2f, -0.9f);
            StrokeResult otherStep = Sketch("guide", new[] { Vector3.zero, Vector3.one });

            Assert.AreEqual(marker, PlaceAtSketchCue.ResolvePosition(null, SketchStepId, marker, 0.8f), "No lines kept on this device.");
            Assert.AreEqual(marker, PlaceAtSketchCue.ResolvePosition(Sketch(SketchStepId), SketchStepId, marker, 0.8f), "The step was skipped.");
            Assert.AreEqual(marker, PlaceAtSketchCue.ResolvePosition(otherStep, SketchStepId, marker, 0.8f), "The lines of another step do not count.");
        }

        [Test]
        public void ApplyInstantShowsTheObjectAtTheMarkerWhenNoSketchIsKept()
        {
            GameObject target = Create(Key);
            TestBinding.Bind(target, Key);
            target.SetActive(false);
            GameObject marker = Create(MarkerKey);
            TestBinding.Bind(marker, MarkerKey);
            marker.transform.SetPositionAndRotation(new Vector3(-0.3f, 1.5f, -1.4f), Quaternion.Euler(20f, 40f, 0f));

            var stage = Create("Stage").AddComponent<ScenarioStage>();
            Assume.That(stage.gameObject.scene.isLoaded, "The stage collects bindings only from a loaded scene.");
            var scenario = ScriptableObject.CreateInstance<ScenarioData>();
            try
            {
                // A key without a StrokeTool behind it: this device kept no sketch.
                var cue = new PlaceAtSketchCue(Key, MarkerKey, SketchStepId) { StrokesKey = "PlaceAtSketchCueTests_NoTool" };
                LogAssertNoTool();
                cue.ApplyInstant(new ScenarioCueContext(stage, scenario));
            }
            finally
            {
                Object.DestroyImmediate(scenario);
            }

            Assert.IsTrue(target.activeSelf);
            Assert.Less(Vector3.Distance(marker.transform.position, target.transform.position), 1e-5f);
            Assert.Less(Quaternion.Angle(marker.transform.rotation, target.transform.rotation), 0.01f);
        }

        // The missing key is reported once as a warning by the stage.
        static void LogAssertNoTool() =>
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("PlaceAtSketchCueTests_NoTool"));

        [Test]
        public void ScaleInEffectReturnsToFullSizeOnReset()
        {
            GameObject root = Create("Mirror");
            GameObject visual = Create("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(1f, 2f, 1f);
            var effect = root.AddComponent<ScaleInEffect>();
            var serialized = new SerializedObject(effect);
            serialized.FindProperty("_visual").objectReferenceValue = visual.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            effect.Play(0.5f);
            Assert.IsTrue(effect.IsPlaying);
            Assert.AreEqual(Vector3.zero, visual.transform.localScale, "It starts from nothing.");

            effect.ResetToBaseline();
            Assert.IsFalse(effect.IsPlaying);
            Assert.AreEqual(new Vector3(1f, 2f, 1f), visual.transform.localScale, "Cut mid-way, it is back to its full size.");

            effect.Play(0f);
            Assert.IsFalse(effect.IsPlaying);
            Assert.AreEqual(new Vector3(1f, 2f, 1f), visual.transform.localScale, "With no time to grow, it shows at once.");
        }
    }
}
