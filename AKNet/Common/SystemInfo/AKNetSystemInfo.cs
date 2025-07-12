using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AKNet.Common
{
    public static partial class AKNetSystemInfo
    {
        public static long GetUsedMemory()
        {
            Process currentProcess = Process.GetCurrentProcess();
            return currentProcess.WorkingSet64;
        }

        public static long GetTotalMemory()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    MEMORYSTATUSEX memStatus = GlobalMemoryStatusEx();
                    return (long)memStatus.ullTotalPhys;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                   
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    
                }
            }
            catch
            { 

            }
            return int.MaxValue;
        }

        public static void PrintInfo()
        {
            Process currentProcess = Process.GetCurrentProcess();
            NetLog.Log("系统信息：");
            NetLog.Log($"当前进程 工作集(物理内存)大小: {currentProcess.WorkingSet64 / 1024 / 1024}M字节");
            NetLog.Log($"系统内存 总大小: {GetTotalMemory() / 1024 / 1024}M字节");
            NetLog.Log($"C# 堆大小：{GC.GetTotalMemory(false) / 1024 / 1024}M字节");
        }
    }
}
