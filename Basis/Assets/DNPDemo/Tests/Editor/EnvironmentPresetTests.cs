using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.TestTools;

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
        Material _fadeMaterial;
        readonly List<EnvironmentFixture> _fixtures = new();

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
            if (_fadeMaterial != null)
            {
                Object.DestroyImmediate(_fadeMaterial);
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
            Assert.AreEqual(AmbientMode.Trilight, _temple.Ambient.Mode);
            AssertPresetComplete(_exhibition);
            AssertPresetComplete(_temple);
        }

        [Test]
        public void PresetsReflectCubemapsBakedFromTheirPanoramas()
        {
            foreach (EnvironmentPreset preset in new[] { _exhibition, _temple })
            {
                Cubemap cubemap = preset.Reflection.Cubemap;
                Assert.IsNotNull(cubemap, preset.name);
                string path = AssetDatabase.GetAssetPath(cubemap);
                Assert.AreEqual(EnvironmentReflectionBaker.ReflectionPath(preset), path, preset.name);
                Assert.AreEqual(EnvironmentReflectionBaker.FaceSize, cubemap.width, $"{preset.name}: the importer keeps the face size of the baked layout.");
                Assert.Greater(cubemap.mipmapCount, 1, $"{preset.name}: rough surfaces sample the blurred mips.");
                var settings = new TextureImporterSettings();
                ((TextureImporter)AssetImporter.GetAtPath(path)).ReadTextureSettings(settings);
                Assert.AreEqual(TextureImporterCubemapConvolution.Specular, settings.cubemapConvolution, preset.name);
            }
            Assert.AreNotSame(_exhibition.Reflection.Cubemap, _temple.Reflection.Cubemap);
        }

        /// <summary>
        /// Renders the temple's sky and its baked reflection cubemap from the same views and compares their average colours.
        /// A cubemap turned by a quarter turn differs by more than 0.2 in some view; filtering differences stay below 0.05.
        /// </summary>
        [Test]
        public void BakedReflectionFacesTheSameWayAsTheSky()
        {
            const float Tolerance = 0.12f;
            var sky = new Material(AssetDatabase.LoadAssetAtPath<Shader>(EnvironmentPresetBuilder.CrossfadeShaderPath));
            var cube = new Material(Shader.Find("Skybox/Cubemap"));
            try
            {
                sky.SetTexture("_PanoramaA", _temple.Panorama);
                sky.SetTexture("_PanoramaB", _temple.Panorama);
                sky.SetFloat("_Blend", 0f);
                sky.SetFloat("_ExposureA", _temple.Exposure);
                sky.SetFloat("_ExposureB", _temple.Exposure);
                sky.SetFloat("_RotationA", _temple.Rotation);
                sky.SetFloat("_RotationB", _temple.Rotation);
                cube.SetTexture("_Tex", _temple.Reflection.Cubemap);

                foreach (Vector3 euler in new[]
                         {
                             new Vector3(0f, 0f, 0f), new Vector3(0f, 90f, 0f), new Vector3(0f, 180f, 0f), new Vector3(0f, 270f, 0f),
                             new Vector3(-60f, 45f, 0f), new Vector3(60f, 135f, 0f), new Vector3(-89f, 0f, 0f), new Vector3(89f, 0f, 0f),
                         })
                {
                    Quaternion view = Quaternion.Euler(euler);
                    Color expected = RenderSkyAverage(sky, view);
                    Color actual = RenderSkyAverage(cube, view);
                    float difference = Mathf.Abs(expected.r - actual.r) + Mathf.Abs(expected.g - actual.g) + Mathf.Abs(expected.b - actual.b);
                    Assert.Less(difference, Tolerance, $"view {euler}: sky {expected}, reflection {actual}");
                }
            }
            finally
            {
                Object.DestroyImmediate(sky);
                Object.DestroyImmediate(cube);
            }
        }

        static Color RenderSkyAverage(Material skybox, Quaternion view)
        {
            var preview = new PreviewRenderUtility();
            try
            {
                Camera camera = preview.camera;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.fieldOfView = 30f;
                camera.transform.SetPositionAndRotation(Vector3.zero, view);
                camera.gameObject.AddComponent<Skybox>().material = skybox;
                preview.BeginStaticPreview(new Rect(0f, 0f, 32f, 32f));
                camera.Render();
                Texture2D image = preview.EndStaticPreview();
                Color[] pixels = image.GetPixels();
                Object.DestroyImmediate(image);
                Color sum = Color.clear;
                foreach (Color pixel in pixels)
                {
                    sum += pixel;
                }
                return sum / pixels.Length;
            }
            finally
            {
                preview.Cleanup();
            }
        }

        static float Frac(float value) => value - Mathf.Floor(value);

        [Test]
        public void ExhibitionRoomPrefabFadesEveryRendererWithPreparedTransparentMaterials()
        {
            var room = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPresetBuilder.ExhibitionRoomPrefabPath);
            Assert.IsNotNull(room, EnvironmentPresetBuilder.ExhibitionRoomPrefabPath);
            var group = room.GetComponent<EnvironmentFadeGroup>();
            Assert.IsNotNull(group, "Env_ExhibitionRoom needs an EnvironmentFadeGroup. Run DNP Demo/Setup/Build Environment Presets.");

            Renderer[] renderers = room.GetComponentsInChildren<Renderer>(true);
            var listed = new HashSet<Renderer>();
            for (int i = 0; i < group.EntryCount; i++)
            {
                EnvironmentFadeGroup.Entry entry = group.GetEntry(i);
                Assert.IsNotNull(entry.Renderer, $"entry {i}");
                listed.Add(entry.Renderer);
                Material[] originals = entry.Renderer.sharedMaterials;
                Assert.AreEqual(originals.Length, entry.FadeMaterials.Length, entry.Renderer.name);
                for (int slot = 0; slot < originals.Length; slot++)
                {
                    Material original = originals[slot];
                    Material fade = entry.FadeMaterials[slot];
                    if (original == null)
                    {
                        Assert.IsNull(fade);
                        continue;
                    }
                    string context = $"{entry.Renderer.name}[{slot}] {original.name}";
                    Assert.IsNotNull(fade, context);
                    StringAssert.StartsWith(EnvironmentPresetBuilder.ExhibitionRoomFadeMaterialFolder + "/", AssetDatabase.GetAssetPath(fade),
                        $"{context}: the fade material must be an asset so its shader variant is built.");
                    Assert.AreSame(original.shader, fade.shader, context);
                    Assert.AreEqual(1f, fade.GetFloat("_Surface"), context);
                    Assert.IsTrue(fade.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), context);
                    Assert.AreEqual((int)RenderQueue.Transparent, fade.renderQueue, context);
                    Assert.AreEqual(0f, fade.GetFloat("_BlendModePreserveSpecular"), $"{context}: highlights must fade with the wall.");
                    Assert.IsFalse(fade.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"), context);
                    Assert.AreEqual((float)BlendMode.SrcAlpha, fade.GetFloat("_SrcBlend"), context);
                    Assert.AreEqual(original.GetColor("_BaseColor"), fade.GetColor("_BaseColor"), context);
                    Assert.AreSame(original.GetTexture("_BaseMap"), fade.GetTexture("_BaseMap"), context);
                }
            }
            CollectionAssert.AreEquivalent(renderers, listed, "Every renderer of the room fades with it.");
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
            Assert.IsFalse(fixture.WallRenderer.enabled, "A hidden room is not drawn, so it casts no shadows.");
            Assert.IsTrue(fixture.WallCollider.enabled, "Fading the display must not disable the navigation collider.");

            fixture.Stage.ResetToBaseline();

            Assert.AreEqual(_exhibition.DirectionalLight.Intensity, fixture.Directional.intensity);
            Assert.IsFalse(fixture.Spot.gameObject.activeSelf, "The exhibition keeps the spot light as the temple left it.");
            Assert.AreSame(_exhibition.Panorama, RenderSettings.skybox.GetTexture("_PanoramaA"));
            AssertWallOpaque(fixture);
        }

        [Test]
        public void ApplyingTheBaselineKeepsTheRoomOnItsOpaqueMaterials()
        {
            EnvironmentFixture fixture = CreateFixture();

            // Entering a step after connecting resets the stage, which is what first initializes the controller.
            fixture.Stage.ResetToBaseline();

            AssertWallOpaque(fixture);
        }

        [Test]
        public void SwitchingEnvironmentsLogsNoWarnings()
        {
            EnvironmentFixture fixture = CreateFixture();
            fixture.Controller.ApplyInstant(_exhibition);
            var logged = new List<string>();
            void Collect(string message, string stackTrace, LogType type)
            {
                if (type != LogType.Log)
                {
                    logged.Add($"{type}: {message}");
                }
            }

            Application.logMessageReceived += Collect;
            try
            {
                // Every frame of a transition applies the lights, so a setter that only the Built-In pipeline supports
                // (Light.shadowResolution) would warn once per light per frame.
                fixture.Controller.TransitionTo(_temple, 2f);
                for (int frame = 0; frame < 20; frame++)
                {
                    fixture.Controller.AdvanceTransition(0.1f);
                }
                fixture.Controller.ApplyInstant(_exhibition);
                fixture.Controller.ApplyInstant(_temple);
                fixture.Stage.ResetToBaseline();
            }
            finally
            {
                Application.logMessageReceived -= Collect;
            }

            Assert.IsEmpty(logged, string.Join("\n", logged));
            LogAssert.NoUnexpectedReceived();
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
            Material fading = fixture.WallRenderer.sharedMaterial;
            Assert.AreNotSame(_wallMaterial, fading);
            Assert.AreNotSame(_fadeMaterial, fading, "The room fades with a runtime copy, so the fade asset is not modified.");
            Assert.IsTrue(fading.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), "The copy keeps the asset's keywords, so it uses the built variant.");
            Assert.AreEqual((int)RenderQueue.Transparent, fading.renderQueue);
            Assert.AreEqual(1f, fading.GetFloat("_ZWrite"), "The fading room keeps writing depth so its faces occlude each other.");
            Assert.AreEqual(0.5f, fading.GetColor("_BaseColor").a, 0.02f);
            Assert.AreEqual(1f, _fadeMaterial.GetColor("_BaseColor").a, "The fade asset is not modified.");
            Assert.AreEqual(ShadowCastingMode.Off, fixture.WallRenderer.shadowCastingMode, "A fading room casts no shadows.");
            Assert.IsTrue(fixture.WallRenderer.enabled);

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
            AssertWallOpaque(fixture);

            fixture.Controller.TransitionTo(_temple, 2f);
            fixture.Controller.AdvanceTransition(0.5f);
            cue.ApplyInstant(context);
            Assert.IsFalse(fixture.Controller.IsTransitioning);
            Assert.AreEqual(_temple.DirectionalLight.Intensity, fixture.Directional.intensity);
            Assert.AreSame(_temple.Panorama, RenderSettings.skybox.GetTexture("_PanoramaA"));
        }

        [Test]
        public void ReflectionDimsToZeroAndSwitchesCubemapsHalfway()
        {
            EnvironmentFixture fixture = CreateFixture();
            fixture.Controller.ApplyInstant(_exhibition);
            Assert.AreEqual(DefaultReflectionMode.Custom, RenderSettings.defaultReflectionMode);
            Assert.AreSame(_exhibition.Reflection.Cubemap, RenderSettings.customReflectionTexture);
            Assert.AreEqual(_exhibition.Reflection.Intensity, RenderSettings.reflectionIntensity, 1e-4f);

            fixture.Controller.TransitionTo(_temple, 2f);
            fixture.Controller.AdvanceTransition(0.5f);
            Assert.AreSame(_exhibition.Reflection.Cubemap, RenderSettings.customReflectionTexture);
            Assert.Less(RenderSettings.reflectionIntensity, _exhibition.Reflection.Intensity, "The outgoing reflection dims first.");
            Assert.Greater(RenderSettings.reflectionIntensity, 0f);

            fixture.Controller.AdvanceTransition(0.5f);
            Assert.AreSame(_temple.Reflection.Cubemap, RenderSettings.customReflectionTexture, "The cubemap switches at the midpoint.");
            Assert.AreEqual(0f, RenderSettings.reflectionIntensity, 1e-4f, "The switch happens while the reflection is dark.");

            fixture.Controller.AdvanceTransition(1f);
            Assert.IsFalse(fixture.Controller.IsTransitioning);
            Assert.AreSame(_temple.Reflection.Cubemap, RenderSettings.customReflectionTexture);
            Assert.AreEqual(_temple.Reflection.Intensity, RenderSettings.reflectionIntensity, 1e-4f);
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
            fixture.WallRenderer = wall.AddComponent<MeshRenderer>();
            fixture.WallRenderer.sharedMaterial = _wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            fixture.WallRenderer.shadowCastingMode = ShadowCastingMode.On;
            _fadeMaterial = new Material(_wallMaterial);
            EnvironmentPresetBuilder.ConfigureFadeMaterial(_fadeMaterial);
            room.AddComponent<EnvironmentFadeGroup>().SetEntries(new[]
            {
                new EnvironmentFadeGroup.Entry { Renderer = fixture.WallRenderer, FadeMaterials = new[] { _fadeMaterial } },
            });

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

        void AssertWallOpaque(EnvironmentFixture fixture)
        {
            Assert.AreSame(_wallMaterial, fixture.WallRenderer.sharedMaterial, "A fully visible room is drawn with its own opaque material.");
            Assert.IsTrue(fixture.WallRenderer.enabled);
            Assert.AreEqual(ShadowCastingMode.On, fixture.WallRenderer.shadowCastingMode, "A fully visible room casts shadows as authored.");
            Assert.AreEqual(0f, _wallMaterial.GetFloat("_Surface"), "The shared wall material must stay opaque.");
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
                source.DisplayName,
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
            readonly List<Object> _objects = new();

            public ScenarioStage Stage;
            public EnvironmentController Controller;
            public Light Directional;
            public Light Spot;
            public BoxCollider WallCollider;
            public Renderer WallRenderer;
            public GameObject Room;
            public UniversalRenderPipelineAsset Urp;

            public T Track<T>(T target) where T : Object
            {
                _objects.Add(target);
                return target;
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
