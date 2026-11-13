using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>
    /// Estimates how much of a zone's volume a selection covers. Probe points are laid on a regular grid over the
    /// zone's <see cref="IDetectionVolume.WorldBounds"/>; the coverage is the share of the probes inside the zone that
    /// lie in at least one sample sphere. The grid holds about <see cref="ProbeBudget"/> cells whatever the zone's size
    /// and proportions, so the error stays at a few percent and the cost stays bounded.
    /// </summary>
    public static class SelectionCoverage
    {
        public const int ProbeBudget = 1000;

        /// <summary>
        /// Share (0 to 1) of <paramref name="zone"/> covered by the union of <paramref name="samples"/>. A zone too
        /// small for any probe to land inside it counts as fully covered when a sample touches it, and as uncovered
        /// otherwise.
        /// </summary>
        public static float Measure(IDetectionVolume zone, IReadOnlyList<SelectionSample> samples)
        {
            if (!DetectionVolumes.IsAlive(zone) || samples == null || samples.Count == 0)
            {
                return 0f;
            }
            Bounds bounds = zone.WorldBounds;
            var nearby = new List<SelectionSample>();
            foreach (SelectionSample sample in samples)
            {
                if (bounds.SqrDistance(sample.Position) <= sample.Radius * sample.Radius)
                {
                    nearby.Add(sample);
                }
            }
            if (nearby.Count == 0)
            {
                return 0f;
            }

            Vector3Int counts = GridCounts(bounds.size, ProbeBudget);
            var cell = new Vector3(bounds.size.x / counts.x, bounds.size.y / counts.y, bounds.size.z / counts.z);
            Vector3 first = bounds.min + cell * 0.5f;
            int inside = 0;
            int covered = 0;
            for (int x = 0; x < counts.x; x++)
            {
                for (int y = 0; y < counts.y; y++)
                {
                    for (int z = 0; z < counts.z; z++)
                    {
                        var probe = new Vector3(first.x + x * cell.x, first.y + y * cell.y, first.z + z * cell.z);
                        if (!zone.ContainsPoint(probe))
                        {
                            continue;
                        }
                        inside++;
                        if (IsCovered(probe, nearby))
                        {
                            covered++;
                        }
                    }
                }
            }
            if (inside == 0)
            {
                return TouchesZone(zone, nearby) ? 1f : 0f;
            }
            return (float)covered / inside;
        }

        /// <summary>
        /// Cells per axis for a grid of about <paramref name="budget"/> roughly cubic cells over a box of
        /// <paramref name="size"/>. An axis thinner than one cell gets a single cell, and the budget is spread over the
        /// other axes, so a flat or thin zone is not starved of probes.
        /// </summary>
        public static Vector3Int GridCounts(Vector3 size, int budget)
        {
            budget = Mathf.Max(1, budget);
            float[] lengths = { size.x, size.y, size.z };
            int[] counts = { 1, 1, 1 };
            bool[] single = new bool[3];
            for (int axis = 0; axis < 3; axis++)
            {
                single[axis] = lengths[axis] <= 0f;
            }

            // Each pass either settles the counts or makes at least one more axis single, so four passes suffice.
            for (int pass = 0; pass < 4; pass++)
            {
                int free = 0;
                float product = 1f;
                for (int axis = 0; axis < 3; axis++)
                {
                    if (!single[axis])
                    {
                        free++;
                        product *= lengths[axis];
                    }
                }
                if (free == 0)
                {
                    break;
                }
                float spacing = Mathf.Pow(product / budget, 1f / free);
                bool narrowed = false;
                for (int axis = 0; axis < 3; axis++)
                {
                    if (!single[axis] && lengths[axis] < spacing)
                    {
                        single[axis] = true;
                        narrowed = true;
                    }
                }
                if (narrowed)
                {
                    continue;
                }
                for (int axis = 0; axis < 3; axis++)
                {
                    if (!single[axis])
                    {
                        counts[axis] = Mathf.Clamp(Mathf.RoundToInt(lengths[axis] / spacing), 1, budget);
                    }
                }
                break;
            }
            return new Vector3Int(counts[0], counts[1], counts[2]);
        }

        static bool IsCovered(Vector3 probe, List<SelectionSample> samples)
        {
            foreach (SelectionSample sample in samples)
            {
                if ((probe - sample.Position).sqrMagnitude <= sample.Radius * sample.Radius)
                {
                    return true;
                }
            }
            return false;
        }

        static bool TouchesZone(IDetectionVolume zone, List<SelectionSample> samples)
        {
            foreach (SelectionSample sample in samples)
            {
                if (zone.OverlapsSphere(sample.Position, sample.Radius))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
