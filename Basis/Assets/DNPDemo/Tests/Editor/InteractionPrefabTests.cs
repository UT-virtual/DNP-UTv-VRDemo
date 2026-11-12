using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Scenario;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class InteractionPrefabTests
    {
        [Test]
        public void DummyToolPanelIsOnUILayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InteractionPrefabBuilder.DummyToolPath);
            Assert.IsNotNull(prefab, $"{InteractionPrefabBuilder.DummyToolPath} (create it from DNP Demo/Create/Interaction Prefabs)");

            int uiLayer = LayerMask.NameToLayer(WorldUiBuilder.UILayerName);
            Transform panel = prefab.transform.Find("Panel");
            Assert.IsNotNull(panel);
            foreach (Transform t in panel.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(uiLayer, t.gameObject.layer,
                    $"'{t.name}' must be on the {WorldUiBuilder.UILayerName} layer so the Basis laser can hit it.");
            }
        }

        [Test]
        public void DemoSystemsRunnerHasTools()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(prefab);
            var runner = prefab.GetComponentInChildren<InteractionRunner>(true);
            Assert.IsNotNull(runner, "Install it from DNP Demo/Setup/Install Scenario Into DemoSystems Prefab.");

            var so = new SerializedObject(runner);
            Assert.IsNotNull(so.FindProperty("_director").objectReferenceValue);
            Assert.IsNotNull(so.FindProperty("_roles").objectReferenceValue);
            SerializedProperty tools = so.FindProperty("_tools");
            Assert.Greater(tools.arraySize, 0);
            for (int i = 0; i < tools.arraySize; i++)
            {
                Assert.IsNotNull(tools.GetArrayElementAtIndex(i).objectReferenceValue, $"Tool {i} is missing.");
            }
        }

        [Test]
        public void ScenarioAssetIsValid()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data);
            CollectionAssert.IsEmpty(data.Validate());
        }
    }
}
