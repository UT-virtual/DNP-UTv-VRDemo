using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>
    /// Turns the pen tip's path into a list of spheres (<see cref="SelectionSample"/>).
    /// <para>
    /// A new sphere is placed once the tip has moved <see cref="SpacingFactor"/> times the radius away from the last one,
    /// so the spacing scales with the radius: a small sphere is sampled densely, a large one sparsely, and neighbouring
    /// spheres always overlap. A change of radius counts as movement too, so growing the sphere in place is recorded.
    /// When the tip moved further than that within one frame, the gap is filled with interpolated spheres.
    /// </para>
    /// <para>
    /// The list stops growing at <see cref="MaxSamples"/>, which bounds the network message and the draw cost.
    /// </para>
    /// </summary>
    public sealed class SelectionSampler
    {
        public const float DefaultSpacingFactor = 0.5f;
        public const int DefaultMaxSamples = 512;

        const float MinRadius = 0.001f;

        readonly List<SelectionSample> _samples = new();

        public SelectionSampler(float spacingFactor = DefaultSpacingFactor, int maxSamples = DefaultMaxSamples)
        {
            SpacingFactor = Mathf.Max(0.05f, spacingFactor);
            MaxSamples = Mathf.Max(1, maxSamples);
        }

        /// <summary>Distance between neighbouring spheres, as a fraction of their radius.</summary>
        public float SpacingFactor { get; }

        public int MaxSamples { get; }

        public IReadOnlyList<SelectionSample> Samples => _samples;

        public int Count => _samples.Count;

        public bool IsFull => _samples.Count >= MaxSamples;

        public void Clear() => _samples.Clear();

        /// <summary>
        /// Feeds the current tip position and radius. Returns true when at least one sphere was added.
        /// The first call always adds one.
        /// </summary>
        public bool Add(Vector3 position, float radius)
        {
            radius = Mathf.Max(MinRadius, radius);
            if (IsFull)
            {
                return false;
            }
            if (_samples.Count == 0)
            {
                _samples.Add(new SelectionSample(position, radius));
                return true;
            }

            SelectionSample last = _samples[_samples.Count - 1];
            float moved = Vector3.Distance(last.Position, position);
            float grown = Mathf.Abs(radius - last.Radius);
            float distance = moved + grown;
            float spacing = SpacingFactor * Mathf.Min(last.Radius, radius);
            if (distance < spacing)
            {
                return false;
            }

            // Step from the last sphere towards the current one, one spacing at a time. Movement and radius change
            // are interpolated together, so the path in (position, radius) is followed evenly.
            int steps = Mathf.FloorToInt(distance / spacing);
            bool added = false;
            for (int i = 1; i <= steps && !IsFull; i++)
            {
                float t = i * spacing / distance;
                _samples.Add(new SelectionSample(
                    Vector3.Lerp(last.Position, position, t),
                    Mathf.Lerp(last.Radius, radius, t)));
                added = true;
            }
            return added;
        }
    }
}
