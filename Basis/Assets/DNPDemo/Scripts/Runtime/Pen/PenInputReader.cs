using Basis.Scripts.Device_Management;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;
using Basis.Scripts.UI;
using UnityEngine.InputSystem;

namespace DNPDemo.Pen
{
    /// <summary>One frame of the controls that operate a held pen.</summary>
    public struct PenInputFrame
    {
        /// <summary>Analog trigger of the hand holding the pen, 0..1.</summary>
        public float Trigger;
        /// <summary>Right thumbstick up/down, -1..1, with the dead zone applied.</summary>
        public float StickY;
        /// <summary>The holding hand's laser is on a UI element, so a trigger press there is meant for the UI.</summary>
        public bool PointingAtUI;
    }

    /// <summary>
    /// The one place that reads the pen's controls from Basis. Grabbing is Basis's own pickup input; this reads what
    /// happens while the pen is held:
    /// <list type="bullet">
    /// <item>VR: the trigger of the hand that holds the pen, and the right thumbstick up/down whichever hand holds it
    /// (the left stick walks).</item>
    /// <item>Desktop and editor: keys stand in for the controller (T trigger, N/M radius), because the mouse buttons
    /// already grab and drop.</item>
    /// </list>
    /// </summary>
    public static class PenInputReader
    {
        public static PenInputFrame Read(BasisInput holder) =>
            BasisDeviceManagement.IsUserInDesktop() ? ReadDesktop() : ReadController(holder);

        static PenInputFrame ReadController(BasisInput holder)
        {
            var frame = new PenInputFrame();
            if (holder != null)
            {
                frame.Trigger = holder.CurrentInputState.Trigger;
                frame.PointingAtUI = holder.HasRaycaster && holder.BasisUIRaycast != null
                    && holder.BasisUIRaycast.WasCorrectLayer && holder.BasisUIRaycast.HadRaycastUITarget;
            }
            BasisDeviceManagement management = BasisDeviceManagement.Instance;
            if (management != null && management.FindDevice(out BasisInput rightHand, BasisBoneTrackedRole.RightHand) && rightHand != null)
            {
                frame.StickY = rightHand.CurrentInputState.Primary2DAxisDeadZoned.y;
            }
            return frame;
        }

        static PenInputFrame ReadDesktop()
        {
            var frame = new PenInputFrame();
            Keyboard keyboard = Keyboard.current;
            bool typing = BasisInputModuleHandler.Instance != null && BasisInputModuleHandler.Instance.HasHoverONInput;
            if (keyboard == null || typing)
            {
                return frame;
            }
            frame.Trigger = keyboard.tKey.isPressed ? 1f : 0f;
            frame.StickY = (keyboard.mKey.isPressed ? 1f : 0f) - (keyboard.nKey.isPressed ? 1f : 0f);
            return frame;
        }
    }
}
