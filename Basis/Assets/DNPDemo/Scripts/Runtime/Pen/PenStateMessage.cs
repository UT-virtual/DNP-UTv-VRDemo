using System;
using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Wire format of <see cref="SpatialPenSync"/>: one message per state, numbered so that a late packet never
    /// overwrites a newer state (states travel on both an unreliable and a reliable channel).
    /// The sender is identified by the network, not by the payload, so a device can only move its own pen.
    /// </summary>
    public readonly struct PenStateMessage
    {
        public const byte FormatVersion = 1;
        // Version, sequence (2), mode, flags, then radius, position (3) and rotation (4) as 32-bit floats.
        public const int Size = 5 + 8 * sizeof(float);

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
            int offset = 0;
            buffer[offset++] = FormatVersion;
            buffer[offset++] = (byte)Sequence;
            buffer[offset++] = (byte)(Sequence >> 8);
            buffer[offset++] = (byte)State.Mode;
            buffer[offset++] = (byte)((State.IsHeld ? HeldFlag : 0) | (State.IsTriggerHeld ? TriggerFlag : 0));
            WriteFloat(buffer, ref offset, State.Radius);
            WriteFloat(buffer, ref offset, State.Position.x);
            WriteFloat(buffer, ref offset, State.Position.y);
            WriteFloat(buffer, ref offset, State.Position.z);
            Quaternion rotation = State.Rotation;
            WriteFloat(buffer, ref offset, rotation.x);
            WriteFloat(buffer, ref offset, rotation.y);
            WriteFloat(buffer, ref offset, rotation.z);
            WriteFloat(buffer, ref offset, rotation.w);
            return buffer;
        }

        public static bool TryParse(byte[] buffer, out PenStateMessage message)
        {
            message = default;
            if (buffer == null || buffer.Length != Size || buffer[0] != FormatVersion)
            {
                return false;
            }
            int offset = 1;
            var sequence = (ushort)(buffer[offset++] | (buffer[offset++] << 8));
            var mode = (PenMode)buffer[offset++];
            if (!Enum.IsDefined(typeof(PenMode), mode))
            {
                return false;
            }
            byte flags = buffer[offset++];
            float radius = ReadFloat(buffer, ref offset);
            var position = new Vector3(ReadFloat(buffer, ref offset), ReadFloat(buffer, ref offset), ReadFloat(buffer, ref offset));
            var rotation = new Quaternion(ReadFloat(buffer, ref offset), ReadFloat(buffer, ref offset), ReadFloat(buffer, ref offset), ReadFloat(buffer, ref offset));
            if (!IsFinite(radius) || !IsFinite(position) || !IsUsableRotation(rotation))
            {
                return false;
            }
            message = new PenStateMessage(sequence, new PenState
            {
                Mode = mode,
                Radius = PenGeometry.ClampRadius(radius),
                IsHeld = (flags & HeldFlag) != 0,
                IsTriggerHeld = (flags & TriggerFlag) != 0,
                Position = position,
                Rotation = Quaternion.Normalize(rotation),
            });
            return true;
        }

        static void WriteFloat(byte[] buffer, ref int offset, float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            buffer[offset++] = (byte)bits;
            buffer[offset++] = (byte)(bits >> 8);
            buffer[offset++] = (byte)(bits >> 16);
            buffer[offset++] = (byte)(bits >> 24);
        }

        static float ReadFloat(byte[] buffer, ref int offset)
        {
            int bits = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);
            offset += 4;
            return BitConverter.Int32BitsToSingle(bits);
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        static bool IsUsableRotation(Quaternion value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w)
            && (value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w) > 1e-6f;
    }
}
