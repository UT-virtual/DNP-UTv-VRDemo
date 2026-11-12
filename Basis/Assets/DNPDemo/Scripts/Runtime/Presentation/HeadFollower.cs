using Basis.Scripts.Drivers;
using UnityEngine;

namespace DNPDemo.Presentation
{
    /// <summary>
    /// Keeps a world-space panel in front of the local player's view, following head turns with a lag so it
    /// does not shake with small head movements. Runs in Basis' before-render pass so it never trails the camera by a frame.
    /// </summary>
    public sealed class HeadFollower : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float _distance = 1.2f;

        [Tooltip("Vertical offset from eye height in metres. Negative places the panel below the line of sight.")]
        [SerializeField] float _heightOffset = -0.35f;

        [Tooltip("How quickly the panel catches up with head turns. 0 locks it rigidly to the view direction.")]
        [SerializeField, Min(0f)] float _followSharpness = 4f;

        float _yaw;
        bool _hasYaw;

        void OnEnable()
        {
            _hasYaw = false;
            BasisLocalCameraDriver.BeforeRender += Follow;
        }

        void OnDisable()
        {
            BasisLocalCameraDriver.BeforeRender -= Follow;
        }

        void Follow()
        {
            if (!HeadPose.IsAvailable)
            {
                return;
            }
            float targetYaw = HeadPose.GetYaw(_yaw);
            if (!_hasYaw || _followSharpness <= 0f)
            {
                _yaw = targetYaw;
                _hasYaw = true;
            }
            else
            {
                _yaw = Mathf.LerpAngle(_yaw, targetYaw, 1f - Mathf.Exp(-_followSharpness * Time.deltaTime));
            }
            HeadPose.Place(transform, _yaw, _distance, _heightOffset);
        }
    }
}
