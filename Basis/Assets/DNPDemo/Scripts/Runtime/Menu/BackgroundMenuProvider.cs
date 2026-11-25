using Basis.BasisUI;
using DNPDemo.Interaction.Background;
using UnityEngine;

namespace DNPDemo.Menu
{
    /// <summary>Menu entry that closes the main menu and opens the background panel (<see cref="BackgroundPanel"/>) in front of the view.</summary>
    public sealed class BackgroundMenuProvider : DemoMenuProvider
    {
        /// <summary>Menu title, also the panel's heading. The task HUD names it when telling the actor where to look.</summary>
        public const string MenuTitle = "背景";

        public override string Title => MenuTitle;
        public override string IconAddress => AddressableAssets.Sprites.World;
        public override int Order => 20;

        public override void RunAction()
        {
            BackgroundPanel panel = BackgroundPanel.Active;
            if (panel == null)
            {
                Debug.LogWarning("[DNPDemo] シーンに BackgroundPanel がないため、背景パネルを開けません。");
                return;
            }
            BasisMainMenu.Close();
            panel.Open();
        }
    }
}
