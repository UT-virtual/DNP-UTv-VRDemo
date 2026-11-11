namespace DNPDemo.BasisPolicy
{
    /// <summary>
    /// Removes the Basis controller actions the demo does not use. Basis rebuilds its bindings on startup and on every
    /// device mode switch, so the actions are removed again each time <see cref="BasisActionDriver.BindingsLoaded"/> fires.
    /// The menu (Y), walking (left stick) and turning (right stick, left/right) stay as Basis sets them.
    /// </summary>
    static class DemoInputPolicy
    {
        internal static readonly BasisActionDriver.ActionId[] RemovedActions =
        {
            BasisActionDriver.ActionId.ToggleMicOnPrimaryReleaseIfNoHover, // X: mute toggle
            BasisActionDriver.ActionId.JumpOnPrimaryButton,                // A: jump
            BasisActionDriver.ActionId.DescendOnGripButton,                // right grip: descend while flying
        };

        internal static void Install()
        {
            BasisActionDriver.BindingsLoaded -= RemoveActions;
            BasisActionDriver.BindingsLoaded += RemoveActions;
        }

        static void RemoveActions()
        {
            foreach (BasisActionDriver.ActionId action in RemovedActions)
            {
                BasisActionDriver.UnbindAll(action);
            }
        }
    }
}
