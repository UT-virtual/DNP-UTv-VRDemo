using System;
using System.Collections.Generic;
using DNPDemo.Venus;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// The branch view of the history panel: a card per reached version, placed by <see cref="HistoryTree"/> and joined
    /// to its parent by a line from the parent's right edge to the child's left edge. A line to a child on another row
    /// drops halfway between the two columns.
    /// <para>
    /// Cards are cloned from an inactive template and lines are plain images, both kept for reuse. The whole tree is scaled
    /// down to fit this object's rectangle when it is larger, and centred in it.
    /// </para>
    /// </summary>
    public sealed class HistoryTreeView : MonoBehaviour
    {
        [Tooltip("Scaled and centred in this object's rectangle. Its children are placed from its top-left corner.")]
        [SerializeField] RectTransform _content;

        [Tooltip("Under the content, before the cards so the lines draw behind them.")]
        [SerializeField] RectTransform _lines;

        [SerializeField] RectTransform _cards;

        [Tooltip("Inactive card cloned for each version. Its size is the size of every card.")]
        [SerializeField] HistoryTreeCard _cardTemplate;

        [Tooltip("Gap between cards, in canvas pixels: x between columns, y between rows.")]
        [SerializeField] Vector2 _spacing = new(36f, 20f);

        [SerializeField, Min(1f)] float _lineWidth = 4f;
        [SerializeField] Color _lineColor = new(0.8f, 0.78f, 0.95f, 0.8f);

        readonly HistoryTree _tree = new();
        readonly List<HistoryTreeCard> _cardPool = new();
        readonly List<Image> _linePool = new();
        int _linesUsed;

        public HistoryTree Tree => _tree;

        /// <summary>A card was pressed. Carries its version.</summary>
        public event Action<VenusVersion> VersionPressed;

        /// <summary>The active card of <paramref name="version"/>, or null. For tests.</summary>
        internal HistoryTreeCard FindCard(VenusVersion version)
        {
            foreach (HistoryTreeCard card in _cardPool)
            {
                if (card.gameObject.activeSelf && card.Version == version)
                {
                    return card;
                }
            }
            return null;
        }

        /// <summary>Shows the tree of <paramref name="reached"/>, oldest first. The newest is framed as the current version.</summary>
        public void Show(IReadOnlyList<VenusVersion> reached)
        {
            _tree.Build(reached);
            VenusVersion current = reached != null && reached.Count > 0 ? reached[reached.Count - 1] : null;
            Vector2 cardSize = _cardTemplate != null ? _cardTemplate.Rect.rect.size : Vector2.zero;

            int cardsUsed = 0;
            _linesUsed = 0;
            foreach (HistoryTree.Node node in _tree.Nodes)
            {
                HistoryTreeCard card = GetCard(cardsUsed++);
                if (card == null)
                {
                    break;
                }
                RectTransform rect = card.Rect;
                PlaceTopLeft(rect, CellOrigin(node.Column, node.Row, cardSize), cardSize);
                card.Show(node.Version, node.Version == current);

                if (node.Parent != null && _tree.TryGetNode(node.Parent, out HistoryTree.Node parent))
                {
                    AddConnector(parent, node, cardSize);
                }
            }
            for (int i = cardsUsed; i < _cardPool.Count; i++)
            {
                _cardPool[i].gameObject.SetActive(false);
            }
            for (int i = _linesUsed; i < _linePool.Count; i++)
            {
                _linePool[i].gameObject.SetActive(false);
            }
            Fit(cardSize);
        }

        /// <summary>Top-left corner of a cell, from the content's top-left corner, y pointing up.</summary>
        Vector2 CellOrigin(int column, int row, Vector2 cardSize) =>
            new(column * (cardSize.x + _spacing.x), -row * (cardSize.y + _spacing.y));

        void AddConnector(HistoryTree.Node parent, HistoryTree.Node child, Vector2 cardSize)
        {
            if (_lines == null)
            {
                return;
            }
            Vector2 from = CellOrigin(parent.Column, parent.Row, cardSize) + new Vector2(cardSize.x, -cardSize.y * 0.5f);
            Vector2 to = CellOrigin(child.Column, child.Row, cardSize) + new Vector2(0f, -cardSize.y * 0.5f);
            if (Mathf.Approximately(from.y, to.y))
            {
                AddHorizontal(from.x, to.x, from.y);
                return;
            }
            float drop = (from.x + to.x) * 0.5f;
            AddHorizontal(from.x, drop, from.y);
            AddVertical(drop, from.y, to.y);
            AddHorizontal(drop, to.x, to.y);
        }

        void AddHorizontal(float x0, float x1, float y)
        {
            float half = _lineWidth * 0.5f;
            // Half a width past each end, so joins with a vertical line are square.
            PlaceTopLeft(GetLine(), new Vector2(Mathf.Min(x0, x1) - half, y + half), new Vector2(Mathf.Abs(x1 - x0) + _lineWidth, _lineWidth));
        }

        void AddVertical(float x, float y0, float y1)
        {
            float half = _lineWidth * 0.5f;
            PlaceTopLeft(GetLine(), new Vector2(x - half, Mathf.Max(y0, y1)), new Vector2(_lineWidth, Mathf.Abs(y1 - y0)));
        }

        void Fit(Vector2 cardSize)
        {
            if (_content == null)
            {
                return;
            }
            int columns = _tree.ColumnCount;
            int rows = _tree.RowCount;
            var size = new Vector2(
                columns > 0 ? columns * cardSize.x + (columns - 1) * _spacing.x : 0f,
                rows > 0 ? rows * cardSize.y + (rows - 1) * _spacing.y : 0f);
            _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0.5f, 0.5f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = size;

            Vector2 area = ((RectTransform)transform).rect.size;
            float scale = 1f;
            if (size.x > area.x && size.x > 0f)
            {
                scale = Mathf.Min(scale, area.x / size.x);
            }
            if (size.y > area.y && size.y > 0f)
            {
                scale = Mathf.Min(scale, area.y / size.y);
            }
            _content.localScale = new Vector3(scale, scale, 1f);
        }

        HistoryTreeCard GetCard(int index)
        {
            if (index < _cardPool.Count)
            {
                _cardPool[index].gameObject.SetActive(true);
                return _cardPool[index];
            }
            if (_cardTemplate == null || _cards == null)
            {
                return null;
            }
            HistoryTreeCard card = Instantiate(_cardTemplate, _cards);
            card.name = $"Card {index}";
            card.Pressed += HandleCardPressed;
            card.gameObject.SetActive(true);
            _cardPool.Add(card);
            return card;
        }

        RectTransform GetLine()
        {
            if (_linesUsed < _linePool.Count)
            {
                Image pooled = _linePool[_linesUsed++];
                pooled.gameObject.SetActive(true);
                return pooled.rectTransform;
            }
            var go = new GameObject($"Line {_linePool.Count}", typeof(RectTransform));
            go.layer = _lines.gameObject.layer;
            go.transform.SetParent(_lines, false);
            var image = go.AddComponent<Image>();
            image.color = _lineColor;
            image.raycastTarget = false;
            _linePool.Add(image);
            _linesUsed++;
            return image.rectTransform;
        }

        static void PlaceTopLeft(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
        }

        void HandleCardPressed(VenusVersion version) => VersionPressed?.Invoke(version);

        void OnDestroy()
        {
            foreach (HistoryTreeCard card in _cardPool)
            {
                if (card != null)
                {
                    card.Pressed -= HandleCardPressed;
                }
            }
        }
    }
}
