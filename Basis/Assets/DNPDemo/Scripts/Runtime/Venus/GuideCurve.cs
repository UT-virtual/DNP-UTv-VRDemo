using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Venus
{
    /// <summary>
    /// The set shape of a guide that a player traces with the pen (step 11: from the left shoulder to the front of the
    /// chest), as a cubic Bézier curve. Its ends are the centres of the guide's start and end zones, referenced rather
    /// than copied so the curve always starts and ends where the stroke is judged; only the two inner control points
    /// belong to the curve, in this object's local space.
    /// <para>
    /// <see cref="_line"/>, when set, draws the curve as a dashed line for the player to follow. It is shown and hidden
    /// by the scenario (a key on that object), and <see cref="GuideSnapEffect"/> runs along the same curve.
    /// </para>
    /// </summary>
    [ExecuteAlways]
    public sealed class GuideCurve : MonoBehaviour
    {
        const int DashTextureWidth = 4;

        [Tooltip("Start of the curve: the centre of the guide's start zone.")]
        [SerializeField] Transform _start;

        [Tooltip("End of the curve: the centre of the guide's end zone.")]
        [SerializeField] Transform _end;

        [Tooltip("Control point leaving the start, in this object's local space.")]
        [SerializeField] Vector3 _startHandle;

        [Tooltip("Control point arriving at the end, in this object's local space.")]
        [SerializeField] Vector3 _endHandle;

        [Tooltip("Dashed line that shows the curve. Optional.")]
        [SerializeField] LineRenderer _line;

        [Tooltip("Points the line is drawn with.")]
        [SerializeField, Range(4, 128)] int _lineResolution = 48;

        [Tooltip("Length of one dash and the gap after it, in metres.")]
        [SerializeField, Min(0.005f)] float _dashLength = 0.04f;

        Vector3[] _linePoints;
        Texture2D _dashTexture;

        public Transform StartPoint => _start;
        public Transform EndPoint => _end;

        /// <summary>Both ends are set.</summary>
        public bool IsValid => _start != null && _end != null;

        /// <summary>The point at <paramref name="t"/> (0 at the start, 1 at the end), in world space.</summary>
        public Vector3 Evaluate(float t)
        {
            if (!IsValid)
            {
                return transform.position;
            }
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            Vector3 p0 = _start.position;
            Vector3 p1 = transform.TransformPoint(_startHandle);
            Vector3 p2 = transform.TransformPoint(_endHandle);
            Vector3 p3 = _end.position;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        /// <summary>Fills <paramref name="points"/> with <paramref name="count"/> points evenly spaced in t, from start to end.</summary>
        public void Sample(int count, List<Vector3> points)
        {
            points.Clear();
            count = Mathf.Max(2, count);
            for (int i = 0; i < count; i++)
            {
                points.Add(Evaluate(i / (float)(count - 1)));
            }
        }

        void OnEnable() => UpdateLine();

        void LateUpdate()
        {
            // Update only during play. In editor mode, end points follow zones in OnEnable after editing,
            // but do not update every frame.
            if (!Application.isPlaying)
            {
                return;
            }
            UpdateLine();
        }

        void OnDestroy()
        {
            if (_dashTexture != null)
            {
                Destroy(_dashTexture);
            }
            if (_line != null && _line.material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_line.material);
                }
                else
                {
                    DestroyImmediate(_line.material);
                }
            }
        }

        void UpdateLine()
        {
            if (_line == null || !IsValid || !_line.gameObject.activeInHierarchy)
            {
                return;
            }
            if (_linePoints == null || _linePoints.Length != _lineResolution)
            {
                _linePoints = new Vector3[_lineResolution];
            }
            float length = 0f;
            for (int i = 0; i < _linePoints.Length; i++)
            {
                _linePoints[i] = Evaluate(i / (float)(_linePoints.Length - 1));
                if (i > 0)
                {
                    length += Vector3.Distance(_linePoints[i - 1], _linePoints[i]);
                }
            }
            _line.useWorldSpace = true;
            _line.positionCount = _linePoints.Length;
            _line.SetPositions(_linePoints);
            if (Application.isPlaying)
            {
                ApplyDashes(length);
            }
        }

        /// <summary>Tiles a half-opaque, half-clear texture along the line, one tile per dash and gap.</summary>
        void ApplyDashes(float length)
        {
            if (_dashTexture == null)
            {
                _dashTexture = new Texture2D(DashTextureWidth, 1, TextureFormat.RGBA32, false)
                {
                    name = "GuideDash",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point,
                };
                var pixels = new Color32[DashTextureWidth];
                for (int i = 0; i < DashTextureWidth; i++)
                {
                    pixels[i] = i < DashTextureWidth / 2 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
                _dashTexture.SetPixels32(pixels);
                _dashTexture.Apply();
                // The line's own material instance, so the shared asset keeps no runtime texture.
                _line.material.mainTexture = _dashTexture;
                _line.textureMode = LineTextureMode.Tile;
            }
            // In Tile mode one texture repeat spans (line width / textureScale.x) metres.
            float width = Mathf.Max(_line.widthMultiplier, 1e-4f);
            _line.textureScale = new Vector2(width / (_dashLength * 2f), 1f);
        }

        void OnDrawGizmos()
        {
            if (!IsValid)
            {
                return;
            }
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
            Vector3 previous = Evaluate(0f);
            for (int i = 1; i <= 32; i++)
            {
                Vector3 next = Evaluate(i / 32f);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.4f);
            Gizmos.DrawLine(_start.position, transform.TransformPoint(_startHandle));
            Gizmos.DrawLine(_end.position, transform.TransformPoint(_endHandle));
        }
    }
}
