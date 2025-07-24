using AKNet.Common;
using System;
using System.Runtime.CompilerServices;

namespace AKNet.Udp1MSQuic.Common
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

        public static bool orBufferEqual(QUIC_SSBuffer buffer1, QUIC_SSBuffer buffer2)
        {
            return orBufferEqual(buffer1.GetSpan(), buffer2.GetSpan());
        }

        public static bool orBufferEqual(ReadOnlySpan<byte> buffer1, ReadOnlySpan<byte> buffer2)
        {
            return BufferTool.orBufferEqual(buffer1, buffer2);
        }

        public static bool orBufferEqual(byte[] buffer1, byte[] buffer2, int nLength)
        {
            return BufferTool.orBufferEqual(buffer1, 0, buffer2, 0, nLength);
        }

        public static bool orBufferEqual(byte[] buffer1, int Offset1, byte[] buffer2,  int nOffset2, int nLength)
        {
            return BufferTool.orBufferEqual(buffer1, Offset1, buffer2, nOffset2, nLength);
        }

    }
}
