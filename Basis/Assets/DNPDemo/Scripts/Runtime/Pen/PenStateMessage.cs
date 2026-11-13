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
        public const byte FormatVersion = 2;

        // Version, sequence (2), mode, flags, then the radius as a 32-bit float.
        public const int Size = 5 + sizeof(float);

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
            int bits = BitConverter.SingleToInt32Bits(State.Radius);
            buffer[5] = (byte)bits;
            buffer[6] = (byte)(bits >> 8);
            buffer[7] = (byte)(bits >> 16);
            buffer[8] = (byte)(bits >> 24);
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
            float radius = BitConverter.Int32BitsToSingle(buffer[5] | (buffer[6] << 8) | (buffer[7] << 16) | (buffer[8] << 24));
            if (float.IsNaN(radius) || float.IsInfinity(radius))
            {
                return false;
            }
            byte flags = buffer[4];
            message = new PenStateMessage((ushort)(buffer[1] | (buffer[2] << 8)), new PenState
            {
                Mode = mode,
                Radius = PenGeometry.ClampRadius(radius),
                IsHeld = (flags & HeldFlag) != 0,
                IsTriggerHeld = (flags & TriggerFlag) != 0,
            });
            return true;
        }
    }
}
