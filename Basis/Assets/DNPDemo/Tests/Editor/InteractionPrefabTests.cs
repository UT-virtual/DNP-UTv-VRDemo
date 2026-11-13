using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Interaction.Selection;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Pen;
using DNPDemo.Scenario;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class InteractionPrefabTests
    {
        const string SpatialPenPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Pen/SpatialPen.prefab";

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
        public void DemoSystemsSelectionToolIsWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(prefab);
            var runner = prefab.GetComponentInChildren<InteractionRunner>(true);
            Assert.IsNotNull(runner);
            var selection = runner.GetComponentInChildren<SelectionTool>(true);
            Assert.IsNotNull(selection, "Install it from DNP Demo/Setup/Install Scenario Into DemoSystems Prefab.");

            var tool = new SerializedObject(selection);
            Assert.IsNotNull(tool.FindProperty("_pen").objectReferenceValue, "SelectionTool needs the pen controller.");
            Assert.IsNotNull(tool.FindProperty("_stage").objectReferenceValue, "SelectionTool needs the scenario stage.");
            var view = (SelectionVolumeView)tool.FindProperty("_view").objectReferenceValue;
            Assert.IsNotNull(view, "SelectionTool needs its view.");
            var material = (Material)new SerializedObject(view).FindProperty("_material").objectReferenceValue;
            Assert.IsNotNull(material, "SelectionVolumeView needs its material.");
            Assert.AreEqual(InteractionPrefabBuilder.SelectionVolumeShaderName, material.shader.name);
            Assert.IsTrue(material.enableInstancing, "The selection is drawn with GPU instancing.");

            SerializedProperty tools = new SerializedObject(runner).FindProperty("_tools");
            bool registered = false;
            for (int i = 0; i < tools.arraySize; i++)
            {
                registered |= tools.GetArrayElementAtIndex(i).objectReferenceValue == selection;
            }
            Assert.IsTrue(registered, "SelectionTool is not registered with the runner.");
        }

        [Test]
        public void DemoSystemsStrokeToolIsWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(prefab);
            var runner = prefab.GetComponentInChildren<InteractionRunner>(true);
            Assert.IsNotNull(runner);
            var stroke = runner.GetComponentInChildren<StrokeTool>(true);
            Assert.IsNotNull(stroke, "Install it from DNP Demo/Setup/Install Scenario Into DemoSystems Prefab.");

            var tool = new SerializedObject(stroke);
            Assert.IsNotNull(tool.FindProperty("_pen").objectReferenceValue, "StrokeTool needs the pen controller.");
            Assert.IsNotNull(tool.FindProperty("_stage").objectReferenceValue, "StrokeTool needs the scenario stage.");
            var view = (StrokeView)tool.FindProperty("_view").objectReferenceValue;
            Assert.IsNotNull(view, "StrokeTool needs its view.");
            Assert.IsNotNull(new SerializedObject(view).FindProperty("_material").objectReferenceValue, "StrokeView needs its material.");

            SerializedProperty tools = new SerializedObject(runner).FindProperty("_tools");
            bool registered = false;
            for (int i = 0; i < tools.arraySize; i++)
            {
                registered |= tools.GetArrayElementAtIndex(i).objectReferenceValue == stroke;
            }
            Assert.IsTrue(registered, "StrokeTool is not registered with the runner.");
        }

        [Test]
        public void DemoSystemsManipulateToolIsWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(prefab);
            var runner = prefab.GetComponentInChildren<InteractionRunner>(true);
            Assert.IsNotNull(runner);
            var manipulate = runner.GetComponentInChildren<ManipulateTool>(true);
            Assert.IsNotNull(manipulate, "Install it from DNP Demo/Setup/Install Scenario Into DemoSystems Prefab.");

            var tool = new SerializedObject(manipulate);
            Assert.IsNotNull(tool.FindProperty("_pen").objectReferenceValue, "ManipulateTool needs the pen controller.");
            Assert.IsNotNull(tool.FindProperty("_stage").objectReferenceValue, "ManipulateTool needs the scenario stage.");
            var ghost = (ManipulateGhostView)tool.FindProperty("_ghost").objectReferenceValue;
            Assert.IsNotNull(ghost, "ManipulateTool needs its ghost view.");
            var material = (Material)new SerializedObject(ghost).FindProperty("_material").objectReferenceValue;
            Assert.IsNotNull(material, "ManipulateGhostView needs its material.");
            Assert.GreaterOrEqual(material.renderQueue, (int)UnityEngine.Rendering.RenderQueue.Transparent, "The ghost is drawn translucent.");

            SerializedProperty tools = new SerializedObject(runner).FindProperty("_tools");
            bool registered = false;
            for (int i = 0; i < tools.arraySize; i++)
            {
                registered |= tools.GetArrayElementAtIndex(i).objectReferenceValue == manipulate;
            }
            Assert.IsTrue(registered, "ManipulateTool is not registered with the runner.");
        }

        [Test]
        public void SpatialPenShowsEveryModePart()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpatialPenPrefabPath);
            Assert.IsNotNull(prefab, SpatialPenPrefabPath);
            var view = prefab.GetComponent<SpatialPenView>();
            Assert.IsNotNull(view);
            var so = new SerializedObject(view);
            foreach (string field in new[] { "_body", "_microphone", "_selectArea", "_eraser" })
            {
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, $"SpatialPenView.{field} is not set.");
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
