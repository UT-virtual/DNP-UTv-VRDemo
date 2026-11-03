namespace DNPDemo
{
    /// <summary>
    /// Player roles from the scenario. Which device gets which role is decided by <see cref="Roles.DemoRoleService"/>.
    /// </summary>
    public enum DemoRole : byte
    {
        None = 0,
        PlayerA = 1,
        PlayerB = 2,
    }

    public static class DemoRoleExtensions
    {
        /// <summary>Name shown to players (nameplates, lobby, HUD).</summary>
        public static string ToDisplayName(this DemoRole role)
        {
            switch (role)
            {
                case DemoRole.PlayerA: return "プレイヤーA";
                case DemoRole.PlayerB: return "プレイヤーB";
                default: return string.Empty;
            }
        }
    }
}
