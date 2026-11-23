using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Interaction.History;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Interaction.Selection;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Interaction.Voice;
using DNPDemo.Pen;
using DNPDemo.Scenario;
using DNPDemo.Venus;
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
        public void HistoryPanelCanvasIsOnUILayerAndPreviewIsOutsideIt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InteractionPrefabBuilder.HistoryToolPath);
            Assert.IsNotNull(prefab, $"{InteractionPrefabBuilder.HistoryToolPath} (create it from DNP Demo/Create/Interaction Prefabs)");
            var panel = prefab.GetComponentInChildren<HistoryPanel>(true);
            Assert.IsNotNull(panel);
            var so = new SerializedObject(panel);
            foreach (string field in new[]
            {
                "_content", "_preview", "_versionText", "_pageText", "_emptyText", "_statusText",
                "_closeButton", "_previousButton", "_nextButton", "_restoreButton",
            })
            {
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, $"HistoryPanel.{field} is not set.");
            }

            var content = (GameObject)so.FindProperty("_content").objectReferenceValue;
            Assert.IsNotNull(content.GetComponent<Canvas>());
            int uiLayer = LayerMask.NameToLayer(WorldUiBuilder.UILayerName);
            foreach (Transform t in content.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(uiLayer, t.gameObject.layer,
                    $"'{t.name}' must be on the {WorldUiBuilder.UILayerName} layer so the Basis laser can hit it.");
            }

            var preview = (HistoryPreview)so.FindProperty("_preview").objectReferenceValue;
            Assert.IsFalse(preview.transform.IsChildOf(content.transform),
                "The canvas is switched off while closed; the preview must stay active so its statue builds the models at scene load.");
            Assert.IsTrue(preview.gameObject.activeSelf);
            var statue = (VenusStatue)new SerializedObject(preview).FindProperty("_statue").objectReferenceValue;
            Assert.IsNotNull(statue, "HistoryPreview needs its statue.");
            Assert.IsNotNull(statue.Catalog, "The preview statue needs the version catalog.");
            Assert.IsNull(statue.Baseline, "The preview shows nothing until the panel opens.");
            Assert.IsNull(statue.GetComponent<ScenarioObjectBinding>(), "The preview is not a stage object; the stage must not reset it.");

            Assert.AreSame(panel, new SerializedObject(prefab.GetComponent<HistoryTool>()).FindProperty("_panel").objectReferenceValue);
        }

        [Test]
        public void DemoSystemsHistoryToolIsWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(prefab);
            var runner = prefab.GetComponentInChildren<InteractionRunner>(true);
            Assert.IsNotNull(runner);
            var history = runner.GetComponentInChildren<HistoryTool>(true);
            Assert.IsNotNull(history, "Install it from DNP Demo/Setup/Install Scenario Into DemoSystems Prefab.");

            var tool = new SerializedObject(history);
            Assert.IsNotNull(tool.FindProperty("_stage").objectReferenceValue, "HistoryTool needs the scenario stage.");
            var panel = (HistoryPanel)tool.FindProperty("_panel").objectReferenceValue;
            Assert.IsNotNull(panel, "HistoryTool needs its panel.");
            Assert.IsNotNull(new SerializedObject(panel).FindProperty("_director").objectReferenceValue, "HistoryPanel needs the director.");

            SerializedProperty tools = new SerializedObject(runner).FindProperty("_tools");
            bool registered = false;
            for (int i = 0; i < tools.arraySize; i++)
            {
                registered |= tools.GetArrayElementAtIndex(i).objectReferenceValue == history;
            }
            Assert.IsTrue(registered, "HistoryTool is not registered with the runner.");
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
        public void DemoSystemsVoiceCommandToolIsWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(prefab);
            var runner = prefab.GetComponentInChildren<InteractionRunner>(true);
            Assert.IsNotNull(runner);
            var voice = runner.GetComponentInChildren<VoiceCommandTool>(true);
            Assert.IsNotNull(voice, "Install it from DNP Demo/Setup/Install Scenario Into DemoSystems Prefab.");
            Assert.IsNotNull(new SerializedObject(voice).FindProperty("_pen").objectReferenceValue, "VoiceCommandTool needs the pen controller.");

            SerializedProperty tools = new SerializedObject(runner).FindProperty("_tools");
            bool registered = false;
            for (int i = 0; i < tools.arraySize; i++)
            {
                registered |= tools.GetArrayElementAtIndex(i).objectReferenceValue == voice;
            }
            Assert.IsTrue(registered, "VoiceCommandTool is not registered with the runner.");
        }

        [Test]
        public void SpatialPenShowsEveryModePart()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpatialPenPrefabPath);
            Assert.IsNotNull(prefab, SpatialPenPrefabPath);
            var view = prefab.GetComponent<SpatialPenView>();
            Assert.IsNotNull(view);
            var so = new SerializedObject(view);
            foreach (string field in new[] { "_body", "_microphone", "_selectArea", "_eraser", "_manipulator" })
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

        [Test]
        public void ScenarioAssetRunsVoiceCommandsOnTheVoiceTool()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data);
            foreach (ScenarioStep step in data.Steps)
            {
                if (step?.Interaction == null)
                {
                    continue;
                }
                foreach (InteractionPart part in step.Interaction)
                {
                    if (part != null && part.Mode == PenMode.Voice)
                    {
                        Assert.IsInstanceOf<VoicePart>(part, $"{step.Id}: a voice command is still a stand-in.");
                    }
                }
            }
        }
    }
}
