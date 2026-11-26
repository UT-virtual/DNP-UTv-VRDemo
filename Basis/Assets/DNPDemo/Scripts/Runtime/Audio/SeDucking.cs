using UnityEngine;

namespace DNPDemo.Audio
{
    /// <summary>
    /// Lowers sound effects while a voice line plays, so a line stays clear over an effect that overlaps it. The gain moves
    /// to <see cref="LineGain"/> while a line plays and back to 1 afterwards, over <see cref="FadeSeconds"/> rather than at
    /// once. It multiplies the source's own volume, on top of the per-event volumes (catalog, sound cue). Used for the
    /// interaction effects (<see cref="SePlayer"/>) and the scenario's effects (<see cref="DNPDemo.Scenario.ScenarioStage"/>).
    /// </summary>
    public sealed class SeDucking
    {
        /// <summary>Gain of the effects while a line plays.</summary>
        public const float LineGain = 0.3f;

        /// <summary>Seconds to go from 1 to <see cref="LineGain"/> or back.</summary>
        public const float FadeSeconds = 0.15f;

        float _gain = 1f;

        public float Gain => _gain;

        /// <summary>Moves the gain towards its target for <paramref name="linePlaying"/> and returns it.</summary>
        public float Step(bool linePlaying, float deltaTime)
        {
            float target = linePlaying ? LineGain : 1f;
            float maxDelta = (1f - LineGain) / FadeSeconds * Mathf.Max(0f, deltaTime);
            _gain = Mathf.MoveTowards(_gain, target, maxDelta);
            return _gain;
        }

        public void Reset() => _gain = 1f;
    }
}
