using Basis.Scripts.Drivers;
using UnityEngine;

namespace DNPDemo.Presentation
{
    /// <summary>Placement of world-space UI relative to the local player's view.</summary>
    public static class HeadPose
    {
        public static bool IsAvailable => BasisLocalCameraDriver.HasInstance;

        public static Vector3 Position => BasisLocalCameraDriver.Position;

        /// <summary>Heading of the view on the horizontal plane, in degrees. Keeps <paramref name="fallback"/> when looking straight up or down.</summary>
        public static float GetYaw(float fallback)
        {
            Vector3 forward = BasisLocalCameraDriver.Rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f)
            {
                return fallback;
            }
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Puts <paramref name="target"/> <paramref name="distance"/> metres ahead along <paramref name="yaw"/>,
        /// offset vertically from eye height, turned so its front faces the eyes.
        /// </summary>
        public static void Place(Transform target, float yaw, float distance, float heightOffset)
        {
            Vector3 eye = Position;
            Vector3 position = eye + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * distance + Vector3.up * heightOffset;
            Vector3 facing = position - eye;
            target.SetPositionAndRotation(position, facing.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(facing) : target.rotation);
        }
    }
}
