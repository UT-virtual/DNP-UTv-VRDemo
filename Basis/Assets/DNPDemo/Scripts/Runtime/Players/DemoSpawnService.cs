using Basis.Scripts.BasisSdk.Players;
using DNPDemo.Roles;
using UnityEngine;

namespace DNPDemo.Players
{
    /// <summary>
    /// Moves the local player to the spawn point of their role once it is assigned,
    /// and back to the lobby point when they leave the session. <see cref="ReturnToRoleSpawn"/> moves them back to it
    /// when the run returns to the lobby for the next visitors.
    /// </summary>
    public sealed class DemoSpawnService : MonoBehaviour
    {
        [SerializeField] DemoRoleService _roleService;
        [SerializeField] Transform _lobbySpawn;
        [SerializeField] Transform _playerASpawn;
        [SerializeField] Transform _playerBSpawn;

        void OnEnable()
        {
            if (_roleService == null)
            {
                Debug.LogError("[DNPDemo] DemoSpawnService needs a DemoRoleService reference.", this);
                return;
            }
            _roleService.LocalRoleChanged += HandleLocalRoleChanged;
        }

        void OnDisable()
        {
            if (_roleService != null)
            {
                _roleService.LocalRoleChanged -= HandleLocalRoleChanged;
            }
        }

        /// <summary>Moves the local player to the spawn point of their current role (the lobby point without a role).</summary>
        public void ReturnToRoleSpawn()
        {
            if (_roleService != null)
            {
                HandleLocalRoleChanged(_roleService.LocalRole);
            }
        }

        void HandleLocalRoleChanged(DemoRole role)
        {
            Transform target = role switch
            {
                DemoRole.PlayerA => _playerASpawn,
                DemoRole.PlayerB => _playerBSpawn,
                _ => _lobbySpawn,
            };
            if (target == null || BasisLocalPlayer.Instance == null)
            {
                return;
            }
            BasisLocalPlayer.Instance.Teleport(target.position, target.rotation, mode: BasisTeleportMode.WorldFeet);
        }
    }
}
