using DNPDemo.Scenario;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DNPDemo.Environment
{
    /// <summary>Applies an environment preset and owns its interruptible, scene-wide transition.</summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentController : MonoBehaviour, IStageResettable
    {
        const string ExhibitionRoomKey = "ExhibitionRoom";
        const string PanoramaShaderName = "DNPDemo/Environment Crossfade Panorama";
        const int SnapshotPass = 1;

        static readonly int PanoramaAId = Shader.PropertyToID("_PanoramaA");
        static readonly int PanoramaBId = Shader.PropertyToID("_PanoramaB");
        static readonly int BlendId = Shader.PropertyToID("_Blend");
        static readonly int ExposureAId = Shader.PropertyToID("_ExposureA");
        static readonly int ExposureBId = Shader.PropertyToID("_ExposureB");
        static readonly int RotationAId = Shader.PropertyToID("_RotationA");
        static readonly int RotationBId = Shader.PropertyToID("_RotationB");

        [SerializeField] EnvironmentPreset _baselinePreset;
        [SerializeField] Light _directionalLight;
        [Tooltip("The statue spotlight. Its placement and whether it is on belong to the prefab and the scenario cues; presets change only its colour, brightness and shadows.")]
        [SerializeField] Light _spotLight;
        [SerializeField] Shader _crossfadeShader;
        [SerializeField, Min(0f)] float _transitionDuration = 2f;

        struct RuntimeState
        {
            public Color DirectionalColor;
            public float DirectionalIntensity;
            public Quaternion DirectionalRotation;
            public LightShadows DirectionalShadows;
            public float DirectionalShadowStrength;
            public float DirectionalShadowBias;
            public float DirectionalShadowNormalBias;
            public float DirectionalShadowNearPlane;
            public Color SpotColor;
            public float SpotIntensity;
            public LightShadows SpotShadows;
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
            public float RoomAlpha;

            public static RuntimeState FromPreset(EnvironmentPreset preset)
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
                    DirectionalShadowStrength = directional.ShadowStrength,
                    DirectionalShadowBias = directional.ShadowBias,
                    DirectionalShadowNormalBias = directional.ShadowNormalBias,
                    DirectionalShadowNearPlane = directional.ShadowNearPlane,
                    SpotColor = spot.Color,
                    SpotIntensity = spot.Intensity,
                    SpotShadows = spot.Shadows,
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
                    RoomAlpha = preset.ExhibitionRoomVisible ? 1f : 0f,
                };
            }

            /// <summary>Interpolates the continuous values; discrete settings switch halfway.</summary>
            public static RuntimeState Lerp(RuntimeState from, RuntimeState to, float t)
            {
                bool second = t >= 0.5f;
                return new RuntimeState
                {
                    DirectionalColor = Color.Lerp(from.DirectionalColor, to.DirectionalColor, t),
                    DirectionalIntensity = Mathf.Lerp(from.DirectionalIntensity, to.DirectionalIntensity, t),
                    DirectionalRotation = Quaternion.Slerp(from.DirectionalRotation, to.DirectionalRotation, t),
                    DirectionalShadows = second ? to.DirectionalShadows : from.DirectionalShadows,
                    DirectionalShadowStrength = Mathf.Lerp(from.DirectionalShadowStrength, to.DirectionalShadowStrength, t),
                    DirectionalShadowBias = Mathf.Lerp(from.DirectionalShadowBias, to.DirectionalShadowBias, t),
                    DirectionalShadowNormalBias = Mathf.Lerp(from.DirectionalShadowNormalBias, to.DirectionalShadowNormalBias, t),
                    DirectionalShadowNearPlane = Mathf.Lerp(from.DirectionalShadowNearPlane, to.DirectionalShadowNearPlane, t),
                    SpotColor = Color.Lerp(from.SpotColor, to.SpotColor, t),
                    SpotIntensity = Mathf.Lerp(from.SpotIntensity, to.SpotIntensity, t),
                    SpotShadows = second ? to.SpotShadows : from.SpotShadows,
                    SpotShadowStrength = Mathf.Lerp(from.SpotShadowStrength, to.SpotShadowStrength, t),
                    SpotShadowBias = Mathf.Lerp(from.SpotShadowBias, to.SpotShadowBias, t),
                    SpotShadowNormalBias = Mathf.Lerp(from.SpotShadowNormalBias, to.SpotShadowNormalBias, t),
                    SpotShadowNearPlane = Mathf.Lerp(from.SpotShadowNearPlane, to.SpotShadowNearPlane, t),
                    AmbientMode = second ? to.AmbientMode : from.AmbientMode,
                    AmbientSky = Color.Lerp(from.AmbientSky, to.AmbientSky, t),
                    AmbientEquator = Color.Lerp(from.AmbientEquator, to.AmbientEquator, t),
                    AmbientGround = Color.Lerp(from.AmbientGround, to.AmbientGround, t),
                    AmbientFlat = Color.Lerp(from.AmbientFlat, to.AmbientFlat, t),
                    AmbientIntensity = Mathf.Lerp(from.AmbientIntensity, to.AmbientIntensity, t),
                    ReflectionMode = second ? to.ReflectionMode : from.ReflectionMode,
                    CustomReflection = second ? to.CustomReflection : from.CustomReflection,
                    ReflectionIntensity = Mathf.Lerp(from.ReflectionIntensity, to.ReflectionIntensity, t),
                    RoomAlpha = Mathf.Lerp(from.RoomAlpha, to.RoomAlpha, t),
                };
            }
        }

        RendererFadeGroup _roomFade;
        Material _skyboxMaterial;
        RenderTexture _skySnapshotA;
        RenderTexture _skySnapshotB;
        RenderTexture _activeSkySnapshot;
        Material _originalSkybox;
        RuntimeState _originalState;
        RuntimeState _transitionStart;
        EnvironmentPreset _transitionTarget;
        ScenarioStage _stage;
        float _elapsed;
        float _duration;
        bool _initialized;
        bool _isTransitioning;
        bool _reportedMissingRoom;

        public bool IsTransitioning => _isTransitioning;
        public float TransitionDuration => _transitionDuration;

        void Update() => AdvanceTransition(Time.unscaledDeltaTime);

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

            CaptureCurrentSky();
            _transitionStart = CaptureCurrentState();
            _transitionTarget = preset;
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
            _isTransitioning = false;
            _transitionTarget = null;
            _elapsed = 0f;
            ApplyState(RuntimeState.FromPreset(preset));
            SetSkyboxPreset(preset);
            ReleaseSnapshots();
        }

        /// <summary>Advances the transition clock. Internal so EditMode tests can drive it deterministically.</summary>
        internal void AdvanceTransition(float deltaTime)
        {
            if (!_isTransitioning)
            {
                return;
            }
            _elapsed = Mathf.Min(_elapsed + Mathf.Max(0f, deltaTime), _duration);
            if (_elapsed >= _duration)
            {
                ApplyInstant(_transitionTarget);
                return;
            }
            float progress = Mathf.SmoothStep(0f, 1f, _elapsed / _duration);
            ApplyState(RuntimeState.Lerp(_transitionStart, RuntimeState.FromPreset(_transitionTarget), progress));
            _skyboxMaterial.SetFloat(BlendId, progress);
        }

        void IStageResettable.ResetToBaseline()
        {
            ApplyInstant(_baselinePreset);
        }

        /// <summary>
        /// Renders the sky currently on screen, including a blend in progress, into an equirectangular snapshot, so a new
        /// transition starts from exactly what is displayed.
        /// </summary>
        void CaptureCurrentSky()
        {
            Texture currentSource = _skyboxMaterial.GetTexture(PanoramaAId);
            bool useB = currentSource != null && currentSource == _skySnapshotA;
            ref RenderTexture snapshot = ref useB ? ref _skySnapshotB : ref _skySnapshotA;
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
            }
            // Blit leaves the snapshot as the active render target, which must not stay active when it is released.
            RenderTexture previousActive = RenderTexture.active;
            Graphics.Blit(null, snapshot, _skyboxMaterial, SnapshotPass);
            RenderTexture.active = previousActive;
            _activeSkySnapshot = snapshot;
        }

        /// <summary>The snapshot already contains its exposure and rotation; the target is drawn as it will finally look.</summary>
        void ConfigureSkyTransition(EnvironmentPreset preset)
        {
            _skyboxMaterial.SetTexture(PanoramaAId, _activeSkySnapshot);
            _skyboxMaterial.SetFloat(ExposureAId, 0f);
            _skyboxMaterial.SetFloat(RotationAId, 0f);
            _skyboxMaterial.SetTexture(PanoramaBId, preset.Panorama);
            _skyboxMaterial.SetFloat(ExposureBId, preset.Exposure);
            _skyboxMaterial.SetFloat(RotationBId, preset.Rotation);
            _skyboxMaterial.SetFloat(BlendId, 0f);
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
        }

        RuntimeState CaptureCurrentState()
        {
            return new RuntimeState
            {
                DirectionalColor = _directionalLight.color,
                DirectionalIntensity = _directionalLight.intensity,
                DirectionalRotation = _directionalLight.transform.rotation,
                DirectionalShadows = _directionalLight.shadows,
                DirectionalShadowStrength = _directionalLight.shadowStrength,
                DirectionalShadowBias = _directionalLight.shadowBias,
                DirectionalShadowNormalBias = _directionalLight.shadowNormalBias,
                DirectionalShadowNearPlane = _directionalLight.shadowNearPlane,
                SpotColor = _spotLight.color,
                SpotIntensity = _spotLight.intensity,
                SpotShadows = _spotLight.shadows,
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
                RoomAlpha = _roomFade != null ? _roomFade.Alpha : 1f,
            };
        }

        void ApplyState(RuntimeState state)
        {
            ApplyLighting(state);
            if (EnsureRoomFade())
            {
                _roomFade.SetAlpha(state.RoomAlpha);
            }
        }

        void ApplyLighting(RuntimeState state)
        {
            if (_directionalLight != null)
            {
                _directionalLight.color = state.DirectionalColor;
                _directionalLight.intensity = state.DirectionalIntensity;
                _directionalLight.transform.rotation = state.DirectionalRotation;
                _directionalLight.shadows = state.DirectionalShadows;
                _directionalLight.shadowStrength = state.DirectionalShadowStrength;
                _directionalLight.shadowBias = state.DirectionalShadowBias;
                _directionalLight.shadowNormalBias = state.DirectionalShadowNormalBias;
                _directionalLight.shadowNearPlane = state.DirectionalShadowNearPlane;
            }
            if (_spotLight != null)
            {
                _spotLight.color = state.SpotColor;
                _spotLight.intensity = state.SpotIntensity;
                _spotLight.shadows = state.SpotShadows;
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
        }

        bool EnsureRoomFade()
        {
            if (_roomFade != null)
            {
                return true;
            }
            if (_stage == null)
            {
                _stage = FindStage(gameObject.scene);
            }
            if (_stage == null || !_stage.TryGetObject(ExhibitionRoomKey, out GameObject room))
            {
                if (!_reportedMissingRoom)
                {
                    Debug.LogWarning($"[DNPDemo] 展示室の表示切り替えに必要なキー「{ExhibitionRoomKey}」がシーンにありません。", this);
                    _reportedMissingRoom = true;
                }
                return false;
            }
            _roomFade = new RendererFadeGroup(room, this);
            return true;
        }

        bool EnsureInitialized()
        {
            if (_initialized)
            {
                return true;
            }
            if (_directionalLight == null || _spotLight == null)
            {
                Debug.LogError("[DNPDemo] EnvironmentController needs a directional light and a spot light.", this);
                return false;
            }
            if (!ValidatePreset(_baselinePreset))
            {
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
            _skyboxMaterial = new Material(_crossfadeShader)
            {
                name = "Environment Crossfade Runtime",
                hideFlags = HideFlags.DontSave,
            };
            RenderSettings.skybox = _skyboxMaterial;
            _stage = FindStage(gameObject.scene);
            _initialized = true;
            ApplyState(RuntimeState.FromPreset(_baselinePreset));
            SetSkyboxPreset(_baselinePreset);
            return true;
        }

        bool ValidatePreset(EnvironmentPreset preset)
        {
            if (preset == null)
            {
                Debug.LogError("[DNPDemo] No environment preset was given.", this);
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

        void OnDestroy()
        {
            if (_initialized)
            {
                _roomFade?.Dispose();
                _roomFade = null;
                if (RenderSettings.skybox == _skyboxMaterial)
                {
                    RenderSettings.skybox = _originalSkybox;
                    ApplyLighting(_originalState);
                }
            }
            ReleaseSnapshots();
            if (_skyboxMaterial != null)
            {
                DestroyRuntimeObject(_skyboxMaterial);
            }
        }

        static void DestroyRuntimeObject(Object target)
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
