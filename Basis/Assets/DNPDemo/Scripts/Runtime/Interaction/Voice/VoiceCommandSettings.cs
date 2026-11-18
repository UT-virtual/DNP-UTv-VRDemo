using System;
using UnityEngine;

namespace DNPDemo.Interaction.Voice
{
    /// <summary>
    /// The level above which the microphone counts as speech in voice commands. It depends on the venue's noise, so it is
    /// tuned on each headset from the operator panel (<see cref="Operator.MicrophoneThresholdControl"/>) and kept in that
    /// headset's PlayerPrefs.
    /// <para>
    /// The level is Basis' processed microphone RMS (<c>BasisLocalMicrophoneDriver.averageRms</c>), after the user's gain,
    /// denoiser, noise gate and AGC, averaged over the last 200 ms.
    /// </para>
    /// </summary>
    public static class VoiceCommandSettings
    {
        public const float DefaultThresholdDecibels = -40f;
        public const float MinThresholdDecibels = -70f;
        public const float MaxThresholdDecibels = -10f;

        const string ThresholdKey = "DNPDemo.VoiceCommand.ThresholdDb";

        static float? _thresholdDecibels;

        /// <summary>Raised on the main thread after the threshold changes.</summary>
        public static event Action Changed;

        /// <summary>Threshold in dBFS, within [<see cref="MinThresholdDecibels"/>, <see cref="MaxThresholdDecibels"/>].</summary>
        public static float ThresholdDecibels
        {
            get
            {
                _thresholdDecibels ??= Clamp(PlayerPrefs.GetFloat(ThresholdKey, DefaultThresholdDecibels));
                return _thresholdDecibels.Value;
            }
            set
            {
                float clamped = Clamp(value);
                if (Mathf.Approximately(clamped, ThresholdDecibels))
                {
                    return;
                }
                _thresholdDecibels = clamped;
                PlayerPrefs.SetFloat(ThresholdKey, clamped);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>The threshold as a linear RMS level, comparable with the microphone level.</summary>
        public static float ThresholdLevel => AudioLevel.FromDecibels(ThresholdDecibels);

        public static void ResetThreshold() => ThresholdDecibels = DefaultThresholdDecibels;

        public static float Clamp(float decibels) =>
            float.IsNaN(decibels) ? DefaultThresholdDecibels : Mathf.Clamp(decibels, MinThresholdDecibels, MaxThresholdDecibels);
    }
}
