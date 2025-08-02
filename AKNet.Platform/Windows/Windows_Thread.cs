#if TARGET_WINDOWS
using System;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public delegate int LPTHREAD_START_ROUTINE(IntPtr lpParameter);

    public enum LOGICAL_PROCESSOR_RELATIONSHIP
    {
        RelationProcessorCore,
        RelationNumaNode,
        RelationCache,
        RelationProcessorPackage,
        RelationGroup,
        RelationProcessorDie,
        RelationNumaNodeEx,
        RelationProcessorModule,
        RelationAll = 0xffff
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GROUP_AFFINITY
    {
        public ulong Mask;
        public ushort Group;
        public fixed ushort Reserved[3];
    }

    public unsafe struct PROCESSOR_RELATIONSHIP
    {
        public byte Flags;
        public byte EfficiencyClass;
        public fixed byte Reserved[20];
        public int GroupCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = OSPlatformFunc.ANYSIZE_ARRAY)]
        public GROUP_AFFINITY* GroupMask;
    }

    public unsafe struct PROCESSOR_GROUP_INFO
    {
        public byte MaximumProcessorCount;
        public byte ActiveProcessorCount;
        public fixed byte Reserved[38];
        public ulong ActiveProcessorMask;
    }

    public unsafe struct GROUP_RELATIONSHIP
    {
        public ushort MaximumGroupCount;
        public ushort ActiveGroupCount;
        public fixed byte Reserved[20];
        public PROCESSOR_GROUP_INFO GroupInfo;
        public PROCESSOR_GROUP_INFO* GetGroupInfo(int i)
        {
            fixed(PROCESSOR_GROUP_INFO* ptr = &GroupInfo)
            return (ptr + i);
        }
    }

    public unsafe struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
    {
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public int Size;

        [StructLayout(LayoutKind.Explicit)]
        public struct DUMMYUNIONNAME_DATA
        {
            [FieldOffset(0)] public PROCESSOR_RELATIONSHIP Processor;
            [FieldOffset(0)] public NUMA_NODE_RELATIONSHIP NumaNode;
            [FieldOffset(0)] public CACHE_RELATIONSHIP Cache;
            [FieldOffset(0)] public GROUP_RELATIONSHIP Group;
        }
        public DUMMYUNIONNAME_DATA DUMMYUNIONNAME;
    }

    public enum PROCESSOR_CACHE_TYPE
    {
        CacheUnified,
        CacheInstruction,
        CacheData,
        CacheTrace,
        CacheUnknown
    }

    public unsafe struct CACHE_RELATIONSHIP
    {
        public byte Level;
        public byte Associativity;
        public short LineSize;
        public int CacheSize;
        PROCESSOR_CACHE_TYPE Type;
        public fixed byte Reserved[18];
        public int GroupCount;

        [StructLayout(LayoutKind.Explicit)]
        public struct DUMMYUNIONNAME_DATA
        {
             [FieldOffset(0)]  public GROUP_AFFINITY GroupMask;
             [MarshalAs(UnmanagedType.ByValArray, SizeConst = OSPlatformFunc.ANYSIZE_ARRAY)] [FieldOffset(0)] public GROUP_AFFINITY* GroupMasks;
        }
        public DUMMYUNIONNAME_DATA DUMMYUNIONNAME;
    }

    public unsafe struct NUMA_NODE_RELATIONSHIP
    {
        public int NodeNumber;
        public fixed byte Reserved[18];
        public short GroupCount;

        [StructLayout(LayoutKind.Explicit)]
        public struct DUMMYUNIONNAME_DATA
        {
            [FieldOffset(0)]  public GROUP_AFFINITY GroupMask;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = OSPlatformFunc.ANYSIZE_ARRAY)][FieldOffset(0)]  public GROUP_AFFINITY* GroupMasks;
        }
        public DUMMYUNIONNAME_DATA DUMMYUNIONNAME;
    }

    public struct PROCESSOR_NUMBER
    {
        public ushort Group;
        public byte Number;
        public byte Reserved;
    }

    public static unsafe partial class OSPlatformFunc
    {
        public const int ANYSIZE_ARRAY = 1;

        public const uint INFINITE = 0xFFFFFFFF;  // Infinite timeout
        public const int MAXLONG = 0x7fffffff;
        public const int THREAD_DYNAMIC_CODE_ALLOW = 1;     // Opt-out of dynamic code generation.
        public const int THREAD_BASE_PRIORITY_LOWRT = 15;  // value that gets a thread to LowRealtime-1
        public const int THREAD_BASE_PRIORITY_MAX = 2;   // maximum thread base priority boost
        public const int THREAD_BASE_PRIORITY_MIN = (-2);  // minimum thread base priority boost
        public const int THREAD_BASE_PRIORITY_IDLE = (-15); // value that gets a thread to idle
        public const int THREAD_PRIORITY_LOWEST = THREAD_BASE_PRIORITY_MIN;
        public const int THREAD_PRIORITY_BELOW_NORMAL = (THREAD_PRIORITY_LOWEST + 1);
        public const int THREAD_PRIORITY_NORMAL = 0;
        public const int THREAD_PRIORITY_HIGHEST = THREAD_BASE_PRIORITY_MAX;
        public const int THREAD_PRIORITY_ABOVE_NORMAL = (THREAD_PRIORITY_HIGHEST - 1);
        public const int THREAD_PRIORITY_ERROR_RETURN = (MAXLONG);
        public const int THREAD_PRIORITY_TIME_CRITICAL = THREAD_BASE_PRIORITY_LOWRT;
        public const int THREAD_PRIORITY_IDLE = THREAD_BASE_PRIORITY_IDLE;
    }
}

#endif