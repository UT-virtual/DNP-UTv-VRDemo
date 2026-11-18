using DNPDemo.Pen;
using UnityEngine;

namespace DNPDemo.Interaction.Voice
{
    /// <summary>
    /// Runs <see cref="VoicePart"/> on the actor's device.
    /// <list type="bullet">
    /// <item>While the pen in voice mode has its trigger held, the microphone is captured and the task HUD reads
    /// <see cref="ListeningStatus"/> (<see cref="VoiceCommandListener"/>).</item>
    /// <item>As soon as the sound adds up to <see cref="VoicePart.RequiredSpeechSeconds"/>, the HUD shows the part's
    /// <see cref="VoicePart.Transcript"/> as the recognition result, and releasing the trigger then completes the part.</item>
    /// <item>A release before that asks the actor to try again.</item>
    /// </list>
    /// Nothing is sent to the other devices besides the part's completion, which the runner sends; the pen's microphone
    /// indicator, synced with the pen, already shows the other player when the actor is speaking.
    /// </summary>
    public sealed class VoiceCommandTool : InteractionTool
    {
        public const string ListeningStatus = "音声入力中…";
        public const string RetryStatus = "聞き取れませんでした。トリガーを押しながら話してください";
        public const string ResultPrefix = "認識結果：";
        public const string ReleaseHint = "トリガーを離すと決定します";

        [SerializeField] SpatialPenController _pen;

        VoiceCommandListener _listener;
        InteractionSession _session;
        VoicePart _part;

        /// <summary>The status shown once a press is recognised: the result and how to confirm it.</summary>
        public static string FormatResult(string transcript) => $"{ResultPrefix}「{transcript}」\n{ReleaseHint}";

        void Awake()
        {
            if (_pen == null)
            {
                Debug.LogError("[DNPDemo] VoiceCommandTool needs a SpatialPenController reference.", this);
                return;
            }
            _listener = new VoiceCommandListener(_pen);
            _listener.Started += HandleStarted;
            _listener.Recognized += HandleRecognized;
            _listener.Finished += HandleFinished;
            _listener.Abandoned += HandleAbandoned;
        }

        void OnDisable() => StopInput();

        void OnDestroy()
        {
            if (_listener != null)
            {
                _listener.Started -= HandleStarted;
                _listener.Recognized -= HandleRecognized;
                _listener.Finished -= HandleFinished;
                _listener.Abandoned -= HandleAbandoned;
                _listener.Stop();
            }
        }

        void Update() => _listener?.Tick(Time.unscaledDeltaTime);

        public override bool CanRun(InteractionPart part) => part is VoicePart;

        public override void Begin(InteractionSession session)
        {
            StopInput();
            _session = session;
            _part = (VoicePart)session.Part;
            if (session.IsLocalActor && _listener != null && isActiveAndEnabled)
            {
                _listener.RequiredSpeechSeconds = _part.RequiredSpeechSeconds;
                _listener.Listen();
            }
        }

        public override void End() => StopInput();

        public override void Discard() => StopInput();

        void HandleStarted()
        {
            if (IsActive())
            {
                _session.SetStatus(ListeningStatus);
            }
        }

        void HandleRecognized(VoiceUtterance utterance)
        {
            if (!IsActive())
            {
                return;
            }
            Debug.Log($"[DNPDemo] 音声指示を認識しました。トリガーを離すと完了します。{utterance}", this);
            _session.SetStatus(FormatResult(_part.Transcript));
        }

        void HandleFinished(VoiceUtterance utterance)
        {
            if (!IsActive())
            {
                return;
            }
            if (utterance.Recognized)
            {
                _session.Complete();
                return;
            }
            Debug.Log($"[DNPDemo] 音声指示の発話が足りません（必要 {_part.RequiredSpeechSeconds:0.##} 秒）。{utterance}", this);
            _session.SetStatus(RetryStatus);
        }

        void HandleAbandoned()
        {
            if (IsActive())
            {
                _session.SetStatus(string.Empty);
            }
        }

        bool IsActive() => _session != null && _session.IsCurrent && _session.IsLocalActor && _part != null;

        void StopInput()
        {
            // Session first, so an abandoned press does not touch the status of a part that is ending; the runner clears it.
            _session = null;
            _part = null;
            _listener?.Stop();
        }
    }
}
