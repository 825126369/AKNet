/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace MSQuic2
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

        public static bool QuicVarIntDecode(ref QUIC_SSBuffer Buffer, ref byte Value)
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

        static QUIC_SSBuffer QuicVarIntEncode2Bytes(ulong Value, QUIC_SSBuffer Buffer)
        {
            NetLog.Assert(Value < 0x4000);
            ushort tmp = (ushort)((0x40 << 8) | (ushort)Value);
            EndianBitConverter.SetBytes(Buffer.GetSpan(), 0, tmp);
            return Buffer + sizeof(ushort);
        }

        static QUIC_SSBuffer QuicVarIntEncode(ulong Value, QUIC_SSBuffer Buffer)
        {
            NetLog.Assert(Value <= QUIC_VAR_INT_MAX, Value);
            if (Value < 0x40) // 64
            {
                Buffer[0] = (byte)Value;
                return Buffer + sizeof(byte);
            }
            else if (Value < 0x4000) //16384, 16KB
            {
                ushort tmp = (ushort)((0x40 << 8) | (ushort)Value);
                EndianBitConverter.SetBytes(Buffer.GetSpan(), 0, tmp);
                return Buffer + sizeof(ushort);
            }
            else if (Value < 0x40000000) //1GB
            {
                uint tmp = (uint)((0x80UL << 24) | (uint)Value);
                EndianBitConverter.SetBytes(Buffer.GetSpan(), 0, tmp);
                return Buffer + sizeof(uint);
            }
            else
            {
                ulong tmp = ((ulong)0xc0UL << 56) | Value;
                EndianBitConverter.SetBytes(Buffer.GetSpan(), 0, tmp);
                return Buffer + sizeof(ulong);
            }
        }

        static bool QuicVarIntDecode(ref QUIC_SSBuffer Buffer, ref ulong Value)
        {
            if (Buffer.Length < sizeof(byte))
            {
                return false;
            }

            if (Buffer[0] < 0x40) // < 64
            {
                Value = Buffer[0];
                NetLog.Assert(Value < 0x100UL);// value < 256
                Buffer += sizeof(byte);
            }
            else if (Buffer[0] < 0x80) //128
            {
                if (Buffer.Length < sizeof(ushort))
                {
                    return false;
                }

                Value = ((ulong)(Buffer[0] & 0x3f)) << 8;
                Value |= Buffer[1];
                NetLog.Assert(Value < 0x10000UL); // 65536 ushort.MaxValue
                Buffer += sizeof(ushort);
            }
            else if (Buffer[0] < 0xc0) // 192
            {
                if (Buffer.Length < sizeof(uint))
                {
                    return false;
                }

                uint v = EndianBitConverter.ToUInt32(Buffer.GetSpan());
                Value = v & 0x3fffffffUL;
                NetLog.Assert(Value < 0x100000000UL); // 4294967295   uint.MaxValue
                Buffer += sizeof(uint);
            }
            else
            {
                if (Buffer.Length < sizeof(ulong))
                {
                    return false;
                }

                ulong v = EndianBitConverter.ToUInt64(Buffer.GetSpan());
                Value = v & 0x3fffffffffffffffUL; // 62 位无符号整数的最大值
                Buffer += (sizeof(ulong));
            }
            return true;
        }

    }
}
