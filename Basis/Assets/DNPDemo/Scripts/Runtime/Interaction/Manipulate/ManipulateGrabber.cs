using System;
using DNPDemo.Pen;

namespace DNPDemo.Interaction.Manipulate
{
    /// <summary>
    /// Grabs a manipulated object with the local pen and drags it within its constraint.
    /// <list type="bullet">
    /// <item>A press of the trigger in the listened mode grabs the object when the tip sphere
    /// (<see cref="PenGeometry.GrabRadius"/>) touches the grab zone. A press elsewhere does nothing until it is released.</item>
    /// <item>While grabbed, every <see cref="Tick"/> moves <see cref="Value"/> with the tip (<see cref="ManipulateDrag"/>).</item>
    /// <item>Releasing the trigger raises <see cref="Released"/>. If the pen's mode changed during the grab (the menu ends
    /// the press), the release is not meant as a placement and is reported as not to be judged.</item>
    /// </list>
    /// It only computes the value; showing the object, judging and sending are up to the owner.
    /// Shared by <see cref="ManipulateTool"/> and the pen test scene, which runs it without a session.
    /// </summary>
    public sealed class ManipulateGrabber
    {
        readonly SpatialPenController _pen;

        PenMode _mode;
        bool _listening;
        ManipulateConstraint _constraint;
        IDetectionVolume _grabZone;
        ManipulateDrag _drag;

        public ManipulateGrabber(SpatialPenController pen)
        {
            _pen = pen ?? throw new ArgumentNullException(nameof(pen));
        }

        /// <summary>The object was grabbed.</summary>
        public event Action Grabbed;

        /// <summary>
        /// The object was let go at the value given. The flag is true for a release the actor meant (judge it), false when
        /// the grab ended because the pen's mode changed.
        /// </summary>
        public event Action<ManipulateValue, bool> Released;

        /// <summary>The current value: where the drag has taken the object, or the value it was given.</summary>
        public ManipulateValue Value { get; private set; }

        public bool IsGrabbing => _drag != null;

        /// <summary>The pen is held in the listened mode, not grabbing, and its tip touches the grab zone.</summary>
        public bool IsHovering { get; private set; }

        /// <summary>
        /// Starts taking presses made in <paramref name="mode"/> on <paramref name="grabZone"/>, with the object at
        /// <paramref name="value"/>. A press already running is not picked up halfway.
        /// </summary>
        public void Listen(PenMode mode, ManipulateConstraint constraint, IDetectionVolume grabZone, ManipulateValue value)
        {
            Stop();
            _mode = mode;
            _constraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
            _grabZone = grabZone;
            Value = constraint.Clamp(value);
            _listening = true;
            _pen.TriggerPressed += HandlePressed;
            _pen.TriggerReleased += HandleReleased;
        }

        /// <summary>Stops taking presses. A grab in progress ends where it is, without <see cref="Released"/>.</summary>
        public void Stop()
        {
            if (_listening)
            {
                _pen.TriggerPressed -= HandlePressed;
                _pen.TriggerReleased -= HandleReleased;
                _listening = false;
            }
            _drag = null;
            IsHovering = false;
        }

        /// <summary>Replaces the value (after a reset, for example). Ignored while grabbing.</summary>
        public void SetValue(ManipulateValue value)
        {
            if (_drag == null && _constraint != null)
            {
                Value = _constraint.Clamp(value);
            }
        }

        /// <summary>Drags the object or updates <see cref="IsHovering"/>. Call once per frame, after the pen has moved.</summary>
        public void Tick()
        {
            if (!_listening)
            {
                IsHovering = false;
                return;
            }
            if (_drag != null)
            {
                IsHovering = false;
                Value = _drag.Update(_pen.TipPosition);
                return;
            }
            IsHovering = _pen.IsHeld && !_pen.IsTriggerHeld && _pen.Mode == _mode && Touches();
        }

        void HandlePressed()
        {
            if (_drag != null || _pen.Mode != _mode || !Touches())
            {
                return;
            }
            IsHovering = false;
            _drag = new ManipulateDrag(_constraint, Value, _pen.TipPosition);
            Grabbed?.Invoke();
        }

        void HandleReleased()
        {
            if (_drag == null)
            {
                return;
            }
            bool intended = _pen.Mode == _mode;
            if (intended)
            {
                Value = _drag.Update(_pen.TipPosition);
            }
            _drag = null;
            Released?.Invoke(Value, intended);
        }

        bool Touches() =>
            DetectionVolumes.IsAlive(_grabZone) && _grabZone.OverlapsSphere(_pen.TipPosition, PenGeometry.GrabRadius);
    }
}
