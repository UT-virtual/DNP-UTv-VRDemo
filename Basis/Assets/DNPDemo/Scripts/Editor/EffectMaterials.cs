using System;
using System.IO;
using DNPDemo.Venus;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// The effect materials and their default look: the selection volume, the pen lines, the manipulation ghost and the
    /// statue's version change (dissolve and scan line). The builders create each material from here when it is missing;
    /// the menu "DNP Demo/Setup/Reset Effect Materials" puts the existing ones back on their shader with the defaults
    /// (values tuned by hand are overwritten, so run it only to start over).
    /// </summary>
    public static class EffectMaterials
    {
        const string InteractionArt = DNPDemoProjectSetup.Root + "/Art/Interaction";
        const string VenusMaterials = DNPDemoProjectSetup.Root + "/Art/Venus/Materials";

        public const string NoiseTexturePath = VenusMaterials + "/T_DissolveNoise.png";
        const int NoiseSize = 128;

        public sealed class Definition
        {
            public Definition(string materialPath, string shaderPath, bool instancing, Action<Material> applyDefaults)
            {
                MaterialPath = materialPath;
                ShaderPath = shaderPath;
                Instancing = instancing;
                ApplyDefaults = applyDefaults;
            }

            public string MaterialPath { get; }
            public string ShaderPath { get; }
            public bool Instancing { get; }
            public Action<Material> ApplyDefaults { get; }
        }

        /// <summary>Translucent teal that pulses every 2 seconds, with a faint rim (SelectionVolumeView).</summary>
        public static readonly Definition Selection = new(
            InteractionPrefabBuilder.SelectionVolumeMaterialPath,
            InteractionArt + "/Selection/SelectionVolume.shader",
            instancing: true,
            material =>
            {
                material.SetColor("_BaseColor", new Color(0.1f, 0.85f, 0.75f, 0.35f));
                material.SetColor("_RimColor", new Color(0.6f, 1f, 0.95f, 1f));
                material.SetFloat("_RimPower", 3f);
                material.SetFloat("_RimIntensity", 0.35f);
                material.SetFloat("_PulseSpeed", 0.5f);
                material.SetFloat("_PulseAmount", 0.2f);
            });

        /// <summary>Cyan (#00E5FF) line whose drawing end glows slightly (StrokeView).</summary>
        public static readonly Definition Stroke = new(
            InteractionPrefabBuilder.StrokeLineMaterialPath,
            InteractionArt + "/Stroke/StrokeLine.shader",
            instancing: false,
            material =>
            {
                material.SetColor("_BaseColor", new Color(0f, 0.898f, 1f, 1f));
                material.SetColor("_TipColor", Color.white);
                material.SetFloat("_TipLength", 0.06f);
                material.SetFloat("_TipIntensity", 0.8f);
            });

        /// <summary>Translucent light blue with a fresnel rim (ManipulateGhostView).</summary>
        public static readonly Definition Ghost = new(
            InteractionPrefabBuilder.ManipulateGhostMaterialPath,
            InteractionArt + "/Manipulate/ManipulateGhost.shader",
            instancing: false,
            material =>
            {
                material.SetColor("_BaseColor", new Color(0.45f, 0.85f, 1f, 0.3f));
                material.SetColor("_RimColor", new Color(0.75f, 0.95f, 1f, 1f));
                material.SetFloat("_RimPower", 2.5f);
                material.SetFloat("_RimIntensity", 1f);
            });

        /// <summary>
        /// The version change (ModelTransition): noise dissolve with an orange (#FF8A00) edge and a white scan line running
        /// down. The base map and colour come from each model's own material at run time.
        /// </summary>
        public static readonly Definition Transition = new(
            VenusAssetBuilder.TransitionMaterialPath,
            VenusAssetBuilder.TransitionShaderPath,
            instancing: false,
            material =>
            {
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Cutoff", 0f);
                material.SetTexture("_NoiseMap", EnsureNoiseTexture());
                material.SetFloat("_NoiseScale", 6f);
                material.SetFloat("_EdgeWidth", 0.06f);
                material.SetColor("_EdgeColor", new Color(1f, 0.541f, 0f, 1f));
                material.SetFloat("_EdgeIntensity", 2f);
                material.SetColor("_ScanColor", Color.white);
                material.SetFloat("_Speed", 1.5f);
                material.SetFloat("_Width", 0.015f);
                material.SetFloat("_Intensity", 0.8f);
                material.SetFloat("_ScanFade", 0f);
            });

        static readonly Definition[] All = { Selection, Stroke, Ghost, Transition };

        [MenuItem("DNP Demo/Setup/Reset Effect Materials")]
        public static void ResetAllFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Reset Effect Materials",
                    "選択範囲・ストローク・ゴースト・版の切り替えのマテリアルを、既定のシェーダーと値に戻します。手で調整した値は上書きされます。", "戻す", "やめる"))
            {
                return;
            }
            foreach (Definition definition in All)
            {
                Reset(definition);
            }
            UseTransitionPropertyNames();
            AssetDatabase.SaveAssets();
            Debug.Log("[DNPDemo] Reset the effect materials to their defaults.");
        }

        /// <summary>The material of <paramref name="definition"/>, created with the defaults when missing.</summary>
        public static Material CreateIfMissing(Definition definition)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(definition.MaterialPath);
            if (existing != null)
            {
                return existing;
            }
            var material = new Material(LoadShader(definition)) { name = Path.GetFileNameWithoutExtension(definition.MaterialPath) };
            Configure(material, definition);
            Directory.CreateDirectory(Path.GetDirectoryName(definition.MaterialPath));
            AssetDatabase.CreateAsset(material, definition.MaterialPath);
            Debug.Log($"[DNPDemo] Created {definition.MaterialPath}");
            return material;
        }

        /// <summary>Puts the existing material back on its shader with the defaults, or creates it.</summary>
        public static Material Reset(Definition definition)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(definition.MaterialPath);
            if (material == null)
            {
                return CreateIfMissing(definition);
            }
            Undo.RecordObject(material, "Reset Effect Material");
            material.shader = LoadShader(definition);
            Configure(material, definition);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Configure(Material material, Definition definition)
        {
            // The queue and the blending come from the shader; drop what an earlier shader (URP Unlit) left behind.
            material.renderQueue = -1;
            material.shaderKeywords = Array.Empty<string>();
            material.enableInstancing = definition.Instancing;
            definition.ApplyDefaults(material);
        }

        static Shader LoadShader(Definition definition)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(definition.ShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException($"{definition.ShaderPath} was not found.");
            }
            return shader;
        }

        /// <summary>
        /// The profile made before the dissolve became a cutoff names the old properties; point it at the ones
        /// ModelTransition drives now. Names changed by hand to anything else are kept.
        /// </summary>
        static void UseTransitionPropertyNames()
        {
            var profile = AssetDatabase.LoadAssetAtPath<ModelTransitionProfile>(VenusAssetBuilder.TransitionProfilePath);
            if (profile == null)
            {
                return;
            }
            var so = new SerializedObject(profile);
            SerializedProperty dissolve = so.FindProperty("_dissolvePropertyName");
            SerializedProperty scanLine = so.FindProperty("_scanLinePropertyName");
            if (dissolve.stringValue == "_Dissolve")
            {
                dissolve.stringValue = ModelTransitionProfile.DefaultDissolvePropertyName;
            }
            if (scanLine.stringValue == "_ScanLine")
            {
                scanLine.stringValue = ModelTransitionProfile.DefaultScanLinePropertyName;
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// The tiling noise of the dissolve: value noise of four octaves on a wrapping grid, saved as a linear greyscale PNG.
        /// Kept when it exists.
        /// </summary>
        public static Texture2D EnsureNoiseTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTexturePath);
            if (existing != null)
            {
                return existing;
            }
            var texture = new Texture2D(NoiseSize, NoiseSize, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[NoiseSize * NoiseSize];
            for (int y = 0; y < NoiseSize; y++)
            {
                for (int x = 0; x < NoiseSize; x++)
                {
                    float value = 0f;
                    float amplitude = 0.5f;
                    float total = 0f;
                    for (int octave = 0, cells = 4; octave < 4; octave++, cells *= 2, amplitude *= 0.5f)
                    {
                        value += amplitude * TilingValueNoise(x * cells / (float)NoiseSize, y * cells / (float)NoiseSize, cells, octave);
                        total += amplitude;
                    }
                    byte level = (byte)Mathf.RoundToInt(Mathf.Clamp01(value / total) * 255f);
                    pixels[y * NoiseSize + x] = new Color32(level, level, level, 255);
                }
            }
            texture.SetPixels32(pixels);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            Directory.CreateDirectory(Path.GetDirectoryName(NoiseTexturePath));
            File.WriteAllBytes(NoiseTexturePath, png);
            AssetDatabase.ImportAsset(NoiseTexturePath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(NoiseTexturePath) is TextureImporter importer)
            {
                importer.sRGBTexture = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.SaveAndReimport();
            }
            Debug.Log($"[DNPDemo] Created {NoiseTexturePath}");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTexturePath);
        }

        /// <summary>Smoothly interpolated random values on a <paramref name="cells"/>² grid that wraps, so the texture tiles.</summary>
        static float TilingValueNoise(float u, float v, int cells, int seed)
        {
            int x0 = Mathf.FloorToInt(u);
            int y0 = Mathf.FloorToInt(v);
            float fx = u - x0;
            float fy = v - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Lattice(x0, y0, cells, seed);
            float b = Lattice(x0 + 1, y0, cells, seed);
            float c = Lattice(x0, y0 + 1, cells, seed);
            float d = Lattice(x0 + 1, y0 + 1, cells, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Lattice(int x, int y, int cells, int seed)
        {
            x = ((x % cells) + cells) % cells;
            y = ((y % cells) + cells) % cells;
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
