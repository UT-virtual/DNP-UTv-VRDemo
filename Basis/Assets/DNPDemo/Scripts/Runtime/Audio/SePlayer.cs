using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Audio
{
    /// <summary>
    /// Plays the sound effects of interaction feedback (<see cref="SeEvent"/>) on this device only, from a source of its
    /// own. It is separate from the scenario's effects source on purpose: that one is stopped whenever the step changes
    /// (<see cref="DNPDemo.Scenario.ScenarioStage.StopTransient"/>), which would cut the sound of the operation that completed the step.
    /// While a voice line plays on this device (<see cref="ScenarioStage.IsLinePlaying"/>) a sound is held and played when
    /// the line ends (<see cref="SeHold"/>: the latest one only, dropped after <see cref="SeHold.MaxWaitSeconds"/>), and
    /// what is already playing is lowered for as long as the line lasts (<see cref="SeDucking"/>).
    /// A missing catalog, source or clip is silent, because the clips are not in the repository; without a stage nothing
    /// is held.
    /// </summary>
    public sealed class SePlayer : MonoBehaviour
    {
        /// <summary>
        /// Volume of the source a new player gets. The clips are normalized on import, so at full volume the effects
        /// drown the voice lines; the catalog's per-event volume scales on top of this.
        /// </summary>
        public const float DefaultSourceVolume = 0.3f;

        [SerializeField] SeCatalog _catalog;

        [Tooltip("Non-positional source the effects are played on.")]
        [SerializeField] AudioSource _source;

        [Tooltip("Whose voice lines the effects wait for. Without it the effects never wait.")]
        [SerializeField] ScenarioStage _stage;

        readonly SeHold _hold = new();
        readonly SeDucking _ducking = new();

        // The source's volume as set in the inspector; the ducking gain multiplies it every frame.
        float _sourceVolume = DefaultSourceVolume;

        public SeCatalog Catalog => _catalog;

        /// <summary>Editor only: a player added by hand takes the source next to it at the default volume.</summary>
        void Reset()
        {
            if (TryGetComponent(out _source))
            {
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
                _source.volume = DefaultSourceVolume;
            }
        }

        void Awake()
        {
            if (_source != null)
            {
                _sourceVolume = _source.volume;
            }
        }

        void OnDisable()
        {
            _hold.Clear();
            _ducking.Reset();
            if (_source != null)
            {
                _source.volume = _sourceVolume;
            }
        }

        void Update()
        {
            bool linePlaying = IsLinePlaying();
            if (_source != null)
            {
                _source.volume = _sourceVolume * _ducking.Step(linePlaying, Time.unscaledDeltaTime);
            }
            if (_hold.IsHolding && _hold.TryRelease(Time.unscaledTime, linePlaying, out SeEvent held))
            {
                PlayNow(held);
            }
        }

        public void Play(SeEvent seEvent)
        {
            if (_hold.Request(seEvent, Time.unscaledTime, IsLinePlaying()))
            {
                PlayNow(seEvent);
            }
        }

        bool IsLinePlaying() => _stage != null && _stage.IsLinePlaying;

        void PlayNow(SeEvent seEvent)
        {
            if (_catalog != null && _source != null && _catalog.TryGet(seEvent, out AudioClip clip, out float volume))
            {
                _source.PlayOneShot(clip, volume);
            }
        }
    }
}
