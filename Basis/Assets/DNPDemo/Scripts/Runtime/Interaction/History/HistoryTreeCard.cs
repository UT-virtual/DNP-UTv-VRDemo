using System;
using DNPDemo.Venus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// One version in the branch view of the history panel: its thumbnail and name on a button. The version the statue
    /// shows now is framed. <see cref="HistoryTreeView"/> clones these from a template.
    /// </summary>
    public sealed class HistoryTreeCard : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] RawImage _thumbnail;
        [SerializeField] TMP_Text _label;

        [Tooltip("Shown on the version the statue shows now.")]
        [SerializeField] GameObject _currentFrame;

        public VenusVersion Version { get; private set; }

        /// <summary>The card was pressed. Carries its version.</summary>
        public event Action<VenusVersion> Pressed;

        public RectTransform Rect => (RectTransform)transform;

        void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(Press);
            }
        }

        void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(Press);
            }
        }

        public void Show(VenusVersion version, bool current)
        {
            Version = version;
            if (_thumbnail != null)
            {
                Texture2D thumbnail = version != null ? version.Thumbnail : null;
                _thumbnail.texture = thumbnail;
                _thumbnail.enabled = thumbnail != null;
            }
            if (_label != null)
            {
                _label.text = version != null ? version.ToString() : string.Empty;
            }
            if (_currentFrame != null)
            {
                _currentFrame.SetActive(current);
            }
        }

        void Press()
        {
            if (Version != null)
            {
                Pressed?.Invoke(Version);
            }
        }
    }
}
