using System.Collections.Generic;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;

namespace DNPDemo.Venus
{
    /// <summary>
    /// The versions a statue has reached, worked out from the step index like every other lasting state.
    /// </summary>
    public static class VenusVersionHistory
    {
        /// <summary>
        /// The versions the <see cref="StatueVersionCue"/>s of steps 0 to <paramref name="stepIndex"/> - 1 show on the
        /// statue keyed <paramref name="statueKey"/>, each once, in the order they first appear. These are the cues a device
        /// entering <paramref name="stepIndex"/> applies instantly; the current step's own cues are not counted.
        /// </summary>
        public static List<VenusVersion> Collect(IReadOnlyList<ScenarioStep> steps, int stepIndex, string statueKey)
        {
            var reached = new List<VenusVersion>();
            if (steps == null)
            {
                return reached;
            }
            int end = System.Math.Min(stepIndex, steps.Count);
            for (int i = 0; i < end; i++)
            {
                List<ScenarioCue> cues = steps[i]?.Cues;
                if (cues == null)
                {
                    continue;
                }
                foreach (ScenarioCue cue in cues)
                {
                    if (cue is StatueVersionCue versionCue
                        && versionCue.StatueKey == statueKey
                        && versionCue.Version != null
                        && !reached.Contains(versionCue.Version))
                    {
                        reached.Add(versionCue.Version);
                    }
                }
            }
            return reached;
        }
    }
}
