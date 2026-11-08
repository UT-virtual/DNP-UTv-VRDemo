using TMPro;
using UnityEngine;

namespace DNPDemo.Presentation
{
    /// <summary>Large text in front of the view (for example "WORKSHOP COMPLETE") that fades in and out.</summary>
    public sealed class BannerView : MonoBehaviour
    {
        [SerializeField] CanvasGroup _group;
        [SerializeField] TMP_Text _text;

        float _targetAlpha;
        float _alphaPerSecond;

        void Awake()
        {
            SetAlpha(0f);
            _targetAlpha = 0f;
            enabled = false;
        }

        public void Show(string text, float fadeSeconds)
        {
            if (_text != null)
            {
                _text.text = text ?? string.Empty;
            }
            FadeTo(1f, fadeSeconds);
        }

        public void Hide(float fadeSeconds) => FadeTo(0f, fadeSeconds);

        void FadeTo(float alpha, float fadeSeconds)
        {
            _targetAlpha = alpha;
            if (fadeSeconds <= 0f)
            {
                SetAlpha(alpha);
                enabled = false;
                return;
            }
            _alphaPerSecond = 1f / fadeSeconds;
            enabled = true;
        }

        void Update()
        {
            if (_group == null)
            {
                enabled = false;
                return;
            }
            float alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, _alphaPerSecond * Time.deltaTime);
            SetAlpha(alpha);
            if (Mathf.Approximately(alpha, _targetAlpha))
            {
                enabled = false;
            }
        }

        void SetAlpha(float alpha)
        {
            if (_group != null)
            {
                _group.alpha = alpha;
            }
        }
    }
}
