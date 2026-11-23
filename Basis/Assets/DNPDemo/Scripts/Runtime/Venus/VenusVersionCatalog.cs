using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Venus
{
    /// <summary>
    /// Every version of the statue, in the order the scenario reaches them. <see cref="VenusStatue"/> builds the models
    /// of all of them up front so that switching never instantiates during the demo.
    /// </summary>
    [CreateAssetMenu(menuName = "DNP Demo/Venus Version Catalog", fileName = "VenusVersions")]
    public sealed class VenusVersionCatalog : ScriptableObject
    {
        [SerializeField] List<VenusVersion> _versions = new();

        public IReadOnlyList<VenusVersion> Versions => _versions;

        public bool Contains(VenusVersion version) => version != null && _versions.Contains(version);

        /// <summary>The version whose <see cref="VenusVersion.Id"/> is <paramref name="id"/>, or null.</summary>
        public VenusVersion Find(string id)
        {
            foreach (VenusVersion version in _versions)
            {
                if (version != null && version.Id == id)
                {
                    return version;
                }
            }
            return null;
        }

        internal void SetVersions(IEnumerable<VenusVersion> versions)
        {
            _versions = new List<VenusVersion>(versions);
        }

        /// <summary>Problems that would break switching or the history. Empty when the catalog is usable.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (_versions.Count == 0)
            {
                problems.Add("版がありません。");
            }
            var ids = new HashSet<string>();
            var seen = new HashSet<VenusVersion>();
            for (int i = 0; i < _versions.Count; i++)
            {
                VenusVersion version = _versions[i];
                string label = $"版 {i}";
                if (version == null)
                {
                    problems.Add($"{label} が未設定です。");
                    continue;
                }
                if (!seen.Add(version))
                {
                    problems.Add($"{label}（{version.name}）が重複しています。");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(version.Id))
                {
                    problems.Add($"{label}（{version.name}）の Id が空です。");
                }
                else if (!ids.Add(version.Id))
                {
                    problems.Add($"{label}（{version.name}）の Id「{version.Id}」が重複しています。");
                }
                if (string.IsNullOrWhiteSpace(version.DisplayName))
                {
                    problems.Add($"{label}（{version.name}）の表示名が空です。");
                }
                if (version.Model == null)
                {
                    problems.Add($"{label}（{version.name}）のモデルが未設定です。");
                }
                // The parent has to be reached first; this also rules out a version being its own ancestor.
                if (version.Parent != null && (version.Parent == version || !seen.Contains(version.Parent)))
                {
                    problems.Add($"{label}（{version.name}）の派生元（{version.Parent.name}）が、カタログでこの版より前にありません。");
                }
            }
            return problems;
        }
    }
}
