using UnityEngine;

namespace DNPDemo.Audio
{
    /// <summary>
    /// Plays the sound effects of interaction feedback (<see cref="SeEvent"/>) on this device only, from a source of its
    /// own. It is separate from the scenario's effects source on purpose: that one is stopped whenever the step changes
    /// (<see cref="DNPDemo.Scenario.ScenarioStage.StopTransient"/>), which would cut the sound of the operation that completed the step.
    /// A missing catalog, source or clip is silent, because the clips are not in the repository.
    /// </summary>
    public sealed class SePlayer : MonoBehaviour
    {
        [SerializeField] SeCatalog _catalog;

        [Tooltip("Non-positional source the effects are played on.")]
        [SerializeField] AudioSource _source;

        public SeCatalog Catalog => _catalog;

        public void Play(SeEvent seEvent)
        {
            if (_catalog != null && _source != null && _catalog.TryGet(seEvent, out AudioClip clip, out float volume))
            {
                _source.PlayOneShot(clip, volume);
            }
        }
    }
}
