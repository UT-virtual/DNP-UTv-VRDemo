using System.IO;
using DNPDemo.Pen;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Generates the spatial pen prefab as a variant of the pen model, so a re-exported model updates the prefab.
    /// An existing prefab is kept as it is; after creation it is edited by hand.
    /// </summary>
    public static class PenPrefabBuilder
    {
        const string ArtFolder = DNPDemoProjectSetup.Root + "/Art/Pen/FBXs";
        const string ModelPath = ArtFolder + "/Pen.fbx";
        const string MicIndicatorMaterialPath = ArtFolder + "/PenMicIndicator.mat";

        public const string PenPath = DNPDemoProjectSetup.Root + "/Prefabs/Pen/SpatialPen.prefab";

        // Child names in the pen model.
        const string BodyName = "PenBody";
        const string MicrophoneName = "PenMicrophone";
        const string SelectAreaName = "PenSelectArea";

        [MenuItem("DNP Demo/Create/Pen Prefab")]
        public static GameObject CreateIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PenPath);
            if (existing != null)
            {
                return existing;
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var micIndicator = AssetDatabase.LoadAssetAtPath<Material>(MicIndicatorMaterialPath);
            if (model == null || micIndicator == null)
            {
                Debug.LogError($"[DNPDemo] {ModelPath} または {MicIndicatorMaterialPath} がありません。");
                return null;
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                root.name = "SpatialPen";
                var view = root.AddComponent<SpatialPenView>();
                WorldUiBuilder.Assign(view, "_body", FindRequired<SkinnedMeshRenderer>(root, BodyName));
                WorldUiBuilder.Assign(view, "_microphone", FindRequired<Renderer>(root, MicrophoneName));
                WorldUiBuilder.Assign(view, "_selectArea", FindRequired<Renderer>(root, SelectAreaName));
                WorldUiBuilder.Assign(view, "_micIndicatorMaterial", micIndicator);

                Directory.CreateDirectory(Path.GetDirectoryName(PenPath));
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PenPath);
                Debug.Log($"[DNPDemo] Created {PenPath}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static T FindRequired<T>(GameObject root, string childName) where T : Component
        {
            Transform child = root.transform.Find(childName);
            T component = child != null ? child.GetComponent<T>() : null;
            if (component == null)
            {
                throw new System.InvalidOperationException($"{ModelPath} に {childName}（{typeof(T).Name}）がありません。");
            }
            return component;
        }
    }
}
