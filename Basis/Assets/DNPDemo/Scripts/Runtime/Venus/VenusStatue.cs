using System;
using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Venus
{
    /// <summary>
    /// A statue that shows one <see cref="VenusVersion"/> at a time: the main statue, or the one on the comparison table.
    /// The model of every version in <see cref="Catalog"/> is instantiated hidden when the statue wakes, and
    /// <see cref="Show"/> only switches which one is active, so no instantiation happens while the demo runs.
    /// <para>
    /// The version follows from the step index: the scenario's <see cref="Scenario.Cues.StatueVersionCue"/> addresses the
    /// statue by the key of its <see cref="ScenarioObjectBinding"/>, and the stage resets it to <see cref="Baseline"/>
    /// (<see cref="IStageResettable"/>) before the cues of earlier steps are applied again.
    /// </para>
    /// </summary>
    public sealed class VenusStatue : MonoBehaviour, IStageResettable
    {
        [Tooltip("Versions whose models are built up front. A version outside it is built the first time it is shown.")]
        [SerializeField] VenusVersionCatalog _catalog;

        [Tooltip("Version shown at scene load and whenever the stage resets, including before the scenario runs. Empty shows nothing.")]
        [SerializeField] VenusVersion _baseline;

        [Tooltip("Parent of the version models. Empty uses this transform.")]
        [SerializeField] Transform _modelRoot;

        [Tooltip("Use versions' stage models so the scenario can display and move their props separately.")]
        [SerializeField] bool _useStageModels;

        [Tooltip("Version drawn in the scene view while not playing, to place zones and goals against. Not shown at runtime.")]
        [SerializeField] VenusVersion _editorPreview;

        readonly Dictionary<VenusVersion, GameObject> _models = new();

        public VenusVersionCatalog Catalog => _catalog;

        /// <summary>The version shown at scene load and after a stage reset; null for nothing.</summary>
        public VenusVersion Baseline => _baseline;

        /// <summary>The version shown now; null when the statue shows nothing.</summary>
        public VenusVersion Current { get; private set; }

        /// <summary>Raised after <see cref="Show"/> has changed <see cref="Current"/>, and after every stage reset even when the baseline was already shown.</summary>
        public event Action<VenusVersionChange> VersionChanged;

        Transform ModelRoot => _modelRoot != null ? _modelRoot : transform;

        void Awake()
        {
            if (_catalog != null)
            {
                foreach (VenusVersion version in _catalog.Versions)
                {
                    GetOrCreateModel(version);
                }
            }
            Show(_baseline, false);
        }

        /// <summary>
        /// Shows <paramref name="version"/>, or nothing when it is null. <paramref name="animate"/> tells listeners of
        /// <see cref="VersionChanged"/> whether this is a scripted change worth a transition (a cue playing) or a rebuild
        /// of the step's state (a jump, a late join, a reset); the models themselves switch at once.
        /// </summary>
        public void Show(VenusVersion version, bool animate)
        {
            if (version == Current)
            {
                return;
            }
            Switch(version, animate);
        }

        /// <summary>The instance of <paramref name="version"/>'s model under this statue, or null when it has no model.</summary>
        public GameObject GetModel(VenusVersion version) =>
            version != null && _models.TryGetValue(version, out GameObject model) ? model : null;

        // Unlike Show, a reset raises VersionChanged even when the baseline is already Current, so that a
        // listener still playing a transition into the baseline finishes it.
        void IStageResettable.ResetToBaseline() => Switch(_baseline, false);

        /// <summary>Activates only <paramref name="version"/>'s model, updates <see cref="Current"/> and raises <see cref="VersionChanged"/>.</summary>
        void Switch(VenusVersion version, bool animate)
        {
            VenusVersion previous = Current;
            GameObject next = version != null ? GetOrCreateModel(version) : null;
            foreach (KeyValuePair<VenusVersion, GameObject> pair in _models)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetActive(pair.Value == next);
                }
            }
            Current = version;
            VersionChanged?.Invoke(new VenusVersionChange(previous, version, animate));
        }

        GameObject GetOrCreateModel(VenusVersion version)
        {
            if (version == null)
            {
                return null;
            }
            if (_models.TryGetValue(version, out GameObject existing) && existing != null)
            {
                return existing;
            }
            GameObject prefab = version.GetModelPrefab(_useStageModels);
            if (prefab == null)
            {
                Debug.LogWarning($"[DNPDemo] {version.name} にモデルが設定されていません。", this);
                _models[version] = null;
                return null;
            }
            GameObject model = Instantiate(prefab, ModelRoot, false);
            model.name = prefab.name;
            model.SetActive(version == Current);
            _models[version] = model;
            return model;
        }

        void OnDrawGizmos()
        {
            GameObject model = _editorPreview != null ? _editorPreview.GetModelPrefab(_useStageModels) : null;
            if (Application.isPlaying || model == null)
            {
                return;
            }
            Matrix4x4 root = ModelRoot.localToWorldMatrix;
            Gizmos.color = new Color(0.85f, 0.85f, 0.8f, 0.6f);
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }
                // A prefab asset's root has no parent, so its localToWorldMatrix is its pose relative to the statue.
                Gizmos.matrix = root * filter.transform.localToWorldMatrix;
                Gizmos.DrawMesh(filter.sharedMesh);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    /// <summary>A change of the version a <see cref="VenusStatue"/> shows.</summary>
    public readonly struct VenusVersionChange
    {
        public VenusVersionChange(VenusVersion previous, VenusVersion current, bool animate)
        {
            Previous = previous;
            Current = current;
            Animate = animate;
        }

        public VenusVersion Previous { get; }
        public VenusVersion Current { get; }

        /// <summary>True when a cue played the change; false when the step's state was rebuilt (jump, late join, reset).</summary>
        public bool Animate { get; }
    }
}
