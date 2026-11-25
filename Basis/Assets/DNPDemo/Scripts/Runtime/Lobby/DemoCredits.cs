using System.Collections.Generic;

namespace DNPDemo.Lobby
{
    /// <summary>
    /// The credits shown in the app, on the role page of the lobby. Every name is also listed in THIRD_PARTY_NOTICES.md
    /// (checked by a test); a voice or sound source added to the demo goes into both.
    /// </summary>
    public static class DemoCredits
    {
        /// <summary>The VOICEVOX characters of the voice lines, as the VOICEVOX terms of use ask them to be credited.</summary>
        public static readonly IReadOnlyList<string> Voices = new[]
        {
            "VOICEVOX:春日部つむぎ",
            "VOICEVOX:青山龍星",
            "VOICEVOX:ずんだもん",
        };

        /// <summary>The sites the sound effects come from.</summary>
        public static readonly IReadOnlyList<string> SoundEffects = new[]
        {
            "効果音ラボ",
            "魔王魂",
        };

        const string Separator = " / ";

        /// <summary>The credit text, one line for the voices and one for the sound effects.</summary>
        public static string Text =>
            $"音声: {string.Join(Separator, Voices)}\n効果音: {string.Join(Separator, SoundEffects)}";
    }
}
