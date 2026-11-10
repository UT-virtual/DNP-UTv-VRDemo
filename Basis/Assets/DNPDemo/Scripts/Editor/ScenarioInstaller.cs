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
    /// player, operator input and panel, narrator voice and subtitle, effects and banner.
    /// Scholars and keyed objects are not wired here; they declare themselves with
    /// <see cref="ScenarioSpeakerBinding"/> / <see cref="ScenarioObjectBinding"/> in their own prefabs.
    /// It runs once; afterwards the prefab is the source of truth and is edited by hand.
    /// </summary>
    public static class ScenarioInstaller
    {
        public const string DemoSystemsPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/DemoSystems.prefab";
        public const string ScenarioObjectName = "Scenario";

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
                if (root.transform.Find(ScenarioObjectName) != null)
                {
                    Fail($"{DemoSystemsPrefabPath} には {ScenarioObjectName} が既にあります。作り直す場合は先に削除してください。");
                    return;
                }
                var roles = root.GetComponentInChildren<DemoRoleService>(true);
                if (roles == null)
                {
                    Fail($"{DemoSystemsPrefabPath} に DemoRoleService がありません。");
                    return;
                }
                Build(root.transform, roles);
                PrefabUtility.SaveAsPrefabAsset(root, DemoSystemsPrefabPath);
                Debug.Log($"[DNPDemo] Installed {ScenarioObjectName} into {DemoSystemsPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void Build(Transform parent, DemoRoleService roles)
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
