using System.Collections.Generic;
using DNPDemo.Environment;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;

namespace DNPDemo.Interaction.Background
{
    /// <summary>
    /// The backgrounds the background panel lists, worked out from the step index like every other lasting state, so both
    /// devices list the same ones.
    /// </summary>
    public static class BackgroundList
    {
        /// <summary>
        /// The baseline (the environment at scene load), then the presets the <see cref="EnvironmentCue"/>s of steps 0 to
        /// <paramref name="stepCount"/> - 1 apply to the controller keyed <paramref name="environmentKey"/>, then
        /// <paramref name="candidate"/> (a background generated during the current part). Each once, nulls skipped.
        /// </summary>
        public static List<EnvironmentPreset> Collect(IReadOnlyList<ScenarioStep> steps, int stepCount, string environmentKey,
            EnvironmentPreset baseline, EnvironmentPreset candidate)
        {
            var presets = new List<EnvironmentPreset>();
            Add(presets, baseline);
            foreach (EnvironmentCue cue in EnumerateCues(steps, stepCount, environmentKey))
            {
                Add(presets, cue.Preset);
            }
            Add(presets, candidate);
            return presets;
        }

        /// <summary>
        /// The background on show once steps 0 to <paramref name="stepCount"/> - 1 have played: the preset of the last
        /// <see cref="EnvironmentCue"/> among them, or <paramref name="baseline"/> when there is none.
        /// </summary>
        public static EnvironmentPreset FindCurrent(IReadOnlyList<ScenarioStep> steps, int stepCount, string environmentKey,
            EnvironmentPreset baseline)
        {
            EnvironmentPreset current = baseline;
            foreach (EnvironmentCue cue in EnumerateCues(steps, stepCount, environmentKey))
            {
                current = cue.Preset;
            }
            return current;
        }

        /// <summary>True when「背景を適用」can be pressed: the scenario allows it and another background than the one on show is selected.</summary>
        public static bool CanApply(bool allowed, EnvironmentPreset selected, EnvironmentPreset current) =>
            allowed && selected != null && selected != current;

        static IEnumerable<EnvironmentCue> EnumerateCues(IReadOnlyList<ScenarioStep> steps, int stepCount, string environmentKey)
        {
            if (steps == null)
            {
                yield break;
            }
            int end = System.Math.Min(stepCount, steps.Count);
            for (int i = 0; i < end; i++)
            {
                List<ScenarioCue> cues = steps[i]?.Cues;
                if (cues == null)
                {
                    continue;
                }
                foreach (ScenarioCue cue in cues)
                {
                    if (cue is EnvironmentCue environmentCue && environmentCue.EnvironmentKey == environmentKey && environmentCue.Preset != null)
                    {
                        yield return environmentCue;
                    }
                }
            }
        }

        static void Add(List<EnvironmentPreset> presets, EnvironmentPreset preset)
        {
            if (preset != null && !presets.Contains(preset))
            {
                presets.Add(preset);
            }
        }
    }
}
