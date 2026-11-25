using System;
using DNPDemo.Pen;

namespace DNPDemo.Interaction.Voice
{
    /// <summary>
    /// Turns presses of the pen's trigger in voice mode into utterances, shared by <see cref="VoiceCommandTool"/> and
    /// the test harness.
    /// <list type="bullet">
    /// <item>A press captures the microphone (<see cref="MicrophoneCapture"/>) and raises <see cref="Started"/>.</item>
    /// <item>Each <see cref="Tick"/> adds the frame to a <see cref="VoiceActivityMeter"/> against the threshold of
    /// <see cref="VoiceCommandSettings"/>. Once the sound adds up to <see cref="RequiredSpeechSeconds"/>, the press is
    /// recognised: <see cref="Recognized"/> is raised and the microphone is released, while the press goes on.</item>
    /// <item>The release raises <see cref="Finished"/> with what the press captured, recognised or not. If the pen's
    /// mode changes during a press (switching modes ends the press), the press ends with <see cref="Abandoned"/> instead.</item>
    /// </list>
    /// The owner must call <see cref="Stop"/> when it is disabled, so the microphone hold is always released.
    /// </summary>
    public sealed class VoiceCommandListener
    {
        readonly SpatialPenController _pen;
        readonly MicrophoneCapture _capture = new();
        readonly VoiceActivityMeter _meter = new();
        bool _listening;
        bool _pressed;
        bool _recognized;

        public VoiceCommandListener(SpatialPenController pen)
        {
            _pen = pen ?? throw new ArgumentNullException(nameof(pen));
        }

        /// <summary>A press began and the microphone is being captured.</summary>
        public event Action Started;

        /// <summary>The press has captured enough sound. Raised once per press, while the trigger is still held.</summary>
        public event Action<VoiceUtterance> Recognized;

        /// <summary>The trigger was released in voice mode. The utterance's measurements are in the argument.</summary>
        public event Action<VoiceUtterance> Finished;

        /// <summary>The press ended without a judgement (the pen's mode changed, or the listener stopped).</summary>
        public event Action Abandoned;

        /// <summary>Seconds of sound a press needs to be recognised.</summary>
        public float RequiredSpeechSeconds { get; set; } = VoicePart.DefaultRequiredSpeechSeconds;

        /// <summary>True while the trigger is held in a press this listener follows.</summary>
        public bool IsPressed => _pressed;

        /// <summary>True while the microphone is captured: during a press, until it is recognised.</summary>
        public bool IsCapturing => _capture.IsCapturing;

        /// <summary>The microphone level while capturing, otherwise 0.</summary>
        public float Level => _capture.Level;

        /// <summary>Starts taking presses of the trigger.</summary>
        public void Listen()
        {
            if (_listening)
            {
                return;
            }
            _listening = true;
            _pen.TriggerPressed += HandlePressed;
            _pen.TriggerReleased += HandleReleased;
        }

        /// <summary>Stops taking presses. A press in progress is abandoned and the microphone released.</summary>
        public void Stop()
        {
            if (_listening)
            {
                _listening = false;
                _pen.TriggerPressed -= HandlePressed;
                _pen.TriggerReleased -= HandleReleased;
            }
            Abandon();
        }

        /// <summary>Call once per frame.</summary>
        public void Tick(float deltaTime)
        {
            if (!_pressed)
            {
                return;
            }
            if (_pen.Mode != PenMode.Voice)
            {
                Abandon();
                return;
            }
            if (_recognized)
            {
                return;
            }
            _meter.Feed(_capture.Level, VoiceCommandSettings.ThresholdLevel, deltaTime);
            if (_meter.VoicedSeconds >= RequiredSpeechSeconds)
            {
                _recognized = true;
                _capture.End();
                Recognized?.Invoke(CurrentUtterance());
            }
        }

        void HandlePressed()
        {
            if (_pen.Mode != PenMode.Voice || _pressed)
            {
                return;
            }
            _pressed = true;
            _recognized = false;
            _meter.Reset();
            _capture.Begin();
            Started?.Invoke();
        }

        void HandleReleased()
        {
            if (!_pressed)
            {
                return;
            }
            if (_pen.Mode != PenMode.Voice)
            {
                Abandon();
                return;
            }
            EndPress();
            Finished?.Invoke(CurrentUtterance());
        }

        void Abandon()
        {
            if (!_pressed)
            {
                return;
            }
            EndPress();
            Abandoned?.Invoke();
        }

        void EndPress()
        {
            _pressed = false;
            _capture.End();
        }

        VoiceUtterance CurrentUtterance() => new(_meter.VoicedSeconds, _meter.PeakLevel, _recognized);
    }

    /// <summary>What one press of the trigger captured.</summary>
    public readonly struct VoiceUtterance
    {
        public VoiceUtterance(float voicedSeconds, float peakLevel, bool recognized)
        {
            VoicedSeconds = voicedSeconds;
            PeakLevel = peakLevel;
            Recognized = recognized;
        }

        /// <summary>Seconds the level stayed above the threshold.</summary>
        public float VoicedSeconds { get; }

        /// <summary>Highest level during the press (linear RMS).</summary>
        public float PeakLevel { get; }

        /// <summary>True when the sound reached the required time during the press.</summary>
        public bool Recognized { get; }

        public override string ToString() =>
            $"有音 {VoicedSeconds:0.00} 秒、最大 {AudioLevel.ToDecibels(PeakLevel):0} dB（しきい値 {VoiceCommandSettings.ThresholdDecibels:0} dB）";
    }
}
