using System.Runtime.CompilerServices;

namespace AKNet.Platform
{
    internal unsafe class CX_PLATFORM
    {
        public IntPtr Heap;
        public int dwBuildNumber;
#if DEBUG
        public int AllocFailDenominator;
        public long AllocCounter;
#endif
    }

    internal enum CXPLAT_THREAD_FLAGS
    {
        CXPLAT_THREAD_FLAG_NONE = 0x0000,
        CXPLAT_THREAD_FLAG_SET_IDEAL_PROC = 0x0001,
        CXPLAT_THREAD_FLAG_SET_AFFINITIZE = 0x0002,
        CXPLAT_THREAD_FLAG_HIGH_PRIORITY = 0x0004
    }

    public static unsafe partial class OSPlatformFunc
    {
        static readonly CX_PLATFORM CxPlatform = new CX_PLATFORM();

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
    }
}
