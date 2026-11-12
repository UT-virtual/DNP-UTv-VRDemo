using TMPro;
using UnityEngine;

namespace DNPDemo.Presentation
{
    /// <summary>A subtitle box: an optional speaker label above the line. Hidden through the canvas group so the layout is kept.</summary>
    public sealed class SubtitleView : MonoBehaviour
    {
        [SerializeField] CanvasGroup _group;
        [SerializeField] TMP_Text _speakerText;
        [SerializeField] TMP_Text _bodyText;

        void Awake()
        {
            Hide();
        }

        public void Show(string speaker, string body)
        {
            if (_speakerText != null)
            {
                bool hasSpeaker = !string.IsNullOrEmpty(speaker);
                _speakerText.text = hasSpeaker ? speaker : string.Empty;
                _speakerText.gameObject.SetActive(hasSpeaker);
            }
            if (_bodyText != null)
            {
                _bodyText.text = body ?? string.Empty;
            }
            if (_group != null)
            {
                _group.alpha = 1f;
            }
        }

        public void Hide()
        {
            if (_group != null)
            {
                _group.alpha = 0f;
            }
        }
    }
}
