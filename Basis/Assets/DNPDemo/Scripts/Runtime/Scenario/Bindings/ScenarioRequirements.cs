using System.Collections.Generic;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// The scene bindings a scenario relies on, gathered from its cues (<see cref="Cues.ScenarioCue.CollectRequirements"/>).
    /// Used to check that the scene provides every speaker and key the data refers to.
    /// </summary>
    public sealed class ScenarioRequirements
    {
        public HashSet<ScenarioSpeaker> Speakers { get; } = new();
        public HashSet<string> ObjectKeys { get; } = new();

        /// <summary>What the scene is missing for this scenario. Empty when everything is bound.</summary>
        public List<string> FindMissing(ScenarioBindingSet bindings)
        {
            var missing = new List<string>();
            foreach (ScenarioSpeaker speaker in Speakers)
            {
                if (!bindings.TryGetSpeaker(speaker, out _))
                {
                    missing.Add($"話者 {speaker} の割り当て（ScenarioSpeakerBinding）がシーンにありません。");
                }
            }
            foreach (string key in ObjectKeys)
            {
                if (!bindings.TryGetObject(key, out _))
                {
                    missing.Add($"キー「{key}」のオブジェクト（ScenarioObjectBinding）がシーンにありません。");
                }
            }
            return missing;
        }
    }
}
