using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Venus
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VenusStatue))]
    public sealed class ModelTransition : MonoBehaviour
    {
        [SerializeField] ModelTransitionProfile _profile;

        VenusStatue _statue;
        GameObject _previousModel;
        GameObject _currentModel;
        readonly List<ModelTransitionMaterial> _previousMaterials = new();
        readonly List<ModelTransitionMaterial> _currentMaterials = new();
        GameObject _particleInstance;
        Bounds? _pendingBounds;
        Bounds _bounds;
        float _elapsed;
        bool _animating;
        float _previousDissolve;
        float _currentDissolve;

        // Resolved from the profile when a transition starts, so that Tick does not hash the names every frame.
        int _dissolveId;
        int _scanLineId;
        int _boundsMinId;
        int _boundsMaxId;

        public bool IsAnimating => _animating;
        internal Bounds ActiveBounds => _bounds;
        internal GameObject ParticleInstance => _particleInstance;
        internal float PreviousDissolveValue => _previousDissolve;
        internal float CurrentDissolveValue => _currentDissolve;

        void Awake()
        {
            _statue = GetComponent<VenusStatue>();
        }

        void OnEnable()
        {
            if (_statue == null)
            {
                _statue = GetComponent<VenusStatue>();
            }
            Subscribe();
        }

        internal void Initialize(VenusStatue statue, ModelTransitionProfile profile)
        {
            Unsubscribe();
            _statue = statue != null ? statue : GetComponent<VenusStatue>();
            _profile = profile;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        // Unity always calls OnDisable before OnDestroy, so this also covers destruction.
        void OnDisable()
        {
            try
            {
                CompleteImmediately();
            }
            finally
            {
                Unsubscribe();
            }
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Overrides the bounds of the next transition only. Null, or no call, uses the bounds of both models' renderers.
        /// </summary>
        public void SetBounds(Bounds? bounds)
        {
            _pendingBounds = bounds;
        }

        public void CompleteImmediately()
        {
            _animating = false;
            try
            {
                DestroyParticle();

                if (_currentModel != null)
                {
                    _currentModel.SetActive(true);
                }

                RestoreMaterials(_previousMaterials);
                RestoreMaterials(_currentMaterials);

                if (_previousModel != null && _previousModel != _currentModel)
                {
                    _previousModel.SetActive(false);
                }
            }
            finally
            {
                _previousModel = null;
                _currentModel = null;
                _elapsed = 0f;
                _pendingBounds = null;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_animating)
            {
                return;
            }
            if (_profile == null || _profile.Duration <= 0f)
            {
                CompleteImmediately();
                return;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            float progress = Mathf.Clamp01(_elapsed / _profile.Duration);
            SetShaderProgress(progress);
            if (progress >= 1f)
            {
                CompleteImmediately();
            }
        }

        void Subscribe()
        {
            if (_statue != null)
            {
                // Removing first keeps a single subscription when OnEnable and Initialize both run.
                _statue.VersionChanged -= OnVersionChanged;
                _statue.VersionChanged += OnVersionChanged;
            }
        }

        void Unsubscribe()
        {
            if (_statue != null)
            {
                _statue.VersionChanged -= OnVersionChanged;
            }
        }

        void OnVersionChanged(VenusVersionChange change)
        {
            Bounds? nextBounds = _pendingBounds;
            if (_animating)
            {
                CompleteImmediately();
            }

            _previousModel = _statue.GetModel(change.Previous);
            _currentModel = _statue.GetModel(change.Current);
            _pendingBounds = null;
            _bounds = nextBounds ?? ComputeFullBounds(_previousModel, _currentModel);

            if (!change.Animate || _profile == null || _profile.TransitionMaterial == null || _profile.Duration <= 0f)
            {
                CompleteImmediately();
                return;
            }

            if (_previousModel != null)
            {
                _previousModel.SetActive(true);
            }

            CaptureMaterials(_previousModel, _previousMaterials);
            CaptureMaterials(_currentModel, _currentMaterials);
            if (_previousMaterials.Count == 0 && _currentMaterials.Count == 0)
            {
                CompleteImmediately();
                return;
            }

            _dissolveId = Shader.PropertyToID(_profile.DissolvePropertyName);
            _scanLineId = Shader.PropertyToID(_profile.ScanLinePropertyName);
            _boundsMinId = Shader.PropertyToID(_profile.BoundsMinPropertyName);
            _boundsMaxId = Shader.PropertyToID(_profile.BoundsMaxPropertyName);

            _elapsed = 0f;
            _animating = true;
            SetShaderProgress(0f);
            SpawnParticle();
        }

        void CaptureMaterials(GameObject model, List<ModelTransitionMaterial> into)
        {
            into.Clear();
            into.AddRange(ModelTransitionMaterial.Capture(
                model,
                _profile.TransitionMaterial,
                _profile.BaseMapPropertyName,
                _profile.BaseColorPropertyName));
        }

        static void RestoreMaterials(List<ModelTransitionMaterial> materials)
        {
            foreach (ModelTransitionMaterial material in materials)
            {
                material.Restore();
            }
            materials.Clear();
        }

        /// <summary>
        /// The old model goes from shown to hidden and the new one from hidden to shown. The shader takes the dissolve as a
        /// cutoff (0 shown, 1 hidden), and the scan line, which runs by itself, fades in and out over the transition.
        /// </summary>
        void SetShaderProgress(float progress)
        {
            _previousDissolve = _previousModel != null ? 1f - progress : 0f;
            _currentDissolve = progress;
            float scanFade = Mathf.Sin(progress * Mathf.PI);
            SetShaderProgress(_previousMaterials, 1f - progress, scanFade);
            SetShaderProgress(_currentMaterials, progress, scanFade);
        }

        void SetShaderProgress(List<ModelTransitionMaterial> materials, float shown, float scanFade)
        {
            foreach (ModelTransitionMaterial material in materials)
            {
                material.SetProperties(
                    _dissolveId,
                    1f - shown,
                    _scanLineId,
                    scanFade,
                    _boundsMinId,
                    _bounds.min,
                    _boundsMaxId,
                    _bounds.max);
            }
        }

        Bounds ComputeFullBounds(GameObject previousModel, GameObject currentModel)
        {
            bool hasBounds = false;
            Bounds bounds = default;
            AddBounds(previousModel, ref hasBounds, ref bounds);
            AddBounds(currentModel, ref hasBounds, ref bounds);
            if (hasBounds)
            {
                return bounds;
            }
            return new Bounds(transform.position, Vector3.one);
        }

        static void AddBounds(GameObject model, ref bool hasBounds, ref Bounds bounds)
        {
            if (model == null)
            {
                return;
            }
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        void SpawnParticle()
        {
            if (_profile.ParticlePrefab == null)
            {
                return;
            }
            _particleInstance = Instantiate(_profile.ParticlePrefab, _bounds.center, Quaternion.identity);
            foreach (ParticleSystem particleSystem in _particleInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Play(true);
            }
        }

        void DestroyParticle()
        {
            if (_particleInstance == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                // Destroy takes effect at the end of the frame; hide the instance so its particles vanish now.
                _particleInstance.SetActive(false);
                Destroy(_particleInstance);
            }
            else
            {
                DestroyImmediate(_particleInstance);
            }
            _particleInstance = null;
        }
    }
}
