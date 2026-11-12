using System.Collections.Generic;
using DNPDemo.Presentation;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// The scene side of the scenario. <see cref="ScenarioData"/> is an asset and cannot reference scene objects,
    /// so cues address speakers and objects through the bindings declared on those objects
    /// (<see cref="ScenarioSpeakerBinding"/>, <see cref="ScenarioObjectBinding"/>). The stage collects them from its
    /// scene once, on Awake, inactive objects included; each binding lives in the prefab of the object it describes.
    /// <para>
    /// It also owns everything a cue can leave behind: <see cref="StopTransient"/> silences lines and effects when
    /// playback is cut, and <see cref="ResetToBaseline"/> returns every bound object to its state at scene load
    /// before the lasting effects of earlier steps are re-applied.
    /// </para>
    /// </summary>
    public sealed class ScenarioStage : MonoBehaviour
    {
        [Tooltip("Non-positional source for sound effects.")]
        [SerializeField] AudioSource _effects;

        [SerializeField] BannerView _banner;

        readonly Dictionary<GameObject, bool> _baselineActive = new();
        readonly HashSet<string> _reportedMissing = new();
        ScenarioBindingSet _bindings;

        void Awake()
        {
            EnsureBindings();
        }

        public void ShowLine(ScenarioSpeaker speaker, string text, AudioClip voice)
        {
            if (!TryGetSpeaker(speaker, out ScenarioSpeakerBinding binding))
            {
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
            if (TryGetSpeaker(speaker, out ScenarioSpeakerBinding binding) && binding.Subtitle != null)
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
            EnsureBindings();
            if (!_bindings.TryGetObject(key, out GameObject target) || target == null)
            {
                ReportMissing("object:" + key, $"キー「{key}」のオブジェクト（ScenarioObjectBinding）がシーンにありません。");
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
            EnsureBindings();
            foreach (ScenarioSpeakerBinding binding in _bindings.Speakers.Values)
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

        /// <summary>Returns every bound object to its state at scene load.</summary>
        public void ResetToBaseline()
        {
            EnsureBindings();
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

        /// <summary>Collects the bindings and records the baseline the first time anything needs them.</summary>
        void EnsureBindings()
        {
            if (_bindings != null)
            {
                return;
            }
            _bindings = ScenarioBindingSet.Collect(gameObject.scene);
            foreach (string problem in _bindings.Problems)
            {
                Debug.LogError("[DNPDemo] " + problem, this);
            }
            foreach (GameObject target in _bindings.Objects.Values)
            {
                if (target != null && !_baselineActive.ContainsKey(target))
                {
                    _baselineActive.Add(target, target.activeSelf);
                }
            }
        }

        bool TryGetSpeaker(ScenarioSpeaker speaker, out ScenarioSpeakerBinding binding)
        {
            EnsureBindings();
            if (_bindings.TryGetSpeaker(speaker, out binding) && binding != null)
            {
                return true;
            }
            ReportMissing("speaker:" + speaker, $"話者 {speaker} の割り当て（ScenarioSpeakerBinding）がシーンにありません。");
            return false;
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
