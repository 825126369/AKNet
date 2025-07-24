#if TARGET_WINDOWS
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public int dwLength;
        public int dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
        

    public static unsafe partial class OSPlatformFunc
    {
        public static void* CxPlatAlloc(int ByteCount, uint Tag = 0)
        {
            return Interop.Kernel32.HeapAlloc(CxPlatform.Heap, 0, ByteCount);
        }

        public static void CxPlatFree(void* Mem, uint Tag = 0)
        {
            Interop.Kernel32.HeapFree(CxPlatform.Heap, 0, Mem);
        }

        public static void CxPlatZeroMemory(void* Destination, int Length)
        {
            Interop.Ucrtbase.memset(Destination, 0, Length);
        }

        public static int memcmp(void* s1, void* s2, int n)
        {
             return Interop.Ucrtbase.memcmp(s1, s2, n);
        }

        public static int CxPlatRandom(int BufferLen, void* Buffer)
        {
            const int BCRYPT_RNG_USE_ENTROPY_IN_BUFFER = 0x00000001;
            const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

            return (int)Interop.BCrypt.BCryptGenRandom(
                    IntPtr.Zero,
                    (byte*)Buffer,
                    BufferLen,
                    BCRYPT_USE_SYSTEM_PREFERRED_RNG);
        }

        //得到内存状态
        public static MEMORYSTATUSEX GlobalMemoryStatusEx()
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            int structSize = Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            memStatus.dwLength = (int)structSize;
            Interop.Kernel32.GlobalMemoryStatusEx(&memStatus);
            return memStatus;
        }

        //得到系统时钟间隔
        public static long GetSystemTimeAdjustment()
        {
            const uint NS_100_PER_MICROSECOND = 10; // 1 μs = 10 * 100ns
            int Adjustment, Increment;
            bool AdjustmentDisabled;
            if (Interop.Kernel32.GetSystemTimeAdjustment(out Adjustment, out Increment, out AdjustmentDisabled))
            {
                return Increment / NS_100_PER_MICROSECOND;
            }
            return 1;
        }
    }
}
#endif