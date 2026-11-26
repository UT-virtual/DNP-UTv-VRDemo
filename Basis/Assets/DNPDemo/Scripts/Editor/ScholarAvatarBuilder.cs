using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DNPDemo.Scenario;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Puts the VRoid models (VRM 0.x) of scholar A and B into their prefab variants.
    /// <list type="number">
    /// <item>"1. Import VRM Models" lets UniVRM turn <c>Art/Scholars/Source~/&lt;name&gt;.vrm</c> into
    /// <c>Art/Scholars/&lt;name&gt;/&lt;name&gt;.prefab</c> and its assets. The import finishes a few frames later.</item>
    /// <item>"2. Build Scholar Prefabs" strips the VRM components from the model, gives it the idle animator, stands it
    /// on the floor in its idle pose, moves the voice to its mouth and the subtitle above its head, and connects the
    /// uLipSync vowels to the mouth blend shapes. Run it again after every import, because UniVRM writes the model
    /// prefab anew and the references into it have to be set again.</item>
    /// </list>
    /// The .vrm files stay in a folder Unity does not import, so UniVRM never rewrites the model prefab on its own
    /// (CONTRIBUTING.md).
    /// </summary>
    public static class ScholarAvatarBuilder
    {
        const string ArtFolder = DNPDemoProjectSetup.Root + "/Art/Scholars";
        const string SourceFolder = ArtFolder + "/Source~";
        const string PrefabFolder = DNPDemoProjectSetup.Root + "/Prefabs";
        public const string ScholarsPrefabPath = PrefabFolder + "/Scholars.prefab";
        public const string IdleControllerPath = ArtFolder + "/AC_ScholarIdle.controller";
        const string IdleClipSourcePath = "Packages/com.unity.3rdpersondemo/Stand--Idle.anim.fbx";

        /// <summary>Name of the model instance under each scholar variant's root.</summary>
        public const string AvatarObjectName = "Avatar";

        /// <summary>The uLipSync phonemes that open the mouth, each driving the VRoid blend shape <see cref="MouthShapePrefix"/> + phoneme.</summary>
        public static readonly string[] Vowels = { "A", "I", "U", "E", "O" };
        public const string MouthShapePrefix = "Fcl_MTH_";

        /// <summary>Gap between the top of the head and the bottom of the subtitle, in metres.</summary>
        public const float SubtitleClearance = 0.25f;

        /// <summary>
        /// Margin added on every side of a skinned mesh's bounds, in metres, for the sway of the idle animation and the
        /// open mouth. The renderers do not update their bounds while playing, so the bounds must hold the whole motion.
        /// </summary>
        public const float BoundsMargin = 0.05f;

        /// <summary>The upper arm turns at least this far from the T-pose in the idle pose; less means the clip did not apply.</summary>
        const float MinIdleArmAngle = 20f;

        static readonly string[] VrmNamespaces = { "VRM", "UniGLTF", "UniHumanoid" };

        public static readonly ScholarAvatar[] Scholars =
        {
            new("ScholarA", PrefabFolder + "/Scholars/Scholar_A_Archaeologist.prefab"),
            new("ScholarB", PrefabFolder + "/Scholars/Scholar_B_Engineer.prefab"),
        };

        public readonly struct ScholarAvatar
        {
            public readonly string Name;
            public readonly string VariantPath;

            public ScholarAvatar(string name, string variantPath)
            {
                Name = name;
                VariantPath = variantPath;
            }

            public string SourceVrmPath => $"{SourceFolder}/{Name}.vrm";
            public string ModelFolder => $"{ArtFolder}/{Name}";
            public string ModelPrefabPath => $"{ModelFolder}/{Name}.prefab";

            public override string ToString() => Name;
        }

        /// <summary>Where the idle pose puts the parts the variant lines up with, relative to the model's root.</summary>
        public readonly struct IdlePose
        {
            /// <summary>Lowest point of the meshes: the soles.</summary>
            public readonly float FootY;
            /// <summary>Highest point of the meshes: the top of the hair.</summary>
            public readonly float TopY;
            /// <summary>Centre of the vertices the "A" mouth shape moves.</summary>
            public readonly Vector3 Mouth;
            public readonly Vector3 Head;
            /// <summary>
            /// Box around each skinned mesh's posed vertices, in the space of its <see cref="BoundsSpace"/> (the space of
            /// <see cref="SkinnedMeshRenderer.localBounds"/>), keyed by the renderer's path under the model's root.
            /// </summary>
            public readonly IReadOnlyDictionary<string, Bounds> VertexBounds;
            /// <summary>Maps each renderer's bounds space (same keys as <see cref="VertexBounds"/>) to the model's root in the idle pose.</summary>
            public readonly IReadOnlyDictionary<string, Matrix4x4> BoundsSpaceToModel;

            public IdlePose(float footY, float topY, Vector3 mouth, Vector3 head, IReadOnlyDictionary<string, Bounds> vertexBounds,
                IReadOnlyDictionary<string, Matrix4x4> boundsSpaceToModel)
            {
                FootY = footY;
                TopY = topY;
                Mouth = mouth;
                Head = head;
                VertexBounds = vertexBounds;
                BoundsSpaceToModel = boundsSpaceToModel;
            }

            /// <summary>Raise of the model that puts its soles on the variant's floor (y = 0).</summary>
            public Vector3 Lift => new(0f, -FootY, 0f);
            public Vector3 VoicePosition => Mouth + Lift;
            public Vector3 SubtitleAnchorPosition => new Vector3(Head.x, TopY + SubtitleClearance, Head.z) + Lift;
        }

        [MenuItem("DNP Demo/Create/Scholar Avatars/1. Import VRM Models")]
        public static void ImportModels()
        {
            foreach (ScholarAvatar scholar in Scholars)
            {
                string vrmPath = Path.GetFullPath(scholar.SourceVrmPath);
                if (!File.Exists(vrmPath))
                {
                    throw new FileNotFoundException($"Put the VRM of {scholar.Name} at {scholar.SourceVrmPath}.", vrmPath);
                }
                EnsureFolder(scholar.ModelFolder);
                VRM.vrmAssetPostprocessor.ImportVrmAndCreatePrefab(vrmPath, UniGLTF.UnityPath.FromUnityPath(scholar.ModelPrefabPath));
            }
            Debug.Log("[DNPDemo] Importing the scholar VRMs. When the console shows 'Import complete' for both, run DNP Demo/Create/Scholar Avatars/2. Build Scholar Prefabs.");
        }

        [MenuItem("DNP Demo/Create/Scholar Avatars/2. Build Scholar Prefabs")]
        public static void BuildPrefabs()
        {
            AnimatorController idle = CreateOrUpdateIdleController();

            // Measure every scholar before writing anything, so a failed measurement leaves the prefabs as they were.
            var poses = new IdlePose[Scholars.Length];
            for (int i = 0; i < Scholars.Length; i++)
            {
                poses[i] = MeasureIdlePose(LoadImportedModel(Scholars[i]), idle);
            }

            for (int i = 0; i < Scholars.Length; i++)
            {
                ScholarAvatar scholar = Scholars[i];
                IdlePose pose = poses[i];
                GameObject model = PrepareModel(scholar, idle, pose);
                BuildVariant(scholar, model, pose);
                Debug.Log($"[DNPDemo] {scholar.Name}: soles at {pose.FootY:F3} m, top at {pose.TopY:F3} m, mouth at {pose.Mouth.y:F3} m before the lift.");
                LogBounds(scholar, model, pose);
            }
            RevertBoundsOverridesInScholarsPrefab();
            AssetDatabase.SaveAssets();
        }

        /// <summary>True for the components UniVRM adds (VRMMeta, VRMBlendShapeProxy, VRMSpringBone, ...).</summary>
        public static bool IsVrmComponent(Component component)
        {
            if (component == null)
            {
                return false;
            }
            string ns = component.GetType().Namespace ?? string.Empty;
            return VrmNamespaces.Any(n => ns == n || ns.StartsWith(n + ".", StringComparison.Ordinal));
        }

        /// <summary>The renderer whose mesh has a mouth blend shape for every vowel: VRoid's face.</summary>
        public static SkinnedMeshRenderer FindMouthRenderer(GameObject model)
        {
            SkinnedMeshRenderer[] faces = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.sharedMesh != null && Vowels.All(v => r.sharedMesh.GetBlendShapeIndex(MouthShapePrefix + v) >= 0))
                .ToArray();
            if (faces.Length != 1)
            {
                throw new InvalidOperationException(
                    $"'{model.name}' must have exactly one mesh with the blend shapes {string.Join(", ", Vowels.Select(v => MouthShapePrefix + v))}; found {faces.Length}.");
            }
            return faces[0];
        }

        /// <summary>
        /// Puts a temporary copy of <paramref name="modelPrefab"/> in the first frame of the idle animation and measures
        /// the posed meshes. The soles of the idle pose differ from the T-pose's, so the T-pose bounds would leave the
        /// model floating or sunk. The copy lives in a preview scene, so the open scenes stay untouched.
        /// </summary>
        public static IdlePose MeasureIdlePose(GameObject modelPrefab, AnimatorController idle)
        {
            AnimationClip clip = IdleClipOf(idle);
            Scene scene = EditorSceneManager.NewPreviewScene();
            var baked = new Mesh();
            bool startedAnimationMode = false;
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, scene);
                model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = model.GetComponent<Animator>();
                Transform upperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                Quaternion tPose = upperArm.rotation;
                bool IsPosed() => Quaternion.Angle(tPose, upperArm.rotation) >= MinIdleArmAngle;

                // Nothing renders in a preview scene, so a culling Animator would leave the bones in T-pose.
                animator.runtimeAnimatorController = idle;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                animator.Rebind();
                animator.Update(0f);
                if (!IsPosed())
                {
                    // The Animation window's way of posing objects in edit mode. Reverted when the mode stops.
                    if (!AnimationMode.InAnimationMode())
                    {
                        AnimationMode.StartAnimationMode();
                        startedAnimationMode = true;
                    }
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(model, clip, 0f);
                    AnimationMode.EndSampling();
                }
                if (!IsPosed())
                {
                    throw new InvalidOperationException(
                        $"Neither the Animator nor AnimationMode put '{modelPrefab.name}' in the pose of {clip.name} " +
                        $"(left upper arm {Quaternion.Angle(tPose, upperArm.rotation):F1}° from the T-pose); it would be measured in T-pose.");
                }

                float footY = float.MaxValue;
                float topY = float.MinValue;
                Vector3? mouth = null;
                var vertexBounds = new Dictionary<string, Bounds>();
                var boundsSpaceToModel = new Dictionary<string, Matrix4x4>();
                SkinnedMeshRenderer face = FindMouthRenderer(model);
                foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.BakeMesh(baked, true);
                    // With useScale the baked vertices carry the renderer's scale, so only its position and rotation are left.
                    Matrix4x4 toModel = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);
                    Transform boundsSpace = BoundsSpace(renderer);
                    Vector3[] vertices = baked.vertices;
                    Bounds local = default;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = toModel.MultiplyPoint3x4(vertices[i]);
                        footY = Mathf.Min(footY, vertices[i].y);
                        topY = Mathf.Max(topY, vertices[i].y);
                        Vector3 inBoundsSpace = boundsSpace.InverseTransformPoint(vertices[i]);
                        if (i == 0)
                        {
                            local = new Bounds(inBoundsSpace, Vector3.zero);
                        }
                        else
                        {
                            local.Encapsulate(inBoundsSpace);
                        }
                    }
                    string path = AnimationUtility.CalculateTransformPath(renderer.transform, model.transform);
                    vertexBounds[path] = local;
                    boundsSpaceToModel[path] = boundsSpace.localToWorldMatrix;
                    if (renderer == face)
                    {
                        mouth = MovedVertexCentre(renderer.sharedMesh, MouthShapePrefix + Vowels[0], vertices);
                    }
                }

                Vector3 head = animator.GetBoneTransform(HumanBodyBones.Head).position;
                return new IdlePose(footY, topY, mouth.Value, head, vertexBounds, boundsSpaceToModel);
            }
            finally
            {
                if (startedAnimationMode)
                {
                    AnimationMode.StopAnimationMode();
                }
                Object.DestroyImmediate(baked);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>
        /// Prints the bounds saved in the model prefab, both as stored (in the bounds space, which the idle pose and the
        /// lift move with the model) and as the height of their centre above the variant's floor.
        /// </summary>
        static void LogBounds(ScholarAvatar scholar, GameObject model, IdlePose pose)
        {
            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(renderer.transform, model.transform);
                Bounds bounds = renderer.localBounds;
                float floorHeight = pose.BoundsSpaceToModel[path].MultiplyPoint3x4(bounds.center).y + pose.Lift.y;
                Debug.Log($"[DNPDemo] {scholar.Name}/{path}: localBounds centre {bounds.center:F3}, extents {bounds.extents:F3} " +
                    $"in '{BoundsSpace(renderer).name}'; centre {floorHeight:F3} m above the floor in the idle pose.");
            }
        }

        /// <summary>The transform <see cref="SkinnedMeshRenderer.localBounds"/> is expressed in.</summary>
        public static Transform BoundsSpace(SkinnedMeshRenderer renderer) =>
            renderer.rootBone != null ? renderer.rootBone : renderer.transform;

        static AnimationClip IdleClipOf(AnimatorController idle) =>
            idle.layers[0].stateMachine.defaultState.motion as AnimationClip
            ?? throw new InvalidOperationException($"{AssetDatabase.GetAssetPath(idle)} has no idle clip in its default state.");

        static GameObject LoadImportedModel(ScholarAvatar scholar) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(scholar.ModelPrefabPath)
            ?? throw new InvalidOperationException($"{scholar.ModelPrefabPath} is missing. Run DNP Demo/Create/Scholar Avatars/1. Import VRM Models first.");

        static Vector3 MovedVertexCentre(Mesh mesh, string blendShape, Vector3[] posedVertices)
        {
            int shape = mesh.GetBlendShapeIndex(blendShape);
            var deltas = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(shape, mesh.GetBlendShapeFrameCount(shape) - 1, deltas, deltaNormals, deltaTangents);
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < deltas.Length; i++)
            {
                if (deltas[i].sqrMagnitude > 1e-10f)
                {
                    sum += posedVertices[i];
                    count++;
                }
            }
            if (count == 0)
            {
                throw new InvalidOperationException($"The blend shape {blendShape} on {mesh.name} moves no vertex.");
            }
            return sum / count;
        }

        static AnimatorController CreateOrUpdateIdleController()
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(IdleClipSourcePath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"No idle clip in {IdleClipSourcePath}.");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleControllerPath);
            if (controller == null)
            {
                return AnimatorController.CreateAnimatorControllerAtPathWithClip(IdleControllerPath, clip);
            }
            AnimatorState state = controller.layers[0].stateMachine.defaultState;
            if (state.motion != clip)
            {
                state.motion = clip;
                EditorUtility.SetDirty(controller);
            }
            return controller;
        }

        static GameObject PrepareModel(ScholarAvatar scholar, RuntimeAnimatorController controller, IdlePose pose)
        {
            LoadImportedModel(scholar);
            GameObject root = PrefabUtility.LoadPrefabContents(scholar.ModelPrefabPath);
            try
            {
                StripVrmComponents(root);
                if (!root.TryGetComponent(out Animator animator) || animator.avatar == null || !animator.avatar.isHuman)
                {
                    throw new InvalidOperationException($"{scholar.ModelPrefabPath} needs an Animator with a humanoid Avatar on its root.");
                }
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                FitBoundsToIdlePose(scholar, root, pose);
                PrefabUtility.SaveAsPrefabAsset(root, scholar.ModelPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(scholar.ModelPrefabPath);
        }

        /// <summary>
        /// The bounds UniVRM writes are not where the idle pose puts the meshes, so Unity culled the face and the hair when
        /// they filled the view. Fits each renderer's bounds to its idle vertices instead of turning on
        /// <see cref="SkinnedMeshRenderer.updateWhenOffscreen"/>, which would skin every mesh every frame on the Quest.
        /// </summary>
        static void FitBoundsToIdlePose(ScholarAvatar scholar, GameObject root, IdlePose pose)
        {
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(renderer.transform, root.transform);
                if (!pose.VertexBounds.TryGetValue(path, out Bounds bounds))
                {
                    throw new InvalidOperationException($"{scholar.Name}: '{path}' was not measured in the idle pose.");
                }
                bounds.Expand(2f * BoundsMargin);
                renderer.updateWhenOffscreen = false;
                renderer.localBounds = bounds;
            }
        }

        /// <summary>
        /// Removes the VRM components. Nothing else writes to the face mesh then, so uLipSync alone drives the mouth, and
        /// the scene does not run UniVRM at all.
        /// </summary>
        static void StripVrmComponents(GameObject root)
        {
            var hosts = new HashSet<GameObject>();
            List<Component> pending = root.GetComponentsInChildren<Component>(true).Where(IsVrmComponent).ToList();
            while (pending.Count > 0)
            {
                // Unity keeps a component another one requires, so remove the ones nothing requires first.
                List<Component> removable = pending.Where(c => !IsRequiredByOthers(c)).ToList();
                if (removable.Count == 0)
                {
                    throw new InvalidOperationException($"Cannot remove {string.Join(", ", pending.Select(c => c.GetType().Name))} from '{root.name}'.");
                }
                foreach (Component component in removable)
                {
                    hosts.Add(component.gameObject);
                    pending.Remove(component);
                    Object.DestroyImmediate(component);
                }
            }
            // Objects that only carried VRM settings, such as VRoid's "secondary" with the spring bones.
            foreach (GameObject host in hosts)
            {
                if (host != root && host.transform.childCount == 0 && host.GetComponents<Component>().Length == 1)
                {
                    Object.DestroyImmediate(host);
                }
            }
        }

        static bool IsRequiredByOthers(Component target)
        {
            foreach (Component other in target.GetComponents<Component>())
            {
                if (other == null || other == target)
                {
                    continue;
                }
                foreach (RequireComponent require in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                {
                    if (IsOfType(require.m_Type0, target) || IsOfType(require.m_Type1, target) || IsOfType(require.m_Type2, target))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static bool IsOfType(Type type, Component component) => type != null && type.IsInstanceOfType(component);

        static void BuildVariant(ScholarAvatar scholar, GameObject model, IdlePose pose)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(scholar.VariantPath);
            try
            {
                GameObject avatar = PlaceModel(root, model, pose);

                var binding = root.GetComponent<ScenarioSpeakerBinding>();
                if (binding == null || binding.Voice == null || binding.Subtitle == null)
                {
                    throw new InvalidOperationException($"{scholar.VariantPath} needs a ScenarioSpeakerBinding with its voice and subtitle set.");
                }
                binding.Voice.transform.position = root.transform.TransformPoint(pose.VoicePosition);
                binding.Subtitle.transform.parent.position = root.transform.TransformPoint(pose.SubtitleAnchorPosition);

                ConnectLipSync(scholar, root, FindMouthRenderer(avatar));
                LogReverted(scholar.VariantPath, RevertBoundsOverrides(root));

                PrefabUtility.SaveAsPrefabAsset(root, scholar.VariantPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Keeps the model instance if it is already there, so the variant's references into it keep their ids.</summary>
        static GameObject PlaceModel(GameObject root, GameObject model, IdlePose pose)
        {
            GameObject avatar = null;
            foreach (Transform child in root.transform.Cast<Transform>().ToList())
            {
                if (child.name != AvatarObjectName)
                {
                    continue;
                }
                if (avatar == null && PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == model)
                {
                    avatar = child.gameObject;
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
            if (avatar == null)
            {
                avatar = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                avatar.name = AvatarObjectName;
            }
            avatar.transform.SetLocalPositionAndRotation(pose.Lift, Quaternion.identity);
            avatar.transform.localScale = Vector3.one;
            avatar.transform.SetAsFirstSibling();
            return avatar;
        }

        static void ConnectLipSync(ScholarAvatar scholar, GameObject root, SkinnedMeshRenderer face)
        {
            var lipSync = root.GetComponent<uLipSync.uLipSync>();
            if (lipSync == null || lipSync.profile == null)
            {
                throw new InvalidOperationException($"{scholar.VariantPath} needs a uLipSync with a profile on its root.");
            }
            string[] missing = Vowels.Where(v => lipSync.profile.mfccs.All(m => m.name != v)).ToArray();
            if (missing.Length > 0)
            {
                Debug.LogWarning($"[DNPDemo] {scholar.Name}: the uLipSync profile {lipSync.profile.name} has no {string.Join(", ", missing)}; the mouth never makes those vowels.", lipSync.profile);
            }

            if (!root.TryGetComponent(out uLipSync.uLipSyncBlendShape mouth))
            {
                mouth = root.AddComponent<uLipSync.uLipSyncBlendShape>();
            }
            mouth.skinnedMeshRenderer = face;
            mouth.blendShapes = Vowels
                .Select(v => new uLipSync.uLipSyncBlendShape.BlendShapeInfo
                {
                    phoneme = v,
                    index = face.sharedMesh.GetBlendShapeIndex(MouthShapePrefix + v),
                })
                .ToList();

            for (int i = lipSync.onLipSyncUpdate.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(lipSync.onLipSyncUpdate, i);
            }
            UnityEventTools.AddPersistentListener(lipSync.onLipSyncUpdate, mouth.OnLipSyncUpdate);
        }

        /// <summary>
        /// The renderer properties the model prefab owns. An override of one on an instance hides the bounds
        /// <see cref="FitBoundsToIdlePose"/> writes, and the face or hair disappears again when the camera comes close.
        /// </summary>
        public static readonly string[] RendererBoundsProperties = { "m_AABB", "m_UpdateWhenOffscreen" };

        /// <summary>"renderer path: property" for every override of <see cref="RendererBoundsProperties"/> under <paramref name="root"/>.</summary>
        public static List<string> FindBoundsOverrides(GameObject root)
        {
            var found = new List<string>();
            foreach (SerializedProperty property in BoundsOverrideProperties(root))
            {
                var renderer = (Component)property.serializedObject.targetObject;
                found.Add($"{AnimationUtility.CalculateTransformPath(renderer.transform, root.transform)}: {property.propertyPath}");
            }
            return found;
        }

        static int RevertBoundsOverrides(GameObject root)
        {
            int count = 0;
            foreach (SerializedProperty property in BoundsOverrideProperties(root).ToList())
            {
                PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
                count++;
            }
            return count;
        }

        static IEnumerable<SerializedProperty> BoundsOverrideProperties(GameObject root)
        {
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(renderer))
                {
                    continue;
                }
                var serialized = new SerializedObject(renderer);
                foreach (string name in RendererBoundsProperties)
                {
                    SerializedProperty property = serialized.FindProperty(name);
                    if (property != null && property.prefabOverride)
                    {
                        yield return property;
                    }
                }
            }
        }

        static void RevertBoundsOverridesInScholarsPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ScholarsPrefabPath);
            try
            {
                int reverted = RevertBoundsOverrides(root);
                if (reverted > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, ScholarsPrefabPath);
                }
                LogReverted(ScholarsPrefabPath, reverted);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void LogReverted(string path, int count)
        {
            if (count > 0)
            {
                Debug.Log($"[DNPDemo] Reverted {count} bounds override(s) of the scholar meshes in {path}.");
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
