using System.Collections.Generic;
using Basis.BasisUI;
using DNPDemo.Interaction;
using DNPDemo.Pen;

namespace DNPDemo.Menu
{
    /// <summary>Menu entry for choosing the pen's mode. Opens a bar with one button per mode above the hotbar.</summary>
    public sealed class PenMenuProvider : DemoBarMenuProvider
    {
        /// <summary>Menu title. The task HUD names it when telling the actor which mode to pick.</summary>
        public const string MenuTitle = "空間ペン";

        public override string Title => MenuTitle;
        public override string IconAddress => AddressableAssets.Sprites.Select;
        public override int Order => 10;

        protected override IReadOnlyList<BarItem> CreateItems()
        {
            var items = new List<BarItem>(PenModeSelection.Selectable.Length);
            foreach (PenMode mode in PenModeSelection.Selectable)
            {
                PenMode captured = mode;
                items.Add(new BarItem(
                    mode.ToDisplayName(),
                    IconFor(mode),
                    () => PenModeSelection.Select(captured),
                    () => PenModeSelection.Current == captured));
            }
            return items;
        }

        static string IconFor(PenMode mode) => mode switch
        {
            PenMode.Select => AddressableAssets.Sprites.Select,
            PenMode.Voice => AddressableAssets.Sprites.Microphone,
            PenMode.Draw => AddressableAssets.Sprites.Polygons,
            PenMode.Manipulate => AddressableAssets.Sprites.Move,
            _ => AddressableAssets.Sprites.Information,
        };
    }
}
