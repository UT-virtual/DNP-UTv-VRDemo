using DNPDemo.Interaction;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Interaction.Selection;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Operator;
using DNPDemo.Pen;
using DNPDemo.Presentation;
using DNPDemo.Roles;
using DNPDemo.Scenario;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Builds the scenario engine into the DemoSystems prefab and wires every reference inside it: director, stage,
    /// player, operator input and panel, narrator voice and subtitle, effects and banner, and the interaction runner
    /// with its tools. Only what is missing is added, so running it again after an update installs the new parts and
    /// leaves hand edits alone. Afterwards the prefab is the source of truth and is edited by hand.
    /// Scholars and keyed objects are not wired here; they declare themselves with
    /// <see cref="ScenarioSpeakerBinding"/> / <see cref="ScenarioObjectBinding"/> in their own prefabs.
    /// </summary>
    public static class ScenarioInstaller
    {
        public const string DemoSystemsPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/DemoSystems.prefab";
        public const string ScenarioObjectName = "Scenario";
        public const string InteractionObjectName = "Interaction";

        [MenuItem("DNP Demo/Setup/Install Scenario Into DemoSystems Prefab")]
        public static void Install()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DemoSystemsPrefabPath) == null)
            {
                Fail($"{DemoSystemsPrefabPath} がありません。");
                return;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(DemoSystemsPrefabPath);
            try
            {
                var roles = root.GetComponentInChildren<DemoRoleService>(true);
                if (roles == null)
                {
                    Fail($"{DemoSystemsPrefabPath} に DemoRoleService がありません。");
                    return;
                }
                bool changed = false;
                Transform scenario = root.transform.Find(ScenarioObjectName);
                if (scenario == null)
                {
                    scenario = BuildScenario(root.transform, roles);
                    changed = true;
                }
                Transform interaction = scenario.Find(InteractionObjectName);
                if (interaction == null)
                {
                    interaction = BuildInteraction(scenario, roles);
                    changed = true;
                }
                changed |= InstallMissingTools(root.transform, scenario, interaction);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, DemoSystemsPrefabPath);
                    Debug.Log($"[DNPDemo] Installed the scenario into {DemoSystemsPrefabPath}.");
                }
                else
                {
                    Debug.Log($"[DNPDemo] {DemoSystemsPrefabPath} already has everything installed.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Transform BuildScenario(Transform parent, DemoRoleService roles)
        {
            ScenarioData data = ScenarioDataBuilder.CreateIfMissing();
            ScenarioPrefabBuilder.CreateAllIfMissing();

            var scenario = new GameObject(ScenarioObjectName);
            scenario.transform.SetParent(parent, false);

            var director = scenario.AddComponent<ScenarioDirector>();
            var stage = scenario.AddComponent<ScenarioStage>();
            var player = scenario.AddComponent<ScenarioPlayer>();
            var input = scenario.AddComponent<OperatorInput>();

            AudioSource effects = CreateAudioSource("Effects", scenario.transform);
            AudioSource narratorVoice = CreateAudioSource("NarratorVoice", scenario.transform);
            var narratorSubtitle = InstantiatePrefab<SubtitleView>(ScenarioPrefabBuilder.NarratorSubtitlePath, scenario.transform);
            var banner = InstantiatePrefab<BannerView>(ScenarioPrefabBuilder.BannerPath, scenario.transform);
            var operatorPanel = InstantiatePrefab<OperatorPanel>(ScenarioPrefabBuilder.OperatorPanelPath, scenario.transform);
            operatorPanel.gameObject.SetActive(false);

            WorldUiBuilder.Assign(director, "_scenario", data);
            WorldUiBuilder.Assign(director, "_roles", roles);
            WorldUiBuilder.Assign(player, "_director", director);
            WorldUiBuilder.Assign(player, "_stage", stage);
            WorldUiBuilder.Assign(input, "_panel", operatorPanel);
            WorldUiBuilder.Assign(input, "_director", director);
            WorldUiBuilder.Assign(operatorPanel, "_director", director);
            WorldUiBuilder.Assign(stage, "_effects", effects);
            WorldUiBuilder.Assign(stage, "_banner", banner);

            var narrator = narratorVoice.gameObject.AddComponent<ScenarioSpeakerBinding>();
            SetSpeakerBinding(narrator, ScenarioSpeaker.Narrator, narratorVoice, narratorSubtitle);
            return scenario.transform;
        }

        /// <summary>Adds the interaction runner. Its tools are added by <see cref="InstallMissingTools"/>.</summary>
        static Transform BuildInteraction(Transform scenario, DemoRoleService roles)
        {
            var interaction = new GameObject(InteractionObjectName);
            interaction.transform.SetParent(scenario, false);
            var runner = interaction.AddComponent<InteractionRunner>();
            WorldUiBuilder.Assign(runner, "_director", scenario.GetComponent<ScenarioDirector>());
            WorldUiBuilder.Assign(runner, "_roles", roles);
            return interaction.transform;
        }

        /// <summary>
        /// Places every tool that is not under <paramref name="interaction"/> yet and registers it with the runner.
        /// Returns true when anything was added.
        /// </summary>
        static bool InstallMissingTools(Transform root, Transform scenario, Transform interaction)
        {
            InteractionPrefabBuilder.CreateAllIfMissing();
            var runner = interaction.GetComponent<InteractionRunner>();
            bool changed = false;

            if (interaction.GetComponentInChildren<SelectionTool>(true) == null)
            {
                InstallPenTool<SelectionTool>(InteractionPrefabBuilder.SelectionToolPath, root, scenario, interaction, runner);
                changed = true;
            }
            if (interaction.GetComponentInChildren<StrokeTool>(true) == null)
            {
                InstallPenTool<StrokeTool>(InteractionPrefabBuilder.StrokeToolPath, root, scenario, interaction, runner);
                changed = true;
            }
            if (interaction.GetComponentInChildren<ManipulateTool>(true) == null)
            {
                InstallPenTool<ManipulateTool>(InteractionPrefabBuilder.ManipulateToolPath, root, scenario, interaction, runner);
                changed = true;
            }
            if (interaction.GetComponentInChildren<DummyInteractionTool>(true) == null)
            {
                var dummy = InstantiatePrefab<DummyInteractionTool>(InteractionPrefabBuilder.DummyToolPath, interaction);
                RegisterTool(runner, dummy);
                changed = true;
            }
            return changed;
        }

        /// <summary>Places a tool that reads the pen and resolves zone keys, and wires its <c>_pen</c> and <c>_stage</c>.</summary>
        static void InstallPenTool<T>(string prefabPath, Transform root, Transform scenario, Transform interaction, InteractionRunner runner)
            where T : InteractionTool
        {
            var tool = InstantiatePrefab<T>(prefabPath, interaction);
            WorldUiBuilder.Assign(tool, "_stage", scenario.GetComponent<ScenarioStage>());
            var pen = root.GetComponentInChildren<SpatialPenController>(true);
            if (pen == null)
            {
                Debug.LogWarning($"[DNPDemo] {DemoSystemsPrefabPath} に SpatialPenController がないため、{typeof(T).Name} のペンを手動で設定してください。");
            }
            WorldUiBuilder.Assign(tool, "_pen", pen);
            RegisterTool(runner, tool);
        }

        static void RegisterTool(InteractionRunner runner, InteractionTool tool)
        {
            var so = new SerializedObject(runner);
            SerializedProperty tools = so.FindProperty("_tools");
            int index = tools.arraySize;
            tools.InsertArrayElementAtIndex(index);
            tools.GetArrayElementAtIndex(index).objectReferenceValue = tool;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fills a <see cref="ScenarioSpeakerBinding"/>. Also used when building the scholar prefabs.</summary>
        public static void SetSpeakerBinding(ScenarioSpeakerBinding binding, ScenarioSpeaker speaker, AudioSource voice, SubtitleView subtitle)
        {
            var so = new SerializedObject(binding);
            so.FindProperty("_speaker").intValue = (int)speaker;
            so.FindProperty("_voice").objectReferenceValue = voice;
            so.FindProperty("_subtitle").objectReferenceValue = subtitle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static AudioSource CreateAudioSource(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        static T InstantiatePrefab<T>(string prefabPath, Transform parent) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance.GetComponent<T>();
        }

        static void Fail(string message)
        {
            EditorUtility.DisplayDialog("DNP Demo", message, "OK");
        }
    }
}
