#if TARGET_WINDOWS
using AKNet.Platform;
using System;
using System.Runtime.InteropServices;
using CXPLAT_THREAD = System.IntPtr;

namespace AKNet.Platform
{
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
        public ulong* ActiveProcessorMask;
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

    internal struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
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

    public static unsafe partial class OSPlatformFunc
    {
        static void* CxPlatAlloc(int ByteCount, uint Tag)
        {
#if DEBUG
            NetLog.Assert(CxPlatform.Heap);
            CXPLAT_DBG_ASSERT(ByteCount != 0);
            uint32_t Rand;
            if ((CxPlatform.AllocFailDenominator > 0 && (CxPlatRandom(sizeof(Rand), &Rand), Rand % CxPlatform.AllocFailDenominator) == 1) ||
                (CxPlatform.AllocFailDenominator < 0 && InterlockedIncrement(&CxPlatform.AllocCounter) % CxPlatform.AllocFailDenominator == 0))
            {
                return NULL;
            }

            void* Alloc = HeapAlloc(CxPlatform.Heap, 0, ByteCount + AllocOffset);
            if (Alloc == NULL)
            {
                return NULL;
            }
            *((uint32_t*)Alloc) = Tag;
            return (void*)((uint8_t*)Alloc + AllocOffset);
#else
    UNREFERENCED_PARAMETER(Tag);
    return HeapAlloc(CxPlatform.Heap, 0, ByteCount);
#endif
        }

        public static int CxPlatProcessorInfoInit()
        {
            int Status = 0;
            int InfoLength = 0;
            IntPtr Info = NULL;
            int ActiveProcessorCount = 0, MaxProcessorCount = 0;
            Status = CxPlatGetProcessorGroupInfo(RelationGroup, &Info, &InfoLength);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            NetLog.Assert(InfoLength != 0);
            NetLog.Assert(Info.Relationship == RelationGroup);
            NetLog.Assert(Info.Group.ActiveGroupCount != 0);
            NetLog.Assert(Info.Group.ActiveGroupCount <= Info.Group.MaximumGroupCount);
            if (Info.Group.ActiveGroupCount == 0)
            {
                goto Error;
            }

            for (int i = 0; i < Info.Group.ActiveGroupCount; ++i)
            {
                ActiveProcessorCount += Info.Group.GroupInfo[i].ActiveProcessorCount;
                MaxProcessorCount += Info.Group.GroupInfo[i].MaximumProcessorCount;
            }

            NetLog.Assert(ActiveProcessorCount > 0);
            NetLog.Assert(ActiveProcessorCount <= ushort.MaxValue);
            if (ActiveProcessorCount == 0 || ActiveProcessorCount > ushort.MaxValue)
            {
                goto Error;
            }

            QuicTraceLogInfo(
                WindowsUserProcessorStateV3,
                "[ dll] Processors: (%u active, %u max), Groups: (%hu active, %hu max)",
                ActiveProcessorCount,
                MaxProcessorCount,
                Info->Group.ActiveGroupCount,
                Info->Group.MaximumGroupCount);

            CXPLAT_FRE_ASSERT(CxPlatProcessorInfo == NULL);
            CxPlatProcessorInfo =
                CXPLAT_ALLOC_NONPAGED(
                    ActiveProcessorCount * sizeof(CXPLAT_PROCESSOR_INFO),
                    QUIC_POOL_PLATFORM_PROC);
            if (CxPlatProcessorInfo == NULL)
            {
                QuicTraceEvent(
                    AllocFailure,
                    "Allocation of '%s' failed. (%llu bytes)",
                    "CxPlatProcessorInfo",
                    ActiveProcessorCount * sizeof(CXPLAT_PROCESSOR_INFO));
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }
            CxPlatZeroMemory(
                CxPlatProcessorInfo,
                ActiveProcessorCount * sizeof(CXPLAT_PROCESSOR_INFO));

            CXPLAT_DBG_ASSERT(CxPlatProcessorGroupInfo == NULL);
            CxPlatProcessorGroupInfo =
                CXPLAT_ALLOC_NONPAGED(
                    Info->Group.ActiveGroupCount * sizeof(CXPLAT_PROCESSOR_GROUP_INFO),
                    QUIC_POOL_PLATFORM_PROC);
            if (CxPlatProcessorGroupInfo == NULL)
            {
                QuicTraceEvent(
                    AllocFailure,
                    "Allocation of '%s' failed. (%llu bytes)",
                    "CxPlatProcessorGroupInfo",
                    Info->Group.ActiveGroupCount * sizeof(CXPLAT_PROCESSOR_GROUP_INFO));
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            CxPlatProcessorCount = 0;
            for (WORD i = 0; i < Info->Group.ActiveGroupCount; ++i)
            {
                CxPlatProcessorGroupInfo[i].Mask = Info->Group.GroupInfo[i].ActiveProcessorMask;
                CxPlatProcessorGroupInfo[i].Count = Info->Group.GroupInfo[i].ActiveProcessorCount;
                CxPlatProcessorGroupInfo[i].Offset = CxPlatProcessorCount;
                CxPlatProcessorCount += Info->Group.GroupInfo[i].ActiveProcessorCount;
            }

            for (uint32_t Proc = 0; Proc < ActiveProcessorCount; ++Proc)
            {
                for (WORD Group = 0; Group < Info->Group.ActiveGroupCount; ++Group)
                {
                    if (Proc >= CxPlatProcessorGroupInfo[Group].Offset &&
                        Proc < CxPlatProcessorGroupInfo[Group].Offset + Info->Group.GroupInfo[Group].ActiveProcessorCount)
                    {
                        CxPlatProcessorInfo[Proc].Group = Group;
                        CXPLAT_DBG_ASSERT(Proc - CxPlatProcessorGroupInfo[Group].Offset <= UINT8_MAX);
                        CxPlatProcessorInfo[Proc].Index = (uint8_t)(Proc - CxPlatProcessorGroupInfo[Group].Offset);
#pragma warning(push)
#pragma warning(disable:6385) // Reading invalid data from 'CxPlatProcessorInfo' (FALSE POSITIVE)
                        QuicTraceLogInfo(
                            ProcessorInfoV3,
                            "[ dll] Proc[%u] Group[%hu] Index[%hhu] Active=%hhu",
                            Proc,
                            (uint16_t)Group,
                            CxPlatProcessorInfo[Proc].Index,
                            (uint8_t)!!(CxPlatProcessorGroupInfo[Group].Mask & (1ULL << CxPlatProcessorInfo[Proc].Index)));
#pragma warning(pop)
            break;
        }
    }
}

Status = QUIC_STATUS_SUCCESS;

Error:

if (Info)
{
    CXPLAT_FREE(Info, QUIC_POOL_PLATFORM_TMP_ALLOC);
}

if (QUIC_FAILED(Status))
{
    if (CxPlatProcessorGroupInfo)
    {
        CXPLAT_FREE(CxPlatProcessorGroupInfo, QUIC_POOL_PLATFORM_PROC);
        CxPlatProcessorGroupInfo = NULL;
    }
    if (CxPlatProcessorInfo)
    {
        CXPLAT_FREE(CxPlatProcessorInfo, QUIC_POOL_PLATFORM_PROC);
        CxPlatProcessorInfo = NULL;
    }
}

return Status;
}

static int CxPlatGetProcessorGroupInfo(LOGICAL_PROCESSOR_RELATIONSHIP Relationship, SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* Buffer, out int BufferLength)
{
    BufferLength = 0;
    Interop.GetLogicalProcessorInformationEx(Relationship, IntPtr.Zero, out BufferLength);
    if (BufferLength == 0)
    {
        return Interop.GetLastError();
    }

    *Buffer = CXPLAT_ALLOC_NONPAGED(*BufferLength, QUIC_POOL_PLATFORM_TMP_ALLOC);
    if (*Buffer == NULL)
    {
        QuicTraceEvent(
            AllocFailure,
            "Allocation of '%s' failed. (%llu bytes)",
            "PSYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX",
            *BufferLength);
        return QUIC_STATUS_OUT_OF_MEMORY;
    }

    if (!GetLogicalProcessorInformationEx(
            Relationship,
            *Buffer,
            BufferLength))
    {
        QuicTraceEvent(
            LibraryErrorStatus,
            "[ lib] ERROR, %u, %s.",
            GetLastError(),
            "GetLogicalProcessorInformationEx failed");
        CXPLAT_FREE(*Buffer, QUIC_POOL_PLATFORM_TMP_ALLOC);
        return HRESULT_FROM_WIN32(GetLastError());
    }

    return QUIC_STATUS_SUCCESS;
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
            Thread = Interop.CreateThread(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    Config.Callback,
                    Config.Context,
                    0,
                    out _);
            if (Thread == IntPtr.Zero)
            {
                int Error = Interop.GetLastError();
                NetLog.LogError(Error);
                return Error;
            }
#endif
            NetLog.Assert(Config.IdealProcessor < CxPlatProcCount());
            const CXPLAT_PROCESSOR_INFO* ProcInfo = &CxPlatProcessorInfo[Config->IdealProcessor];
            GROUP_AFFINITY Group = { 0 };
            if (Config->Flags & CXPLAT_THREAD_FLAG_SET_AFFINITIZE)
            {
                Group.Mask = (KAFFINITY)(1ull << ProcInfo->Index);          // Fixed processor
            }
            else
            {
                Group.Mask = CxPlatProcessorGroupInfo[ProcInfo->Group].Mask;
            }
            Group.Group = ProcInfo->Group;
            if (!SetThreadGroupAffinity(*Thread, &Group, NULL))
            {
                QuicTraceEvent(
                    LibraryErrorStatus,
                    "[ lib] ERROR, %u, %s.",
                    GetLastError(),
                    "SetThreadGroupAffinity");
            }
            if (Config->Flags & CXPLAT_THREAD_FLAG_SET_IDEAL_PROC &&
                !SetThreadIdealProcessorEx(*Thread, (PROCESSOR_NUMBER*)ProcInfo, NULL))
            {
                QuicTraceEvent(
                    LibraryErrorStatus,
                    "[ lib] ERROR, %u, %s.",
                    GetLastError(),
                    "SetThreadIdealProcessorEx");
            }
            if (Config->Flags & CXPLAT_THREAD_FLAG_HIGH_PRIORITY &&
                !SetThreadPriority(*Thread, THREAD_PRIORITY_HIGHEST))
            {
                QuicTraceEvent(
                    LibraryErrorStatus,
                    "[ lib] ERROR, %u, %s.",
                    GetLastError(),
                    "SetThreadPriority");
            }
            if (Config->Name)
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