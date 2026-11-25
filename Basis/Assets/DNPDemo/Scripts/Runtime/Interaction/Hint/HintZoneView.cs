using System.Collections.Generic;
using DNPDemo.Common;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Interaction.Hint
{
    /// <summary>
    /// Lights up detection zones for a hint: each zone's shape (sphere, capsule or box) is drawn translucent and pulses
    /// slowly. The shapes are drawn every frame with <see cref="Graphics.RenderMesh"/> at the zone's current pose, so a
    /// zone that moves (the apple being dragged) is followed, and a zone that is switched off is not drawn.
    /// </summary>
    public sealed class HintZoneView : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Translucent unlit material (URP Unlit, transparent). Its colour is replaced by the colour below.")]
        [SerializeField] Material _material;

        [Tooltip("Colour of the zones. The alpha pulses between the two values below.")]
        [SerializeField] Color _color = new(1f, 0.85f, 0.2f, 1f);

        [SerializeField, Range(0f, 1f)] float _minAlpha = 0.08f;
        [SerializeField, Range(0f, 1f)] float _maxAlpha = 0.35f;

        [Tooltip("Seconds of one pulse.")]
        [SerializeField, Min(0.1f)] float _pulseSeconds = 1.2f;

        [SerializeField, Range(0, IcosphereMesh.MaxSubdivisions)] int _sphereSubdivisions = 2;

        readonly List<DetectionZone> _zones = new();
        readonly Dictionary<(float radius, float height), Mesh> _capsules = new();
        MaterialPropertyBlock _properties;
        Mesh _sphere;
        Mesh _box;
        float _shownAt;

        public bool IsVisible => _zones.Count > 0;

        /// <summary>Lights up the zones of <paramref name="volumes"/> (each zone of a group). Replaces what was shown.</summary>
        public void Show(IReadOnlyList<IDetectionVolume> volumes)
        {
            _zones.Clear();
            if (volumes != null)
            {
                foreach (IDetectionVolume volume in volumes)
                {
                    switch (volume)
                    {
                        case DetectionZoneGroup group when group != null:
                            foreach (DetectionZone zone in group.Zones)
                            {
                                AddZone(zone);
                            }
                            break;
                        case DetectionZone zone:
                            AddZone(zone);
                            break;
                    }
                }
            }
            if (_zones.Count > 0 && _material == null)
            {
                Debug.LogError("[DNPDemo] HintZoneView: マテリアルが設定されていません。", this);
            }
            _shownAt = Time.unscaledTime;
        }

        public void Hide() => _zones.Clear();

        void AddZone(DetectionZone zone)
        {
            if (zone != null && !_zones.Contains(zone))
            {
                _zones.Add(zone);
            }
        }

        void LateUpdate()
        {
            if (_zones.Count == 0 || _material == null)
            {
                return;
            }
            // Starts faint and fades in, so the zones do not pop up at full strength.
            float wave = 0.5f - 0.5f * Mathf.Cos((Time.unscaledTime - _shownAt) / _pulseSeconds * 2f * Mathf.PI);
            Color color = _color;
            color.a = Mathf.Lerp(_minAlpha, _maxAlpha, wave);
            _properties ??= new MaterialPropertyBlock();
            _properties.SetColor(BaseColorId, color);

            foreach (DetectionZone zone in _zones)
            {
                if (zone == null || !zone.isActiveAndEnabled)
                {
                    continue;
                }
                (Mesh mesh, Matrix4x4 shape) = ShapeOf(zone);
                var parameters = new RenderParams(_material)
                {
                    layer = zone.gameObject.layer,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    matProps = _properties,
                };
                Graphics.RenderMesh(parameters, mesh, 0, zone.transform.localToWorldMatrix * shape);
            }
        }

        /// <summary>The mesh of the zone's shape and its transform within the zone's local space.</summary>
        (Mesh mesh, Matrix4x4 shape) ShapeOf(DetectionZone zone)
        {
            switch (zone.ShapeType)
            {
                case ZoneShapeType.Box:
                    _box ??= ZoneShapeMeshes.CreateBox();
                    return (_box, Matrix4x4.Scale(zone.BoxSize));
                case ZoneShapeType.Capsule:
                    (float, float) size = (zone.Radius, zone.Height);
                    if (!_capsules.TryGetValue(size, out Mesh capsule))
                    {
                        capsule = ZoneShapeMeshes.CreateCapsule(zone.Radius, zone.Height);
                        _capsules.Add(size, capsule);
                    }
                    return (capsule, Matrix4x4.identity);
                default:
                    _sphere ??= IcosphereMesh.Create(_sphereSubdivisions, "ZoneSphere");
                    return (_sphere, Matrix4x4.Scale(Vector3.one * zone.Radius));
            }
        }

        void OnDisable() => Hide();

        void OnDestroy()
        {
            DestroyMesh(_sphere);
            DestroyMesh(_box);
            foreach (Mesh capsule in _capsules.Values)
            {
                DestroyMesh(capsule);
            }
            _capsules.Clear();
        }

        static void DestroyMesh(Mesh mesh)
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }
        }
    }
}
