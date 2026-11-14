namespace DNPDemo.Interaction.Voice
{
    /// <summary>
    /// Adds up how long the microphone level stays above a threshold during one press of the trigger. The time does not
    /// have to be in one run, so pauses between words do not start it over.
    /// </summary>
    public sealed class VoiceActivityMeter
    {
        /// <summary>Seconds above the threshold since <see cref="Reset"/>.</summary>
        public float VoicedSeconds { get; private set; }

        /// <summary>Highest level fed since <see cref="Reset"/>, for logs and threshold tuning.</summary>
        public float PeakLevel { get; private set; }

        public void Reset()
        {
            VoicedSeconds = 0f;
            PeakLevel = 0f;
        }

        /// <summary>
        /// Adds one frame: <paramref name="deltaTime"/> seconds at <paramref name="level"/> (linear RMS). The threshold is
        /// passed every frame so a change made while speaking applies at once.
        /// </summary>
        public void Feed(float level, float threshold, float deltaTime)
        {
            if (!(deltaTime > 0f) || float.IsNaN(level))
            {
                return;
            }
            if (level > PeakLevel)
            {
                PeakLevel = level;
            }
            if (level > threshold)
            {
                VoicedSeconds += deltaTime;
            }
        }
    }
}
