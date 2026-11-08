using DNPDemo.Operator;
using DNPDemo.Presentation;
using DNPDemo.Roles;
using DNPDemo.Scenario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Places the scenario engine into DNPDemo_Main and wires every reference: director, player, stage bindings
    /// (scholar voices and subtitles, narrator, effects, banner, spotlight) and the operator panel.
    /// It runs once; afterwards the scene is the source of truth and is edited by hand.
    /// </summary>
    public static class ScenarioSceneInstaller
    {
        const string ScenarioObjectName = "Scenario";
        const string ScenarioRootPath = "ScenarioRoot";
        const string SpotlightPath = "Lighting/Spot_Statue";

        static readonly (ScenarioSpeaker speaker, string path)[] Scholars =
        {
            (ScenarioSpeaker.ScholarA, "Scholars/Scholar_A_Archaeologist"),
            (ScenarioSpeaker.ScholarB, "Scholars/Scholar_B_Engineer"),
        };

        [MenuItem("DNP Demo/Setup/Install Scenario Into Open Scene")]
        public static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != DNPDemoProjectSetup.MainScenePath)
            {
                Fail($"{DNPDemoProjectSetup.MainScenePath} を開いてから実行してください。");
                return;
            }
            Transform scenarioRoot = FindInScene(scene, ScenarioRootPath);
            if (scenarioRoot == null)
            {
                Fail($"シーンに {ScenarioRootPath} がありません。");
                return;
            }
            if (scenarioRoot.Find(ScenarioObjectName) != null)
            {
                Fail($"{ScenarioRootPath}/{ScenarioObjectName} は既にあります。作り直す場合は先に削除してください。");
                return;
            }
            var roles = scenarioRoot.GetComponentInChildren<DemoRoleService>(true);
            if (roles == null)
            {
                Fail("シーンに DemoRoleService がありません。");
                return;
            }

            ScenarioData data = ScenarioDataBuilder.CreateIfMissing();
            ScenarioPrefabBuilder.CreateAllIfMissing();

            var scenario = new GameObject(ScenarioObjectName);
            Undo.RegisterCreatedObjectUndo(scenario, "Install Scenario");
            scenario.transform.SetParent(scenarioRoot, false);

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

            var stageSo = new SerializedObject(stage);
            SerializedProperty speakers = stageSo.FindProperty("_speakers");
            speakers.arraySize = 0;
            AddSpeaker(speakers, ScenarioSpeaker.Narrator, narratorVoice, narratorSubtitle);
            foreach ((ScenarioSpeaker speaker, string path) in Scholars)
            {
                Transform scholar = FindInScene(scene, path);
                if (scholar == null)
                {
                    Debug.LogWarning($"[DNPDemo] {path} が見つからないため、{speaker} の割り当てを省きました。");
                    continue;
                }
                Transform anchor = scholar.Find("SubtitleAnchor") ?? scholar;
                AudioSource voice = scholar.Find("VoiceSource")?.GetComponent<AudioSource>();
                if (voice == null)
                {
                    Debug.LogWarning($"[DNPDemo] {path}/VoiceSource に AudioSource がありません。{speaker} の音声は再生されません。");
                }
                var subtitle = InstantiatePrefab<SubtitleView>(ScenarioPrefabBuilder.SpeakerSubtitlePath, anchor);
                AddSpeaker(speakers, speaker, voice, subtitle);
            }

            SerializedProperty objects = stageSo.FindProperty("_objects");
            objects.arraySize = 0;
            Transform spotlight = FindInScene(scene, SpotlightPath);
            if (spotlight != null)
            {
                AddObject(objects, ScenarioDataBuilder.SpotlightKey, spotlight.gameObject);
                // The lobby is the dimmed room; the opening switches the spotlight on.
                Undo.RecordObject(spotlight.gameObject, "Install Scenario");
                spotlight.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"[DNPDemo] {SpotlightPath} が見つからないため、キー「{ScenarioDataBuilder.SpotlightKey}」を登録しませんでした。");
            }
            stageSo.FindProperty("_effects").objectReferenceValue = effects;
            stageSo.FindProperty("_banner").objectReferenceValue = banner;
            stageSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = scenario;
            Debug.Log($"[DNPDemo] Installed {ScenarioRootPath}/{ScenarioObjectName}. Save the scene to keep it.");
        }

        static void AddSpeaker(SerializedProperty list, ScenarioSpeaker speaker, AudioSource voice, SubtitleView subtitle)
        {
            int index = list.arraySize;
            list.arraySize++;
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Speaker").intValue = (int)speaker;
            element.FindPropertyRelative("Voice").objectReferenceValue = voice;
            element.FindPropertyRelative("Subtitle").objectReferenceValue = subtitle;
        }

        static void AddObject(SerializedProperty list, string key, GameObject target)
        {
            int index = list.arraySize;
            list.arraySize++;
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Key").stringValue = key;
            element.FindPropertyRelative("Target").objectReferenceValue = target;
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
            Undo.RegisterCreatedObjectUndo(instance, "Install Scenario");
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance.GetComponent<T>();
        }

        /// <summary>Finds a transform by "Root/Child/..." path, including inactive objects.</summary>
        static Transform FindInScene(Scene scene, string path)
        {
            int slash = path.IndexOf('/');
            string rootName = slash < 0 ? path : path.Substring(0, slash);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != rootName)
                {
                    continue;
                }
                return slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
            }
            return null;
        }

        static void Fail(string message)
        {
            EditorUtility.DisplayDialog("DNP Demo", message, "OK");
        }
    }
}
