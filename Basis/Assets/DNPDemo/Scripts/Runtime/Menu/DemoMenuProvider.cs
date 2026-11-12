using Basis.BasisUI;
using UnityEngine;

namespace DNPDemo.Menu
{
    /// <summary>
    /// Base for the demo's own buttons on the Basis main menu hotbar (opened with Y).
    /// <see cref="BasisPolicy.DemoMenuPolicy"/> keeps only subclasses of this type on the menu and registers them;
    /// add a new entry by deriving from this class and listing it in <see cref="BasisPolicy.DemoMenuPolicy"/>.
    /// Pressing the button toggles a page whose content the subclass builds.
    /// </summary>
    public abstract class DemoMenuProvider : BasisMenuActionProvider<BasisMainMenu>
    {
        // Same page geometry as the Basis providers, so the page sits beside the hotbar like theirs.
        static readonly Vector2 PageSize = new(587f, 1025f);
        static readonly Vector3 PagePosition = new(456f, 25f, 0f);

        public override bool Hidden => false;

        public sealed override void RunAction()
        {
            if (BasisMainMenu.ActiveMenuTitle == Title)
            {
                BasisMainMenu.CloseActivePanel();
                return;
            }

            BasisMenuPanel page = BasisMainMenu.CreateActiveMenu(
                new BasisMenuPanel.PanelData
                {
                    Title = Title,
                    PanelSize = PageSize,
                    PanelPosition = PagePosition,
                },
                BasisMenuPanel.PanelStyles.Page,
                this);
            BoundButton?.BindActiveStateToAddressablesInstance(page);
            BuildPage(page.Descriptor.ContentParent);
        }

        /// <summary>Fills a freshly opened page. Called each time the page opens.</summary>
        protected abstract void BuildPage(RectTransform content);
    }
}
