using System.Collections.Generic;
using DNPDemo.Venus;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// Where each reached version sits in the branch view of the history panel, worked out from the versions reached so
    /// far and their <see cref="VenusVersion.Parent"/>. The tree grows as the scenario reaches more versions.
    /// <list type="bullet">
    /// <item>A version stands one column to the right of its parent.</item>
    /// <item>A version continues its parent's row, unless it <see cref="VenusVersion.StartsBranch"/> or another child already
    /// continues that row; then it starts a new row below every row so far.</item>
    /// <item>A version whose parent is unset or not reached yet starts a new row in the first column.</item>
    /// </list>
    /// No two versions share a cell. A branch line can cross another row's line when a branch starts below rows that
    /// continue past its column; the scenario's history has no such case.
    /// </summary>
    public sealed class HistoryTree
    {
        public readonly struct Node
        {
            public Node(VenusVersion version, VenusVersion parent, int column, int row)
            {
                Version = version;
                Parent = parent;
                Column = column;
                Row = row;
            }

            public VenusVersion Version { get; }

            /// <summary>The parent drawn in the tree: <see cref="VenusVersion.Parent"/> when it is reached, otherwise null.</summary>
            public VenusVersion Parent { get; }

            public int Column { get; }
            public int Row { get; }
        }

        readonly List<Node> _nodes = new();
        readonly Dictionary<VenusVersion, int> _indexOf = new();

        /// <summary>The reached versions in the order they were given (oldest first), each once.</summary>
        public IReadOnlyList<Node> Nodes => _nodes;

        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        /// <summary>
        /// Lays out <paramref name="reached"/>, oldest first, as <see cref="VenusVersionHistory.Collect"/> returns them.
        /// Empty entries and repeats are dropped.
        /// </summary>
        public void Build(IEnumerable<VenusVersion> reached)
        {
            _nodes.Clear();
            _indexOf.Clear();
            ColumnCount = 0;
            RowCount = 0;
            if (reached == null)
            {
                return;
            }
            // Parents whose row a child already continues.
            var continued = new HashSet<VenusVersion>();
            foreach (VenusVersion version in reached)
            {
                if (version == null || _indexOf.ContainsKey(version))
                {
                    continue;
                }
                VenusVersion parent = version.Parent;
                int column;
                int row;
                if (parent != null && _indexOf.TryGetValue(parent, out int parentIndex))
                {
                    Node parentNode = _nodes[parentIndex];
                    column = parentNode.Column + 1;
                    row = !version.StartsBranch && continued.Add(parent) ? parentNode.Row : RowCount;
                }
                else
                {
                    parent = null;
                    column = 0;
                    row = RowCount;
                }
                _indexOf.Add(version, _nodes.Count);
                _nodes.Add(new Node(version, parent, column, row));
                ColumnCount = System.Math.Max(ColumnCount, column + 1);
                RowCount = System.Math.Max(RowCount, row + 1);
            }
        }

        /// <summary>The node of <paramref name="version"/>, if it is in the tree.</summary>
        public bool TryGetNode(VenusVersion version, out Node node)
        {
            if (version != null && _indexOf.TryGetValue(version, out int index))
            {
                node = _nodes[index];
                return true;
            }
            node = default;
            return false;
        }
    }
}
