using System;
using Basis.Scripts.BasisSdk.Players;
using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Drives the local player's pen and is the one source of pen input for the tools.
    /// <list type="bullet">
    /// <item>Grip near the pen grabs it and keeps the hand-to-pen offset; letting go leaves it floating there (<see cref="PenHold"/>).</item>
    /// <item>While held: the trigger counts as pressed (unless the press began on a menu or panel), and in select mode
    /// the thumbstick up/down changes the selection radius.</item>
    /// <item>The mode comes from the main menu (<see cref="PenModeSelection"/>).</item>
    /// </list>
    /// <see cref="SpatialPenSync"/> hands it the pen of the local role with <see cref="Bind"/>.
    /// Tools read <see cref="IsTriggerHeld"/>, <see cref="TipPosition"/>, <see cref="Radius"/> and <see cref="Mode"/>,
    /// or listen to <see cref="TriggerPressed"/> / <see cref="TriggerReleased"/>; they act only while
    /// <see cref="Mode"/> matches the mode their part needs.
    /// </summary>
    public sealed class SpatialPenController : MonoBehaviour
    {
        // After Basis has posed the local player for rendering (BasisLockToBone and the menu use 99).
        const int RenderPriority = 100;

        const float TriggerPressThreshold = 0.6f;
        const float TriggerReleaseThreshold = 0.4f;

        [Tooltip("How close the hand must be to the pen (to its axis, from butt to tip) to grab it.")]
        [SerializeField, Min(0.01f)] float _grabReach = 0.15f;

        [Tooltip("Seconds of a full thumbstick push to go from the smallest to the largest selection radius.")]
        [SerializeField, Min(0.1f)] float _radiusFullRangeSeconds = 1.5f;

        readonly PenHold _hold = new();
        SpatialPenView _view;
        float _radius = PenGeometry.DefaultSelectRadius;
        bool _triggerDown;
        bool _triggerIgnoredUntilRelease;
        bool _registered;

        /// <summary>The pen this device drives, or null while the local player has no role.</summary>
        public SpatialPenView View => _view;

        public PenMode Mode => PenModeSelection.Current;
        public bool IsHeld => _hold.IsHeld;

        /// <summary>The pen is held and its trigger is down. This is the pen's "press" for every tool.</summary>
        public bool IsTriggerHeld => _hold.IsHeld && _triggerDown;

        public float Radius => _radius;

        /// <summary>World position of the tip, the centre of the selection sphere.</summary>
        public Vector3 TipPosition => _view != null ? _view.TipPosition : Vector3.zero;

        public event Action TriggerPressed;
        public event Action TriggerReleased;

        public PenState CurrentState
        {
            get
            {
                Pose pose = _view != null ? _view.WorldPose : Pose.identity;
                return new PenState
                {
                    Mode = Mode,
                    Radius = _radius,
                    IsHeld = IsHeld,
                    IsTriggerHeld = IsTriggerHeld,
                    Position = pose.position,
                    Rotation = pose.rotation,
                };
            }
        }

        /// <summary>Makes <paramref name="view"/> the local pen (null when the local player has no role).</summary>
        public void Bind(SpatialPenView view)
        {
            if (_view == view)
            {
                return;
            }
            _hold.Reset();
            SetTriggerDown(false);
            _view = view;
            ShowState();
        }

        void OnEnable()
        {
            PenModeSelection.Changed += HandleModeChanged;
            if (!_registered)
            {
                BasisLocalPlayer.AfterSimulateOnRender.AddAction(RenderPriority, FollowHandForRender);
                _registered = true;
            }
        }

        void OnDisable()
        {
            PenModeSelection.Changed -= HandleModeChanged;
            if (_registered)
            {
                BasisLocalPlayer.AfterSimulateOnRender.RemoveAction(RenderPriority, FollowHandForRender);
                _registered = false;
            }
            _hold.Reset();
            SetTriggerDown(false);
        }

        void Update()
        {
            if (_view == null)
            {
                return;
            }
            PenInputFrame input = PenInputReader.Read();

            Pose pen = _view.WorldPose;
            bool inReach = input.IgnoresReach || IsWithinReach(input.Hand);
            _hold.Update(input.Grip, input.HandTracked, input.Hand, pen, inReach);
            if (_hold.IsHeld)
            {
                _view.SetPose(_hold.Follow(input.Hand));
            }

            UpdateTrigger(input);

            if (_hold.IsHeld && Mode == PenMode.Select)
            {
                _radius = PenGeometry.AdjustRadius(_radius, input.StickY, Time.deltaTime, _radiusFullRangeSeconds);
            }
            ShowState();
        }

        bool IsWithinReach(Pose hand)
        {
            Vector3 local = _view.transform.InverseTransformPoint(hand.position);
            return PenGeometry.DistanceToAxis(local, _view.Length) <= _grabReach;
        }

        /// <summary>
        /// The trigger is read with hysteresis so a half-pressed trigger does not flicker. A press that begins while
        /// the laser is on a menu or panel belongs to that UI and is ignored until released.
        /// </summary>
        void UpdateTrigger(PenInputFrame input)
        {
            if (!_hold.IsHeld)
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

        /// <summary>Re-reads the hand right before rendering so the held pen does not trail the hand by a frame.</summary>
        void FollowHandForRender()
        {
            if (_view != null && _hold.IsHeld && PenInputReader.TryGetHandPose(out Pose hand))
            {
                _view.SetPose(_hold.Follow(hand));
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
                _view.ShowState(Mode, _radius, IsHeld, IsTriggerHeld);
            }
        }
    }
}
