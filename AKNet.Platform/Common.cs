using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        public static long CxPlatTotalMemory;
        static readonly CX_PLATFORM CxPlatform = new CX_PLATFORM();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool STATUS_FAILED(int Status)
        {
            return Status > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool STATUS_SUCCEEDED(int Status)
        {
            return Status <= 0;
        }

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
        private static void SetFlag(ref ulong Flags, ulong flag, bool bEnable)
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
        private static bool HasFlag(ulong Flags, ulong flag)
        {
            return BoolOk(Flags & flag);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong BIT(int nr)
        {
            return (ulong)(1 << nr);
        }

        private static T* CXPLAT_CONTAINING_RECORD<T>(void* address, string fieldName) where T : struct
        {
             IntPtr offset = Marshal.OffsetOf(typeof(T), fieldName);
             return (T*) ((byte*) address - offset.ToInt32());
        }
    }
}
