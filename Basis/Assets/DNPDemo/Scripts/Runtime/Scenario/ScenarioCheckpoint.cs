using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// Remembers on the device with scenario authority which step the run is at, so that a run cut short by a restart of
    /// the host app can be resumed from the operator panel. The step is stored by its <see cref="ScenarioStep.Id"/>, which
    /// survives steps being reordered between builds.
    /// </summary>
    public static class ScenarioCheckpoint
    {
        const string LastStepIdKey = "DNPDemo.Scenario.LastStepId";

        /// <summary>Stores <paramref name="stepId"/> as the step the run is at.</summary>
        public static void Save(string stepId)
        {
            PlayerPrefs.SetString(LastStepIdKey, stepId ?? string.Empty);
            PlayerPrefs.Save();
        }

        /// <summary>The stored step id, or an empty string.</summary>
        public static string Load() => PlayerPrefs.GetString(LastStepIdKey, string.Empty);

        /// <summary>
        /// The step an interrupted run can resume at, or -1. A run counts as interrupted when the stored step lies
        /// between the first step (the lobby) and the last one (the closing, which waits for the next visitors).
        /// </summary>
        public static int ResolveResumeIndex(IReadOnlyList<ScenarioStep> steps, string stepId)
        {
            if (steps == null || string.IsNullOrEmpty(stepId))
            {
                return -1;
            }
            for (int i = 1; i < steps.Count - 1; i++)
            {
                if (steps[i] != null && steps[i].Id == stepId)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
