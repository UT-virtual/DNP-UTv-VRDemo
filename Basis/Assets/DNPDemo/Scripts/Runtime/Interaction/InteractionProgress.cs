namespace DNPDemo.Interaction
{
    /// <summary>
    /// Which part of the current interaction step is in progress. It belongs to one scenario state
    /// (sequence, step); a new state starts it over. The actor's device advances it when a part completes and
    /// tells the others, who copy it with <see cref="ApplyRemote"/>.
    /// </summary>
    public sealed class InteractionProgress
    {
        public ushort Sequence { get; private set; }

        /// <summary>Step this progress belongs to, or -1 when there is no interaction step.</summary>
        public int StepIndex { get; private set; } = -1;

        /// <summary>Current part. Equal to <see cref="PartCount"/> once every part is done.</summary>
        public int PartIndex { get; private set; }

        public int PartCount { get; private set; }

        /// <summary>True while a part is waiting to be done.</summary>
        public bool IsActive => StepIndex >= 0 && PartIndex < PartCount;

        /// <summary>True once every part of the step is done.</summary>
        public bool IsFinished => StepIndex >= 0 && PartCount > 0 && PartIndex >= PartCount;

        /// <summary>Starts the parts of (<paramref name="sequence"/>, <paramref name="stepIndex"/>) from the first one.</summary>
        public void Begin(ushort sequence, int stepIndex, int partCount)
        {
            Sequence = sequence;
            StepIndex = stepIndex;
            PartCount = partCount < 0 ? 0 : partCount;
            PartIndex = 0;
        }

        public void Clear()
        {
            StepIndex = -1;
            PartIndex = 0;
            PartCount = 0;
        }

        public bool IsCurrentStep(ushort sequence, int stepIndex) =>
            StepIndex >= 0 && Sequence == sequence && StepIndex == stepIndex;

        /// <summary>Marks <paramref name="partIndex"/> done. False when it is not the current part.</summary>
        public bool CompletePart(int partIndex)
        {
            if (!IsActive || partIndex != PartIndex)
            {
                return false;
            }
            PartIndex++;
            return true;
        }

        /// <summary>
        /// Copies the actor's progress. Only moves forward, and only within the same scenario state,
        /// so late or duplicated messages are ignored.
        /// </summary>
        public bool ApplyRemote(ushort sequence, int stepIndex, int partIndex)
        {
            if (!IsCurrentStep(sequence, stepIndex) || partIndex <= PartIndex || partIndex > PartCount)
            {
                return false;
            }
            PartIndex = partIndex;
            return true;
        }
    }
}
