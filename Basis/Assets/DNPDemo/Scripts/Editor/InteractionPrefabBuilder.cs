using System;
using System.IO;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Selection;
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

        public const string SelectionVolumeShaderName = "DNPDemo/SelectionVolume";
        public const string SelectionVolumeMaterialPath = DNPDemoProjectSetup.Root + "/Art/Interaction/Selection/SelectionVolume.mat";

        static readonly Color PanelBackground = new(0.06f, 0.08f, 0.14f, 0.92f);

        // Same red as the pen's tip sphere (PenSelectAreaMaterial), a little lighter so the volume does not hide the statue.
        static readonly Color SelectionVolumeColor = new(1f, 0f, 0f, 0.3f);

        [MenuItem("DNP Demo/Create/Interaction Prefabs")]
        public static void CreateAllIfMissing()
        {
            CreateDummyTool();
            CreateSelectionTool();
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
