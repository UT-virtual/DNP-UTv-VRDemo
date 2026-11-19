using UnityEngine;

namespace DNPDemo.Venus
{
    /// <summary>
    /// One version of the statue (armless, Ver 1.0 … Ver 3.1): the whole-body model shown for it and the name players see.
    /// Every model shares the body's position, scale and origin, so switching versions swaps the model in place.
    /// The objects the steps operate on (zones, goals, pivots) belong to the step definitions, not to the model.
    /// </summary>
    [CreateAssetMenu(menuName = "DNP Demo/Venus Version", fileName = "VenusVersion")]
    public sealed class VenusVersion : ScriptableObject
    {
        [Tooltip("Stable identifier, used in logs and when a version travels over the network. Keep it unchanged when the model is replaced.")]
        [SerializeField] string _id;

        [Tooltip("Name shown to players, e.g. \"Ver 1.0\".")]
        [SerializeField] string _displayName;

        [Tooltip("Whole-body model. Instantiated under VenusStatue with its local pose kept.")]
        [SerializeField] GameObject _model;

        public string Id => _id;
        public string DisplayName => _displayName;
        public GameObject Model => _model;

        /// <summary>Sets every field at once, for the asset builder and tests.</summary>
        internal void Initialize(string id, string displayName, GameObject model)
        {
            _id = id;
            _displayName = displayName;
            _model = model;
        }

        public override string ToString() => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    }
}
