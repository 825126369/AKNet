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
                ushort tmp = CxPlatByteSwapUint16((0x40 << 8) | (ushort)Value);
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(2);
            }
            else if (Value < 0x40000000)
            {
                uint tmp = CxPlatByteSwapUint32((0x80 << 24) | (uint)Value);
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(4);
            }
            else
            {
                ulong tmp = CxPlatByteSwapUint64((0xc0 << 56) | Value);
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(8);
            }
        }

        static Span<byte> QuicVarIntEncode2Bytes(ulong Value, Span<byte> Buffer)
        {
            NetLog.Assert(Value < 0x4000);
            ushort tmp = CxPlatByteSwapUint16((0x40 << 8) | (ushort)Value);
            EndianBitConverter.SetBytes(Buffer, 0, tmp);
            return Buffer.Slice(8);
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
                NetLog.Assert(Value < 0x100);
                Offset += sizeof(byte);
            }
            else if (Buffer[Offset] < 0x80)
            {
                if (BufferLength < 2 + Offset)
                {
                    return false;
                }

                Value = ((ulong)(Buffer[Offset] & 0x3f)) << 8;
                Value |= Buffer[Offset + 1];
                NetLog.Assert(Value < 0x10000);
                Offset += sizeof(ushort);
            }
            else if (Buffer[Offset] < 0xc0)
            {
                if (BufferLength < sizeof(uint) + Offset)
                {
                    return false;
                }
                uint v = EndianBitConverter.ToUInt32(Buffer, Offset);
                Value = CxPlatByteSwapUint32(v) & 0x3fffffff;
                NetLog.Assert(Value < 0x100000000);
                Offset += sizeof(uint);
            }
            else
            {
                if (BufferLength < sizeof(ulong) + Offset)
                {
                    return false;
                }
                ulong v = EndianBitConverter.ToUInt64(Buffer, Offset);
                Value = CxPlatByteSwapUint64(v) & 0x3fffffffffffffff;
                Offset += sizeof(ulong);
            }
            return true;
        }
    }
}
