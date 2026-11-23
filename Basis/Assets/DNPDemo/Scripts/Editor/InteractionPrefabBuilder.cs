using System;
using System.IO;
using DNPDemo.Interaction;
using DNPDemo.Interaction.History;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Interaction.Selection;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Interaction.Stroke.Testing;
using DNPDemo.Interaction.Voice;
using DNPDemo.Menu;
using DNPDemo.Venus;
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
        public const string ManipulateToolPath = Folder + "/ManipulateTool.prefab";
        public const string VoiceCommandToolPath = Folder + "/VoiceCommandTool.prefab";
        public const string HistoryToolPath = Folder + "/HistoryTool.prefab";

        /// <summary>Name of the child of <see cref="HistoryToolPath"/> that holds the <see cref="HistoryPanel"/>.</summary>
        public const string HistoryPanelObjectName = "HistoryPanel";
        public const string StrokeTestPanelPath = Folder + "/Testing/StrokeTestPanel.prefab";

        public const string SelectionVolumeShaderName = "DNPDemo/SelectionVolume";
        public const string SelectionVolumeMaterialPath = DNPDemoProjectSetup.Root + "/Art/Interaction/Selection/SelectionVolume.mat";

        public const string StrokeLineShaderName = "Universal Render Pipeline/Unlit";
        public const string StrokeLineMaterialPath = DNPDemoProjectSetup.Root + "/Art/Interaction/Stroke/StrokeLine.mat";

        public const string ManipulateGhostShaderName = "Universal Render Pipeline/Unlit";

        public const string HistoryPanelBackgroundShaderName = "UI/Default";
        public const string HistoryPanelBackgroundMaterialPath = DNPDemoProjectSetup.Root + "/Art/Interaction/History/HistoryPanelBackground.mat";
        public const string HistoryPanelDepthShaderName = "DNPDemo/HistoryPanelDepth";
        public const string HistoryPanelDepthMaterialPath = DNPDemoProjectSetup.Root + "/Art/Interaction/History/HistoryPanelDepth.mat";
        public const string ManipulateGhostMaterialPath = DNPDemoProjectSetup.Root + "/Art/Interaction/Manipulate/ManipulateGhost.mat";

        static readonly Color PanelBackground = new(0.06f, 0.08f, 0.14f, 0.92f);

        // Same red as the pen's tip sphere (PenSelectAreaMaterial), a little lighter so the volume does not hide the statue.
        static readonly Color SelectionVolumeColor = new(1f, 0f, 0f, 0.3f);

        // Pale cyan, apart from the red selection, so the goal reads as a different thing.
        static readonly Color ManipulateGhostColor = new(0.45f, 0.85f, 1f, 0.35f);

        [MenuItem("DNP Demo/Create/Interaction Prefabs")]
        public static void CreateAllIfMissing()
        {
            CreateDummyTool();
            CreateSelectionTool();
            CreateStrokeTool();
            CreateManipulateTool();
            CreateVoiceCommandTool();
            CreateHistoryTool();
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
        /// Tool for <see cref="ManipulatePart"/>, with the view that draws the ghost. The pen and the stage live in
        /// DemoSystems, so <see cref="ScenarioInstaller"/> wires them when it places the tool there.
        /// </summary>
        public static GameObject CreateManipulateTool()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ManipulateToolPath);
            if (existing != null)
            {
                return existing;
            }
            Material material = CreateManipulateGhostMaterial();
            var root = new GameObject("ManipulateTool");
            try
            {
                var ghost = root.AddComponent<ManipulateGhostView>();
                Assign(ghost, "_material", material);
                var tool = root.AddComponent<ManipulateTool>();
                Assign(tool, "_ghost", ghost);
                Directory.CreateDirectory(Path.GetDirectoryName(ManipulateToolPath));
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ManipulateToolPath);
                Debug.Log($"[DNPDemo] Created {ManipulateToolPath}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Tool for <see cref="VoicePart"/>. It shows its state on the task HUD, so it has no view of its own. The pen
        /// lives in DemoSystems, so <see cref="ScenarioInstaller"/> wires it when it places the tool there.
        /// </summary>
        public static GameObject CreateVoiceCommandTool()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(VoiceCommandToolPath);
            if (existing != null)
            {
                return existing;
            }
            var root = new GameObject("VoiceCommandTool");
            try
            {
                root.AddComponent<VoiceCommandTool>();
                Directory.CreateDirectory(Path.GetDirectoryName(VoiceCommandToolPath));
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, VoiceCommandToolPath);
                Debug.Log($"[DNPDemo] Created {VoiceCommandToolPath}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Material of <see cref="ManipulateGhostView"/>: translucent unlit, without depth writes so the object behind the
        /// ghost stays visible.
        /// </summary>
        public static Material CreateManipulateGhostMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(ManipulateGhostMaterialPath);
            if (existing != null)
            {
                return existing;
            }
            Shader shader = Shader.Find(ManipulateGhostShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader {ManipulateGhostShaderName} was not found.");
            }
            var material = new Material(shader);
            material.SetColor("_BaseColor", ManipulateGhostColor);
            // URP's transparent surface: the same properties the material inspector sets for Surface Type = Transparent.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            Directory.CreateDirectory(Path.GetDirectoryName(ManipulateGhostMaterialPath));
            AssetDatabase.CreateAsset(material, ManipulateGhostMaterialPath);
            Debug.Log($"[DNPDemo] Created {ManipulateGhostMaterialPath}");
            return material;
        }

        /// <summary>
        /// Material of the history panel's background: the default UI shader drawn in
        /// <see cref="HistoryRenderQueues.PanelBackground"/>, before the preview model.
        /// </summary>
        public static Material CreateHistoryPanelBackgroundMaterial() =>
            CreateMaterialIfMissing(HistoryPanelBackgroundMaterialPath, HistoryPanelBackgroundShaderName, HistoryRenderQueues.PanelBackground);

        /// <summary>
        /// Material writing the history panel's shape to depth only, in <see cref="HistoryRenderQueues.PanelDepth"/>,
        /// after the preview model.
        /// </summary>
        public static Material CreateHistoryPanelDepthMaterial() =>
            CreateMaterialIfMissing(HistoryPanelDepthMaterialPath, HistoryPanelDepthShaderName, HistoryRenderQueues.PanelDepth);

        static Material CreateMaterialIfMissing(string path, string shaderName, int renderQueue)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader {shaderName} was not found.");
            }
            var material = new Material(shader) { renderQueue = renderQueue };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(material, path);
            Debug.Log($"[DNPDemo] Created {path}");
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

        /// <summary>Size of the history panel in canvas pixels (millimetres). Wide enough for the branch tree of every version.</summary>
        public static readonly Vector2 HistoryPanelSize = new(960, 620);

        /// <summary>Size of a version card in the branch view, in canvas pixels.</summary>
        public static readonly Vector2 HistoryCardSize = new(128, 158);

        /// <summary>
        /// Tool for <see cref="HistoryPart"/> with the history panel as its child, styled like the Basis menus
        /// (<see cref="BasisUiStyle"/>). The panel is a plain transform placed in front of the view when it opens; under it,
        /// the canvas (switched off while closed) and the turning preview, which stays active so its statue builds the
        /// version models at scene load. The director and the stage live in DemoSystems, so <see cref="ScenarioInstaller"/>
        /// wires them when it places the tool there.
        /// <para>
        /// The canvas holds the close button and the empty message, shared by its two pages: the tree page (the title and
        /// the branch view with an inactive card template) and the detail page (「一覧へ」, the version name, ◀ ▶ and
        /// 「モデルを復元」).
        /// </para>
        /// <para>
        /// The preview stands behind the canvas. The background and a depth-only copy of its shape are drawn in the queues
        /// of <see cref="HistoryRenderQueues"/>, around the preview, so the background colour never covers the model while
        /// the labels and buttons, in front of it, draw over it as ordinary UI.
        /// </para>
        /// </summary>
        /// <param name="path">Where to save the prefab; another path builds a fresh copy to compare with or replace it.</param>
        public static GameObject CreateHistoryTool(string path = HistoryToolPath) =>
            CreatePrefabIfMissing(path, "HistoryTool", root =>
            {
                var panelObject = new GameObject(HistoryPanelObjectName);
                panelObject.transform.SetParent(root.transform, false);

                GameObject canvas = CreateChild("Canvas", panelObject.transform);
                SetupWorldCanvas(canvas, HistoryPanelSize, interactive: true);
                Transform ui = canvas.transform;
                const float panelRadius = 28f;
                BasisUiStyle.CreatePanelImage("Background", ui, BasisUiStyle.PanelColor, panelRadius, CreateHistoryPanelBackgroundMaterial());
                // The depth-only pass is a plain quad renderer rather than a canvas graphic: Prefab Mode draws canvases
                // through a path that ignores its colour mask. Its square corners stick out of the rounded background by
                // a few millimetres, where they only hide transparent objects behind the panel. Under the canvas, it is
                // shown and hidden with the panel; 2 mm behind it, so the labels and buttons do not fight it for depth.
                GameObject depth = CreateChild("Depth", ui);
                depth.transform.localPosition = new Vector3(0f, 0f, 2f);
                depth.transform.localScale = new Vector3(HistoryPanelSize.x, HistoryPanelSize.y, 1f);
                depth.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var depthRenderer = depth.AddComponent<MeshRenderer>();
                depthRenderer.sharedMaterial = CreateHistoryPanelDepthMaterial();
                depthRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                depthRenderer.receiveShadows = false;

                Button close = BasisUiStyle.CreateButton("CloseButton", ui, BasisUiStyle.SecondaryButtonColor, cornerRadius: 32);
                Anchor((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-88, -88), new Vector2(-24, -24));
                BasisUiStyle.AddIcon(close, BasisUiStyle.CloseIconPath, inset: 18);
                TMP_Text empty = CreateText("Empty", ui, HistoryPanel.EmptyMessage, 30, TextAlignmentOptions.Center);
                Anchor(empty.rectTransform, Vector2.zero, Vector2.one, new Vector2(120, 170), new Vector2(-120, -190));

                GameObject treePage = CreatePage("TreePage", ui);
                TMP_Text title = CreateText("Title", treePage.transform, HistoryMenuProvider.MenuTitle, 34, TextAlignmentOptions.MidlineLeft);
                Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, -84), new Vector2(-120, -24));
                HistoryTreeView treeView = CreateHistoryTreeView(treePage.transform);

                GameObject detailPage = CreatePage("DetailPage", ui);
                Transform detail = detailPage.transform;
                Button back = BasisUiStyle.CreateButton("BackButton", detail, BasisUiStyle.SecondaryButtonColor, cornerRadius: 32);
                Anchor((RectTransform)back.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -88), new Vector2(184, -24));
                BasisUiStyle.AddLabel(back, HistoryPanel.BackLabel, 28);

                TMP_Text version = CreateText("VersionName", detail, string.Empty, 40, TextAlignmentOptions.Center);
                Anchor(version.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(200, -150), new Vector2(-200, -90));
                TMP_Text page = CreateText("Page", detail, string.Empty, 24, TextAlignmentOptions.Center);
                Anchor(page.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(200, -190), new Vector2(-200, -150));

                // Foldout Arrow points down; turned a quarter to point left or right.
                Button previous = BasisUiStyle.CreateButton("PreviousButton", detail, BasisUiStyle.SecondaryButtonColor, cornerRadius: 40);
                Anchor((RectTransform)previous.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, -55), new Vector2(104, 25));
                BasisUiStyle.AddIcon(previous, BasisUiStyle.ArrowIconPath, inset: 16, rotation: -90f);
                Button next = BasisUiStyle.CreateButton("NextButton", detail, BasisUiStyle.SecondaryButtonColor, cornerRadius: 40);
                Anchor((RectTransform)next.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-104, -55), new Vector2(-24, 25));
                BasisUiStyle.AddIcon(next, BasisUiStyle.ArrowIconPath, inset: 16, rotation: 90f);

                Button restore = BasisUiStyle.CreateButton("RestoreButton", detail, BasisUiStyle.PrimaryButtonColor, cornerRadius: 40);
                Anchor((RectTransform)restore.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-170, 70), new Vector2(170, 150));
                BasisUiStyle.AddLabel(restore, HistoryPanel.RestoreLabel, 32);
                TMP_Text status = CreateText("Status", detail, string.Empty, 22, TextAlignmentOptions.Center);
                Anchor(status.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 20), new Vector2(-30, 64));
                detailPage.SetActive(false);

                SetLayerRecursively(canvas.transform, LayerMask.NameToLayer(UILayerName));

                // Between the page number and the restore button, behind the canvas (the viewer is on its -Z side). Its
                // centre sits far enough back that the turning model, arms included, never reaches in front of the canvas.
                var previewObject = new GameObject("Preview");
                previewObject.transform.SetParent(panelObject.transform, false);
                previewObject.transform.localPosition = new Vector3(0f, -0.02f, 0.09f);
                var modelObject = new GameObject("Model");
                modelObject.transform.SetParent(previewObject.transform, false);
                var statue = modelObject.AddComponent<VenusStatue>();
                Assign(statue, "_catalog", AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(VenusAssetBuilder.CatalogPath));
                var preview = previewObject.AddComponent<HistoryPreview>();
                Assign(preview, "_statue", statue);

                var panel = panelObject.AddComponent<HistoryPanel>();
                Assign(panel, "_content", canvas);
                Assign(panel, "_preview", preview);
                Assign(panel, "_treePage", treePage);
                Assign(panel, "_treeView", treeView);
                Assign(panel, "_detailPage", detailPage);
                Assign(panel, "_backButton", back);
                Assign(panel, "_versionText", version);
                Assign(panel, "_pageText", page);
                Assign(panel, "_emptyText", empty);
                Assign(panel, "_statusText", status);
                Assign(panel, "_closeButton", close);
                Assign(panel, "_previousButton", previous);
                Assign(panel, "_nextButton", next);
                Assign(panel, "_restoreButton", restore);

                var tool = root.AddComponent<HistoryTool>();
                Assign(tool, "_panel", panel);
            }, rectTransformRoot: false);

        /// <summary>A child filling <paramref name="parent"/>, shown and hidden as one page of the history panel.</summary>
        static GameObject CreatePage(string name, Transform parent)
        {
            GameObject page = CreateChild(name, parent);
            Anchor((RectTransform)page.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return page;
        }

        /// <summary>
        /// The branch view below the title: the content (lines, then cards, so the lines draw behind) and the inactive card
        /// template. A card is a frame shown on the current version, behind a button holding the thumbnail and the name.
        /// </summary>
        static HistoryTreeView CreateHistoryTreeView(Transform parent)
        {
            GameObject area = CreateChild("Tree", parent);
            Anchor((RectTransform)area.transform, Vector2.zero, Vector2.one, new Vector2(32, 32), new Vector2(-32, -104));
            var content = (RectTransform)CreateChild("Content", area.transform).transform;
            var lines = (RectTransform)CreateChild("Lines", content).transform;
            Anchor(lines, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var cards = (RectTransform)CreateChild("Cards", content).transform;
            Anchor(cards, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject template = CreateChild("CardTemplate", area.transform);
            var templateRect = (RectTransform)template.transform;
            templateRect.anchorMin = templateRect.anchorMax = templateRect.pivot = new Vector2(0f, 1f);
            templateRect.sizeDelta = HistoryCardSize;
            Image frame = BasisUiStyle.CreatePanelImage("CurrentFrame", template.transform, BasisUiStyle.PrimaryButtonColor, 22f);
            Anchor(frame.rectTransform, Vector2.zero, Vector2.one, new Vector2(-6, -6), new Vector2(6, 6));
            frame.raycastTarget = false;
            Button button = BasisUiStyle.CreateButton("Button", template.transform, BasisUiStyle.SecondaryButtonColor, cornerRadius: 18);
            Anchor((RectTransform)button.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject thumbnailObject = CreateChild("Thumbnail", button.transform);
            var thumbnail = thumbnailObject.AddComponent<RawImage>();
            thumbnail.raycastTarget = false;
            float side = HistoryCardSize.x - 16f;
            Anchor(thumbnail.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -8 - side), new Vector2(-8, -8));
            TMP_Text label = CreateText("Label", button.transform, string.Empty, 22, TextAlignmentOptions.Center);
            Anchor(label.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(4, 4), new Vector2(-4, 38));
            label.raycastTarget = false;
            var card = template.AddComponent<HistoryTreeCard>();
            Assign(card, "_button", button);
            Assign(card, "_thumbnail", thumbnail);
            Assign(card, "_label", label);
            Assign(card, "_currentFrame", frame.gameObject);
            frame.gameObject.SetActive(false);
            template.SetActive(false);

            var view = area.AddComponent<HistoryTreeView>();
            Assign(view, "_content", content);
            Assign(view, "_lines", lines);
            Assign(view, "_cards", cards);
            Assign(view, "_cardTemplate", card);
            return view;
        }

        /// <summary>
        /// Tool for <see cref="DummyInteractionPart"/>. The tool sits on the root; the panel is a child canvas that is
        /// switched off as a whole while no part runs, so its laser target disappears with it.
        /// </summary>
        public static GameObject CreateDummyTool() =>
            CreatePrefabIfMissing(DummyToolPath, "DummyTool", root =>
            {
                GameObject panel = CreateChild("Panel", root.transform);
                SetupWorldCanvas(panel, new Vector2(800, 300), interactive: true);
                panel.AddComponent<Image>().color = PanelBackground;

                Transform content = CreateVerticalGroup("Content", panel.transform, new Vector2(40, 36), new Vector2(-40, -36), 16);
                TMP_Text status = CreateText("Status", content, string.Empty, 34, TextAlignmentOptions.Center);
                status.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
                Button press = CreateButton("PressButton", content, "完了", 100, 40);

                AddHeadFollower(panel, distance: 1.1f, heightOffset: -0.25f, sharpness: 4f);

                var tool = root.AddComponent<DummyInteractionTool>();
                Assign(tool, "_content", panel);
                Assign(tool, "_statusText", status);
                Assign(tool, "_pressButton", press);

                SetLayerRecursively(panel.transform, LayerMask.NameToLayer(UILayerName));
            });
    }
}
