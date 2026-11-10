namespace DNPDemo.Interaction
{
    /// <summary>
    /// What the spatial pen does right now. Decided by the current interaction part and the local role
    /// (<see cref="InteractionRunner.LocalPenMode"/>); visitors never switch it themselves.
    /// </summary>
    public enum PenMode : byte
    {
        /// <summary>No operation for this device (not the actor, or no interaction step).</summary>
        None = 0,
        /// <summary>Range selection with the tip sphere (SelectionTool).</summary>
        Select = 1,
        /// <summary>Drawing strokes in the air, as a guide or a line drawing (StrokeTool).</summary>
        Guide = 2,
        /// <summary>Grabbing and moving a selected part within constraints (ManipulateTool).</summary>
        Manipulate = 3,
        /// <summary>Voice command while the button is held (VoiceCommandTool).</summary>
        Voice = 4,
        /// <summary>Operating a hand panel with the laser (HistoryPanel).</summary>
        Panel = 5,
    }

    public static class PenModeExtensions
    {
        /// <summary>Name shown to players (HUD, pen tip).</summary>
        public static string ToDisplayName(this PenMode mode)
        {
            switch (mode)
            {
                case PenMode.Select: return "範囲選択";
                case PenMode.Guide: return "ガイド";
                case PenMode.Manipulate: return "操作";
                case PenMode.Voice: return "音声入力";
                case PenMode.Panel: return "パネル";
                default: return string.Empty;
            }
        }
    }
}
