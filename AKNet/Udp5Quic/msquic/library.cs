using AKNet.Common;
using System;
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
        //void* ClientContext;
    }

    internal class QUIC_LIBRARY
    {
        public bool Loaded;
        public bool LazyInitComplete;
        public bool IsVerifying;
        public bool InUse;
        public bool SendRetryEnabled;
        public bool CurrentStatelessRetryKey;
        public uint[] Version = new uint[4];
        public string GitHash;

        //
        // Configurable (app & registry) settings.
        //
        QUIC_SETTINGS_INTERNAL Settings;
        public Monitor Lock;

        //
        // Controls access to all datapath internal state of the library.
        //
        CXPLAT_DISPATCH_LOCK DatapathLock;

        //
        // Total outstanding references from calls to MsQuicLoadLibrary.
        //
        volatile short LoadRefCount;

        //
        // Total outstanding references from calls to MsQuicOpenVersion.
        //
        public ushort OpenRefCount;
        public ushort ProcessorCount;
        public ushort PartitionCount;
        public ushort PartitionMask;
        public long ConnectionCount;
        public byte TimerResolutionMs;
        public byte CidServerIdLength;
        public byte CidTotalLength;
        public ulong ConnectionCorrelationId;
        public ulong HandshakeMemoryLimit;
        public ulong CurrentHandshakeMemoryUsage;

        //
        // Handle to global persistent storage (registry).
        //
        CXPLAT_STORAGE* Storage;

        //
        // Configuration for execution of the library (optionally set by the app).
        //
        QUIC_EXECUTION_CONFIG* ExecutionConfig;

        //
        // Datapath instance for the library.
        //
        CXPLAT_DATAPATH* Datapath;

        //
        // List of all registrations in the current process (or kernel).
        //
        CXPLAT_LIST_ENTRY Registrations;

        //
        // List of all UDP bindings in the current process (or kernel).
        //
        CXPLAT_LIST_ENTRY Bindings;

        //
        // Contains all (server) connections currently not in an app's registration.
        //
        QUIC_REGISTRATION* StatelessRegistration;

        //
        // Per-processor storage. Count of `ProcessorCount`.
        //
        _Field_size_(ProcessorCount)
    QUIC_LIBRARY_PP* PerProc;

        //
        // Controls access to the stateless retry keys when rotated.
        //
        CXPLAT_DISPATCH_LOCK StatelessRetryKeysLock;

        //
        // Keys used for encryption of stateless retry tokens.
        //
        CXPLAT_KEY* StatelessRetryKeys[2];

        //
        // Timestamp when the current stateless retry key expires.
        //
        int64_t StatelessRetryKeysExpiration[2];

        //
        // The Toeplitz hash used for hashing received long header packets.
        //
        CXPLAT_TOEPLITZ_HASH ToeplitzHash;

#if QUIC_TEST_DATAPATH_HOOKS_ENABLED
    //
    // An optional callback to allow test code to modify the data path.
    //
    QUIC_TEST_DATAPATH_HOOKS* TestDatapathHooks;
#endif

        //
        // Default client compatibility list. Use for connections that don't
        // specify a custom list. Generated for QUIC_VERSION_LATEST
        //
        const uint32_t* DefaultCompatibilityList;
        uint32_t DefaultCompatibilityListLength;

        //
        // Last sample of the performance counters
        //
        uint64_t PerfCounterSamplesTime;
        int64_t PerfCounterSamples[QUIC_PERF_COUNTER_MAX];

        //
        // The worker pool
        //
        CXPLAT_WORKER_POOL WorkerPool;

    }

    internal static partial class MSQuicFunc
    {
        static QUIC_LIBRARY MsQuicLib = new QUIC_LIBRARY();
        static long QuicLibraryInitializePartitions()
        {
            NetLog.Assert(MsQuicLib.PerProc == null);
            MsQuicLib.ProcessorCount = Environment.ProcessorCount;
            NetLog.Assert(MsQuicLib.ProcessorCount > 0);

            if (MsQuicLib.ExecutionConfig && MsQuicLib.ExecutionConfig->ProcessorCount)
            {
                MsQuicLib.PartitionCount = (uint16_t)MsQuicLib.ExecutionConfig->ProcessorCount;
            }
            else
            {
                MsQuicLib.PartitionCount = MsQuicLib.ProcessorCount;

                uint MaxPartitionCount = QUIC_MAX_PARTITION_COUNT;
                if (MsQuicLib.Storage != NULL)
                {
                    uint MaxPartitionCountLen = sizeof(MaxPartitionCount);
                    CxPlatStorageReadValue(
                        MsQuicLib.Storage,
                        QUIC_SETTING_MAX_PARTITION_COUNT,
                        (uint8_t*)&MaxPartitionCount,
                        &MaxPartitionCountLen);
                    if (MaxPartitionCount == 0)
                    {
                        MaxPartitionCount = QUIC_MAX_PARTITION_COUNT;
                    }
                }
                if (MsQuicLib.PartitionCount > MaxPartitionCount)
                {
                    MsQuicLib.PartitionCount = (uint16_t)MaxPartitionCount;
                }
            }

            MsQuicCalculatePartitionMask();

            int PerProcSize = MsQuicLib.ProcessorCount * sizeof(QUIC_LIBRARY_PP);
            MsQuicLib.PerProc = CXPLAT_ALLOC_NONPAGED(PerProcSize, QUIC_POOL_PERPROC);
            if (MsQuicLib.PerProc == null)
            {
                QuicTraceEvent(
                    AllocFailure,
                    "Allocation of '%s' failed. (%llu bytes)",
                    "connection pools",
                    PerProcSize);
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

            CxPlatZeroMemory(MsQuicLib.PerProc, PerProcSize);
            for (ushort i = 0; i < MsQuicLib.ProcessorCount; ++i)
            {
                QUIC_LIBRARY_PP* PerProc = &MsQuicLib.PerProc[i];
                CxPlatPoolInitialize(FALSE, sizeof(QUIC_CONNECTION), QUIC_POOL_CONN, &PerProc->ConnectionPool);
                CxPlatPoolInitialize(FALSE, sizeof(QUIC_TRANSPORT_PARAMETERS), QUIC_POOL_TP, &PerProc->TransportParamPool);
                CxPlatPoolInitialize(FALSE, sizeof(QUIC_PACKET_SPACE), QUIC_POOL_TP, &PerProc->PacketSpacePool);
                CxPlatLockInitialize(&PerProc->ResetTokenLock);
            }

            uint8_t ResetHashKey[20];
            CxPlatRandom(sizeof(ResetHashKey), ResetHashKey);
            for (uint16_t i = 0; i < MsQuicLib.ProcessorCount; ++i)
            {
                QUIC_LIBRARY_PP* PerProc = &MsQuicLib.PerProc[i];
                QUIC_STATUS Status =
                    CxPlatHashCreate(
                        CXPLAT_HASH_SHA256,
                        ResetHashKey,
                        sizeof(ResetHashKey),
                        &PerProc->ResetTokenHash);
                if (QUIC_FAILED(Status))
                {
                    CxPlatSecureZeroMemory(ResetHashKey, sizeof(ResetHashKey));
                    MsQuicLibraryFreePartitions();
                    return Status;
                }
            }
            CxPlatSecureZeroMemory(ResetHashKey, sizeof(ResetHashKey));
            return QUIC_STATUS_SUCCESS;
        }

        public long QuicLibraryLazyInitialize(bool AcquireLock)
        {
            CXPLAT_UDP_DATAPATH_CALLBACKS DatapathCallbacks =
            {
                QuicBindingReceive,
                QuicBindingUnreachable,
            };

            long Status = QUIC_STATUS_SUCCESS;
            if (AcquireLock)
            {
                CxPlatLockAcquire(&MsQuicLib.Lock);
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
                CxPlatLockRelease(&MsQuicLib.Lock);
            }

            return Status;
        }
    }
}
