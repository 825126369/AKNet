/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MSQuic2
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
        public object ClientContext;
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
        public readonly QUIC_SETTINGS Settings = new QUIC_SETTINGS();
        public readonly object Lock = new object();
        public readonly object DatapathLock = new object();
        public readonly object StatelessRetryKeysLock = new object();

        public int LoadRefCount;
        public int OpenRefCount;
        public int ProcessorCount;
        public int ConnectionCount;
        public int CidServerIdLength;
        public byte CidTotalLength;
        public long ConnectionCorrelationId;
        public long HandshakeMemoryLimit;
        public long CurrentHandshakeMemoryUsage;
        public QUIC_GLOBAL_EXECUTION_CONFIG ExecutionConfig;
        public CXPLAT_DATAPATH Datapath;

        public bool CustomExecutions;
        public bool CustomPartitions;
        public int PartitionCount;
        public int PartitionMask;
        public QUIC_PARTITION[] Partitions;
        public readonly byte[] BaseRetrySecret = new byte[(int)CXPLAT_AEAD_TYPE_SIZE.CXPLAT_AEAD_AES_256_GCM_SIZE];

        public long TimerResolutionMs;
        public long PerfCounterSamplesTime;

        public readonly CXPLAT_LIST_ENTRY Registrations = new CXPLAT_LIST_ENTRY<QUIC_REGISTRATION>(null);
        public readonly CXPLAT_LIST_ENTRY Bindings = new CXPLAT_LIST_ENTRY<QUIC_BINDING>(null);
        
        public QUIC_REGISTRATION StatelessRegistration; //无状态注册实例，用于执行无状态的关闭操作。
        public readonly CXPLAT_KEY[] StatelessRetryKeys = new CXPLAT_KEY[0];
        public readonly long[] StatelessRetryKeysExpiration = new long[2];

        public readonly List<uint> DefaultCompatibilityList = new List<uint>();
        public readonly long[] PerfCounterSamples = new long[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX];
        public CXPLAT_WORKER_POOL WorkerPool = null;
        public readonly CXPLAT_TOEPLITZ_HASH ToeplitzHash = new CXPLAT_TOEPLITZ_HASH();
    }

    internal static partial class MSQuicFunc
    {
        static readonly QUIC_LIBRARY MsQuicLib = new QUIC_LIBRARY();

        static QUIC_PARTITION QuicLibraryGetPartitionFromProcessorIndex(int ProcessorIndex)
        {
            NetLog.Assert(MsQuicLib.Partitions != null);
            if (MsQuicLib.CustomPartitions)
            {
                for (int i = 0; i < MsQuicLib.PartitionCount; ++i)
                {
                    if (ProcessorIndex <= MsQuicLib.Partitions[i].Processor)
                    {
                        return MsQuicLib.Partitions[i];
                    }
                }
                return MsQuicLib.Partitions[MsQuicLib.PartitionCount - 1];
            }
            return MsQuicLib.Partitions[ProcessorIndex % MsQuicLib.PartitionCount];
        }

        static QUIC_PARTITION QuicLibraryGetCurrentPartition()
        {
            int CurrentProc = CxPlatProcCurrentNumber();
            return QuicLibraryGetPartitionFromProcessorIndex(CurrentProc);
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

        static int QuicLibraryInitializePartitions()
        {
            NetLog.Assert(MsQuicLib.Partitions == null);
            MsQuicLib.PartitionCount = CxPlatProcCount();
            NetLog.Assert(MsQuicLib.PartitionCount > 0);

            List<int> ProcessorList = null;
            if (!_KERNEL_MODE && MsQuicLib.WorkerPool != null)
            {
                MsQuicLib.CustomPartitions = true;
                MsQuicLib.PartitionCount = CxPlatWorkerPoolGetCount(MsQuicLib.WorkerPool);
            }
            else if (MsQuicLib.ExecutionConfig != null && MsQuicLib.ExecutionConfig.ProcessorList.Count > 0 &&
                        MsQuicLib.ExecutionConfig.ProcessorList.Count != MsQuicLib.PartitionCount)
            {
                MsQuicLib.CustomPartitions = true;
                MsQuicLib.PartitionCount = MsQuicLib.ExecutionConfig.ProcessorList.Count;
                ProcessorList = MsQuicLib.ExecutionConfig.ProcessorList;
            }
            else
            {
                MsQuicLib.CustomPartitions = false;
                int MaxPartitionCount = QUIC_MAX_PARTITION_COUNT;
                if (MsQuicLib.PartitionCount > MaxPartitionCount)
                {
                    MsQuicLib.PartitionCount = MaxPartitionCount;
                }
            }
            
            NetLog.Assert(MsQuicLib.PartitionCount > 0);
            MsQuicCalculatePartitionMask();

            MsQuicLib.Partitions = new QUIC_PARTITION[MsQuicLib.PartitionCount];
            if (MsQuicLib.Partitions == null)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

            byte[] ResetHashKey = new byte[20];
            CxPlatRandom.Random(ResetHashKey);
            CxPlatRandom.Random(MsQuicLib.BaseRetrySecret);
            int Status;
            int i = 0;
            for (i = 0; i < MsQuicLib.PartitionCount; ++i)
            {
                int nProcessorId = -1;
                if (!_KERNEL_MODE)
                {
                    if (ProcessorList != null)
                    {
                        nProcessorId = ProcessorList[i];
                    }
                    else
                    {
                        if (MsQuicLib.CustomPartitions)
                        {
                            nProcessorId = CxPlatWorkerPoolGetIdealProcessor(MsQuicLib.WorkerPool, i);
                        }
                        else
                        {
                            nProcessorId = i;
                        }
                    }
                }
                else
                {
                    nProcessorId = ProcessorList != null ? ProcessorList[i] : i;
                }

                MsQuicLib.Partitions[i] = new QUIC_PARTITION();
                Status = QuicPartitionInitialize(MsQuicLib.Partitions[i], i, nProcessorId, CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256, ResetHashKey);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }
            }

            ResetHashKey.AsSpan().Clear();
            return QUIC_STATUS_SUCCESS;
        Error:
            ResetHashKey.AsSpan().Clear();
            for (int j = 0; j < i; ++j)
            {
                QuicPartitionUninitialize(MsQuicLib.Partitions[j]);
            }
            MsQuicLib.Partitions = null;
            return Status;
        }

        static void MsQuicLibraryFreePartitions()
        {
            if (MsQuicLib.Partitions != null)
            {
                for (int i = 0; i < MsQuicLib.PartitionCount; ++i)
                {
                    QuicPartitionUninitialize(MsQuicLib.Partitions[i]);
                }
                MsQuicLib.Partitions = null;
            }
        }

        public static int QuicLibraryLazyInitialize(bool AcquireLock)
        {
            CXPLAT_UDP_DATAPATH_CALLBACKS DatapathCallbacks = new CXPLAT_UDP_DATAPATH_CALLBACKS();
            DatapathCallbacks.Receive = QuicBindingReceive;
            DatapathCallbacks.Unreachable = QuicBindingUnreachable;

            int Status = QUIC_STATUS_SUCCESS;
            bool CreatedWorkerPool = false;

            if (AcquireLock)
            {
                CxPlatLockAcquire(MsQuicLib.Lock);
            }

            if (MsQuicLib.LazyInitComplete)
            {
                goto Exit;
            }

            NetLog.Assert(MsQuicLib.Partitions == null);
            NetLog.Assert(MsQuicLib.Datapath == null);

            Status = QuicLibraryInitializePartitions();
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            if (!_KERNEL_MODE)
            {
                if (MsQuicLib.WorkerPool == null)
                {
                    MsQuicLib.WorkerPool = CxPlatWorkerPoolCreate(MsQuicLib.ExecutionConfig);
                    if (MsQuicLib.WorkerPool == null)
                    {
                        Status = QUIC_STATUS_OUT_OF_MEMORY;
                        MsQuicLibraryFreePartitions();
                        goto Exit;
                    }
                    CreatedWorkerPool = true;
                }
            }

            Status = CxPlatDataPathInitialize(DatapathCallbacks,  MsQuicLib.WorkerPool, out MsQuicLib.Datapath);
            if (QUIC_SUCCEEDED(Status))
            {

            }
            else
            {
                MsQuicLibraryFreePartitions();
                if (!_KERNEL_MODE)
                { 
                    if (CreatedWorkerPool)
                    {
                        CxPlatWorkerPoolDelete(MsQuicLib.WorkerPool);
                        MsQuicLib.WorkerPool = null;
                    }
                }
                goto Exit;
            }

            NetLog.Assert(MsQuicLib.Partitions != null);
            NetLog.Assert(MsQuicLib.Datapath != null);
            MsQuicLib.LazyInitComplete = true;

        Exit:
            if (AcquireLock)
            {
                CxPlatLockRelease(MsQuicLib.Lock); ;
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

        static void QuicLibrarySumPerfCounters(Span<long> Counters)
        {
            if (MsQuicLib.Partitions == null)
            {
                Counters.Clear();
                return;
            }

            for (int ProcIndex = 1; ProcIndex < MsQuicLib.PartitionCount; ++ProcIndex)
            {
                for (int CounterIndex = 0; CounterIndex < Counters.Length; ++CounterIndex)
                {
                    //先前的统计都是 分区单独统计的，这里把加起来，算个总的
                    Counters[CounterIndex] += MsQuicLib.Partitions[ProcIndex].PerfCounters[CounterIndex];
                }
            }
            
            //for (int CounterIndex = 0; CounterIndex < Counters.Length; ++CounterIndex)
            //{
            //    if (Counters[CounterIndex] < 0)
            //    {
            //        Counters[CounterIndex] = 0;
            //    }
            //}
        }

        public static void QuicPerfCounterSnapShot(long TimeDiffUs)
        {
            TimeDiffUs = Math.Max(1, TimeDiffUs);

            long[] PerfCounterSamples = new long[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX];
            QuicLibrarySumPerfCounters(PerfCounterSamples);

            void QUIC_COUNTER_LIMIT_HZ(QUIC_PERFORMANCE_COUNTERS TYPE, long LIMIT_PER_SECOND)
            {
                NetLog.Assert(((1000 * 1000 * (PerfCounterSamples[(int)TYPE] - MsQuicLib.PerfCounterSamples[(int)TYPE])) / TimeDiffUs) < LIMIT_PER_SECOND);
            }

            void QUIC_COUNTER_CAP(QUIC_PERFORMANCE_COUNTERS TYPE, long MAX_LIMIT)
            {
                NetLog.Assert(PerfCounterSamples[(int)TYPE] < MAX_LIMIT && MsQuicLib.PerfCounterSamples[(int)TYPE] < MAX_LIMIT);
            }

#if DEBUG
            QUIC_COUNTER_LIMIT_HZ(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_HANDSHAKE_FAIL, 1000000); // Don't have 1 million failed handshakes per second
            QUIC_COUNTER_CAP(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH, 100000); // Don't maintain huge queue depths
#endif

            Array.Copy(PerfCounterSamples, MsQuicLib.PerfCounterSamples, PerfCounterSamples.Length);
            QuicPrintPerfCounters();
        }

        static void QuicPerfCounterTrySnapShot(long TimeNow)
        {
            long TimeLast = MsQuicLib.PerfCounterSamplesTime;
            long TimeDiff = CxPlatTimeDiff(TimeLast, TimeNow);
            if (TimeDiff < S_TO_US(QUIC_PERF_SAMPLE_INTERVAL_S))
            {
                return;
            }

            if (TimeLast != Interlocked.CompareExchange(ref MsQuicLib.PerfCounterSamplesTime, TimeNow, TimeLast))
            {
                return;
            }
            QuicPerfCounterSnapShot(TimeDiff);
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
            Interlocked.Add(ref MsQuicLib.CurrentHandshakeMemoryUsage, QUIC_CONN_HANDSHAKE_MEMORY_USAGE);
            QuicLibraryEvaluateSendRetryState();
        }

        static void QuicLibraryOnHandshakeConnectionRemoved()
        {
            Interlocked.Add(ref MsQuicLib.CurrentHandshakeMemoryUsage, -1 * QUIC_CONN_HANDSHAKE_MEMORY_USAGE);
            QuicLibraryEvaluateSendRetryState();
        }

        static QUIC_BINDING QuicLibraryLookupBinding(QUIC_ADDR LocalAddress, QUIC_ADDR RemoteAddress)
        {
            QUIC_ADDR TempAddr = new QUIC_ADDR();
            for (CXPLAT_LIST_ENTRY Link = MsQuicLib.Bindings.Next; Link != MsQuicLib.Bindings; Link = Link.Next)
            {
                QUIC_BINDING Binding = CXPLAT_CONTAINING_RECORD<QUIC_BINDING>(Link);
                QuicBindingGetLocalAddress(Binding, TempAddr);
                if (Binding.Connected)
                {
                    if (RemoteAddress != null && QuicAddrCompare(LocalAddress, TempAddr))
                    {
                        QuicBindingGetRemoteAddress(Binding, TempAddr);
                        if (QuicAddrCompare(RemoteAddress, TempAddr))
                        {
                            return Binding;
                        }
                    }
                }
                else
                {
                    if (QuicAddrGetPort(TempAddr) == QuicAddrGetPort(LocalAddress))
                    {
                        return Binding;
                    }
                }
            }
            return null;
        }

        static int QuicLibraryGetBinding(CXPLAT_UDP_CONFIG UdpConfig, out QUIC_BINDING NewBinding)
        {
            int Status;
            NewBinding = null;
            QUIC_BINDING Binding;
            QUIC_ADDR NewLocalAddress = new QUIC_ADDR();
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
                if (!ShareBinding || Binding.Exclusive || ServerOwned != Binding.ServerOwned)
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

            Status = QuicBindingInitialize(UdpConfig, out NewBinding);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            QuicBindingGetLocalAddress(NewBinding, NewLocalAddress);

            CxPlatDispatchLockAcquire(MsQuicLib.DatapathLock);
            if (BoolOk(CxPlatDataPathGetSupportedFeatures(MsQuicLib.Datapath) & CXPLAT_DATAPATH_FEATURE_LOCAL_PORT_SHARING))
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

        static QUIC_CID QuicCidNewRandomSource(QUIC_CONNECTION Connection, QUIC_SSBuffer ServerID, int PartitionID, int PrefixLength, QUIC_SSBuffer Prefix)
        {
            NetLog.Assert(MsQuicLib.CidTotalLength <= QUIC_MAX_CONNECTION_ID_LENGTH_V1);
            NetLog.Assert(MsQuicLib.CidTotalLength == MsQuicLib.CidServerIdLength + QUIC_CID_PID_LENGTH + QUIC_CID_PAYLOAD_LENGTH);
            NetLog.Assert(QUIC_CID_PAYLOAD_LENGTH > PrefixLength);

            QUIC_CID Entry = new QUIC_CID(MsQuicLib.CidTotalLength);
            if (Entry != null)
            {
                Entry.Connection = Connection;
                QUIC_SSBuffer Data = Entry.Data;
                if (!ServerID.IsEmpty)
                {
                    ServerID.Slice(0, MsQuicLib.CidServerIdLength).CopyTo(Data);
                }
                else
                {
                    CxPlatRandom.Random(Data.Slice(0, MsQuicLib.CidServerIdLength));
                }
                
                Data += MsQuicLib.CidServerIdLength;

                NetLog.Assert(QUIC_CID_PID_LENGTH == sizeof(ushort), "Assumes a 2 byte PID");
                EndianBitConverter.SetBytes(Data.Buffer, 0, (ushort)PartitionID);
                Data += sizeof(ushort);

                if (PrefixLength != 0)
                {
                    Prefix.Slice(0, PrefixLength).CopyTo(Data);
                    Data += PrefixLength;
                }
                CxPlatRandom.Random(Data.Slice(0, QUIC_CID_PAYLOAD_LENGTH - PrefixLength));
            }
            return Entry;
        }

        static int QuicLibraryGenerateStatelessResetToken(QUIC_PARTITION Partition, QUIC_SSBuffer CID, QUIC_SSBuffer ResetToken)
        {
            QUIC_SSBuffer HashOutput = new byte[CXPLAT_HASH_SHA256_SIZE];
            CxPlatLockAcquire(Partition.ResetTokenLock);
            int Status = CxPlatHashCompute(Partition.ResetTokenHash, CID.Slice(0, MsQuicLib.CidTotalLength), HashOutput);
            CxPlatLockRelease(Partition.ResetTokenLock);
            if (QUIC_SUCCEEDED(Status)) 
            {
                HashOutput.GetSpan().Slice(0, QUIC_STATELESS_RESET_TOKEN_LENGTH).CopyTo(ResetToken.GetSpan());
            }
            return Status;
        }

        static bool QuicLibraryOnListenerRegistered(QUIC_LISTENER Listener)
        {
            bool Success = true;
            CxPlatLockAcquire(MsQuicLib.Lock);
            if (MsQuicLib.StatelessRegistration == null)
            {
                QUIC_REGISTRATION_CONFIG Config = new QUIC_REGISTRATION_CONFIG()
                {
                    AppName = "Stateless",
                    ExecutionProfile = QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_INTERNAL
                };

                if (QUIC_FAILED(MsQuicRegistrationOpen(Config, out MsQuicLib.StatelessRegistration)))
                {
                    Success = false;
                    goto Fail;
                }
            }
        Fail:
            CxPlatLockRelease(MsQuicLib.Lock);
            return Success;
        }

        public static void MsQuicSetCallbackHandler_For_QUIC_STREAM(QUIC_STREAM Handle, QUIC_STREAM_CALLBACK Handler, object Context)
        {
            Handle.ClientCallbackHandler = (QUIC_STREAM_CALLBACK)Handler;
            Handle.ClientContext = Context;
        }

        public static void MsQuicSetCallbackHandler_For_QUIC_LISTENER(QUIC_LISTENER Handle, QUIC_LISTENER_CALLBACK Handler, object Context)
        {
            Handle.ClientCallbackHandler = Handler;
            Handle.ClientContext = Context;
        }

        public static void MsQuicSetCallbackHandler_For_QUIC_CONNECTION(QUIC_CONNECTION Handle, QUIC_CONNECTION_CALLBACK Handler, object Context)
        {
            Handle.ClientCallbackHandler = Handler;
            Handle.ClientContext = Context;
        }

        static void QuicTraceRundown()
        {
            if (!MsQuicLib.Loaded)
            {
                return;
            }

            CxPlatLockAcquire(MsQuicLib.Lock);

            if (MsQuicLib.OpenRefCount > 0)
            {
                if (MsQuicLib.Datapath != null)
                {
                }

                if (MsQuicLib.StatelessRegistration != null)
                {
                    QuicRegistrationTraceRundown(MsQuicLib.StatelessRegistration);
                }

                for (CXPLAT_LIST_ENTRY Link = MsQuicLib.Registrations.Next; Link != MsQuicLib.Registrations; Link = Link.Next)
                {
                    QuicRegistrationTraceRundown(CXPLAT_CONTAINING_RECORD<QUIC_REGISTRATION>(Link));
                }

                CxPlatDispatchLockAcquire(MsQuicLib.DatapathLock);
                for (CXPLAT_LIST_ENTRY Link = MsQuicLib.Bindings.Next; Link != MsQuicLib.Bindings; Link = Link.Next)
                {
                   // QuicBindingTraceRundown(CXPLAT_CONTAINING_RECORD<QUIC_BINDING>(Link));
                }
                CxPlatDispatchLockRelease(MsQuicLib.DatapathLock);
            }


            CxPlatLockRelease(MsQuicLib.Lock);
        }

        static int QuicLibraryGetParam(QUIC_HANDLE Handle, uint Param, QUIC_SSBuffer Buffer)
        {
            int Status = 0;
            QUIC_REGISTRATION Registration;
            QUIC_CONFIGURATION Configuration;
            QUIC_LISTENER Listener;
            QUIC_CONNECTION Connection;
            QUIC_STREAM Stream;

            NetLog.Assert(Buffer.Length > 0);

            switch (Handle.Type)
            {
                case  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION:
                    Stream = null;
                    Connection = null;
                    Listener = null;
                    Configuration = null;
                    Registration = (QUIC_REGISTRATION)Handle;
                    break;

                case  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION:
                    Stream = null;
                    Connection = null;
                    Listener = null;
                    Configuration = (QUIC_CONFIGURATION)Handle;
                    Registration = Configuration.Registration;
                    break;

                case  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER:
                    Stream = null;
                    Connection = null;
                    Listener = (QUIC_LISTENER)Handle;
                    Configuration = null;
                    Registration = Listener.Registration;
                    break;

                case  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT:
                case  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER:
                    Stream = null;
                    Listener = null;
                    Connection = (QUIC_CONNECTION)Handle;
                    Configuration = Connection.Configuration;
                    Registration = Connection.Registration;
                    break;

                case  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_STREAM:
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
                    if (Registration == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicRegistrationParamGet(Registration, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_CONFIGURATION:
                    if (Configuration == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicConfigurationParamGet(Configuration, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_LISTENER:
                    if (Listener == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicListenerParamGet(Listener, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_CONNECTION:
                    if (Connection == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                       // Status = QuicConnParamGet(Connection, Param, Buffer);
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
                        //Status = CxPlatTlsParamGet(Connection.Crypto.TLS, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_STREAM:
                    if (Stream == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        //Status = QuicStreamParamGet(Stream, Param, Buffer);
                    }
                    break;

                default:
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
            }

        Error:

            return Status;
        }


        public const int QUIC_API_VERSION_1 = 1; // Not supported any more
        public const int QUIC_API_VERSION_2 = 2; // Current latest

        public static int MsQuicOpenVersion(uint Version)
        {
            if (Version != QUIC_API_VERSION_2)
            {
                return QUIC_STATUS_NOT_SUPPORTED;
            }

            MsQuicLibraryLoad();
            int Status = MsQuicAddRef();
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }
        Exit:
            return Status;
        }

        static int MsQuicAddRef()
        {
            NetLog.Assert(MsQuicLib.Loaded);
            if (!MsQuicLib.Loaded)
            {
                return QUIC_STATUS_INVALID_STATE;
            }

            int Status = QUIC_STATUS_SUCCESS;

            CxPlatLockAcquire(MsQuicLib.Lock);
            if (++MsQuicLib.OpenRefCount == 1)
            {
                Status = MsQuicLibraryInitialize();
                if (QUIC_FAILED(Status))
                {
                    MsQuicLib.OpenRefCount--;
                    goto Error;
                }
            }
        Error:
            CxPlatLockRelease(MsQuicLib.Lock);
            return Status;
        }

        static int MsQuicLibraryInitialize()
        {
            int Status = QUIC_STATUS_SUCCESS;
            Status = CxPlatInitialize();
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            NetLog.Assert(US_TO_MS(CxPlatGetTimerResolutionUs()) + 1 <= byte.MaxValue);
            MsQuicLib.TimerResolutionMs = US_TO_MS(CxPlatGetTimerResolutionUs()) + 1;
            MsQuicLib.PerfCounterSamplesTime = CxPlatTimeUs();
            Array.Clear(MsQuicLib.PerfCounterSamples, 0, MsQuicLib.PerfCounterSamples.Length);

            CxPlatRandom.Random(MsQuicLib.ToeplitzHash.HashKey);
            MsQuicLib.ToeplitzHash.InputSize = CXPLAT_TOEPLITZ_INPUT_SIZE.CXPLAT_TOEPLITZ_INPUT_SIZE_QUIC;
            CxPlatToeplitzHashInitialize(MsQuicLib.ToeplitzHash);

            if (QUIC_FAILED(Status))
            {
                Status = QUIC_STATUS_SUCCESS;
            }

            MsQuicLibraryReadSettings(null); // NULL means don't update registrations.

            Array.Clear(MsQuicLib.StatelessRetryKeys, 0, MsQuicLib.StatelessRetryKeys.Length);
            Array.Clear(MsQuicLib.StatelessRetryKeysExpiration, 0, MsQuicLib.StatelessRetryKeysExpiration.Length);
            
            QuicVersionNegotiationExtGenerateCompatibleVersionsList(
                QUIC_VERSION_LATEST,
                DefaultSupportedVersionsList,
                MsQuicLib.DefaultCompatibilityList);
        Error:
            return Status;
        }

        static void MsQuicLibraryReadSettings(object Context)
        {
            QuicSettingsSetDefault(MsQuicLib.Settings);
            MsQuicLibraryOnSettingsChanged(Context != null);
        }

        static void MsQuicLibraryOnSettingsChanged(bool UpdateRegistrations)
        {
            if (!MsQuicLib.InUse)
            {
                QuicLibApplyLoadBalancingSetting();
            }

            MsQuicLib.HandshakeMemoryLimit = (MsQuicLib.Settings.RetryMemoryLimit * CxPlatTotalMemory) / ushort.MaxValue;
            QuicLibraryEvaluateSendRetryState();

            if (UpdateRegistrations)
            {
                CxPlatLockAcquire(MsQuicLib.Lock);

                for (CXPLAT_LIST_ENTRY Link = MsQuicLib.Registrations.Next; Link != MsQuicLib.Registrations; Link = Link.Next)
                {
                    QuicRegistrationSettingsChanged(CXPLAT_CONTAINING_RECORD<QUIC_REGISTRATION>(Link));
                }

                CxPlatLockRelease(MsQuicLib.Lock);
            }
        }

        static void MsQuicLibraryUninitialize()
        {
            if (MsQuicLib.StatelessRegistration != null)
            {
                MsQuicRegistrationShutdown(MsQuicLib.StatelessRegistration,  QUIC_CONNECTION_SHUTDOWN_FLAGS.QUIC_CONNECTION_SHUTDOWN_FLAG_SILENT, 0);
            }
            
            if (MsQuicLib.StatelessRegistration != null)
            {
                MsQuicRegistrationClose(MsQuicLib.StatelessRegistration);
                MsQuicLib.StatelessRegistration = null;
            }
            
            NetLog.Assert(CxPlatListIsEmpty(MsQuicLib.Registrations));
            if (MsQuicLib.Datapath != null)
            {
                CxPlatDataPathUninitialize(MsQuicLib.Datapath);
                MsQuicLib.Datapath = null;
            }

#if DEBUG
            long[] PerfCounters = new long[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_MAX];
            QuicLibrarySumPerfCounters(PerfCounters);

            NetLog.Assert(PerfCounters[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_ACTIVE] == 0);
            NetLog.Assert(PerfCounters[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_CONNECTED] == 0);
            NetLog.Assert(PerfCounters[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_STRM_ACTIVE] == 0);
            NetLog.Assert(PerfCounters[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH] == 0);
            NetLog.Assert(PerfCounters[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH] == 0);
            NetLog.Assert(PerfCounters[(int)QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_WORK_OPER_QUEUE_DEPTH] == 0);
#endif
            NetLog.Assert(CxPlatListIsEmpty(MsQuicLib.Bindings));
            MsQuicLibraryFreePartitions();
            QuicSettingsCleanup(MsQuicLib.Settings);

            if (MsQuicLib.ExecutionConfig != null)
            {
                MsQuicLib.ExecutionConfig = null;
            }

            MsQuicLib.LazyInitComplete = false;
            if (!_KERNEL_MODE)
            {
                CxPlatWorkerPoolDelete(MsQuicLib.WorkerPool);
                MsQuicLib.WorkerPool = null;
            }
        }

    }
}
