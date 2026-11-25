using System;
using System.IO;
using DNPDemo.Environment;
using DNPDemo.Scenario;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Editor
{
    /// <summary>Creates the temporary environment panoramas and presets, and wires their scene prefabs.</summary>
    public static class EnvironmentPresetBuilder
    {
        public const string DataFolder = DNPDemoProjectSetup.Root + "/Data/Environment";
        public const string ExhibitionPresetPath = DataFolder + "/EnvironmentPreset_Exhibition.asset";
        public const string TemplePresetPath = DataFolder + "/EnvironmentPreset_Temple.asset";
        public const string ExhibitionPanoramaPath = DNPDemoProjectSetup.Root + "/Art/Environment/Environment_Exhibition_Panorama.png";
        public const string TemplePanoramaPath = DNPDemoProjectSetup.Root + "/Art/Environment/Environment_Temple_Panorama.png";
        public const string CrossfadeShaderPath = DNPDemoProjectSetup.Root + "/Scripts/Runtime/Environment/EnvironmentCrossfadePanorama.shader";

        const string LightingPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Lighting.prefab";
        const string ExhibitionRoomPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Env_ExhibitionRoom.prefab";
        const int PanoramaWidth = 512;
        const int PanoramaHeight = 256;

        [MenuItem("DNP Demo/Setup/Build Environment Presets")]
        public static void BuildAssetsAndPrefabs()
        {
            CreatePresetsIfMissing();
            ConfigureLightingPrefab();
            ConfigureExhibitionRoomPrefab();
            ScenarioDataBuilder.EnsureEnvironmentCueInExistingAsset();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DNPDemo] Environment presets and prefab bindings are ready.");
        }

        public static void CreatePresetsIfMissing()
        {
            Directory.CreateDirectory(AbsolutePath(DataFolder));
            EnsurePanorama(ExhibitionPanoramaPath, temple: false);
            EnsurePanorama(TemplePanoramaPath, temple: true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Texture2D exhibitionPanorama = AssetDatabase.LoadAssetAtPath<Texture2D>(ExhibitionPanoramaPath);
            Texture2D templePanorama = AssetDatabase.LoadAssetAtPath<Texture2D>(TemplePanoramaPath);
            if (exhibitionPanorama == null || templePanorama == null)
            {
                throw new InvalidOperationException("Environment panorama textures could not be imported.");
            }

            EnsurePreset(
                ExhibitionPresetPath,
                "EnvironmentPreset_Exhibition",
                exhibitionPanorama,
                exposure: 0f,
                rotation: 0f,
                exhibitionRoomVisible: true,
                directional: new DirectionalLightSettings
                {
                    Color = new Color(1f, 0.94f, 0.84f),
                    Intensity = 0.15f,
                    Rotation = new Vector3(50f, -30f, 0f),
                    Shadows = LightShadows.None,
                    ShadowResolution = LightShadowResolution.Medium,
                    ShadowStrength = 0f,
                    ShadowBias = 0.05f,
                    ShadowNormalBias = 0.4f,
                    ShadowNearPlane = 0.2f,
                },
                spot: new SpotLightSettings
                {
                    Enabled = SpotLightMode.Keep,
                    Color = Color.white,
                    Intensity = 30f,
                    Rotation = new Vector3(80f, 0f, 0f),
                    Range = 8f,
                    SpotAngle = 40f,
                    InnerSpotAngle = 22f,
                    Shadows = LightShadows.None,
                    ShadowResolution = LightShadowResolution.Medium,
                    ShadowStrength = 0f,
                    ShadowBias = 0.05f,
                    ShadowNormalBias = 0.4f,
                    ShadowNearPlane = 0.2f,
                },
                ambient: new AmbientSettings
                {
                    Mode = AmbientMode.Trilight,
                    SkyColor = new Color(0.35f, 0.34f, 0.32f),
                    EquatorColor = new Color(0.2f, 0.19f, 0.17f),
                    GroundColor = new Color(0.08f, 0.075f, 0.07f),
                    FlatColor = new Color(0.2f, 0.19f, 0.17f),
                    Intensity = 1f,
                },
                reflection: new ReflectionSettings
                {
                    Mode = DefaultReflectionMode.Skybox,
                    CustomReflectionTexture = null,
                    Intensity = 0.6f,
                });

            EnsurePreset(
                TemplePresetPath,
                "EnvironmentPreset_Temple",
                templePanorama,
                exposure: 0.2f,
                rotation: 0f,
                exhibitionRoomVisible: false,
                directional: new DirectionalLightSettings
                {
                    Color = new Color(1f, 0.91f, 0.76f),
                    Intensity = 1.4f,
                    Rotation = new Vector3(38f, -42f, 0f),
                    Shadows = LightShadows.Soft,
                    ShadowResolution = LightShadowResolution.High,
                    ShadowStrength = 0.85f,
                    ShadowBias = 0.035f,
                    ShadowNormalBias = 0.3f,
                    ShadowNearPlane = 0.2f,
                },
                spot: new SpotLightSettings
                {
                    Enabled = SpotLightMode.Off,
                    Color = Color.white,
                    Intensity = 0f,
                    Rotation = new Vector3(40f, 0f, 0f),
                    Range = 8f,
                    SpotAngle = 40f,
                    InnerSpotAngle = 22f,
                    Shadows = LightShadows.None,
                    ShadowResolution = LightShadowResolution.Medium,
                    ShadowStrength = 0f,
                    ShadowBias = 0.05f,
                    ShadowNormalBias = 0.4f,
                    ShadowNearPlane = 0.2f,
                },
                ambient: new AmbientSettings
                {
                    Mode = AmbientMode.Trilight,
                    SkyColor = new Color(0.48f, 0.65f, 0.83f),
                    EquatorColor = new Color(0.3f, 0.43f, 0.57f),
                    GroundColor = new Color(0.13f, 0.16f, 0.2f),
                    FlatColor = new Color(0.3f, 0.43f, 0.57f),
                    Intensity = 1f,
                },
                reflection: new ReflectionSettings
                {
                    Mode = DefaultReflectionMode.Skybox,
                    CustomReflectionTexture = null,
                    Intensity = 0.9f,
                });
            AssetDatabase.SaveAssets();
        }

        static void EnsurePanorama(string assetPath, bool temple)
        {
            string fullPath = AbsolutePath(assetPath);
            if (File.Exists(fullPath))
            {
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            var texture = new Texture2D(PanoramaWidth, PanoramaHeight, TextureFormat.RGB24, mipChain: false, linear: false)
            {
                name = Path.GetFileNameWithoutExtension(assetPath),
            };
            Color[] pixels = new Color[PanoramaWidth * PanoramaHeight];
            for (int y = 0; y < PanoramaHeight; y++)
            {
                float v = y / (PanoramaHeight - 1f);
                for (int x = 0; x < PanoramaWidth; x++)
                {
                    float u = x / (PanoramaWidth - 1f);
                    pixels[y * PanoramaWidth + x] = temple
                        ? TempleSkyColor(u, v)
                        : ExhibitionSkyColor(v);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        static string AbsolutePath(string assetPath)
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectPath, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        static Color ExhibitionSkyColor(float v)
        {
            float t = Mathf.SmoothStep(0f, 1f, v);
            return Color.Lerp(new Color(0.19f, 0.17f, 0.15f), new Color(0.36f, 0.33f, 0.29f), t);
        }

        static Color TempleSkyColor(float u, float v)
        {
            float t = Mathf.SmoothStep(0f, 1f, v);
            Color sky = Color.Lerp(new Color(0.38f, 0.69f, 0.87f), new Color(0.055f, 0.26f, 0.58f), t);
            float deltaU = Mathf.Min(Mathf.Abs(u - 0.71f), 1f - Mathf.Abs(u - 0.71f));
            float distance = Mathf.Sqrt(deltaU * deltaU + (v - 0.62f) * (v - 0.62f));
            float sun = 1f - Mathf.SmoothStep(0.018f, 0.045f, distance);
            float glow = Mathf.Exp(-distance * 24f) * 0.35f;
            return Color.Lerp(sky, new Color(1f, 0.84f, 0.56f), Mathf.Clamp01(sun + glow));
        }

        static void EnsurePreset(
            string assetPath,
            string assetName,
            Texture2D panorama,
            float exposure,
            float rotation,
            bool exhibitionRoomVisible,
            DirectionalLightSettings directional,
            SpotLightSettings spot,
            AmbientSettings ambient,
            ReflectionSettings reflection)
        {
            EnvironmentPreset preset = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(assetPath);
            bool created = preset == null;
            if (created)
            {
                preset = ScriptableObject.CreateInstance<EnvironmentPreset>();
                preset.name = assetName;
                AssetDatabase.CreateAsset(preset, assetPath);
            }
            if (created || preset.Panorama == null)
            {
                preset.Configure(
                    panorama,
                    exposure,
                    rotation,
                    exhibitionRoomVisible,
                    directional,
                    spot,
                    ambient,
                    reflection);
                EditorUtility.SetDirty(preset);
            }
        }

        static void ConfigureLightingPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(LightingPrefabPath);
            try
            {
                Light directional = null;
                Light spot = null;
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type == LightType.Directional)
                    {
                        directional = light;
                    }
                    else if (light.type == LightType.Spot)
                    {
                        spot = light;
                    }
                }
                if (directional == null || spot == null)
                {
                    throw new InvalidOperationException("Lighting prefab must contain a directional light and a spot light.");
                }

                ScenarioObjectBinding binding = root.GetComponent<ScenarioObjectBinding>();
                if (binding == null)
                {
                    binding = root.AddComponent<ScenarioObjectBinding>();
                }
                SetBindingKey(binding, EnvironmentController.EnvironmentKey);

                EnvironmentController controller = root.GetComponent<EnvironmentController>();
                if (controller == null)
                {
                    controller = root.AddComponent<EnvironmentController>();
                }
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(CrossfadeShaderPath);
                if (shader == null)
                {
                    throw new InvalidOperationException($"Crossfade shader is missing: {CrossfadeShaderPath}");
                }
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_baselinePreset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(ExhibitionPresetPath);
                serialized.FindProperty("_directionalLight").objectReferenceValue = directional;
                serialized.FindProperty("_spotLight").objectReferenceValue = spot;
                serialized.FindProperty("_crossfadeShader").objectReferenceValue = shader;
                serialized.FindProperty("_transitionDuration").floatValue = 2f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, LightingPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ConfigureExhibitionRoomPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ExhibitionRoomPrefabPath);
            try
            {
                ScenarioObjectBinding binding = root.GetComponent<ScenarioObjectBinding>();
                if (binding == null)
                {
                    binding = root.AddComponent<ScenarioObjectBinding>();
                }
                SetBindingKey(binding, EnvironmentController.ExhibitionRoomKey);
                PrefabUtility.SaveAsPrefabAsset(root, ExhibitionRoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SetBindingKey(ScenarioObjectBinding binding, string key)
        {
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("_key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
