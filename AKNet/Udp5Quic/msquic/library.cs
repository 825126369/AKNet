using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_HANDLE_TYPE
    {
        QUIC_HANDLE_TYPE_REGISTRATION,
        QUIC_HANDLE_TYPE_CONFIGURATION,
        QUIC_HANDLE_TYPE_LISTENER,
        QUIC_HANDLE_TYPE_CONNECTION_CLIENT,
        QUIC_HANDLE_TYPE_CONNECTION_SERVER,
        QUIC_HANDLE_TYPE_STREAM
    }

    internal class QUIC_HANDLE
    {
        public QUIC_HANDLE_TYPE Type;
    }

    internal class QUIC_LIBRARY_PP
    {
        public readonly CXPLAT_POOL<QUIC_CONNECTION> ConnectionPool = new CXPLAT_POOL<QUIC_CONNECTION>();
        public readonly CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS> TransportParamPool = new CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS>();
        public readonly CXPLAT_POOL<QUIC_PACKET_SPACE> PacketSpacePool = new CXPLAT_POOL<QUIC_PACKET_SPACE>();

        public CXPLAT_HASH ResetTokenHash;
        public readonly object ResetTokenLock = new object();
        public ulong SendBatchId;
        public ulong SendPacketId;
        public ulong ReceivePacketId;
        public readonly long[] PerfCounters = new long[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX];
    }

    internal class QUIC_LIBRARY
    {
        public bool Loaded;
        public bool LazyInitComplete;
        public bool IsVerifying;
        public bool InUse;
        public bool SendRetryEnabled;
        public bool CurrentStatelessRetryKey;
        public readonly uint[] Version = new uint[4];
        public QUIC_SETTINGS_INTERNAL Settings;
        public readonly object Lock = new object();
        public readonly object DatapathLock = new object();
        public readonly object StatelessRetryKeysLock = new object();

        public int LoadRefCount;
        public int OpenRefCount;
        public int ProcessorCount;
        public int PartitionCount;
        public int PartitionMask;
        public int ConnectionCount;
        public byte TimerResolutionMs;
        public byte CidServerIdLength;
        public byte CidTotalLength;
        public long ConnectionCorrelationId;
        public ulong HandshakeMemoryLimit;
        public ulong CurrentHandshakeMemoryUsage;
        public CXPLAT_STORAGE Storage;
        public QUIC_EXECUTION_CONFIG ExecutionConfig;
        public CXPLAT_DATAPATH Datapath;

        public CXPLAT_LIST_ENTRY Registrations;
        public CXPLAT_LIST_ENTRY Bindings;

        public QUIC_REGISTRATION StatelessRegistration;
        public List<QUIC_LIBRARY_PP> PerProc = new List<QUIC_LIBRARY_PP>();
        public string[] StatelessRetryKeys = new string[2];
        public readonly long[] StatelessRetryKeysExpiration = new long[2];

        public uint DefaultCompatibilityList;
        public uint DefaultCompatibilityListLength;
        public long PerfCounterSamplesTime;
        public long[] PerfCounterSamples = new long[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX];
        public readonly CXPLAT_WORKER_POOL WorkerPool = new CXPLAT_WORKER_POOL();
    }

    internal static partial class MSQuicFunc
    {
        static readonly QUIC_LIBRARY MsQuicLib = new QUIC_LIBRARY();

        static void QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS Type, long Value = 1)
        {
            NetLog.Assert(Type >= 0 && Type < QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX);
            Interlocked.Add(ref QuicLibraryGetPerProc().PerfCounters[(int)Type], Value);
        }

        static int QuicLibraryGetPartitionProcessor(int PartitionIndex)
        {
            NetLog.Assert(MsQuicLib.PerProc != null);
            if (MsQuicLib.ExecutionConfig != null && MsQuicLib.ExecutionConfig.ProcessorList.Count > 0)
            {
                return MsQuicLib.ExecutionConfig.ProcessorList[PartitionIndex];
            }
            return PartitionIndex;
        }

        static int QuicLibraryGetCurrentPartition()
        {
            int CurrentProc = CxPlatProcCurrentNumber();
            if (MsQuicLib.ExecutionConfig != null && MsQuicLib.ExecutionConfig.ProcessorList.Count > 0)
            {
                for (int i = 0; i < MsQuicLib.ExecutionConfig.ProcessorList.Count; ++i)
                {
                    if (CurrentProc <= MsQuicLib.ExecutionConfig.ProcessorList[i])
                    {
                        return i;
                    }
                }
                return MsQuicLib.ExecutionConfig.ProcessorList.Count - 1;
            }
            return CurrentProc % MsQuicLib.PartitionCount;
        }

        static int QuicPartitionIdCreate(int BaseIndex)
        {
            NetLog.Assert(BaseIndex < MsQuicLib.PartitionCount);
            int PartitionId = RandomTool.Random(ushort.MinValue, ushort.MaxValue);
            return (PartitionId & ~MsQuicLib.PartitionMask) | BaseIndex;
        }

        static int QuicPartitionIdGetIndex(int PartitionId)
        {
            return (PartitionId & MsQuicLib.PartitionMask) % MsQuicLib.PartitionCount;
        }

        static QUIC_LIBRARY_PP QuicLibraryGetPerProc()
        {
            NetLog.Assert(MsQuicLib.PerProc != null);
            int CurrentProc = CxPlatProcCurrentNumber() % MsQuicLib.ProcessorCount;
            return MsQuicLib.PerProc[CurrentProc];
        }

        static void MsQuicLibraryLoad()
        {
            if (Interlocked.Increment(ref MsQuicLib.LoadRefCount) == 1)
            {
                CxPlatSystemLoad();
                CxPlatListInitializeHead(MsQuicLib.Registrations);
                CxPlatListInitializeHead(MsQuicLib.Bindings);
                QuicTraceRundownCallback = QuicTraceRundown;
                MsQuicLib.Loaded = true;
                MsQuicLib.Version[0] = VER_MAJOR;
                MsQuicLib.Version[1] = VER_MINOR;
                MsQuicLib.Version[2] = VER_PATCH;
                MsQuicLib.Version[3] = VER_BUILD_ID;
            }
        }

        static void MsQuicCalculatePartitionMask()
        {
            NetLog.Assert(MsQuicLib.PartitionCount != 0);
            NetLog.Assert(MsQuicLib.PartitionCount != 0xFFFF);

            int PartitionCount = MsQuicLib.PartitionCount;
            PartitionCount |= (PartitionCount >> 1);
            PartitionCount |= (PartitionCount >> 2);
            PartitionCount |= (PartitionCount >> 4);
            PartitionCount |= (PartitionCount >> 8);

            MsQuicLib.PartitionMask = (ushort)PartitionCount;
        }

        static ulong QuicLibraryInitializePartitions()
        {
            MsQuicLib.ProcessorCount = (ushort)Environment.ProcessorCount;
            NetLog.Assert(MsQuicLib.ProcessorCount > 0);

            if (MsQuicLib.ExecutionConfig != null && MsQuicLib.ExecutionConfig.ProcessorList.Count > 0)
            {
                MsQuicLib.PartitionCount = (ushort)MsQuicLib.ExecutionConfig.ProcessorList.Count;
            }
            else
            {
                MsQuicLib.PartitionCount = MsQuicLib.ProcessorCount;
                uint MaxPartitionCount = QUIC_MAX_PARTITION_COUNT;

                //if (MsQuicLib.Storage != null)
                //{
                //    uint MaxPartitionCountLen = sizeof(uint);
                //    CxPlatStorageReadValue(MsQuicLib.Storage, QUIC_SETTING_MAX_PARTITION_COUNT, MaxPartitionCount,MaxPartitionCountLen);
                //    if (MaxPartitionCount == 0)
                //    {
                //        MaxPartitionCount = QUIC_MAX_PARTITION_COUNT;
                //    }
                //}

                if (MsQuicLib.PartitionCount > MaxPartitionCount)
                {
                    MsQuicLib.PartitionCount = (ushort)MaxPartitionCount;
                }
            }

            MsQuicCalculatePartitionMask();
            MsQuicLib.PerProc.Clear();
            for (int i = 0; i < MsQuicLib.ProcessorCount; ++i)
            {
                QUIC_LIBRARY_PP PerProc = MsQuicLib.PerProc[i];
                PerProc.ConnectionPool.CxPlatPoolInitialize();
                PerProc.TransportParamPool.CxPlatPoolInitialize();
                PerProc.PacketSpacePool.CxPlatPoolInitialize();
            }

            byte[] ResetHashKey = new byte[20];
            CxPlatRandom(ResetHashKey.Length, ResetHashKey);
            for (ushort i = 0; i < MsQuicLib.ProcessorCount; ++i)
            {
                QUIC_LIBRARY_PP PerProc = MsQuicLib.PerProc[i];
                ulong Status = CxPlatHashCreate(CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256, ResetHashKey, ResetHashKey.Length, ref PerProc.ResetTokenHash);
                if (QUIC_FAILED(Status))
                {
                    MsQuicLibraryFreePartitions();
                    return Status;
                }
            }
            return QUIC_STATUS_SUCCESS;
        }

        static void MsQuicLibraryFreePartitions()
        {
            if (MsQuicLib.PerProc != null)
            {
                MsQuicLib.PerProc = null;
            }
        }

        public static ulong QuicLibraryLazyInitialize(bool AcquireLock)
        {
            CXPLAT_UDP_DATAPATH_CALLBACKS DatapathCallbacks =
            {
                QuicBindingReceive,
                QuicBindingUnreachable,
            };

            ulong Status = QUIC_STATUS_SUCCESS;
            if (AcquireLock)
            {
                Monitor.Enter(MsQuicLib.Lock);
            }

            if (MsQuicLib.LazyInitComplete)
            {
                goto Exit;
            }

            NetLog.Assert(MsQuicLib.PerProc == null);
            NetLog.Assert(MsQuicLib.Datapath == null);

            Status = QuicLibraryInitializePartitions();
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            Status =
                CxPlatDataPathInitialize(
                    sizeof(QUIC_RX_PACKET),
                    &DatapathCallbacks,
                    NULL,                   // TcpCallbacks
                    &MsQuicLib.WorkerPool,
                    MsQuicLib.ExecutionConfig,
                    &MsQuicLib.Datapath);
            if (QUIC_SUCCEEDED(Status))
            {
                QuicTraceEvent(
                    DataPathInitialized,
                    "[data] Initialized, DatapathFeatures=%u",
                    CxPlatDataPathGetSupportedFeatures(MsQuicLib.Datapath));
            }
            else
            {
                MsQuicLibraryFreePartitions();
                goto Exit;
            }

            CXPLAT_DBG_ASSERT(MsQuicLib.PerProc != NULL);
            CXPLAT_DBG_ASSERT(MsQuicLib.Datapath != NULL);
            MsQuicLib.LazyInitComplete = TRUE;

        Exit:
            if (AcquireLock)
            {
                Monitor.Exit(MsQuicLib.Lock);
            }
            return Status;
        }

        static ulong QuicLibrarySetGlobalParam(uint Param, int BufferLength, void* Buffer)
{
    QUIC_STATUS Status = QUIC_STATUS_SUCCESS;
        QUIC_SETTINGS_INTERNAL InternalSettings = { 0 };

    switch (Param) {
    case QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT:

        if (BufferLength != sizeof(MsQuicLib.Settings.RetryMemoryLimit)) {
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }

    MsQuicLib.Settings.RetryMemoryLimit = * (uint16_t*) Buffer;
    MsQuicLib.Settings.IsSet.RetryMemoryLimit = TRUE;

        QuicTraceLogInfo(
            LibraryRetryMemoryLimitSet,
            "[ lib] Updated retry memory limit = %hu",
            MsQuicLib.Settings.RetryMemoryLimit);

    MsQuicLib.HandshakeMemoryLimit =
            (MsQuicLib.Settings.RetryMemoryLimit* CxPlatTotalMemory) / UINT16_MAX;
        QuicLibraryEvaluateSendRetryState();

    Status = QUIC_STATUS_SUCCESS;
        break;

    case QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE: {

        if (BufferLength != sizeof(uint16_t)) {
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }

if (*(uint16_t*)Buffer > QUIC_LOAD_BALANCING_SERVER_ID_IP)
{
    Status = QUIC_STATUS_INVALID_PARAMETER;
    break;
}

if (MsQuicLib.InUse &&
    MsQuicLib.Settings.LoadBalancingMode != *(uint16_t*)Buffer)
{
    QuicTraceLogError(
        LibraryLoadBalancingModeSetAfterInUse,
        "[ lib] Tried to change load balancing mode after library in use!");
    Status = QUIC_STATUS_INVALID_STATE;
    break;
}

MsQuicLib.Settings.LoadBalancingMode = *(uint16_t*)Buffer;
MsQuicLib.Settings.IsSet.LoadBalancingMode = TRUE;

QuicLibApplyLoadBalancingSetting();

QuicTraceLogInfo(
    LibraryLoadBalancingModeSet,
    "[ lib] Updated load balancing mode = %hu",
    MsQuicLib.Settings.LoadBalancingMode);

Status = QUIC_STATUS_SUCCESS;
break;
    }

    case QUIC_PARAM_GLOBAL_SETTINGS:

    if (Buffer == NULL)
    {
        Status = QUIC_STATUS_INVALID_PARAMETER;
        break;
    }

    QuicTraceLogInfo(
        LibrarySetSettings,
        "[ lib] Setting new settings");

    Status =
        QuicSettingsSettingsToInternal(
            BufferLength,
            (QUIC_SETTINGS*)Buffer,
            &InternalSettings);
    if (QUIC_FAILED(Status))
    {
        break;
    }

    if (!QuicSettingApply(
            &MsQuicLib.Settings,
            TRUE,
            TRUE,
            &InternalSettings))
    {
        Status = QUIC_STATUS_INVALID_PARAMETER;
        break;
    }

    if (QUIC_SUCCEEDED(Status))
    {
        MsQuicLibraryOnSettingsChanged(TRUE);
    }

    break;

case QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS:

    if (Buffer == NULL)
    {
        Status = QUIC_STATUS_INVALID_PARAMETER;
        break;
    }

    QuicTraceLogInfo(
        LibrarySetSettings,
        "[ lib] Setting new settings");

    Status =
        QuicSettingsGlobalSettingsToInternal(
            BufferLength,
            (QUIC_GLOBAL_SETTINGS*)Buffer,
            &InternalSettings);
    if (QUIC_FAILED(Status))
    {
        break;
    }

    if (!QuicSettingApply(
            &MsQuicLib.Settings,
            TRUE,
            TRUE,
            &InternalSettings))
    {
        Status = QUIC_STATUS_INVALID_PARAMETER;
        break;
    }

    if (QUIC_SUCCEEDED(Status))
    {
        MsQuicLibraryOnSettingsChanged(TRUE);
    }

    break;

case QUIC_PARAM_GLOBAL_VERSION_SETTINGS:

    if (Buffer == NULL)
    {
        Status = QUIC_STATUS_INVALID_PARAMETER;
        break;
    }

    QuicTraceLogInfo(
        LibrarySetSettings,
        "[ lib] Setting new settings");

    Status =
        QuicSettingsVersionSettingsToInternal(
            BufferLength,
            (QUIC_VERSION_SETTINGS*)Buffer,
            &InternalSettings);
    if (QUIC_FAILED(Status))
    {
        break;
    }

    if (!QuicSettingApply(
            &MsQuicLib.Settings,
            TRUE,
            TRUE,
            &InternalSettings))
    {
        QuicSettingsCleanup(&InternalSettings);
        Status = QUIC_STATUS_INVALID_PARAMETER;
        break;
    }
    QuicSettingsCleanup(&InternalSettings);

    if (QUIC_SUCCEEDED(Status))
    {
        MsQuicLibraryOnSettingsChanged(TRUE);
    }

    break;

case QUIC_PARAM_GLOBAL_EXECUTION_CONFIG:
    {
        if (BufferLength == 0)
        {
            if (MsQuicLib.ExecutionConfig != NULL)
            {
                CXPLAT_FREE(MsQuicLib.ExecutionConfig, QUIC_POOL_EXECUTION_CONFIG);
                MsQuicLib.ExecutionConfig = NULL;
            }
            return QUIC_STATUS_SUCCESS;
        }

        if (Buffer == NULL || BufferLength < QUIC_EXECUTION_CONFIG_MIN_SIZE)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        QUIC_EXECUTION_CONFIG* Config = (QUIC_EXECUTION_CONFIG*)Buffer;

        if (BufferLength < QUIC_EXECUTION_CONFIG_MIN_SIZE + sizeof(uint16_t) * Config->ProcessorCount)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        for (uint32_t i = 0; i < Config->ProcessorCount; ++i)
        {
            if (Config->ProcessorList[i] >= CxPlatProcCount())
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }
        }

        CxPlatLockAcquire(&MsQuicLib.Lock);
        if (MsQuicLib.LazyInitComplete)
        {

            //
            // We only allow for updating the polling idle timeout after MsQuic library has
            // finished up lazy initialization, which initializes both PerProc struct and
            // the datapath; and only if the app set some custom config to begin with.
            //
            CXPLAT_DBG_ASSERT(MsQuicLib.PerProc != NULL);
            CXPLAT_DBG_ASSERT(MsQuicLib.Datapath != NULL);

            if (MsQuicLib.ExecutionConfig == NULL)
            {
                Status = QUIC_STATUS_INVALID_STATE;
            }
            else
            {
                MsQuicLib.ExecutionConfig->PollingIdleTimeoutUs = Config->PollingIdleTimeoutUs;
                CxPlatDataPathUpdateConfig(MsQuicLib.Datapath, MsQuicLib.ExecutionConfig);
                Status = QUIC_STATUS_SUCCESS;
            }
            CxPlatLockRelease(&MsQuicLib.Lock);
            break;
        }

        QUIC_EXECUTION_CONFIG* NewConfig =
            CXPLAT_ALLOC_NONPAGED(BufferLength, QUIC_POOL_EXECUTION_CONFIG);
        if (NewConfig == NULL)
        {
            QuicTraceEvent(
                AllocFailure,
                "Allocation of '%s' failed. (%llu bytes)",
                "Execution config",
                BufferLength);
            Status = QUIC_STATUS_OUT_OF_MEMORY;
            CxPlatLockRelease(&MsQuicLib.Lock);
            break;
        }

        if (MsQuicLib.ExecutionConfig != NULL)
        {
            CXPLAT_FREE(MsQuicLib.ExecutionConfig, QUIC_POOL_EXECUTION_CONFIG);
        }

        CxPlatCopyMemory(NewConfig, Config, BufferLength);
        MsQuicLib.ExecutionConfig = NewConfig;
        CxPlatLockRelease(&MsQuicLib.Lock);

        QuicTraceLogInfo(
            LibraryExecutionConfigSet,
            "[ lib] Setting execution config");

        Status = QUIC_STATUS_SUCCESS;
        break;
    }
#if QUIC_TEST_DATAPATH_HOOKS_ENABLED
    case QUIC_PARAM_GLOBAL_TEST_DATAPATH_HOOKS:

        if (BufferLength != sizeof(QUIC_TEST_DATAPATH_HOOKS*)) {
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }

        MsQuicLib.TestDatapathHooks = *(QUIC_TEST_DATAPATH_HOOKS**)Buffer;
        QuicTraceLogWarning(
            LibraryTestDatapathHooksSet,
            "[ lib] Updated test datapath hooks");

        Status = QUIC_STATUS_SUCCESS;
        break;
#endif

# ifdef QUIC_TEST_ALLOC_FAILURES_ENABLED
case QUIC_PARAM_GLOBAL_ALLOC_FAIL_DENOMINATOR:
    {
        if (BufferLength != sizeof(int32_t))
        {
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }
        int32_t Value;
        CxPlatCopyMemory(&Value, Buffer, sizeof(Value));
        if (Value < 0)
        {
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }
        CxPlatSetAllocFailDenominator(Value);
        Status = QUIC_STATUS_SUCCESS;
        break;
    }

case QUIC_PARAM_GLOBAL_ALLOC_FAIL_CYCLE:
    {
        if (BufferLength != sizeof(int32_t))
        {
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }
        int32_t Value;
        CxPlatCopyMemory(&Value, Buffer, sizeof(Value));
        if (Value < 0)
        {
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }
        CxPlatSetAllocFailDenominator(-Value);
        Status = QUIC_STATUS_SUCCESS;
        break;
    }
#endif

case QUIC_PARAM_GLOBAL_VERSION_NEGOTIATION_ENABLED:

    if (Buffer == NULL ||
        BufferLength < sizeof(BOOLEAN))
    {
        Status = QUIC_STATUS_INVALID_PARAMETER;
        break;
    }

    MsQuicLib.Settings.IsSet.VersionNegotiationExtEnabled = TRUE;
    MsQuicLib.Settings.VersionNegotiationExtEnabled = *(BOOLEAN*)Buffer;

    Status = QUIC_STATUS_SUCCESS;
    break;

case QUIC_PARAM_GLOBAL_STATELESS_RESET_KEY:
    if (!MsQuicLib.LazyInitComplete)
    {
        Status = QUIC_STATUS_INVALID_STATE;
        break;
    }
    if (BufferLength != QUIC_STATELESS_RESET_KEY_LENGTH * sizeof(uint8_t))
    {
        Status = QUIC_STATUS_INVALID_PARAMETER;
        break;
    }

    Status = QUIC_STATUS_SUCCESS;
    for (uint16_t i = 0; i < MsQuicLib.ProcessorCount; ++i)
    {
        CXPLAT_HASH* TokenHash = NULL;
        Status =
            CxPlatHashCreate(
                CXPLAT_HASH_SHA256,
                (uint8_t*)Buffer,
                QUIC_STATELESS_RESET_KEY_LENGTH * sizeof(uint8_t),
                &TokenHash);
        if (QUIC_FAILED(Status))
        {
            break;
        }

        QUIC_LIBRARY_PP* PerProc = &MsQuicLib.PerProc[i];
        CxPlatLockAcquire(&PerProc->ResetTokenLock);
        CxPlatHashFree(PerProc->ResetTokenHash);
        PerProc->ResetTokenHash = TokenHash;
        CxPlatLockRelease(&PerProc->ResetTokenLock);
    }
    break;

default:
    Status = QUIC_STATUS_INVALID_PARAMETER;
    break;
}

return Status;
}

        static ulong QuicLibrarySetParam(QUIC_HANDLE Handle, uint Param, int BufferLength, void* Buffer)
        {
            ulong Status;
                QUIC_REGISTRATION* Registration;
                QUIC_CONFIGURATION* Configuration;
                QUIC_LISTENER* Listener;
                QUIC_CONNECTION* Connection;
                QUIC_STREAM* Stream;

            switch (Handle->Type) {

            case QUIC_HANDLE_TYPE_REGISTRATION:
                Stream = NULL;
                Connection = NULL;
                Listener = NULL;
                Configuration = NULL;
        #pragma prefast(suppress: __WARNING_25024, "Pointer cast already validated.")
                Registration = (QUIC_REGISTRATION*) Handle;
                break;

            case QUIC_HANDLE_TYPE_CONFIGURATION:
                Stream = NULL;
                Connection = NULL;
                Listener = NULL;
        #pragma prefast(suppress: __WARNING_25024, "Pointer cast already validated.")
                Configuration = (QUIC_CONFIGURATION*) Handle;
                Registration = Configuration->Registration;
                break;

            case QUIC_HANDLE_TYPE_LISTENER:
                Stream = NULL;
                Connection = NULL;
        #pragma prefast(suppress: __WARNING_25024, "Pointer cast already validated.")
                Listener = (QUIC_LISTENER*) Handle;
                Configuration = NULL;
                Registration = Listener->Registration;
                break;

            case QUIC_HANDLE_TYPE_CONNECTION_CLIENT:
            case QUIC_HANDLE_TYPE_CONNECTION_SERVER:
                Stream = NULL;
                Listener = NULL;
        #pragma prefast(suppress: __WARNING_25024, "Pointer cast already validated.")
                Connection = (QUIC_CONNECTION*) Handle;
                Configuration = Connection->Configuration;
                Registration = Connection->Registration;
                break;

            case QUIC_HANDLE_TYPE_STREAM:
                Listener = NULL;
        #pragma prefast(suppress: __WARNING_25024, "Pointer cast already validated.")
                Stream = (QUIC_STREAM*) Handle;
                Connection = Stream->Connection;
                Configuration = Connection->Configuration;
                Registration = Connection->Registration;
                break;

            default:
                CXPLAT_TEL_ASSERT(FALSE);
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            switch (Param & 0x7F000000)
            {
            case QUIC_PARAM_PREFIX_REGISTRATION:
                if (Registration == NULL) {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                } else
        {
            Status = QuicRegistrationParamSet(Registration, Param, BufferLength, Buffer);
        }
        break;

            case QUIC_PARAM_PREFIX_CONFIGURATION:
            if (Configuration == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
            }
            else
            {
                Status = QuicConfigurationParamSet(Configuration, Param, BufferLength, Buffer);
            }
            break;

        case QUIC_PARAM_PREFIX_LISTENER:
            if (Listener == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
            }
            else
            {
                Status = QuicListenerParamSet(Listener, Param, BufferLength, Buffer);
            }
            break;

        case QUIC_PARAM_PREFIX_CONNECTION:
            if (Connection == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
            }
            else
            {
                Status = QuicConnParamSet(Connection, Param, BufferLength, Buffer);
            }
            break;

        case QUIC_PARAM_PREFIX_TLS:
        case QUIC_PARAM_PREFIX_TLS_SCHANNEL:
            if (Connection == NULL || Connection->Crypto.TLS == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
            }
            else
            {
                Status = CxPlatTlsParamSet(Connection->Crypto.TLS, Param, BufferLength, Buffer);
            }
            break;

        case QUIC_PARAM_PREFIX_STREAM:
            if (Stream == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
            }
            else
            {
                Status = QuicStreamParamSet(Stream, Param, BufferLength, Buffer);
            }
            break;

        default:
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }

        Error:

        return Status;
        }

    }
}
