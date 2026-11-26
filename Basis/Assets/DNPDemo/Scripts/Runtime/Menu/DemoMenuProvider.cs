using Basis.BasisUI;

namespace DNPDemo.Menu
{
    /// <summary>
    /// Base for the demo's own buttons on the Basis main menu hotbar (opened with Y).
    /// <see cref="BasisPolicy.DemoMenuPolicy"/> keeps only subclasses of this type on the menu and registers them.
    /// Add a new entry by deriving from one of these and listing it in <see cref="BasisPolicy.DemoMenuPolicy"/>:
    /// <list type="bullet">
    /// <item><see cref="DemoPageMenuProvider"/>: a tall page beside the hotbar.</item>
    /// <item><see cref="DemoBarMenuProvider"/>: a row of buttons above the hotbar.</item>
    /// <item>This class directly, implementing <see cref="BasisMenuActionProvider{TMenu}.RunAction"/>: an action that
    /// does not open a menu panel, such as opening one of the demo's world-space panels.</item>
    /// </list>
    /// </summary>
    public abstract class DemoMenuProvider : BasisMenuActionProvider<BasisMainMenu>
    {
        public override bool Hidden => false;
    }
}
