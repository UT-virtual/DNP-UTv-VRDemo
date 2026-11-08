using System;
using System.Collections;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>Fades the full-view banner (for example "WORKSHOP COMPLETE") in or out. Persistent.</summary>
    [Serializable]
    public sealed class BannerCue : ScenarioCue
    {
        public bool Visible = true;

        public string Text;

        [Min(0f)] public float FadeSeconds = 1f;

        public BannerCue()
        {
        }

        public BannerCue(string text, bool visible, float fadeSeconds)
        {
            Text = text;
            Visible = visible;
            FadeSeconds = fadeSeconds;
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            context.Stage.SetBanner(Visible, Text, FadeSeconds);
            if (FadeSeconds > 0f)
            {
                yield return new WaitForSeconds(FadeSeconds);
            }
        }

        public override void ApplyInstant(ScenarioCueContext context) => context.Stage.SetBanner(Visible, Text, 0f);

        public override string ToString() => Visible ? $"バナー表示「{Text}」" : "バナー非表示";
    }
}
