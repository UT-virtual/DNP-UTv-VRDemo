using System;
using DNPDemo.Pen;

namespace DNPDemo.Interaction.Stroke
{
    /// <summary>
    /// Draws lines with the local pen into a <see cref="StrokeSet"/> and shows them, and runs the eraser at the butt.
    /// <list type="bullet">
    /// <item>One press of the trigger is one line: it starts on <see cref="SpatialPenController.TriggerPressed"/>, grows
    /// every <see cref="Tick"/> once the tip has moved <see cref="StrokeSet.MinPointSpacing"/>, and ends on
    /// <see cref="SpatialPenController.TriggerReleased"/> with <see cref="Finished"/>. If the pen's mode changes during a
    /// press (the menu ends the press), the line is dropped with <see cref="Abandoned"/>.</item>
    /// <item>While the pen is held in the listened mode and the trigger is up, every finished line the eraser sphere
    /// (<see cref="PenGeometry.EraserRadius"/>) touches is removed with <see cref="Erased"/>. The eraser is off while
    /// drawing so the butt cannot wipe a line by accident.</item>
    /// </list>
    /// What happens to a finished line (judging, sending, fading) is up to the owner.
    /// Shared by <see cref="StrokeTool"/> and the pen test scene, which runs it without a session.
    /// </summary>
    public sealed class StrokeRecorder
    {
        // A sweep through a tangle of lines removes a few per frame rather than all at once.
        const int MaxErasePerTick = 4;

        readonly SpatialPenController _pen;
        readonly StrokeView _view;
        readonly StrokeSet _set;

        PenMode _mode;
        bool _listening;
        StrokeLine _current;

        public StrokeRecorder(SpatialPenController pen, StrokeView view, StrokeSet set)
        {
            _pen = pen ?? throw new ArgumentNullException(nameof(pen));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        /// <summary>A line was begun. It already holds its first point.</summary>
        public event Action<StrokeLine> Started;

        /// <summary>A line was finished by releasing the trigger. It stays in the set.</summary>
        public event Action<StrokeLine> Finished;

        /// <summary>A line being drawn was dropped (the mode changed, or listening stopped). It is no longer in the set.</summary>
        public event Action<StrokeLine> Abandoned;

        /// <summary>The eraser removed a finished line. It is no longer in the set.</summary>
        public event Action<StrokeLine> Erased;

        /// <summary>The line being drawn, or null.</summary>
        public StrokeLine Current => _current;

        /// <summary>
        /// Starts taking presses made in <paramref name="mode"/>. A press already running is not picked up halfway; the
        /// next one is.
        /// </summary>
        public void Listen(PenMode mode)
        {
            Stop();
            _mode = mode;
            _listening = true;
            _pen.TriggerPressed += HandlePressed;
            _pen.TriggerReleased += HandleReleased;
        }

        /// <summary>Stops taking presses and erasing. A line being drawn is dropped; finished lines stay.</summary>
        public void Stop()
        {
            if (_listening)
            {
                _pen.TriggerPressed -= HandlePressed;
                _pen.TriggerReleased -= HandleReleased;
                _listening = false;
            }
            if (_current != null)
            {
                Abandon();
            }
        }

        /// <summary>Grows the line being drawn, or erases. Call once per frame, after the pen has moved.</summary>
        public void Tick()
        {
            if (!_listening)
            {
                return;
            }
            if (_current != null)
            {
                if (_pen.Mode != _mode)
                {
                    Abandon();
                    return;
                }
                if (_set.Extend(_current, _pen.TipPosition))
                {
                    _view.Show(_current);
                }
                return;
            }
            if (_pen.IsHeld && !_pen.IsTriggerHeld && _pen.Mode == _mode)
            {
                Erase();
            }
        }

        void HandlePressed()
        {
            if (_pen.Mode != _mode || _current != null)
            {
                return;
            }
            _current = _set.Begin(_pen.TipPosition);
            if (_current == null)
            {
                UnityEngine.Debug.LogWarning("[DNPDemo] 線の数か点の数が上限に達したため、これ以上描けません。消しゴムで消してください。");
                return;
            }
            _view.Show(_current);
            Started?.Invoke(_current);
        }

        void HandleReleased()
        {
            if (_current == null)
            {
                return;
            }
            if (_pen.Mode != _mode)
            {
                Abandon();
                return;
            }
            StrokeLine line = _current;
            _current = null;
            if (_set.Extend(line, _pen.TipPosition))
            {
                _view.Show(line);
            }
            _set.Finish(line);
            Finished?.Invoke(line);
        }

        void Erase()
        {
            float reach = PenGeometry.EraserRadius + _view.Width * 0.5f;
            for (int i = 0; i < MaxErasePerTick; i++)
            {
                if (!_set.TryFindTouching(_pen.EraserPosition, reach, out StrokeLine line))
                {
                    return;
                }
                _set.Remove(line.Id);
                _view.Hide(line.Id);
                Erased?.Invoke(line);
            }
        }

        void Abandon()
        {
            StrokeLine line = _current;
            _current = null;
            _set.Remove(line.Id);
            _view.Hide(line.Id);
            Abandoned?.Invoke(line);
        }
    }
}
