namespace DNPDemo.Scenario
{
    public enum ScenarioMessageType : byte
    {
        /// <summary>Authority → others: the current (sequence, step) and the step an interrupted run can resume at.</summary>
        State = 1,
        /// <summary>Non-authority → authority: send me the current state (on joining or reconnecting).</summary>
        RequestState = 2,
        /// <summary>Actor → authority: the interaction of (sequence, step) is done.</summary>
        ReportComplete = 3,
        /// <summary>Any device → authority: operator command made while seeing (sequence, step).</summary>
        Command = 4,
    }

    public enum OperatorCommand : byte
    {
        None = 0,
        Next = 1,
        Back = 2,
        /// <summary>Back to the first step (the lobby), for the next visitors.</summary>
        Restart = 3,
        /// <summary>To the step in <see cref="ScenarioMessage.TargetStep"/>.</summary>
        JumpTo = 4,
    }

    /// <summary>Wire format of <see cref="ScenarioDirector"/> messages. Every message has the same fixed size.</summary>
    public readonly struct ScenarioMessage
    {
        public const int Size = 10;

        /// <summary>Value of a step field that names no step.</summary>
        public const ushort NoStep = ushort.MaxValue;

        public readonly ScenarioMessageType Type;
        public readonly ushort Sequence;
        public readonly ushort StepIndex;
        public readonly OperatorCommand Command;

        /// <summary>Destination of a <see cref="OperatorCommand.JumpTo"/> command; <see cref="NoStep"/> otherwise.</summary>
        public readonly ushort TargetStep;

        /// <summary>In a <see cref="ScenarioMessageType.State"/>: the step an interrupted run can resume at, or <see cref="NoStep"/>.</summary>
        public readonly ushort ResumeStep;

        public ScenarioMessage(ScenarioMessageType type, ushort sequence, ushort stepIndex, OperatorCommand command,
            ushort targetStep = NoStep, ushort resumeStep = NoStep)
        {
            Type = type;
            Sequence = sequence;
            StepIndex = stepIndex;
            Command = command;
            TargetStep = targetStep;
            ResumeStep = resumeStep;
        }

        /// <param name="resumeStepIndex">The step an interrupted run can resume at, or a negative value for none.</param>
        public static ScenarioMessage State(ushort sequence, int stepIndex, int resumeStepIndex = -1) =>
            new(ScenarioMessageType.State, sequence, (ushort)stepIndex, OperatorCommand.None,
                resumeStep: ToStepField(resumeStepIndex));

        public static ScenarioMessage RequestState() =>
            new(ScenarioMessageType.RequestState, 0, 0, OperatorCommand.None);

        public static ScenarioMessage ReportComplete(ushort sequence, int stepIndex) =>
            new(ScenarioMessageType.ReportComplete, sequence, (ushort)stepIndex, OperatorCommand.None);

        public static ScenarioMessage CommandFor(OperatorCommand command, ushort sequence, int stepIndex, int targetStepIndex = -1) =>
            new(ScenarioMessageType.Command, sequence, (ushort)stepIndex, command, targetStep: ToStepField(targetStepIndex));

        /// <summary><see cref="ResumeStep"/> as an index, or -1 for none.</summary>
        public int ResumeStepIndex => FromStepField(ResumeStep);

        /// <summary><see cref="TargetStep"/> as an index, or -1 for none.</summary>
        public int TargetStepIndex => FromStepField(TargetStep);

        public byte[] Serialize()
        {
            return new[]
            {
                (byte)Type,
                (byte)Sequence,
                (byte)(Sequence >> 8),
                (byte)StepIndex,
                (byte)(StepIndex >> 8),
                (byte)Command,
                (byte)TargetStep,
                (byte)(TargetStep >> 8),
                (byte)ResumeStep,
                (byte)(ResumeStep >> 8),
            };
        }

        public static bool TryParse(byte[] buffer, out ScenarioMessage message)
        {
            message = default;
            if (buffer == null || buffer.Length < Size)
            {
                return false;
            }
            var type = (ScenarioMessageType)buffer[0];
            if (type < ScenarioMessageType.State || type > ScenarioMessageType.Command)
            {
                return false;
            }
            var command = (OperatorCommand)buffer[5];
            ushort targetStep = (ushort)(buffer[6] | (buffer[7] << 8));
            if (type == ScenarioMessageType.Command
                && (command < OperatorCommand.Next || command > OperatorCommand.JumpTo
                    || (command == OperatorCommand.JumpTo && targetStep == NoStep)))
            {
                return false;
            }
            message = new ScenarioMessage(
                type,
                (ushort)(buffer[1] | (buffer[2] << 8)),
                (ushort)(buffer[3] | (buffer[4] << 8)),
                command,
                targetStep,
                (ushort)(buffer[8] | (buffer[9] << 8)));
            return true;
        }

        static ushort ToStepField(int stepIndex) =>
            stepIndex >= 0 && stepIndex < NoStep ? (ushort)stepIndex : NoStep;

        static int FromStepField(ushort value) => value == NoStep ? -1 : value;
    }
}
