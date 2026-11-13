using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>
    /// Payload of <see cref="SelectionTool"/> progress (inside an <see cref="InteractionMessage"/>): a confirmed
    /// selection as its list of spheres.
    /// <para>
    /// Layout: format version, sample count (ushort), the origin (3 floats, the centre of the samples' bounds), then per
    /// sample the offset from the origin in millimetres (3 shorts) and the radius in tenths of a millimetre (ushort).
    /// That is 8 bytes a sample, about 4 KB for <see cref="SelectionSampler.DefaultMaxSamples"/>. Offsets beyond
    /// ±32.767 m are clamped; a selection never spans that far.
    /// </para>
    /// </summary>
    public static class SelectionMessage
    {
        public const byte FormatVersion = 1;

        public const int HeaderSize = 1 + sizeof(ushort) + 3 * sizeof(float);
        public const int BytesPerSample = 4 * sizeof(short);

        /// <summary>Most samples one message carries.</summary>
        public const int MaxSamples = ushort.MaxValue;

        /// <summary>Resolution of positions after a round trip, in metres.</summary>
        public const float PositionStep = 0.001f;

        /// <summary>Resolution of radii after a round trip, in metres.</summary>
        public const float RadiusStep = 0.0001f;

        public static byte[] Serialize(IReadOnlyList<SelectionSample> samples)
        {
            int count = Mathf.Min(samples?.Count ?? 0, MaxSamples);
            var buffer = new byte[HeaderSize + count * BytesPerSample];
            Vector3 origin = count > 0 ? BoundsCenter(samples, count) : Vector3.zero;

            int offset = 0;
            buffer[offset++] = FormatVersion;
            WriteUInt16(buffer, ref offset, (ushort)count);
            WriteSingle(buffer, ref offset, origin.x);
            WriteSingle(buffer, ref offset, origin.y);
            WriteSingle(buffer, ref offset, origin.z);
            for (int i = 0; i < count; i++)
            {
                Vector3 local = samples[i].Position - origin;
                WriteInt16(buffer, ref offset, Quantize(local.x, PositionStep, short.MinValue, short.MaxValue));
                WriteInt16(buffer, ref offset, Quantize(local.y, PositionStep, short.MinValue, short.MaxValue));
                WriteInt16(buffer, ref offset, Quantize(local.z, PositionStep, short.MinValue, short.MaxValue));
                WriteUInt16(buffer, ref offset, (ushort)Quantize(samples[i].Radius, RadiusStep, 0, ushort.MaxValue));
            }
            return buffer;
        }

        /// <summary>Reads a payload into <paramref name="samples"/> (cleared first). False when the payload is malformed.</summary>
        public static bool TryParse(ArraySegment<byte> payload, List<SelectionSample> samples)
        {
            samples.Clear();
            byte[] buffer = payload.Array;
            if (buffer == null || payload.Count < HeaderSize || buffer[payload.Offset] != FormatVersion)
            {
                return false;
            }
            int offset = payload.Offset + 1;
            int count = ReadUInt16(buffer, ref offset);
            if (payload.Count != HeaderSize + count * BytesPerSample)
            {
                return false;
            }
            var origin = new Vector3(ReadSingle(buffer, ref offset), ReadSingle(buffer, ref offset), ReadSingle(buffer, ref offset));
            if (!IsFinite(origin))
            {
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                var local = new Vector3(
                    ReadInt16(buffer, ref offset) * PositionStep,
                    ReadInt16(buffer, ref offset) * PositionStep,
                    ReadInt16(buffer, ref offset) * PositionStep);
                float radius = ReadUInt16(buffer, ref offset) * RadiusStep;
                samples.Add(new SelectionSample(origin + local, radius));
            }
            return true;
        }

        static Vector3 BoundsCenter(IReadOnlyList<SelectionSample> samples, int count)
        {
            Vector3 min = samples[0].Position;
            Vector3 max = min;
            for (int i = 1; i < count; i++)
            {
                min = Vector3.Min(min, samples[i].Position);
                max = Vector3.Max(max, samples[i].Position);
            }
            return (min + max) * 0.5f;
        }

        static int Quantize(float value, float step, int min, int max) =>
            Mathf.Clamp(Mathf.RoundToInt(value / step), min, max);

        static bool IsFinite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)
            && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);

        static void WriteUInt16(byte[] buffer, ref int offset, ushort value)
        {
            buffer[offset++] = (byte)value;
            buffer[offset++] = (byte)(value >> 8);
        }

        static void WriteInt16(byte[] buffer, ref int offset, int value) => WriteUInt16(buffer, ref offset, (ushort)(short)value);

        static void WriteSingle(byte[] buffer, ref int offset, float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            buffer[offset++] = (byte)bits;
            buffer[offset++] = (byte)(bits >> 8);
            buffer[offset++] = (byte)(bits >> 16);
            buffer[offset++] = (byte)(bits >> 24);
        }

        static ushort ReadUInt16(byte[] buffer, ref int offset)
        {
            int value = buffer[offset] | (buffer[offset + 1] << 8);
            offset += 2;
            return (ushort)value;
        }

        static short ReadInt16(byte[] buffer, ref int offset) => (short)ReadUInt16(buffer, ref offset);

        static float ReadSingle(byte[] buffer, ref int offset)
        {
            int bits = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);
            offset += 4;
            return BitConverter.Int32BitsToSingle(bits);
        }
    }
}
