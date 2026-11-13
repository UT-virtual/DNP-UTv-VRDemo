using System;
using System.Collections.Generic;
using UnityEngine;
using static DNPDemo.Interaction.PayloadCodec;

namespace DNPDemo.Interaction.Stroke
{
    public enum StrokeMessageKind : byte
    {
        /// <summary>Points of a line being drawn, from a given index on. The first one creates the line.</summary>
        Append = 1,
        /// <summary>A line was finished (the trigger was released).</summary>
        End = 2,
        /// <summary>A line was removed, by the eraser or because a guide line failed (then it fades out).</summary>
        Erase = 3,
        /// <summary>Every line of the part, for a device that entered late. Replaces what it had.</summary>
        Snapshot = 4,
    }

    /// <summary>What a received message changed, so the view can follow.</summary>
    public readonly struct StrokeUpdate
    {
        public StrokeUpdate(StrokeMessageKind kind, ushort lineId, bool fade)
        {
            Kind = kind;
            LineId = lineId;
            Fade = fade;
        }

        public StrokeMessageKind Kind { get; }
        public ushort LineId { get; }

        /// <summary>Erase only: the line should fade out rather than vanish.</summary>
        public bool Fade { get; }
    }

    /// <summary>
    /// Payload of <see cref="StrokeTool"/> progress (inside an <see cref="InteractionMessage"/>). Every message starts
    /// with the format version and a <see cref="StrokeMessageKind"/>; then:
    /// <list type="bullet">
    /// <item>Append: line id (ushort), index of the first point (ushort), point count (ushort), origin (3 floats), and per
    /// point the offset from the origin in millimetres (3 shorts).</item>
    /// <item>End: line id.</item>
    /// <item>Erase: line id, fade flag (byte).</item>
    /// <item>Snapshot: line count (byte), origin (3 floats), then per line its id (ushort), finished flag (byte), point
    /// count (ushort) and points as in Append.</item>
    /// </list>
    /// Points are 6 bytes each, so a full <see cref="StrokeSet"/> is about 24 KB. Offsets beyond ±32.767 m from the
    /// origin are clamped; a drawing never spans that far.
    /// <para>
    /// Messages are sent reliably and in order, so an Append follows on from the previous one; a gap (never expected)
    /// is joined with a straight segment.
    /// </para>
    /// </summary>
    public static class StrokeMessage
    {
        public const byte FormatVersion = 1;

        /// <summary>Resolution of positions after a round trip, in metres.</summary>
        public const float PositionStep = 0.001f;

        public const int BytesPerPoint = 3 * sizeof(short);

        const int KindHeaderSize = 2;
        const int OriginSize = 3 * sizeof(float);
        const int AppendHeaderSize = KindHeaderSize + 3 * sizeof(ushort) + OriginSize;
        const int EndSize = KindHeaderSize + sizeof(ushort);
        const int EraseSize = KindHeaderSize + sizeof(ushort) + 1;
        const int SnapshotHeaderSize = KindHeaderSize + 1 + OriginSize;
        const int SnapshotLineHeaderSize = sizeof(ushort) + 1 + sizeof(ushort);

        /// <summary>The points of <paramref name="line"/> from <paramref name="firstIndex"/> to its end.</summary>
        public static byte[] Append(StrokeLine line, int firstIndex)
        {
            firstIndex = Mathf.Clamp(firstIndex, 0, line.Count);
            int count = line.Count - firstIndex;
            var buffer = new byte[AppendHeaderSize + count * BytesPerPoint];
            Vector3 origin = Center(line.Points, firstIndex, count);
            int offset = WriteHeader(buffer, StrokeMessageKind.Append);
            WriteUInt16(buffer, ref offset, line.Id);
            WriteUInt16(buffer, ref offset, (ushort)firstIndex);
            WriteUInt16(buffer, ref offset, (ushort)count);
            WriteVector3(buffer, ref offset, origin);
            WritePoints(buffer, ref offset, line.Points, firstIndex, count, origin);
            return buffer;
        }

        public static byte[] End(ushort lineId)
        {
            var buffer = new byte[EndSize];
            int offset = WriteHeader(buffer, StrokeMessageKind.End);
            WriteUInt16(buffer, ref offset, lineId);
            return buffer;
        }

        public static byte[] Erase(ushort lineId, bool fade)
        {
            var buffer = new byte[EraseSize];
            int offset = WriteHeader(buffer, StrokeMessageKind.Erase);
            WriteUInt16(buffer, ref offset, lineId);
            WriteByte(buffer, ref offset, fade ? (byte)1 : (byte)0);
            return buffer;
        }

        public static byte[] Snapshot(StrokeSet set)
        {
            IReadOnlyList<StrokeLine> lines = set.Lines;
            int lineCount = Mathf.Min(lines.Count, StrokeSet.MaxLines);
            int size = SnapshotHeaderSize;
            bool any = false;
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;
            for (int i = 0; i < lineCount; i++)
            {
                StrokeLine line = lines[i];
                size += SnapshotLineHeaderSize + line.Count * BytesPerPoint;
                if (line.Count == 0)
                {
                    continue;
                }
                Bounds bounds = line.Bounds;
                min = any ? Vector3.Min(min, bounds.min) : bounds.min;
                max = any ? Vector3.Max(max, bounds.max) : bounds.max;
                any = true;
            }
            Vector3 origin = (min + max) * 0.5f;

            var buffer = new byte[size];
            int offset = WriteHeader(buffer, StrokeMessageKind.Snapshot);
            WriteByte(buffer, ref offset, (byte)lineCount);
            WriteVector3(buffer, ref offset, origin);
            for (int i = 0; i < lineCount; i++)
            {
                StrokeLine line = lines[i];
                WriteUInt16(buffer, ref offset, line.Id);
                WriteByte(buffer, ref offset, line.IsFinished ? (byte)1 : (byte)0);
                WriteUInt16(buffer, ref offset, (ushort)line.Count);
                WritePoints(buffer, ref offset, line.Points, 0, line.Count, origin);
            }
            return buffer;
        }

        /// <summary>
        /// Applies a payload to <paramref name="set"/>. False, with the set untouched, when the payload is malformed.
        /// </summary>
        public static bool TryApply(ArraySegment<byte> payload, StrokeSet set, out StrokeUpdate update)
        {
            update = default;
            byte[] buffer = payload.Array;
            if (buffer == null || payload.Count < KindHeaderSize || buffer[payload.Offset] != FormatVersion)
            {
                return false;
            }
            var kind = (StrokeMessageKind)buffer[payload.Offset + 1];
            int offset = payload.Offset + KindHeaderSize;
            switch (kind)
            {
                case StrokeMessageKind.Append:
                    return TryApplyAppend(buffer, offset, payload.Count, set, out update);
                case StrokeMessageKind.End:
                {
                    if (payload.Count != EndSize)
                    {
                        return false;
                    }
                    ushort id = ReadUInt16(buffer, ref offset);
                    set.Finish(set.Find(id));
                    update = new StrokeUpdate(kind, id, false);
                    return true;
                }
                case StrokeMessageKind.Erase:
                {
                    if (payload.Count != EraseSize)
                    {
                        return false;
                    }
                    ushort id = ReadUInt16(buffer, ref offset);
                    bool fade = ReadByte(buffer, ref offset) != 0;
                    set.Remove(id);
                    update = new StrokeUpdate(kind, id, fade);
                    return true;
                }
                case StrokeMessageKind.Snapshot:
                    return TryApplySnapshot(buffer, offset, payload.Count, set, out update);
                default:
                    return false;
            }
        }

        static bool TryApplyAppend(byte[] buffer, int offset, int length, StrokeSet set, out StrokeUpdate update)
        {
            update = default;
            if (length < AppendHeaderSize)
            {
                return false;
            }
            ushort id = ReadUInt16(buffer, ref offset);
            int firstIndex = ReadUInt16(buffer, ref offset);
            int count = ReadUInt16(buffer, ref offset);
            if (length != AppendHeaderSize + count * BytesPerPoint)
            {
                return false;
            }
            Vector3 origin = ReadVector3(buffer, ref offset);
            if (!IsFinite(origin))
            {
                return false;
            }
            StrokeLine line = set.GetOrCreate(id);
            if (line != null && !line.IsFinished)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 point = ReadPoint(buffer, ref offset, origin);
                    // Points the line already has (a repeat after a snapshot) are skipped.
                    if (firstIndex + i >= line.Count)
                    {
                        set.Append(line, point);
                    }
                }
            }
            update = new StrokeUpdate(StrokeMessageKind.Append, id, false);
            return true;
        }

        static bool TryApplySnapshot(byte[] buffer, int offset, int length, StrokeSet set, out StrokeUpdate update)
        {
            update = default;
            int end = offset - KindHeaderSize + length;
            if (length < SnapshotHeaderSize)
            {
                return false;
            }
            int lineCount = ReadByte(buffer, ref offset);
            Vector3 origin = ReadVector3(buffer, ref offset);
            if (!IsFinite(origin))
            {
                return false;
            }
            // Check the whole layout before touching the set.
            int scan = offset;
            for (int i = 0; i < lineCount; i++)
            {
                if (scan + SnapshotLineHeaderSize > end)
                {
                    return false;
                }
                int count = buffer[scan + 3] | (buffer[scan + 4] << 8);
                scan += SnapshotLineHeaderSize + count * BytesPerPoint;
            }
            if (scan != end)
            {
                return false;
            }

            set.Clear();
            for (int i = 0; i < lineCount; i++)
            {
                ushort id = ReadUInt16(buffer, ref offset);
                bool finished = ReadByte(buffer, ref offset) != 0;
                int count = ReadUInt16(buffer, ref offset);
                StrokeLine line = set.GetOrCreate(id);
                for (int p = 0; p < count; p++)
                {
                    Vector3 point = ReadPoint(buffer, ref offset, origin);
                    set.Append(line, point);
                }
                if (finished)
                {
                    set.Finish(line);
                }
            }
            update = new StrokeUpdate(StrokeMessageKind.Snapshot, 0, false);
            return true;
        }

        static int WriteHeader(byte[] buffer, StrokeMessageKind kind)
        {
            buffer[0] = FormatVersion;
            buffer[1] = (byte)kind;
            return KindHeaderSize;
        }

        static void WritePoints(byte[] buffer, ref int offset, IReadOnlyList<Vector3> points, int start, int count, Vector3 origin)
        {
            for (int i = start; i < start + count; i++)
            {
                Vector3 local = points[i] - origin;
                WriteInt16(buffer, ref offset, Quantize(local.x, PositionStep, short.MinValue, short.MaxValue));
                WriteInt16(buffer, ref offset, Quantize(local.y, PositionStep, short.MinValue, short.MaxValue));
                WriteInt16(buffer, ref offset, Quantize(local.z, PositionStep, short.MinValue, short.MaxValue));
            }
        }

        static Vector3 ReadPoint(byte[] buffer, ref int offset, Vector3 origin) =>
            origin + new Vector3(
                ReadInt16(buffer, ref offset) * PositionStep,
                ReadInt16(buffer, ref offset) * PositionStep,
                ReadInt16(buffer, ref offset) * PositionStep);

        static Vector3 Center(IReadOnlyList<Vector3> points, int start, int count)
        {
            if (count <= 0)
            {
                return Vector3.zero;
            }
            Vector3 min = points[start];
            Vector3 max = min;
            for (int i = start + 1; i < start + count; i++)
            {
                min = Vector3.Min(min, points[i]);
                max = Vector3.Max(max, points[i]);
            }
            return (min + max) * 0.5f;
        }
    }
}
