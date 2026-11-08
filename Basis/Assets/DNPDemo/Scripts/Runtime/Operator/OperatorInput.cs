using Basis.Scripts.Device_Management;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;
using DNPDemo.Scenario;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNPDemo.Operator
{
    /// <summary>
    /// Staff controls. In VR, pressing both thumbsticks in for <see cref="_holdSeconds"/> toggles the operator panel;
    /// a visitor will not do that by accident. On desktop and in the editor, function keys do the same and also
    /// step the scenario directly.
    /// </summary>
    public sealed class OperatorInput : MonoBehaviour
    {
        [SerializeField] OperatorPanel _panel;
        [SerializeField] ScenarioDirector _director;

        [SerializeField, Min(0.1f)] float _holdSeconds = 2f;

        float _heldSeconds;
        bool _waitForRelease;

        void Update()
        {
            UpdateControllerGesture();
            UpdateKeyboard();
        }

        void UpdateControllerGesture()
        {
            bool held = IsStickClickHeld(BasisBoneTrackedRole.LeftHand) && IsStickClickHeld(BasisBoneTrackedRole.RightHand);
            if (!held)
            {
                _heldSeconds = 0f;
                _waitForRelease = false;
                return;
            }
            if (_waitForRelease)
            {
                return;
            }
            _heldSeconds += Time.unscaledDeltaTime;
            if (_heldSeconds >= _holdSeconds)
            {
                _waitForRelease = true;
                TogglePanel();
            }
        }

        void UpdateKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                TogglePanel();
            }
            if (_director == null)
            {
                return;
            }
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                _director.RequestCommand(OperatorCommand.Back);
            }
            if (keyboard.f11Key.wasPressedThisFrame)
            {
                _director.RequestCommand(OperatorCommand.Next);
            }
        }

        void TogglePanel()
        {
            if (_panel != null)
            {
                _panel.Toggle();
            }
        }

        static bool IsStickClickHeld(BasisBoneTrackedRole role)
        {
            BasisDeviceManagement management = BasisDeviceManagement.Instance;
            return management != null
                && management.FindDevice(out BasisInput input, role)
                && input != null
                && input.CurrentInputState.Primary2DAxisClick;
        }
    }
}
