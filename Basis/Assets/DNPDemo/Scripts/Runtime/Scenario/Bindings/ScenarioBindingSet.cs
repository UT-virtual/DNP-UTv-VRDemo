using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// The speaker and object bindings found in one scene, indexed for lookup. Duplicates are reported in
    /// <see cref="Problems"/> and the first binding found wins.
    /// </summary>
    public sealed class ScenarioBindingSet
    {
        readonly Dictionary<ScenarioSpeaker, ScenarioSpeakerBinding> _speakers = new();
        readonly Dictionary<string, GameObject> _objects = new();
        readonly List<string> _problems = new();

        public IReadOnlyDictionary<ScenarioSpeaker, ScenarioSpeakerBinding> Speakers => _speakers;
        public IReadOnlyDictionary<string, GameObject> Objects => _objects;

        /// <summary>Duplicate or empty bindings. Empty when the scene is consistent.</summary>
        public IReadOnlyList<string> Problems => _problems;

        /// <summary>Collects every binding under the scene's root objects, inactive ones included.</summary>
        public static ScenarioBindingSet Collect(Scene scene)
        {
            var set = new ScenarioBindingSet();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return set;
            }
            var speakers = new List<ScenarioSpeakerBinding>();
            var objects = new List<ScenarioObjectBinding>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                root.GetComponentsInChildren(true, speakers);
                foreach (ScenarioSpeakerBinding binding in speakers)
                {
                    set.Add(binding);
                }
                root.GetComponentsInChildren(true, objects);
                foreach (ScenarioObjectBinding binding in objects)
                {
                    set.Add(binding);
                }
            }
            return set;
        }

        public bool TryGetSpeaker(ScenarioSpeaker speaker, out ScenarioSpeakerBinding binding) =>
            _speakers.TryGetValue(speaker, out binding);

        public bool TryGetObject(string key, out GameObject target)
        {
            target = null;
            return key != null && _objects.TryGetValue(key, out target);
        }

        void Add(ScenarioSpeakerBinding binding)
        {
            if (_speakers.TryGetValue(binding.Speaker, out ScenarioSpeakerBinding existing))
            {
                _problems.Add($"話者 {binding.Speaker} の割り当てが重複しています（{PathOf(existing)} と {PathOf(binding)}）。");
                return;
            }
            _speakers.Add(binding.Speaker, binding);
        }

        void Add(ScenarioObjectBinding binding)
        {
            if (string.IsNullOrWhiteSpace(binding.Key))
            {
                _problems.Add($"{PathOf(binding)} のキーが空です。");
                return;
            }
            if (_objects.TryGetValue(binding.Key, out GameObject existing))
            {
                _problems.Add($"キー「{binding.Key}」が重複しています（{PathOf(existing)} と {PathOf(binding)}）。");
                return;
            }
            _objects.Add(binding.Key, binding.gameObject);
        }

        static string PathOf(Component component) => PathOf(component.gameObject);

        static string PathOf(GameObject go)
        {
            string path = go.name;
            for (Transform t = go.transform.parent; t != null; t = t.parent)
            {
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
