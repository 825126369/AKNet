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
using System;

namespace MSQuic1
{
    internal static partial class MSQuicFunc
    {
        public static Span<byte> QuicVarIntEncode(int Value, Span<byte> Buffer)
        {
            return QuicVarIntEncode((ulong)Value, Buffer);
        }

        public static Span<byte> QuicVarIntEncode(uint Value, Span<byte> Buffer)
        {
            return QuicVarIntEncode((ulong)Value, Buffer);
        }

        public static Span<byte> QuicVarIntEncode(long Value, Span<byte> Buffer)
        {
            return QuicVarIntEncode((ulong)Value, Buffer);
        }

        public static Span<byte> QuicVarIntEncode2Bytes(ulong Value, Span<byte> Buffer)
        {
            NetLog.Assert(Value < 0x4000);
            ushort tmp = (ushort)((0x40 << 8) | (ushort)Value);
            EndianBitConverter.SetBytes(Buffer, 0, tmp);
            return Buffer.Slice(sizeof(ushort));
        }

        public static bool QuicVarIntDecode(ref ReadOnlySpan<byte> Buffer, ref byte Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (byte)value2;
            return result;
        }

        public static bool QuicVarIntDecode(ref ReadOnlySpan<byte> Buffer, ref int Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (int)value2;
            return result;
        }

        public static bool QuicVarIntDecode(ref ReadOnlySpan<byte> Buffer, ref uint Value)
        {
            ulong value2 = Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (uint)value2;
            return result;
        }

        public static bool QuicVarIntDecode(ref ReadOnlySpan<byte> Buffer, ref long Value)
        {
            ulong value2 = (ulong)Value;
            bool result = QuicVarIntDecode(ref Buffer, ref value2);
            Value = (long)value2;
            return result;
        }

        public static Span<byte> QuicVarIntEncode(ulong Value, Span<byte> Buffer)
        {
            CommonFunc.AssertWithException(Value <= QUIC_VAR_INT_MAX, Value);
            if (Value < 0x40)
            {
                Buffer[0] = (byte)Value;
                return Buffer.Slice(sizeof(byte));
            }
            else if (Value < 0x4000)
            {
                ushort tmp = (ushort)((0x40 << 8) | (ushort)Value);
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(sizeof(ushort));
            }
            else if (Value < 0x40000000)
            {
                uint tmp = (uint)((0x80 << 24) | (uint)Value);
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(sizeof(uint));
            }
            else
            {
                ulong tmp = ((ulong)0xc0 << 56) | Value;
                EndianBitConverter.SetBytes(Buffer, 0, tmp);
                return Buffer.Slice(sizeof(ulong));
            }
        }

        public static bool QuicVarIntDecode(ref ReadOnlySpan<byte> Buffer, ref ulong Value)
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
                if (Buffer.Length < sizeof(ushort))
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
                Value = v & 0x3fffffffUL;
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
                Value = v & 0x3fffffffffffffffUL;
                Buffer = Buffer.Slice(sizeof(ulong));
            }
            return true;
        }
    }
}
