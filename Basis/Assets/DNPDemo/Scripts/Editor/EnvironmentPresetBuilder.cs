using System;
using System.Collections.Generic;
using System.IO;
using DNPDemo.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Creates the environment presets and the exhibition's temporary panorama, configures the temple panorama's
    /// import, bakes the reflections, and wires the scene prefabs.
    /// </summary>
    public static class EnvironmentPresetBuilder
    {
        public const string DataFolder = DNPDemoProjectSetup.Root + "/Data/Environment";
        /// <summary>Display names of the presets (<see cref="EnvironmentPreset.DisplayName"/>).</summary>
        public const string ExhibitionDisplayName = "展示室";
        public const string TempleDisplayName = "神殿";

        public const string ExhibitionPresetPath = DataFolder + "/EnvironmentPreset_Exhibition.asset";
        public const string TemplePresetPath = DataFolder + "/EnvironmentPreset_Temple.asset";
        public const string ExhibitionPanoramaPath = DNPDemoProjectSetup.Root + "/Art/Environment/Environment_Exhibition_Panorama.png";
        public const string TemplePanoramaPath = DNPDemoProjectSetup.Root + "/Art/Environment/Environment_Temple_Panorama.jpg";
        public const string CrossfadeShaderPath = DNPDemoProjectSetup.Root + "/Scripts/Runtime/Environment/EnvironmentCrossfadePanorama.shader";
        public const string SnapshotShaderPath = DNPDemoProjectSetup.Root + "/Scripts/Runtime/Environment/EnvironmentPanoramaSnapshot.shader";

        const string LightingPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Lighting.prefab";
        public const string ExhibitionRoomPrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Env_ExhibitionRoom.prefab";
        public const string ExhibitionRoomFadeMaterialFolder = DNPDemoProjectSetup.Root + "/Art/ExhibitionRoom/Materials/Fade";
        const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        public const string TemplePrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Env_Temple.prefab";
        public const string TempleFloorMaterialPath = DNPDemoProjectSetup.Root + "/Art/Environment/M_TempleFloor.mat";
        public const string TempleFloorName = "TempleFloor";

        // A marble disc under the statue. The panorama's floor is infinitely far away, so without it the pedestal floats and
        // nothing receives the statue's shadow. Its top sits 1 mm below the exhibition floor (y = 0), which is still fading
        // out while the floor appears, so the two never fight for depth.
        const float TempleFloorRadius = 10f;
        const float TempleFloorThickness = 0.01f;
        const float TempleFloorTop = -0.001f;
        static readonly Color TempleFloorColor = new(0.88f, 0.86f, 0.82f);
        const float TempleFloorSmoothness = 0.3f;
        const int PanoramaWidth = 512;
        const int PanoramaHeight = 256;
        const int TemplePanoramaMaxSize = 4096;

        [MenuItem("DNP Demo/Setup/Build Environment Presets")]
        public static void BuildAssetsAndPrefabs()
        {
            CreatePresetsIfMissing();
            EnvironmentReflectionBaker.BakePresets();
            ConfigureLightingPrefab();
            ConfigureExhibitionRoomPrefab();
            ConfigureTemplePrefab();
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
                ExhibitionDisplayName,
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
                // The cubemap is baked from the panorama afterwards (EnvironmentReflectionBaker).
                reflection: new ReflectionSettings { Intensity = 0.6f },
                shadowQuality: new ShadowQualitySettings { Override = false });

            EnsurePreset(
                TemplePresetPath,
                "EnvironmentPreset_Temple",
                TempleDisplayName,
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
                    ShadowStrength = 0f,
                    ShadowBias = 0.05f,
                    ShadowNormalBias = 0.4f,
                    ShadowNearPlane = 0.2f,
                },
                ambient: new AmbientSettings
                {
                    Mode = AmbientMode.Trilight,
                    // The hues of the panorama's linear mean over the zenith (45 to 90 degrees), the horizon (within 15
                    // degrees) and the floor (below -45 degrees); the sunlit marble floor makes the ground the brightest.
                    // The mean is scaled by 0.6 in linear light and the resulting colour halved, which leaves the side away
                    // from the sun dark enough to read as shade.
                    SkyColor = new Color(0.24f, 0.265f, 0.31f),
                    EquatorColor = new Color(0.27f, 0.265f, 0.27f),
                    GroundColor = new Color(0.305f, 0.3f, 0.3f),
                    FlatColor = new Color(0.27f, 0.265f, 0.27f),
                    Intensity = 1f,
                },
                reflection: new ReflectionSettings { Intensity = 0.5f },
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
            string displayName,
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
                    displayName,
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

                ScenarioBindingEditor.Bind(root, EnvironmentController.EnvironmentKey);

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

        /// <summary>
        /// Binds the room as <see cref="EnvironmentController.ExhibitionRoomKey"/> and lists every renderer in its
        /// <see cref="EnvironmentFadeGroup"/> with a transparent counterpart of each material. The whole room fades, as the
        /// temple brings its own floor.
        /// </summary>
        static void ConfigureExhibitionRoomPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ExhibitionRoomPrefabPath);
            try
            {
                ScenarioBindingEditor.Bind(root, EnvironmentController.ExhibitionRoomKey);
                EnvironmentFadeGroup group = root.GetComponent<EnvironmentFadeGroup>();
                if (group == null)
                {
                    group = root.AddComponent<EnvironmentFadeGroup>();
                }

                Directory.CreateDirectory(AbsolutePath(ExhibitionRoomFadeMaterialFolder));
                var fadeMaterials = new Dictionary<Material, Material>();
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                var entries = new EnvironmentFadeGroup.Entry[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    Material[] originals = renderers[i].sharedMaterials;
                    var fades = new Material[originals.Length];
                    for (int slot = 0; slot < originals.Length; slot++)
                    {
                        fades[slot] = EnsureFadeMaterial(originals[slot], fadeMaterials);
                    }
                    entries[i] = new EnvironmentFadeGroup.Entry { Renderer = renderers[i], FadeMaterials = fades };
                }
                group.SetEntries(entries);
                EditorUtility.SetDirty(group);
                PrefabUtility.SaveAsPrefabAsset(root, ExhibitionRoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// A material embedded in a model is prefixed with the model's name, as it may share its name with an external material.
        /// </summary>
        public static string FadeMaterialPath(Material original)
        {
            string name = AssetDatabase.IsSubAsset(original)
                ? $"{Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(original))}_{original.name}"
                : original.name;
            return $"{ExhibitionRoomFadeMaterialFolder}/{name}_Fade.mat";
        }

        /// <summary>
        /// Creates or refreshes the transparent asset for a room material. Being an asset referenced by the prefab, its shader
        /// variant is part of the build, which a material made at runtime would not guarantee.
        /// </summary>
        static Material EnsureFadeMaterial(Material original, Dictionary<Material, Material> created)
        {
            if (original == null)
            {
                return null;
            }
            if (created.TryGetValue(original, out Material existing))
            {
                return existing;
            }
            if (original.shader == null || original.shader.name != UrpLitShaderName)
            {
                throw new InvalidOperationException(
                    $"The exhibition room material '{original.name}' uses '{original.shader?.name}', but fading needs {UrpLitShaderName}.");
            }
            string path = FadeMaterialPath(original);
            foreach (KeyValuePair<Material, Material> pair in created)
            {
                if (AssetDatabase.GetAssetPath(pair.Value) == path)
                {
                    throw new InvalidOperationException($"Two exhibition room materials are named '{original.name}'. Rename one of them.");
                }
            }

            Material fade = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (fade == null)
            {
                fade = new Material(original) { name = Path.GetFileNameWithoutExtension(path) };
                ConfigureFadeMaterial(fade);
                AssetDatabase.CreateAsset(fade, path);
            }
            else
            {
                fade.shader = original.shader;
                fade.CopyPropertiesFromMaterial(original);
                ConfigureFadeMaterial(fade);
                EditorUtility.SetDirty(fade);
            }
            created.Add(original, fade);
            return fade;
        }

        /// <summary>
        /// Makes a URP Lit material alpha blended, as URP's own material inspector would. Depth writing stays off here because
        /// URP's validation turns it off for transparent Lit materials anyway; <see cref="EnvironmentFadeGroup"/> turns it on
        /// in its runtime copies.
        /// </summary>
        public static void ConfigureFadeMaterial(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
        }

        /// <summary>
        /// Puts the marble floor in Env_Temple and binds the root as <see cref="ScenarioDataBuilder.TempleFloorKey"/>. The root
        /// stays hidden in the scene; the closing step shows it.
        /// </summary>
        static void ConfigureTemplePrefab()
        {
            Material floorMaterial = EnsureTempleFloorMaterial();
            GameObject root = PrefabUtility.LoadPrefabContents(TemplePrefabPath);
            try
            {
                ScenarioBindingEditor.Bind(root, ScenarioDataBuilder.TempleFloorKey);

                Transform floor = root.transform.Find(TempleFloorName);
                if (floor == null)
                {
                    GameObject created = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    created.name = TempleFloorName;
                    // Walking and teleporting use the exhibition room's colliders, which stay in place.
                    UnityEngine.Object.DestroyImmediate(created.GetComponent<Collider>());
                    created.transform.SetParent(root.transform, false);
                    floor = created.transform;
                }
                // The cylinder primitive is 1 m wide and 2 m tall.
                floor.localPosition = new Vector3(0f, TempleFloorTop - TempleFloorThickness * 0.5f, 0f);
                floor.localRotation = Quaternion.identity;
                floor.localScale = new Vector3(TempleFloorRadius * 2f, TempleFloorThickness * 0.5f, TempleFloorRadius * 2f);
                var renderer = floor.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = floorMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                PrefabUtility.SaveAsPrefabAsset(root, TemplePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Material EnsureTempleFloorMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TempleFloorMaterialPath);
            if (material != null)
            {
                return material;
            }
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                throw new InvalidOperationException("The URP Lit shader was not found.");
            }
            material = new Material(lit) { name = Path.GetFileNameWithoutExtension(TempleFloorMaterialPath) };
            material.SetColor("_BaseColor", TempleFloorColor);
            material.SetFloat("_Smoothness", TempleFloorSmoothness);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, TempleFloorMaterialPath);
            return material;
        }
    }
}
