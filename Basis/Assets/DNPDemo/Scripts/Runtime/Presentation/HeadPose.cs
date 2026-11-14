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
            Vector3 position = PointAhead(yaw, distance, 0f, heightOffset);
            target.SetPositionAndRotation(position, FacingEyes(position, target.rotation));
        }

        /// <summary>
        /// The point <paramref name="distance"/> metres ahead along <paramref name="yaw"/>, moved
        /// <paramref name="sideOffset"/> metres to the right (negative is left) and vertically from eye height.
        /// </summary>
        public static Vector3 PointAhead(float yaw, float distance, float sideOffset, float heightOffset)
        {
            Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
            return Position + heading * new Vector3(sideOffset, 0f, distance) + Vector3.up * heightOffset;
        }

        /// <summary>
        /// Rotation that turns a panel at <paramref name="position"/> so its front faces the eyes, keeping world up.
        /// Returns <paramref name="fallback"/> when the panel is at the eyes.
        /// </summary>
        public static Quaternion FacingEyes(Vector3 position, Quaternion fallback)
        {
            Vector3 facing = position - Position;
            return facing.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(facing) : fallback;
        }
    }
}
