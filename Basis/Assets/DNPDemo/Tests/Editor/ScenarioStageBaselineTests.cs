using System.Collections.Generic;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    /// <summary>
    /// <see cref="ScenarioStage.ResetToBaseline"/> returns keyed objects to their state at scene load, so what a tool or a
    /// cue changed during a step is undone when the step changes.
    /// </summary>
    public class ScenarioStageBaselineTests
    {
        // Unlikely to clash with a binding of whatever scene the test runner has open.
        const string Key = "ScenarioStageBaselineTests_Target";

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

        [Test]
        public void ResetRestoresLocalPoseAndActiveState()
        {
            GameObject root = Create("Statue");
            root.transform.position = new Vector3(1f, 0f, 2f);
            GameObject target = Create(Key);
            target.transform.SetParent(root.transform, false);
            TestBinding.Bind(target, Key);
            target.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.5f, -0.5f), Quaternion.Euler(0f, 30f, 0f));
            target.SetActive(false);

            var stage = Create("Stage").AddComponent<ScenarioStage>();
            Assume.That(stage.gameObject.scene.isLoaded, "The stage collects bindings only from a loaded scene.");
            // Collects the bindings and records the baseline.
            Assert.IsTrue(stage.TryGetObject(Key, out GameObject found));
            Assert.AreSame(target, found);

            target.SetActive(true);
            target.transform.SetPositionAndRotation(new Vector3(5f, 5f, 5f), Quaternion.Euler(10f, 20f, 30f));
            stage.ResetToBaseline();

            Assert.IsFalse(target.activeSelf);
            Assert.Less(Vector3.Distance(new Vector3(0f, 1.5f, -0.5f), target.transform.localPosition), 1e-5f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 30f, 0f), target.transform.localRotation), 0.01f);
        }

        [Test]
        public void MatchPoseCueKeepsAMovedObjectAtItsMarkerAfterAReset()
        {
            const string markerKey = Key + "_Marker";
            GameObject target = Create(Key);
            TestBinding.Bind(target, Key);
            GameObject marker = Create(markerKey);
            TestBinding.Bind(marker, markerKey);
            marker.transform.SetPositionAndRotation(new Vector3(0.2f, 1.4f, -0.6f), Quaternion.Euler(0f, 45f, 0f));

            var stage = Create("Stage").AddComponent<ScenarioStage>();
            Assume.That(stage.gameObject.scene.isLoaded, "The stage collects bindings only from a loaded scene.");
            Assert.IsTrue(stage.TryGetObject(Key, out _));
            var scenario = ScriptableObject.CreateInstance<ScenarioData>();
            try
            {
                // What the player rebuilding the next step does: reset, then apply the lasting cues of earlier steps.
                stage.ResetToBaseline();
                new MatchPoseCue(Key, markerKey).ApplyInstant(new ScenarioCueContext(stage, scenario));
            }
            finally
            {
                Object.DestroyImmediate(scenario);
            }

            Assert.Less(Vector3.Distance(marker.transform.position, target.transform.position), 1e-5f);
            Assert.Less(Quaternion.Angle(marker.transform.rotation, target.transform.rotation), 0.01f);
        }
    }
}
