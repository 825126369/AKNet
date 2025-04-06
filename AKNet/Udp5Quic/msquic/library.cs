using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Collections.Generic;
using System.Net;
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
        public long SendBatchId;
        public long SendPacketId;
        public long ReceivePacketId;
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
        public int CidServerIdLength;
        public byte CidTotalLength;
        public long ConnectionCorrelationId;
        public ulong HandshakeMemoryLimit;
        public long CurrentHandshakeMemoryUsage;
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
            CXPLAT_UDP_DATAPATH_CALLBACKS DatapathCallbacks = new CXPLAT_UDP_DATAPATH_CALLBACKS();
            DatapathCallbacks.Receive = QuicBindingReceive;
            DatapathCallbacks.Unreachable = QuicBindingUnreachable;

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

            Status = CxPlatDataPathInitialize(DatapathCallbacks, null, MsQuicLib.WorkerPool, MsQuicLib.ExecutionConfig, MsQuicLib.Datapath);
            if (QUIC_SUCCEEDED(Status))
            {

            }
            else
            {
                MsQuicLibraryFreePartitions();
                goto Exit;
            }

            NetLog.Assert(MsQuicLib.PerProc != null);
            NetLog.Assert(MsQuicLib.Datapath != null);
            MsQuicLib.LazyInitComplete = true;

        Exit:
            if (AcquireLock)
            {
                Monitor.Exit(MsQuicLib.Lock);
            }
            return Status;
        }

        static void QuicLibraryEvaluateSendRetryState()
        {
            bool NewSendRetryState = MsQuicLib.CurrentHandshakeMemoryUsage >= MsQuicLib.HandshakeMemoryLimit;
            if (NewSendRetryState != MsQuicLib.SendRetryEnabled)
            {
                MsQuicLib.SendRetryEnabled = NewSendRetryState;
            }
        }

        static void QuicLibApplyLoadBalancingSetting()
        {
            switch (MsQuicLib.Settings.LoadBalancingMode)
            {
                case QUIC_LOAD_BALANCING_MODE.QUIC_LOAD_BALANCING_DISABLED:
                default:
                    MsQuicLib.CidServerIdLength = 0;
                    break;
                case QUIC_LOAD_BALANCING_MODE.QUIC_LOAD_BALANCING_SERVER_ID_IP:    // 1 + 4 for IP address/suffix
                case QUIC_LOAD_BALANCING_MODE.QUIC_LOAD_BALANCING_SERVER_ID_FIXED: // 1 + 4 for fixed value
                    MsQuicLib.CidServerIdLength = 5;
                    break;
            }

            MsQuicLib.CidTotalLength = (byte)(MsQuicLib.CidServerIdLength + QUIC_CID_PID_LENGTH + QUIC_CID_PAYLOAD_LENGTH);

            NetLog.Assert(MsQuicLib.CidServerIdLength <= QUIC_MAX_CID_SID_LENGTH);
            NetLog.Assert(MsQuicLib.CidTotalLength >= QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH);
            NetLog.Assert(MsQuicLib.CidTotalLength <= QUIC_CID_MAX_LENGTH);
        }

        static void QuicPerfCounterSnapShot(long TimeDiffUs)
        {

        }


        static void QuicPerfCounterTrySnapShot(long TimeNow)
        {
            long TimeLast = MsQuicLib.PerfCounterSamplesTime;
            long TimeDiff = CxPlatTimeDiff64(TimeLast, TimeNow);
            if (TimeDiff < QUIC_PERF_SAMPLE_INTERVAL_S)
            {
                return;
            }

            if (TimeLast != Interlocked.CompareExchange(ref MsQuicLib.PerfCounterSamplesTime, TimeNow, TimeLast))
            {
                return;
            }
            QuicPerfCounterSnapShot(TimeDiff);
        }

        static ulong QuicLibrarySetGlobalParam(uint Param, int BufferLength, byte[] Buffer)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            QUIC_SETTINGS_INTERNAL InternalSettings = new QUIC_SETTINGS_INTERNAL();

            switch (Param)
            {
                case QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT:

                    if (BufferLength != MsQuicLib.Settings.RetryMemoryLimit)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    MsQuicLib.Settings.RetryMemoryLimit = EndianBitConverter.ToUInt16(Buffer, 0);
                    MsQuicLib.HandshakeMemoryLimit = (MsQuicLib.Settings.RetryMemoryLimit * CxPlatTotalMemory) / ushort.MaxValue;
                    QuicLibraryEvaluateSendRetryState();

                    Status = QUIC_STATUS_SUCCESS;
                    break;
                case QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE:
                    {
                        if (BufferLength != sizeof(ushort))
                        {
                            Status = QUIC_STATUS_INVALID_PARAMETER;
                            break;
                        }

                        if (EndianBitConverter.ToUInt16(Buffer, 0) > (int)QUIC_LOAD_BALANCING_MODE.QUIC_LOAD_BALANCING_SERVER_ID_IP)
                        {
                            Status = QUIC_STATUS_INVALID_PARAMETER;
                            break;
                        }

                        if (MsQuicLib.InUse && (int)MsQuicLib.Settings.LoadBalancingMode != EndianBitConverter.ToUInt16(Buffer, 0))
                        {
                            Status = QUIC_STATUS_INVALID_STATE;
                            break;
                        }

                        MsQuicLib.Settings.LoadBalancingMode = (QUIC_LOAD_BALANCING_MODE)EndianBitConverter.ToUInt16(Buffer, 0);
                        MsQuicLib.Settings.IsSet.LoadBalancingMode = true;

                        QuicLibApplyLoadBalancingSetting();
                        Status = QUIC_STATUS_SUCCESS;
                        break;
                    }

                case QUIC_PARAM_GLOBAL_SETTINGS:

                    if (Buffer == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    Status = QuicSettingsSettingsToInternal(BufferLength, (QUIC_SETTINGS*)Buffer, InternalSettings);
                    if (QUIC_FAILED(Status))
                    {
                        break;
                    }

                    if (!QuicSettingApply(MsQuicLib.Settings, true, true, InternalSettings))
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

                    if (Buffer == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    Status = QuicSettingsGlobalSettingsToInternal(BufferLength, (QUIC_GLOBAL_SETTINGS*)Buffer, InternalSettings);
                    if (QUIC_FAILED(Status))
                    {
                        break;
                    }

                    if (!QuicSettingApply(MsQuicLib.Settings, true, true, InternalSettings))
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    if (QUIC_SUCCEEDED(Status))
                    {
                        MsQuicLibraryOnSettingsChanged(true);
                    }

                    break;

                case QUIC_PARAM_GLOBAL_VERSION_SETTINGS:

                    if (Buffer == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    Status = QuicSettingsVersionSettingsToInternal(BufferLength, (QUIC_VERSION_SETTINGS)Buffer, InternalSettings);
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
            QUIC_REGISTRATION Registration;
            QUIC_CONFIGURATION Configuration;
            QUIC_LISTENER Listener;
            QUIC_CONNECTION Connection;
            QUIC_STREAM Stream;

            switch (Handle.Type)
            {

                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION:
                    Stream = null;
                    Connection = null;
                    Listener = null;
                    Configuration = null;
                    Registration = (QUIC_REGISTRATION)Handle;
                    break;
                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION:
                    Stream = null;
                    Connection = null;
                    Listener = null;
                    Configuration = (QUIC_CONFIGURATION)Handle;
                    Registration = Configuration.Registration;
                    break;
                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER:
                    Stream = null;
                    Connection = null;
                    Listener = (QUIC_LISTENER)Handle;
                    Configuration = null;
                    Registration = Listener.Registration;
                    break;

                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT:
                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER:
                    Stream = null;
                    Listener = null;
                    Connection = (QUIC_CONNECTION)Handle;
                    Configuration = Connection.Configuration;
                    Registration = Connection.Registration;
                    break;

                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_STREAM:
                    Listener = null;
                    Stream = (QUIC_STREAM)Handle;
                    Connection = Stream.Connection;
                    Configuration = Connection.Configuration;
                    Registration = Connection.Registration;
                    break;

                default:
                    NetLog.Assert(false);
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Error;
            }

            switch (Param & 0x7F000000)
            {
                case QUIC_PARAM_PREFIX_REGISTRATION:
                    if (Registration == null) {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    } else
                    {
                        Status = QuicRegistrationParamSet(Registration, Param, BufferLength, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_CONFIGURATION:
                    if (Configuration == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicConfigurationParamSet(Configuration, Param, BufferLength, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_LISTENER:
                    if (Listener == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicListenerParamSet(Listener, Param, BufferLength, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_CONNECTION:
                    if (Connection == null)
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
                    if (Connection == null || Connection.Crypto.TLS == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = CxPlatTlsParamSet(Connection.Crypto.TLS, Param, BufferLength, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_STREAM:
                    if (Stream == null)
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

        static void QuicLibraryOnHandshakeConnectionRemoved()
        {
            Interlocked.Add(ref MsQuicLib.CurrentHandshakeMemoryUsage, -1 * 10000);
            QuicLibraryEvaluateSendRetryState();
        }

        static QUIC_WORKER QuicLibraryGetWorker(QUIC_RX_PACKET Packet)
        {
            NetLog.Assert(MsQuicLib.StatelessRegistration != null);
            return MsQuicLib.StatelessRegistration.WorkerPool.Workers[Packet.PartitionIndex % MsQuicLib.StatelessRegistration.WorkerPool.Workers.Count];
        }

        static void QuicLibraryReleaseBinding(QUIC_BINDING Binding)
        {
            bool Uninitialize = false;
            CxPlatDispatchLockAcquire(MsQuicLib.DatapathLock);
            NetLog.Assert(Binding.RefCount > 0);
            if (--Binding.RefCount == 0)
            {
                CxPlatListEntryRemove(Binding.Link);
                Uninitialize = true;

                if (CxPlatListIsEmpty(MsQuicLib.Bindings))
                {
                    MsQuicLib.InUse = false;
                }
            }
            CxPlatDispatchLockRelease(MsQuicLib.DatapathLock);
            if (Uninitialize)
            {
                QuicBindingUninitialize(Binding);
            }
        }

        static CXPLAT_KEY QuicLibraryGetStatelessRetryKeyForTimestamp(long Timestamp)
        {
            if (Timestamp < MsQuicLib.StatelessRetryKeysExpiration[!MsQuicLib.CurrentStatelessRetryKey] - QUIC_STATELESS_RETRY_KEY_LIFETIME_MS)
            {
                return null;
            }

            if (Timestamp < MsQuicLib.StatelessRetryKeysExpiration[!MsQuicLib.CurrentStatelessRetryKey])
            {
                if (MsQuicLib.StatelessRetryKeys[!MsQuicLib.CurrentStatelessRetryKey] == null)
                {
                    return null;
                }
                return MsQuicLib.StatelessRetryKeys[!MsQuicLib.CurrentStatelessRetryKey];
            }

            if (Timestamp < MsQuicLib.StatelessRetryKeysExpiration[MsQuicLib.CurrentStatelessRetryKey])
            {
                if (MsQuicLib.StatelessRetryKeys[MsQuicLib.CurrentStatelessRetryKey] == null)
                {
                    return null;
                }
                return MsQuicLib.StatelessRetryKeys[MsQuicLib.CurrentStatelessRetryKey];
            }
            return null;
        }

        static bool QuicLibraryTryAddRefBinding(QUIC_BINDING Binding)
        {
            bool Success = false;
            CxPlatDispatchLockAcquire(MsQuicLib.DatapathLock);
            if (Binding.RefCount > 0)
            {
                Binding.RefCount++;
                Success = true;
            }
            CxPlatDispatchLockRelease(MsQuicLib.DatapathLock);
            return Success;
        }

        static void QuicLibraryOnHandshakeConnectionAdded()
        {
            Interlocked.Add(ref MsQuicLib.CurrentHandshakeMemoryUsage, 1);
            QuicLibraryEvaluateSendRetryState();
        }

        static QUIC_BINDING QuicLibraryLookupBinding(IPEndPoint LocalAddress, IPEndPoint RemoteAddress)
        {
            for (CXPLAT_LIST_ENTRY Link = MsQuicLib.Bindings.Flink; Link != MsQuicLib.Bindings; Link = Link.Flink)
            {
                QUIC_BINDING Binding = CXPLAT_CONTAINING_RECORD<QUIC_BINDING>(Link);
                IPEndPoint BindingLocalAddr = null;
                QuicBindingGetLocalAddress(Binding, ref BindingLocalAddr);
                if (Binding.Connected)
                {
                    if (RemoteAddress != null && LocalAddress == BindingLocalAddr)
                    {
                        IPEndPoint BindingRemoteAddr = null;
                        QuicBindingGetRemoteAddress(Binding, ref BindingRemoteAddr);
                        if (RemoteAddress == BindingRemoteAddr)
                        {
                            return Binding;
                        }
                    }
                }
                else
                {
                    if (QuicAddrGetPort(BindingLocalAddr) == QuicAddrGetPort(LocalAddress))
                    {
                        return Binding;
                    }
                }
            }
            return null;
        }

        static ulong QuicLibraryGetBinding(CXPLAT_UDP_CONFIG UdpConfig, ref QUIC_BINDING NewBinding)
        {
            ulong Status;
            QUIC_BINDING Binding;
            IPEndPoint NewLocalAddress;
            bool PortUnspecified = UdpConfig.LocalAddress == null || QuicAddrGetPort(UdpConfig.LocalAddress) == 0;
            bool ShareBinding = BoolOk(UdpConfig.Flags & CXPLAT_SOCKET_FLAG_SHARE);
            bool ServerOwned = BoolOk(UdpConfig.Flags & CXPLAT_SOCKET_SERVER_OWNED);


            if (PortUnspecified)
            {
                goto NewBinding;
            }

            Status = QUIC_STATUS_NOT_FOUND;
            CxPlatDispatchLockAcquire(MsQuicLib.DatapathLock);

            Binding = QuicLibraryLookupBinding(UdpConfig.LocalAddress, UdpConfig.RemoteAddress);
            if (Binding != null)
            {
                if (!ShareBinding || Binding.Exclusive || (ServerOwned != Binding.ServerOwned))
                {
                    Status = QUIC_STATUS_ADDRESS_IN_USE;
                }
                else
                {
                    NetLog.Assert(Binding.RefCount > 0);
                    Binding.RefCount++;
                    NewBinding = Binding;
                    Status = QUIC_STATUS_SUCCESS;
                }
            }

            CxPlatDispatchLockRelease(MsQuicLib.DatapathLock);

            if (Status != QUIC_STATUS_NOT_FOUND)
            {
                goto Exit;
            }

        NewBinding:

            Status = QuicBindingInitialize(UdpConfig, NewBinding);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            QuicBindingGetLocalAddress(NewBinding, NewLocalAddress);
            CxPlatDispatchLockAcquire(MsQuicLib.DatapathLock);
            if (CxPlatDataPathGetSupportedFeatures(MsQuicLib.Datapath) & CXPLAT_DATAPATH_FEATURE_LOCAL_PORT_SHARING)
            {
                Binding = QuicLibraryLookupBinding(NewLocalAddress, UdpConfig.RemoteAddress);
            }
            else
            {
                Binding = QuicLibraryLookupBinding(NewLocalAddress, null);
            }

            if (Binding != null)
            {
                if (!PortUnspecified && !Binding.Exclusive)
                {
                    NetLog.Assert(Binding.RefCount > 0);
                    Binding.RefCount++;
                }
            }
            else
            {
                if (CxPlatListIsEmpty(MsQuicLib.Bindings))
                {
                    MsQuicLib.InUse = true;
                }
                NewBinding.RefCount++;
                CxPlatListInsertTail(MsQuicLib.Bindings, NewBinding.Link);
            }

            CxPlatDispatchLockRelease(MsQuicLib.DatapathLock);

            if (Binding != null)
            {
                if (PortUnspecified)
                {
                    QuicBindingUninitialize(NewBinding);
                    NewBinding = null;
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                }
                else if (Binding.Exclusive)
                {
                    QuicBindingUninitialize(NewBinding);
                    NewBinding = null;
                    Status = QUIC_STATUS_ADDRESS_IN_USE;

                }
                else
                {
                    QuicBindingUninitialize(NewBinding);
                    NewBinding = Binding;
                    Status = QUIC_STATUS_SUCCESS;
                }
            }

        Exit:
            return Status;
        }
    }
}
