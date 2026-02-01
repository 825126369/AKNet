/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:04
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal enum CXPLAT_THREAD_FLAGS
    {
        CXPLAT_THREAD_FLAG_NONE = 0x0000,
        CXPLAT_THREAD_FLAG_SET_IDEAL_PROC = 0x0001,
        CXPLAT_THREAD_FLAG_SET_AFFINITIZE = 0x0002,
        CXPLAT_THREAD_FLAG_HIGH_PRIORITY = 0x0004
    }

    public static unsafe partial class OSPlatformFunc
    {
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
            return 1UL << nr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T* CXPLAT_CONTAINING_RECORD<T>(void* address, string fieldName) where T : struct
        {
             int offset = Marshal.OffsetOf(typeof(T), fieldName).ToInt32();
             return (T*) (((byte*)address) - offset);
        }

        // 内存分配
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* CxPlatAlloc(int ByteCount, uint Tag = 0)
        {
            return (void*)Marshal.AllocHGlobal(ByteCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* CxPlatAllocAndClear(int ByteCount, uint Tag = 0)
        {
            void* ptr = (void*)Marshal.AllocHGlobal(ByteCount);
            CxPlatZeroMemory(ptr, ByteCount);
            return ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CxPlatFree(void* Mem, uint Tag = 0)
        {
            Marshal.FreeHGlobal((IntPtr)Mem);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CxPlatZeroMemory(void* Destination, int Length)
        {
            new Span<byte>(Destination, Length).Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CxPlatCopyMemory(void* s1, void* s2, int n)
        {
            Span<byte> ptr1 = new Span<byte>(s1, n);
            Span<byte> ptr2 = new Span<byte>(s2, n);
            ptr2.CopyTo(ptr1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool memcmp(void* s1, void* s2, int n)
        {
            Span<byte> ptr1 = new Span<byte>(s1, n);
            Span<byte> ptr2 = new Span<byte>(s2, n);
            return ptr1.SequenceEqual(ptr2);
        }
    }
}
