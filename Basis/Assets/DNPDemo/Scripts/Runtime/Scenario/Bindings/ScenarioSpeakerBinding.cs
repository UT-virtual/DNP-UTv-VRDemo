using DNPDemo.Presentation;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// Declares where a speaker's lines are voiced and subtitled. Put it on the speaker's object (a scholar, the narrator);
    /// <see cref="ScenarioStage"/> collects every binding in its scene when it starts, so adding or moving a speaker
    /// only touches the speaker's own prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScenarioSpeakerBinding : MonoBehaviour
    {
        [SerializeField] ScenarioSpeaker _speaker;

        [Tooltip("Source the voice clips play from. Positional for scholars, 2D for the narrator.")]
        [SerializeField] AudioSource _voice;

        [SerializeField] SubtitleView _subtitle;

        public ScenarioSpeaker Speaker => _speaker;
        public AudioSource Voice => _voice;
        public SubtitleView Subtitle => _subtitle;
    }
}
