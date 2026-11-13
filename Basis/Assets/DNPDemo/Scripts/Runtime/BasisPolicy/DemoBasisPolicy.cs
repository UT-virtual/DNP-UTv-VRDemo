using UnityEngine;

namespace DNPDemo.BasisPolicy
{
    /// <summary>
    /// Turns off the Basis features a visitor could trigger by accident during the demo, before any scene loads.
    /// This project only ships the demo, so the policy applies to every scene in play mode and in builds.
    /// <list type="bullet">
    /// <item><see cref="DemoMenuPolicy"/>: the main menu (Y) keeps only the demo's own entries.</item>
    /// <item><see cref="DemoInputPolicy"/>: X (mute), A (jump) and grip (descend) do nothing.</item>
    /// <item><see cref="DemoSettingsPolicy"/>: grabbing avatars and the playspace mover stay off, prop pickup stays on for the
    /// spatial pens, and the microphone icon is hidden.</item>
    /// <item><see cref="MicrophoneMuteLock"/>: the microphone stays muted; capture for voice commands still works.</item>
    /// </list>
    /// </summary>
    static class DemoBasisPolicy
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            DemoMenuPolicy.Install();
            DemoInputPolicy.Install();
            DemoSettingsPolicy.Install();
            MicrophoneMuteLock.Install();
        }
    }
}
