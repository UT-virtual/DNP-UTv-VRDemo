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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DNPDemo.Tests
{
    public class EnvironmentPresetTests
    {
        /// <summary>The temple panorama is used as generated, 2912 x 1440 (2.022:1); the skybox maps any width to 360 degrees.</summary>
        const float PanoramaAspectTolerance = 0.025f;

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
            _exhibition = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.ExhibitionPresetPath);
            _temple = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.TemplePresetPath);
            Assert.IsNotNull(_exhibition, $"{EnvironmentPresetBuilder.ExhibitionPresetPath} がありません。");
            Assert.IsNotNull(_temple, $"{EnvironmentPresetBuilder.TemplePresetPath} がありません。");
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
            Assert.AreEqual(2f, (float)_exhibition.Panorama.width / _exhibition.Panorama.height, PanoramaAspectTolerance);
            Assert.AreEqual(2f, (float)_temple.Panorama.width / _temple.Panorama.height, PanoramaAspectTolerance);
            Assert.IsTrue(_exhibition.ExhibitionRoomVisible);
            Assert.IsFalse(_temple.ExhibitionRoomVisible);
            Assert.Greater(_temple.DirectionalLight.Intensity, _exhibition.DirectionalLight.Intensity);
            Assert.AreEqual(LightShadows.Hard, _temple.DirectionalLight.Shadows, "Quest's URP asset has no soft shadows.");
            Assert.Greater(_temple.DirectionalLight.ShadowStrength, 0f);
            Assert.AreEqual(DefaultReflectionMode.Skybox, _temple.Reflection.Mode);
            Assert.AreEqual(AmbientMode.Trilight, _temple.Ambient.Mode);
            AssertPresetComplete(_exhibition);
            AssertPresetComplete(_temple);
        }

        [Test]
        public void TemplePresetUsesThePanoramaJpegAndOverridesShadowQuality()
        {
            Assert.AreEqual(EnvironmentPresetBuilder.TemplePanoramaPath, AssetDatabase.GetAssetPath(_temple.Panorama));
            StringAssert.EndsWith("/Environment_Temple_Panorama.jpg", EnvironmentPresetBuilder.TemplePanoramaPath);
            Assert.AreEqual(2f, (float)_temple.Panorama.width / _temple.Panorama.height, PanoramaAspectTolerance,
                "The temple panorama is an equirectangular image.");

            ShadowQualitySettings temple = _temple.ShadowQuality;
            Assert.IsTrue(temple.Override);
            Assert.GreaterOrEqual(temple.MainLightShadowmapResolution, 1024);
            Assert.Greater(temple.ShadowDistance, 0f);
            Assert.That(temple.CascadeCount, Is.InRange(1, 4));
            Assert.IsFalse(_exhibition.ShadowQuality.Override, "The exhibition keeps Basis' shadow quality.");
        }

        [Test]
        public void SkyShaderHasASinglePassAndTheSnapshotIsSeparate()
        {
            // Unity draws every pass of the skybox material, so any extra pass would be drawn over the sky.
            Shader sky = AssetDatabase.LoadAssetAtPath<Shader>(EnvironmentPresetBuilder.CrossfadeShaderPath);
            Shader snapshot = AssetDatabase.LoadAssetAtPath<Shader>(EnvironmentPresetBuilder.SnapshotShaderPath);
            Assert.IsNotNull(sky);
            Assert.IsNotNull(snapshot);
            Assert.AreEqual(1, sky.passCount);
            Assert.AreEqual(1, snapshot.passCount);
            Assert.IsTrue(sky.isSupported && snapshot.isSupported);
        }

        [Test]
        public void PanoramaUvAndDirectionMappingsAreInverses()
        {
            for (int i = 0; i <= 16; i++)
            {
                for (int j = 1; j < 16; j++)
                {
                    var uv = new Vector2(i / 16f, j / 16f);
                    Vector3 direction = PanoramaMapping.DirectionFromUv(uv);
                    Assert.AreEqual(1f, direction.magnitude, 1e-5f);
                    Vector2 back = PanoramaMapping.UvFromDirection(direction, 0f);
                    float du = Mathf.Abs(back.x - uv.x);
                    Assert.Less(Mathf.Min(du, 1f - du), 1e-4f, $"u at {uv}");
                    Assert.AreEqual(uv.y, back.y, 1e-4f, $"v at {uv}");
                }
            }

            // The image centre is +Z and the horizon; u grows towards +X; the top row is the zenith.
            AssertDirection(new Vector3(0f, 0f, 1f), PanoramaMapping.DirectionFromUv(new Vector2(0.5f, 0.5f)));
            AssertDirection(new Vector3(1f, 0f, 0f), PanoramaMapping.DirectionFromUv(new Vector2(0.75f, 0.5f)));
            AssertDirection(Vector3.up, PanoramaMapping.DirectionFromUv(new Vector2(0.5f, 1f)));
            // A positive rotation moves a direction to a larger u.
            Assert.AreEqual(0.75f, PanoramaMapping.UvFromDirection(Vector3.forward, 90f).x, 1e-4f);
        }

        static void AssertDirection(Vector3 expected, Vector3 actual) =>
            Assert.Less(Vector3.Distance(expected, actual), 1e-4f, $"expected {expected}, was {actual}");

        /// <summary>C# copy of EnvironmentPanorama.cginc's mappings between a direction and equirectangular UV.</summary>
        static class PanoramaMapping
        {
            public static Vector2 UvFromDirection(Vector3 direction, float rotation)
            {
                direction.Normalize();
                float longitude = Mathf.Atan2(direction.x, direction.z) / (2f * Mathf.PI) + rotation / 360f;
                float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) / Mathf.PI + 0.5f;
                return new Vector2(Frac(longitude + 0.5f), latitude);
            }

            public static Vector3 DirectionFromUv(Vector2 uv)
            {
                float longitude = (uv.x - 0.5f) * 2f * Mathf.PI;
                float latitude = (uv.y - 0.5f) * Mathf.PI;
                float latitudeCos = Mathf.Cos(latitude);
                return new Vector3(Mathf.Sin(longitude) * latitudeCos, Mathf.Sin(latitude), Mathf.Cos(longitude) * latitudeCos);
            }

            static float Frac(float value) => value - Mathf.Floor(value);
        }

        [Test]
        public void ShadowQualityOverrideSwitchesHalfwayAndResetRestoresBasisValues()
        {
            EnvironmentFixture fixture = CreateFixture();
            Values basis = Values.Read(fixture.Urp);

            fixture.Controller.TransitionTo(_temple, 2f);
            fixture.Controller.AdvanceTransition(0.4f);
            Assert.AreEqual(basis, Values.Read(fixture.Urp), "The shadow quality switches with the other discrete values at t = 0.5.");
            fixture.Controller.AdvanceTransition(0.8f);
            Assert.AreEqual(Values.From(_temple.ShadowQuality), Values.Read(fixture.Urp));
            fixture.Controller.AdvanceTransition(1f);
            Assert.AreEqual(Values.From(_temple.ShadowQuality), Values.Read(fixture.Urp));

            fixture.Stage.ResetToBaseline();
            Assert.AreEqual(basis, Values.Read(fixture.Urp));

            fixture.Controller.ApplyInstant(_temple);
            Assert.AreEqual(Values.From(_temple.ShadowQuality), Values.Read(fixture.Urp));
            fixture.Controller.ApplyInstant(_exhibition);
            Assert.AreEqual(basis, Values.Read(fixture.Urp));
        }

        [Test]
        public void ShadowQualityOverrideSurvivesBasisRewritingTheAsset()
        {
            EnvironmentFixture fixture = CreateFixture();
            fixture.Controller.ApplyInstant(_temple);

            var basisTier = new Values { Resolution = 4096, Distance = 150f, Cascades = 4 };
            basisTier.Write(fixture.Urp);
            fixture.Controller.MaintainShadowQuality();
            Assert.AreEqual(Values.From(_temple.ShadowQuality), Values.Read(fixture.Urp));

            fixture.Controller.MaintainShadowQuality();
            Assert.AreEqual(Values.From(_temple.ShadowQuality), Values.Read(fixture.Urp));

            fixture.Stage.ResetToBaseline();
            Assert.AreEqual(basisTier, Values.Read(fixture.Urp), "The tier Basis chose last is the one to return to.");
        }

        [Test]
        public void SuspendedShadowQualityOverrideRestoresTheAssetAndResumes()
        {
            UniversalRenderPipelineAsset first = CreateUrpAsset();
            UniversalRenderPipelineAsset second = CreateUrpAsset();
            try
            {
                UniversalRenderPipelineAsset active = first;
                var shadowQuality = new ShadowQualityOverride(() => active);
                Values basis = Values.Read(first);
                Values temple = Values.From(_temple.ShadowQuality);

                shadowQuality.Request(_temple.ShadowQuality);
                Assert.AreEqual(temple, Values.Read(first));

                shadowQuality.Suspend();
                Assert.AreEqual(basis, Values.Read(first), "Disabling the controller must leave the project asset as it was.");
                shadowQuality.Maintain();
                Assert.AreEqual(basis, Values.Read(first), "A suspended override stays off.");

                shadowQuality.Resume();
                Assert.AreEqual(temple, Values.Read(first));

                active = second;
                shadowQuality.Maintain();
                Assert.AreEqual(basis, Values.Read(first), "A quality level switch returns the old asset to its values.");
                Assert.AreEqual(temple, Values.Read(second));

                shadowQuality.Request(_exhibition.ShadowQuality);
                Assert.AreEqual(basis, Values.Read(second));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
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
        public void FadingRoomStopsCastingShadowsAndTheExhibitionRestoresThem()
        {
            EnvironmentFixture fixture = CreateFixture();
            var wall = fixture.Room.GetComponentInChildren<Renderer>(true);
            Assume.That(wall.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));

            fixture.Controller.TransitionTo(_temple, 2f);
            fixture.Controller.AdvanceTransition(0.2f);
            Assert.Less(fixture.WallMaterial().GetColor("_BaseColor").a, 1f);
            Assert.AreEqual(ShadowCastingMode.Off, wall.shadowCastingMode,
                "A room that is fading out must not shade the temple once its sun casts shadows.");
            fixture.Controller.AdvanceTransition(2f);
            Assert.AreEqual(ShadowCastingMode.Off, wall.shadowCastingMode);

            fixture.Stage.ResetToBaseline();
            Assert.AreEqual(ShadowCastingMode.On, wall.shadowCastingMode, "The exhibition room casts shadows as authored.");
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
            TestBinding.Bind(room, "ExhibitionRoom");
            fixture.WallCollider = room.AddComponent<BoxCollider>();
            var wall = new GameObject("WallDisplay");
            wall.transform.SetParent(room.transform, false);
            var renderer = wall.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            var environment = fixture.Track(new GameObject("Environment"));
            TestBinding.Bind(environment, "Environment");
            fixture.Controller = environment.AddComponent<EnvironmentController>();
            var serialized = new SerializedObject(fixture.Controller);
            serialized.FindProperty("_baselinePreset").objectReferenceValue = _exhibition;
            serialized.FindProperty("_directionalLight").objectReferenceValue = fixture.Directional;
            serialized.FindProperty("_spotLight").objectReferenceValue = fixture.Spot;
            serialized.FindProperty("_crossfadeShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(EnvironmentPresetBuilder.CrossfadeShaderPath);
            serialized.FindProperty("_snapshotShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(EnvironmentPresetBuilder.SnapshotShaderPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // The shadow override writes to the URP asset; keep the project's asset out of the tests.
            fixture.Urp = fixture.Track(CreateUrpAsset());
            fixture.Controller.SetShadowQualityAssetProvider(() => fixture.Urp);
            return fixture;
        }

        /// <summary>An in-memory URP asset holding a tier that differs from the temple's override in every field.</summary>
        static UniversalRenderPipelineAsset CreateUrpAsset()
        {
            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            asset.hideFlags = HideFlags.DontSave;
            new Values { Resolution = 512, Distance = 50f, Cascades = 2 }.Write(asset);
            return asset;
        }

        struct Values
        {
            public int Resolution;
            public float Distance;
            public int Cascades;

            public static Values Read(UniversalRenderPipelineAsset asset) => new()
            {
                Resolution = asset.mainLightShadowmapResolution,
                Distance = asset.shadowDistance,
                Cascades = asset.shadowCascadeCount,
            };

            public static Values From(ShadowQualitySettings settings) => new()
            {
                Resolution = settings.MainLightShadowmapResolution,
                Distance = settings.ShadowDistance,
                Cascades = settings.CascadeCount,
            };

            public void Write(UniversalRenderPipelineAsset asset)
            {
                asset.mainLightShadowmapResolution = Resolution;
                asset.shadowDistance = Distance;
                asset.shadowCascadeCount = Cascades;
            }

            public override string ToString() => $"{Resolution}px, {Distance}m, {Cascades} cascade(s)";
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
                source.Reflection,
                source.ShadowQuality);
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
            public UniversalRenderPipelineAsset Urp;

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
