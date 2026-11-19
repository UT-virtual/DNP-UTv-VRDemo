namespace DNPDemo.Scenario
{
    /// <summary>
    /// State of a keyed object (<see cref="ScenarioObjectBinding"/>) that the stage cannot restore by itself, such as the
    /// version a statue shows. <see cref="ScenarioStage.ResetToBaseline"/> calls it on every component of a bound object
    /// that implements it, after restoring the object's active state and pose, so the lasting cues of earlier steps start
    /// again from the state at scene load.
    /// </summary>
    public interface IStageResettable
    {
        /// <summary>Returns to the state at scene load, without animation.</summary>
        void ResetToBaseline();
    }
}
