using DNPDemo.Scenario;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>Gives objects a scenario key from editor code (builders and installers).</summary>
    public static class ScenarioBindingEditor
    {
        /// <summary>
        /// Registers <paramref name="target"/> under <paramref name="key"/>: adds a <see cref="ScenarioObjectBinding"/>
        /// if it has none and sets its key.
        /// </summary>
        public static ScenarioObjectBinding Bind(GameObject target, string key)
        {
            if (!target.TryGetComponent(out ScenarioObjectBinding binding))
            {
                binding = target.AddComponent<ScenarioObjectBinding>();
            }
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("_key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return binding;
        }
    }
}
