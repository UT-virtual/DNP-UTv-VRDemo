namespace DNPDemo.BasisPolicy
{
    /// <summary>
    /// Keeps the microphone muted so nothing is ever sent as voice chat. Whatever tries to unmute it (a button, a menu,
    /// a key) is ignored by <c>BasisLocalMicrophoneDriver</c>, and the microphone starts muted. Capturing while muted
    /// through <c>BasisLocalMicrophoneDriver.AddCaptureHold</c>, which voice commands use, is unaffected.
    /// </summary>
    static class MicrophoneMuteLock
    {
        internal static void Install()
        {
#if !BASIS_DISABLE_MICROPHONE
            BasisLocalMicrophoneDriver.SetUnmuteLocked(true);
#endif
        }
    }
}
