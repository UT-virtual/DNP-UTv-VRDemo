using System;
using System.IO;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Selection;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Interaction.Stroke.Testing;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static DNPDemo.Editor.WorldUiBuilder;
using Object = UnityEngine.Object;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Generates the initial prefabs of the interaction tools. Existing prefabs are kept as they are;
    /// after creation they are edited by hand.
    /// </summary>
    public static class InteractionPrefabBuilder
    {
        const string Folder = DNPDemoProjectSetup.Root + "/Prefabs/Interaction";
        public const string DummyToolPath = Folder + "/DummyTool.prefab";
        public const string SelectionToolPath = Folder + "/SelectionTool.prefab";
        public const string StrokeToolPath = Folder + "/StrokeTool.prefab";
        public const string StrokeTestPanelPath = Folder + "/Testing/StrokeTestPanel.prefab";

        public const string SelectionVolumeShaderName = "DNPDemo/SelectionVolume";
        public const string SelectionVolumeMaterialPath = DNPDemoProjectSetup.Root + "/Art/Interaction/Selection/SelectionVolume.mat";

        public const string StrokeLineShaderName = "Universal Render Pipeline/Unlit";
        public const string StrokeLineMaterialPath = DNPDemoProjectSetup.Root + "/Art/Interaction/Stroke/StrokeLine.mat";

        static readonly Color PanelBackground = new(0.06f, 0.08f, 0.14f, 0.92f);

        // Same red as the pen's tip sphere (PenSelectAreaMaterial), a little lighter so the volume does not hide the statue.
        static readonly Color SelectionVolumeColor = new(1f, 0f, 0f, 0.3f);

        [MenuItem("DNP Demo/Create/Interaction Prefabs")]
        public static void CreateAllIfMissing()
        {
            CreateDummyTool();
            CreateSelectionTool();
            CreateStrokeTool();
        }

        /// <summary>
        /// Tool for <see cref="SelectionPart"/>, with the view that draws the selection. The pen and the stage live in
        /// DemoSystems, so <see cref="ScenarioInstaller"/> wires them when it places the tool there.
        /// </summary>
        public static GameObject CreateSelectionTool()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SelectionToolPath);
            if (existing != null)
            {
                return existing;
            }
            Material material = CreateSelectionVolumeMaterial();
            var root = new GameObject("SelectionTool");
            try
            {
                var view = root.AddComponent<SelectionVolumeView>();
                Assign(view, "_material", material);
                var tool = root.AddComponent<SelectionTool>();
                Assign(tool, "_view", view);
                Directory.CreateDirectory(Path.GetDirectoryName(SelectionToolPath));
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SelectionToolPath);
                Debug.Log($"[DNPDemo] Created {SelectionToolPath}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Material of <see cref="SelectionVolumeView"/>: the selection shader with GPU instancing on.</summary>
        public static Material CreateSelectionVolumeMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(SelectionVolumeMaterialPath);
            if (existing != null)
            {
                return existing;
            }
            Shader shader = Shader.Find(SelectionVolumeShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader {SelectionVolumeShaderName} was not found.");
            }
            var material = new Material(shader) { enableInstancing = true };
            material.SetColor("_BaseColor", SelectionVolumeColor);
            Directory.CreateDirectory(Path.GetDirectoryName(SelectionVolumeMaterialPath));
            AssetDatabase.CreateAsset(material, SelectionVolumeMaterialPath);
            Debug.Log($"[DNPDemo] Created {SelectionVolumeMaterialPath}");
            return material;
        }

        /// <summary>
        /// Tool for <see cref="StrokePart"/>, with the view that draws the lines. The pen and the stage live in
        /// DemoSystems, so <see cref="ScenarioInstaller"/> wires them when it places the tool there.
        /// </summary>
        public static GameObject CreateStrokeTool()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(StrokeToolPath);
            if (existing != null)
            {
                return existing;
            }
            Material material = CreateStrokeLineMaterial();
            var root = new GameObject("StrokeTool");
            try
            {
                var view = root.AddComponent<StrokeView>();
                Assign(view, "_material", material);
                var tool = root.AddComponent<StrokeTool>();
                Assign(tool, "_view", view);
                Directory.CreateDirectory(Path.GetDirectoryName(StrokeToolPath));
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, StrokeToolPath);
                Debug.Log($"[DNPDemo] Created {StrokeToolPath}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Material of <see cref="StrokeView"/>: opaque unlit white, the colour of the pen's lines.</summary>
        public static Material CreateStrokeLineMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(StrokeLineMaterialPath);
            if (existing != null)
            {
                return existing;
            }
            Shader shader = Shader.Find(StrokeLineShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader {StrokeLineShaderName} was not found.");
            }
            var material = new Material(shader);
            material.SetColor("_BaseColor", Color.white);
            Directory.CreateDirectory(Path.GetDirectoryName(StrokeLineMaterialPath));
            AssetDatabase.CreateAsset(material, StrokeLineMaterialPath);
            Debug.Log($"[DNPDemo] Created {StrokeLineMaterialPath}");
            return material;
        }

        /// <summary>
        /// Panel of the pen test scene that switches <see cref="StrokeTestHarness"/> between the guide and the sketch
        /// and shows the last verdict. Its harness is a scene object, so it is wired where the panel is placed.
        /// </summary>
        [MenuItem("DNP Demo/Create/Stroke Test Panel")]
        public static GameObject CreateStrokeTestPanel() =>
            CreatePrefabIfMissing(StrokeTestPanelPath, "StrokeTestPanel", root =>
            {
                SetupWorldCanvas(root, new Vector2(600, 340), interactive: true);
                root.AddComponent<Image>().color = PanelBackground;

                Transform content = CreateVerticalGroup("Content", root.transform, new Vector2(30, 24), new Vector2(-30, -24), 12);
                CreateText("Title", content, "線画のテスト", 30, TextAlignmentOptions.TopLeft, 40);
                TMP_Text goal = CreateText("Goal", content, string.Empty, 26, TextAlignmentOptions.TopLeft, 80);
                goal.textWrappingMode = TextWrappingModes.Normal;
                TMP_Text verdict = CreateText("Verdict", content, string.Empty, 22, TextAlignmentOptions.TopLeft);
                verdict.textWrappingMode = TextWrappingModes.Normal;
                verdict.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
                Button switchButton = CreateButton("SwitchButton", content, string.Empty, 70, 28);

                var panel = root.AddComponent<StrokeTestPanel>();
                Assign(panel, "_goalText", goal);
                Assign(panel, "_verdictText", verdict);
                Assign(panel, "_switchButton", switchButton);
                Assign(panel, "_switchLabel", switchButton.GetComponentInChildren<TMP_Text>());

                SetLayerRecursively(root.transform, LayerMask.NameToLayer(UILayerName));
            });

        /// <summary>
        /// Tool for <see cref="DummyInteractionPart"/>. The tool sits on the root; the panel is a child canvas that is
        /// switched off as a whole while no part runs, so its laser target disappears with it.
        /// </summary>
        public static GameObject CreateDummyTool() =>
            CreatePrefabIfMissing(DummyToolPath, "DummyTool", root =>
            {
                GameObject panel = CreateChild("Panel", root.transform);
                SetupWorldCanvas(panel, new Vector2(800, 420), interactive: true);
                panel.AddComponent<Image>().color = PanelBackground;

                Transform content = CreateVerticalGroup("Content", panel.transform, new Vector2(40, 36), new Vector2(-40, -36), 16);
                TMP_Text title = CreateText("Title", content, string.Empty, 30, TextAlignmentOptions.TopLeft, 110);
                title.textWrappingMode = TextWrappingModes.Normal;
                TMP_Text status = CreateText("Status", content, string.Empty, 34, TextAlignmentOptions.Center);
                status.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
                Button press = CreateButton("PressButton", content, "完了", 100, 40);

                AddHeadFollower(panel, distance: 1.1f, heightOffset: -0.25f, sharpness: 4f);

                var tool = root.AddComponent<DummyInteractionTool>();
                Assign(tool, "_content", panel);
                Assign(tool, "_titleText", title);
                Assign(tool, "_statusText", status);
                Assign(tool, "_pressButton", press);

                SetLayerRecursively(panel.transform, LayerMask.NameToLayer(UILayerName));
            });
    }
}
