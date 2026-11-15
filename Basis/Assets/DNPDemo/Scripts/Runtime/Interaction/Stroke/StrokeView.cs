using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Interaction.Stroke
{
    /// <summary>
    /// Draws the lines of a <see cref="StrokeSet"/>, one pooled <see cref="LineRenderer"/> per line, in world space.
    /// The actor's lines and the received ones are drawn the same way. A line can fade out, which narrows it to nothing,
    /// so the material can stay opaque.
    /// </summary>
    public sealed class StrokeView : MonoBehaviour
    {
        [Tooltip("Material of the lines (opaque, unlit white).")]
        [SerializeField] Material _material;

        [Tooltip("Width of a line, in metres.")]
        [SerializeField, Min(0.001f)] float _width = 0.006f;

        sealed class Fading
        {
            public LineRenderer Line;
            public float StartTime;
            public float Seconds;
        }

        readonly Dictionary<ushort, LineRenderer> _shown = new();
        readonly List<Fading> _fading = new();
        readonly Stack<LineRenderer> _pool = new();
        readonly List<ushort> _scratchIds = new();

        public float Width => _width;

        /// <summary>Number of lines shown, not counting those fading out.</summary>
        public int Count => _shown.Count;

        void Awake()
        {
            if (_material == null)
            {
                Debug.LogError("[DNPDemo] StrokeView: マテリアルが設定されていません。", this);
            }
        }

        /// <summary>Shows <paramref name="line"/> as it is now, adding or updating its renderer.</summary>
        public void Show(StrokeLine line)
        {
            if (line == null || line.Count == 0)
            {
                return;
            }
            if (!_shown.TryGetValue(line.Id, out LineRenderer renderer))
            {
                renderer = Rent();
                _shown.Add(line.Id, renderer);
            }
            IReadOnlyList<Vector3> points = line.Points;
            if (points.Count == 1)
            {
                // A single point is not drawn by a LineRenderer; two equal points show it as a dot.
                renderer.positionCount = 2;
                renderer.SetPosition(0, points[0]);
                renderer.SetPosition(1, points[0]);
                return;
            }
            int shown = renderer.positionCount;
            // Lines only grow while drawn, so normally only the new tail is set.
            int start = shown <= points.Count && shown > 1 ? shown - 1 : 0;
            renderer.positionCount = points.Count;
            for (int i = start; i < points.Count; i++)
            {
                renderer.SetPosition(i, points[i]);
            }
        }

        /// <summary>Removes the line, at once or fading out over <paramref name="fadeSeconds"/>.</summary>
        public void Hide(ushort id, float fadeSeconds = 0f)
        {
            if (!_shown.Remove(id, out LineRenderer renderer))
            {
                return;
            }
            if (fadeSeconds > 0f)
            {
                _fading.Add(new Fading { Line = renderer, StartTime = Time.time, Seconds = fadeSeconds });
            }
            else
            {
                Return(renderer);
            }
        }

        /// <summary>Shows exactly the lines of <paramref name="set"/>. Lines already fading out keep fading.</summary>
        public void Sync(StrokeSet set)
        {
            _scratchIds.Clear();
            foreach (ushort id in _shown.Keys)
            {
                if (set.Find(id) == null)
                {
                    _scratchIds.Add(id);
                }
            }
            foreach (ushort id in _scratchIds)
            {
                Hide(id);
            }
            foreach (StrokeLine line in set.Lines)
            {
                Show(line);
            }
        }

        /// <summary>Removes every line at once, including those fading out.</summary>
        public void Clear()
        {
            foreach (LineRenderer renderer in _shown.Values)
            {
                Return(renderer);
            }
            _shown.Clear();
            foreach (Fading fading in _fading)
            {
                Return(fading.Line);
            }
            _fading.Clear();
        }

        void LateUpdate()
        {
            for (int i = _fading.Count - 1; i >= 0; i--)
            {
                Fading fading = _fading[i];
                float remaining = 1f - (Time.time - fading.StartTime) / fading.Seconds;
                if (remaining <= 0f || fading.Line == null)
                {
                    Return(fading.Line);
                    _fading.RemoveAt(i);
                    continue;
                }
                fading.Line.widthMultiplier = _width * remaining;
            }
        }

        void OnDestroy()
        {
            Clear();
            while (_pool.Count > 0)
            {
                LineRenderer renderer = _pool.Pop();
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }
        }

        LineRenderer Rent()
        {
            LineRenderer renderer = null;
            while (renderer == null && _pool.Count > 0)
            {
                renderer = _pool.Pop();
            }
            if (renderer == null)
            {
                renderer = CreateRenderer();
            }
            renderer.widthMultiplier = _width;
            renderer.positionCount = 0;
            renderer.gameObject.SetActive(true);
            return renderer;
        }

        void Return(LineRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }
            renderer.positionCount = 0;
            renderer.gameObject.SetActive(false);
            _pool.Push(renderer);
        }

        LineRenderer CreateRenderer()
        {
            var go = new GameObject("StrokeLine");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.alignment = LineAlignment.View;
            renderer.widthMultiplier = _width;
            renderer.numCapVertices = 4;
            renderer.numCornerVertices = 2;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.sharedMaterial = _material;
            return renderer;
        }
    }
}
