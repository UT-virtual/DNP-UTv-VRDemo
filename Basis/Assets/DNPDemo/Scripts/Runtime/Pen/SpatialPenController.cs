using System;
using System.Collections.Generic;
using Basis.Scripts.Device_Management.Devices;
using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Operates the local player's pen and is the one source of pen input for the tools.
    /// <list type="bullet">
    /// <item>Grabbing and holding are Basis's pickup: grip near the pen with either hand, and it keeps the hand-to-pen
    /// offset; letting go leaves it floating there. This class only follows which hand holds it.</item>
    /// <item>While held: the holding hand's trigger counts as pressed (unless the press began on a menu or panel), and
    /// in select mode the right thumbstick up/down changes the selection radius.</item>
    /// <item>The mode comes from the main menu (<see cref="PenModeSelection"/>).</item>
    /// </list>
    /// <see cref="SpatialPenSync"/> hands it the pen of the local role with <see cref="Bind"/>.
    /// Tools read <see cref="IsTriggerHeld"/>, <see cref="TipPosition"/>, <see cref="Radius"/> and <see cref="Mode"/>,
    /// or listen to <see cref="TriggerPressed"/> / <see cref="TriggerReleased"/>; they act only while
    /// <see cref="Mode"/> matches the mode their part needs.
    /// </summary>
    public sealed class SpatialPenController : MonoBehaviour
    {
        const float TriggerPressThreshold = 0.6f;
        const float TriggerReleaseThreshold = 0.4f;

        [Tooltip("Seconds of a full thumbstick push to go from the smallest to the largest selection radius.")]
        [SerializeField, Min(0.1f)] float _radiusFullRangeSeconds = 1.5f;

        // Hands currently holding the pen, oldest first. Handing it over can briefly overlap two hands.
        readonly List<BasisInput> _holders = new();
        SpatialPenView _view;
        float _radius = PenGeometry.DefaultSelectRadius;
        bool _triggerDown;
        bool _triggerIgnoredUntilRelease;

        /// <summary>The pen this device operates, or null while the local player has no role.</summary>
        public SpatialPenView View => _view;

        public PenMode Mode => PenModeSelection.Current;
        public bool IsHeld => _holders.Count > 0;

        /// <summary>The input holding the pen (the latest grab), or null.</summary>
        public BasisInput Holder => _holders.Count > 0 ? _holders[_holders.Count - 1] : null;

        /// <summary>The pen is held and its trigger is down. This is the pen's "press" for every tool.</summary>
        public bool IsTriggerHeld => IsHeld && _triggerDown;

        public float Radius => _radius;

        /// <summary>World position of the tip, the centre of the selection sphere.</summary>
        public Vector3 TipPosition => _view != null ? _view.TipPosition : Vector3.zero;

        /// <summary>World position of the eraser sphere at the butt (radius <see cref="PenGeometry.EraserRadius"/>).</summary>
        public Vector3 EraserPosition => _view != null ? _view.EraserPosition : Vector3.zero;

        public event Action TriggerPressed;
        public event Action TriggerReleased;

        public PenState CurrentState => new()
        {
            Mode = Mode,
            Radius = _radius,
            IsHeld = IsHeld,
            IsTriggerHeld = IsTriggerHeld,
        };

        /// <summary>Makes <paramref name="view"/> the local pen (null when the local player has no role).</summary>
        public void Bind(SpatialPenView view)
        {
            if (_view == view)
            {
                return;
            }
            if (_view != null)
            {
                _view.Grabbed -= HandleGrabbed;
                _view.Released -= HandleReleased;
                _view.SetGrabbableLocally(false);
            }
            _holders.Clear();
            SetTriggerDown(false);
            _view = view;
            if (_view != null)
            {
                _view.Grabbed += HandleGrabbed;
                _view.Released += HandleReleased;
                _view.SetGrabbableLocally(true);
            }
            ShowState();
        }

        void OnEnable()
        {
            PenModeSelection.Changed += HandleModeChanged;
        }

        void OnDisable()
        {
            PenModeSelection.Changed -= HandleModeChanged;
            SetTriggerDown(false);
        }

        void OnDestroy() => Bind(null);

        void Update()
        {
            if (_view == null)
            {
                return;
            }
            PenInputFrame input = PenInputReader.Read(Holder);
            UpdateTrigger(input);
            if (IsHeld && Mode == PenMode.Select)
            {
                _radius = PenGeometry.AdjustRadius(_radius, input.StickY, Time.deltaTime, _radiusFullRangeSeconds);
            }
            ShowState();
        }

        void HandleGrabbed(BasisInput input)
        {
            if (input != null && !_holders.Contains(input))
            {
                _holders.Add(input);
            }
        }

        void HandleReleased(BasisInput input)
        {
            _holders.Remove(input);
            if (!IsHeld)
            {
                _triggerIgnoredUntilRelease = false;
                SetTriggerDown(false);
            }
        }

        /// <summary>
        /// The trigger is read with hysteresis so a half-pressed trigger does not flicker. A press that begins while
        /// the laser is on a menu or panel belongs to that UI and is ignored until released; a press that is already
        /// running continues even if the laser crosses a panel, so a stroke is not cut.
        /// </summary>
        void UpdateTrigger(PenInputFrame input)
        {
            if (!IsHeld)
            {
                _triggerIgnoredUntilRelease = false;
                SetTriggerDown(false);
                return;
            }
            if (_triggerDown)
            {
                if (input.Trigger < TriggerReleaseThreshold)
                {
                    SetTriggerDown(false);
                }
                return;
            }
            if (input.Trigger < TriggerReleaseThreshold)
            {
                _triggerIgnoredUntilRelease = false;
                return;
            }
            if (input.Trigger < TriggerPressThreshold || _triggerIgnoredUntilRelease)
            {
                return;
            }
            if (input.PointingAtUI)
            {
                _triggerIgnoredUntilRelease = true;
                return;
            }
            SetTriggerDown(true);
        }

        void SetTriggerDown(bool down)
        {
            if (_triggerDown == down)
            {
                return;
            }
            _triggerDown = down;
            if (down)
            {
                TriggerPressed?.Invoke();
            }
            else
            {
                TriggerReleased?.Invoke();
            }
        }

        void HandleModeChanged(PenMode mode)
        {
            // Switching modes from the menu ends any press, so a tool never sees a press that spans two modes.
            SetTriggerDown(false);
            _triggerIgnoredUntilRelease = true;
            ShowState();
        }

        void ShowState()
        {
            if (_view != null)
            {
                _view.ShowState(CurrentState);
            }
        }
    }
}
