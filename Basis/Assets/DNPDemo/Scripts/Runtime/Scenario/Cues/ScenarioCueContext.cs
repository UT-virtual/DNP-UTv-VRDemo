namespace DNPDemo.Scenario.Cues
{
    /// <summary>What a cue can reach while it plays: the scene bindings and the scenario-wide settings.</summary>
    public sealed class ScenarioCueContext
    {
        public ScenarioCueContext(ScenarioStage stage, ScenarioData scenario)
        {
            Stage = stage;
            Scenario = scenario;
        }

        public ScenarioStage Stage { get; }
        public ScenarioData Scenario { get; }
        public LineTiming LineTiming => Scenario.LineTiming;
    }
}
