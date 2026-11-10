using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// Registers this GameObject with <see cref="ScenarioStage"/> under <see cref="Key"/>, so cues (which live in an asset
    /// and cannot reference scene objects) can address it. The stage collects bindings from its whole scene, inactive
    /// objects included, when it starts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScenarioObjectBinding : MonoBehaviour
    {
        [Tooltip("Name the scenario data uses for this object. Must be unique in the scene.")]
        [SerializeField] string _key;

        public string Key => _key;
    }
}
