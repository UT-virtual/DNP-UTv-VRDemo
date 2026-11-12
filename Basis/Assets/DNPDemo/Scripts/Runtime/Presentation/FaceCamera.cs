using Basis.Scripts.Drivers;
using UnityEngine;

namespace DNPDemo.Presentation
{
    /// <summary>Turns a world-space panel around the vertical axis so its front faces the local player's view.</summary>
    public sealed class FaceCamera : MonoBehaviour
    {
        void OnEnable()
        {
            BasisLocalCameraDriver.BeforeRender += Face;
        }

        void OnDisable()
        {
            BasisLocalCameraDriver.BeforeRender -= Face;
        }

        void Face()
        {
            if (!HeadPose.IsAvailable)
            {
                return;
            }
            Vector3 away = transform.position - HeadPose.Position;
            away.y = 0f;
            if (away.sqrMagnitude > 1e-6f)
            {
                transform.rotation = Quaternion.LookRotation(away);
            }
        }
    }
}
