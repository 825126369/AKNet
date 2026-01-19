using System;
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    internal static class CommonFunc
    {
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
    }
}
