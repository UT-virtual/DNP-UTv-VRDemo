using System;
using System.Collections;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>Plays a non-positional sound effect.</summary>
    [Serializable]
    public sealed class SoundCue : ScenarioCue
    {
        public AudioClip Clip;

        [Range(0f, 1f)] public float Volume = 1f;

        [Tooltip("Hold the timeline until the clip has finished.")]
        public bool WaitForEnd;

        public override IEnumerator Play(ScenarioCueContext context)
        {
            if (Clip == null)
            {
                yield break;
            }
            context.Stage.PlayEffect(Clip, Volume);
            if (WaitForEnd)
            {
                yield return new WaitForSeconds(Clip.length);
            }
        }

        public override string ToString() => $"効果音 {(Clip != null ? Clip.name : "（未設定）")}";
    }
}
