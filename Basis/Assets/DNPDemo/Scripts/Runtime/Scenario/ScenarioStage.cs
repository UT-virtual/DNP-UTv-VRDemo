using System;
using System.Collections.Generic;
using DNPDemo.Presentation;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// The scene side of the scenario. <see cref="ScenarioData"/> is an asset and cannot reference scene objects,
    /// so cues address speakers and objects through the bindings registered here.
    /// <para>
    /// It also owns everything a cue can leave behind: <see cref="StopTransient"/> silences lines and effects when
    /// playback is cut, and <see cref="ResetToBaseline"/> returns every persistent target to its state at scene load
    /// before the lasting effects of earlier steps are re-applied.
    /// </para>
    /// </summary>
    public sealed class ScenarioStage : MonoBehaviour
    {
        [Serializable]
        public sealed class SpeakerBinding
        {
            public ScenarioSpeaker Speaker;
            public AudioSource Voice;
            public SubtitleView Subtitle;
        }

        [Serializable]
        public sealed class ObjectBinding
        {
            public string Key;
            public GameObject Target;
        }

        [SerializeField] List<SpeakerBinding> _speakers = new();
        [SerializeField] List<ObjectBinding> _objects = new();

        [Tooltip("Non-positional source for sound effects.")]
        [SerializeField] AudioSource _effects;

        [SerializeField] BannerView _banner;

        readonly Dictionary<GameObject, bool> _baselineActive = new();
        readonly HashSet<string> _reportedMissing = new();
        bool _baselineCaptured;

        void Awake()
        {
            CaptureBaseline();
        }

        public void ShowLine(ScenarioSpeaker speaker, string text, AudioClip voice)
        {
            SpeakerBinding binding = FindSpeaker(speaker);
            if (binding == null)
            {
                ReportMissing("speaker:" + speaker, $"話者 {speaker} の割り当てがありません。");
                return;
            }
            if (binding.Subtitle != null)
            {
                binding.Subtitle.Show(speaker.ToDisplayName(), text);
            }
            if (voice != null && binding.Voice != null)
            {
                binding.Voice.Stop();
                binding.Voice.clip = voice;
                binding.Voice.Play();
            }
        }

        public void EndLine(ScenarioSpeaker speaker)
        {
            SpeakerBinding binding = FindSpeaker(speaker);
            if (binding != null && binding.Subtitle != null)
            {
                binding.Subtitle.Hide();
            }
        }

        public void PlayEffect(AudioClip clip, float volume)
        {
            if (clip != null && _effects != null)
            {
                _effects.PlayOneShot(clip, volume);
            }
        }

        public void SetObjectActive(string key, bool active)
        {
            GameObject target = FindObject(key);
            if (target == null)
            {
                ReportMissing("object:" + key, $"キー「{key}」のオブジェクトが登録されていません。");
                return;
            }
            target.SetActive(active);
        }

        public void SetBanner(bool visible, string text, float fadeSeconds)
        {
            if (_banner == null)
            {
                ReportMissing("banner", "バナー表示が割り当てられていません。");
                return;
            }
            if (visible)
            {
                _banner.Show(text, fadeSeconds);
            }
            else
            {
                _banner.Hide(fadeSeconds);
            }
        }

        /// <summary>Stops voices and effects and hides subtitles. Called when playback of a step is cut.</summary>
        public void StopTransient()
        {
            foreach (SpeakerBinding binding in _speakers)
            {
                if (binding == null) continue;
                if (binding.Voice != null) binding.Voice.Stop();
                if (binding.Subtitle != null) binding.Subtitle.Hide();
            }
            if (_effects != null)
            {
                _effects.Stop();
            }
        }

        /// <summary>Returns every persistent target to its state at scene load.</summary>
        public void ResetToBaseline()
        {
            CaptureBaseline();
            foreach (KeyValuePair<GameObject, bool> pair in _baselineActive)
            {
                if (pair.Key != null)
                {
                    pair.Key.SetActive(pair.Value);
                }
            }
            if (_banner != null)
            {
                _banner.Hide(0f);
            }
        }

        void CaptureBaseline()
        {
            if (_baselineCaptured)
            {
                return;
            }
            _baselineCaptured = true;
            foreach (ObjectBinding binding in _objects)
            {
                if (binding != null && binding.Target != null && !_baselineActive.ContainsKey(binding.Target))
                {
                    _baselineActive.Add(binding.Target, binding.Target.activeSelf);
                }
            }
        }

        SpeakerBinding FindSpeaker(ScenarioSpeaker speaker)
        {
            foreach (SpeakerBinding binding in _speakers)
            {
                if (binding != null && binding.Speaker == speaker)
                {
                    return binding;
                }
            }
            return null;
        }

        GameObject FindObject(string key)
        {
            foreach (ObjectBinding binding in _objects)
            {
                if (binding != null && binding.Key == key)
                {
                    return binding.Target;
                }
            }
            return null;
        }

        void ReportMissing(string id, string message)
        {
            if (_reportedMissing.Add(id))
            {
                Debug.LogWarning("[DNPDemo] " + message, this);
            }
        }
    }
}
