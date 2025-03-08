using System;
using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LIBRARY
    {
        public bool Loaded = false;
        public bool LazyInitComplete : 1;
        public bool IsVerifying : 1;
        public bool InUse;
        public bool SendRetryEnabled;
        public bool CurrentStatelessRetryKey;
        public uint Version[4];
        public string GitHash;
        
        QUIC_SETTINGS_INTERNAL Settings;
        CXPLAT_LOCK Lock;
        CXPLAT_DISPATCH_LOCK DatapathLock;
        public short LoadRefCount;
        public ushort OpenRefCount;
        public ushort ProcessorCount;
        public ushort PartitionCount;
        public ushort PartitionMask;
        public long ConnectionCount;
        public uint TimerResolutionMs;
        public byte CidServerIdLength;
        public byte CidTotalLength;
        public UInt64 ConnectionCorrelationId;
        public ulong HandshakeMemoryLimit;
        public ulong CurrentHandshakeMemoryUsage;
        CXPLAT_STORAGE* Storage;
        QUIC_EXECUTION_CONFIG* ExecutionConfig;
        CXPLAT_DATAPATH* Datapath;
        CXPLAT_LIST_ENTRY Registrations;
        CXPLAT_LIST_ENTRY Bindings;
        QUIC_REGISTRATION* StatelessRegistration;
        QUIC_LIBRARY_PP* PerProc;
        CXPLAT_DISPATCH_LOCK StatelessRetryKeysLock;
        CXPLAT_KEY* StatelessRetryKeys[2];
        public long StatelessRetryKeysExpiration[2];
        CXPLAT_TOEPLITZ_HASH ToeplitzHash;
        public List<uint> DefaultCompatibilityList;
        uint64_t PerfCounterSamplesTime;
        int64_t PerfCounterSamples[QUIC_PERF_COUNTER_MAX];
        CXPLAT_WORKER_POOL WorkerPool;
    }

    internal static partial class MSQuicFunc
    {
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
    }
}
