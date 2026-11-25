using System;
using System.IO;
using DNPDemo.Environment;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Bakes each environment preset's panorama into the cubemap used as its reflection. The panorama is resampled into a small
    /// equirectangular PNG with the preset's rotation and exposure applied, and Unity's importer turns it into a cubemap with
    /// glossy (GGX) convolution, so rougher surfaces sample blurrier mips.
    /// </summary>
    public static class EnvironmentReflectionBaker
    {
        public const string ReflectionFolder = DNPDemoProjectSetup.Root + "/Art/Environment/Reflections";
        /// <summary>Edge length of a cubemap face. Glossy reflections need little detail, and Quest samples it on every lit pixel.</summary>
        public const int FaceSize = 128;
        /// <summary>The layout baked for the importer, twice the face size tall; the importer scales its faces down to <see cref="FaceSize"/>.</summary>
        public const int SourceWidth = FaceSize * 4;
        public const int SourceHeight = FaceSize * 2;
        const int MaxSupersampling = 8;

        [MenuItem("DNP Demo/Setup/Bake Environment Reflections")]
        public static void BakeFromMenu()
        {
            BakePresets();
            Debug.Log("[DNPDemo] Environment reflections are baked.");
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
                throw new InvalidOperationException($"Environment preset '{preset.name}' has no panorama to bake a reflection from.");
            }
            string assetPath = ReflectionPath(preset);
            string fullPath = AbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, EncodeReflectionSource(preset));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureImport(assetPath);

            Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
            if (cubemap == null)
            {
                throw new InvalidOperationException($"The reflection could not be imported as a cubemap: {assetPath}");
            }
            preset.SetReflectionCubemap(cubemap);
            EditorUtility.SetDirty(preset);
        }

        /// <summary>
        /// The panorama read from its source file at full resolution, since the imported texture is compressed and not readable.
        /// </summary>
        static byte[] EncodeReflectionSource(EnvironmentPreset preset)
        {
            string panoramaPath = AssetDatabase.GetAssetPath(preset.Panorama);
            var panorama = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: false);
            var output = new Texture2D(SourceWidth, SourceHeight, TextureFormat.RGB24, mipChain: false, linear: false);
            try
            {
                if (!panorama.LoadImage(File.ReadAllBytes(AbsolutePath(panoramaPath))))
                {
                    throw new InvalidOperationException($"The panorama could not be decoded: {panoramaPath}");
                }
                output.SetPixels(Resample(panorama.GetPixels(), panorama.width, panorama.height, preset.Rotation, preset.Exposure));
                output.Apply(updateMipmaps: false);
                return output.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panorama);
                UnityEngine.Object.DestroyImmediate(output);
            }
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
                    Color average = sum / (samples * samples) * scale;
                    average.a = 1f;
                    result[y * SourceWidth + x] = ClampToLdr(average).gamma;
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
