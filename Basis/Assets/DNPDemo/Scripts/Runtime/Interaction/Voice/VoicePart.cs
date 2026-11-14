using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction.Voice
{
    /// <summary>
    /// Voice command, recognised by loudness rather than by words: while the trigger of the pen in voice mode is held, the
    /// microphone is captured (still muted, so nothing is sent) and the time the level stays above the threshold
    /// (<see cref="VoiceCommandSettings"/>) is added up. When the trigger is released after at least
    /// <see cref="RequiredSpeechSeconds"/> of sound, <see cref="Transcript"/> is shown as the recognition result and the
    /// part completes. See <see cref="VoiceCommandTool"/>.
    /// </summary>
    [Serializable]
    public sealed class VoicePart : InteractionPart
    {
        public const float DefaultRequiredSpeechSeconds = 1f;

        [Tooltip("The scripted sentence, shown as the recognition result.")]
        [TextArea(1, 3)]
        public string Transcript;

        [Tooltip("Seconds of sound above the threshold needed while the trigger is held. They do not have to be in one run.")]
        [Min(0.1f)] public float RequiredSpeechSeconds = DefaultRequiredSpeechSeconds;

        public VoicePart()
        {
        }

        public VoicePart(string instruction, string transcript, float requiredSpeechSeconds = DefaultRequiredSpeechSeconds)
        {
            Instruction = instruction;
            Transcript = transcript;
            RequiredSpeechSeconds = requiredSpeechSeconds;
        }

        public override PenMode Mode => PenMode.Voice;

        public override void Validate(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(Transcript))
            {
                problems.Add("音声指示の台本の文が空です。");
            }
            if (!(RequiredSpeechSeconds > 0f))
            {
                problems.Add("音声指示に必要な発話時間は 0 より大きくしてください。");
            }
        }

        public override string ToString() => $"音声指示（{RequiredSpeechSeconds:0.##} 秒以上）{Instruction}";
    }
}
