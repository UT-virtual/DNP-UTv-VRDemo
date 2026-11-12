using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Common;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.Drivers;
using Basis.Scripts.TransformBinders.BoneControl;
using Basis.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNPDemo.Pen
{
    /// <summary>One frame of the controls that drive the pen.</summary>
    public struct PenInputFrame
    {
        public bool HandTracked;
        public Pose Hand;
        public bool Grip;
        /// <summary>Analog trigger, 0..1.</summary>
        public float Trigger;
        /// <summary>Thumbstick up/down, -1..1, with the dead zone applied.</summary>
        public float StickY;
        /// <summary>The hand's laser is on a UI element, so a trigger press there is meant for the UI.</summary>
        public bool PointingAtUI;
        /// <summary>The pen can be grabbed from any distance (desktop, where the hand cannot reach out).</summary>
        public bool IgnoresReach;
    }

    /// <summary>
    /// The one place that reads the controls of the pen hand from Basis. In VR it reads the right controller and the
    /// right hand pose; on desktop and in the editor, keys stand in for the controller (G grip, T trigger, N/M radius)
    /// and the hand sits in front of the camera.
    /// </summary>
    public static class PenInputReader
    {
        const BasisBoneTrackedRole PenHand = BasisBoneTrackedRole.RightHand;

        // Where the stand-in hand sits relative to the desktop camera.
        static readonly Vector3 DesktopHandOffset = new(0.15f, -0.15f, 0.45f);

        public static PenInputFrame Read()
        {
            return BasisDeviceManagement.IsUserInDesktop() ? ReadDesktop() : ReadController();
        }

        static PenInputFrame ReadController()
        {
            var frame = new PenInputFrame();
            BasisDeviceManagement management = BasisDeviceManagement.Instance;
            if (management == null || !management.FindDevice(out BasisInput input, PenHand) || input == null)
            {
                return frame;
            }
            BasisInputState state = input.CurrentInputState;
            frame.Grip = state.GripButton;
            frame.Trigger = state.Trigger;
            frame.StickY = state.Primary2DAxisDeadZoned.y;
            frame.PointingAtUI = input.HasRaycaster && input.BasisUIRaycast != null
                && input.BasisUIRaycast.WasCorrectLayer && input.BasisUIRaycast.HadRaycastUITarget;
            frame.HandTracked = TryGetHandPose(out frame.Hand);
            return frame;
        }

        static PenInputFrame ReadDesktop()
        {
            var frame = new PenInputFrame { IgnoresReach = true };
            frame.HandTracked = TryGetHandPose(out frame.Hand);
            Keyboard keyboard = Keyboard.current;
            bool typing = BasisInputModuleHandler.Instance != null && BasisInputModuleHandler.Instance.HasHoverONInput;
            if (keyboard == null || typing)
            {
                return frame;
            }
            frame.Grip = keyboard.gKey.isPressed;
            frame.Trigger = keyboard.tKey.isPressed ? 1f : 0f;
            frame.StickY = (keyboard.mKey.isPressed ? 1f : 0f) - (keyboard.nKey.isPressed ? 1f : 0f);
            return frame;
        }

        /// <summary>
        /// World pose of the pen hand: the right hand in VR, a point in front of the camera on desktop.
        /// Read again just before rendering so a held pen does not lag behind the hand.
        /// </summary>
        public static bool TryGetHandPose(out Pose pose)
        {
            pose = Pose.identity;
            if (BasisDeviceManagement.IsUserInDesktop())
            {
                BasisLocalCameraDriver cameraDriver = BasisLocalCameraDriver.Instance;
                if (cameraDriver == null || cameraDriver.Camera == null)
                {
                    return false;
                }
                Transform camera = cameraDriver.Camera.transform;
                pose = new Pose(camera.TransformPoint(DesktopHandOffset), camera.rotation);
                return true;
            }
            BasisLocalBoneControl hand = BasisLocalBoneDriver.RightHandControl;
            if (BasisLocalPlayer.Instance == null || hand == null)
            {
                return false;
            }
            BasisCalibratedCoords world = hand.OutgoingWorldData;
            pose = new Pose(world.position, world.rotation);
            return true;
        }
    }
}
