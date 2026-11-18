using DNPDemo.Pen;
using TMPro;
using UnityEngine;

namespace DNPDemo.Interaction.Voice.Testing
{
    /// <summary>
    /// Lets voice commands be tried in a test scene without a session, roles or scenario. While the pen in voice mode has
    /// its trigger held, the microphone is captured as in <see cref="VoiceCommandTool"/>; once the sound reaches
    /// <see cref="_requiredSpeechSeconds"/> the result is shown, and on release the outcome is logged. The optional text
    /// shows the live level against the threshold (<see cref="VoiceCommandSettings"/>) and the last result.
    /// Not for the demo scenes, where <see cref="VoiceCommandTool"/> runs the voice parts.
    /// </summary>
    public sealed class VoiceTestHarness : MonoBehaviour
    {
        [SerializeField] SpatialPenController _pen;

        [Tooltip("Optional text for the live level and the last result.")]
        [SerializeField] TMP_Text _statusText;

        [SerializeField, Min(0.1f)] float _requiredSpeechSeconds = VoicePart.DefaultRequiredSpeechSeconds;

        [SerializeField, TextArea(1, 3)] string _transcript = "鏡を握らせる";

        VoiceCommandListener _listener;
        string _lastResult = "トリガーを押しながら話してください";

        void Start()
        {
            if (_pen == null)
            {
                Debug.LogError("[DNPDemo] VoiceTestHarness: SpatialPenController を設定してください。", this);
                return;
            }
            _listener = new VoiceCommandListener(_pen) { RequiredSpeechSeconds = _requiredSpeechSeconds };
            _listener.Started += HandleStarted;
            _listener.Recognized += HandleRecognized;
            _listener.Finished += HandleFinished;
            _listener.Abandoned += HandleAbandoned;
            _listener.Listen();
        }

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

        void Update()
        {
            if (_listener == null)
            {
                return;
            }
            _listener.Tick(Time.unscaledDeltaTime);
            if (_statusText != null)
            {
                string level = _listener.IsCapturing ? Operator.MicrophoneThresholdControl.FormatLevelLine(_listener.Level) : "待機中";
                _statusText.text = $"{level}\n{_lastResult}";
            }
        }

        void HandleStarted() => _lastResult = VoiceCommandTool.ListeningStatus;

        void HandleRecognized(VoiceUtterance utterance) => _lastResult = VoiceCommandTool.FormatResult(_transcript);

        void HandleFinished(VoiceUtterance utterance)
        {
            _lastResult = utterance.Recognized ? $"{VoiceCommandTool.ResultPrefix}「{_transcript}」（完了）" : VoiceCommandTool.RetryStatus;
            Debug.Log($"[DNPDemo] 音声指示のテスト：{(utterance.Recognized ? "成功" : "失敗")}（必要 {_requiredSpeechSeconds:0.##} 秒）{utterance}", this);
        }

        void HandleAbandoned() => _lastResult = "中断しました";
    }
}
