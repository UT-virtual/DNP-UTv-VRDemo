namespace DNPDemo.Scenario
{
    /// <summary>
    /// The replicated scenario state: the current step and a sequence number that changes on every transition.
    /// The authoritative device moves it with <see cref="Start"/> / <see cref="Next"/> / <see cref="Back"/> / <see cref="Restart"/>;
    /// other devices copy it with <see cref="Apply"/>. Reports and commands carry the sequence they were made
    /// against, so ones that refer to a step that has already moved on are dropped (<see cref="IsCurrent"/>).
    /// </summary>
    public sealed class ScenarioProgress
    {
        public ScenarioProgress(int stepCount)
        {
            StepCount = stepCount;
        }

        public int StepCount { get; }

        /// <summary>Current step, or -1 while no run is active (not in a session).</summary>
        public int StepIndex { get; private set; } = -1;

        /// <summary>Changes on every transition, including a restart onto the same step.</summary>
        public ushort Sequence { get; private set; }

        public bool IsRunning => StepIndex >= 0;

        public bool Start() => !IsRunning && StepCount > 0 && GoTo(0);

        public bool Next() => IsRunning && StepIndex < StepCount - 1 && GoTo(StepIndex + 1);

        public bool Back() => IsRunning && StepIndex > 0 && GoTo(StepIndex - 1);

        public bool Restart() => IsRunning && GoTo(0);

        /// <summary>Ends the run (the local player left the session). The sequence keeps counting.</summary>
        public void Stop() => StepIndex = -1;

        /// <summary>True when (<paramref name="sequence"/>, <paramref name="stepIndex"/>) is the state shown right now.</summary>
        public bool IsCurrent(ushort sequence, int stepIndex) =>
            IsRunning && sequence == Sequence && stepIndex == StepIndex;

        /// <summary>Copies the authority's state. Returns false for duplicates and out-of-range steps.</summary>
        public bool Apply(ushort sequence, int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= StepCount || IsCurrent(sequence, stepIndex))
            {
                return false;
            }
            StepIndex = stepIndex;
            Sequence = sequence;
            return true;
        }

        bool GoTo(int stepIndex)
        {
            StepIndex = stepIndex;
            Sequence = unchecked((ushort)(Sequence + 1));
            return true;
        }
    }
}
