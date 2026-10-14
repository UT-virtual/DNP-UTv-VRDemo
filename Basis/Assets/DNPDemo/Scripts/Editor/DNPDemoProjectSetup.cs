using System.Collections.Generic;
using System.IO;
using Basis.Scripts.BasisSdk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DNPDemo.Editor
{
    /// <summary>
    /// One-shot setup for the DNP x UTv demo: player settings, build scene order and the main scene skeleton.
    /// Run from the menu, or headless: Unity.exe -batchmode -projectPath Basis -executeMethod DNPDemo.Editor.DNPDemoProjectSetup.RunAll -quit
    /// </summary>
    public static class DNPDemoProjectSetup
    {
        public const string Root = "Assets/DNPDemo";
        public const string MainScenePath = Root + "/Scenes/DNPDemo_Main.unity";
        const string MaterialsPath = Root + "/Art/Materials";
        const string InitScenePath = "Packages/com.basis.framework/Scenes/initialization.unity";

        public const string ProductName = "DNP UTv VR Demo";
        public const string CompanyName = "DNP UTv";
        public const string AndroidAppId = "com.dnputv.vrdemo";

        [MenuItem("DNP Demo/Setup/Run All")]
        public static void RunAll()
        {
            ApplyPlayerSettings();
            if (!File.Exists(MainScenePath))
            {
                CreateMainScene();
            }
            ApplyBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[DNPDemo] Setup complete.");
        }

        [MenuItem("DNP Demo/Setup/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, AndroidAppId);
        }

        [MenuItem("DNP Demo/Setup/Apply Build Scenes")]
        public static void ApplyBuildScenes()
        {
            // The demo scene boots directly as the world (it carries its own BasisScene).
            // initialization stays in the list but disabled so it can be switched back for debugging.
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainScenePath, true),
            };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == MainScenePath)
                {
                    continue;
                }
                scenes.Add(new EditorBuildSettingsScene(existing.path, existing.path != InitScenePath && existing.enabled));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("DNP Demo/Setup/Recreate Main Scene (overwrites)")]
        public static void RecreateMainSceneMenu()
        {
            if (EditorUtility.DisplayDialog("DNP Demo", $"{MainScenePath} を作り直します。よろしいですか？", "OK", "Cancel"))
            {
                CreateMainScene();
            }
        }

        /// <summary>
        /// Builds the placeholder layout described in docs/PLAN.md (exhibition room, statue, verification table, Phase 2 booths).
        /// Placeholder primitives get swapped for real assets as they arrive.
        /// </summary>
        public static void CreateMainScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MainScenePath));
            Directory.CreateDirectory(MaterialsPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material floorMat = GetOrCreateMaterial("M_Floor", new Color(0.12f, 0.12f, 0.13f));
            Material wallMat = GetOrCreateMaterial("M_Wall", new Color(0.2f, 0.2f, 0.22f));
            Material marbleMat = GetOrCreateMaterial("M_MarblePlaceholder", new Color(0.92f, 0.9f, 0.86f));
            Material pedestalMat = GetOrCreateMaterial("M_Pedestal", new Color(0.35f, 0.33f, 0.3f));
            Material scholarAMat = GetOrCreateMaterial("M_ScholarA", new Color(0.75f, 0.5f, 0.25f));
            Material scholarBMat = GetOrCreateMaterial("M_ScholarB", new Color(0.25f, 0.45f, 0.75f));

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.12f, 0.14f);

            // --- Basis world root ---
            GameObject basisRoot = new GameObject("BasisScene");
            BasisScene basisScene = basisRoot.AddComponent<BasisScene>();
            basisScene.RespawnHeight = -20;

            Transform spawn = NewEmpty("SpawnPoint", basisRoot.transform, new Vector3(0, 0, -2.5f)).transform;
            basisScene.SpawnPoint = spawn;

            GameObject camGo = new GameObject("SceneCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(basisRoot.transform, false);
            camGo.transform.position = new Vector3(0, 1.6f, -2.5f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            cam.nearClipPlane = 0.05f;
            basisScene.MainCamera = cam;

            // --- Environment (Phase 3 swaps this for the Milos temple) ---
            GameObject env = new GameObject("Env_ExhibitionRoom");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(env.transform, false);
            floor.transform.localScale = new Vector3(1.4f, 1, 1.4f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            const float half = 7f, height = 4.5f;
            NewBox("Wall_N", env.transform, new Vector3(0, height / 2, half), new Vector3(2 * half, height, 0.2f), wallMat);
            NewBox("Wall_S", env.transform, new Vector3(0, height / 2, -half), new Vector3(2 * half, height, 0.2f), wallMat);
            NewBox("Wall_E", env.transform, new Vector3(half, height / 2, 0), new Vector3(0.2f, height, 2 * half), wallMat);
            NewBox("Wall_W", env.transform, new Vector3(-half, height / 2, 0), new Vector3(0.2f, height, 2 * half), wallMat);

            GameObject lights = new GameObject("Lighting");
            Light dir = NewEmpty("DimDirectional", lights.transform, new Vector3(0, 4, 0)).AddComponent<Light>();
            dir.type = LightType.Directional;
            dir.intensity = 0.15f;
            dir.transform.rotation = Quaternion.Euler(50, -30, 0);
            Light spot = NewEmpty("Spot_Statue", lights.transform, new Vector3(0, 4.2f, -0.6f)).AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 40;
            spot.range = 8;
            spot.intensity = 30;
            spot.transform.rotation = Quaternion.Euler(80, 0, 0);

            // --- Statue (Scholar A's original) ---
            GameObject statueRoot = NewEmpty("Statue_Original", null, Vector3.zero);
            NewBox("Pedestal", statueRoot.transform, new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.5f, 0.9f), pedestalMat);
            Transform statueAnchor = NewEmpty("StatueAnchor", statueRoot.transform, new Vector3(0, 0.5f, 0)).transform;
            NewPlaceholderStatue("Venus_Armless (placeholder)", statueAnchor, marbleMat);

            // --- Verification table (Scholar B's working copy lands here) ---
            GameObject table = NewEmpty("VerificationTable", null, new Vector3(2.0f, 0, 0.2f));
            NewBox("Pedestal", table.transform, new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.5f, 0.9f), pedestalMat);
            NewEmpty("CopyAnchor", table.transform, new Vector3(0, 0.5f, 0));

            // --- Phase 2 history booths (hidden until Phase 2) ---
            GameObject booths = NewEmpty("Phase2_Booths", null, new Vector3(0, 0, 4.5f));
            string[] boothNames = { "Booth1_Ver1.0_Misread", "Booth2_Ver2.0_SketchAI", "Booth3_Ver3.0_Consensus" };
            for (int i = 0; i < boothNames.Length; i++)
            {
                GameObject booth = NewEmpty(boothNames[i], booths.transform, new Vector3((i - 1) * 2.6f, 0, 0));
                NewBox("Pedestal", booth.transform, new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.5f, 0.9f), pedestalMat);
                NewEmpty("ModelAnchor", booth.transform, new Vector3(0, 0.5f, 0));
                NewEmpty("LabelAnchor", booth.transform, new Vector3(0, 0.1f, -0.6f));
            }
            booths.SetActive(false);

            // --- Scholars (NPC placeholders; voice + subtitles driven by the scenario) ---
            GameObject scholars = new GameObject("Scholars");
            NewScholar("Scholar_A_Archaeologist", scholars.transform, new Vector3(-1.6f, 0, 0.6f), scholarAMat);
            NewScholar("Scholar_B_Engineer", scholars.transform, new Vector3(3.2f, 0, 0.8f), scholarBMat);

            // --- Player role markers (A = host, B = client) ---
            GameObject roles = new GameObject("PlayerRoleSpawns");
            NewEmpty("Spawn_AssistantA", roles.transform, new Vector3(-0.8f, 0, -2.2f));
            NewEmpty("Spawn_AssistantB", roles.transform, new Vector3(1.4f, 0, -1.6f));

            // --- Scenario runtime root (ScenarioDirector etc. go here) ---
            new GameObject("ScenarioRoot");

            EditorSceneManager.SaveScene(scene, MainScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[DNPDemo] Created {MainScenePath}");
        }

        static GameObject NewEmpty(string name, Transform parent, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            go.transform.localPosition = localPos;
            return go;
        }

        static GameObject NewBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void NewPlaceholderStatue(string name, Transform parent, Material mat)
        {
            GameObject statue = NewEmpty(name, parent, Vector3.zero);
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(statue.transform, false);
            body.transform.localPosition = new Vector3(0, 1.0f, 0);
            body.transform.localScale = new Vector3(0.55f, 1.0f, 0.4f);
            body.GetComponent<Renderer>().sharedMaterial = mat;
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(statue.transform, false);
            head.transform.localPosition = new Vector3(0, 2.15f, 0);
            head.transform.localScale = Vector3.one * 0.28f;
            head.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void NewScholar(string name, Transform parent, Vector3 pos, Material mat)
        {
            GameObject root = NewEmpty(name, parent, pos);
            root.transform.rotation = Quaternion.LookRotation(new Vector3(-pos.x, 0, -2.5f - pos.z));
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body (placeholder)";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.85f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);
            body.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            NewEmpty("VoiceSource", root.transform, new Vector3(0, 1.6f, 0)).AddComponent<AudioSource>().spatialBlend = 1f;
            NewEmpty("SubtitleAnchor", root.transform, new Vector3(0, 2.1f, 0));
        }

        static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                return mat;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
