using System;

namespace DNPDemo.Interaction.Manipulate
{
    public enum ManipulateMessageKind : byte
    {
        /// <summary>The object is being dragged. Sent unreliably at a limited rate.</summary>
        Drag = 1,
        /// <summary>The object was let go where it is. Reliable.</summary>
        Release = 2,
        /// <summary>The object was let go at the goal and snaps to it. Reliable.</summary>
        Snap = 3,
        /// <summary>The whole state, for a device that entered the part late. Reliable.</summary>
        Snapshot = 4,
    }

    /// <summary>
    /// Progress payload of <see cref="ManipulateTool"/>: the constraint value (<see cref="ManipulateValue"/>) and whether
    /// the actor is holding the object. Both devices know the part, so only the value travels (20 bytes).
    /// <para>
    /// Drags go unreliably and the rest reliably. The two channels do not keep order with each other, so every message
    /// carries a counter and a receiver drops anything older than what it has applied (<see cref="IsNewer"/>): a drag
    /// that arrives after the release cannot move the object back.
    /// </para>
    /// </summary>
    public readonly struct ManipulateMessage
    {
        public const int Size = 20;

        const byte GrabbingFlag = 1;
        const byte SnappedFlag = 2;

        public readonly ManipulateMessageKind Kind;
        public readonly ushort Counter;
        public readonly bool IsGrabbing;
        public readonly ManipulateValue Value;

        public ManipulateMessage(ManipulateMessageKind kind, ushort counter, bool isGrabbing, ManipulateValue value)
        {
            Kind = kind;
            Counter = counter;
            IsGrabbing = isGrabbing;
            Value = value;
        }

        /// <summary>True when <paramref name="counter"/> was sent after <paramref name="latest"/>, allowing for wrap-around.</summary>
        public static bool IsNewer(ushort counter, ushort latest) => (short)(counter - latest) > 0;

        public byte[] Serialize()
        {
            var buffer = new byte[Size];
            int offset = 0;
            PayloadCodec.WriteByte(buffer, ref offset, (byte)Kind);
            PayloadCodec.WriteUInt16(buffer, ref offset, Counter);
            byte flags = (byte)((IsGrabbing ? GrabbingFlag : 0) | (Value.Snapped ? SnappedFlag : 0));
            PayloadCodec.WriteByte(buffer, ref offset, flags);
            PayloadCodec.WriteVector3(buffer, ref offset, Value.Offset);
            PayloadCodec.WriteSingle(buffer, ref offset, Value.Angle);
            return buffer;
        }

        /// <summary>False when the payload is not a well-formed manipulate message (wrong size, unknown kind, non-finite numbers).</summary>
        public static bool TryParse(ArraySegment<byte> payload, out ManipulateMessage message)
        {
            message = default;
            if (payload.Array == null || payload.Count != Size)
            {
                return false;
            }
            byte[] buffer = payload.Array;
            int offset = payload.Offset;
            var kind = (ManipulateMessageKind)PayloadCodec.ReadByte(buffer, ref offset);
            if (kind < ManipulateMessageKind.Drag || kind > ManipulateMessageKind.Snapshot)
            {
                return false;
            }
            ushort counter = PayloadCodec.ReadUInt16(buffer, ref offset);
            byte flags = PayloadCodec.ReadByte(buffer, ref offset);
            UnityEngine.Vector3 offsetValue = PayloadCodec.ReadVector3(buffer, ref offset);
            float angle = PayloadCodec.ReadSingle(buffer, ref offset);
            if (!PayloadCodec.IsFinite(offsetValue) || float.IsNaN(angle) || float.IsInfinity(angle))
            {
                return false;
            }
            var value = new ManipulateValue
            {
                Offset = offsetValue,
                Angle = angle,
                Snapped = (flags & SnappedFlag) != 0,
            };
            message = new ManipulateMessage(kind, counter, (flags & GrabbingFlag) != 0, value);
            return true;
        }
    }
}
