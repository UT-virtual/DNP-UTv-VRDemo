using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Interaction.Manipulate
{
    /// <summary>
    /// Draws a translucent copy (the ghost) of a manipulated object at the pose it should reach. The meshes of the
    /// object's visible <see cref="MeshRenderer"/>s are drawn every frame with <see cref="Graphics.RenderMesh"/>, so no
    /// objects are created and the object itself is left untouched.
    /// <para>
    /// Each frame the pieces are placed as they sit on the object at that moment (relative to its position and
    /// rotation), so the ghost follows changes to the object's look, such as the hand mirror growing in as step 14 begins.
    /// </para>
    /// </summary>
    public sealed class ManipulateGhostView : MonoBehaviour
    {
        readonly struct Piece
        {
            public readonly Transform Transform;
            public readonly Mesh Mesh;
            public readonly int SubMeshCount;
            public readonly int Layer;

            public Piece(Transform transform, Mesh mesh, int layer)
            {
                Transform = transform;
                Mesh = mesh;
                SubMeshCount = mesh.subMeshCount;
                Layer = layer;
            }
        }

        [Tooltip("Translucent material the ghost is drawn with.")]
        [SerializeField] Material _material;

        readonly List<Piece> _pieces = new();
        readonly List<MeshFilter> _filters = new();
        Transform _target;
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
            target.GetComponentsInChildren(false, _filters);
            foreach (MeshFilter filter in _filters)
            {
                if (filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer renderer) || !renderer.enabled)
                {
                    continue;
                }
                _pieces.Add(new Piece(filter.transform, filter.sharedMesh, filter.gameObject.layer));
            }
            _filters.Clear();
            _target = target;
            _pose = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
            _visible = _pieces.Count > 0;
        }

        public void Hide()
        {
            _pieces.Clear();
            _target = null;
            _visible = false;
        }

        /// <summary>Where each piece is drawn this frame, in the order the pieces were found; for tests.</summary>
        internal void CollectMatrices(List<Matrix4x4> matrices)
        {
            matrices.Clear();
            if (!_visible || _target == null)
            {
                return;
            }
            Matrix4x4 toGhost = ToGhost();
            foreach (Piece piece in _pieces)
            {
                if (piece.Transform != null)
                {
                    matrices.Add(toGhost * piece.Transform.localToWorldMatrix);
                }
            }
        }

        /// <summary>From world space as the object is now to world space at the ghost's pose (scale kept in the pieces).</summary>
        Matrix4x4 ToGhost() => _pose * Matrix4x4.TRS(_target.position, _target.rotation, Vector3.one).inverse;

        void LateUpdate()
        {
            if (!_visible)
            {
                return;
            }
            if (_target == null)
            {
                Hide();
                return;
            }
            Matrix4x4 toGhost = ToGhost();
            foreach (Piece piece in _pieces)
            {
                if (piece.Mesh == null || piece.Transform == null)
                {
                    continue;
                }
                var parameters = new RenderParams(_material)
                {
                    layer = piece.Layer,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                };
                Matrix4x4 matrix = toGhost * piece.Transform.localToWorldMatrix;
                for (int i = 0; i < piece.SubMeshCount; i++)
                {
                    Graphics.RenderMesh(parameters, piece.Mesh, i, matrix);
                }
            }
        }

        void OnDisable() => Hide();
    }
}
