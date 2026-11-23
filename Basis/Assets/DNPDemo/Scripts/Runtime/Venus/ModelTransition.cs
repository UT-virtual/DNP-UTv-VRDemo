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
        VenusVersion _previous;
        VenusVersion _current;
        GameObject _previousModel;
        GameObject _currentModel;
        readonly List<ModelTransitionMaterial> _materials = new();
        readonly List<ModelTransitionMaterial> _previousMaterials = new();
        readonly List<ModelTransitionMaterial> _currentMaterials = new();
        GameObject _particleInstance;
        Bounds? _pendingBounds;
        bool _hasPendingBounds;
        Bounds _bounds;
        float _elapsed;
        bool _animating;
        float _previousDissolve;
        float _currentDissolve;

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
            if (_statue != null)
            {
                _statue.VersionChanged -= OnVersionChanged;
                _statue.VersionChanged += OnVersionChanged;
            }
        }

        internal void Initialize(VenusStatue statue, ModelTransitionProfile profile)
        {
            if (_statue != null)
            {
                _statue.VersionChanged -= OnVersionChanged;
            }
            _statue = statue != null ? statue : GetComponent<VenusStatue>();
            _profile = profile;
            if (isActiveAndEnabled && _statue != null)
            {
                _statue.VersionChanged -= OnVersionChanged;
                _statue.VersionChanged += OnVersionChanged;
            }
        }

        void OnDisable()
        {
            _animating = false;
            try
            {
                CompleteImmediately();
            }
            finally
            {
                _animating = false;
                if (_statue != null)
                {
                    _statue.VersionChanged -= OnVersionChanged;
                }
            }
        }

        void OnDestroy()
        {
            _animating = false;
            try
            {
                CompleteImmediately();
            }
            finally
            {
                _animating = false;
                if (_statue != null)
                {
                    _statue.VersionChanged -= OnVersionChanged;
                }
            }
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void SetBounds(Bounds? bounds)
        {
            _pendingBounds = bounds;
            _hasPendingBounds = true;
        }

        public void CompleteImmediately()
        {
            _animating = false;
            try
            {
                if (_particleInstance != null)
                {
                    foreach (ParticleSystem particleSystem in _particleInstance.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
                DestroyParticle();

                if (_currentModel != null)
                {
                    _currentModel.SetActive(true);
                }

                foreach (ModelTransitionMaterial material in _materials)
                {
                    material.Restore();
                }
                _materials.Clear();
                _previousMaterials.Clear();
                _currentMaterials.Clear();

                if (_previousModel != null && _previousModel != _currentModel)
                {
                    _previousModel.SetActive(false);
                }
            }
            finally
            {
                _previous = null;
                _current = null;
                _previousModel = null;
                _currentModel = null;
                _elapsed = 0f;
                _animating = false;
                _pendingBounds = null;
                _hasPendingBounds = false;
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

        void OnVersionChanged(VenusVersionChange change)
        {
            if (_statue == null)
            {
                _statue = GetComponent<VenusStatue>();
            }
            if (_statue == null)
            {
                CompleteImmediately();
                return;
            }

            Bounds? nextBounds = _hasPendingBounds ? _pendingBounds : null;
            if (_animating)
            {
                CompleteImmediately();
            }

            _previous = change.Previous;
            _current = change.Current;
            _previousModel = _statue.GetModel(_previous);
            _currentModel = _statue.GetModel(_current);

            _pendingBounds = null;
            _hasPendingBounds = false;
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

            _materials.Clear();
            _previousMaterials.Clear();
            _currentMaterials.Clear();
            if (_previousModel != null)
            {
                List<ModelTransitionMaterial> previousMaterials = ModelTransitionMaterial.Capture(
                    _previousModel,
                    _profile.TransitionMaterial,
                    _profile.BaseMapPropertyName,
                    _profile.BaseColorPropertyName);
                _previousMaterials.AddRange(previousMaterials);
                _materials.AddRange(previousMaterials);
            }
            if (_currentModel != null)
            {
                List<ModelTransitionMaterial> currentMaterials = ModelTransitionMaterial.Capture(
                    _currentModel,
                    _profile.TransitionMaterial,
                    _profile.BaseMapPropertyName,
                    _profile.BaseColorPropertyName);
                _currentMaterials.AddRange(currentMaterials);
                _materials.AddRange(currentMaterials);
            }

            if (_materials.Count == 0)
            {
                CompleteImmediately();
                return;
            }

            _elapsed = 0f;
            _animating = true;
            SetShaderProgress(0f);
            SpawnParticle();
        }

        void SetShaderProgress(float progress)
        {
            _previousDissolve = _previousModel != null ? 1f - progress : 0f;
            _currentDissolve = progress;

            int dissolveId = Shader.PropertyToID(_profile.DissolvePropertyName);
            int scanLineId = Shader.PropertyToID(_profile.ScanLinePropertyName);
            int boundsMinId = Shader.PropertyToID(_profile.BoundsMinPropertyName);
            int boundsMaxId = Shader.PropertyToID(_profile.BoundsMaxPropertyName);

            foreach (ModelTransitionMaterial material in _previousMaterials)
            {
                material.SetProperties(
                    dissolveId,
                    1f - progress,
                    scanLineId,
                    progress,
                    boundsMinId,
                    _bounds.min,
                    boundsMaxId,
                    _bounds.max);
            }

            foreach (ModelTransitionMaterial material in _currentMaterials)
            {
                material.SetProperties(
                    dissolveId,
                    progress,
                    scanLineId,
                    progress,
                    boundsMinId,
                    _bounds.min,
                    boundsMaxId,
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
