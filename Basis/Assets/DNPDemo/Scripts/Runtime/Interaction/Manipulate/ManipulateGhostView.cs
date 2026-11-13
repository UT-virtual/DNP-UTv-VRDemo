using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Interaction.Manipulate
{
    /// <summary>
    /// Draws a translucent copy (the ghost) of a manipulated object at the pose it should reach. The meshes of the
    /// object's visible <see cref="MeshRenderer"/>s are drawn every frame with <see cref="Graphics.RenderMesh"/>, so no
    /// objects are created and the object itself is left untouched.
    /// </summary>
    public sealed class ManipulateGhostView : MonoBehaviour
    {
        readonly struct Piece
        {
            public readonly Mesh Mesh;
            public readonly int SubMeshCount;
            public readonly int Layer;

            /// <summary>The renderer's transform relative to the object's position and rotation (scale kept in the piece).</summary>
            public readonly Matrix4x4 Local;

            public Piece(Mesh mesh, int layer, Matrix4x4 local)
            {
                Mesh = mesh;
                SubMeshCount = mesh.subMeshCount;
                Layer = layer;
                Local = local;
            }
        }

        [Tooltip("Translucent material the ghost is drawn with.")]
        [SerializeField] Material _material;

        readonly List<Piece> _pieces = new();
        readonly List<MeshFilter> _filters = new();
        Matrix4x4 _pose;
        bool _visible;

        public bool IsVisible => _visible;

        /// <summary>Shows a ghost of <paramref name="target"/> at <paramref name="pose"/> (world space).</summary>
        public void Show(Transform target, Pose pose)
        {
            Hide();
            if (target == null)
            {
                return;
            }
            if (_material == null)
            {
                Debug.LogError("[DNPDemo] ManipulateGhostView: マテリアルが設定されていません。", this);
                return;
            }
            Matrix4x4 toTarget = Matrix4x4.TRS(target.position, target.rotation, Vector3.one).inverse;
            target.GetComponentsInChildren(false, _filters);
            foreach (MeshFilter filter in _filters)
            {
                if (filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer renderer) || !renderer.enabled)
                {
                    continue;
                }
                _pieces.Add(new Piece(filter.sharedMesh, filter.gameObject.layer, toTarget * filter.transform.localToWorldMatrix));
            }
            _filters.Clear();
            _pose = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
            _visible = _pieces.Count > 0;
        }

        public void Hide()
        {
            _pieces.Clear();
            _visible = false;
        }

        void LateUpdate()
        {
            if (!_visible)
            {
                return;
            }
            foreach (Piece piece in _pieces)
            {
                if (piece.Mesh == null)
                {
                    continue;
                }
                var parameters = new RenderParams(_material)
                {
                    layer = piece.Layer,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                };
                Matrix4x4 matrix = _pose * piece.Local;
                for (int i = 0; i < piece.SubMeshCount; i++)
                {
                    Graphics.RenderMesh(parameters, piece.Mesh, i, matrix);
                }
            }
        }

        void OnDisable() => Hide();
    }
}
