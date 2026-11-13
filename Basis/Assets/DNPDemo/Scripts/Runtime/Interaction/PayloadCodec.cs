using System;
using UnityEngine;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// Little-endian reading and writing shared by the tools' progress payloads (<see cref="Selection.SelectionMessage"/>,
    /// <see cref="Stroke.StrokeMessage"/>). Callers check lengths before reading.
    /// </summary>
    internal static class PayloadCodec
    {
        public static void WriteByte(byte[] buffer, ref int offset, byte value) => buffer[offset++] = value;

        public static void WriteUInt16(byte[] buffer, ref int offset, ushort value)
        {
            buffer[offset++] = (byte)value;
            buffer[offset++] = (byte)(value >> 8);
        }

        public static void WriteInt16(byte[] buffer, ref int offset, int value) => WriteUInt16(buffer, ref offset, (ushort)(short)value);

        public static void WriteSingle(byte[] buffer, ref int offset, float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            buffer[offset++] = (byte)bits;
            buffer[offset++] = (byte)(bits >> 8);
            buffer[offset++] = (byte)(bits >> 16);
            buffer[offset++] = (byte)(bits >> 24);
        }

        public static void WriteVector3(byte[] buffer, ref int offset, Vector3 value)
        {
            WriteSingle(buffer, ref offset, value.x);
            WriteSingle(buffer, ref offset, value.y);
            WriteSingle(buffer, ref offset, value.z);
        }

        public static byte ReadByte(byte[] buffer, ref int offset) => buffer[offset++];

        public static ushort ReadUInt16(byte[] buffer, ref int offset)
        {
            int value = buffer[offset] | (buffer[offset + 1] << 8);
            offset += 2;
            return (ushort)value;
        }

        public static short ReadInt16(byte[] buffer, ref int offset) => (short)ReadUInt16(buffer, ref offset);

        public static float ReadSingle(byte[] buffer, ref int offset)
        {
            int bits = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);
            offset += 4;
            return BitConverter.Int32BitsToSingle(bits);
        }

        public static Vector3 ReadVector3(byte[] buffer, ref int offset) =>
            new(ReadSingle(buffer, ref offset), ReadSingle(buffer, ref offset), ReadSingle(buffer, ref offset));

        public static int Quantize(float value, float step, int min, int max) =>
            Mathf.Clamp(Mathf.RoundToInt(value / step), min, max);

        public static bool IsFinite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)
            && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }
}
