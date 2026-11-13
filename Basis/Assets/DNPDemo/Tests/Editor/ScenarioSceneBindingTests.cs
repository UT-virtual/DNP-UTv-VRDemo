using System.Collections.Generic;
using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Selection;
using DNPDemo.Scenario;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DNPDemo.Tests
{
    /// <summary>
    /// Checks that DNPDemo_Main provides every speaker and object key the scenario data refers to,
    /// and that no binding is declared twice.
    /// </summary>
    public class ScenarioSceneBindingTests
    {
        Scene _scene;
        bool _openedByTest;

        [OneTimeSetUp]
        public void OpenScene()
        {
            _scene = SceneManager.GetSceneByPath(DNPDemoProjectSetup.MainScenePath);
            if (!_scene.isLoaded)
            {
                _scene = EditorSceneManager.OpenScene(DNPDemoProjectSetup.MainScenePath, OpenSceneMode.Additive);
                _openedByTest = true;
            }
        }

        [OneTimeTearDown]
        public void CloseScene()
        {
            if (_openedByTest && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }
        }

        [Test]
        public void SceneBindingsHaveNoDuplicates()
        {
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            Assert.IsEmpty(bindings.Problems, string.Join("\n", bindings.Problems));
        }

        [Test]
        public void SceneProvidesEverythingTheScenarioUses()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data, $"{ScenarioDataBuilder.AssetPath} (create it from DNP Demo/Create/Scenario Data)");

            List<string> missing = data.CollectRequirements().FindMissing(ScenarioBindingSet.Collect(_scene));
            Assert.IsEmpty(missing, string.Join("\n", missing));
        }

        [Test]
        public void SceneHasOneScenarioStage()
        {
            int count = 0;
            foreach (UnityEngine.GameObject root in _scene.GetRootGameObjects())
            {
                count += root.GetComponentsInChildren<ScenarioStage>(true).Length;
            }
            Assert.AreEqual(1, count, "DNPDemo_Main must contain exactly one ScenarioStage (in the DemoSystems prefab).");
        }

        [Test]
        public void SelectionZoneKeysResolveToDetectionVolumes()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data);
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            int checkedKeys = 0;
            foreach (ScenarioStep step in data.Steps)
            {
                foreach (InteractionPart part in step.Interaction)
                {
                    if (part is not SelectionPart selection)
                    {
                        continue;
                    }
                    foreach (string key in selection.TargetZoneKeys)
                    {
                        Assert.IsTrue(bindings.TryGetObject(key, out UnityEngine.GameObject target), $"{step.Id}: key '{key}' is not bound.");
                        Assert.IsNotNull(DetectionVolumes.Find(target), $"{step.Id}: '{key}' has neither a DetectionZoneGroup nor a DetectionZone.");
                        checkedKeys++;
                    }
                }
            }
            Assert.Greater(checkedKeys, 0, "The scenario should have a selection part to check.");
        }

        [Test]
        public void CollectingAnUnloadedSceneGivesAnEmptySet()
        {
            // A scene that is still loading (as in Awake) counts as not loaded, and has nothing to collect yet.
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(default);
            Assert.IsEmpty(bindings.Objects);
            Assert.IsEmpty(bindings.Speakers);
            Assert.IsEmpty(ScenarioBindingSet.Empty.Objects);
            Assert.IsEmpty(ScenarioBindingSet.Empty.Problems);
        }
    }
}
