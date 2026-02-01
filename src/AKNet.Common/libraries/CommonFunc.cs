/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    internal static class CommonFunc
    {
        [Conditional("DEBUG")]
        public static void AssertWithException(bool bTrue, object tag = null)
        {
            if(!bTrue)
            {
                throw new Exception(tag.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BoolOk(long q)
        {
            return q != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BoolOk(ulong q)
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
            return 1UL << nr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetByteArrayStr(byte[] buffer)
        {
            return string.Join(' ', buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetByteArrayStr(ReadOnlySpan<byte> buffer)
        {
            return string.Join<byte>(' ', buffer.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }
    }
}
