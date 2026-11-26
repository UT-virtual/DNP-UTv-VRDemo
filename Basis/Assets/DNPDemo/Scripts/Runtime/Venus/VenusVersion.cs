using UnityEngine;

namespace DNPDemo.Venus
{
    /// <summary>
    /// One version of the statue (armless, Ver 1.0 … Ver 3.1): the whole-body model shown for it and the name players see.
    /// Every model shares the body's position, scale and origin, so switching versions swaps the model in place.
    /// The objects the steps operate on (zones, goals, pivots) belong to the step definitions, not to the model.
    /// <para>
    /// <see cref="Parent"/>, <see cref="StartsBranch"/> and <see cref="Thumbnail"/> are for the branch view of the history
    /// panel: the parent is the version this one was made from, a version that starts a branch (another person's take on
    /// the parent) is drawn on a row of its own, and the thumbnail is an image of the model made in the editor
    /// (DNP Demo/Create/Venus Thumbnails), to be made again whenever the model is replaced.
    /// </para>
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

        [Tooltip("Optional model for the main statue when the scenario displays an interactive prop separately.")]
        [SerializeField] GameObject _stageModel;

        [Tooltip("Version this one was made from, drawn as its parent in the history panel's branch view. Empty for the original. It has to come earlier in the catalog.")]
        [SerializeField] VenusVersion _parent;

        [Tooltip("True when this version starts a new branch off its parent instead of continuing the parent's line. Drawn on a new row of the history panel's branch view.")]
        [SerializeField] bool _startsBranch;

        [Tooltip("Image of the model for the history panel's branch view. Made by DNP Demo/Create/Venus Thumbnails; make it again after replacing the model.")]
        [SerializeField] Texture2D _thumbnail;

        public string Id => _id;
        public string DisplayName => _displayName;
        public GameObject Model => _model;

        internal GameObject GetModelPrefab(bool useStageModel) =>
            useStageModel && _stageModel != null ? _stageModel : _model;

        /// <summary>The version this one was made from, or null for the original.</summary>
        public VenusVersion Parent => _parent;

        /// <summary>True when this version starts a new branch off <see cref="Parent"/> instead of continuing its line.</summary>
        public bool StartsBranch => _startsBranch;

        /// <summary>Image of the model for the branch view, or null when it has not been made.</summary>
        public Texture2D Thumbnail => _thumbnail;

        /// <summary>Sets every field at once, for the asset builder and tests.</summary>
        internal void Initialize(string id, string displayName, GameObject model, VenusVersion parent = null, bool startsBranch = false)
        {
            _id = id;
            _displayName = displayName;
            _model = model;
            _parent = parent;
            _startsBranch = startsBranch;
        }

        /// <summary>For the thumbnail builder and tests.</summary>
        internal void SetThumbnail(Texture2D thumbnail) => _thumbnail = thumbnail;

        public override string ToString() => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    }
}
