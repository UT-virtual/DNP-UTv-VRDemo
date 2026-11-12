using System;
using System.Collections;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>
    /// One action in a step's timeline.
    /// <para>
    /// Transient cues (lines, sounds, waits) only matter while they play. Persistent cues change what the
    /// world looks like afterwards and override <see cref="ApplyInstant"/>; when a device enters step N it
    /// resets the stage and calls <see cref="ApplyInstant"/> for every cue of steps 0..N-1, so the lasting
    /// state depends only on the step index.
    /// </para>
    /// <para>
    /// Playback can be cut at any yield when the step changes. Anything a cue starts that outlives the cut
    /// must be owned by <see cref="ScenarioStage"/>, which stops transient output and resets the baseline.
    /// </para>
    /// </summary>
    [Serializable]
    public abstract class ScenarioCue
    {
        /// <summary>Plays the cue. Yield <c>null</c>, a YieldInstruction or a nested IEnumerator.</summary>
        public abstract IEnumerator Play(ScenarioCueContext context);

        /// <summary>Applies the lasting result without animation. Transient cues leave this empty.</summary>
        public virtual void ApplyInstant(ScenarioCueContext context)
        {
        }

        /// <summary>Adds the scene bindings (speakers, object keys) this cue addresses through <see cref="ScenarioStage"/>.</summary>
        public virtual void CollectRequirements(ScenarioRequirements requirements)
        {
        }

        /// <summary>One-line summary used as the inspector label.</summary>
        public abstract override string ToString();
    }
}
