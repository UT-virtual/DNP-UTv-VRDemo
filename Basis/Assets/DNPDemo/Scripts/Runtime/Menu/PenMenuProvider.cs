using Basis.BasisUI;

namespace DNPDemo.Menu
{
    /// <summary>Menu entry for choosing the pen. Placeholder until the pen tools are implemented.</summary>
    public sealed class PenMenuProvider : PlaceholderMenuProvider
    {
        public override string Title => "ペン";
        public override string IconAddress => AddressableAssets.Sprites.Select;
        public override int Order => 10;
    }
}
