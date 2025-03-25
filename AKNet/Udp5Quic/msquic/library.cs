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
        void* ClientContext;
    }

    internal class QUIC_LIBRARY_PP
    {
        public ObjectPool<QUIC_CONNECTION> ConnectionPool;
        public ObjectPool<QUIC_TRANSPORT_PARAMETERS> TransportParamPool;
        public ObjectPool<QUIC_RX_PACKET> PacketSpacePool;

        public string ResetTokenHash;
        public Monitor ResetTokenLock;
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
        public ulong ConnectionCorrelationId;
        public ulong HandshakeMemoryLimit;
        public ulong CurrentHandshakeMemoryUsage;
        public CXPLAT_STORAGE Storage;
        public QUIC_EXECUTION_CONFIG ExecutionConfig;
        public CXPLAT_DATAPATH Datapath;

        public CXPLAT_LIST_ENTRY Registrations;
        public CXPLAT_LIST_ENTRY Bindings;

        public QUIC_REGISTRATION StatelessRegistration;
        public readonly List<QUIC_LIBRARY_PP> PerProc = new List<QUIC_LIBRARY_PP>();
        public string[] StatelessRetryKeys = new string[2];
        public long StatelessRetryKeysExpiration = new long[2];
        //CXPLAT_TOEPLITZ_HASH ToeplitzHash;

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

        static long QuicLibraryInitializePartitions()
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
