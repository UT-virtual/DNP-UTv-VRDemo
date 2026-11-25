using System.Collections.Generic;
using System.Linq;
using DNPDemo.Editor;
using DNPDemo.Environment;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Interaction.Selection;
using DNPDemo.Interaction.Stroke;
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
        public void EnvironmentBindingHasAConfiguredController()
        {
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            Assert.IsTrue(bindings.TryGetObject(EnvironmentController.EnvironmentKey, out UnityEngine.GameObject target),
                $"No object has the key \"{EnvironmentController.EnvironmentKey}\" (Lighting prefab).");
            Assert.IsTrue(target.TryGetComponent(out EnvironmentController controller),
                $"The \"{EnvironmentController.EnvironmentKey}\" object has no EnvironmentController.");

            var serialized = new SerializedObject(controller);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.ExhibitionPresetPath),
                serialized.FindProperty("_baselinePreset").objectReferenceValue, "The baseline must be the exhibition preset.");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(EnvironmentPresetBuilder.CrossfadeShaderPath),
                serialized.FindProperty("_crossfadeShader").objectReferenceValue);

            var directional = serialized.FindProperty("_directionalLight").objectReferenceValue as UnityEngine.Light;
            Assert.IsNotNull(directional, "The directional light is not assigned.");
            Assert.AreEqual(UnityEngine.LightType.Directional, directional.type);

            var spot = serialized.FindProperty("_spotLight").objectReferenceValue as UnityEngine.Light;
            Assert.IsNotNull(spot, "The spot light is not assigned.");
            Assert.AreEqual(UnityEngine.LightType.Spot, spot.type);
            Assert.IsTrue(bindings.TryGetObject(ScenarioDataBuilder.SpotlightKey, out UnityEngine.GameObject statueSpot));
            Assert.AreSame(statueSpot, spot.gameObject, "The controller must drive the statue spotlight that the scenario turns on.");
        }

        [Test]
        public void ExhibitionRoomKeyResolvesToRenderedRoom()
        {
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            Assert.IsTrue(bindings.TryGetObject(EnvironmentController.ExhibitionRoomKey, out UnityEngine.GameObject room),
                $"No object has the key \"{EnvironmentController.ExhibitionRoomKey}\" (Env_ExhibitionRoom prefab). EnvironmentController fades it.");
            Assert.IsNotEmpty(room.GetComponentsInChildren<UnityEngine.Renderer>(true), "The exhibition room has nothing to fade.");
        }

        [Test]
        public void SceneCameraClearsToTheSkybox()
        {
            UnityEngine.Camera sceneCamera = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<UnityEngine.Camera>(true))
                .SingleOrDefault(camera => camera.name == "SceneCamera");
            Assert.IsNotNull(sceneCamera, "DNPDemo_Main has no SceneCamera.");
            Assert.AreEqual(UnityEngine.CameraClearFlags.Skybox, sceneCamera.clearFlags,
                "EnvironmentController shows each environment through RenderSettings.skybox, so a solid colour background would hide the panorama.");
        }

        [Test]
        public void TempleFloorKeyResolvesToTheHiddenTempleWithAShadowReceivingFloor()
        {
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            Assert.IsTrue(bindings.TryGetObject(ScenarioDataBuilder.TempleFloorKey, out UnityEngine.GameObject temple),
                $"No object has the key \"{ScenarioDataBuilder.TempleFloorKey}\" (Env_Temple prefab).");
            Assert.IsFalse(temple.activeSelf, "The temple stays hidden until the closing step shows it.");
            Assert.IsTrue(temple.transform.parent == null || temple.transform.parent.gameObject.activeInHierarchy,
                "Showing the bound object must be enough to make the floor visible.");

            UnityEngine.Transform floor = temple.transform.Find(EnvironmentPresetBuilder.TempleFloorName);
            Assert.IsNotNull(floor, "Env_Temple has no floor.");
            Assert.IsTrue(floor.gameObject.activeSelf);
            var renderer = floor.GetComponent<UnityEngine.MeshRenderer>();
            Assert.IsNotNull(renderer);
            Assert.IsTrue(renderer.receiveShadows, "The floor must receive the statue's shadow.");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(EnvironmentPresetBuilder.TempleFloorMaterialPath),
                renderer.sharedMaterial);

            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            ScenarioStep closing = data.Steps.Single(step => step.Id == "closing");
            int environment = closing.Cues.FindIndex(cue => cue is EnvironmentCue);
            Assert.GreaterOrEqual(environment, 0, "The closing step switches to the temple.");
            Assert.IsTrue(closing.Cues[environment + 1] is SetObjectActiveCue { Key: ScenarioDataBuilder.TempleFloorKey, Active: true },
                "The temple floor is shown right after the environment switches.");
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
        public void GuideSnapCurvesJoinTheZonesOfTheGuideBeforeThem()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data);
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            int checkedCurves = 0;
            for (int i = 1; i < data.Steps.Count; i++)
            {
                foreach (ScenarioCue cue in data.Steps[i].Cues)
                {
                    if (cue is not GuideSnapCue snap)
                    {
                        continue;
                    }
                    StrokePart guide = data.Steps[i - 1].Interaction.OfType<StrokePart>().FirstOrDefault(part => part.Goal == StrokeGoal.Guide);
                    Assert.IsNotNull(guide, $"{data.Steps[i].Id}: the arm snaps along a guide, so the step before it should draw one.");
                    Assert.IsTrue(bindings.TryGetObject(snap.Key, out UnityEngine.GameObject target), $"'{snap.Key}' is not bound.");
                    Assert.IsTrue(target.TryGetComponent(out GuideCurve curve), $"'{snap.Key}' has no GuideCurve.");
                    Assert.IsTrue(target.TryGetComponent(out GuideSnapEffect _), $"'{snap.Key}' has no GuideSnapEffect.");
                    Assert.IsTrue(bindings.TryGetObject(guide.StartZoneKey, out UnityEngine.GameObject start));
                    Assert.IsTrue(bindings.TryGetObject(guide.EndZoneKey, out UnityEngine.GameObject end));
                    Assert.IsTrue(DetectionVolumes.Find(start).ContainsPoint(curve.Evaluate(0f)), $"'{snap.Key}' should start in '{guide.StartZoneKey}'.");
                    Assert.IsTrue(DetectionVolumes.Find(end).ContainsPoint(curve.Evaluate(1f)), $"'{snap.Key}' should end in '{guide.EndZoneKey}'.");
                    checkedCurves++;
                }
            }
            Assert.Greater(checkedCurves, 0, "Step 12 should play the arm snapping along the guide of step 11.");
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
