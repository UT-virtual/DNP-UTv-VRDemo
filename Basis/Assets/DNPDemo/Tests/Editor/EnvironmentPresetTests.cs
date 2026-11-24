using System.Collections;
using DNPDemo.Editor;
using DNPDemo.Environment;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DNPDemo.Tests
{
    public class EnvironmentPresetTests
    {
        Scene _scene;
        Scene _previousActiveScene;
        bool _sceneOpenedAdditively;
        EnvironmentPreset _exhibition;
        EnvironmentPreset _temple;
        Material _wallMaterial;
        readonly System.Collections.Generic.List<EnvironmentFixture> _fixtures = new();

        [SetUp]
        public void SetUp()
        {
            EnvironmentPresetBuilder.CreatePresetsIfMissing();
            _exhibition = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.ExhibitionPresetPath);
            _temple = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.TemplePresetPath);
            _previousActiveScene = SceneManager.GetActiveScene();
            // RenderSettings live on the active scene, so each test needs its own. The Test Runner runs Edit Mode tests
            // in an untitled scratch scene, and NewScene(Additive) throws once that scene is dirty; replace it instead,
            // following the rule of the Test Framework's CreateNewSceneTask. The runner restores the user's scenes afterwards.
            _sceneOpenedAdditively = !(SceneManager.sceneCount == 1 && string.IsNullOrEmpty(_previousActiveScene.path));
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                _sceneOpenedAdditively ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(_scene);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (EnvironmentFixture fixture in _fixtures)
            {
                fixture.Dispose();
            }
            _fixtures.Clear();

            if (_sceneOpenedAdditively && _scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }
            if (_wallMaterial != null)
            {
                Object.DestroyImmediate(_wallMaterial);
            }
            if (_previousActiveScene.IsValid() && _previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActiveScene);
            }
        }

        [Test]
        public void PresetAssetsHaveCompletePanoramasAndTempleSunlight()
        {
            Assert.IsNotNull(_exhibition);
            Assert.IsNotNull(_temple);
            Assert.IsNotNull(_exhibition.Panorama);
            Assert.IsNotNull(_temple.Panorama);
            Assert.AreEqual(2f, (float)_exhibition.Panorama.width / _exhibition.Panorama.height, 0.01f);
            Assert.AreEqual(2f, (float)_temple.Panorama.width / _temple.Panorama.height, 0.01f);
            Assert.IsTrue(_exhibition.ExhibitionRoomVisible);
            Assert.IsFalse(_temple.ExhibitionRoomVisible);
            Assert.Greater(_temple.DirectionalLight.Intensity, _exhibition.DirectionalLight.Intensity);
            Assert.AreEqual(LightShadows.Soft, _temple.DirectionalLight.Shadows);
            Assert.Greater(_temple.DirectionalLight.ShadowStrength, 0f);
            Assert.AreEqual(DefaultReflectionMode.Skybox, _temple.Reflection.Mode);
            Assert.AreEqual(AmbientMode.Trilight, _temple.Ambient.Mode);
            AssertPresetComplete(_exhibition);
            AssertPresetComplete(_temple);
        }

        [Test]
        public void EnvironmentCueAppliesInstantlyAndResetRestoresExhibition()
        {
            EnvironmentFixture fixture = CreateFixture();
            var scenario = fixture.Track(ScriptableObject.CreateInstance<ScenarioData>());
            var context = new ScenarioCueContext(fixture.Stage, scenario);
            var cue = new EnvironmentCue(_temple);

            cue.ApplyInstant(context);

            Assert.AreEqual(_temple.DirectionalLight.Intensity, fixture.Directional.intensity);
            Assert.IsFalse(fixture.Spot.gameObject.activeSelf);
            Assert.AreSame(_temple.Panorama, RenderSettings.skybox.GetTexture("_PanoramaA"));
            Assert.AreEqual(0f, fixture.WallMaterial().GetColor("_BaseColor").a, 0.001f);
            Assert.IsTrue(fixture.WallCollider.enabled, "Fading the display must not disable the navigation collider.");

            fixture.Stage.ResetToBaseline();

            Assert.AreEqual(_exhibition.DirectionalLight.Intensity, fixture.Directional.intensity);
            Assert.IsFalse(fixture.Spot.gameObject.activeSelf, "The exhibition keeps the spot light as the temple left it.");
            Assert.AreSame(_exhibition.Panorama, RenderSettings.skybox.GetTexture("_PanoramaA"));
            Assert.AreEqual(1f, fixture.WallMaterial().GetColor("_BaseColor").a, 0.001f);
        }

        [Test]
        public void PlayingCueFadesAndAnInterruptedTransitionConvergesFromItsCurrentState()
        {
            EnvironmentFixture fixture = CreateFixture();
            var context = new ScenarioCueContext(fixture.Stage, fixture.Track(ScriptableObject.CreateInstance<ScenarioData>()));
            var cue = new EnvironmentCue(_temple, 2f);
            IEnumerator playback = cue.Play(context);

            Assert.IsTrue(playback.MoveNext());
            Assert.IsTrue(fixture.Controller.IsTransitioning);
            fixture.Controller.AdvanceTransition(1f);
            float halfwayIntensity = fixture.Directional.intensity;
            Assert.Greater(halfwayIntensity, _exhibition.DirectionalLight.Intensity);
            Assert.Less(halfwayIntensity, _temple.DirectionalLight.Intensity);
            Assert.AreEqual(0.5f, fixture.WallMaterial().GetColor("_BaseColor").a, 0.02f);

            Texture currentSky = RenderSettings.skybox.GetTexture("_PanoramaA");
            fixture.Controller.TransitionTo(_exhibition, 2f);
            Assert.IsInstanceOf<RenderTexture>(RenderSettings.skybox.GetTexture("_PanoramaA"));
            Assert.AreNotSame(currentSky, RenderSettings.skybox.GetTexture("_PanoramaA"),
                "A retarget takes a fresh snapshot of the in-flight panorama blend.");
            Assert.AreEqual(halfwayIntensity, fixture.Directional.intensity, 0.001f,
                "A retarget begins from the state currently on screen.");
            fixture.Controller.AdvanceTransition(2f);

            Assert.IsFalse(fixture.Controller.IsTransitioning);
            Assert.AreEqual(_exhibition.DirectionalLight.Intensity, fixture.Directional.intensity);
            Assert.AreSame(_exhibition.Panorama, RenderSettings.skybox.GetTexture("_PanoramaA"));
            Assert.AreEqual(1f, fixture.WallMaterial().GetColor("_BaseColor").a, 0.001f);

            fixture.Controller.TransitionTo(_temple, 2f);
            fixture.Controller.AdvanceTransition(0.5f);
            cue.ApplyInstant(context);
            Assert.IsFalse(fixture.Controller.IsTransitioning);
            Assert.AreEqual(_temple.DirectionalLight.Intensity, fixture.Directional.intensity);
            Assert.AreSame(_temple.Panorama, RenderSettings.skybox.GetTexture("_PanoramaA"));
        }

        [Test]
        public void TargetSkyUsesItsOwnExposureAndRotationFromTheFirstFrame()
        {
            EnvironmentFixture fixture = CreateFixture();
            EnvironmentPreset rotated = CreateVariant(fixture, _temple, 90f, _temple.SpotLight);

            fixture.Controller.TransitionTo(rotated, 2f);
            fixture.Controller.AdvanceTransition(0.1f);

            Material sky = RenderSettings.skybox;
            Assert.AreEqual(rotated.Exposure, sky.GetFloat("_ExposureB"), 0.0001f);
            Assert.AreEqual(90f, sky.GetFloat("_RotationB"), 0.0001f, "The target panorama must not spin while it fades in.");
            Assert.AreEqual(0f, sky.GetFloat("_ExposureA"), 0.0001f, "The snapshot already contains the outgoing exposure.");
            Assert.AreEqual(0f, sky.GetFloat("_RotationA"), 0.0001f, "The snapshot already contains the outgoing rotation.");
        }

        [Test]
        public void SpotLightFadesFromDarkWhenEnabledAndToDarkWhenDisabled()
        {
            EnvironmentFixture fixture = CreateFixture();
            SpotLightSettings litSpot = _exhibition.SpotLight;
            litSpot.Enabled = SpotLightMode.On;
            litSpot.Intensity = 10f;
            EnvironmentPreset lit = CreateVariant(fixture, _exhibition, _exhibition.Rotation, litSpot);
            Assume.That(_exhibition.SpotLight.Enabled, Is.EqualTo(SpotLightMode.Keep));
            Assume.That(_exhibition.SpotLight.Intensity, Is.GreaterThan(litSpot.Intensity));
            Assume.That(_temple.SpotLight.Enabled, Is.EqualTo(SpotLightMode.Off));

            fixture.Controller.ApplyInstant(_exhibition);
            Assert.IsFalse(fixture.Spot.gameObject.activeSelf, "Keep must not turn on a spot light that is off.");
            fixture.Controller.TransitionTo(lit, 2f);
            fixture.Controller.AdvanceTransition(0.2f);
            Assert.IsTrue(fixture.Spot.gameObject.activeSelf);
            Assert.Less(fixture.Spot.intensity, 1f, "A disabled spot light counts as dark, so it must not start at its stored intensity.");
            fixture.Controller.AdvanceTransition(2f);
            Assert.AreEqual(litSpot.Intensity, fixture.Spot.intensity, 0.001f);

            fixture.Controller.TransitionTo(_exhibition, 2f);
            fixture.Controller.AdvanceTransition(2f);
            Assert.IsTrue(fixture.Spot.gameObject.activeSelf, "Keep must not turn off a spot light that is on.");
            Assert.AreEqual(_exhibition.SpotLight.Intensity, fixture.Spot.intensity, 0.001f);

            fixture.Controller.TransitionTo(_temple, 2f);
            fixture.Controller.AdvanceTransition(1f);
            Assert.IsTrue(fixture.Spot.gameObject.activeSelf, "The light stays active while it fades out.");
            Assert.Less(fixture.Spot.intensity, _exhibition.SpotLight.Intensity);
            fixture.Controller.AdvanceTransition(1f);
            Assert.IsFalse(fixture.Spot.gameObject.activeSelf);
        }

        EnvironmentFixture CreateFixture()
        {
            var fixture = new EnvironmentFixture();
            _fixtures.Add(fixture);
            var stageObject = fixture.Track(new GameObject("ScenarioStage"));
            fixture.Stage = stageObject.AddComponent<ScenarioStage>();

            var directionalObject = fixture.Track(new GameObject("Directional"));
            fixture.Directional = directionalObject.AddComponent<Light>();
            fixture.Directional.type = LightType.Directional;
            fixture.Directional.intensity = 0.15f;

            var spotObject = fixture.Track(new GameObject("Spot"));
            fixture.Spot = spotObject.AddComponent<Light>();
            fixture.Spot.type = LightType.Spot;
            spotObject.SetActive(false);

            var room = fixture.Track(new GameObject("ExhibitionRoom"));
            fixture.Room = room;
            SetBindingKey(room.AddComponent<ScenarioObjectBinding>(), "ExhibitionRoom");
            fixture.WallCollider = room.AddComponent<BoxCollider>();
            var wall = new GameObject("WallDisplay");
            wall.transform.SetParent(room.transform, false);
            var renderer = wall.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            var environment = fixture.Track(new GameObject("Environment"));
            SetBindingKey(environment.AddComponent<ScenarioObjectBinding>(), "Environment");
            fixture.Controller = environment.AddComponent<EnvironmentController>();
            var serialized = new SerializedObject(fixture.Controller);
            serialized.FindProperty("_baselinePreset").objectReferenceValue = _exhibition;
            serialized.FindProperty("_directionalLight").objectReferenceValue = fixture.Directional;
            serialized.FindProperty("_spotLight").objectReferenceValue = fixture.Spot;
            serialized.FindProperty("_crossfadeShader").objectReferenceValue = Shader.Find("DNPDemo/Environment Crossfade Panorama");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return fixture;
        }

        static void SetBindingKey(ScenarioObjectBinding binding, string key)
        {
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("_key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssertPresetComplete(EnvironmentPreset preset)
        {
            Assert.Greater(preset.DirectionalLight.Intensity, 0f, preset.name);
            Assert.Greater(preset.DirectionalLight.ShadowNearPlane, 0f, preset.name);
            Assert.Greater(preset.SpotLight.Range, 0f, preset.name);
            Assert.Greater(preset.SpotLight.SpotAngle, 0f, preset.name);
            Assert.Greater(preset.SpotLight.InnerSpotAngle, 0f, preset.name);
            Assert.Greater(preset.SpotLight.ShadowNearPlane, 0f, preset.name);
            Assert.Greater(preset.Ambient.Intensity, 0f, preset.name);
            Assert.GreaterOrEqual(preset.Reflection.Intensity, 0f, preset.name);
        }

        static EnvironmentPreset CreateVariant(EnvironmentFixture fixture, EnvironmentPreset source, float rotation, SpotLightSettings spot)
        {
            EnvironmentPreset preset = fixture.Track(ScriptableObject.CreateInstance<EnvironmentPreset>());
            preset.Configure(
                source.Panorama,
                source.Exposure,
                rotation,
                source.ExhibitionRoomVisible,
                source.DirectionalLight,
                spot,
                source.Ambient,
                source.Reflection);
            return preset;
        }

        sealed class EnvironmentFixture
        {
            readonly System.Collections.Generic.List<Object> _objects = new();

            public ScenarioStage Stage;
            public EnvironmentController Controller;
            public Light Directional;
            public Light Spot;
            public BoxCollider WallCollider;
            public GameObject Room;

            public T Track<T>(T target) where T : Object
            {
                _objects.Add(target);
                return target;
            }

            public Material WallMaterial()
            {
                Renderer renderer = Room.GetComponentInChildren<Renderer>(true);
                return renderer.sharedMaterial;
            }

            public void Dispose()
            {
                foreach (Object target in _objects)
                {
                    if (target != null)
                    {
                        Object.DestroyImmediate(target);
                    }
                }
                _objects.Clear();
            }
        }
    }
}
