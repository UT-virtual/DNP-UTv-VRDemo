using System;
using System.IO;
using DNPDemo.Environment;
using DNPDemo.Scenario;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Creates the environment presets and the exhibition's temporary panorama, configures the temple panorama's
    /// import, and wires the scene prefabs.
    /// </summary>
    public static class EnvironmentPresetBuilder
    {
        public const string DataFolder = DNPDemoProjectSetup.Root + "/Data/Environment";
        public const string ExhibitionPresetPath = DataFolder + "/EnvironmentPreset_Exhibition.asset";
        public const string TemplePresetPath = DataFolder + "/EnvironmentPreset_Temple.asset";
        public const string ExhibitionPanoramaPath = DNPDemoProjectSetup.Root + "/Art/Environment/Environment_Exhibition_Panorama.png";
        public const string TemplePanoramaPath = DNPDemoProjectSetup.Root + "/Art/Environment/Environment_Temple_Panorama.jpg";
        public const string CrossfadeShaderPath = DNPDemoProjectSetup.Root + "/Scripts/Runtime/Environment/EnvironmentCrossfadePanorama.shader";
        public const string SnapshotShaderPath = DNPDemoProjectSetup.Root + "/Scripts/Runtime/Environment/EnvironmentPanoramaSnapshot.shader";

        const string LightingPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Lighting.prefab";
        const string ExhibitionRoomPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Env_ExhibitionRoom.prefab";
        const int PanoramaWidth = 512;
        const int PanoramaHeight = 256;
        const int TemplePanoramaMaxSize = 4096;

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
            EnsureExhibitionPanorama();
            if (!File.Exists(AbsolutePath(TemplePanoramaPath)))
            {
                throw new FileNotFoundException(
                    $"The temple panorama is missing. Place a 2:1 equirectangular JPEG at {TemplePanoramaPath}.", TemplePanoramaPath);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTemplePanoramaImport();

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
                },
                shadowQuality: new ShadowQualitySettings { Override = false });

            EnsurePreset(
                TemplePresetPath,
                "EnvironmentPreset_Temple",
                templePanorama,
                // The JPEG is LDR and the marble in it is already near white, so it is shown as is.
                exposure: 0f,
                rotation: 0f,
                exhibitionRoomVisible: false,
                // The sun is hidden behind the cornice in the panorama. Its glow peaks at u = 0.70 and keeps brightening down
                // to the cornice at 50 degrees, which matches the column shadows on the floor. The skybox puts u at the
                // azimuth 360 * (u - 0.5) - rotation, measured from +Z towards +X, so the sun sits at 72 degrees and the
                // light, which points away from it, has a yaw of 72 - 180.
                directional: new DirectionalLightSettings
                {
                    Color = new Color(1f, 0.95f, 0.87f),
                    Intensity = 1f,
                    Rotation = new Vector3(50f, -108f, 0f),
                    // Android's URP asset has no soft shadows; Hard keeps the Editor identical to Quest.
                    Shadows = LightShadows.Hard,
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
                    // The panorama's linear mean over the zenith (45 to 90 degrees), the horizon (within 15 degrees) and
                    // the floor (below -45 degrees), scaled by 0.6 so the side away from the sun keeps its contrast. The
                    // sunlit marble floor makes the ground the brightest.
                    SkyColor = new Color(0.48f, 0.53f, 0.62f),
                    EquatorColor = new Color(0.54f, 0.53f, 0.54f),
                    GroundColor = new Color(0.61f, 0.6f, 0.6f),
                    FlatColor = new Color(0.54f, 0.53f, 0.54f),
                    Intensity = 1f,
                },
                reflection: new ReflectionSettings
                {
                    Mode = DefaultReflectionMode.Skybox,
                    CustomReflectionTexture = null,
                    Intensity = 0.5f,
                },
                // Basis' Very Low tier on Quest leaves a 256 px map over 5 m, too coarse for the statue's shadow.
                shadowQuality: new ShadowQualitySettings
                {
                    Override = true,
                    MainLightShadowmapResolution = 2048,
                    ShadowDistance = 8f,
                    CascadeCount = 1,
                });
            AssetDatabase.SaveAssets();
        }

        static void EnsureExhibitionPanorama()
        {
            string assetPath = ExhibitionPanoramaPath;
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
                    pixels[y * PanoramaWidth + x] = ExhibitionSkyColor(v);
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

        /// <summary>
        /// Imports the temple panorama as an equirectangular sky: it wraps around in longitude only, keeps its full
        /// resolution (it is not a power of two), and uses ASTC 6x6 on Quest.
        /// </summary>
        static void ConfigureTemplePanoramaImport()
        {
            var importer = AssetImporter.GetAtPath(TemplePanoramaPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"The temple panorama could not be imported: {TemplePanoramaPath}");
            }
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = TemplePanoramaMaxSize;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = TemplePanoramaMaxSize,
                format = TextureImporterFormat.ASTC_6x6,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = (int)TextureCompressionQuality.Normal,
            });
            importer.SaveAndReimport();
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
            ReflectionSettings reflection,
            ShadowQualitySettings shadowQuality)
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
                    reflection,
                    shadowQuality);
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
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_baselinePreset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(ExhibitionPresetPath);
                serialized.FindProperty("_directionalLight").objectReferenceValue = directional;
                serialized.FindProperty("_spotLight").objectReferenceValue = spot;
                serialized.FindProperty("_crossfadeShader").objectReferenceValue = LoadShader(CrossfadeShaderPath);
                serialized.FindProperty("_snapshotShader").objectReferenceValue = LoadShader(SnapshotShaderPath);
                serialized.FindProperty("_transitionDuration").floatValue = 2f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, LightingPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Shader LoadShader(string assetPath)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader is missing: {assetPath}");
            }
            return shader;
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
