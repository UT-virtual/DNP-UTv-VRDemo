using System;
using DNPDemo.Interaction;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Wire format of <see cref="SpatialPenSync"/>: one message per state, numbered so that a late packet never
    /// overwrites a newer state (states travel on both an unreliable and a reliable channel).
    /// The sender is identified by the network, not by the payload, so a device can only change its own pen.
    /// </summary>
    public readonly struct PenStateMessage
    {
        public const byte FormatVersion = 3;

        // Version, sequence (2), mode, flags, then the radius and the extension as 32-bit floats.
        public const int Size = 5 + 2 * sizeof(float);

        const byte HeldFlag = 1 << 0;
        const byte TriggerFlag = 1 << 1;

        public readonly ushort Sequence;
        public readonly PenState State;

        public PenStateMessage(ushort sequence, PenState state)
        {
            Sequence = sequence;
            State = state;
        }

        /// <summary>True when <paramref name="candidate"/> was sent after <paramref name="latest"/>, allowing wrap-around.</summary>
        public static bool IsNewer(ushort candidate, ushort latest) => (short)(candidate - latest) > 0;

        public byte[] Serialize()
        {
            var buffer = new byte[Size];
            buffer[0] = FormatVersion;
            buffer[1] = (byte)Sequence;
            buffer[2] = (byte)(Sequence >> 8);
            buffer[3] = (byte)State.Mode;
            buffer[4] = (byte)((State.IsHeld ? HeldFlag : 0) | (State.IsTriggerHeld ? TriggerFlag : 0));
            WriteSingle(buffer, 5, State.Radius);
            WriteSingle(buffer, 9, State.Extension);
            return buffer;
        }

        public static bool TryParse(byte[] buffer, out PenStateMessage message)
        {
            message = default;
            if (buffer == null || buffer.Length != Size || buffer[0] != FormatVersion)
            {
                return false;
            }
            var mode = (PenMode)buffer[3];
            if (!Enum.IsDefined(typeof(PenMode), mode))
            {
                return false;
            }
            float radius = ReadSingle(buffer, 5);
            float extension = ReadSingle(buffer, 9);
            if (!IsFinite(radius) || !IsFinite(extension))
            {
                return false;
            }
            byte flags = buffer[4];
            message = new PenStateMessage((ushort)(buffer[1] | (buffer[2] << 8)), new PenState
            {
                Mode = mode,
                Radius = PenGeometry.ClampRadius(radius),
                Extension = Math.Clamp(extension, 0f, 1f),
                IsHeld = (flags & HeldFlag) != 0,
                IsTriggerHeld = (flags & TriggerFlag) != 0,
            });
            return true;
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static void WriteSingle(byte[] buffer, int offset, float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            buffer[offset] = (byte)bits;
            buffer[offset + 1] = (byte)(bits >> 8);
            buffer[offset + 2] = (byte)(bits >> 16);
            buffer[offset + 3] = (byte)(bits >> 24);
        }

        static float ReadSingle(byte[] buffer, int offset) => BitConverter.Int32BitsToSingle(
            buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24));
    }
}
