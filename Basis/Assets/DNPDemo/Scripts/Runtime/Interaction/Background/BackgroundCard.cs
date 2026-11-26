using System;
using DNPDemo.Environment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Interaction.Background
{
    /// <summary>
    /// One background on the background panel: its panorama and name on a button. The background on show is framed, and
    /// the selected one is marked. <see cref="BackgroundPanel"/> clones these from a template.
    /// </summary>
    public sealed class BackgroundCard : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] RawImage _thumbnail;
        [SerializeField] TMP_Text _label;

        [Tooltip("Shown on the background on show.")]
        [SerializeField] GameObject _currentFrame;

        [Tooltip("Shown on the selected background.")]
        [SerializeField] GameObject _selectedMark;

        public EnvironmentPreset Preset { get; private set; }

        /// <summary>The card was pressed. Carries its background.</summary>
        public event Action<EnvironmentPreset> Pressed;

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

        public void Show(EnvironmentPreset preset, bool current, bool selected)
        {
            Preset = preset;
            if (_thumbnail != null)
            {
                Texture2D panorama = preset != null ? preset.Panorama : null;
                _thumbnail.texture = panorama;
                _thumbnail.enabled = panorama != null;
            }
            if (_label != null)
            {
                _label.text = preset != null ? preset.DisplayName : string.Empty;
            }
            if (_currentFrame != null)
            {
                _currentFrame.SetActive(current);
            }
            if (_selectedMark != null)
            {
                _selectedMark.SetActive(selected);
            }
        }

        void Press()
        {
            if (Preset != null)
            {
                Pressed?.Invoke(Preset);
            }
        }
    }
}
