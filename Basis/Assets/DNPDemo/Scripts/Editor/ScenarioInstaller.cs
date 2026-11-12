using DNPDemo.Interaction;
using DNPDemo.Operator;
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
                if (scenario.Find(InteractionObjectName) == null)
                {
                    BuildInteraction(scenario, roles);
                    changed = true;
                }
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

        /// <summary>Adds the interaction runner and the tools it can hand parts to.</summary>
        static void BuildInteraction(Transform scenario, DemoRoleService roles)
        {
            InteractionPrefabBuilder.CreateAllIfMissing();

            var interaction = new GameObject(InteractionObjectName);
            interaction.transform.SetParent(scenario, false);
            var runner = interaction.AddComponent<InteractionRunner>();
            var dummy = InstantiatePrefab<DummyInteractionTool>(InteractionPrefabBuilder.DummyToolPath, interaction.transform);

            WorldUiBuilder.Assign(runner, "_director", scenario.GetComponent<ScenarioDirector>());
            WorldUiBuilder.Assign(runner, "_roles", roles);
            var so = new SerializedObject(runner);
            SerializedProperty tools = so.FindProperty("_tools");
            tools.arraySize = 1;
            tools.GetArrayElementAtIndex(0).objectReferenceValue = dummy;
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
