using DNPDemo.Scenario;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    /// <summary>Gives test objects a scenario key, as <see cref="ScenarioObjectBinding"/> does in the scene.</summary>
    static class TestBinding
    {
        public static ScenarioObjectBinding Bind(GameObject target, string key)
        {
            var binding = target.AddComponent<ScenarioObjectBinding>();
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("_key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return binding;
        }
    }
}
