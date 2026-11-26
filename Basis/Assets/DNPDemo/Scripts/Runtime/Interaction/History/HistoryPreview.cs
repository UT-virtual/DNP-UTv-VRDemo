using System.Collections.Generic;
using DNPDemo.Venus;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// The turning miniature of a version on the history panel. A <see cref="VenusStatue"/> under this transform builds
    /// every version's model up front like the statues on stage, so switching versions never instantiates; this
    /// component scales it down and turns it about the vertical axis.
    /// <para>
    /// Every version shares the body's origin and scale (<see cref="VenusVersion"/>), so one scale and offset, taken from
    /// the first version of the catalog, fit all of them and the body keeps its size while stepping through the history.
    /// </para>
    /// <para>
    /// The model stands behind the panel. Its renderers draw with copies of their materials moved to
    /// <see cref="HistoryRenderQueues.Preview"/>, after the panel background, so the background colour never covers it
    /// (see <see cref="HistoryRenderQueues"/>). They cast no shadows, so the miniature does not shade the room.
    /// </para>
    /// </summary>
    public sealed class HistoryPreview : MonoBehaviour
    {
        [Tooltip("Statue that holds the models. Its transform is scaled and moved so the model stands centred on this one.")]
        [SerializeField] VenusStatue _statue;

        [Tooltip("Height in metres of the first version of the catalog when shown.")]
        [SerializeField, Min(0.05f)] float _height = 0.26f;

        [Tooltip("Turning speed about the vertical axis, in degrees per second. 0 keeps the model still.")]
        [SerializeField] float _spinDegreesPerSecond = 30f;

        readonly Dictionary<Material, Material> _previewMaterials = new();
        bool _prepared;

        void Awake()
        {
            enabled = false;
        }

        // The statue builds its models in its Awake, which has run for every object of the scene by now.
        void Start() => Prepare();

        void OnDestroy()
        {
            foreach (Material material in _previewMaterials.Values)
            {
                Destroy(material);
            }
            _previewMaterials.Clear();
        }

        void Update()
        {
            if (_spinDegreesPerSecond != 0f)
            {
                transform.Rotate(0f, _spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
            }
        }

        /// <summary>Shows <paramref name="version"/>, or nothing when it is null. The model turns only while one is shown.</summary>
        public void Show(VenusVersion version)
        {
            if (_statue == null)
            {
                return;
            }
            // The panel hides the preview from its own Awake, possibly before the statue has built the models.
            if (version != null)
            {
                Prepare();
            }
            if (version != null && _statue.Current == null)
            {
                // Each time the panel opens, start with the model's front towards the viewer.
                transform.localRotation = Quaternion.identity;
            }
            _statue.Show(version, false);
            enabled = version != null;
        }

        void Prepare()
        {
            if (_prepared || _statue == null)
            {
                return;
            }
            _prepared = true;
            foreach (Renderer renderer in _statue.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = GetPreviewMaterial(materials[i]);
                }
                renderer.sharedMaterials = materials;
            }
            Fit();
        }

        /// <summary>A copy of <paramref name="source"/> drawn in the preview's queue, one per source material.</summary>
        Material GetPreviewMaterial(Material source)
        {
            if (source == null)
            {
                return null;
            }
            if (!_previewMaterials.TryGetValue(source, out Material copy))
            {
                copy = new Material(source) { name = source.name + " (History Preview)", renderQueue = HistoryRenderQueues.Preview };
                _previewMaterials.Add(source, copy);
            }
            return copy;
        }

        void Fit()
        {
            VenusVersionCatalog catalog = _statue.Catalog;
            if (catalog == null)
            {
                return;
            }
            foreach (VenusVersion version in catalog.Versions)
            {
                if (TryMeasure(_statue.GetModel(version), _statue.transform, out Bounds bounds) && bounds.size.y > 1e-4f)
                {
                    float scale = _height / bounds.size.y;
                    _statue.transform.localScale = Vector3.one * scale;
                    _statue.transform.localPosition = -bounds.center * scale;
                    return;
                }
            }
        }

        /// <summary>
        /// Bounds of <paramref name="model"/>'s meshes in the local space of <paramref name="space"/>, inactive objects
        /// included (a hidden model's renderers report no world bounds).
        /// </summary>
        internal static bool TryMeasure(GameObject model, Transform space, out Bounds bounds)
        {
            bounds = default;
            if (model == null || space == null)
            {
                return false;
            }
            bool found = false;
            Matrix4x4 toSpace = space.worldToLocalMatrix;
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    Encapsulate(toSpace * filter.transform.localToWorldMatrix, filter.sharedMesh.bounds, ref found, ref bounds);
                }
            }
            foreach (SkinnedMeshRenderer skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh != null)
                {
                    Encapsulate(toSpace * skinned.transform.localToWorldMatrix, skinned.sharedMesh.bounds, ref found, ref bounds);
                }
            }
            return found;
        }

        static void Encapsulate(Matrix4x4 matrix, Bounds local, ref bool found, ref Bounds bounds)
        {
            Vector3 min = local.min;
            Vector3 max = local.max;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                if (!found)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }
    }
}
