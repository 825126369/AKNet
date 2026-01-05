/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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