namespace DNPDemo.Interaction
{
    /// <summary>
    /// What the spatial pen does. Players pick the pen's mode on the main menu (<see cref="Pen.PenModeSelection"/>);
    /// each interaction part declares the mode it needs (<see cref="InteractionPart.Mode"/>), and a tool takes pen input
    /// only while the two match.
    /// </summary>
    public enum PenMode : byte
    {
        /// <summary>No operation (not the actor, or no interaction step).</summary>
        None = 0,
        /// <summary>Range selection with the tip sphere (SelectionTool).</summary>
        Select = 1,
        /// <summary>Drawing strokes in the air, as a guide or a line drawing (StrokeTool).</summary>
        Draw = 2,
        /// <summary>Grabbing and moving a selected part within constraints (ManipulateTool).</summary>
        Manipulate = 3,
        /// <summary>Voice command while the trigger is held (VoiceCommandTool).</summary>
        Voice = 4,
        /// <summary>Operating a hand panel with the laser (HistoryPanel).</summary>
        Panel = 5,
    }

    public static class PenModeExtensions
    {
        /// <summary>Name shown to players (menu, HUD).</summary>
        public static string ToDisplayName(this PenMode mode)
        {
            switch (mode)
            {
                case PenMode.Select: return "範囲選択";
                case PenMode.Draw: return "線画";
                case PenMode.Manipulate: return "操作";
                case PenMode.Voice: return "音声入力";
                case PenMode.Panel: return "パネル";
                default: return string.Empty;
            }
        }
    }
}
