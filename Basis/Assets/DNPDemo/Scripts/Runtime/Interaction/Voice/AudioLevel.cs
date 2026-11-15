using UnityEngine;

namespace DNPDemo.Interaction.Voice
{
    /// <summary>Converts between a linear RMS level (1 = full scale) and decibels relative to full scale (dBFS).</summary>
    public static class AudioLevel
    {
        /// <summary>The level reported for silence, and the floor of <see cref="ToDecibels"/>.</summary>
        public const float SilenceDecibels = -100f;

        public static float ToDecibels(float level)
        {
            if (!(level > 0f))
            {
                return SilenceDecibels;
            }
            return Mathf.Max(SilenceDecibels, 20f * Mathf.Log10(level));
        }

        public static float FromDecibels(float decibels) => Mathf.Pow(10f, decibels / 20f);
    }
}
