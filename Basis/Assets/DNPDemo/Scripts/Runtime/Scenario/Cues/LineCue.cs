using System;
using System.Collections;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>
    /// A spoken line: shows the subtitle and plays the voice clip from the speaker's source.
    /// Without a clip, the subtitle stays for a time estimated from the text (see <see cref="LineTiming"/>).
    /// </summary>
    [Serializable]
    public sealed class LineCue : ScenarioCue
    {
        public ScenarioSpeaker Speaker;

        [TextArea(2, 6)]
        public string Text;

        [Tooltip("Recorded or synthesized voice. Leave empty until the audio is ready.")]
        public AudioClip Voice;

        public LineCue()
        {
        }

        public LineCue(ScenarioSpeaker speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            ScenarioStage stage = context.Stage;
            float duration = Voice != null ? Voice.length : context.LineTiming.EstimateSeconds(Text);
            stage.ShowLine(Speaker, Text, Voice);
            yield return new WaitForSeconds(duration);
            stage.EndLine(Speaker);
            float gap = context.LineTiming.GapSeconds;
            if (gap > 0f)
            {
                yield return new WaitForSeconds(gap);
            }
        }

        public override string ToString()
        {
            string speaker = Speaker == ScenarioSpeaker.Narrator ? "ナレーター" : Speaker.ToDisplayName();
            string text = string.IsNullOrEmpty(Text) ? string.Empty : Text.Replace('\n', ' ');
            if (text.Length > 28)
            {
                text = text.Substring(0, 28) + "…";
            }
            return $"{speaker}: {text}";
        }
    }
}
