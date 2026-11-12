using DNPDemo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class ScenarioPrefabTests
    {
        [Test]
        public void OperatorPanelIsOnUILayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioPrefabBuilder.OperatorPanelPath);
            Assert.IsNotNull(prefab, $"{ScenarioPrefabBuilder.OperatorPanelPath} (create it from DNP Demo/Create/Scenario Prefabs)");

            int uiLayer = LayerMask.NameToLayer(WorldUiBuilder.UILayerName);
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(uiLayer, t.gameObject.layer,
                    $"'{t.name}' must be on the {WorldUiBuilder.UILayerName} layer so the Basis laser can hit it.");
            }
        }
    }
}
