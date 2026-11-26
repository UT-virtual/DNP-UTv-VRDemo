using System.Collections.Generic;
using System.Linq;
using DNPDemo.Editor;
using DNPDemo.Scenario;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DNPDemo.Editor.ScholarAvatarBuilder;

namespace DNPDemo.Tests
{
    /// <summary>
    /// The scholar variants show their VRoid model, stand on the floor in the idle pose, voice and subtitle at the
    /// model's mouth and head, and move its mouth with uLipSync. Rebuilt with DNP Demo/Create/Scholar Avatars.
    /// </summary>
    public class ScholarAvatarPrefabTests
    {
        const float PositionTolerance = 0.01f;

        static ScholarAvatar[] AllScholars => ScholarAvatarBuilder.Scholars;

        [TestCaseSource(nameof(AllScholars))]
        public void ModelIsAnIdlingHumanoidWithoutVrmComponents(ScholarAvatar scholar)
        {
            GameObject model = LoadModel(scholar);

            Assert.IsTrue(model.TryGetComponent(out Animator animator), $"{scholar.ModelPrefabPath} has no Animator on its root.");
            Assert.IsNotNull(animator.avatar, $"{scholar.Name}: the Animator has no Avatar.");
            Assert.IsTrue(animator.avatar.isHuman, $"{scholar.Name}: the Animator's Avatar is not humanoid.");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleControllerPath), animator.runtimeAnimatorController,
                $"{scholar.Name} must play {IdleControllerPath}, or it stands in T-pose.");
            Assert.IsFalse(animator.applyRootMotion, $"{scholar.Name}: root motion would walk the model off its spot.");

            foreach (Component component in model.GetComponentsInChildren<Component>(true))
            {
                Assert.IsNotNull(component, $"{scholar.ModelPrefabPath} has a missing script.");
                Assert.IsFalse(IsVrmComponent(component),
                    $"{scholar.Name}: {component.GetType().Name} on '{component.name}' is left over from the VRM import.");
            }
        }

        [TestCaseSource(nameof(AllScholars))]
        public void VariantShowsOnlyTheModel(ScholarAvatar scholar)
        {
            GameObject model = LoadModel(scholar);
            GameObject variant = LoadVariant(scholar);

            Animator[] animators = variant.GetComponentsInChildren<Animator>(true);
            Assert.AreEqual(1, animators.Length, $"{scholar.VariantPath} must hold exactly one model.");
            Assert.AreEqual(model, PrefabUtility.GetCorrespondingObjectFromOriginalSource(animators[0].gameObject),
                $"{scholar.VariantPath} shows another model than {scholar.ModelPrefabPath}.");
            Assert.IsEmpty(variant.GetComponentsInChildren<MeshFilter>(true), $"{scholar.VariantPath} still has a placeholder mesh.");
        }

        [TestCaseSource(nameof(AllScholars))]
        public void ModelStandsOnTheFloorWithVoiceAndSubtitleAtItsHead(ScholarAvatar scholar)
        {
            var idle = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleControllerPath);
            Assert.IsNotNull(idle, IdleControllerPath);
            IdlePose pose = MeasureIdlePose(LoadModel(scholar), idle);
            GameObject variant = LoadVariant(scholar);
            var binding = variant.GetComponent<ScenarioSpeakerBinding>();
            Assert.IsNotNull(binding, $"{scholar.VariantPath} has no ScenarioSpeakerBinding.");

            AssertNear(pose.Lift, variant.GetComponentInChildren<Animator>(true).transform.localPosition, $"{scholar.Name}: the soles are off the floor");
            AssertNear(pose.VoicePosition, variant.transform.InverseTransformPoint(binding.Voice.transform.position), $"{scholar.Name}: the voice is not at the mouth");
            AssertNear(pose.SubtitleAnchorPosition, variant.transform.InverseTransformPoint(binding.Subtitle.transform.parent.position),
                $"{scholar.Name}: the subtitle is not above the head");
        }

        [TestCaseSource(nameof(AllScholars))]
        public void BoundsHoldTheIdlePose(ScholarAvatar scholar)
        {
            GameObject model = LoadModel(scholar);
            var idle = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleControllerPath);
            Assert.IsNotNull(idle, IdleControllerPath);
            IdlePose pose = MeasureIdlePose(model, idle);

            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.IsNotEmpty(renderers, $"{scholar.ModelPrefabPath} has no skinned mesh.");
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                string path = AnimationUtility.CalculateTransformPath(renderer.transform, model.transform);
                Assert.IsFalse(renderer.updateWhenOffscreen, $"{scholar.Name}: '{path}' skins every frame; fit its bounds instead.");
                Assert.IsTrue(pose.VertexBounds.TryGetValue(path, out Bounds posed), $"{scholar.Name}: '{path}' was not measured.");
                Bounds bounds = renderer.localBounds;
                Assert.IsTrue(Contains(bounds, posed.min) && Contains(bounds, posed.max),
                    $"{scholar.Name}: the bounds of '{path}' ({bounds}) do not hold its idle vertices ({posed}), so Unity culls it when it fills the view.");
            }
        }

        [TestCaseSource(nameof(AllScholars))]
        public void LipSyncDrivesTheMouthBlendShapes(ScholarAvatar scholar)
        {
            GameObject variant = LoadVariant(scholar);
            var binding = variant.GetComponent<ScenarioSpeakerBinding>();

            var lipSync = variant.GetComponent<uLipSync.uLipSync>();
            Assert.IsNotNull(lipSync, $"{scholar.VariantPath} has no uLipSync.");
            Assert.IsNotNull(lipSync.profile, $"{scholar.Name}: the uLipSync has no profile.");
            foreach (string vowel in Vowels)
            {
                Assert.IsTrue(lipSync.profile.mfccs.Any(m => m.name == vowel && m.mfccCalibrationDataList.Count > 0),
                    $"{scholar.Name}: the profile {lipSync.profile.name} has no calibrated '{vowel}'.");
            }
            Assert.IsNotNull(lipSync.audioSourceProxy, $"{scholar.Name}: the uLipSync does not listen to any source.");
            Assert.AreEqual(binding.Voice.gameObject, lipSync.audioSourceProxy.gameObject, $"{scholar.Name}: the uLipSync listens to another source than the voice.");

            var mouth = variant.GetComponent<uLipSync.uLipSyncBlendShape>();
            Assert.IsNotNull(mouth, $"{scholar.VariantPath} has no uLipSyncBlendShape.");
            Assert.IsNotNull(mouth.skinnedMeshRenderer, $"{scholar.Name}: the uLipSyncBlendShape has no mesh.");
            Assert.IsTrue(mouth.skinnedMeshRenderer.transform.IsChildOf(variant.transform), $"{scholar.Name}: the mouth mesh is outside the prefab.");
            Mesh face = mouth.skinnedMeshRenderer.sharedMesh;
            foreach (string vowel in Vowels)
            {
                var shape = mouth.GetBlendShapeInfo(vowel);
                Assert.IsNotNull(shape, $"{scholar.Name}: '{vowel}' moves no blend shape.");
                Assert.That(shape.index, Is.InRange(0, face.blendShapeCount - 1), $"{scholar.Name}: '{vowel}' points at no blend shape on {face.name}.");
                Assert.AreEqual(MouthShapePrefix + vowel, face.GetBlendShapeName(shape.index), $"{scholar.Name}: '{vowel}' moves the wrong blend shape.");
            }

            Assert.AreEqual(1, lipSync.onLipSyncUpdate.GetPersistentEventCount(), $"{scholar.Name}: onLipSyncUpdate must call only the mouth.");
            Assert.AreEqual(mouth, lipSync.onLipSyncUpdate.GetPersistentTarget(0));
            Assert.AreEqual(nameof(uLipSync.uLipSyncBlendShape.OnLipSyncUpdate), lipSync.onLipSyncUpdate.GetPersistentMethodName(0));
        }

        [TestCaseSource(nameof(AllScholars))]
        public void VariantKeepsTheModelBounds(ScholarAvatar scholar)
        {
            List<string> overrides = FindBoundsOverrides(LoadVariant(scholar));
            Assert.IsEmpty(overrides, $"{scholar.VariantPath} overrides the bounds of the model (run DNP Demo/Create/Scholar Avatars/2. Build Scholar Prefabs): {string.Join(", ", overrides)}");
        }

        [Test]
        public void ScholarsPrefabKeepsTheModelBounds()
        {
            var group = AssetDatabase.LoadAssetAtPath<GameObject>(ScholarsPrefabPath);
            Assert.IsNotNull(group, ScholarsPrefabPath);
            List<string> overrides = FindBoundsOverrides(group);
            Assert.IsEmpty(overrides, $"{ScholarsPrefabPath} overrides the bounds of the scholar models: {string.Join(", ", overrides)}");
        }

        [Test]
        public void MainSceneKeepsTheModelBounds()
        {
            Scene scene = SceneManager.GetSceneByPath(DNPDemoProjectSetup.MainScenePath);
            bool openedByTest = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(DNPDemoProjectSetup.MainScenePath, OpenSceneMode.Additive);
                openedByTest = true;
            }
            try
            {
                var overrides = new List<string>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (ScenarioSpeakerBinding speaker in root.GetComponentsInChildren<ScenarioSpeakerBinding>(true))
                    {
                        overrides.AddRange(FindBoundsOverrides(speaker.gameObject).Select(o => $"{speaker.name}/{o}"));
                    }
                }
                Assert.IsEmpty(overrides,
                    $"{DNPDemoProjectSetup.MainScenePath} overrides the bounds of the scholar models; revert them in the Inspector: {string.Join(", ", overrides)}");
            }
            finally
            {
                if (openedByTest)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void EachScholarInTheGroupHasOneLipSync()
        {
            var group = AssetDatabase.LoadAssetAtPath<GameObject>(ScholarsPrefabPath);
            Assert.IsNotNull(group, ScholarsPrefabPath);
            ScenarioSpeakerBinding[] speakers = group.GetComponentsInChildren<ScenarioSpeakerBinding>(true);
            Assert.AreEqual(AllScholars.Length, speakers.Length, $"{ScholarsPrefabPath} must hold every scholar.");
            foreach (ScenarioSpeakerBinding speaker in speakers)
            {
                Assert.AreEqual(1, speaker.GetComponents<uLipSync.uLipSync>().Length, $"'{speaker.name}' must analyse its voice once.");
            }
        }

        static GameObject LoadModel(ScholarAvatar scholar)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(scholar.ModelPrefabPath);
            Assert.IsNotNull(model, $"{scholar.ModelPrefabPath} (import it with DNP Demo/Create/Scholar Avatars)");
            return model;
        }

        static GameObject LoadVariant(ScholarAvatar scholar)
        {
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(scholar.VariantPath);
            Assert.IsNotNull(variant, scholar.VariantPath);
            return variant;
        }

        static bool Contains(Bounds bounds, Vector3 point)
        {
            const float epsilon = 1e-4f;
            Vector3 min = bounds.min - Vector3.one * epsilon;
            Vector3 max = bounds.max + Vector3.one * epsilon;
            return point.x >= min.x && point.y >= min.y && point.z >= min.z && point.x <= max.x && point.y <= max.y && point.z <= max.z;
        }

        static void AssertNear(Vector3 expected, Vector3 actual, string message) =>
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(PositionTolerance), $"{message}: expected {expected}, was {actual}.");
    }
}
