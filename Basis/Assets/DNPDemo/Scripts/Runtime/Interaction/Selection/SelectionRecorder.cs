using System;
using System.Collections.Generic;
using DNPDemo.Pen;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>
    /// Records range selections from the local pen and shows them while they are drawn. One press of the pen's trigger
    /// is one selection: it starts on <see cref="SpatialPenController.TriggerPressed"/>, samples the tip every
    /// <see cref="Tick"/>, and on <see cref="SpatialPenController.TriggerReleased"/> raises <see cref="Finished"/> with
    /// the samples. What happens next (judging, keeping or clearing the display) is up to the owner.
    /// <para>
    /// Only presses made while the pen is in the listened mode count. If the mode changes during a press (the menu
    /// ends the press), the selection is dropped without <see cref="Finished"/>.
    /// </para>
    /// <para>
    /// Shared by <see cref="SelectionTool"/> and the pen test scene, which runs it without a session.
    /// </para>
    /// </summary>
    public sealed class SelectionRecorder
    {
        readonly SpatialPenController _pen;
        readonly SelectionVolumeView _view;
        readonly SelectionSampler _sampler;

        PenMode _mode;
        bool _listening;
        bool _recording;

        public SelectionRecorder(SpatialPenController pen, SelectionVolumeView view, SelectionSampler sampler)
        {
            _pen = pen ?? throw new ArgumentNullException(nameof(pen));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
        }

        /// <summary>
        /// A press ended in the listened mode. The list belongs to the recorder and is reused by the next press; copy or
        /// serialize it before returning.
        /// </summary>
        public event Action<IReadOnlyList<SelectionSample>> Finished;

        public bool IsRecording => _recording;

        /// <summary>
        /// Starts taking presses made in <paramref name="mode"/>. A press already running is not picked up halfway;
        /// the next one is.
        /// </summary>
        public void Listen(PenMode mode)
        {
            Stop();
            _mode = mode;
            _listening = true;
            _pen.TriggerPressed += HandlePressed;
            _pen.TriggerReleased += HandleReleased;
        }

        /// <summary>Stops taking presses. A selection being drawn is dropped and its display cleared; a finished one stays.</summary>
        public void Stop()
        {
            if (_listening)
            {
                _pen.TriggerPressed -= HandlePressed;
                _pen.TriggerReleased -= HandleReleased;
                _listening = false;
            }
            if (_recording)
            {
                Abandon();
            }
        }

        /// <summary>Samples the tip while a press is running. Call once per frame, after the pen has moved.</summary>
        public void Tick()
        {
            if (!_recording)
            {
                return;
            }
            if (_pen.Mode != _mode)
            {
                Abandon();
                return;
            }
            if (_sampler.Add(_pen.TipPosition, _pen.Radius))
            {
                _view.Show(_sampler.Samples);
            }
        }

        void HandlePressed()
        {
            if (_pen.Mode != _mode)
            {
                return;
            }
            _recording = true;
            _sampler.Clear();
            _sampler.Add(_pen.TipPosition, _pen.Radius);
            _view.Show(_sampler.Samples);
        }

        void HandleReleased()
        {
            if (!_recording)
            {
                return;
            }
            if (_pen.Mode != _mode)
            {
                Abandon();
                return;
            }
            _recording = false;
            if (_sampler.Add(_pen.TipPosition, _pen.Radius))
            {
                _view.Show(_sampler.Samples);
            }
            Finished?.Invoke(_sampler.Samples);
        }

        void Abandon()
        {
            _recording = false;
            _sampler.Clear();
            _view.Clear();
        }
    }
}
