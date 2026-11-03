using System;
using UnityEngine;

namespace DNPDemo.Session
{
    /// <summary>
    /// Holds Android's WifiManager.MulticastLock, without which the Wi-Fi stack may filter out
    /// broadcast packets not addressed to this device. Requires CHANGE_WIFI_MULTICAST_STATE in the manifest.
    /// No-op on other platforms and in the editor.
    /// </summary>
    public sealed class AndroidMulticastLock : IDisposable
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject _lock;
#endif

        public void Acquire()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_lock != null)
            {
                return;
            }
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
                using AndroidJavaObject wifi = context.Call<AndroidJavaObject>("getSystemService", "wifi");
                _lock = wifi.Call<AndroidJavaObject>("createMulticastLock", "DNPDemoDiscovery");
                _lock.Call("setReferenceCounted", false);
                _lock.Call("acquire");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DNPDemo] Could not acquire the multicast lock; host discovery may not receive beacons. {e.Message}");
                _lock?.Dispose();
                _lock = null;
            }
#endif
        }

        public void Release()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_lock == null)
            {
                return;
            }
            try
            {
                _lock.Call("release");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DNPDemo] Releasing the multicast lock failed: {e.Message}");
            }
            _lock.Dispose();
            _lock = null;
#endif
        }

        public void Dispose() => Release();
    }
}
