namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// Render queues that let the history preview stand behind the panel and still show over its background.
    /// All three draw in the opaque range (up to 2500), after the ordinary scene geometry (2000):
    /// <list type="number">
    /// <item><see cref="PanelBackground"/>: the panel's background colour, depth tested but not written, so whatever is in
    /// front of the panel (a hand, the pen) hides it and the room behind it is covered.</item>
    /// <item><see cref="Preview"/>: the turning model behind the panel. The background wrote no depth, so the model draws
    /// over it.</item>
    /// <item><see cref="PanelDepth"/>: the panel's shape written to depth only, so transparent objects behind the panel
    /// (selections, subtitles, ghosts) do not show through it.</item>
    /// </list>
    /// The labels and buttons are ordinary transparent UI (3000) in front of the model.
    /// </summary>
    public static class HistoryRenderQueues
    {
        public const int PanelBackground = 2450;
        public const int Preview = 2460;
        public const int PanelDepth = 2470;
    }
}
