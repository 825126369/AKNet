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
        //void* ClientContext;
    }

    internal class QUIC_LIBRARY_PP
    {
        public CXPLAT_POOL ConnectionPool;
        public CXPLAT_POOL TransportParamPool;
        public CXPLAT_POOL PacketSpacePool;
        public string ResetTokenHash;
        public Monitor ResetTokenLock;

        public ulong SendBatchId;
        public ulong SendPacketId;
        public ulong ReceivePacketId;
        public long[] PerfCounters = new long[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX];
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
        public readonly object Lock = new object();

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
        public CXPLAT_STORAGE Storage;
        public QUIC_EXECUTION_CONFIG ExecutionConfig;
        public CXPLAT_DATAPATH Datapath;
        public CXPLAT_LIST_ENTRY Registrations;
        public CXPLAT_LIST_ENTRY Bindings;

        //
        // Contains all (server) connections currently not in an app's registration.
        //
        QUIC_REGISTRATION* StatelessRegistration;

        //
        // Per-processor storage. Count of `ProcessorCount`.
        //
        public List<QUIC_LIBRARY_PP> PerProc = new List<QUIC_LIBRARY_PP>();

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

        public void MsQuicLibraryLoad()
        {
            if (InterlockedIncrement16(&MsQuicLib.LoadRefCount) == 1)
            {
                CxPlatSystemLoad();
                CxPlatLockInitialize(&MsQuicLib.Lock);
                CxPlatDispatchLockInitialize(&MsQuicLib.DatapathLock);
                CxPlatDispatchLockInitialize(&MsQuicLib.StatelessRetryKeysLock);
                CxPlatListInitializeHead(&MsQuicLib.Registrations);
                CxPlatListInitializeHead(&MsQuicLib.Bindings);
                QuicTraceRundownCallback = QuicTraceRundown;
                MsQuicLib.Loaded = TRUE;
                MsQuicLib.Version[0] = VER_MAJOR;
                MsQuicLib.Version[1] = VER_MINOR;
                MsQuicLib.Version[2] = VER_PATCH;
                MsQuicLib.Version[3] = VER_BUILD_ID;
                MsQuicLib.GitHash = VER_GIT_HASH_STR;
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

        static long QuicLibraryInitializePartitions()
        {
            NetLog.Assert(MsQuicLib.PerProc == null);
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

            int PerProcSize = MsQuicLib.ProcessorCount * sizeof(QUIC_LIBRARY_PP);
            MsQuicLib.PerProc = new List<QUIC_LIBRARY_PP>();
            for (ushort i = 0; i < MsQuicLib.ProcessorCount; ++i)
            {
                QUIC_LIBRARY_PP PerProc = MsQuicLib.PerProc[i];
                CxPlatPoolInitialize(false, sizeof(QUIC_CONNECTION), QUIC_POOL_CONN, PerProc.ConnectionPool);
                CxPlatPoolInitialize(false, sizeof(QUIC_TRANSPORT_PARAMETERS), QUIC_POOL_TP, &PerProc->TransportParamPool);
                CxPlatPoolInitialize(false, sizeof(QUIC_PACKET_SPACE), QUIC_POOL_TP, &PerProc->PacketSpacePool);
            }

            byte[] ResetHashKey = new byte[20];
            CxPlatRandom(ResetHashKey.Length, ResetHashKey);
            for (ushort i = 0; i < MsQuicLib.ProcessorCount; ++i)
            {
                QUIC_LIBRARY_PP PerProc = MsQuicLib.PerProc[i];
                long Status = CxPlatHashCreate(CXPLAT_HASH_SHA256, ResetHashKey, ResetHashKey.Length, PerProc.ResetTokenHash);
                if (QUIC_FAILED(Status))
                {
                    CxPlatSecureZeroMemory(ResetHashKey, sizeof(ResetHashKey));
                    MsQuicLibraryFreePartitions();
                    return Status;
                }
            }
            return QUIC_STATUS_SUCCESS;
        }

        public static long QuicLibraryLazyInitialize(bool AcquireLock)
        {
            CXPLAT_UDP_DATAPATH_CALLBACKS DatapathCallbacks =
            {
                QuicBindingReceive,
                QuicBindingUnreachable,
            };

            long Status = QUIC_STATUS_SUCCESS;
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
    }
}
