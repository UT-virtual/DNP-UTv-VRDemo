using System.Collections.Generic;
using DNPDemo.Venus;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// Which version of the history the detail view of the panel shows, and stepping back and forth through the list.
    /// The list is oldest first; <see cref="HistoryPanel"/> replaces it whenever the scenario reaches another step.
    /// </summary>
    public sealed class HistoryBrowser
    {
        readonly List<VenusVersion> _versions = new();

        /// <summary>The versions reached so far, oldest first.</summary>
        public IReadOnlyList<VenusVersion> Versions => _versions;

        /// <summary>Position of <see cref="Selected"/> in <see cref="Versions"/>; -1 when the list is empty.</summary>
        public int Index { get; private set; } = -1;

        /// <summary>The version on show, or null when the list is empty.</summary>
        public VenusVersion Selected => Index >= 0 ? _versions[Index] : null;

        public bool CanGoBack => Index > 0;
        public bool CanGoForward => Index >= 0 && Index < _versions.Count - 1;

        /// <summary>
        /// Replaces the list. The selected version stays selected while it is still in the list; otherwise the newest
        /// version is selected (for example after the operator moved the scenario back past it).
        /// </summary>
        public void SetVersions(IEnumerable<VenusVersion> versions)
        {
            VenusVersion previous = Selected;
            _versions.Clear();
            if (versions != null)
            {
                foreach (VenusVersion version in versions)
                {
                    if (version != null && !_versions.Contains(version))
                    {
                        _versions.Add(version);
                    }
                }
            }
            int kept = previous != null ? _versions.IndexOf(previous) : -1;
            Index = kept >= 0 ? kept : _versions.Count - 1;
        }

        /// <summary>Selects the newest version, the one the statue shows now.</summary>
        public void SelectLatest() => Index = _versions.Count - 1;

        /// <summary>Selects <paramref name="version"/>. Returns false, keeping the selection, when it is not in the list.</summary>
        public bool Select(VenusVersion version)
        {
            int index = version != null ? _versions.IndexOf(version) : -1;
            if (index < 0)
            {
                return false;
            }
            Index = index;
            return true;
        }

        /// <summary>Selects the next older version. Returns false when there is none.</summary>
        public bool GoBack()
        {
            if (!CanGoBack)
            {
                return false;
            }
            Index--;
            return true;
        }

        /// <summary>Selects the next newer version. Returns false when there is none.</summary>
        public bool GoForward()
        {
            if (!CanGoForward)
            {
                return false;
            }
            Index++;
            return true;
        }
    }
}
