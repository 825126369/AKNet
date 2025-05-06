using AKNet.Common;
using System;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        static int QuicVarIntSize(ulong Value)
        {
            return Value < 0x40 ? sizeof(byte) : (Value < 0x4000 ? sizeof(ushort) : (Value < 0x40000000 ? sizeof(uint) : sizeof(ulong)));
        }

        static int QuicVarIntSize(int Value)
        {
            return QuicVarIntSize((ulong)Value);
        }

        static int QuicVarIntSize(long Value)
        {
            return QuicVarIntSize((ulong)Value);
        }

        static QUIC_SSBuffer QuicVarIntEncode(int Value, QUIC_SSBuffer Buffer)
        {
            return QuicVarIntEncode((ulong)Value, Buffer);
        }


        static QUIC_SSBuffer QuicVarIntEncode(uint Value, QUIC_SSBuffer Buffer)
        {
            return QuicVarIntEncode((ulong)Value, Buffer);
        }

        static QUIC_SSBuffer QuicVarIntEncode(long Value, QUIC_SSBuffer Buffer)
        {
            return QuicVarIntEncode((ulong)Value, Buffer);
        }

        static QUIC_SSBuffer QuicVarIntEncode(ulong Value, QUIC_SSBuffer Buffer)
        {
            NetLog.Assert(Value <= QUIC_VAR_INT_MAX);
            if (Value < 0x40)
            {
                Buffer[0] = (byte)Value;
                return Buffer + sizeof(byte);
            }
            else if (Value < 0x4000)
            {
                ushort tmp = (ushort)((0x40 << 8) | (ushort)Value);
                EndianBitConverter.SetBytes(Buffer.Buffer, 0, tmp);
                return Buffer + sizeof(ushort);
            }
            else if (Value < 0x40000000)
            {
                uint tmp = (uint)((0x80 << 24) | (uint)Value);
                EndianBitConverter.SetBytes(Buffer.Buffer, 0, tmp);
                return Buffer + sizeof(uint);
            }
            else
            {
                ulong tmp = ((ulong)0xc0 << 56) | Value;
                EndianBitConverter.SetBytes(Buffer.Buffer, 0, tmp);
                return Buffer + sizeof(ulong);
            }
        }



        static QUIC_SSBuffer QuicVarIntEncode2Bytes(ulong Value, QUIC_SSBuffer Buffer)
        {
            NetLog.Assert(Value < 0x4000);
            ushort tmp = (ushort)((0x40 << 8) | (ushort)Value);
            EndianBitConverter.SetBytes(Buffer.GetSpan(), 0, tmp);
            return Buffer.Slice(8);
        }

        static bool QuicVarIntDecode2(QUIC_SSBuffer Buffer, ref byte Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (byte)value2;
            return result;
        }

        static bool QuicVarIntDecode2(QUIC_SSBuffer Buffer, ref int Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (int)value2;
            return result;
        }

        static bool QuicVarIntDecode2(QUIC_SSBuffer Buffer, ref long Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (long)value2;
            return result;
        }

        static bool QuicVarIntDecode2(QUIC_SSBuffer Buffer, ref ulong value)
        {
            return QuicVarIntDecode(ref Buffer, ref value);
        }

        static bool QuicVarIntDecode(ref QUIC_SSBuffer Buffer, ref byte Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (byte)value2;
            return result;
        }

        static bool QuicVarIntDecode(ref QUIC_SSBuffer Buffer, ref int Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (int)value2;
            return result;
        }

        static bool QuicVarIntDecode(ref QUIC_SSBuffer Buffer, ref uint Value)
        {
            ulong value2 = Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (uint)value2;
            return result;
        }

        static bool QuicVarIntDecode(ref QUIC_SSBuffer Buffer, ref long Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (long)value2;
            return result;
        }

        static bool QuicVarIntDecode(ref QUIC_SSBuffer Buffer, ref ulong Value)
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
                uint v = EndianBitConverter.ToUInt32(Buffer.GetSpan());
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

                ulong v = EndianBitConverter.ToUInt64(Buffer.GetSpan());
                Value = CxPlatByteSwapUint64(v) & 0x3fffffffffffffffUL;
                Buffer = Buffer.Slice(sizeof(ulong));
            }
            return true;
        }
    }
}
