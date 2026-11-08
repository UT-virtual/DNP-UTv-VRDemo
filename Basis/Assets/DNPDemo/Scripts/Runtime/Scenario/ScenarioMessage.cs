namespace DNPDemo.Scenario
{
    public enum ScenarioMessageType : byte
    {
        /// <summary>Authority → others: the current (sequence, step).</summary>
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
        Restart = 3,
    }

    /// <summary>Wire format of <see cref="ScenarioDirector"/> messages. Every message has the same fixed size.</summary>
    public readonly struct ScenarioMessage
    {
        public const int Size = 6;

        public readonly ScenarioMessageType Type;
        public readonly ushort Sequence;
        public readonly ushort StepIndex;
        public readonly OperatorCommand Command;

        public ScenarioMessage(ScenarioMessageType type, ushort sequence, ushort stepIndex, OperatorCommand command)
        {
            Type = type;
            Sequence = sequence;
            StepIndex = stepIndex;
            Command = command;
        }

        public static ScenarioMessage State(ushort sequence, int stepIndex) =>
            new(ScenarioMessageType.State, sequence, (ushort)stepIndex, OperatorCommand.None);

        public static ScenarioMessage RequestState() =>
            new(ScenarioMessageType.RequestState, 0, 0, OperatorCommand.None);

        public static ScenarioMessage ReportComplete(ushort sequence, int stepIndex) =>
            new(ScenarioMessageType.ReportComplete, sequence, (ushort)stepIndex, OperatorCommand.None);

        public static ScenarioMessage CommandFor(OperatorCommand command, ushort sequence, int stepIndex) =>
            new(ScenarioMessageType.Command, sequence, (ushort)stepIndex, command);

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
            if (type == ScenarioMessageType.Command && (command < OperatorCommand.Next || command > OperatorCommand.Restart))
            {
                return false;
            }
            message = new ScenarioMessage(
                type,
                (ushort)(buffer[1] | (buffer[2] << 8)),
                (ushort)(buffer[3] | (buffer[4] << 8)),
                command);
            return true;
        }
    }
}
