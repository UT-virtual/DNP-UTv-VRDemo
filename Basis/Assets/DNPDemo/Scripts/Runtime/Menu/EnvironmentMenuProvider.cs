using Basis.BasisUI;

namespace DNPDemo.Menu
{
    /// <summary>Menu entry for choosing the scenery. Placeholder until the environment presets are implemented.</summary>
    public sealed class EnvironmentMenuProvider : PlaceholderMenuProvider
    {
        public override string Title => "風景";
        public override string IconAddress => AddressableAssets.Sprites.World;
        public override int Order => 20;
    }
}
