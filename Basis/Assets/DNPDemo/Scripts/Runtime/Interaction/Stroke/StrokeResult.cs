using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Stroke
{
    /// <summary>
    /// The finished lines a stroke part ended with, copied out of the tool's <see cref="StrokeSet"/> so they outlive the
    /// step (see <see cref="StrokeTool.LastResult"/>). World-space points in drawing order.
    /// <para>
    /// このインスタンスは <see cref="StrokeTool.LastResult"/> を通じて、後続のステップの演出から参照できます。
    /// 次の部品（例：音声指示、腕スナップ）が開始するときに、<see cref="StrokeTool.Begin(InteractionSession)"/>
    /// で <see cref="StrokeTool.LastResult"/> がクリアされます。
    /// </para>
    /// </summary>
    public sealed class StrokeResult
    {
        StrokeResult(ScenarioStep step, StrokePart part, List<IReadOnlyList<Vector3>> lines)
        {
            Step = step;
            Part = part;
            Lines = lines;
        }

        /// <summary>The step the lines were drawn in.</summary>
        public ScenarioStep Step { get; }

        public StrokePart Part { get; }

        /// <summary>The finished lines, oldest first. Empty when nothing was drawn (for example, when the step was skipped).</summary>
        public IReadOnlyList<IReadOnlyList<Vector3>> Lines { get; }

        public static StrokeResult Capture(ScenarioStep step, StrokePart part, StrokeSet set)
        {
            var lines = new List<IReadOnlyList<Vector3>>();
            foreach (StrokeLine line in set.Lines)
            {
                if (line.IsFinished && line.Count > 0)
                {
                    lines.Add(new List<Vector3>(line.Points));
                }
            }
            return new StrokeResult(step, part, lines);
        }
    }
}
