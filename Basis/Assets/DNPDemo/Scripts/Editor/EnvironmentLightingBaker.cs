using System;
using System.IO;
using DNPDemo.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Bakes each environment preset's panorama into its lighting: the cubemap used as its reflection, and the spherical
    /// harmonics used as its ambient light when the ambient source is Skybox. The panorama is first resampled into a small
    /// equirectangular layout in world orientation, with the preset's rotation and exposure applied. Unity's importer turns
    /// the layout into a cubemap with glossy (GGX) convolution, so rougher surfaces sample blurrier mips, and the same layout
    /// is projected onto the spherical harmonics.
    /// </summary>
    public static class EnvironmentLightingBaker
    {
        public const string ReflectionFolder = DNPDemoProjectSetup.Root + "/Art/Environment/Reflections";
        /// <summary>Edge length of a cubemap face. Glossy reflections need little detail, and Quest samples it on every lit pixel.</summary>
        public const int FaceSize = 128;
        /// <summary>The layout baked for the importer, twice the face size tall; the importer scales its faces down to <see cref="FaceSize"/>.</summary>
        public const int SourceWidth = FaceSize * 4;
        public const int SourceHeight = FaceSize * 2;
        const int MaxSupersampling = 8;

        [MenuItem("DNP Demo/Setup/Bake Environment Lighting")]
        public static void BakeFromMenu()
        {
            BakePresets();
            Debug.Log("[DNPDemo] Environment lighting is baked.");
        }

        public static void BakePresets()
        {
            foreach (string presetPath in new[] { EnvironmentPresetBuilder.ExhibitionPresetPath, EnvironmentPresetBuilder.TemplePresetPath })
            {
                EnvironmentPreset preset = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(presetPath);
                if (preset == null)
                {
                    throw new InvalidOperationException($"The environment preset is missing: {presetPath}");
                }
                Bake(preset);
            }
            AssetDatabase.SaveAssets();
        }

        public static string ReflectionPath(EnvironmentPreset preset) => $"{ReflectionFolder}/{preset.name}_Reflection.png";

        public static void Bake(EnvironmentPreset preset)
        {
            if (preset.Panorama == null)
            {
                throw new InvalidOperationException($"Environment preset '{preset.name}' has no panorama to bake its lighting from.");
            }
            Color[] layout = LoadWorldLayout(preset);
            preset.SetReflectionCubemap(WriteReflection(preset, layout));
            preset.SetSkyboxAmbientProbe(ProjectAmbient(layout));
            EditorUtility.SetDirty(preset);
        }

        /// <summary>
        /// The panorama in the baked layout, in linear light. It is read from its source file at full resolution, since the
        /// imported texture is compressed and not readable.
        /// </summary>
        static Color[] LoadWorldLayout(EnvironmentPreset preset)
        {
            string panoramaPath = AssetDatabase.GetAssetPath(preset.Panorama);
            var panorama = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: false);
            try
            {
                if (!panorama.LoadImage(File.ReadAllBytes(AbsolutePath(panoramaPath))))
                {
                    throw new InvalidOperationException($"The panorama could not be decoded: {panoramaPath}");
                }
                return Resample(panorama.GetPixels(), panorama.width, panorama.height, preset.Rotation, preset.Exposure);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panorama);
            }
        }

        static Cubemap WriteReflection(EnvironmentPreset preset, Color[] layout)
        {
            string assetPath = ReflectionPath(preset);
            string fullPath = AbsolutePath(assetPath);
            var output = new Texture2D(SourceWidth, SourceHeight, TextureFormat.RGB24, mipChain: false, linear: false);
            try
            {
                var pixels = new Color[layout.Length];
                for (int i = 0; i < layout.Length; i++)
                {
                    pixels[i] = layout[i].gamma;
                }
                output.SetPixels(pixels);
                output.Apply(updateMipmaps: false);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output);
            }
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureImport(assetPath);

            Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
            if (cubemap == null)
            {
                throw new InvalidOperationException($"The reflection could not be imported as a cubemap: {assetPath}");
            }
            return cubemap;
        }

        /// <summary>
        /// Projects the layout onto spherical harmonics, treating each texel as a directional light weighted by its solid
        /// angle. AddDirectionalLight has units of its own, so the sum is divided by that of a uniform white sky: a uniform
        /// sky of radiance L then gives an ambient of L, as Unity's Skybox ambient does. EnvironmentPresetTests compares the
        /// result with Unity's Skybox ambient.
        /// </summary>
        static SphericalHarmonicsL2 ProjectAmbient(Color[] layout)
        {
            var probe = new SphericalHarmonicsL2();
            var white = new SphericalHarmonicsL2();
            for (int y = 0; y < SourceHeight; y++)
            {
                float latitude = ((y + 0.5f) / SourceHeight - 0.5f) * Mathf.PI;
                float latitudeCos = Mathf.Cos(latitude);
                float solidAngle = latitudeCos * (Mathf.PI / SourceHeight) * (2f * Mathf.PI / SourceWidth);
                for (int x = 0; x < SourceWidth; x++)
                {
                    float longitude = ((x + 0.5f) / SourceWidth - 0.5f) * 2f * Mathf.PI;
                    var direction = new Vector3(Mathf.Sin(longitude) * latitudeCos, Mathf.Sin(latitude), Mathf.Cos(longitude) * latitudeCos);
                    probe.AddDirectionalLight(direction, layout[y * SourceWidth + x], solidAngle);
                    white.AddDirectionalLight(direction, Color.white, solidAngle);
                }
            }
            var response = new Color[1];
            white.Evaluate(new[] { Vector3.up }, response);
            float normalization = 1f / response[0].r;
            for (int channel = 0; channel < 3; channel++)
            {
                for (int coefficient = 0; coefficient < 9; coefficient++)
                {
                    probe[channel, coefficient] *= normalization;
                }
            }
            return probe;
        }

        /// <summary>
        /// Resamples the panorama into the importer's layout, averaging in linear light. The importer's latitude-longitude
        /// layout puts directions where the sky shader does at rotation 0 (+Z at the centre, see EnvironmentPanorama.cginc),
        /// so the output is the panorama shifted by the preset's rotation. EnvironmentPresetTests renders both to check this.
        /// </summary>
        static Color[] Resample(Color[] source, int sourceWidth, int sourceHeight, float rotation, float exposure)
        {
            var linear = new Color[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                linear[i] = source[i].linear;
            }
            float scale = Mathf.Pow(2f, exposure);
            int samples = Mathf.Clamp(Mathf.CeilToInt((float)sourceWidth / SourceWidth), 1, MaxSupersampling);
            float uOffset = rotation / 360f;
            var result = new Color[SourceWidth * SourceHeight];
            for (int y = 0; y < SourceHeight; y++)
            {
                for (int x = 0; x < SourceWidth; x++)
                {
                    Color sum = Color.clear;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        float v = (y + (sy + 0.5f) / samples) / SourceHeight;
                        for (int sx = 0; sx < samples; sx++)
                        {
                            float u = (x + (sx + 0.5f) / samples) / SourceWidth + uOffset;
                            sum += SampleBilinear(linear, sourceWidth, sourceHeight, u, v);
                        }
                    }
                    result[y * SourceWidth + x] = ClampToLdr(sum / (samples * samples) * scale);
                }
            }
            return result;
        }


        /// <summary>Wraps around in longitude and clamps at the poles, like the sky's sampler.</summary>
        static Color SampleBilinear(Color[] pixels, int width, int height, float u, float v)
        {
            float x = (u - Mathf.Floor(u)) * width - 0.5f;
            float y = Mathf.Clamp(v * height - 0.5f, 0f, height - 1f);
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float tx = x - x0;
            float ty = y - y0;
            int x1 = (x0 + 1) % width;
            x0 = (x0 % width + width) % width;
            int y1 = Mathf.Min(y0 + 1, height - 1);
            Color bottom = Color.Lerp(pixels[y0 * width + x0], pixels[y0 * width + x1], tx);
            Color top = Color.Lerp(pixels[y1 * width + x0], pixels[y1 * width + x1], tx);
            return Color.Lerp(bottom, top, ty);
        }

        /// <summary>The source is LDR, so a positive exposure saturates instead of adding range.</summary>
        static Color ClampToLdr(Color color) => new(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), 1f);

        static void ConfigureImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"The reflection source could not be imported: {assetPath}");
            }
            // The convolution and seam settings are only exposed through TextureImporterSettings.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Default;
            settings.textureShape = TextureImporterShape.TextureCube;
            settings.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            settings.seamlessCubemap = true;
            settings.sRGBTexture = true;
            settings.mipmapEnabled = true;
            settings.readable = false;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.filterMode = FilterMode.Trilinear;
            importer.SetTextureSettings(settings);
            // The importer makes the faces as large as the layout's height, then scales them down to the max size.
            importer.maxTextureSize = FaceSize;
            importer.SaveAndReimport();
        }

        static string AbsolutePath(string assetPath)
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectPath, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
