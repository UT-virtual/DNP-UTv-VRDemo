using System.Collections;
using Basis.Scripts.BasisSdk.Players;
using DNPDemo.Roles;
using UnityEngine;

namespace DNPDemo.Players
{
    /// <summary>
    /// Applies the assistant avatar for the locally assigned scenario role.
    /// </summary>
    public sealed class DemoAssistantAvatarService : MonoBehaviour
    {
        [SerializeField] DemoRoleService _roleService;
        [SerializeField] string _playerAAddressableKey = "AssistantA";
        [SerializeField] string _playerBAddressableKey = "AssistantB";

        string _loadedAddressableKey;
        int _loadGeneration;

        void OnEnable()
        {
            if (_roleService == null)
            {
                Debug.LogError("[DNPDemo] DemoAssistantAvatarService needs a DemoRoleService reference.", this);
                return;
            }

            _roleService.LocalRoleChanged += HandleLocalRoleChanged;
            HandleLocalRoleChanged(_roleService.LocalRole);
        }

        void OnDisable()
        {
            if (_roleService != null)
            {
                _roleService.LocalRoleChanged -= HandleLocalRoleChanged;
            }

            _loadGeneration++;
        }

        void HandleLocalRoleChanged(DemoRole role)
        {
            string addressableKey = role switch
            {
                DemoRole.PlayerA => _playerAAddressableKey,
                DemoRole.PlayerB => _playerBAddressableKey,
                _ => null,
            };

            if (string.IsNullOrWhiteSpace(addressableKey) || addressableKey == _loadedAddressableKey)
            {
                return;
            }

            if (BasisLocalPlayer.Instance == null)
            {
                Debug.LogWarning("[DNPDemo] Local player is not initialized; waiting for it before applying the avatar.", this);
                StartCoroutine(WaitForLocalPlayerAndApply(addressableKey));
                return;
            }

            ApplyAvatarAsync(BasisLocalPlayer.Instance, addressableKey);
        }

        IEnumerator WaitForLocalPlayerAndApply(string addressableKey)
        {
            while (BasisLocalPlayer.Instance == null)
            {
                yield return null;
            }

            if (isActiveAndEnabled && addressableKey != _loadedAddressableKey)
            {
                ApplyAvatarAsync(BasisLocalPlayer.Instance, addressableKey);
            }
        }

        async void ApplyAvatarAsync(BasisLocalPlayer localPlayer, string addressableKey)
        {
            int loadGeneration = ++_loadGeneration;

            BasisLoadableBundle bundle = new BasisLoadableBundle
            {
                UnlockPassword = "N/A",
                BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle
                {
                    RemoteBeeFileLocation = addressableKey,
                },
                BasisBundleConnector = new BasisBundleConnector(),
                BasisLocalEncryptedBundle = new BasisStoredEncryptedBundle(),
            };

            try
            {
                await localPlayer.CreateAvatarFromMode(BasisLoadMode.Local, bundle);
                if (isActiveAndEnabled && loadGeneration == _loadGeneration)
                {
                    _loadedAddressableKey = addressableKey;
                    Debug.Log($"[DNPDemo] Applied assistant avatar: {addressableKey}", this);
                }
            }
            catch (System.Exception exception)
            {
                if (isActiveAndEnabled && loadGeneration == _loadGeneration)
                {
                    Debug.LogError($"[DNPDemo] Failed to apply assistant avatar '{addressableKey}'. {exception}", this);
                }
            }
        }
    }
}
