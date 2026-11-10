using System.Collections.Generic;
using DNPDemo.Editor;
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
    }
}
