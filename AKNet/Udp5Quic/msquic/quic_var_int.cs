using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static int QuicVarIntSize(ulong Value)
        {
            return Value < 0x40 ? sizeof(byte) : (Value < 0x4000 ? sizeof(ushort) : (Value < 0x40000000 ? sizeof(uint) : sizeof(ulong)));
        }

        static Span<byte> QuicVarIntEncode(ulong Value, Span<byte> Buffer)
        {
            NetLog.Assert(Value <= QUIC_VAR_INT_MAX);
            if (Value < 0x40)
            {
                Buffer[0] = (byte)Value;
                return Buffer.Slice(1);
            }
            else if (Value < 0x4000)
            {
                ushort tmp = (ushort)((0x40 << 8) | (ushort)Value);
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(2);
            }
            else if (Value < 0x40000000)
            {
                uint tmp = (uint)((0x80 << 24) | (uint)Value);
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(4);
            }
            else
            {
                ulong tmp = ((ulong)0xc0 << 56) | Value;
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(8);
            }
        }

        static Span<byte> QuicVarIntEncode2Bytes(ulong Value, Span<byte> Buffer)
        {
            NetLog.Assert(Value < 0x4000);
            ushort tmp = (ushort)((0x40 << 8) | (ushort)Value);
            EndianBitConverter.SetBytes(Buffer, 0, tmp);
            return Buffer.Slice(8);
        }

        static bool QuicVarIntDecode(ref ReadOnlySpan<byte> Buffer, ref int Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (int)value2;
            return result;
        }

        static bool QuicVarIntDecode(ref ReadOnlySpan<byte> Buffer, ref long Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (long)value2;
            return result;
        }

        static bool QuicVarIntDecode(ref ReadOnlySpan<byte> Buffer, ref ulong Value)
        {
            if (Buffer.Length < sizeof(byte))
            {
                return false;
            }

            if (Buffer[0] < 0x40)
            {
                Value = Buffer[0];
                NetLog.Assert(Value < 0x100UL);
                Buffer = Buffer.Slice(sizeof(byte));
            }
            else if (Buffer[0] < 0x80)
            {
                if (Buffer.Length < 2)
                {
                    return false;
                }

                Value = ((ulong)(Buffer[0] & 0x3f)) << 8;
                Value |= Buffer[1];
                NetLog.Assert(Value < 0x10000UL);
                Buffer = Buffer.Slice(sizeof(ushort));
            }
            else if (Buffer[0] < 0xc0)
            {
                if (Buffer.Length < sizeof(uint))
                {
                    return false;
                }
                uint v = EndianBitConverter.ToUInt32(Buffer);
                Value = CxPlatByteSwapUint32(v) & 0x3fffffffUL;
                NetLog.Assert(Value < 0x100000000);
                Buffer = Buffer.Slice(sizeof(uint));
            }
            else
            {
                if (Buffer.Length < sizeof(ulong))
                {
                    return false;
                }

                ulong v = EndianBitConverter.ToUInt64(Buffer);
                Value = CxPlatByteSwapUint64(v) & 0x3fffffffffffffffUL;
                Buffer = Buffer.Slice(sizeof(ulong));
            }
            return true;
        }




        static bool QuicVarIntDecode(int BufferLength, byte[] Buffer, ref int Offset, ref long Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref value2);
            Value = (long)value2;
            return result;
        }

        static bool QuicVarIntDecode(int BufferLength, byte[] Buffer, ref int Offset, ref int Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref value2);
            Value = (int)value2;
            return result;
        }

        static bool QuicVarIntDecode(int BufferLength, byte[] Buffer, ref int Offset, ref ulong Value)
        {
            if (BufferLength < sizeof(byte) + Offset)
            {
                return false;
            }
            if (Buffer[Offset] < 0x40)
            {
                Value = Buffer[Offset];
                NetLog.Assert(Value < 0x100UL);
                Offset += sizeof(byte);
            }
            else if (Buffer[Offset] < 0x80)
            {
                if (BufferLength < sizeof(ushort) + Offset)
                {
                    return false;
                }

                Value = (ulong)(Buffer[Offset] & 0x3f) << 8;
                Value |= Buffer[Offset + 1];
                NetLog.Assert(Value < 0x10000UL);
                Offset += sizeof(ushort);
            }
            else if (Buffer[Offset] < 0xc0)
            {
                if (BufferLength < sizeof(uint) + Offset)
                {
                    return false;
                }

                uint v = EndianBitConverter.ToUInt32(Buffer, Offset);
                Value = CxPlatByteSwapUint32(v) & 0x3fffffffUL;
                NetLog.Assert(Value < 0x100000000UL);
                Offset += sizeof(uint);
            }
            else
            {
                if (BufferLength < sizeof(ulong) + Offset)
                {
                    return false;
                }
                ulong v = EndianBitConverter.ToUInt64(Buffer, Offset);
                Value = CxPlatByteSwapUint64(v) & 0x3fffffffffffffffUL;
                Offset += sizeof(ulong);
            }
            return true;
        }

    }
}
