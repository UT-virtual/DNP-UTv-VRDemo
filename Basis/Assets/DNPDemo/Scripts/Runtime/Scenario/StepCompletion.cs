namespace DNPDemo.Scenario
{
    /// <summary>
    /// What moves a step on to the next one. Whatever the condition, the operator can always move it on
    /// (see <see cref="ScenarioDirector.RequestCommand"/>).
    /// </summary>
    public enum StepCompletion : byte
    {
        /// <summary>Advances when the authoritative device finishes playing the step's cues.</summary>
        CuesFinished = 0,

        /// <summary>
        /// Advances when the second player arrives and both roles are present. It reacts to the arrival,
        /// not to the state: after a restart with both players still connected, the operator starts the run.
        /// </summary>
        PlayersReady = 1,

        /// <summary>Advances when the device of <see cref="ScenarioStep.Actor"/> reports the step's interaction as done.</summary>
        Interaction = 2,

        /// <summary>Only the operator advances it (for example the closing step that waits for a restart).</summary>
        Manual = 3,
    }

    public static class StepCompletionExtensions
    {
        /// <summary>Description shown on the operator panel.</summary>
        public static string ToDisplayName(this StepCompletion completion, DemoRole actor)
        {
            switch (completion)
            {
                case StepCompletion.CuesFinished: return "台詞が終わると自動で進む";
                case StepCompletion.PlayersReady: return "2人がそろうと開始";
                case StepCompletion.Interaction: return $"{actor.ToDisplayName()} の操作が完了すると進む";
                case StepCompletion.Manual: return "運営の操作で進む";
                default: return completion.ToString();
            }
        }
    }
}
