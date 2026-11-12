namespace DNPDemo.Scenario
{
    /// <summary>Who delivers a line. Each speaker is bound to a voice source and a subtitle view by a <see cref="ScenarioSpeakerBinding"/> in the scene.</summary>
    public enum ScenarioSpeaker : byte
    {
        Narrator = 0,
        ScholarA = 1,
        ScholarB = 2,
    }

    public static class ScenarioSpeakerExtensions
    {
        /// <summary>Name shown above the subtitle. Empty for the narrator, whose subtitle carries no label.</summary>
        public static string ToDisplayName(this ScenarioSpeaker speaker)
        {
            switch (speaker)
            {
                case ScenarioSpeaker.ScholarA: return "学者A（考古学）";
                case ScenarioSpeaker.ScholarB: return "学者B（構造力学）";
                default: return string.Empty;
            }
        }
    }
}
