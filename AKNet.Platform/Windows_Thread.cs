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
                        CxPlatProcessorInfo[Proc].Group = Group;
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
#if CXPLAT_USE_CUSTOM_THREAD_CONTEXT
            CXPLAT_THREAD_CUSTOM_CONTEXT* CustomContext =
                CXPLAT_ALLOC_NONPAGED(sizeof(CXPLAT_THREAD_CUSTOM_CONTEXT), QUIC_POOL_CUSTOM_THREAD);
            if (CustomContext == NULL)
            {
                QuicTraceEvent(
                    AllocFailure,
                    "Allocation of '%s' failed. (%llu bytes)",
                    "Custom thread context",
                    sizeof(CXPLAT_THREAD_CUSTOM_CONTEXT));
                return QUIC_STATUS_OUT_OF_MEMORY;
            }
            CustomContext->Callback = Config->Callback;
            CustomContext->Context = Config->Context;
            *Thread =
                CreateThread(
                    NULL,
                    0,
                    CxPlatThreadCustomStart,
                    CustomContext,
                    0,
                    NULL);
            if (*Thread == NULL)
            {
                CXPLAT_FREE(CustomContext, QUIC_POOL_CUSTOM_THREAD);
                DWORD Error = GetLastError();
                QuicTraceEvent(
                    LibraryErrorStatus,
                    "[ lib] ERROR, %u, %s.",
                    Error,
                    "CreateThread");
                return Error;
            }
#else // CXPLAT_USE_CUSTOM_THREAD_CONTEXT

            Thread = Interop.Kernel32.CreateThread(IntPtr.Zero, IntPtr.Zero, Config.Callback, Config.Context, 0, out _);
            if (Thread == IntPtr.Zero)
            {
                int Error = Interop.Kernel32.GetLastError();
                NetLog.LogError(Error);
                return Error;
            }
#endif
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
            if (HasFlag(Config.Flags, (ulong)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY &&
                !SetThreadPriority(*Thread, THREAD_PRIORITY_HIGHEST))
            {
                NetLog.LogError("SetThreadPriority");
            }

            if (Config.Name != null)
            {
                WCHAR WideName[64] = L"";
                size_t WideNameLength;
                mbstowcs_s(
                    &WideNameLength,
                    WideName,
                    ARRAYSIZE(WideName) - 1,
                    Config->Name,
                    _TRUNCATE);
#if defined(QUIC_RESTRICTED_BUILD)
        SetThreadDescription(*Thread, WideName);
#else
                THREAD_NAME_INFORMATION_PRIVATE ThreadNameInfo;
                RtlInitUnicodeString(&ThreadNameInfo.ThreadName, WideName);
                NtSetInformationThread(
                    *Thread,
                    ThreadNameInformationPrivate,
                    &ThreadNameInfo,
                    sizeof(ThreadNameInfo));
#endif
            }
            return QUIC_STATUS_SUCCESS;
        }
    }
}

#endif