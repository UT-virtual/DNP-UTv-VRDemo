using System;
using System.Collections;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>Pauses the timeline.</summary>
    [Serializable]
    public sealed class WaitCue : ScenarioCue
    {
        [Min(0f)] public float Seconds = 1f;

        public WaitCue()
        {
        }

        public WaitCue(float seconds)
        {
            Seconds = seconds;
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            yield return new WaitForSeconds(Seconds);
        }

        public override string ToString() => $"待機 {Seconds:0.##} 秒";
    }
}
