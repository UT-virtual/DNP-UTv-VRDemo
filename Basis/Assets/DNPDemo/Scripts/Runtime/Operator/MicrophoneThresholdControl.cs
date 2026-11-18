using DNPDemo.Interaction.Voice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Operator
{
    /// <summary>
    /// Operator panel section for tuning the voice command threshold (<see cref="VoiceCommandSettings"/>) to the venue.
    /// While the panel is open the microphone is captured (still muted, nothing is sent) and a meter shows the live level
    /// against the threshold, so staff can set it between the room's noise and a speaking voice. The value is kept on
    /// this headset.
    /// <para>
    /// It also shows which microphone Basis records from and switches to the next one (<see cref="MicrophoneSelection"/>),
    /// since a PC may start on a virtual device that stays silent.
    /// </para>
    /// </summary>
    public sealed class MicrophoneThresholdControl : MonoBehaviour
    {
        const float MeterMinDecibels = -80f;
        const float MeterMaxDecibels = 0f;

        [SerializeField] TMP_Text _deviceText;
        [SerializeField] Button _switchDeviceButton;

        [SerializeField] TMP_Text _levelText;

        [Tooltip("Fill of the meter. Its right anchor follows the level.")]
        [SerializeField] RectTransform _meterFill;

        [Tooltip("Thin marker on the meter. Its horizontal anchors follow the threshold.")]
        [SerializeField] RectTransform _thresholdMarker;

        [SerializeField] Button _lowerButton;
        [SerializeField] Button _raiseButton;
        [SerializeField] Button _resetButton;

        [SerializeField, Min(0.5f)] float _stepDecibels = 2f;

        [SerializeField] Color _belowThresholdColor = new(0.55f, 0.6f, 0.7f);
        [SerializeField] Color _aboveThresholdColor = new(0.35f, 0.9f, 0.45f);

        readonly MicrophoneCapture _capture = new();
        Graphic _meterFillGraphic;

        void Awake()
        {
            if (_meterFill != null)
            {
                _meterFillGraphic = _meterFill.GetComponent<Graphic>();
            }
            if (_switchDeviceButton != null)
            {
                _switchDeviceButton.onClick.AddListener(MicrophoneSelection.SelectNext);
            }
            if (_lowerButton != null)
            {
                _lowerButton.onClick.AddListener(() => VoiceCommandSettings.ThresholdDecibels -= _stepDecibels);
            }
            if (_raiseButton != null)
            {
                _raiseButton.onClick.AddListener(() => VoiceCommandSettings.ThresholdDecibels += _stepDecibels);
            }
            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(VoiceCommandSettings.ResetThreshold);
            }
        }

        void OnEnable()
        {
            _capture.Begin();
            VoiceCommandSettings.Changed += ShowThreshold;
            MicrophoneSelection.Changed += ShowDevice;
            ShowThreshold();
            ShowDevice();
        }

        void OnDisable()
        {
            VoiceCommandSettings.Changed -= ShowThreshold;
            MicrophoneSelection.Changed -= ShowDevice;
            _capture.End();
        }

        void Update()
        {
            float level = _capture.Level;
            float levelDecibels = AudioLevel.ToDecibels(level);
            bool above = level > VoiceCommandSettings.ThresholdLevel;
            if (_meterFill != null)
            {
                _meterFill.anchorMax = new Vector2(MeterPosition(levelDecibels), _meterFill.anchorMax.y);
            }
            if (_meterFillGraphic != null)
            {
                _meterFillGraphic.color = above ? _aboveThresholdColor : _belowThresholdColor;
            }
            if (_levelText != null)
            {
                _levelText.text = FormatLevelLine(level);
            }
        }

        void ShowThreshold()
        {
            float threshold = VoiceCommandSettings.ThresholdDecibels;
            if (_thresholdMarker != null)
            {
                float x = MeterPosition(threshold);
                _thresholdMarker.anchorMin = new Vector2(x, _thresholdMarker.anchorMin.y);
                _thresholdMarker.anchorMax = new Vector2(x, _thresholdMarker.anchorMax.y);
            }
            if (_lowerButton != null)
            {
                _lowerButton.interactable = threshold > VoiceCommandSettings.MinThresholdDecibels;
            }
            if (_raiseButton != null)
            {
                _raiseButton.interactable = threshold < VoiceCommandSettings.MaxThresholdDecibels;
            }
        }

        void ShowDevice()
        {
            if (_deviceText != null)
            {
                _deviceText.text = FormatDeviceLine(MicrophoneSelection.Current);
            }
            if (_switchDeviceButton != null)
            {
                _switchDeviceButton.interactable = MicrophoneSelection.Devices.Length > 1;
            }
        }

        /// <summary>The device line shown on the panel, for example "マイク：マイク (USB Headset)".</summary>
        public static string FormatDeviceLine(string device) =>
            $"マイク：{(string.IsNullOrEmpty(device) ? "なし" : device)}";

        /// <summary>The level line shown on the panel, for example "マイク音量 -32 dB　しきい値 -40 dB".</summary>
        public static string FormatLevelLine(float level)
        {
            float decibels = AudioLevel.ToDecibels(level);
            string levelLabel = decibels <= AudioLevel.SilenceDecibels ? "無音" : $"{decibels:0} dB";
            return $"マイク音量 {levelLabel}　しきい値 {VoiceCommandSettings.ThresholdDecibels:0} dB";
        }

        static float MeterPosition(float decibels) => Mathf.InverseLerp(MeterMinDecibels, MeterMaxDecibels, decibels);
    }
}
