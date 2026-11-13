using Basis.BasisUI;

namespace DNPDemo.Menu
{
    /// <summary>
    /// Base for the demo's own buttons on the Basis main menu hotbar (opened with Y).
    /// <see cref="BasisPolicy.DemoMenuPolicy"/> keeps only subclasses of this type on the menu and registers them;
    /// add a new entry by deriving from <see cref="DemoPageMenuProvider"/> (a tall page beside the hotbar) or
    /// <see cref="DemoBarMenuProvider"/> (a row of buttons above the hotbar) and listing it in
    /// <see cref="BasisPolicy.DemoMenuPolicy"/>. Pressing the hotbar button toggles the panel.
    /// </summary>
    public abstract class DemoMenuProvider : BasisMenuActionProvider<BasisMainMenu>
    {
        public override bool Hidden => false;

        public sealed override void RunAction()
        {
            if (BasisMainMenu.ActiveMenuTitle == Title)
            {
                BasisMainMenu.CloseActivePanel();
                return;
            }

            BasisMenuPanel panel = BasisMainMenu.CreateActiveMenu(CreatePanelData(), PanelStyle, this);
            BoundButton?.BindActiveStateToAddressablesInstance(panel);
            BuildPanel(panel);
        }

        /// <summary>Title, size and position of the panel, in hotbar canvas units.</summary>
        protected abstract BasisMenuPanel.PanelData CreatePanelData();

        /// <summary>Prefab the panel is made from (<see cref="BasisMenuPanel.PanelStyles"/>).</summary>
        protected abstract string PanelStyle { get; }

        /// <summary>Fills a freshly opened panel. Called each time the panel opens.</summary>
        protected abstract void BuildPanel(BasisMenuPanel panel);
    }
}
