using System;
using System.Runtime.InteropServices;

namespace AKNet.Common
{
    public unsafe static partial class AKNetSystemInfo
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }
        
        [DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ___GlobalMemoryStatusEx(IntPtr lpBuffer);
        [DllImport("kernel32.dll", EntryPoint = "GetSystemTimeAdjustment", SetLastError = true)]
        private static extern bool ___GetSystemTimeAdjustment(out int lpTimeAdjustment, out int lpTimeIncrement,out bool lpTimeAdjustmentDisabled);

        //得到内存状态
        private static MEMORYSTATUSEX GlobalMemoryStatusEx()
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            int structSize = Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            memStatus.dwLength = (uint)structSize;

            IntPtr memStatusPtr = Marshal.AllocHGlobal(structSize);
            Marshal.StructureToPtr(memStatus, memStatusPtr, false);
            ___GlobalMemoryStatusEx(memStatusPtr);
            return Marshal.PtrToStructure<MEMORYSTATUSEX>(memStatusPtr);
        }

        //得到系统时钟间隔
        private static long GetSystemTimeAdjustment()
        {
            const uint NS_100_PER_MICROSECOND = 10; // 1 μs = 10 * 100ns
            int Adjustment, Increment;
            bool AdjustmentDisabled;
            if (___GetSystemTimeAdjustment(out Adjustment, out Increment, out AdjustmentDisabled))
            {
                return Increment / NS_100_PER_MICROSECOND;
            }
            return 1;
        }

    }
}
