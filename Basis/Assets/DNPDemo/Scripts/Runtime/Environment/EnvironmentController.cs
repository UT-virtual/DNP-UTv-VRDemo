using System;
using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DNPDemo.Environment
{
    /// <summary>Applies an environment preset and owns its interruptible, scene-wide transition.</summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentController : MonoBehaviour, IStageResettable
    {
        /// <summary>Default <see cref="DNPDemo.Scenario.ScenarioObjectBinding"/> key of the object that holds this controller.</summary>
        public const string EnvironmentKey = "Environment";
        /// <summary>Key of the exhibition room whose renderers fade out when a preset hides the room.</summary>
        public const string ExhibitionRoomKey = "ExhibitionRoom";
        const string PanoramaShaderName = "DNPDemo/Environment Crossfade Panorama";

        static readonly int PanoramaAId = Shader.PropertyToID("_PanoramaA");
        static readonly int PanoramaBId = Shader.PropertyToID("_PanoramaB");
        static readonly int BlendId = Shader.PropertyToID("_Blend");
        static readonly int ExposureAId = Shader.PropertyToID("_ExposureA");
        static readonly int ExposureBId = Shader.PropertyToID("_ExposureB");
        static readonly int RotationAId = Shader.PropertyToID("_RotationA");
        static readonly int RotationBId = Shader.PropertyToID("_RotationB");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        [SerializeField] EnvironmentPreset _baselinePreset;
        [SerializeField] Light _directionalLight;
        [SerializeField] Light _spotLight;
        [SerializeField] Shader _crossfadeShader;
        [SerializeField, Min(0f)] float _transitionDuration = 2f;

        sealed class WallRenderer
        {
            public Renderer Renderer;
            public Material[] OriginalMaterials;
            public Material[] FadeMaterials;
            public Color[] BaseColors;
            public int[] ColorPropertyIds;
        }

        struct RuntimeState
        {
            public Color DirectionalColor;
            public float DirectionalIntensity;
            public Quaternion DirectionalRotation;
            public LightShadows DirectionalShadows;
            public LightShadowResolution DirectionalShadowResolution;
            public float DirectionalShadowStrength;
            public float DirectionalShadowBias;
            public float DirectionalShadowNormalBias;
            public float DirectionalShadowNearPlane;
            public bool SpotEnabled;
            public Color SpotColor;
            public float SpotIntensity;
            public Quaternion SpotRotation;
            public float SpotRange;
            public float SpotAngle;
            public float SpotInnerAngle;
            public LightShadows SpotShadows;
            public LightShadowResolution SpotShadowResolution;
            public float SpotShadowStrength;
            public float SpotShadowBias;
            public float SpotShadowNormalBias;
            public float SpotShadowNearPlane;
            public AmbientMode AmbientMode;
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;
            public Color AmbientFlat;
            public float AmbientIntensity;
            public DefaultReflectionMode ReflectionMode;
            public Texture CustomReflection;
            public float ReflectionIntensity;
            public ShadowQualitySettings ShadowQuality;
            public float WallAlpha;

            /// <param name="spotEnabled">Spot light activation already resolved from the preset's <see cref="SpotLightMode"/>.</param>
            public static RuntimeState FromPreset(EnvironmentPreset preset, bool spotEnabled)
            {
                DirectionalLightSettings directional = preset.DirectionalLight;
                SpotLightSettings spot = preset.SpotLight;
                AmbientSettings ambient = preset.Ambient;
                ReflectionSettings reflection = preset.Reflection;
                return new RuntimeState
                {
                    DirectionalColor = directional.Color,
                    DirectionalIntensity = directional.Intensity,
                    DirectionalRotation = Quaternion.Euler(directional.Rotation),
                    DirectionalShadows = directional.Shadows,
                    DirectionalShadowResolution = directional.ShadowResolution,
                    DirectionalShadowStrength = directional.ShadowStrength,
                    DirectionalShadowBias = directional.ShadowBias,
                    DirectionalShadowNormalBias = directional.ShadowNormalBias,
                    DirectionalShadowNearPlane = directional.ShadowNearPlane,
                    SpotEnabled = spotEnabled,
                    SpotColor = spot.Color,
                    SpotIntensity = spot.Intensity,
                    SpotRotation = Quaternion.Euler(spot.Rotation),
                    SpotRange = spot.Range,
                    SpotAngle = spot.SpotAngle,
                    SpotInnerAngle = spot.InnerSpotAngle,
                    SpotShadows = spot.Shadows,
                    SpotShadowResolution = spot.ShadowResolution,
                    SpotShadowStrength = spot.ShadowStrength,
                    SpotShadowBias = spot.ShadowBias,
                    SpotShadowNormalBias = spot.ShadowNormalBias,
                    SpotShadowNearPlane = spot.ShadowNearPlane,
                    AmbientMode = ambient.Mode,
                    AmbientSky = ambient.SkyColor,
                    AmbientEquator = ambient.EquatorColor,
                    AmbientGround = ambient.GroundColor,
                    AmbientFlat = ambient.FlatColor,
                    AmbientIntensity = ambient.Intensity,
                    ReflectionMode = reflection.Mode,
                    CustomReflection = reflection.CustomReflectionTexture,
                    ReflectionIntensity = reflection.Intensity,
                    ShadowQuality = preset.ShadowQuality,
                    WallAlpha = preset.ExhibitionRoomVisible ? 1f : 0f,
                };
            }

            /// <summary>
            /// Intermediate state of a transition. A disabled spot light counts as intensity 0, so a light that turns on
            /// fades in from dark and one that turns off fades out before it is deactivated at the end.
            /// </summary>
            public static RuntimeState Lerp(RuntimeState from, RuntimeState to, float t)
            {
                float fromSpotIntensity = from.SpotEnabled ? from.SpotIntensity : 0f;
                float toSpotIntensity = to.SpotEnabled ? to.SpotIntensity : 0f;
                return new RuntimeState
                {
                    DirectionalColor = Color.Lerp(from.DirectionalColor, to.DirectionalColor, t),
                    DirectionalIntensity = Mathf.Lerp(from.DirectionalIntensity, to.DirectionalIntensity, t),
                    DirectionalRotation = Quaternion.Slerp(from.DirectionalRotation, to.DirectionalRotation, t),
                    DirectionalShadows = t < 0.5f ? from.DirectionalShadows : to.DirectionalShadows,
                    DirectionalShadowResolution = t < 0.5f ? from.DirectionalShadowResolution : to.DirectionalShadowResolution,
                    DirectionalShadowStrength = Mathf.Lerp(from.DirectionalShadowStrength, to.DirectionalShadowStrength, t),
                    DirectionalShadowBias = Mathf.Lerp(from.DirectionalShadowBias, to.DirectionalShadowBias, t),
                    DirectionalShadowNormalBias = Mathf.Lerp(from.DirectionalShadowNormalBias, to.DirectionalShadowNormalBias, t),
                    DirectionalShadowNearPlane = Mathf.Lerp(from.DirectionalShadowNearPlane, to.DirectionalShadowNearPlane, t),
                    SpotEnabled = t >= 1f ? to.SpotEnabled : from.SpotEnabled || to.SpotEnabled,
                    SpotColor = Color.Lerp(from.SpotColor, to.SpotColor, t),
                    SpotIntensity = Mathf.Lerp(fromSpotIntensity, toSpotIntensity, t),
                    SpotRotation = Quaternion.Slerp(from.SpotRotation, to.SpotRotation, t),
                    SpotRange = Mathf.Lerp(from.SpotRange, to.SpotRange, t),
                    SpotAngle = Mathf.Lerp(from.SpotAngle, to.SpotAngle, t),
                    SpotInnerAngle = Mathf.Lerp(from.SpotInnerAngle, to.SpotInnerAngle, t),
                    SpotShadows = t < 0.5f ? from.SpotShadows : to.SpotShadows,
                    SpotShadowResolution = t < 0.5f ? from.SpotShadowResolution : to.SpotShadowResolution,
                    SpotShadowStrength = Mathf.Lerp(from.SpotShadowStrength, to.SpotShadowStrength, t),
                    SpotShadowBias = Mathf.Lerp(from.SpotShadowBias, to.SpotShadowBias, t),
                    SpotShadowNormalBias = Mathf.Lerp(from.SpotShadowNormalBias, to.SpotShadowNormalBias, t),
                    SpotShadowNearPlane = Mathf.Lerp(from.SpotShadowNearPlane, to.SpotShadowNearPlane, t),
                    AmbientMode = t < 0.5f ? from.AmbientMode : to.AmbientMode,
                    AmbientSky = Color.Lerp(from.AmbientSky, to.AmbientSky, t),
                    AmbientEquator = Color.Lerp(from.AmbientEquator, to.AmbientEquator, t),
                    AmbientGround = Color.Lerp(from.AmbientGround, to.AmbientGround, t),
                    AmbientFlat = Color.Lerp(from.AmbientFlat, to.AmbientFlat, t),
                    AmbientIntensity = Mathf.Lerp(from.AmbientIntensity, to.AmbientIntensity, t),
                    ReflectionMode = t < 0.5f ? from.ReflectionMode : to.ReflectionMode,
                    CustomReflection = t < 0.5f ? from.CustomReflection : to.CustomReflection,
                    ReflectionIntensity = Mathf.Lerp(from.ReflectionIntensity, to.ReflectionIntensity, t),
                    ShadowQuality = t < 0.5f ? from.ShadowQuality : to.ShadowQuality,
                    WallAlpha = Mathf.Lerp(from.WallAlpha, to.WallAlpha, t),
                };
            }
        }

        readonly List<WallRenderer> _wallRenderers = new();
        Material _skyboxMaterial;
        RenderTexture _skySnapshotA;
        RenderTexture _skySnapshotB;
        RenderTexture _activeSkySnapshot;
        Material _originalSkybox;
        RuntimeState _originalState;
        Func<UniversalRenderPipelineAsset> _shadowQualityAssetProvider = ShadowQualityOverride.ActiveAsset;
        ShadowQualityOverride _shadowQuality;
        RuntimeState _transitionStart;
        EnvironmentPreset _transitionTarget;
        bool _transitionTargetSpotEnabled;
        ScenarioStage _stage;
        float _wallAlpha = 1f;
        float _elapsed;
        float _duration;
        bool _initialized;
        bool _isTransitioning;
        bool _reportedMissingWalls;
        bool _reportedWallMaterial;

        public bool IsTransitioning => _isTransitioning;

        void Update() => AdvanceTransition(Time.unscaledDeltaTime);

        void LateUpdate() => MaintainShadowQuality();

        void OnEnable() => _shadowQuality?.Resume();

        /// <summary>The URP asset is a project asset, so the shadow override must not outlive this component.</summary>
        void OnDisable() => _shadowQuality?.Suspend();

        /// <summary>Begins a fade from the currently displayed state; a new request replaces an in-flight destination.</summary>
        public void TransitionTo(EnvironmentPreset preset, float duration = -1f)
        {
            if (!EnsureInitialized() || !ValidatePreset(preset))
            {
                return;
            }
            if (duration < 0f)
            {
                duration = _transitionDuration;
            }
            if (duration <= 0f)
            {
                ApplyInstant(preset);
                return;
            }

            bool targetSpotEnabled = ResolveSpotEnabled(preset);
            CaptureCurrentSky();
            _transitionStart = CaptureCurrentState();
            _transitionTarget = preset;
            _transitionTargetSpotEnabled = targetSpotEnabled;
            _elapsed = 0f;
            _duration = duration;
            _isTransitioning = true;
            ConfigureSkyTransition(preset);
        }

        /// <summary>Stops any active fade and applies the supplied preset without animation.</summary>
        public void ApplyInstant(EnvironmentPreset preset)
        {
            if (!EnsureInitialized() || !ValidatePreset(preset))
            {
                return;
            }
            bool spotEnabled = ResolveSpotEnabled(preset);
            _isTransitioning = false;
            _transitionTarget = null;
            _elapsed = 0f;
            RuntimeState state = RuntimeState.FromPreset(preset, spotEnabled);
            ApplyState(state);
            SetSkyboxPreset(preset);
            ReleaseSnapshots();
        }

        /// <summary>
        /// Re-applies the preset's shadow quality if Basis rewrote the URP asset. Runs after the frame's other updates so the
        /// override is in place before rendering. Internal so EditMode tests can drive it.
        /// </summary>
        internal void MaintainShadowQuality() => _shadowQuality?.Maintain();

        /// <summary>Replaces the URP asset the shadow override writes to. Tests call it before the controller initializes.</summary>
        internal void SetShadowQualityAssetProvider(Func<UniversalRenderPipelineAsset> provider)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("The shadow quality asset must be set before the controller initializes.");
            }
            _shadowQualityAssetProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>Advances the transition clock. Internal so EditMode tests can drive it deterministically.</summary>
        internal void AdvanceTransition(float deltaTime)
        {
            if (!_isTransitioning)
            {
                return;
            }
            _elapsed = Mathf.Min(_elapsed + Mathf.Max(0f, deltaTime), _duration);
            float linear = _duration <= 0f ? 1f : _elapsed / _duration;
            float progress = Mathf.SmoothStep(0f, 1f, linear);
            RuntimeState target = RuntimeState.FromPreset(_transitionTarget, _transitionTargetSpotEnabled);
            ApplyState(RuntimeState.Lerp(_transitionStart, target, progress));
            _skyboxMaterial.SetFloat(BlendId, progress);

            if (_elapsed >= _duration)
            {
                EnvironmentPreset targetPreset = _transitionTarget;
                _isTransitioning = false;
                _transitionTarget = null;
                ApplyState(target);
                SetSkyboxPreset(targetPreset);
                ReleaseSnapshots();
            }
        }

        bool IsSpotLightOn() => _spotLight.gameObject.activeSelf && _spotLight.enabled;

        /// <summary>
        /// Resolves the preset's spot light mode to an activation. Keep follows the destination of an in-flight transition,
        /// so a light that is fading out is not turned back on by a preset that leaves it alone.
        /// </summary>
        bool ResolveSpotEnabled(EnvironmentPreset preset)
        {
            bool current = _isTransitioning ? _transitionTargetSpotEnabled : IsSpotLightOn();
            return preset.SpotLight.Enabled switch
            {
                SpotLightMode.On => true,
                SpotLightMode.Off => false,
                _ => current,
            };
        }

        void IStageResettable.ResetToBaseline()
        {
            _isTransitioning = false;
            _transitionTarget = null;
            _elapsed = 0f;
            ReleaseSnapshots();
            ApplyInstant(_baselinePreset);
        }

        void CaptureCurrentSky()
        {
            Texture currentSource = _skyboxMaterial.GetTexture(PanoramaAId);
            RenderTexture snapshot = currentSource == _skySnapshotA ? _skySnapshotB : _skySnapshotA;
            if (snapshot == null)
            {
                snapshot = new RenderTexture(1024, 512, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
                {
                    name = "Environment Transition Snapshot",
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                };
                snapshot.Create();
                if (currentSource == _skySnapshotA)
                {
                    _skySnapshotB = snapshot;
                }
                else
                {
                    _skySnapshotA = snapshot;
                }
            }
            Graphics.Blit(null, snapshot, _skyboxMaterial, 1);
            _activeSkySnapshot = snapshot;
        }

        /// <summary>
        /// The snapshot already has the outgoing exposure and rotation baked in, so it is drawn as is. The target panorama
        /// uses its own exposure and rotation from the first frame; only the blend factor animates.
        /// </summary>
        void ConfigureSkyTransition(EnvironmentPreset preset)
        {
            _skyboxMaterial.SetTexture(PanoramaAId, _activeSkySnapshot);
            _skyboxMaterial.SetTexture(PanoramaBId, preset.Panorama);
            _skyboxMaterial.SetFloat(BlendId, 0f);
            _skyboxMaterial.SetFloat(ExposureAId, 0f);
            _skyboxMaterial.SetFloat(ExposureBId, preset.Exposure);
            _skyboxMaterial.SetFloat(RotationAId, 0f);
            _skyboxMaterial.SetFloat(RotationBId, preset.Rotation);
        }

        void SetSkyboxPreset(EnvironmentPreset preset)
        {
            _skyboxMaterial.SetTexture(PanoramaAId, preset.Panorama);
            _skyboxMaterial.SetTexture(PanoramaBId, preset.Panorama);
            _skyboxMaterial.SetFloat(BlendId, 0f);
            _skyboxMaterial.SetFloat(ExposureAId, preset.Exposure);
            _skyboxMaterial.SetFloat(ExposureBId, preset.Exposure);
            _skyboxMaterial.SetFloat(RotationAId, preset.Rotation);
            _skyboxMaterial.SetFloat(RotationBId, preset.Rotation);
            // The Skybox reflection is a cubemap captured from the sky, not the live sky. Recapture it once the panorama has
            // settled so reflective materials show the new environment.
            DynamicGI.UpdateEnvironment();
        }

        RuntimeState CaptureCurrentState()
        {
            return new RuntimeState
            {
                DirectionalColor = _directionalLight.color,
                DirectionalIntensity = _directionalLight.intensity,
                DirectionalRotation = _directionalLight.transform.rotation,
                DirectionalShadows = _directionalLight.shadows,
                DirectionalShadowResolution = _directionalLight.shadowResolution,
                DirectionalShadowStrength = _directionalLight.shadowStrength,
                DirectionalShadowBias = _directionalLight.shadowBias,
                DirectionalShadowNormalBias = _directionalLight.shadowNormalBias,
                DirectionalShadowNearPlane = _directionalLight.shadowNearPlane,
                SpotEnabled = IsSpotLightOn(),
                SpotColor = _spotLight.color,
                SpotIntensity = _spotLight.intensity,
                SpotRotation = _spotLight.transform.rotation,
                SpotRange = _spotLight.range,
                SpotAngle = _spotLight.spotAngle,
                SpotInnerAngle = _spotLight.innerSpotAngle,
                SpotShadows = _spotLight.shadows,
                SpotShadowResolution = _spotLight.shadowResolution,
                SpotShadowStrength = _spotLight.shadowStrength,
                SpotShadowBias = _spotLight.shadowBias,
                SpotShadowNormalBias = _spotLight.shadowNormalBias,
                SpotShadowNearPlane = _spotLight.shadowNearPlane,
                AmbientMode = RenderSettings.ambientMode,
                AmbientSky = RenderSettings.ambientSkyColor,
                AmbientEquator = RenderSettings.ambientEquatorColor,
                AmbientGround = RenderSettings.ambientGroundColor,
                AmbientFlat = RenderSettings.ambientLight,
                AmbientIntensity = RenderSettings.ambientIntensity,
                ReflectionMode = RenderSettings.defaultReflectionMode,
                CustomReflection = RenderSettings.customReflectionTexture,
                ReflectionIntensity = RenderSettings.reflectionIntensity,
                ShadowQuality = _shadowQuality != null ? _shadowQuality.Requested : default,
                WallAlpha = _wallAlpha,
            };
        }

        void ApplyState(RuntimeState state)
        {
            ApplyLightingState(state);
            SetWallAlpha(state.WallAlpha);
        }

        /// <summary>Applies the lights and RenderSettings. The lights are null-checked because OnDestroy may run after them.</summary>
        void ApplyLightingState(RuntimeState state)
        {
            if (_directionalLight != null)
            {
                _directionalLight.color = state.DirectionalColor;
                _directionalLight.intensity = state.DirectionalIntensity;
                _directionalLight.transform.rotation = state.DirectionalRotation;
                _directionalLight.shadows = state.DirectionalShadows;
                _directionalLight.shadowResolution = state.DirectionalShadowResolution;
                _directionalLight.shadowStrength = state.DirectionalShadowStrength;
                _directionalLight.shadowBias = state.DirectionalShadowBias;
                _directionalLight.shadowNormalBias = state.DirectionalShadowNormalBias;
                _directionalLight.shadowNearPlane = state.DirectionalShadowNearPlane;
            }

            if (_spotLight != null)
            {
                _spotLight.gameObject.SetActive(state.SpotEnabled);
                _spotLight.enabled = true;
                _spotLight.color = state.SpotColor;
                _spotLight.intensity = state.SpotIntensity;
                _spotLight.transform.rotation = state.SpotRotation;
                _spotLight.range = state.SpotRange;
                _spotLight.spotAngle = state.SpotAngle;
                _spotLight.innerSpotAngle = state.SpotInnerAngle;
                _spotLight.shadows = state.SpotShadows;
                _spotLight.shadowResolution = state.SpotShadowResolution;
                _spotLight.shadowStrength = state.SpotShadowStrength;
                _spotLight.shadowBias = state.SpotShadowBias;
                _spotLight.shadowNormalBias = state.SpotShadowNormalBias;
                _spotLight.shadowNearPlane = state.SpotShadowNearPlane;
            }

            RenderSettings.ambientMode = state.AmbientMode;
            RenderSettings.ambientSkyColor = state.AmbientSky;
            RenderSettings.ambientEquatorColor = state.AmbientEquator;
            RenderSettings.ambientGroundColor = state.AmbientGround;
            RenderSettings.ambientLight = state.AmbientFlat;
            RenderSettings.ambientIntensity = state.AmbientIntensity;

            RenderSettings.defaultReflectionMode = state.ReflectionMode;
            RenderSettings.customReflectionTexture = state.CustomReflection;
            RenderSettings.reflectionIntensity = state.ReflectionIntensity;

            _shadowQuality?.Request(state.ShadowQuality);
        }

        void SetWallAlpha(float alpha)
        {
            if (!EnsureWallRenderers())
            {
                return;
            }
            _wallAlpha = alpha;
            foreach (WallRenderer wall in _wallRenderers)
            {
                if (wall.Renderer == null)
                {
                    continue;
                }
                for (int i = 0; i < wall.FadeMaterials.Length; i++)
                {
                    Material material = wall.FadeMaterials[i];
                    if (material == null)
                    {
                        continue;
                    }
                    Color color = wall.BaseColors[i];
                    color.a *= alpha;
                    material.SetColor(wall.ColorPropertyIds[i], color);
                }
            }
        }

        bool EnsureWallRenderers()
        {
            if (_wallRenderers.Count > 0)
            {
                return true;
            }
            if (_stage == null)
            {
                _stage = FindStage(gameObject.scene);
            }
            if (_stage == null || !_stage.TryGetObject(ExhibitionRoomKey, out GameObject room))
            {
                if (!_reportedMissingWalls)
                {
                    Debug.LogWarning($"[DNPDemo] 環境の壁表示に必要なキー「{ExhibitionRoomKey}」がシーンにありません。", this);
                    _reportedMissingWalls = true;
                }
                return false;
            }

            foreach (Renderer renderer in room.GetComponentsInChildren<Renderer>(true))
            {
                Material[] originals = renderer.sharedMaterials;
                var fades = new Material[originals.Length];
                var colors = new Color[originals.Length];
                var colorProperties = new int[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    Material original = originals[i];
                    if (original == null)
                    {
                        continue;
                    }
                    int colorProperty = original.HasProperty(BaseColorId) ? BaseColorId : original.HasProperty(LegacyColorId) ? LegacyColorId : 0;
                    if (colorProperty == 0 || !original.HasProperty("_Surface"))
                    {
                        fades[i] = original;
                        if (!_reportedWallMaterial)
                        {
                            Debug.LogWarning($"[DNPDemo] 壁のマテリアル「{original.name}」は URP Lit の透明フェードに対応していません。", renderer);
                            _reportedWallMaterial = true;
                        }
                        continue;
                    }
                    Material fade = new(original) { hideFlags = HideFlags.DontSave };
                    ConfigureTransparentMaterial(fade);
                    fades[i] = fade;
                    colors[i] = fade.GetColor(colorProperty);
                    colorProperties[i] = colorProperty;
                }
                renderer.sharedMaterials = fades;
                _wallRenderers.Add(new WallRenderer
                {
                    Renderer = renderer,
                    OriginalMaterials = originals,
                    FadeMaterials = fades,
                    BaseColors = colors,
                    ColorPropertyIds = colorProperties,
                });
            }
            return _wallRenderers.Count > 0;
        }

        static void ConfigureTransparentMaterial(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        bool EnsureInitialized()
        {
            if (_initialized)
            {
                return true;
            }
            if (_baselinePreset == null || _directionalLight == null || _spotLight == null)
            {
                Debug.LogError("[DNPDemo] EnvironmentController needs a baseline preset, directional light, and spot light.", this);
                return false;
            }
            if (_crossfadeShader == null)
            {
                _crossfadeShader = Shader.Find(PanoramaShaderName);
            }
            if (_crossfadeShader == null)
            {
                Debug.LogError($"[DNPDemo] Skybox shader '{PanoramaShaderName}' was not found.", this);
                return false;
            }
            _originalSkybox = RenderSettings.skybox;
            _originalState = CaptureCurrentState();
            _shadowQuality = new ShadowQualityOverride(_shadowQualityAssetProvider);
            _skyboxMaterial = new Material(_crossfadeShader)
            {
                name = "Environment Crossfade Runtime",
                hideFlags = HideFlags.DontSave,
            };
            RenderSettings.skybox = _skyboxMaterial;
            _stage = FindStage(gameObject.scene);
            _initialized = true;
            ApplyState(RuntimeState.FromPreset(_baselinePreset, ResolveSpotEnabled(_baselinePreset)));
            SetSkyboxPreset(_baselinePreset);
            return true;
        }

        bool ValidatePreset(EnvironmentPreset preset)
        {
            if (preset == null)
            {
                Debug.LogError("[DNPDemo] EnvironmentCue has no target preset.", this);
                return false;
            }
            if (preset.Panorama == null)
            {
                Debug.LogError($"[DNPDemo] Environment preset '{preset.name}' has no panorama texture.", preset);
                return false;
            }
            if (preset.Reflection.Mode == DefaultReflectionMode.Custom && preset.Reflection.CustomReflectionTexture == null)
            {
                Debug.LogError($"[DNPDemo] Environment preset '{preset.name}' selects a custom reflection without a texture.", preset);
                return false;
            }
            return true;
        }

        static ScenarioStage FindStage(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ScenarioStage stage = root.GetComponentInChildren<ScenarioStage>(true);
                if (stage != null)
                {
                    return stage;
                }
            }
            return null;
        }

        void ReleaseSnapshots()
        {
            ReleaseSnapshot(ref _skySnapshotA);
            ReleaseSnapshot(ref _skySnapshotB);
            _activeSkySnapshot = null;
        }

        static void ReleaseSnapshot(ref RenderTexture snapshot)
        {
            if (snapshot == null)
            {
                return;
            }
            snapshot.Release();
            DestroyRuntimeObject(snapshot);
            snapshot = null;
        }

        void RestoreWallMaterials()
        {
            foreach (WallRenderer wall in _wallRenderers)
            {
                if (wall.Renderer != null)
                {
                    wall.Renderer.sharedMaterials = wall.OriginalMaterials;
                }
                for (int i = 0; i < wall.FadeMaterials.Length; i++)
                {
                    Material material = wall.FadeMaterials[i];
                    if (material != null && material != wall.OriginalMaterials[i])
                    {
                        DestroyRuntimeObject(material);
                    }
                }
            }
            _wallRenderers.Clear();
        }

        void OnDestroy()
        {
            _shadowQuality?.Suspend();
            if (_initialized)
            {
                RestoreWallMaterials();
                if (RenderSettings.skybox == _skyboxMaterial)
                {
                    RenderSettings.skybox = _originalSkybox;
                    ApplyLightingState(_originalState);
                }
            }
            ReleaseSnapshots();
            if (_skyboxMaterial != null)
            {
                DestroyRuntimeObject(_skyboxMaterial);
            }
        }

        static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
