using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static byte QuicVarIntSize(ulong Value)
        {
            var t = Value < 0x40 ? sizeof(byte) : (Value < 0x4000 ? sizeof(ushort) : (Value < 0x40000000 ? sizeof(uint) : sizeof(ulong)));
            return (byte)t;
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
            ushort tmp = CxPlatByteSwapUint16((ushort)((0x40 << 8) | (ushort)Value));
            EndianBitConverter.SetBytes(Buffer, 0, tmp);
            return Buffer.Slice(8);
        }

        static bool QuicVarIntDecode(ref Span<byte> Buffer, ref ulong Value)
        {
            if (Buffer.Length < sizeof(byte))
            {
                return false;
            }

            if (Buffer[0] < 0x40)
            {
                Value = Buffer[0];
                NetLog.Assert(Value < 0x100);
                Buffer  = Buffer.Slice(sizeof(byte));
            }
            else if (Buffer[0] < 0x80)
            {
                if (Buffer.Length < 2)
                {
                    return false;
                }

                Value = ((ulong)(Buffer[0] & 0x3f)) << 8;
                Value |= Buffer[1];
                NetLog.Assert(Value < 0x10000);
                Buffer = Buffer.Slice(sizeof(ushort));
            }
            else if (Buffer[0] < 0xc0)
            {
                if (Buffer.Length < sizeof(uint))
                {
                    return false;
                }
                uint v = EndianBitConverter.ToUInt32(Buffer);
                Value = CxPlatByteSwapUint32(v) & 0x3fffffff;
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
                Value = CxPlatByteSwapUint64(v) & 0x3fffffffffffffff;
                Buffer = Buffer.Slice(sizeof(ulong));
            }
            return true;
        }
    }
}
