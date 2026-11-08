using System;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>Pacing of spoken lines. Lines without a voice clip use an estimate from their length.</summary>
    [Serializable]
    public sealed class LineTiming
    {
        [Tooltip("Seconds per character for lines that have no voice clip yet.")]
        [Min(0f)] public float SecondsPerCharacter = 0.13f;

        [Tooltip("Shortest time a line without a voice clip stays on screen.")]
        [Min(0f)] public float MinimumSeconds = 1.5f;

        [Tooltip("Pause after each line before the next cue.")]
        [Min(0f)] public float GapSeconds = 0.4f;

        /// <summary>How long a line is shown when it has no voice clip.</summary>
        public float EstimateSeconds(string text)
        {
            int length = string.IsNullOrEmpty(text) ? 0 : text.Length;
            return Mathf.Max(MinimumSeconds, length * SecondsPerCharacter);
        }
    }
}
