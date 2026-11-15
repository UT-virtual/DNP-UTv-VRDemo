using System;
using UnityEngine;

namespace DNPDemo.Interaction.Voice
{
    /// <summary>
    /// Which microphone Basis records from. Basis opens a device by name and keeps the choice per device mode (desktop,
    /// VR) in PlayerPrefs; the first time it takes the first device Unity lists, which on a PC may be a virtual device
    /// that stays silent (a streaming app's microphone, for example). The demo removes Basis' settings menu, so the
    /// operator panel switches it through this class (<see cref="Operator.MicrophoneThresholdControl"/>).
    /// <para>
    /// The choice goes into Basis' settings. While the microphone is captured, Basis reopens it on the new device within
    /// its device scan (2 seconds), even though it stays muted.
    /// </para>
    /// </summary>
    public static class MicrophoneSelection
    {
        /// <summary>Raised when the chosen device or the list of devices may have changed.</summary>
        public static event Action Changed
        {
            add
            {
                bool first = _changed == null;
                _changed += value;
#if !BASIS_DISABLE_MICROPHONE
                // Basis' events are joined once, while anyone listens.
                if (first && _changed != null)
                {
                    SMDMicrophone.OnMicrophoneSettingsChanged += HandleSettingsChanged;
                    SMDMicrophone.OnMicrophoneDevicesChanged += HandleDevicesChanged;
                }
#endif
            }
            remove
            {
                _changed -= value;
#if !BASIS_DISABLE_MICROPHONE
                if (_changed == null)
                {
                    SMDMicrophone.OnMicrophoneSettingsChanged -= HandleSettingsChanged;
                    SMDMicrophone.OnMicrophoneDevicesChanged -= HandleDevicesChanged;
                }
#endif
            }
        }

        static Action _changed;

        /// <summary>The chosen device's name, or empty when none is chosen.</summary>
        public static string Current
        {
            get
            {
#if BASIS_DISABLE_MICROPHONE
                return string.Empty;
#else
                return SMDMicrophone.Current.Microphone ?? string.Empty;
#endif
            }
        }

        /// <summary>The devices that can be chosen, as Unity names them.</summary>
        public static string[] Devices
        {
            get
            {
#if BASIS_DISABLE_MICROPHONE
                return Array.Empty<string>();
#else
                string[] known = SMDMicrophone.MicrophoneDevices;
                return known != null && known.Length > 0 ? known : Microphone.devices;
#endif
            }
        }

        /// <summary>Chooses the device after the current one in <see cref="Devices"/>, wrapping around.</summary>
        public static void SelectNext()
        {
            string next = Next(Devices, Current);
            if (string.IsNullOrEmpty(next) || next == Current)
            {
                return;
            }
#if !BASIS_DISABLE_MICROPHONE
            SMDMicrophone.SetMicrophone(next);
            Debug.Log($"[DNPDemo] マイクを「{next}」に切り替えました。");
#endif
        }

        /// <summary>
        /// The device after <paramref name="current"/> in <paramref name="devices"/>, wrapping around. The first device
        /// when <paramref name="current"/> is not in the list; null when the list is empty.
        /// </summary>
        public static string Next(string[] devices, string current)
        {
            if (devices == null || devices.Length == 0)
            {
                return null;
            }
            int index = Array.IndexOf(devices, current);
            return devices[(index + 1) % devices.Length];
        }

#if !BASIS_DISABLE_MICROPHONE
        static void HandleSettingsChanged(SMDMicrophone.MicSettings settings) => _changed?.Invoke();

        static void HandleDevicesChanged() => _changed?.Invoke();
#endif
    }
}
