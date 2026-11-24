using System.Collections.Generic;
using Basis.Scripts.BasisSdk;
using DNPDemo.Editor;
using DNPDemo.Environment;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Interaction.Selection;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using DNPDemo.Venus;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DNPDemo.Tests
{
    /// <summary>
    /// Checks that DNPDemo_Main provides every speaker and object key the scenario data refers to,
    /// and that no binding is declared twice.
    /// </summary>
    public class ScenarioSceneBindingTests
    {
        Scene _scene;
        bool _openedByTest;

        [OneTimeSetUp]
        public void OpenScene()
        {
            _scene = SceneManager.GetSceneByPath(DNPDemoProjectSetup.MainScenePath);
            if (!_scene.isLoaded)
            {
                _scene = EditorSceneManager.OpenScene(DNPDemoProjectSetup.MainScenePath, OpenSceneMode.Additive);
                _openedByTest = true;
            }
        }

        [OneTimeTearDown]
        public void CloseScene()
        {
            if (_openedByTest && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }
        }

        [Test]
        public void SceneBindingsHaveNoDuplicates()
        {
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            Assert.IsEmpty(bindings.Problems, string.Join("\n", bindings.Problems));
        }

        [Test]
        public void SceneProvidesEverythingTheScenarioUses()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data, $"{ScenarioDataBuilder.AssetPath} (create it from DNP Demo/Create/Scenario Data)");

            List<string> missing = data.CollectRequirements().FindMissing(ScenarioBindingSet.Collect(_scene));
            Assert.IsEmpty(missing, string.Join("\n", missing));
        }

        [Test]
        public void SceneHasOneScenarioStage()
        {
            int count = 0;
            foreach (UnityEngine.GameObject root in _scene.GetRootGameObjects())
            {
                count += root.GetComponentsInChildren<ScenarioStage>(true).Length;
            }
            Assert.AreEqual(1, count, "DNPDemo_Main must contain exactly one ScenarioStage (in the DemoSystems prefab).");
        }

        [Test]
        public void SceneCameraDrawsTheEnvironmentSkybox()
        {
            // Basis copies the clear flags of BasisScene.MainCamera to the local player's camera
            // (BasisSceneFactory.LoadCameraProperties); a solid colour would hide the environment panorama.
            BasisScene basisScene = null;
            foreach (UnityEngine.GameObject root in _scene.GetRootGameObjects())
            {
                basisScene = root.GetComponentInChildren<BasisScene>(true);
                if (basisScene != null)
                {
                    break;
                }
            }
            Assert.IsNotNull(basisScene, "DNPDemo_Main must contain a BasisScene.");
            Assert.IsNotNull(basisScene.MainCamera, "BasisScene.MainCamera is the camera Basis takes its settings from.");
            Assert.AreEqual(UnityEngine.CameraClearFlags.Skybox, basisScene.MainCamera.clearFlags);
        }

        [Test]
        public void EnvironmentBindingHasAConfiguredController()
        {
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            Assert.IsTrue(bindings.TryGetObject("Environment", out UnityEngine.GameObject target));
            Assert.IsTrue(target.TryGetComponent(out EnvironmentController controller));
            Assert.IsNotNull(controller);
        }

        [Test]
        public void SelectionZoneKeysResolveToDetectionVolumes()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data);
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            int checkedKeys = 0;
            foreach (ScenarioStep step in data.Steps)
            {
                foreach (InteractionPart part in step.Interaction)
                {
                    if (part is not SelectionPart selection)
                    {
                        continue;
                    }
                    foreach (string key in selection.TargetZoneKeys)
                    {
                        Assert.IsTrue(bindings.TryGetObject(key, out UnityEngine.GameObject target), $"{step.Id}: key '{key}' is not bound.");
                        Assert.IsNotNull(DetectionVolumes.Find(target), $"{step.Id}: '{key}' has neither a DetectionZoneGroup nor a DetectionZone.");
                        checkedKeys++;
                    }
                }
            }
            Assert.Greater(checkedKeys, 0, "The scenario should have a selection part to check.");
        }

        [Test]
        public void ManipulateGrabZonesResolveToDetectionVolumes()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data);
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            int checkedParts = 0;
            foreach (ScenarioStep step in data.Steps)
            {
                foreach (InteractionPart part in step.Interaction)
                {
                    if (part is not ManipulatePart manipulate)
                    {
                        continue;
                    }
                    string key = manipulate.EffectiveGrabZoneKey;
                    Assert.IsTrue(bindings.TryGetObject(key, out UnityEngine.GameObject target), $"{step.Id}: key '{key}' is not bound.");
                    Assert.IsNotNull(DetectionVolumes.Find(target), $"{step.Id}: '{key}' has neither a DetectionZoneGroup nor a DetectionZone to grab.");
                    checkedParts++;
                }
            }
            Assert.Greater(checkedParts, 0, "The scenario should have a manipulate part to check.");
        }

        [Test]
        public void StatueVersionCuesAddressStatuesThatPrebuildTheirVersions()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data);
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            var statueKeys = new HashSet<string>();
            foreach (ScenarioStep step in data.Steps)
            {
                foreach (ScenarioCue cue in step.Cues)
                {
                    if (cue is not StatueVersionCue versionCue)
                    {
                        continue;
                    }
                    Assert.IsTrue(bindings.TryGetObject(versionCue.StatueKey, out UnityEngine.GameObject target), $"{step.Id}: key '{versionCue.StatueKey}' is not bound.");
                    Assert.IsTrue(target.TryGetComponent(out VenusStatue statue), $"{step.Id}: '{versionCue.StatueKey}' has no VenusStatue.");
                    Assert.IsNotNull(versionCue.Version, $"{step.Id}: the cue for '{versionCue.StatueKey}' has no version.");
                    Assert.IsNotNull(statue.Catalog, $"'{versionCue.StatueKey}' has no catalog.");
                    Assert.IsTrue(statue.Baseline == null || statue.Catalog.Contains(statue.Baseline),
                        $"The baseline of '{versionCue.StatueKey}' is not in its catalog.");
                    Assert.IsTrue(statue.Catalog.Contains(versionCue.Version),
                        $"{step.Id}: {versionCue.Version.name} is not in the catalog of '{versionCue.StatueKey}', so it would be built during the demo.");
                    statueKeys.Add(versionCue.StatueKey);
                }
            }
            CollectionAssert.AreEquivalent(new[] { VenusStatueKeys.Main, VenusStatueKeys.Comparison }, statueKeys,
                "Both the main statue and the comparison table show versions.");
        }

        [Test]
        public void CollectingAnUnloadedSceneGivesAnEmptySet()
        {
            // A scene that is still loading (as in Awake) counts as not loaded, and has nothing to collect yet.
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(default);
            Assert.IsEmpty(bindings.Objects);
            Assert.IsEmpty(bindings.Speakers);
            Assert.IsEmpty(ScenarioBindingSet.Empty.Objects);
            Assert.IsEmpty(ScenarioBindingSet.Empty.Problems);
        }
    }
}
