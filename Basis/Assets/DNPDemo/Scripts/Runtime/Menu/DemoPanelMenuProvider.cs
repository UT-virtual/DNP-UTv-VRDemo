using Basis.BasisUI;

namespace DNPDemo.Menu
{
    /// <summary>
    /// A menu entry that opens a Basis menu panel next to the hotbar. Pressing the hotbar button toggles the panel.
    /// Derive from <see cref="DemoPageMenuProvider"/> or <see cref="DemoBarMenuProvider"/> rather than from this class.
    /// </summary>
    public abstract class DemoPanelMenuProvider : DemoMenuProvider
    {
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
