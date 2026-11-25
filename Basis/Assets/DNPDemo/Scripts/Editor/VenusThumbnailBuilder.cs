using System.Collections.Generic;
using System.IO;
using DNPDemo.Interaction.History;
using DNPDemo.Venus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Renders every version of the catalog to a square PNG with a transparent background and sets it as the version's
    /// <see cref="VenusVersion.Thumbnail"/>, for the branch view of the history panel. Run it again after replacing a model;
    /// it overwrites the images.
    /// <para>
    /// Every version is framed alike, from the bounds of all models together, so the body keeps its size from card to card.
    /// The camera looks at the statue's front from a little to the side and above, so an arm reaching forward shows.
    /// </para>
    /// <para>
    /// Each image is rendered over black and over white; the difference gives the opacity. This does not depend on whether
    /// the render pipeline keeps the alpha of the camera target.
    /// </para>
    /// </summary>
    public static class VenusThumbnailBuilder
    {
        public const string Folder = DNPDemoProjectSetup.Root + "/Art/Venus/Thumbnails";
        public const int Size = 256;

        // The statue faces -Z. Yaw moves the camera round to her left (+X), the side of the arm the scenario changes most;
        // pitch looks down a little.
        static readonly Quaternion ViewRotation = Quaternion.Euler(10f, -50f, 0f);
        const float Margin = 1.06f;

        [MenuItem("DNP Demo/Create/Venus Thumbnails")]
        public static void CreateFromMenu()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(VenusAssetBuilder.CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[DNPDemo] {VenusAssetBuilder.CatalogPath} がありません。DNP Demo/Create/Venus Versions で作成してください。");
                return;
            }
            Create(catalog);
        }

        public static string GetPath(VenusVersion version) => $"{Folder}/{version.name}_Thumbnail.png";

        /// <summary>Renders the thumbnail of every version of <paramref name="catalog"/> that has a model and assigns it.</summary>
        public static void Create(VenusVersionCatalog catalog)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            var rendered = new List<VenusVersion>();
            try
            {
                var models = new Dictionary<VenusVersion, GameObject>();
                bool hasBounds = false;
                Bounds bounds = default;
                foreach (VenusVersion version in catalog.Versions)
                {
                    if (version == null || version.Model == null)
                    {
                        continue;
                    }
                    var instance = Object.Instantiate(version.Model);
                    SceneManager.MoveGameObjectToScene(instance, scene);
                    instance.SetActive(false);
                    models.Add(version, instance);
                    // The models share their origin, so their local bounds go together.
                    if (HistoryPreview.TryMeasure(instance, instance.transform, out Bounds modelBounds))
                    {
                        if (hasBounds)
                        {
                            bounds.Encapsulate(modelBounds);
                        }
                        else
                        {
                            bounds = modelBounds;
                            hasBounds = true;
                        }
                    }
                }
                if (!hasBounds)
                {
                    Debug.LogWarning("[DNPDemo] サムネイルを作れる版のモデルがありません。");
                    return;
                }

                Camera camera = CreateCamera(scene, bounds);
                AddLight(scene, "Key", Quaternion.Euler(35f, 25f, 0f), 1.1f);
                AddLight(scene, "Fill", Quaternion.Euler(15f, -40f, 0f), 0.45f);
                AddLight(scene, "Back", Quaternion.Euler(30f, 170f, 0f), 0.35f);

                Directory.CreateDirectory(Folder);
                foreach (KeyValuePair<VenusVersion, GameObject> entry in models)
                {
                    foreach (GameObject model in models.Values)
                    {
                        model.SetActive(model == entry.Value);
                    }
                    byte[] png = Render(camera);
                    File.WriteAllBytes(GetPath(entry.Key), png);
                    rendered.Add(entry.Key);
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            foreach (VenusVersion version in rendered)
            {
                string path = GetPath(version);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                ConfigureImporter(path);
                version.SetThumbnail(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                EditorUtility.SetDirty(version);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[DNPDemo] {rendered.Count} 版のサムネイルを {Folder} に作成しました。");
        }

        /// <summary>An orthographic camera along <see cref="ViewRotation"/> that just holds <paramref name="bounds"/>.</summary>
        static Camera CreateCamera(Scene scene, Bounds bounds)
        {
            var go = new GameObject("ThumbnailCamera");
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false;
            camera.allowMSAA = true;

            float radius = bounds.extents.magnitude;
            go.transform.SetPositionAndRotation(bounds.center - ViewRotation * Vector3.forward * (radius + 1f), ViewRotation);
            float halfSize = 0f;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 center = go.transform.InverseTransformPoint(bounds.center);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Vector3 local = go.transform.InverseTransformPoint(corner) - center;
                halfSize = Mathf.Max(halfSize, Mathf.Abs(local.x), Mathf.Abs(local.y));
            }
            camera.orthographicSize = halfSize * Margin;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = (radius + 1f) * 2f + 1f;
            return camera;
        }

        static void AddLight(Scene scene, string name, Quaternion rotation, float intensity)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.rotation = rotation;
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        /// <summary>Renders over black and over white and returns the PNG with the opacity worked out from the two.</summary>
        static byte[] Render(Camera camera)
        {
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
            var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                Color[] overBlack = RenderOver(camera, Color.black, target, readback);
                Color[] overWhite = RenderOver(camera, Color.white, target, readback);
                var result = new Color[overBlack.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    Color black = overBlack[i];
                    Color white = overWhite[i];
                    // Over black a pixel is a·c, over white a·c + (1 - a).
                    float alpha = Mathf.Clamp01(1f - ((white.r - black.r) + (white.g - black.g) + (white.b - black.b)) / 3f);
                    result[i] = alpha > 1e-3f
                        ? new Color(Mathf.Clamp01(black.r / alpha), Mathf.Clamp01(black.g / alpha), Mathf.Clamp01(black.b / alpha), alpha)
                        : Color.clear;
                }
                readback.SetPixels(result);
                readback.Apply();
                return readback.EncodeToPNG();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
            }
        }

        static Color[] RenderOver(Camera camera, Color background, RenderTexture target, Texture2D readback)
        {
            camera.backgroundColor = background;
            camera.Render();
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            readback.Apply();
            return readback.GetPixels();
        }

        static void ConfigureImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = Size;
            importer.SaveAndReimport();
        }
    }
}
