using System;

namespace AKNet.Common
{
    internal static class SystemInfo
    {
        public static void Print()
        {
            NetLog.Log($"系统信息 总内存：{GC.GetTotalMemory(false) / 1024 / 1024}");
        }

        public static long TotalMemory()
        {
            return GC.GetTotalMemory(false) / 1024;
        }
    }
}
