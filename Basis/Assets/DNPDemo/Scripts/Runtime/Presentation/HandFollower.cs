using Basis.Scripts.Device_Management;
using Basis.Scripts.Drivers;
using Basis.Scripts.TransformBinders.BoneControl;
using UnityEngine;

namespace DNPDemo.Presentation
{
    /// <summary>
    /// Keeps a world-space panel just above one of the local player's hands (<see cref="Side"/>), always turned to face
    /// the eyes so it reads the same however the wrist is turned. The hand is the bone after IK, the same pose Basis
    /// pickups hold on to.
    /// <para>
    /// On desktop, or while that hand has no tracker (controller off), the panel instead follows the view, low and to
    /// the side of the hand.
    /// </para>
    /// Runs in Basis' before-render pass so it never trails the hand by a frame.
    /// </summary>
    public sealed class HandFollower : MonoBehaviour
    {
        // A jump larger than this (teleport, respawn, switching between hand and view) is taken at once instead of eased.
        const float SnapDistance = 0.5f;

        [Tooltip("Hand to follow at start. Owners change it at run time through Side.")]
        [SerializeField] HandSide _side = HandSide.Left;

        [Tooltip("Height of the panel's pivot above the hand, in metres. With the pivot at the bottom edge this is the gap below the panel.")]
        [SerializeField] float _heightAboveHand = 0.08f;

        [Tooltip("How quickly the panel catches up with the hand. 0 attaches it rigidly.")]
        [SerializeField, Min(0f)] float _handFollowSharpness = 20f;

        [Header("Following the view (desktop, or the hand is not tracked)")]
        [SerializeField, Min(0.1f)] float _viewDistance = 0.9f;

        [Tooltip("Sideways offset in metres, towards the followed hand's side.")]
        [SerializeField, Min(0f)] float _viewSideOffset = 0.35f;

        [Tooltip("Vertical offset from eye height in metres. Negative places the panel below the line of sight.")]
        [SerializeField] float _viewHeightOffset = -0.2f;

        [Tooltip("How quickly the panel catches up with head turns. 0 locks it rigidly to the view direction.")]
        [SerializeField, Min(0f)] float _viewFollowSharpness = 4f;

        Vector3 _position;
        bool _hasPosition;
        float _yaw;
        bool _hasYaw;

        /// <summary>The hand the panel stays above.</summary>
        public HandSide Side
        {
            get => _side;
            set => _side = value;
        }

        void OnEnable()
        {
            _hasPosition = false;
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
            Vector3 target;
            float sharpness;
            if (TryGetHandPosition(_side, out Vector3 hand))
            {
                target = hand + Vector3.up * _heightAboveHand;
                sharpness = _handFollowSharpness;
                _hasYaw = false;
            }
            else
            {
                target = ViewPosition();
                // The yaw is already eased; easing the position as well would only add lag.
                sharpness = 0f;
            }
            if (!_hasPosition || sharpness <= 0f || (target - _position).sqrMagnitude > SnapDistance * SnapDistance)
            {
                _position = target;
                _hasPosition = true;
            }
            else
            {
                _position = Vector3.Lerp(_position, target, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
            }
            transform.SetPositionAndRotation(_position, HeadPose.FacingEyes(_position, transform.rotation));
        }

        Vector3 ViewPosition()
        {
            float targetYaw = HeadPose.GetYaw(_yaw);
            if (!_hasYaw || _viewFollowSharpness <= 0f)
            {
                _yaw = targetYaw;
                _hasYaw = true;
            }
            else
            {
                _yaw = Mathf.LerpAngle(_yaw, targetYaw, 1f - Mathf.Exp(-_viewFollowSharpness * Time.deltaTime));
            }
            float side = _side == HandSide.Left ? -_viewSideOffset : _viewSideOffset;
            return HeadPose.PointAhead(_yaw, _viewDistance, side, _viewHeightOffset);
        }

        static bool TryGetHandPosition(HandSide side, out Vector3 position)
        {
            position = default;
            if (BasisDeviceManagement.IsUserInDesktop())
            {
                return false;
            }
            BasisLocalBoneControl control = side == HandSide.Left
                ? BasisLocalBoneDriver.LeftHandControl
                : BasisLocalBoneDriver.RightHandControl;
            if (control == null || control.HasTracked != BasisHasTracked.HasTracker)
            {
                return false;
            }
            position = control.OutgoingWorldData.position;
            return true;
        }
    }
}
