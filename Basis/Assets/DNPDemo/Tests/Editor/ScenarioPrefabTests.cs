using Basis.Scripts.UI;
using DNPDemo.Editor;
using DNPDemo.Operator;
using DNPDemo.Players;
using DNPDemo.TaskHud;
using NUnit.Framework;
using TMPro;
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
        public void OperatorPanelHasTheMicrophoneThresholdControl()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioPrefabBuilder.OperatorPanelPath);
            Assert.IsNotNull(prefab);
            var control = prefab.GetComponentInChildren<MicrophoneThresholdControl>(true);
            Assert.IsNotNull(control, "The operator panel needs the voice threshold section.");
            var so = new SerializedObject(control);
            foreach (string field in new[] { "_deviceText", "_switchDeviceButton", "_levelText", "_meterFill", "_thresholdMarker", "_lowerButton", "_raiseButton", "_resetButton" })
            {
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, $"MicrophoneThresholdControl.{field} is not set.");
            }

            // A long device name must not squeeze the switch button.
            var button = (Button)so.FindProperty("_switchDeviceButton").objectReferenceValue;
            var layout = button.GetComponent<LayoutElement>();
            Assert.IsNotNull(layout);
            Assert.Greater(layout.minWidth, 0f);
            Assert.AreEqual(layout.preferredWidth, layout.minWidth, "The switch button has a fixed width.");
            Assert.LessOrEqual(layout.flexibleWidth, 0f);
        }

        [Test]
        public void OperatorPanelHasTheRecoveryControls()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioPrefabBuilder.OperatorPanelPath);
            Assert.IsNotNull(prefab);
            var panel = prefab.GetComponent<OperatorPanel>();
            Assert.IsNotNull(panel);
            var so = new SerializedObject(panel);
            foreach (string field in new[]
                     {
                         "_statusText", "_backButton", "_nextButton", "_resumeButton", "_resumeText", "_jumpTargetText",
                         "_jumpPreviousButton", "_jumpNextButton", "_jumpButton", "_restartButton", "_closeButton",
                     })
            {
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue,
                    $"OperatorPanel.{field} is not set (run DNP Demo/Setup/Install Scenario Into DemoSystems Prefab).");
            }
            var restart = (Button)so.FindProperty("_restartButton").objectReferenceValue;
            Assert.AreEqual(ScenarioPrefabBuilder.OperatorRestartLabel, restart.GetComponentInChildren<TMP_Text>(true).text);
        }

        [Test]
        public void DemoSystemsResetsForTheNextVisitor()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(prefab);
            var reset = prefab.GetComponentInChildren<VisitorReset>(true);
            Assert.IsNotNull(reset, "Install it from DNP Demo/Setup/Install Scenario Into DemoSystems Prefab.");
            var so = new SerializedObject(reset);
            foreach (string field in new[] { "_director", "_spawn", "_pen" })
            {
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, $"VisitorReset.{field} is not set.");
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
            foreach (string field in new[] { "_content", "_follower", "_headingText", "_taskText", "_linesText", "_statusText", "_modeHintText", "_hintText" })
            {
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, $"TaskHudView.{field} is not set.");
            }
            // The HUD sits by the hand the laser comes from; it must not catch the laser meant for menus and panels.
            Assert.IsEmpty(prefab.GetComponentsInChildren<BasisGraphicUIRayCaster>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<GraphicRaycaster>(true));
        }
    }
}
