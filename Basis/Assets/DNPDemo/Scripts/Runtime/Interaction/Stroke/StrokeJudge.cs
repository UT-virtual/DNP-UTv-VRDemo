using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction.Stroke
{
    /// <summary>Outcome of judging a stroke, with what was measured for the log.</summary>
    public readonly struct StrokeVerdict
    {
        public StrokeVerdict(bool succeeded, string detail)
        {
            Succeeded = succeeded;
            Detail = detail;
        }

        public bool Succeeded { get; }
        public string Detail { get; }

        public override string ToString() => (Succeeded ? "成功" : "失敗") + "：" + Detail;
    }

    /// <summary>The rules of <see cref="StrokePart"/>. Pure functions so they can be tested without a scene.</summary>
    public static class StrokeJudge
    {
        /// <summary>
        /// Guide: the line reaches both zones, in either order. A point counts as reaching a zone when the line's
        /// thickness there (a sphere of <paramref name="lineRadius"/>) touches it. Fails when either zone is missing.
        /// </summary>
        public static StrokeVerdict Guide(IReadOnlyList<Vector3> points, IDetectionVolume start, IDetectionVolume end, float lineRadius)
        {
            if (!DetectionVolumes.IsAlive(start) || !DetectionVolumes.IsAlive(end))
            {
                return new StrokeVerdict(false, "始点または終点のゾーンがありません");
            }
            if (points == null || points.Count == 0)
            {
                return new StrokeVerdict(false, "線がありません");
            }
            bool reachedStart = false;
            bool reachedEnd = false;
            for (int i = 0; i < points.Count && !(reachedStart && reachedEnd); i++)
            {
                reachedStart |= start.OverlapsSphere(points[i], lineRadius);
                reachedEnd |= end.OverlapsSphere(points[i], lineRadius);
            }
            string detail = $"始点{(reachedStart ? "に届いた" : "に届いていない")}、終点{(reachedEnd ? "に届いた" : "に届いていない")}（{points.Count} 点）";
            return new StrokeVerdict(reachedStart && reachedEnd, detail);
        }

        /// <summary>Sketch: the longest side of the box around all lines is at least <paramref name="minExtent"/>.</summary>
        public static StrokeVerdict Sketch(StrokeSet set, float minExtent)
        {
            if (set == null || !set.TryGetBounds(out Bounds bounds))
            {
                return new StrokeVerdict(false, "線がありません");
            }
            float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            return new StrokeVerdict(extent >= minExtent, $"外接サイズ {extent:0.000}m（基準 {minExtent:0.000}m、{set.Lines.Count} 本）");
        }
    }
}
