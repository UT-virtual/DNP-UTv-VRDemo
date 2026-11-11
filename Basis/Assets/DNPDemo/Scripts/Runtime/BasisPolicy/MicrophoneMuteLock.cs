namespace DNPDemo.BasisPolicy
{
    /// <summary>
    /// Keeps the microphone muted so nothing is ever sent as voice chat. Whatever unmutes it (a button, a menu, a key),
    /// it is muted again at once. Capturing while muted through <c>BasisLocalMicrophoneDriver.AddCaptureHold</c>,
    /// which voice commands use, is unaffected.
    /// </summary>
    static class MicrophoneMuteLock
    {
        internal static void Install()
        {
#if !BASIS_DISABLE_MICROPHONE
            BasisLocalMicrophoneDriver.OnInitializedAction -= HandleInitialized;
            BasisLocalMicrophoneDriver.OnInitializedAction += HandleInitialized;
            BasisLocalMicrophoneDriver.OnPausedAction -= HandlePausedChanged;
            BasisLocalMicrophoneDriver.OnPausedAction += HandlePausedChanged;
#endif
        }

#if !BASIS_DISABLE_MICROPHONE
        static void HandleInitialized(bool initialized)
        {
            if (initialized)
            {
                EnsureMuted();
            }
        }

        static void HandlePausedChanged(bool paused)
        {
            if (!paused)
            {
                EnsureMuted();
            }
        }

        static void EnsureMuted()
        {
            if (!BasisLocalMicrophoneDriver.isPaused)
            {
                BasisLocalMicrophoneDriver.ToggleIsPaused();
            }
        }
#endif
    }
}
