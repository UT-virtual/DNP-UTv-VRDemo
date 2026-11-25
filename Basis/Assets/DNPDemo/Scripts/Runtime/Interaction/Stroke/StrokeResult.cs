using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Stroke
{
    /// <summary>
    /// The finished lines a stroke part ended with, copied out of the tool's <see cref="StrokeSet"/> so they outlive the
    /// step (see <see cref="StrokeTool.LastResult"/>). World-space points in drawing order.
    /// <para>
    /// Cleared by <see cref="StrokeTool.Begin(InteractionSession)"/> when the next stroke part begins.
    /// </para>
    /// </summary>
    public sealed class StrokeResult
    {
        StrokeResult(ScenarioStep step, List<IReadOnlyList<Vector3>> lines)
        {
            Step = step;
            Lines = lines;
        }

        /// <summary>The step the lines were drawn in.</summary>
        public ScenarioStep Step { get; }

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
            return new StrokeResult(step, lines);
        }
    }
}
