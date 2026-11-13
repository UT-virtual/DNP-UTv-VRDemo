using Basis.BasisUI;
using UnityEngine;

namespace DNPDemo.Menu
{
    /// <summary>A menu entry that opens a tall page beside the hotbar, like the Basis settings pages.</summary>
    public abstract class DemoPageMenuProvider : DemoMenuProvider
    {
        // Same page geometry as the Basis providers, so the page sits beside the hotbar like theirs.
        static readonly Vector2 PageSize = new(587f, 1025f);
        static readonly Vector3 PagePosition = new(456f, 25f, 0f);

        protected sealed override BasisMenuPanel.PanelData CreatePanelData() => new()
        {
            Title = Title,
            PanelSize = PageSize,
            PanelPosition = PagePosition,
        };

        protected sealed override string PanelStyle => BasisMenuPanel.PanelStyles.Page;

        protected sealed override void BuildPanel(BasisMenuPanel panel) => BuildPage(panel.Descriptor.ContentParent);

        /// <summary>Fills a freshly opened page. Called each time the page opens.</summary>
        protected abstract void BuildPage(RectTransform content);
    }
}
