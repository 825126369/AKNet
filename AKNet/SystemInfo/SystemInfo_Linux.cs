using System;
using System.Runtime.InteropServices;

namespace AKNet.Common
{
    public static partial class AKNetSystemInfo
    {
        [StructLayout(LayoutKind.Sequential)]
        struct sysinfo
        {
            public long uptime;
            public ulong loads1;      // 1-minute load average
            public ulong loads5;      // 5-minute load average
            public ulong loads15;     // 15-minute load average
            public ulong totalram;    // Total usable main memory size
            public ulong freeram;     // Available memory size
            public ulong sharedram;   // Amount of shared memory
            public ulong bufferram;   // Memory used by buffers
            public ulong totalswap;   // Total swap space size
            public ulong freeswap;    // Swap space still available
            public ushort procs;      // Number of current processes
            public ulong totalhigh;   // Total high memory size
            public ulong freehigh;    // Available high memory size
            public int mem_unit;      // Memory unit size in bytes
        }

        [DllImport("libc",  EntryPoint = "sysinfo", SetLastError = true)]
        static extern int get_sysinfo(ref sysinfo info);
    }
}
