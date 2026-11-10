using System;

namespace DNPDemo.Interaction
{
    public enum InteractionMessageType : byte
    {
        /// <summary>Actor → others: a tool's in-progress state for (sequence, step, part). Carries the tool's payload.</summary>
        Progress = 1,
        /// <summary>Actor → others: the current part of (sequence, step) is now <c>Part</c> (equal to the part count when all are done).</summary>
        PartAdvanced = 2,
        /// <summary>Observer → actor: tell me the current part of (sequence, step) (after joining or entering the step).</summary>
        RequestPartState = 3,
    }

    /// <summary>
    /// Wire format of <see cref="InteractionRunner"/> messages: a fixed header that pins the message to one scenario
    /// state and part, followed by an optional tool payload.
    /// </summary>
    public readonly struct InteractionMessage
    {
        public const int HeaderSize = 6;

        /// <summary>Part indices travel as one byte.</summary>
        public const int MaxPartCount = byte.MaxValue;

        public readonly InteractionMessageType Type;
        public readonly ushort Sequence;
        public readonly ushort StepIndex;
        public readonly byte Part;

        public InteractionMessage(InteractionMessageType type, ushort sequence, int stepIndex, int part)
        {
            Type = type;
            Sequence = sequence;
            StepIndex = (ushort)stepIndex;
            Part = (byte)part;
        }

        public byte[] Serialize(byte[] payload = null)
        {
            int payloadLength = payload?.Length ?? 0;
            var buffer = new byte[HeaderSize + payloadLength];
            buffer[0] = (byte)Type;
            buffer[1] = (byte)Sequence;
            buffer[2] = (byte)(Sequence >> 8);
            buffer[3] = (byte)StepIndex;
            buffer[4] = (byte)(StepIndex >> 8);
            buffer[5] = Part;
            if (payloadLength > 0)
            {
                Buffer.BlockCopy(payload, 0, buffer, HeaderSize, payloadLength);
            }
            return buffer;
        }

        public static bool TryParse(byte[] buffer, out InteractionMessage message, out ArraySegment<byte> payload)
        {
            message = default;
            payload = default;
            if (buffer == null || buffer.Length < HeaderSize)
            {
                return false;
            }
            var type = (InteractionMessageType)buffer[0];
            if (type < InteractionMessageType.Progress || type > InteractionMessageType.RequestPartState)
            {
                return false;
            }
            message = new InteractionMessage(
                type,
                (ushort)(buffer[1] | (buffer[2] << 8)),
                (ushort)(buffer[3] | (buffer[4] << 8)),
                buffer[5]);
            payload = new ArraySegment<byte>(buffer, HeaderSize, buffer.Length - HeaderSize);
            return true;
        }
    }
}
