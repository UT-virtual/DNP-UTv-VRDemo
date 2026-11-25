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
        public static readonly Vector3 RightShoulder = new(0.3f, 1.6f, 0f);
        static readonly Vector3 HeadCenter = new(0f, 2.15f, 0f);

        /// <summary>Right elbow of Ver 3.0 and 3.1. The arm is the same in both; only the wrist twists.</summary>
        public static readonly Vector3 RightElbow = new(0.4f, 1.3f, -0.1f);

        /// <summary>Right wrist of Ver 3.0 and 3.1, the joint the wrist twists about.</summary>
        public static readonly Vector3 RightWrist = new(0.36f, 1.55f, -0.36f);

        /// <summary>The handle continues the forearm: the centre of the mirror is this far beyond the wrist, in metres.</summary>
        public const float MirrorReach = 0.16f;

        /// <summary>
        /// Ver 3.0 → Ver 3.1: the wrist twists this far about the forearm (degrees, Unity's rotation direction about
        /// <see cref="ForearmDirection"/>), which turns the mirror from facing up and outwards to facing the head.
        /// </summary>
        public const float MirrorTwistDegrees = -50f;

        /// <summary>Direction of the right forearm from the elbow to the wrist; the axis of the wrist's twist.</summary>
        public static Vector3 ForearmDirection => (RightWrist - RightElbow).normalized;

        /// <summary>Centre of the hand mirror of Ver 3.0 and 3.1. On the twist axis, so the twist does not move it.</summary>
        public static Vector3 MirrorCenter => RightWrist + ForearmDirection * MirrorReach;

        /// <summary>
        /// Rotation of the right hand and the mirror: up along the forearm, forward out of the mirror's face. Ver 3.1 faces
        /// the head; Ver 3.0 is the same hand twisted back by <see cref="MirrorTwistDegrees"/>.
        /// </summary>
        public static Quaternion MirrorRotation(bool facesHead)
        {
            Vector3 axis = ForearmDirection;
            Quaternion towardHead = Quaternion.LookRotation(Vector3.ProjectOnPlane(HeadCenter - MirrorCenter, axis), axis);
            return facesHead ? towardHead : Quaternion.AngleAxis(-MirrorTwistDegrees, axis) * towardHead;
        }

        /// <summary>One primitive of the hand mirror, in the mirror's space (origin at the centre of the mirror, see <see cref="MirrorRotation"/>).</summary>
        public readonly struct MirrorPart
        {
            public MirrorPart(string name, PrimitiveType type, Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Name = name;
                Type = type;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public string Name { get; }
            public PrimitiveType Type { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }
        }

        /// <summary>
        /// The placeholder hand mirror: a disc facing forward and a handle running back to the wrist along the forearm.
        /// The statue's mirror of Ver 3.0 and 3.1 and the separate mirror of steps 13 to 15 use the same parts.
        /// </summary>
        public static readonly MirrorPart[] MirrorParts =
        {
            // A cylinder's flat faces point along its Y axis; the disc faces forward.
            new("Mirror", PrimitiveType.Cylinder, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.18f, 0.006f, 0.18f)),
            new("MirrorHandle", PrimitiveType.Cylinder, new Vector3(0f, -0.6f * MirrorReach, 0f), Quaternion.identity, new Vector3(0.025f, 0.06f, 0.025f)),
        };

        /// <summary>The right hand around the handle, in the mirror's space. It twists with the mirror.</summary>
        public static readonly MirrorPart RightHandPart =
            new("RightHand", PrimitiveType.Cube, new Vector3(0f, -MirrorReach, 0f), Quaternion.identity, new Vector3(0.07f, 0.09f, 0.05f));

        sealed class Definition
        {
            public Definition(string id, string parentId, bool startsBranch, string displayName, string fileName, Action<Builder> build)
            {
                Id = id;
                ParentId = parentId;
                StartsBranch = startsBranch;
                DisplayName = displayName;
                FileName = fileName;
                Build = build;
            }

            public string Id { get; }

            /// <summary>Id of the version this one was made from (the branches of the history panel), or null for the original.</summary>
            public string ParentId { get; }

            /// <summary>True when the version starts a new branch off its parent (<see cref="VenusVersion.StartsBranch"/>).</summary>
            public bool StartsBranch { get; }

            public string DisplayName { get; }
            public string FileName { get; }
            public Action<Builder> Build { get; }
            public string VersionPath => $"{DataFolder}/{FileName}.asset";
            public string ModelPath => $"{ModelFolder}/{FileName}_Placeholder.prefab";
        }

        /// <summary>
        /// The versions in the order the scenario reaches them, each with the version it was made from (docs/PLAN.md §2.5):
        /// Scholar B's Ver 1.1 starts a branch off Scholar A's Ver 1.0, and Ver 2.0 starts another off Ver 1.1 instead of
        /// following the rejected Ver 1.2.
        /// </summary>
        static readonly Definition[] Definitions =
        {
            new(ArmlessId, null, false, "腕なし", "Venus_Armless", b => b.Body()),
            new(Ver10Id, ArmlessId, false, "Ver 1.0", "Venus_Ver1_0", b =>
            {
                // Left arm straight forward, palm up. The apple on the palm is the separate Apple object of the step
                // definitions until Ver 1.1, because step 6 moves it.
                b.Body();
                Vector3 hand = new(-0.29f, 1.58f, -0.55f);
                b.Limb("LeftArm", LeftShoulder, hand);
                b.Box("LeftHand", new Vector3(-0.275f, 1.56f, -0.6f), new Vector3(0.1f, 0.02f, 0.12f), Quaternion.identity);
            }),
            new(Ver11Id, Ver10Id, true, "Ver 1.1", "Venus_Ver1_1", b =>
            {
                // Same arm; the fingers hold the apple from above.
                b.Body();
                b.Limb("LeftArm", LeftShoulder, new Vector3(-0.29f, 1.58f, -0.55f));
                b.Box("LeftHand", new Vector3(-0.275f, 1.52f, -0.6f), new Vector3(0.1f, 0.03f, 0.12f), Quaternion.identity);
                b.Apple(new Vector3(-0.275f, 1.45f, -0.6f));
            }),
            new(Ver12Id, Ver11Id, false, "Ver 1.2", "Venus_Ver1_2", b =>
            {
                // The arm slid inwards as one straight piece: an awkward, shortened angle.
                b.Body();
                Vector3 hand = new(-0.06f, 1.5f, -0.42f);
                b.Limb("LeftArm", LeftShoulder, hand);
                b.Box("LeftHand", hand + new Vector3(0f, -0.02f, -0.03f), new Vector3(0.1f, 0.03f, 0.12f), Quaternion.identity);
                b.Apple(hand + new Vector3(0f, -0.09f, -0.03f));
            }),
            new(Ver20Id, Ver11Id, true, "Ver 2.0", "Venus_Ver2_0", b =>
            {
                b.Body();
                b.CurvedLeftArm();
            }),
            new(Ver30Id, Ver20Id, false, "Ver 3.0", "Venus_Ver3_0", b =>
            {
                // Right arm raised, the mirror facing up and outwards, away from her eyes.
                b.Body();
                b.CurvedLeftArm();
                b.RightArmWithMirror(false);
            }),
            new(Ver31Id, Ver30Id, false, "Ver 3.1", "Venus_Ver3_1", b =>
            {
                // The same arm with the wrist twisted so the mirror faces the head (step 17).
                b.Body();
                b.CurvedLeftArm();
                b.RightArmWithMirror(true);
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
            var byId = new Dictionary<string, VenusVersion>();
            foreach (Definition definition in Definitions)
            {
                VenusVersion parent = definition.ParentId != null ? byId[definition.ParentId] : null;
                VenusVersion version = CreateVersion(definition, parent);
                versions.Add(version);
                byId.Add(definition.Id, version);
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

        static VenusVersion CreateVersion(Definition definition, VenusVersion parent)
        {
            var existing = AssetDatabase.LoadAssetAtPath<VenusVersion>(definition.VersionPath);
            if (existing != null)
            {
                return existing;
            }
            var version = ScriptableObject.CreateInstance<VenusVersion>();
            version.Initialize(definition.Id, definition.DisplayName, CreateModel(definition), parent, definition.StartsBranch);
            Directory.CreateDirectory(DataFolder);
            AssetDatabase.CreateAsset(version, definition.VersionPath);
            Debug.Log($"[DNPDemo] Created {definition.VersionPath}");
            return version;
        }

        /// <summary>
        /// Builds the placeholder model of the version <paramref name="id"/> again from its definition and overwrites the
        /// prefab in place, so the version asset keeps referring to it. For when a placeholder's definition changes; the
        /// thumbnail is not retaken (DNP Demo/Create/Venus Thumbnails).
        /// </summary>
        public static GameObject RebuildPlaceholderModel(string id)
        {
            Definition definition = Array.Find(Definitions, candidate => candidate.Id == id)
                ?? throw new ArgumentException($"No placeholder version has the id {id}.", nameof(id));
            return BuildModel(definition);
        }

        static GameObject CreateModel(Definition definition)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(definition.ModelPath);
            if (existing != null)
            {
                return existing;
            }
            return BuildModel(definition);
        }

        static GameObject BuildModel(Definition definition)
        {
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
            /// Right arm raised in front of the shoulder, the hand holding the mirror (<see cref="MirrorParts"/>). The mirror
            /// faces the head when <paramref name="facesHead"/> (Ver 3.1), otherwise up and outwards (Ver 3.0).
            /// </summary>
            public void RightArmWithMirror(bool facesHead)
            {
                Limb("RightUpperArm", RightShoulder, RightElbow);
                Limb("RightForearm", RightElbow, RightWrist);
                Quaternion rotation = MirrorRotation(facesHead);
                Part(RightHandPart, MirrorCenter, rotation, _marble);
                _mirror ??= CreateMirrorMaterial();
                foreach (MirrorPart part in MirrorParts)
                {
                    Part(part, MirrorCenter, rotation, _mirror);
                }
            }

            void Part(MirrorPart part, Vector3 origin, Quaternion rotation, Material material) =>
                Primitive(part.Type, part.Name, origin + rotation * part.Position, part.Scale, rotation * part.Rotation, material);

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
