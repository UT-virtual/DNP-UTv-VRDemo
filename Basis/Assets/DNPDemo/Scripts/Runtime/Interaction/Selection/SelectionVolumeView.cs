using System.Collections.Generic;
using DNPDemo.Common;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>
    /// Draws a range selection: one sphere per <see cref="SelectionSample"/>, rendered with GPU instancing
    /// (<see cref="Graphics.RenderMeshInstanced"/>) in a single draw call. The material
    /// (<c>Art/Interaction/Selection/SelectionVolume.mat</c>, shader <c>DNPDemo/SelectionVolume</c>) blends each pixel
    /// once, so the spheres read as one translucent volume.
    /// <para>
    /// Positions are world space; the view does not follow its own transform.
    /// </para>
    /// </summary>
    public sealed class SelectionVolumeView : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Instanced draws are limited per call; larger selections are drawn in chunks.
        const int MaxInstancesPerDraw = 1023;

        [Tooltip("Material with the DNPDemo/SelectionVolume shader and GPU instancing enabled.")]
        [SerializeField] Material _material;

        [Tooltip("Subdivisions of the sphere mesh. 2 gives 320 triangles.")]
        [SerializeField, Range(0, IcosphereMesh.MaxSubdivisions)] int _sphereSubdivisions = 2;

        Mesh _sphereMesh;
        Matrix4x4[] _matrices = new Matrix4x4[0];
        int _count;
        Bounds _bounds;
        MaterialPropertyBlock _block;
        Color _baseColor;
        float _fadeStartTime;
        float _fadeSeconds;
        bool _fading;

        /// <summary>Number of spheres shown (0 once cleared, including after a fade ends).</summary>
        public int Count => _count;

        void Awake()
        {
            if (_material == null)
            {
                Debug.LogError("[DNPDemo] SelectionVolumeView: マテリアルが設定されていません。", this);
                enabled = false;
                return;
            }
            if (!_material.enableInstancing)
            {
                Debug.LogWarning("[DNPDemo] SelectionVolumeView: マテリアルの GPU インスタンシングが無効です。", this);
            }
            _baseColor = _material.HasProperty(BaseColorId) ? _material.GetColor(BaseColorId) : Color.red;
            _block = new MaterialPropertyBlock();
            _sphereMesh = IcosphereMesh.Create(_sphereSubdivisions, "SelectionSphere");
        }

        void OnDestroy()
        {
            if (_sphereMesh != null)
            {
                Destroy(_sphereMesh);
            }
        }

        /// <summary>Shows <paramref name="samples"/>, replacing what was shown and cancelling a running fade.</summary>
        public void Show(IReadOnlyList<SelectionSample> samples)
        {
            _fading = false;
            _count = samples?.Count ?? 0;
            if (_count == 0)
            {
                return;
            }
            if (_matrices.Length < _count)
            {
                _matrices = new Matrix4x4[Mathf.NextPowerOfTwo(_count)];
            }
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            for (int i = 0; i < _count; i++)
            {
                SelectionSample sample = samples[i];
                _matrices[i] = Matrix4x4.TRS(sample.Position, Quaternion.identity, Vector3.one * sample.Radius);
                Vector3 extent = Vector3.one * sample.Radius;
                min = Vector3.Min(min, sample.Position - extent);
                max = Vector3.Max(max, sample.Position + extent);
            }
            _bounds = new Bounds((min + max) * 0.5f, max - min);
        }

        /// <summary>Removes the selection, at once or fading out over <paramref name="fadeSeconds"/>.</summary>
        public void Clear(float fadeSeconds = 0f)
        {
            if (_count == 0 || fadeSeconds <= 0f)
            {
                _count = 0;
                _fading = false;
                return;
            }
            _fading = true;
            _fadeSeconds = fadeSeconds;
            _fadeStartTime = Time.time;
        }

        void LateUpdate()
        {
            if (_count == 0)
            {
                return;
            }
            float alphaScale = 1f;
            if (_fading)
            {
                alphaScale = 1f - (Time.time - _fadeStartTime) / _fadeSeconds;
                if (alphaScale <= 0f)
                {
                    _count = 0;
                    _fading = false;
                    return;
                }
            }
            Color color = _baseColor;
            color.a *= alphaScale;
            _block.SetColor(BaseColorId, color);

            var renderParams = new RenderParams(_material)
            {
                layer = gameObject.layer,
                worldBounds = _bounds,
                matProps = _block,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
            };
            for (int start = 0; start < _count; start += MaxInstancesPerDraw)
            {
                Graphics.RenderMeshInstanced(in renderParams, _sphereMesh, 0, _matrices, Mathf.Min(MaxInstancesPerDraw, _count - start), start);
            }
        }
    }
}
