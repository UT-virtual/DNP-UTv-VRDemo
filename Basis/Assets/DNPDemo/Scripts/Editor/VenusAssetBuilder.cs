using System;
using System.Collections.Generic;
using System.IO;
using DNPDemo.Venus;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Generates the placeholder model of every statue version (docs/PLAN.md §2.5, §3), the <see cref="VenusVersion"/>
    /// assets that point at them and the <see cref="VenusVersionCatalog"/>. Existing assets are kept as they are; after
    /// creation they are edited by hand, and the real models replace the placeholders in each <see cref="VenusVersion"/>.
    /// <para>
    /// The placeholders are primitives in the space of the old placeholder statue: the statue faces -Z, its left side is
    /// -X, and the body stands on the origin (the top of the pedestal).
    /// </para>
    /// </summary>
    public static class VenusAssetBuilder
    {
        const string DataFolder = DNPDemoProjectSetup.Root + "/Data/Venus";
        const string ModelFolder = DNPDemoProjectSetup.Root + "/Art/Venus/Placeholders";
        const string MaterialFolder = DNPDemoProjectSetup.Root + "/Art/Venus/Materials";

        public const string CatalogPath = DataFolder + "/VenusVersions.asset";
        public const string MarbleMaterialPath = MaterialFolder + "/M_MarblePlaceholder.mat";
        public const string AppleMaterialPath = MaterialFolder + "/M_ApplePlaceholder.mat";
        public const string MirrorMaterialPath = MaterialFolder + "/M_MirrorPlaceholder.mat";
        public const string TransitionShaderPath = MaterialFolder + "/ModelTransition.shader";
        public const string TransitionMaterialPath = MaterialFolder + "/M_ModelTransition.mat";
        public const string TransitionProfilePath = DNPDemoProjectSetup.Root + "/Data/Venus/ModelTransitionProfile.asset";
        public const string StatuePrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/Statue_Original.prefab";
        public const string ComparisonTablePrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/ComparisonTable.prefab";

        public const string ArmlessId = "armless";
        public const string Ver10Id = "v1.0";
        public const string Ver11Id = "v1.1";
        public const string Ver12Id = "v1.2";
        public const string Ver20Id = "v2.0";
        public const string Ver30Id = "v3.0";
        public const string Ver31Id = "v3.1";

        const float ArmThickness = 0.09f;
        const float AppleSize = 0.1f;

        static readonly Vector3 LeftShoulder = new(-0.3f, 1.6f, 0f);
        static readonly Vector3 RightShoulder = new(0.3f, 1.6f, 0f);
        static readonly Vector3 HeadCenter = new(0f, 2.15f, 0f);

        sealed class Definition
        {
            public Definition(string id, string displayName, string fileName, Action<Builder> build)
            {
                Id = id;
                DisplayName = displayName;
                FileName = fileName;
                Build = build;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string FileName { get; }
            public Action<Builder> Build { get; }
            public string VersionPath => $"{DataFolder}/{FileName}.asset";
            public string ModelPath => $"{ModelFolder}/{FileName}_Placeholder.prefab";
        }

        /// <summary>The versions in the order the scenario reaches them (docs/PLAN.md §2.5).</summary>
        static readonly Definition[] Definitions =
        {
            new(ArmlessId, "腕なし", "Venus_Armless", b => b.Body()),
            new(Ver10Id, "Ver 1.0", "Venus_Ver1_0", b =>
            {
                // Left arm straight forward, palm up. The apple on the palm is the separate Apple object of the step
                // definitions until Ver 1.1, because step 6 moves it.
                b.Body();
                Vector3 hand = new(-0.29f, 1.58f, -0.55f);
                b.Limb("LeftArm", LeftShoulder, hand);
                b.Box("LeftHand", new Vector3(-0.275f, 1.56f, -0.6f), new Vector3(0.1f, 0.02f, 0.12f), Quaternion.identity);
            }),
            new(Ver11Id, "Ver 1.1", "Venus_Ver1_1", b =>
            {
                // Same arm; the fingers hold the apple from above.
                b.Body();
                b.Limb("LeftArm", LeftShoulder, new Vector3(-0.29f, 1.58f, -0.55f));
                b.Box("LeftHand", new Vector3(-0.275f, 1.52f, -0.6f), new Vector3(0.1f, 0.03f, 0.12f), Quaternion.identity);
                b.Apple(new Vector3(-0.275f, 1.45f, -0.6f));
            }),
            new(Ver12Id, "Ver 1.2", "Venus_Ver1_2", b =>
            {
                // The arm slid inwards as one straight piece: an awkward, shortened angle.
                b.Body();
                Vector3 hand = new(-0.06f, 1.5f, -0.42f);
                b.Limb("LeftArm", LeftShoulder, hand);
                b.Box("LeftHand", hand + new Vector3(0f, -0.02f, -0.03f), new Vector3(0.1f, 0.03f, 0.12f), Quaternion.identity);
                b.Apple(hand + new Vector3(0f, -0.09f, -0.03f));
            }),
            new(Ver20Id, "Ver 2.0", "Venus_Ver2_0", b =>
            {
                b.Body();
                b.CurvedLeftArm();
            }),
            new(Ver30Id, "Ver 3.0", "Venus_Ver3_0", b =>
            {
                // Right arm raised, holding the mirror facing forward.
                b.Body();
                b.CurvedLeftArm();
                b.RightArmWithMirror(Vector3.back);
            }),
            new(Ver31Id, "Ver 3.1", "Venus_Ver3_1", b =>
            {
                // The wrist turned so the mirror faces the head.
                b.Body();
                b.CurvedLeftArm();
                b.RightArmWithMirror(null);
            }),
        };

        [MenuItem("DNP Demo/Create/Venus Versions")]
        public static void CreateFromMenu()
        {
            EditorGUIUtility.PingObject(CreateIfMissing());
        }

        /// <summary>Creates whatever is missing of the placeholder models, the version assets and the catalog.</summary>
        public static VenusVersionCatalog CreateIfMissing()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(CatalogPath);
            if (catalog != null)
            {
                EnsureModelTransitionAssets();
                EnsureModelTransitionComponents();
                return catalog;
            }
            var versions = new List<VenusVersion>();
            foreach (Definition definition in Definitions)
            {
                versions.Add(CreateVersion(definition));
            }
            catalog = ScriptableObject.CreateInstance<VenusVersionCatalog>();
            catalog.SetVersions(versions);
            Directory.CreateDirectory(DataFolder);
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DNPDemo] Created {CatalogPath}");
            EnsureModelTransitionAssets();
            EnsureModelTransitionComponents();
            return catalog;
        }

        static void EnsureModelTransitionAssets()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(TransitionShaderPath);
            if (shader == null)
            {
                Debug.LogWarning($"[DNPDemo] {TransitionShaderPath} を読み込めません。", null);
                return;
            }

            var transitionMaterial = AssetDatabase.LoadAssetAtPath<Material>(TransitionMaterialPath);
            if (transitionMaterial == null)
            {
                transitionMaterial = new Material(shader);
                transitionMaterial.name = "M_ModelTransition";
                AssetDatabase.CreateAsset(transitionMaterial, TransitionMaterialPath);
            }

            var profile = AssetDatabase.LoadAssetAtPath<ModelTransitionProfile>(TransitionProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ModelTransitionProfile>();
                profile.name = "ModelTransitionProfile";
                profile.Initialize(transitionMaterial, null, 1f);
                AssetDatabase.CreateAsset(profile, TransitionProfilePath);
                AssetDatabase.SaveAssets();
            }
        }

        static void EnsureModelTransitionComponents()
        {
            ModelTransitionProfile profile = AssetDatabase.LoadAssetAtPath<ModelTransitionProfile>(TransitionProfilePath);
            EnsureModelTransitionComponents(StatuePrefabPath, profile);
            EnsureModelTransitionComponents(ComparisonTablePrefabPath, profile);
        }

        static void EnsureModelTransitionComponents(string prefabPath, ModelTransitionProfile profile)
        {
            if (!File.Exists(prefabPath))
            {
                return;
            }
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                bool changed = false;
                foreach (VenusStatue statue in contents.GetComponentsInChildren<VenusStatue>(true))
                {
                    ModelTransition transition = statue.GetComponent<ModelTransition>();
                    if (transition == null)
                    {
                        transition = statue.gameObject.AddComponent<ModelTransition>();
                        changed = true;
                    }

                    var transitionSo = new SerializedObject(transition);
                    SerializedProperty profileProperty = transitionSo.FindProperty("_profile");
                    if (profileProperty != null && profileProperty.objectReferenceValue != profile)
                    {
                        profileProperty.objectReferenceValue = profile;
                        transitionSo.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>The version asset with <paramref name="id"/> in the catalog, or null when the catalog has not been created.</summary>
        public static VenusVersion LoadVersion(string id)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(CatalogPath);
            return catalog != null ? catalog.Find(id) : null;
        }

        static VenusVersion CreateVersion(Definition definition)
        {
            var existing = AssetDatabase.LoadAssetAtPath<VenusVersion>(definition.VersionPath);
            if (existing != null)
            {
                return existing;
            }
            var version = ScriptableObject.CreateInstance<VenusVersion>();
            version.Initialize(definition.Id, definition.DisplayName, CreateModel(definition));
            Directory.CreateDirectory(DataFolder);
            AssetDatabase.CreateAsset(version, definition.VersionPath);
            Debug.Log($"[DNPDemo] Created {definition.VersionPath}");
            return version;
        }

        static GameObject CreateModel(Definition definition)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(definition.ModelPath);
            if (existing != null)
            {
                return existing;
            }
            var root = new GameObject($"{definition.FileName}_Placeholder");
            try
            {
                definition.Build(new Builder(root.transform));
                Directory.CreateDirectory(ModelFolder);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, definition.ModelPath);
                Debug.Log($"[DNPDemo] Created {definition.ModelPath}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException($"{path} was not found.");
            }
            return material;
        }

        static Material CreateMirrorMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MirrorMaterialPath);
            if (existing != null)
            {
                return existing;
            }
            // Same shader as the other placeholders, tinted like polished bronze.
            var material = new Material(LoadMaterial(MarbleMaterialPath));
            material.SetColor("_BaseColor", new Color(0.72f, 0.58f, 0.36f));
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.8f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.75f);
            AssetDatabase.CreateAsset(material, MirrorMaterialPath);
            Debug.Log($"[DNPDemo] Created {MirrorMaterialPath}");
            return material;
        }

        /// <summary>Adds primitives without colliders under one placeholder root.</summary>
        sealed class Builder
        {
            readonly Transform _root;
            readonly Material _marble;
            Material _apple;
            Material _mirror;

            public Builder(Transform root)
            {
                _root = root;
                _marble = LoadMaterial(MarbleMaterialPath);
            }

            public void Body()
            {
                Primitive(PrimitiveType.Capsule, "Body", new Vector3(0f, 1f, 0f), new Vector3(0.55f, 1f, 0.4f), Quaternion.identity, _marble);
                Primitive(PrimitiveType.Sphere, "Head", HeadCenter, Vector3.one * 0.28f, Quaternion.identity, _marble);
            }

            /// <summary>Upper arm and forearm bent so the hand rests in front of the chest with the apple (Ver 2.0 onwards).</summary>
            public void CurvedLeftArm()
            {
                Vector3 elbow = new(-0.32f, 1.25f, -0.12f);
                Vector3 wrist = new(-0.06f, 1.36f, -0.32f);
                Limb("LeftUpperArm", LeftShoulder, elbow);
                Limb("LeftForearm", elbow, wrist);
                Box("LeftHand", wrist + new Vector3(0.02f, -0.02f, -0.04f), new Vector3(0.1f, 0.03f, 0.12f), Quaternion.identity);
                Apple(wrist + new Vector3(0.02f, -0.09f, -0.04f));
            }

            /// <summary>
            /// Right arm raised in front of the shoulder with the hand mirror. The mirror faces <paramref name="facing"/>,
            /// or the head when it is null.
            /// </summary>
            public void RightArmWithMirror(Vector3? facing)
            {
                Vector3 elbow = new(0.4f, 1.3f, -0.1f);
                Vector3 wrist = new(0.36f, 1.55f, -0.36f);
                Limb("RightUpperArm", RightShoulder, elbow);
                Limb("RightForearm", elbow, wrist);
                Vector3 mirrorCenter = wrist + new Vector3(0f, 0.16f, -0.02f);
                Vector3 normal = (facing ?? (HeadCenter - mirrorCenter)).normalized;
                // A cylinder's flat faces point along its Y axis.
                Quaternion faceRotation = Quaternion.FromToRotation(Vector3.up, normal);
                _mirror ??= CreateMirrorMaterial();
                Primitive(PrimitiveType.Cylinder, "MirrorHandle", Vector3.Lerp(wrist, mirrorCenter, 0.4f),
                    new Vector3(0.025f, 0.06f, 0.025f), Quaternion.FromToRotation(Vector3.up, mirrorCenter - wrist), _mirror);
                Primitive(PrimitiveType.Cylinder, "Mirror", mirrorCenter, new Vector3(0.18f, 0.006f, 0.18f), faceRotation, _mirror);
            }

            public void Apple(Vector3 position)
            {
                _apple ??= LoadMaterial(AppleMaterialPath);
                Primitive(PrimitiveType.Sphere, "Apple", position, Vector3.one * AppleSize, Quaternion.identity, _apple);
            }

            /// <summary>A cylinder from <paramref name="from"/> to <paramref name="to"/>.</summary>
            public void Limb(string name, Vector3 from, Vector3 to)
            {
                Vector3 axis = to - from;
                // Unity's cylinder is 2 units tall.
                Primitive(PrimitiveType.Cylinder, name, (from + to) * 0.5f,
                    new Vector3(ArmThickness, axis.magnitude * 0.5f, ArmThickness),
                    Quaternion.FromToRotation(Vector3.up, axis), _marble);
            }

            public void Box(string name, Vector3 position, Vector3 size, Quaternion rotation) =>
                Primitive(PrimitiveType.Cube, name, position, size, rotation, _marble);

            void Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
            {
                GameObject go = GameObject.CreatePrimitive(type);
                go.name = name;
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(_root, false);
                go.transform.SetLocalPositionAndRotation(position, rotation);
                go.transform.localScale = scale;
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }
    }
}
