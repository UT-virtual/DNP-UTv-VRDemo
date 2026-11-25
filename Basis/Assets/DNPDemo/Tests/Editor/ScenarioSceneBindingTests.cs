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
        public void SketchPlacedObjectsReadTheSketchAndCanReachTheirGoal()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data);
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            int checkedCues = 0;
            foreach (ScenarioStep step in data.Steps)
            {
                foreach (ScenarioCue cue in step.Cues)
                {
                    if (cue is not PlaceAtSketchCue place)
                    {
                        continue;
                    }
                    Assert.IsTrue(bindings.TryGetObject(place.StrokesKey, out UnityEngine.GameObject tool), $"'{place.StrokesKey}' is not bound.");
                    Assert.IsTrue(tool.TryGetComponent(out StrokeTool _), $"'{place.StrokesKey}' should be the StrokeTool that keeps the sketch.");

                    // The step that shows the object moves it to a goal; from anywhere it can appear, the goal must be in reach.
                    ManipulatePart move = step.Interaction.OfType<ManipulatePart>()
                        .FirstOrDefault(part => part.Constraint == ManipulateConstraintKind.Translate && part.TargetKey == place.Key);
                    Assert.IsNotNull(move, $"{step.Id} should move '{place.Key}'.");
                    Assert.IsTrue(bindings.TryGetObject(place.MarkerKey, out UnityEngine.GameObject marker), $"'{place.MarkerKey}' is not bound.");
                    Assert.IsTrue(bindings.TryGetObject(move.GoalKey, out UnityEngine.GameObject goal), $"'{move.GoalKey}' is not bound.");
                    float farthest = UnityEngine.Vector3.Distance(marker.transform.position, goal.transform.position) + place.MaxOffset;
                    Assert.LessOrEqual(farthest, move.MaxDistance - move.SnapDistance,
                        $"{step.Id}: '{place.Key}' can appear {farthest:0.##} m from '{move.GoalKey}', farther than it may move.");
                    checkedCues++;
                }
            }
            Assert.Greater(checkedCues, 0, "Step 14 should put the hand mirror where the sketch was drawn.");
        }

        [Test]
        public void LeftAndRightKeysOnTheStatueAreHerOwnSides()
        {
            ScenarioBindingSet bindings = ScenarioBindingSet.Collect(_scene);
            Assert.IsTrue(bindings.TryGetObject(VenusStatueKeys.Main, out UnityEngine.GameObject statue));
            // The statue faces -Z of its anchor, so her left is +X and her right is -X (docs/PLAN.md §2.5).
            UnityEngine.Transform anchor = statue.transform.parent;
            int left = 0;
            int right = 0;
            foreach (KeyValuePair<string, UnityEngine.GameObject> pair in bindings.Objects)
            {
                if (pair.Value == null || !pair.Value.transform.IsChildOf(anchor))
                {
                    continue;
                }
                // A curve sits at the origin and lies where its points are; other objects on the centre line (such as the
                // dashed line under the curve) are containers that tell no side.
                UnityEngine.Vector3 point = pair.Value.TryGetComponent(out GuideCurve curve) ? curve.Evaluate(0.5f) : pair.Value.transform.position;
                float x = anchor.InverseTransformPoint(point).x;
                if (UnityEngine.Mathf.Abs(x) < 0.01f)
                {
                    continue;
                }
                if (pair.Key.Contains("Left"))
                {
                    Assert.Greater(x, 0f, $"'{pair.Key}' should be on her left, the +X side.");
                    left++;
                }
                else if (pair.Key.Contains("Right"))
                {
                    Assert.Less(x, 0f, $"'{pair.Key}' should be on her right, the -X side.");
                    right++;
                }
            }
            Assert.Greater(left, 0, "The statue should have keys for her left arm.");
            Assert.Greater(right, 0, "The statue should have keys for her right arm.");
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
