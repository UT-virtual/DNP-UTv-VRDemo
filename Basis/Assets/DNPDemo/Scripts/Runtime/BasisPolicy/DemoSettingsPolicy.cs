using System.Collections.Generic;
using Basis;
using Basis.BasisUI;
using Basis.Scripts.Device_Management;

namespace DNPDemo.BasisPolicy
{
    /// <summary>
    /// Pins Basis settings that would let a visitor grab or move things the demo does not expect.
    /// Settings persist on the device, so values changed during development are overwritten here once Basis has
    /// loaded them. The settings page is not on the demo menu, so they cannot be changed back during the demo.
    /// </summary>
    static class DemoSettingsPolicy
    {
        internal static void Install()
        {
            BasisDeviceManagement.OnInitializationCompleted -= Apply;
            BasisDeviceManagement.OnInitializationCompleted += Apply;
        }

        static void Apply()
        {
#if !BASIS_DISABLE_MICROPHONE
            // MicrophoneMuteLock keeps the microphone muted; starting muted avoids unmuting for a frame at launch.
            Pin(BasisSettingsDefaults.MicStartBehavior, BasisLocalMicrophoneDriver.SettingStartOff);
#endif
            // Grabbing another player's hair or clothes (trigger or grip).
            Pin(BasisSettingsDefaults.JiggleGrabInteractions, false);
            // Picking up props (trigger or grip).
            Pin(BasisSettingsDefaults.DisablePropPickup, true);
            // Dragging the play space with the grip.
            Pin(BasisSettingsDefaults.EnablePlayspaceMover, false);
        }

        static void Pin<T>(BasisSettingsBinding<T> binding, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(binding.RawValue, value))
            {
                binding.SetValue(value);
            }
        }
    }
}
