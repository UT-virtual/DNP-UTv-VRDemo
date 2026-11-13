using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>The thresholds a selection is judged by (see <see cref="SelectionJudge"/>).</summary>
    public readonly struct SelectionCriteria
    {
        /// <summary>Share of the samples that must touch some target zone.</summary>
        public readonly float RequiredInsideRatio;

        /// <summary>Share of each target zone's volume the selection must cover.</summary>
        public readonly float RequiredCoverage;

        public SelectionCriteria(float requiredInsideRatio, float requiredCoverage)
        {
            RequiredInsideRatio = Mathf.Clamp01(requiredInsideRatio);
            RequiredCoverage = Mathf.Clamp01(requiredCoverage);
        }
    }

    /// <summary>Outcome of judging one confirmed selection against the target zones of a <see cref="SelectionPart"/>.</summary>
    public readonly struct SelectionVerdict
    {
        public readonly int SampleCount;

        /// <summary>Samples that touch at least one target zone.</summary>
        public readonly int InsideCount;

        public readonly int TargetCount;

        /// <summary>Target zones that no sample touches.</summary>
        public readonly int MissedTargetCount;

        /// <summary>Coverage of the least covered target zone (0 with no target).</summary>
        public readonly float MinCoverage;

        public readonly SelectionCriteria Criteria;

        public SelectionVerdict(int sampleCount, int insideCount, int targetCount, int missedTargetCount, float minCoverage,
            SelectionCriteria criteria)
        {
            SampleCount = sampleCount;
            InsideCount = insideCount;
            TargetCount = targetCount;
            MissedTargetCount = missedTargetCount;
            MinCoverage = minCoverage;
            Criteria = criteria;
        }

        public float InsideRatio => SampleCount > 0 ? (float)InsideCount / SampleCount : 0f;

        /// <summary>
        /// Every target zone is touched and covered enough, and enough of the selection lies on the targets.
        /// </summary>
        public bool Succeeded =>
            SampleCount > 0 && TargetCount > 0 && MissedTargetCount == 0
            && MinCoverage >= Criteria.RequiredCoverage
            && InsideRatio >= Criteria.RequiredInsideRatio;

        public override string ToString() =>
            $"{(Succeeded ? "成功" : "失敗")}：ゾーン {TargetCount - MissedTargetCount}/{TargetCount}、" +
            $"最も覆えていないゾーンの被覆率 {MinCoverage:P0}（必要 {Criteria.RequiredCoverage:P0}）、" +
            $"ゾーン上のサンプル {InsideCount}/{SampleCount}（{InsideRatio:P0}、必要 {Criteria.RequiredInsideRatio:P0}）";
    }

    /// <summary>
    /// Judges a selection. It succeeds when all of these hold:
    /// <list type="bullet">
    /// <item>every target zone is touched by at least one sample;</item>
    /// <item>the selection covers at least <see cref="SelectionCriteria.RequiredCoverage"/> of every target zone's
    /// volume (<see cref="SelectionCoverage"/>), so a small selection somewhere inside the zone is not enough;</item>
    /// <item>at least <see cref="SelectionCriteria.RequiredInsideRatio"/> of the samples touch some target zone, so the
    /// selection does not stray far beyond it. A ratio below 1 tolerates a little overshoot.</item>
    /// </list>
    /// "Touch" is <see cref="IDetectionVolume.OverlapsSphere"/>: a sample counts when its sphere reaches the zone.
    /// A <see cref="DetectionZoneGroup"/> is one target, and its coverage is measured over the union of its zones.
    /// Null targets are ignored; with no target left the selection fails.
    /// </summary>
    public static class SelectionJudge
    {
        public static SelectionVerdict Evaluate(
            IReadOnlyList<SelectionSample> samples,
            IReadOnlyList<IDetectionVolume> targets,
            SelectionCriteria criteria)
        {
            int sampleCount = samples?.Count ?? 0;
            int targetCount = 0;
            int missedTargets = 0;
            float minCoverage = 1f;
            if (targets != null)
            {
                foreach (IDetectionVolume target in targets)
                {
                    if (!DetectionVolumes.IsAlive(target))
                    {
                        continue;
                    }
                    targetCount++;
                    if (!TouchesAny(target, samples))
                    {
                        missedTargets++;
                        minCoverage = 0f;
                        continue;
                    }
                    minCoverage = Mathf.Min(minCoverage, SelectionCoverage.Measure(target, samples));
                }
            }
            if (targetCount == 0)
            {
                minCoverage = 0f;
            }

            int inside = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                if (TouchesAnyTarget(samples[i], targets))
                {
                    inside++;
                }
            }
            return new SelectionVerdict(sampleCount, inside, targetCount, missedTargets, minCoverage, criteria);
        }

        static bool TouchesAny(IDetectionVolume target, IReadOnlyList<SelectionSample> samples)
        {
            if (samples == null)
            {
                return false;
            }
            foreach (SelectionSample sample in samples)
            {
                if (target.OverlapsSphere(sample.Position, sample.Radius))
                {
                    return true;
                }
            }
            return false;
        }

        static bool TouchesAnyTarget(in SelectionSample sample, IReadOnlyList<IDetectionVolume> targets)
        {
            if (targets == null)
            {
                return false;
            }
            foreach (IDetectionVolume target in targets)
            {
                if (DetectionVolumes.IsAlive(target) && target.OverlapsSphere(sample.Position, sample.Radius))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
