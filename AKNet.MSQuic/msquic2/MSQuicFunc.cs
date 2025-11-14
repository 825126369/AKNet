/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Runtime.CompilerServices;

namespace MSQuic2
{
    internal static partial class MSQuicFunc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool BoolOk(long q)
        {
            return q != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool BoolOk(ulong q)
        {
            return q != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFlag(ref uint Flags, uint flag, bool bEnable)
        {
            ulong Flags2 = Flags;
            SetFlag(ref Flags2, flag, bEnable);
            Flags = (uint)Flags2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFlag(ref ulong Flags, ulong flag, bool bEnable)
        {
            if (bEnable)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlag(ulong Flags, ulong flag)
        {
            return BoolOk(Flags & flag);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BIT(int nr)
        {
            return (ulong)(1 << nr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orBufferEqual(QUIC_SSBuffer buffer1, QUIC_SSBuffer buffer2)
        {
            return orBufferEqual(buffer1.GetSpan(), buffer2.GetSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orBufferEqual(ReadOnlySpan<byte> buffer1, ReadOnlySpan<byte> buffer2)
        {
            return buffer1.SequenceEqual(buffer2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orBufferEqual(byte[] buffer1, byte[] buffer2, int nLength)
        {
            return BufferTool.orBufferEqual(buffer1, 0, buffer2, 0, nLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orBufferEqual(byte[] buffer1, int Offset1, byte[] buffer2,  int nOffset2, int nLength)
        {
            return BufferTool.orBufferEqual(buffer1, Offset1, buffer2, nOffset2, nLength);
        }

    }
}
