using UnityEngine;

namespace DNPDemo.BasisPolicy
{
    /// <summary>
    /// Turns off the Basis features a visitor could trigger by accident during the demo, before any scene loads.
    /// This project only ships the demo, so the policy applies to every scene in play mode and in builds.
    /// <list type="bullet">
    /// <item><see cref="DemoMenuPolicy"/>: the main menu (Y) keeps only the demo's own entries.</item>
    /// </list>
    /// </summary>
    static class DemoBasisPolicy
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            DemoMenuPolicy.Install();
        }
    }
}
