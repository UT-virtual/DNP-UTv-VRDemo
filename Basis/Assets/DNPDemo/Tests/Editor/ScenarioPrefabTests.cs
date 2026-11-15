using Basis.Scripts.UI;
using DNPDemo.Editor;
using DNPDemo.TaskHud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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

        [Test]
        public void TaskHudIsWiredAndIsNoLaserTarget()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioPrefabBuilder.TaskHudPath);
            Assert.IsNotNull(prefab, $"{ScenarioPrefabBuilder.TaskHudPath} (create it from DNP Demo/Create/Scenario Prefabs)");

            var view = prefab.GetComponent<TaskHudView>();
            Assert.IsNotNull(view);
            var so = new SerializedObject(view);
            foreach (string field in new[] { "_content", "_follower", "_headingText", "_taskText", "_linesText", "_modeHintText" })
            {
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, $"TaskHudView.{field} is not set.");
            }
            // The HUD sits by the hand the laser comes from; it must not catch the laser meant for menus and panels.
            Assert.IsEmpty(prefab.GetComponentsInChildren<BasisGraphicUIRayCaster>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<GraphicRaycaster>(true));
        }
    }
}
