using UnityEngine;

namespace DNPDemo.Venus
{
    [CreateAssetMenu(menuName = "DNP Demo/Model Transition Profile", fileName = "ModelTransitionProfile")]
    public sealed class ModelTransitionProfile : ScriptableObject
    {
        /// <summary>Properties of DNPDemo/ModelTransition that <see cref="ModelTransition"/> drives.</summary>
        public const string DefaultDissolvePropertyName = "_Cutoff";
        public const string DefaultScanLinePropertyName = "_ScanFade";

        [Header("Temporary effects")]
        [Tooltip("Shared transition material used for both dissolving models.")]
        [SerializeField] Material _transitionMaterial;

        [Tooltip("Optional particle prefab played at the transition bounds.")]
        [SerializeField] GameObject _particlePrefab;

        [Min(0f)]
        [Tooltip("Seconds for one complete transition.")]
        [SerializeField] float _duration = 1f;

        [Header("Shader properties")]
        [Tooltip("Dissolve cutoff set every frame: 0 shows the model, 1 hides it.")]
        [SerializeField] string _dissolvePropertyName = DefaultDissolvePropertyName;
        [Tooltip("Scan line strength set every frame: rises from 0 to 1 and back over the transition.")]
        [SerializeField] string _scanLinePropertyName = DefaultScanLinePropertyName;
        [SerializeField] string _boundsMinPropertyName = "_BoundsMin";
        [SerializeField] string _boundsMaxPropertyName = "_BoundsMax";
        [SerializeField] string _baseMapPropertyName = "_BaseMap";
        [SerializeField] string _baseColorPropertyName = "_BaseColor";

        public Material TransitionMaterial => _transitionMaterial;
        public GameObject ParticlePrefab => _particlePrefab;
        public float Duration => _duration;
        public string DissolvePropertyName => string.IsNullOrWhiteSpace(_dissolvePropertyName) ? DefaultDissolvePropertyName : _dissolvePropertyName;
        public string ScanLinePropertyName => string.IsNullOrWhiteSpace(_scanLinePropertyName) ? DefaultScanLinePropertyName : _scanLinePropertyName;
        public string BoundsMinPropertyName => string.IsNullOrWhiteSpace(_boundsMinPropertyName) ? "_BoundsMin" : _boundsMinPropertyName;
        public string BoundsMaxPropertyName => string.IsNullOrWhiteSpace(_boundsMaxPropertyName) ? "_BoundsMax" : _boundsMaxPropertyName;
        public string BaseMapPropertyName => string.IsNullOrWhiteSpace(_baseMapPropertyName) ? "_BaseMap" : _baseMapPropertyName;
        public string BaseColorPropertyName => string.IsNullOrWhiteSpace(_baseColorPropertyName) ? "_BaseColor" : _baseColorPropertyName;

        internal void Initialize(Material transitionMaterial, GameObject particlePrefab, float duration)
        {
            _transitionMaterial = transitionMaterial;
            _particlePrefab = particlePrefab;
            _duration = duration;
        }
    }
}
