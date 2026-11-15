using System.Collections.Generic;
using Basis;
using Basis.BasisUI;
using Basis.Scripts.Device_Management;

namespace DNPDemo.BasisPolicy
{
    /// <summary>
    /// Pins Basis settings that decide what a visitor can grab or move, and hides the microphone icon.
    /// Settings persist on the device, so values changed during development are overwritten here once Basis has
    /// loaded them. The settings page is not on the demo menu, so they cannot be changed back during the demo.
    /// </summary>
    static class DemoSettingsPolicy
    {
        // The value SMModuleIcons maps to MicrophoneDisplayMode.Off.
        internal const string MicrophoneIconHidden = "hidden";

        internal static void Install()
        {
            BasisDeviceManagement.OnInitializationCompleted -= Apply;
            BasisDeviceManagement.OnInitializationCompleted += Apply;
        }

        static void Apply()
        {
            // The microphone icon. MicrophoneMuteLock keeps the microphone muted, so it would only ever show muted.
            Pin(BasisSettingsDefaults.MicrophoneIcon, MicrophoneIconHidden);
            // Grabbing another player's hair or clothes (trigger or grip).
            Pin(BasisSettingsDefaults.JiggleGrabInteractions, false);
            // Picking up props. The spatial pens are pickups, so this stays allowed; the world holds no other pickups
            // (the library and camera menus that spawn them are off), and each pen limits who may grab it.
            Pin(BasisSettingsDefaults.DisablePropPickup, false);
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
