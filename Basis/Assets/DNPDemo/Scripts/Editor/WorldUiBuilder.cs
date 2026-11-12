using System;
using System.IO;
using Basis.Scripts.UI;
using DNPDemo.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DNPDemo.Editor
{
    /// <summary>Shared helpers for the editor code that generates the demo's world-space uGUI prefabs.</summary>
    public static class WorldUiBuilder
    {
        /// <summary>Layer every laser-interactable panel must live on. Basis only draws the laser and dispatches pointer events on UI layers.</summary>
        public const string UILayerName = "UI";

        /// <summary>Canvas pixels per world metre. A 1000 px wide panel is 1 m wide.</summary>
        public const float PixelsPerMeter = 1000f;

        /// <summary>
        /// Builds a prefab at <paramref name="path"/> unless one already exists, and returns the asset.
        /// Existing prefabs are never overwritten: after creation they are edited by hand like any other asset.
        /// </summary>
        public static GameObject CreatePrefabIfMissing(string path, string rootName, Action<GameObject> build)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }
            var root = new GameObject(rootName, typeof(RectTransform));
            try
            {
                build(root);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[DNPDemo] Created {path}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Turns <paramref name="root"/> into a world-space canvas of <paramref name="size"/> pixels.
        /// <paramref name="interactive"/> adds the Basis raycaster so the laser can operate it.
        /// </summary>
        public static Canvas SetupWorldCanvas(GameObject root, Vector2 size, bool interactive)
        {
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one / PixelsPerMeter;
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (interactive)
            {
                var raycaster = root.AddComponent<BasisGraphicUIRayCaster>();
                raycaster.Canvas = canvas;
            }
            return canvas;
        }

        public static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            foreach (Transform child in target)
            {
                SetLayerRecursively(child, layer);
            }
        }

        public static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        /// <summary>A child filling its parent with the given insets, laying its children out top to bottom.</summary>
        public static Transform CreateVerticalGroup(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, float spacing)
        {
            GameObject group = CreateChild(name, parent);
            Anchor((RectTransform)group.transform, Vector2.zero, Vector2.one, offsetMin, offsetMax);
            var layout = group.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return group.transform;
        }

        public static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, float preferredHeight = -1)
        {
            GameObject go = TMP_DefaultControls.CreateText(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            if (preferredHeight > 0)
            {
                go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            }
            return tmp;
        }

        public static Button CreateButton(string name, Transform parent, string label, float preferredHeight, float fontSize)
        {
            GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var labelText = go.GetComponentInChildren<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = fontSize;
            labelText.color = new Color(0.1f, 0.1f, 0.12f);
            if (preferredHeight > 0)
            {
                go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            }
            return go.GetComponent<Button>();
        }

        public static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        /// <summary>Makes <paramref name="root"/> follow the viewer's head at the given distance and height offset.</summary>
        public static void AddHeadFollower(GameObject root, float distance, float heightOffset, float sharpness)
        {
            var follower = root.AddComponent<HeadFollower>();
            var so = new SerializedObject(follower);
            so.FindProperty("_distance").floatValue = distance;
            so.FindProperty("_heightOffset").floatValue = heightOffset;
            so.FindProperty("_followSharpness").floatValue = sharpness;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a serialized object reference by field name (works for private [SerializeField] fields).</summary>
        public static void Assign(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                throw new ArgumentException($"{target.GetType().Name} has no serialized field '{property}'.");
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
