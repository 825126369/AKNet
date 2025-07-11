using System;
using System.Net.Mail;
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
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ___GlobalMemoryStatusEx(IntPtr lpBuffer);
        public static MEMORYSTATUSEX GlobalMemoryStatusEx()
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            int structSize = Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            memStatus.dwLength = (uint)structSize;

            IntPtr memStatusPtr = Marshal.AllocHGlobal(structSize);
            Marshal.StructureToPtr(memStatus, memStatusPtr, false);
            ___GlobalMemoryStatusEx(memStatusPtr);
            return Marshal.PtrToStructure<MEMORYSTATUSEX>(memStatusPtr);
        }

        public static void System_Win_PrintInfo(string[] args)
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

            //if (GlobalMemoryStatusEx(memStatus))
            //{
            //    Console.WriteLine($"总物理内存: {memStatus.ullTotalPhys / (1024 * 1024)} MB");
            //    Console.WriteLine($"可用物理内存: {memStatus.ullAvailPhys / (1024 * 1024)} MB");
            //    Console.WriteLine($"内存使用率: {memStatus.dwMemoryLoad}%");
            //}
            //else
            //{
            //    Console.WriteLine("无法获取内存状态。");
            //}
        }

    }
}
