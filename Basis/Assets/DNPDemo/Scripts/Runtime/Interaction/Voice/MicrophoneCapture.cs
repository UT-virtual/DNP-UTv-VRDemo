using UnityEngine;

namespace DNPDemo.Interaction.Voice
{
    /// <summary>
    /// One owner's hold on the Basis microphone. While held the microphone records even though it stays muted
    /// (<c>BasisLocalMicrophoneDriver.AddCaptureHold</c>); nothing is sent, because Basis transmits only while unmuted.
    /// <para>
    /// Basis counts holds across all owners, so every hold must be released exactly once. This class keeps track of its
    /// own hold: <see cref="Begin"/> and <see cref="End"/> may be called any number of times, and an owner only has to
    /// call <see cref="End"/> when it is disabled or destroyed.
    /// </para>
    /// </summary>
    public sealed class MicrophoneCapture
    {
        static bool _reportedNoDevice;

        public bool IsCapturing { get; private set; }

        /// <summary>
        /// The microphone level (linear RMS, averaged over the last 200 ms) while this hold is active, otherwise 0.
        /// It starts at 0 and needs a moment after <see cref="Begin"/> while the microphone starts.
        /// </summary>
        public float Level
        {
            get
            {
#if BASIS_DISABLE_MICROPHONE
                return 0f;
#else
                return IsCapturing ? BasisLocalMicrophoneDriver.averageRms : 0f;
#endif
            }
        }

        public void Begin()
        {
            if (IsCapturing)
            {
                return;
            }
            IsCapturing = true;
#if BASIS_DISABLE_MICROPHONE
            Debug.LogWarning("[DNPDemo] マイクが無効なビルドのため、音声を取り込めません。");
#else
            if (Microphone.devices.Length == 0 && !_reportedNoDevice)
            {
                _reportedNoDevice = true;
                Debug.LogWarning("[DNPDemo] マイクが見つからないため、音声を取り込めません。");
            }
            BasisLocalMicrophoneDriver.AddCaptureHold();
#endif
        }

        public void End()
        {
            if (!IsCapturing)
            {
                return;
            }
            IsCapturing = false;
#if !BASIS_DISABLE_MICROPHONE
            BasisLocalMicrophoneDriver.ReleaseCaptureHold();
#endif
        }
    }
}
