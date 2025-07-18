#if TARGET_WINDOWS
using System;
using System.Runtime.InteropServices;
using CXPLAT_THREAD = System.IntPtr;

namespace AKNet.Platform
{
    public delegate int LPTHREAD_START_ROUTINE(IntPtr lpParameter);

    public struct CXPLAT_THREAD_CONFIG
    {
        public ushort Flags;
        public ushort IdealProcessor;
        public string Name;
        public LPTHREAD_START_ROUTINE Callback;
        public IntPtr Context;
    }

    internal enum LOGICAL_PROCESSOR_RELATIONSHIP
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
    internal unsafe struct GROUP_AFFINITY
    {
        public ulong Mask;
        public ushort Group;
        public fixed ushort Reserved[3];
    }

    internal unsafe struct PROCESSOR_RELATIONSHIP
    {
        private const int ANYSIZE_ARRAY = 1;

        public byte Flags;
        public byte EfficiencyClass;
        public fixed byte Reserved[20];
        public int GroupCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)]
        public GROUP_AFFINITY[] GroupMask;
    }

    internal unsafe struct PROCESSOR_GROUP_INFO
    {
        public byte MaximumProcessorCount;
        public byte ActiveProcessorCount;
        public fixed byte Reserved[38];
        public ulong ActiveProcessorMask;
    }

    internal unsafe struct GROUP_RELATIONSHIP
    {
        private const int ANYSIZE_ARRAY = 1;

        public int MaximumGroupCount;
        public int ActiveGroupCount;
        public fixed byte Reserved[20];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)]
        public PROCESSOR_GROUP_INFO[] GroupInfo;
    }

    internal unsafe struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
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

    internal enum PROCESSOR_CACHE_TYPE
    {
        CacheUnified,
        CacheInstruction,
        CacheData,
        CacheTrace,
        CacheUnknown
    }

    internal unsafe struct CACHE_RELATIONSHIP
    {
        private const int ANYSIZE_ARRAY = 1;

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
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)] [FieldOffset(0)] public GROUP_AFFINITY[] GroupMasks;
        }
        public DUMMYUNIONNAME_DATA DUMMYUNIONNAME;
    }

    internal unsafe struct NUMA_NODE_RELATIONSHIP
    {
        private const int ANYSIZE_ARRAY = 1;

        public int NodeNumber;
        public fixed byte Reserved[18];
        public short GroupCount;

        [StructLayout(LayoutKind.Explicit)]
        public struct DUMMYUNIONNAME_DATA
        {
            [FieldOffset(0)]  public GROUP_AFFINITY GroupMask;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)][FieldOffset(0)]  public GROUP_AFFINITY[] GroupMasks;
        }
        public DUMMYUNIONNAME_DATA DUMMYUNIONNAME;
    }

    internal unsafe struct CXPLAT_PROCESSOR_INFO
    {
        public ushort Group;  // The group number this processor is a part of
        public byte Index;   // Index in the current group
        public byte PADDING; // Here to align with PROCESSOR_NUMBER struct
    }

    internal unsafe struct CXPLAT_PROCESSOR_GROUP_INFO
    {
        public ulong Mask;  // Bit mask of active processors in the group
        public int Count;  // Count of active processors in the group
        public int Offset; // Base process index offset this group starts at
    }

    internal struct PROCESSOR_NUMBER
    {
        public ushort Group;
        public byte Number;
        public byte Reserved;
    }















    public static unsafe partial class OSPlatformFunc
    {
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

        static CXPLAT_PROCESSOR_INFO* CxPlatProcessorInfo;
        static CXPLAT_PROCESSOR_GROUP_INFO* CxPlatProcessorGroupInfo;
        static int CxPlatProcessorCount;

        public static int CxPlatProcCount()
        {
            return CxPlatProcessorCount;
        }

        public static int CxPlatProcessorInfoInit()
        {
            int Status = 0;
            int InfoLength = 0;
            SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* Info = null;
            int ActiveProcessorCount = 0, MaxProcessorCount = 0;
            Status = CxPlatGetProcessorGroupInfo(LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup, Info, out InfoLength);
            if (Status != 0)
            {
                goto Error;
            }

            NetLog.Assert(InfoLength != 0);
            NetLog.Assert(Info->Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup);
            NetLog.Assert(Info->DUMMYUNIONNAME.Group.ActiveGroupCount != 0);
            NetLog.Assert(Info->DUMMYUNIONNAME.Group.ActiveGroupCount <= Info->DUMMYUNIONNAME.Group.MaximumGroupCount);
            if (Info->DUMMYUNIONNAME.Group.ActiveGroupCount == 0)
            {
                goto Error;
            }

            for (int i = 0; i < Info->DUMMYUNIONNAME.Group.ActiveGroupCount; ++i)
            {
                ActiveProcessorCount += Info->DUMMYUNIONNAME.Group.GroupInfo[i].ActiveProcessorCount;
                MaxProcessorCount += Info->DUMMYUNIONNAME.Group.GroupInfo[i].MaximumProcessorCount;
            }

            NetLog.Assert(ActiveProcessorCount > 0);
            NetLog.Assert(ActiveProcessorCount <= ushort.MaxValue);
            if (ActiveProcessorCount == 0 || ActiveProcessorCount > ushort.MaxValue)
            {
                goto Error;
            }

            NetLog.Log(string.Format("[ dll] Processors: ({0} active, {1} max), Groups: ({2} active, {3} max)",
                ActiveProcessorCount,
                MaxProcessorCount,
                Info->DUMMYUNIONNAME.Group.ActiveGroupCount,
                Info->DUMMYUNIONNAME.Group.MaximumGroupCount));

            NetLog.Assert(CxPlatProcessorInfo == null);
            CxPlatProcessorInfo = (CXPLAT_PROCESSOR_INFO*)CxPlatAlloc(ActiveProcessorCount * sizeof(CXPLAT_PROCESSOR_INFO));
            if (CxPlatProcessorInfo == null)
            {
                goto Error;
            }

            CxPlatZeroMemory(
                CxPlatProcessorInfo,
                ActiveProcessorCount * sizeof(CXPLAT_PROCESSOR_INFO));

            NetLog.Assert(CxPlatProcessorGroupInfo == null);
            CxPlatProcessorGroupInfo = (CXPLAT_PROCESSOR_GROUP_INFO*)CxPlatAlloc(
                    Info->DUMMYUNIONNAME.Group.ActiveGroupCount * sizeof(CXPLAT_PROCESSOR_GROUP_INFO));

            if (CxPlatProcessorGroupInfo == null)
            {
                goto Error;
            }

            CxPlatProcessorCount = 0;
            for (int i = 0; i < Info->DUMMYUNIONNAME.Group.ActiveGroupCount; ++i)
            {
                CxPlatProcessorGroupInfo[i].Mask = Info->DUMMYUNIONNAME.Group.GroupInfo[i].ActiveProcessorMask;
                CxPlatProcessorGroupInfo[i].Count = Info->DUMMYUNIONNAME.Group.GroupInfo[i].ActiveProcessorCount;
                CxPlatProcessorGroupInfo[i].Offset = CxPlatProcessorCount;
                CxPlatProcessorCount += Info->DUMMYUNIONNAME.Group.GroupInfo[i].ActiveProcessorCount;
            }

            for (int Proc = 0; Proc < ActiveProcessorCount; ++Proc)
            {
                for (int Group = 0; Group < Info->DUMMYUNIONNAME.Group.ActiveGroupCount; ++Group)
                {
                    if (Proc >= CxPlatProcessorGroupInfo[Group].Offset &&
                        Proc < CxPlatProcessorGroupInfo[Group].Offset + Info->DUMMYUNIONNAME.Group.GroupInfo[Group].ActiveProcessorCount)
                    {
                        CxPlatProcessorInfo[Proc].Group = (ushort)Group;
                        NetLog.Assert(Proc - CxPlatProcessorGroupInfo[Group].Offset <= byte.MaxValue);
                        CxPlatProcessorInfo[Proc].Index = (byte)(Proc - CxPlatProcessorGroupInfo[Group].Offset);
                        break;
                    }
                }
            }

            if (Info != null)
            {
                CxPlatFree(Info);
            }

            return 0;
        Error:
            if (Info != null)
            {
                CxPlatFree(Info);
            }
            if (CxPlatProcessorGroupInfo != null)
            {
                CxPlatFree(CxPlatProcessorGroupInfo);
                CxPlatProcessorGroupInfo = null;
            }
            if (CxPlatProcessorInfo != null)
            {
                CxPlatFree(CxPlatProcessorInfo);
                CxPlatProcessorInfo = null;
            }
            return 1;
        }

        static void CxPlatProcessorInfoUnInit()
        {
            CxPlatFree(CxPlatProcessorGroupInfo);
            CxPlatProcessorGroupInfo = null;
            CxPlatFree(CxPlatProcessorInfo);
            CxPlatProcessorInfo = null;
        }

        static int CxPlatGetProcessorGroupInfo(LOGICAL_PROCESSOR_RELATIONSHIP Relationship, SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX** Buffer, out int BufferLength)
        {
            BufferLength = 0;
            Interop.Kernel32.GetLogicalProcessorInformationEx(Relationship, null, out BufferLength);
            if (BufferLength == 0)
            {
                return Interop.Kernel32.GetLastError();
            }

            *Buffer = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)CxPlatAlloc(BufferLength);
            if (*Buffer == null)
            {
                return 1;
            }

            if (!Interop.Kernel32.GetLogicalProcessorInformationEx(Relationship, *Buffer, out BufferLength))
            {
                CxPlatFree(*Buffer);
                return Interop.Kernel32.GetLastError();
            }
            return 0;
        }

        public static int CxPlatThreadCreate(CXPLAT_THREAD_CONFIG Config, out CXPLAT_THREAD Thread)
        {
            Thread = Interop.Kernel32.CreateThread(IntPtr.Zero, IntPtr.Zero, Config.Callback, Config.Context, 0, out _);
            if (Thread == IntPtr.Zero)
            {
                int Error = Interop.Kernel32.GetLastError();
                NetLog.LogError(Error);
                return Error;
            }

            NetLog.Assert(Config.IdealProcessor < CxPlatProcCount());
            CXPLAT_PROCESSOR_INFO ProcInfo = CxPlatProcessorInfo[Config.IdealProcessor];
            GROUP_AFFINITY Group;
            if (HasFlag(Config.Flags, (ushort)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_AFFINITIZE))
            {
                Group.Mask = (ulong)(1ul << ProcInfo.Index);          // Fixed processor
            }
            else
            {
                Group.Mask = CxPlatProcessorGroupInfo[ProcInfo.Group].Mask;
            }

            Group.Group = ProcInfo.Group;
            if (!Interop.Kernel32.SetThreadGroupAffinity(Thread, &Group, null))
            {
                NetLog.LogError("SetThreadGroupAffinity");
            }
            if (HasFlag(Config.Flags, (ulong)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_IDEAL_PROC) &&
                !Interop.Kernel32.SetThreadIdealProcessorEx(Thread, (PROCESSOR_NUMBER*)&ProcInfo, null))
            {
                NetLog.LogError("SetThreadIdealProcessorEx");
            }
            if (HasFlag(Config.Flags, (ulong)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY) &&
                !Interop.Kernel32.SetThreadPriority(Thread, THREAD_PRIORITY_HIGHEST))
            {
                NetLog.LogError("SetThreadPriority");
            }

            if (Config.Name != null)
            {
                Interop.Kernel32.SetThreadDescription(Thread, Config.Name);
            }
            return 0;
        }
    }
}

#endif