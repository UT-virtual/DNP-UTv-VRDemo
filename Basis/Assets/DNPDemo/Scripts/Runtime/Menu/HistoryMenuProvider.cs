using Basis.BasisUI;
using DNPDemo.Interaction.History;
using UnityEngine;

namespace DNPDemo.Menu
{
    /// <summary>Menu entry that closes the main menu and opens the history panel (<see cref="HistoryPanel"/>) in front of the view.</summary>
    public sealed class HistoryMenuProvider : DemoMenuProvider
    {
        /// <summary>Menu title, also the panel's heading. The task HUD names it when telling the actor where to look.</summary>
        public const string MenuTitle = "モデルの履歴";

        public override string Title => MenuTitle;
        public override string IconAddress => AddressableAssets.Sprites.Clock;
        public override int Order => 30;

        public override void RunAction()
        {
            HistoryPanel panel = HistoryPanel.Active;
            if (panel == null)
            {
                Debug.LogWarning("[DNPDemo] シーンに HistoryPanel がないため、履歴パネルを開けません。");
                return;
            }
            BasisMainMenu.Close();
            panel.Open();
        }
    }
}
