using AKNet.Common;
using System;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal enum QUIC_CONNECTION_REF
    {
        QUIC_CONN_REF_HANDLE_OWNER,         // Application or Core.
        QUIC_CONN_REF_LOOKUP_TABLE,         // Per registered CID.
        QUIC_CONN_REF_LOOKUP_RESULT,        // For connections returned from lookups.
        QUIC_CONN_REF_WORKER,               // Worker is (queued for) processing.
        QUIC_CONN_REF_TIMER_WHEEL,          // The timer wheel is tracking the connection.
        QUIC_CONN_REF_ROUTE,                // Route resolution is undergoing.
        QUIC_CONN_REF_STREAM,               // A stream depends on the connection.
        QUIC_CONN_REF_COUNT
    }

    internal class QUIC_RECEIVE_PROCESSING_STATE
    {
        public bool ResetIdleTimeout;
        public bool UpdatePartitionId;
        public int PartitionIndex;
    }

    internal class QUIC_CONNECTION_STATE
    {
        public ulong Flags;
        public bool Allocated;    // Allocated. Used for Debugging.
        public bool Initialized;    // Initialized successfully. Used for Debugging.
        public bool Started;    // Handshake started.
        public bool Connected;    // Handshake completed.
        public bool ClosedLocally;    // Locally closed.
        public bool ClosedRemotely;    // Remotely closed.
        public bool AppClosed;    // Application (not transport) closed connection.
        public bool ShutdownComplete;   // Shutdown callback delivered for handle.
        public bool HandleClosed;    // Handle closed by application layer.
        public bool Freed;    // Freed. Used for Debugging.
        public bool HeaderProtectionEnabled; // TODO - Remove since it's not used
        public bool Disable1RttEncrytion;
        public bool ExternalOwner;
        public bool Registered;
        public bool GotFirstServerResponse;
        public bool HandshakeUsedRetryPacket;
        public bool HandshakeConfirmed;
        public bool ListenerAccepted;
        public bool LocalAddressSet;
        public bool RemoteAddressSet;
        public bool PeerTransportParameterValid;
        public bool UpdateWorker;
        public bool ShutdownCompleteTimedOut;
        public bool ProcessShutdownComplete;
        public bool ShareBinding;
        public bool TestTransportParameterSet;
        public bool UseRoundRobinStreamScheduling;
        public bool ResumptionEnabled;
        public bool InlineApiExecution;
        public bool CompatibleVerNegotiationAttempted;
        public bool CompatibleVerNegotiationCompleted;
        public bool LocalInterfaceSet;
        public bool FixedBit;
        public bool ReliableResetStreamNegotiated;
        public bool TimestampSendNegotiated;
        public bool TimestampRecvNegotiated;
        public bool DelayedApplicationError;
        public bool IsVerifying;
        public bool IgnoreReordering;
    }

    internal class QUIC_CONN_STATS
    {
        public long CorrelationId;
        public uint VersionNegotiation;
        public uint StatelessRetry;
        public uint ResumptionAttempted;
        public bool ResumptionSucceeded;
        public bool GreaseBitNegotiated;
        public uint EncryptionOffloaded;
        public uint QuicVersion;

        public readonly Timing_DATA Timing = new Timing_DATA();
        public readonly Schedule_DATA Schedule = new Schedule_DATA();
        public readonly Handshake_DATA Handshake = new Handshake_DATA();
        public readonly Send_DATA Send = new Send_DATA();
        public readonly Recv_DATA Recv = new Recv_DATA();
        public readonly Misc_DATA Misc = new Misc_DATA();

        public class Timing_DATA
        {
            public long Start;
            public long InitialFlightEnd;      // Processed all peer's Initial packets
            public long HandshakeFlightEnd;    // Processed all peer's Handshake packets
            public long PhaseShift;             // Time between local and peer epochs
        }

        public class Schedule_DATA
        {
            public long LastQueueTime;         // Time the connection last entered the work queue.
            public ulong DrainCount;            // Sum of drain calls
            public ulong OperationCount;        // Sum of operations processed
        }

        public class Handshake_DATA
        {
            public int ClientFlight1Bytes;    // Sum of TLS payloads
            public int ServerFlight1Bytes;    // Sum of TLS payloads
            public int ClientFlight2Bytes;    // Sum of TLS payloads
            public byte HandshakeHopLimitTTL;   // TTL value in the initial packet of the handshake.
        }

        public class Send_DATA
        {
            public ulong TotalPackets;          //总共发送的 QUIC 数据包数量（注意：多个 QUIC 包可能被合并进一个 UDP 报文）
            public ulong RetransmittablePackets; //可以重传的数据包数（即包含需要可靠传输的数据，如 STREAM、ACK 等帧）
            public ulong SuspectedLostPackets; //被怀疑丢失的数据包数量（基于 RTT 和 ACK 判断）
            public ulong SpuriousLostPackets;   //被误判为丢失但后来确认成功接收的数据包数（即“虚假丢包”）
            public ulong TotalBytes;            // Sum of UDP payloads
            public ulong TotalStreamBytes;      // Sum of stream payloads
            public uint CongestionCount; //遇到拥塞事件的次数（比如因丢包而触发拥塞控制）
            public uint EcnCongestionCount; //使用 ECN（显式拥塞通知）检测到的拥塞次数
            public uint PersistentCongestionCount; //持续性拥塞发生的次数（当多个路径/RTT周期内持续发生丢包时判定为“持久拥塞”
        }

        public class Recv_DATA
        {
            public long TotalPackets;          // 接收到的总数据包数, 一个 UDP 数据报可能包含多个 QUIC packet（coalesced）。
            public long ReorderedPackets;      // 因为网络乱序而被接收方识别为“较旧”的数据包数量。这些包虽然顺序错乱但仍然有效。.
            public long DroppedPackets;        // 被丢弃的数据包总数。这包括重复包、解密失败、无效格式等。
            public long DuplicatePackets;      // 重复的数据包数量。例如，因为 ACK 没有及时送达，发送方重传后再次收到相同 packet number 的包。
            public long DecryptionFailures;    // 解密失败的数据包数量。可能是由于密钥错误、数据损坏、攻击等原因。
            public long ValidPackets;          // 成功解密的数据包数量，或那些不需要解密的初始包（如 Initial 包）
            public long ValidAckFrames;        // 接收到的有效 ACK 帧的数量。用于确认远程端点是否正常响应。
            public long TotalBytes;            // Sum of UDP payloads
            public long TotalStreamBytes;      // Sum of stream payloads
        }
        public class Misc_DATA
        {
            public uint KeyUpdateCount;        // Count of key updates completed.
            public uint DestCidUpdateCount;    // Number of times the destination CID changed.
        }
    }

    internal class QUIC_CONNECTION : QUIC_HANDLE, CXPLAT_POOL_Interface<QUIC_CONNECTION>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_CONNECTION> POOL_ENTRY = null;
        public readonly CXPLAT_LIST_ENTRY RegistrationLink;
        public readonly CXPLAT_LIST_ENTRY WorkerLink;
        public readonly CXPLAT_LIST_ENTRY<QUIC_CONNECTION> TimerLink = null;

        public QUIC_WORKER Worker;
        public QUIC_PARTITION Partition;
        public QUIC_REGISTRATION Registration;
        public QUIC_CONFIGURATION Configuration;
        public readonly QUIC_SETTINGS Settings = new QUIC_SETTINGS();
        public long RefCount;
        public readonly int[] RefTypeCount = new int[(int)QUIC_CONNECTION_REF.QUIC_CONN_REF_COUNT];

        public readonly QUIC_CONNECTION_STATE State = new QUIC_CONNECTION_STATE();
        public int WorkerThreadID;
        
        public readonly byte[] ServerID = new byte[MSQuicFunc.QUIC_MAX_CID_SID_LENGTH];
        public int PartitionID;
        public int DestCidCount;
        public int RetiredDestCidCount;
        public byte SourceCidLimit;
        public int PathsCount;
        public int NextPathId;
        public bool WorkerProcessing;
        public bool HasQueuedWork;
        public bool HasPriorityWork;
        
        public byte OutFlowBlockedReasons; // Set of QUIC_FLOW_BLOCKED_* flags
        public byte AckDelayExponent;
        public byte PacketTolerance;
        public int PeerPacketTolerance;
        public byte ReorderingThreshold;
        public byte PeerReorderingThreshold;
        public byte DSCP;
        public ulong SendAckFreqSeqNum;
        public ulong NextRecvAckFreqSeqNum;
        public ulong NextSourceCidSequenceNumber;
        public ulong RetirePriorTo;
        public readonly QUIC_PATH[] Paths = new QUIC_PATH[MSQuicFunc.QUIC_MAX_PATH_COUNT];
        public readonly CXPLAT_LIST_ENTRY SourceCids = new CXPLAT_LIST_ENTRY<QUIC_CID>(null);
        public readonly CXPLAT_LIST_ENTRY DestCids = new CXPLAT_LIST_ENTRY<QUIC_CID>(null);
        public QUIC_CID OrigDestCID;

        //CibirId 是一个用户配置的 CID 前缀。
        //第 0 字节：表示这个标识符的长度（Length）
        //第 1 字节：表示这个标识符在完整 CID 中的偏移量（Offset）
        //后续字节：实际的数据内容（Payload）
        //支持无状态路由、连接迁移、负载均衡等
        public readonly byte[] CibirId = new byte[2 + MSQuicFunc.QUIC_MAX_CIBIR_LENGTH];
        public readonly long[] ExpirationTimes = new long[(int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_COUNT];
        public long EarliestExpirationTime;
        public int ReceiveQueueCount;
        public int ReceiveQueueByteCount;
        public QUIC_RX_PACKET ReceiveQueue;
        public QUIC_RX_PACKET ReceiveQueueTail;
        public readonly object ReceiveQueueLock = new object();
        public readonly QUIC_OPERATION_QUEUE OperQ = new QUIC_OPERATION_QUEUE();
        public QUIC_OPERATION BackUpOper;
        public QUIC_API_CONTEXT BackupApiContext;
        public int BackUpOperUsed;
        public int CloseStatus;
        public int CloseErrorCode;
        public string CloseReasonPhrase;

        public string RemoteServerName;
        public QUIC_CID RemoteHashEntry;
        public readonly QUIC_TRANSPORT_PARAMETERS PeerTransportParams = new QUIC_TRANSPORT_PARAMETERS();
        public readonly QUIC_RANGE DecodedAckRanges = new QUIC_RANGE();
        public readonly QUIC_STREAM_SET Streams = new QUIC_STREAM_SET();
        public QUIC_CONGESTION_CONTROL CongestionControl;
        public readonly QUIC_LOSS_DETECTION LossDetection = new QUIC_LOSS_DETECTION();
        public readonly QUIC_PACKET_SPACE[] Packets = new QUIC_PACKET_SPACE[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_COUNT];
        public readonly QUIC_CRYPTO Crypto = new QUIC_CRYPTO();
        public readonly QUIC_SEND Send = new QUIC_SEND();
        public readonly QUIC_SEND_BUFFER SendBuffer = new QUIC_SEND_BUFFER();
        public readonly QUIC_DATAGRAM Datagram = new QUIC_DATAGRAM();
        public QUIC_CONNECTION_CALLBACK ClientCallbackHandler;
        
        public QUIC_TRANSPORT_PARAMETERS HandshakeTP = null;
        public readonly QUIC_CONN_STATS Stats = new QUIC_CONN_STATS();
        public QUIC_PRIVATE_TRANSPORT_PARAMETER TestTransportParameter;
        public QUIC_TLS_SECRETS TlsSecrets;
        public uint PreviousQuicVersion;
        public uint OriginalQuicVersion;
        public ushort KeepAlivePadding;
        public BlockedTimings_DATA BlockedTimings;

        public struct BlockedTimings_DATA
        {
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER Scheduling;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER Pacing;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER AmplificationProt;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER CongestionControl;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER FlowControl;
        }

        public QUIC_CONNECTION()
        {
            TimerLink = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>(this);
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_CONNECTION>(this);
            RegistrationLink = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>(this);
            WorkerLink = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>(this);

            Send.mConnection = this;
            Crypto.mConnection = this;
            for (int i = 0; i < Paths.Length; i++)
            {
                Paths[i] = new QUIC_PATH();
            }
        }

        public void Reset()
        {
            
        }

        public CXPLAT_POOL_ENTRY<QUIC_CONNECTION> GetEntry()
        {
            return POOL_ENTRY;
        }
    }

    internal static partial class MSQuicFunc
    {
        static void QuicConnValidate(QUIC_CONNECTION Connection)
        {
#if DEBUG
            NetLog.Assert(!Connection.State.Freed);
#endif
        }

        static void QuicConnAddRef(QUIC_CONNECTION Connection, QUIC_CONNECTION_REF Ref)
        {
            QuicConnValidate(Connection);

#if DEBUG
            Interlocked.Increment(ref Connection.RefTypeCount[(int)Ref]);
#endif
            Interlocked.Increment(ref Connection.RefCount);
        }

        static void QuicConnRelease(QUIC_CONNECTION Connection, QUIC_CONNECTION_REF Ref)
        {
            QuicConnValidate(Connection);
#if DEBUG
            NetLog.Assert(Connection.RefTypeCount[(int)Ref] > 0);
            ushort result = (ushort)Interlocked.Decrement(ref Connection.RefTypeCount[(int)Ref]);
#endif
            NetLog.Assert(Connection.RefCount > 0);
            if (Interlocked.Decrement(ref Connection.RefCount) == 0)
            {
#if DEBUG
                for (int i = 0; i < (int)QUIC_CONNECTION_REF.QUIC_CONN_REF_COUNT; i++)
                {
                    NetLog.Assert(Connection.RefTypeCount[i] == 0);
                }
#endif
                if (Ref == QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT)
                {
                    NetLog.Assert(Connection.Worker != null);
                    QuicWorkerQueueConnection(Connection.Worker, Connection);
                }
                else
                {
                    QuicConnFree(Connection);
                }
            }
        }

        static bool QuicConnIsServer(QUIC_CONNECTION Connection)
        {
            return Connection.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER;
        }

        static bool QuicConnIsClient(QUIC_CONNECTION Connection)
        {
            return Connection.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT;
        }

        static QUIC_CONNECTION QuicCryptoGetConnection(QUIC_CRYPTO Crypto)
        {
            return Crypto.mConnection;
        }

        static QUIC_CONNECTION QuicSendGetConnection(QUIC_SEND Send)
        {
            return Send.mConnection;
        }

        static QUIC_CONNECTION QuicLossDetectionGetConnection(QUIC_LOSS_DETECTION LossDetection)
        {
            return LossDetection.mConnection;
        }

        static bool QuicConnIsClosed(QUIC_CONNECTION Connection)
        {
            return Connection.State.ClosedLocally || Connection.State.ClosedRemotely;
        }

        static QUIC_CONNECTION QuicDatagramGetConnection(QUIC_DATAGRAM Datagram)
        {
            return Datagram.mConnection;
        }

        static QUIC_CONNECTION QuicCongestionControlGetConnection(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.mConnection;
        }

        static long QuicGetEarliestExpirationTime(QUIC_CONNECTION Connection)
        {
            long EarliestExpirationTime = Connection.ExpirationTimes[0];
            for (int Type = 1; Type < (int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_COUNT; ++Type)
            {
                if (Connection.ExpirationTimes[Type] < EarliestExpirationTime)
                {
                    EarliestExpirationTime = Connection.ExpirationTimes[Type];
                }
            }
            return EarliestExpirationTime;
        }

        static void QuicConnTimerCancel(QUIC_CONNECTION Connection, QUIC_CONN_TIMER_TYPE Type)
        {
            NetLog.Assert(Connection.EarliestExpirationTime <= Connection.ExpirationTimes[(int)Type]);
            if (Connection.EarliestExpirationTime == long.MaxValue)
            {
                return;
            }

            if (Connection.ExpirationTimes[(int)Type] == Connection.EarliestExpirationTime)
            {
                Connection.ExpirationTimes[(int)Type] = long.MaxValue;
                long NewEarliestExpirationTime = QuicGetEarliestExpirationTime(Connection);
                if (NewEarliestExpirationTime != Connection.EarliestExpirationTime)
                {
                    Connection.EarliestExpirationTime = NewEarliestExpirationTime;
                    QuicTimerWheelUpdateConnection(Connection.Worker.TimerWheel, Connection);
                }
            }
            else
            {
                Connection.ExpirationTimes[(long)Type] = long.MaxValue;
            }
        }

        static void QuicConnQueueOper(QUIC_CONNECTION Connection, QUIC_OPERATION Oper)
        {
#if DEBUG
            if (!Connection.State.Initialized)
            {
                NetLog.Assert(QuicConnIsServer(Connection));
                NetLog.Assert(Connection.SourceCids.Next != null);
            }
#endif
            if (QuicOperationEnqueue(Connection.OperQ, Connection.Partition, Oper))
            {
                QuicWorkerQueueConnection(Connection.Worker, Connection);
            }
        }

        static void QuicConnQueuePriorityOper(QUIC_CONNECTION Connection, QUIC_OPERATION Oper)
        {
#if DEBUG
            if (!Connection.State.Initialized)
            {
                NetLog.Assert(QuicConnIsServer(Connection));
                NetLog.Assert(Connection.SourceCids.Next != null);
            }
#endif
            if (QuicOperationEnqueuePriority(Connection.OperQ, Connection.Partition, Oper))
            {
                QuicWorkerQueuePriorityConnection(Connection.Worker, Connection);
            }
        }

        static void QuicConnQueueHighestPriorityOper(QUIC_CONNECTION Connection, QUIC_OPERATION Oper)
        {
            if (QuicOperationEnqueueFront(Connection.OperQ, Connection.Partition, Oper))
            {
                QuicWorkerQueuePriorityConnection(Connection.Worker, Connection);
            }
        }

        static void QuicConnOnQuicVersionSet(QUIC_CONNECTION Connection)
        {
            switch (Connection.Stats.QuicVersion)
            {
                case QUIC_VERSION_1:
                case QUIC_VERSION_2:
                default:
                    Connection.State.HeaderProtectionEnabled = true;
                    break;
            }
        }

        static void QuicConnUnregister(QUIC_CONNECTION Connection)
        {
            if (Connection.State.Registered)
            {
                Monitor.Enter(Connection.Registration.ConnectionLock);
                CxPlatListEntryRemove(Connection.RegistrationLink);
                Monitor.Exit(Connection.Registration.ConnectionLock);
                CxPlatRundownRelease(Connection.Registration.Rundown);
                Connection.Registration = null;
                Connection.State.Registered = false;
            }
        }

        static bool QuicConnRegister(QUIC_CONNECTION Connection, QUIC_REGISTRATION Registration)
        {
            QuicConnUnregister(Connection);

            if (!CxPlatRundownAcquire(Registration.Rundown))
            {
                return false;
            }

            Connection.State.Registered = true;
            Connection.Registration = Registration;
            bool RegistrationShuttingDown;

            CxPlatDispatchLockAcquire(Registration.ConnectionLock);
            RegistrationShuttingDown = Registration.ShuttingDown;
            if (!RegistrationShuttingDown)
            {
                if (Connection.Worker == null)
                {
                    QuicRegistrationQueueNewConnection(Registration, Connection);
                }
                CxPlatListInsertTail(Registration.Connections, Connection.RegistrationLink);
            }
            CxPlatDispatchLockRelease(Registration.ConnectionLock);

            if (RegistrationShuttingDown)
            {
                Connection.State.Registered = false;
                Connection.Registration = null;
                CxPlatRundownRelease(Registration.Rundown);
            }

            return !RegistrationShuttingDown;
        }

        static int QuicConnAlloc(QUIC_REGISTRATION Registration, QUIC_PARTITION Partition, QUIC_WORKER Worker, QUIC_RX_PACKET Packet, out QUIC_CONNECTION NewConnection)
        {
            bool IsServer = Packet != null;
            NewConnection = null;
            int Status;
            
            int PartitionId = QuicPartitionIdCreate(Partition.Index);
            NetLog.Assert(Partition.Index == QuicPartitionIdGetIndex(PartitionId));

            QUIC_CONNECTION Connection = Partition.ConnectionPool.CxPlatPoolAlloc();
            if (Connection == null)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }
            
            Connection.Partition = Partition;
#if DEBUG
            Interlocked.Increment(ref MsQuicLib.ConnectionCount);
#endif
            QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_CREATED);
            QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_ACTIVE);

            Connection.Stats.CorrelationId = Interlocked.Increment(ref MsQuicLib.ConnectionCorrelationId) - 1;
            
            Connection.RefCount = 1;
#if DEBUG
            Connection.RefTypeCount[(int)QUIC_CONNECTION_REF.QUIC_CONN_REF_HANDLE_OWNER] = 1;
#endif
            Connection.PartitionID = PartitionId;
            Connection.State.Allocated = true;
            Connection.State.ShareBinding = IsServer;
            Connection.State.FixedBit = true;
            Connection.Stats.Timing.Start = CxPlatTime();

            Connection.SourceCidLimit = QUIC_ACTIVE_CONNECTION_ID_LIMIT;
            Connection.AckDelayExponent = QUIC_ACK_DELAY_EXPONENT;
            Connection.PacketTolerance = QUIC_MIN_ACK_SEND_NUMBER;
            Connection.PeerPacketTolerance = QUIC_MIN_ACK_SEND_NUMBER;
            Connection.ReorderingThreshold = QUIC_MIN_REORDERING_THRESHOLD;
            Connection.PeerReorderingThreshold = QUIC_MIN_REORDERING_THRESHOLD;
            Connection.PeerTransportParams.AckDelayExponent = QUIC_TP_ACK_DELAY_EXPONENT_DEFAULT;
            Connection.ReceiveQueueTail = Connection.ReceiveQueue = null;
            QuicSettingsCopy(Connection.Settings, MsQuicLib.Settings);
            Connection.Settings.IsSetFlags = 0; // Just grab the global values, not IsSet flags.

            CxPlatListInitializeHead(Connection.SourceCids);
            CxPlatListInitializeHead(Connection.DestCids);
            QuicStreamSetInitialize(Connection, Connection.Streams);
            QuicSendBufferInitialize(Connection.SendBuffer);
            QuicOperationQueueInitialize(Connection.OperQ);
            QuicSendInitialize(Connection.Send, Connection.Settings);
            QuicCongestionControlInitialize(out Connection.CongestionControl, Connection);
            QuicLossDetectionInitialize(Connection.LossDetection,Connection);
            QuicDatagramInitialize(Connection.Datagram, Connection);
            QuicRangeInitialize(QUIC_MAX_RANGE_DECODE_ACKS, Connection.DecodedAckRanges);

            for (int i = 0; i < Connection.Packets.Length; i++)
            {
                Status = QuicPacketSpaceInitialize(Connection, (QUIC_ENCRYPT_LEVEL)i, out Connection.Packets[i]);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }
            }

            QUIC_PATH Path = Connection.Paths[0];
            QuicPathInitialize(Connection, Path);
            Path.IsActive = true;
            Connection.PathsCount = 1;

            Connection.EarliestExpirationTime = long.MaxValue;
            for (QUIC_CONN_TIMER_TYPE Type = 0; Type < QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_COUNT; ++Type)
            {
                Connection.ExpirationTimes[(int)Type] = long.MaxValue;
            }

            if (IsServer)
            {
                Connection.Type = QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER;
                if (MsQuicLib.Settings.LoadBalancingMode == QUIC_LOAD_BALANCING_MODE.QUIC_LOAD_BALANCING_SERVER_ID_IP)
                {
                    Connection.ServerID[0] = CxPlatRandom.RandomByte();
                    byte[] IP_Array = Packet.Route.LocalAddress.GetBytes();
                    if (Packet.Route.LocalAddress.Family == AddressFamily.InterNetwork)
                    {
                        Array.Copy(IP_Array, 0, Connection.ServerID, 1, 4);
                    }
                    else
                    {
                        Array.Copy(IP_Array, 12, Connection.ServerID, 1, 4);
                    }
                }
                else if (MsQuicLib.Settings.LoadBalancingMode == QUIC_LOAD_BALANCING_MODE.QUIC_LOAD_BALANCING_SERVER_ID_FIXED)
                {
                    Connection.ServerID[0] = CxPlatRandom.RandomByte();
                    EndianBitConverter.SetBytes(Connection.ServerID, 1, (uint)MsQuicLib.Settings.FixedServerID);
                }

                Connection.Stats.QuicVersion = Packet.Invariant.LONG_HDR.Version;
                QuicConnOnQuicVersionSet(Connection);
                QuicCopyRouteInfo(Path.Route, Packet.Route);
                Connection.State.LocalAddressSet = true;
                Connection.State.RemoteAddressSet = true;

                Path.DestCid = QuicCidNewDestination(Packet.SourceCid.Data);
                if (Path.DestCid == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Error;
                }

                Path.DestCid.UsedLocally = true;
                CxPlatListInsertTail(Connection.DestCids, Path.DestCid.Link);

                QUIC_CID SourceCid = QuicCidNewSource(Connection, Packet.DestCid.Data);
                if (SourceCid == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Error;
                }
                SourceCid.IsInitial = true;
                SourceCid.UsedByPeer = true;
                CxPlatListInsertHead(Connection.SourceCids, SourceCid.Link);
            }
            else
            {
                Connection.Type = QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT;
                Connection.State.ExternalOwner = true;
                Path.IsPeerValidated = true;
                Path.Allowance = int.MaxValue;

                Path.DestCid = QuicCidNewRandomDestination();
                if (Path.DestCid == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Error;
                }
                Path.DestCid.UsedLocally = true;
                Connection.DestCidCount++;
                CxPlatListInsertTail(Connection.DestCids, Path.DestCid.Link);
                Connection.State.Initialized = true;
            }

            QuicPathValidate(Path);
            if (Worker != null)
            {
                QuicWorkerAssignConnection(Worker, Connection);
            }

            if (!QuicConnRegister(Connection, Registration))
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Error;
            }

            NewConnection = Connection;
            return QUIC_STATUS_SUCCESS;

        Error:
            Connection.State.HandleClosed = true;
            for (int i = 0; i < Connection.Packets.Length; i++)
            {
                if (Connection.Packets[i] != null)
                {
                    QuicPacketSpaceUninitialize(Connection.Packets[i]);
                    Connection.Packets[i] = null;
                }
            }

            if (IsServer && !CxPlatListIsEmpty(Connection.SourceCids))
            {
                CxPlatListInitializeHead(Connection.SourceCids);
            }

            if(!CxPlatListIsEmpty(Connection.DestCids))
            {
                CxPlatListInitializeHead(Connection.DestCids);
            }

            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_HANDLE_OWNER);
            return Status;
        }

        static ushort CxPlatSocketGetLocalMtu(CXPLAT_SOCKET Socket)
        {
            NetLog.Assert(Socket != null);
            return Socket.Mtu;
        }

        static ushort QuicConnGetMaxMtuForPath(QUIC_CONNECTION Connection, QUIC_PATH Path)
        {
            ushort LocalMtu = Path.LocalMtu;
            if (LocalMtu == 0)
            {
                LocalMtu = CxPlatSocketGetLocalMtu(Path.Binding.Socket);
                Path.LocalMtu = LocalMtu;
            }

            ushort RemoteMtu = 0xFFFF;
            if (BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE))
            {
                RemoteMtu = PacketSizeFromUdpPayloadSize(QuicAddrGetFamily(Path.Route.RemoteAddress), (ushort)Connection.PeerTransportParams.MaxUdpPayloadSize);
            }
            ushort SettingsMtu = Connection.Settings.MaximumMtu;
            return Math.Min(Math.Min(LocalMtu, RemoteMtu), SettingsMtu);
        }

        static void QuicConnTimerSetEx(QUIC_CONNECTION Connection, QUIC_CONN_TIMER_TYPE Type, long Delay, long TimeNow)
        {
            long NewExpirationTime = TimeNow + Delay;
            Connection.ExpirationTimes[(int)Type] = NewExpirationTime;
            long NewEarliestExpirationTime = QuicGetEarliestExpirationTime(Connection);
            if (NewEarliestExpirationTime != Connection.EarliestExpirationTime)
            {
                Connection.EarliestExpirationTime = NewEarliestExpirationTime;
                QuicTimerWheelUpdateConnection(Connection.Worker.TimerWheel, Connection);
            }
        }

        static void QuicConnTimerSet(QUIC_CONNECTION Connection, QUIC_CONN_TIMER_TYPE Type, long DelayUs)
        {
            long TimeNow = CxPlatTime();
            QuicConnTimerSetEx(Connection, Type, DelayUs, TimeNow);
        }

        static int QuicErrorCodeToStatus(int ErrorCode)
        {
            switch (ErrorCode)
            {
                case QUIC_ERROR_NO_ERROR: return QUIC_STATUS_SUCCESS;
                case QUIC_ERROR_CONNECTION_REFUSED: return QUIC_STATUS_CONNECTION_REFUSED;
                case QUIC_ERROR_PROTOCOL_VIOLATION: return QUIC_STATUS_PROTOCOL_ERROR;
                case QUIC_ERROR_APPLICATION_ERROR:
                case QUIC_ERROR_CRYPTO_USER_CANCELED: return QUIC_STATUS_USER_CANCELED;
                case QUIC_ERROR_CRYPTO_HANDSHAKE_FAILURE: return QUIC_STATUS_HANDSHAKE_FAILURE;
                case QUIC_ERROR_CRYPTO_NO_APPLICATION_PROTOCOL: return QUIC_STATUS_ALPN_NEG_FAILURE;
                case QUIC_ERROR_VERSION_NEGOTIATION_ERROR: return QUIC_STATUS_VER_NEG_ERROR;
                default:
                    return QUIC_STATUS_INTERNAL_ERROR;
            }
        }

        static void QuicConnTryClose(QUIC_CONNECTION Connection, uint Flags, int ErrorCode, string RemoteReasonPhrase)
        {
            bool ClosedRemotely = BoolOk(Flags & QUIC_CLOSE_REMOTE);
            bool SilentClose = BoolOk(Flags & QUIC_CLOSE_SILENT);

            if ((ClosedRemotely && Connection.State.ClosedRemotely) || (!ClosedRemotely && Connection.State.ClosedLocally))
            {
                if (SilentClose && Connection.State.ClosedLocally && !Connection.State.ClosedRemotely)
                {
                    Connection.State.ShutdownCompleteTimedOut = false;
                    Connection.State.ProcessShutdownComplete = true;
                }
                return;
            }

            if (ClosedRemotely)
            {
                NetLog.LogError("ClosedRemotely");
                Connection.State.ClosedRemotely = true;
            }
            else
            {
                NetLog.LogError("ClosedLocally");
                Connection.State.ClosedLocally = true;
                if (!Connection.State.ExternalOwner)
                {
                    Connection.State.ProcessShutdownComplete = true;
                }
            }

            bool ResultQuicStatus = BoolOk(Flags & QUIC_CLOSE_QUIC_STATUS);
            bool IsFirstCloseForConnection = true;

            if (ClosedRemotely && !Connection.State.ClosedLocally)
            {
                if (!Connection.State.Connected && QuicConnIsClient(Connection))
                {
                    SilentClose = true;
                }

                if (!SilentClose)
                {
                    QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_SHUTDOWN, Math.Max(15, Connection.Paths[0].SmoothedRtt * 2));
                    QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE);
                }
            }
            else if (!ClosedRemotely && !Connection.State.ClosedRemotely)
            {
                if (!SilentClose)
                {
                    long Pto = QuicLossDetectionComputeProbeTimeout(Connection.LossDetection, Connection.Paths[0], QUIC_CLOSE_PTO_COUNT);
                    QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_SHUTDOWN, Pto);
                    QuicSendSetSendFlag(Connection.Send, BoolOk(Flags & QUIC_CLOSE_APPLICATION) ? QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE : QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE);
                }
            }
            else
            {
                if (QuicConnIsClient(Connection))
                {

                }
                else if (!SilentClose)
                {
                    QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_SHUTDOWN, Math.Max(15, Connection.Paths[0].SmoothedRtt * 2));
                }

                IsFirstCloseForConnection = false;
            }

            if (IsFirstCloseForConnection)
            {
                Connection.State.ShutdownCompleteTimedOut = true;
                for (QUIC_CONN_TIMER_TYPE TimerType = QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_IDLE; TimerType < QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_SHUTDOWN; ++TimerType)
                {
                    QuicConnTimerCancel(Connection, TimerType);
                }

                if (ResultQuicStatus)
                {
                    Connection.CloseStatus = ErrorCode;
                    Connection.CloseErrorCode = QUIC_ERROR_INTERNAL_ERROR;
                }
                else
                {
                    Connection.CloseStatus = QuicErrorCodeToStatus(ErrorCode);
                    Connection.CloseErrorCode = ErrorCode;
                    if (QuicErrorIsProtocolError(ErrorCode))
                    {
                        QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_PROTOCOL_ERRORS);
                    }
                }

                if (BoolOk(Flags & QUIC_CLOSE_APPLICATION))
                {
                    Connection.State.AppClosed = true;
                }

                if (BoolOk(Flags & QUIC_CLOSE_SEND_NOTIFICATION) && Connection.State.ExternalOwner)
                {
                    QuicConnIndicateShutdownBegin(Connection);
                }

                if (Connection.CloseReasonPhrase != null)
                {
                    Connection.CloseReasonPhrase = null;
                }

                Connection.CloseReasonPhrase = RemoteReasonPhrase;
                QuicStreamSetShutdown(Connection.Streams);
                QuicDatagramSendShutdown(Connection.Datagram);
            }

            if (SilentClose)
            {
                QuicSendClear(Connection.Send);
            }

            if (SilentClose || (Connection.State.ClosedRemotely && Connection.State.ClosedLocally))
            {
                Connection.State.ShutdownCompleteTimedOut = false;
                Connection.State.ProcessShutdownComplete = true;
            }
        }

        static void QuicConnCloseLocally(QUIC_CONNECTION Connection, uint Flags, int ErrorCode, string ErrorMsg)
        {
            NetLog.Assert(ErrorMsg == null || ErrorMsg.Length < ushort.MaxValue);
            QuicConnTryClose(Connection, Flags, ErrorCode, ErrorMsg);
        }

        static void QuicConnCloseHandle(QUIC_CONNECTION Connection)
        {
            NetLog.LogError("QuicConnCloseHandle");
            NetLog.Assert(!Connection.State.HandleClosed);
            Connection.State.HandleClosed = true;
            QuicConnCloseLocally(Connection, QUIC_CLOSE_SILENT | QUIC_CLOSE_QUIC_STATUS, QUIC_STATUS_ABORTED, null);
            if (Connection.State.ProcessShutdownComplete)
            {
                QuicConnOnShutdownComplete(Connection);
            }
            QuicConnUnregister(Connection);
        }

        static QUIC_CONNECTION QuicStreamSetGetConnection(QUIC_STREAM_SET StreamSet)
        {
            return StreamSet.mConnection;
        }

        static int QuicConnIndicateEvent(QUIC_CONNECTION Connection, ref QUIC_CONNECTION_EVENT Event)
        {
            int Status;
            if (Connection.ClientCallbackHandler != null)
            {
                NetLog.Assert(!Connection.State.InlineApiExecution || Connection.State.HandleClosed);
                Status = Connection.ClientCallbackHandler(Connection, Connection.ClientContext, ref Event);
            }
            else
            {
                NetLog.Assert(Connection.State.HandleClosed || Connection.State.ShutdownComplete || !Connection.State.ExternalOwner);
                Status = QUIC_STATUS_INVALID_STATE;
            }
            return Status;
        }

        static void QuicConnOnShutdownComplete(QUIC_CONNECTION Connection)
        {
            Connection.State.ProcessShutdownComplete = false;
            if (Connection.State.ShutdownComplete)
            {
                return;
            }
            Connection.State.ShutdownComplete = true;
            Connection.State.UpdateWorker = false;

            QUIC_PATH Path = Connection.Paths[0];
            if (Path.Binding != null)
            {
                if (Path.EncryptionOffloading)
                {
                    QuicPathUpdateQeo(Connection, Path, CXPLAT_QEO_OPERATION.CXPLAT_QEO_OPERATION_REMOVE);
                }
                QuicBindingRemoveConnection(Connection.Paths[0].Binding, Connection);
            }

            QuicTimerWheelRemoveConnection(Connection.Worker.TimerWheel, Connection);
            QuicLossDetectionUninitialize(Connection.LossDetection);
            QuicSendUninitialize(Connection.Send);
            QuicDatagramSendShutdown(Connection.Datagram);

            if (Connection.State.ExternalOwner)
            {
                QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_SHUTDOWN_COMPLETE;
                Event.SHUTDOWN_COMPLETE.HandshakeCompleted = Connection.State.Connected;
                Event.SHUTDOWN_COMPLETE.PeerAcknowledgedShutdown = !Connection.State.ShutdownCompleteTimedOut;
                Event.SHUTDOWN_COMPLETE.AppCloseInProgress = Connection.State.HandleClosed;

                QuicConnIndicateEvent(Connection, ref Event);
                Connection.ClientCallbackHandler = null;
            }
            else
            {
                QuicConnUnregister(Connection);
                QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_HANDLE_OWNER);
            }
        }

        static void QuicConnLogOutFlowStats(QUIC_CONNECTION Connection)
        {

        }

        static void QuicConnQueueUnreachable(QUIC_CONNECTION Connection, QUIC_ADDR RemoteAddress)
        {
            if (Connection.Crypto.TlsState.ReadKey > QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL)
            {
                return;
            }

            QUIC_OPERATION ConnOper = QuicOperationAlloc(Connection.Partition, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_UNREACHABLE);
            if (ConnOper != null)
            {
                ConnOper.UNREACHABLE.RemoteAddress = RemoteAddress;
                QuicConnQueueOper(Connection, ConnOper);
            }
        }

        static void QuicConnTimerExpired(QUIC_CONNECTION Connection, long TimeNow)
        {
            bool FlushSendImmediate = false;
            Connection.EarliestExpirationTime = long.MaxValue;
            for (int Type = 0; Type < (int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_COUNT; ++Type)
            {
                if (Connection.ExpirationTimes[Type] <= TimeNow)
                {
                    Connection.ExpirationTimes[Type] = long.MaxValue;
                    if (Type == (int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_ACK_DELAY)
                    {
                        QuicSendProcessDelayedAckTimer(Connection.Send);
                        FlushSendImmediate = true;
                    }
                    else if (Type == (int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_PACING)
                    {
                        FlushSendImmediate = true;
                    }
                    else
                    {
                        QUIC_OPERATION Oper = null;
                        if ((Oper = QuicOperationAlloc(Connection.Partition, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_TIMER_EXPIRED)) != null)
                        {
                            Oper.TIMER_EXPIRED.Type = (QUIC_CONN_TIMER_TYPE)Type;
                            QuicConnQueueOper(Connection, Oper);
                        }
                    }
                }
                else if (Connection.ExpirationTimes[Type] < Connection.EarliestExpirationTime)
                {
                    Connection.EarliestExpirationTime = Connection.ExpirationTimes[Type];
                }
            }

            QuicTimerWheelUpdateConnection(Connection.Worker.TimerWheel, Connection);
            if (FlushSendImmediate)
            {
                QuicSendFlush(Connection.Send);
            }
        }

        static void QuicConnSilentlyAbort(QUIC_CONNECTION Connection)
        {
            QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL | QUIC_CLOSE_QUIC_STATUS | QUIC_CLOSE_SILENT, QUIC_STATUS_ABORTED, null);
        }

        //当一个本地 Connection ID（CID）不再需要时，主动将其标记为“退役”状态，并通知对端该 CID 不再使用。
        static void QuicConnRetireCid(QUIC_CONNECTION Connection, QUIC_CID DestCid)
        {
            Connection.DestCidCount--;
            DestCid.Retired = true;
            DestCid.NeedsToSend = true;
            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_RETIRE_CONNECTION_ID);

            Connection.RetiredDestCidCount++;
            if (Connection.RetiredDestCidCount > 8 * QUIC_ACTIVE_CONNECTION_ID_LIMIT)
            {
                QuicConnSilentlyAbort(Connection);
            }
        }

        static QUIC_CID QuicConnGetUnusedDestCid(QUIC_CONNECTION Connection)
        {
            for (CXPLAT_LIST_ENTRY Entry = Connection.DestCids.Next; Entry != Connection.DestCids; Entry = Entry.Next)
            {
                QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                if (!DestCid.UsedLocally && !DestCid.Retired)
                {
                    return DestCid;
                }
            }
            return null;
        }

        //弃用 当前的目的CID
        static bool QuicConnRetireCurrentDestCid(QUIC_CONNECTION Connection, QUIC_PATH Path)
        {
            if (Path.DestCid.Data.Length == 0)
            {
                return true;
            }

            QUIC_CID NewDestCid = QuicConnGetUnusedDestCid(Connection);
            if (NewDestCid == null)
            {
                return false;
            }

            NetLog.Assert(Path.DestCid != NewDestCid);
            QUIC_CID OldDestCid = Path.DestCid;
            QuicConnRetireCid(Connection, Path.DestCid);
            Path.DestCid = NewDestCid;
            Path.DestCid.UsedLocally = true;
            Connection.Stats.Misc.DestCidUpdateCount++;
            return true;
        }

        static void QuicMtuDiscoveryCheckSearchCompleteTimeout(QUIC_CONNECTION Connection, long TimeNow)
        {
            long TimeoutTime = Connection.Settings.MtuDiscoverySearchCompleteTimeoutUs;
            for (int i = 0; i < Connection.PathsCount; i++)
            {
                QUIC_PATH Path = Connection.Paths[i];
                if (!Path.IsActive || !Path.MtuDiscovery.IsSearchComplete)
                {
                    continue;
                }
                if (CxPlatTimeDiff(Path.MtuDiscovery.SearchCompleteEnterTimeUs, TimeNow) >= TimeoutTime)
                {
                    QuicMtuDiscoveryMoveToSearching(Path.MtuDiscovery, Connection);
                }
            }
        }

        static bool QuicConnRemoveOutFlowBlockedReason(QUIC_CONNECTION Connection, byte Reason)
        {
            if (BoolOk(Connection.OutFlowBlockedReasons & Reason))
            {
                long Now = CxPlatTime();
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_PACING) && BoolOk(Reason & QUIC_FLOW_BLOCKED_PACING))
                {
                    Connection.BlockedTimings.Pacing.CumulativeTimeUs += CxPlatTimeDiff(Connection.BlockedTimings.Pacing.LastStartTimeUs, Now);
                    Connection.BlockedTimings.Pacing.LastStartTimeUs = 0;
                }
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_SCHEDULING) && BoolOk(Reason & QUIC_FLOW_BLOCKED_SCHEDULING))
                {
                    Connection.BlockedTimings.Scheduling.CumulativeTimeUs += CxPlatTimeDiff(Connection.BlockedTimings.Scheduling.LastStartTimeUs, Now);
                    Connection.BlockedTimings.Scheduling.LastStartTimeUs = 0;
                }
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT) && BoolOk(Reason & QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT))
                {
                    Connection.BlockedTimings.AmplificationProt.CumulativeTimeUs += CxPlatTimeDiff(Connection.BlockedTimings.AmplificationProt.LastStartTimeUs, Now);
                    Connection.BlockedTimings.AmplificationProt.LastStartTimeUs = 0;
                }
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_CONGESTION_CONTROL) && BoolOk(Reason & QUIC_FLOW_BLOCKED_CONGESTION_CONTROL))
                {
                    Connection.BlockedTimings.CongestionControl.CumulativeTimeUs += CxPlatTimeDiff(Connection.BlockedTimings.CongestionControl.LastStartTimeUs, Now);
                    Connection.BlockedTimings.CongestionControl.LastStartTimeUs = 0;
                }
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_CONN_FLOW_CONTROL) && BoolOk(Reason & QUIC_FLOW_BLOCKED_CONN_FLOW_CONTROL))
                {
                    Connection.BlockedTimings.FlowControl.CumulativeTimeUs += CxPlatTimeDiff(Connection.BlockedTimings.FlowControl.LastStartTimeUs, Now);
                    Connection.BlockedTimings.FlowControl.LastStartTimeUs = 0;
                }

                Connection.OutFlowBlockedReasons = (byte)(Connection.OutFlowBlockedReasons & ~Reason);
                return true;
            }
            return false;
        }

        static void QuicConnFatalError(QUIC_CONNECTION Connection, int Status, string ErrorMsg)
        {
            NetLog.LogError(ErrorMsg);
            QuicConnCloseLocally(
                Connection,
                QUIC_CLOSE_INTERNAL | QUIC_CLOSE_QUIC_STATUS,
                Status,
                ErrorMsg);
        }

        static bool QuicConnDrainOperations(QUIC_CONNECTION Connection, ref bool StillHasPriorityWork)
        {
            QUIC_OPERATION Oper;
            int MaxOperationCount = Connection.Settings.MaxOperationsPerDrain;
            int OperationCount = 0;
            bool HasMoreWorkToDo = true;

            if (!Connection.State.Initialized && !Connection.State.ShutdownComplete)
            {
                NetLog.Assert(QuicConnIsServer(Connection));
                int Status;
                if (QUIC_FAILED(Status = QuicCryptoInitialize(Connection.Crypto)))
                {
                    QuicConnFatalError(Connection, Status, "Lazily initialize failure");
                }
                else
                {
                    Connection.State.Initialized = true;
                    if (Connection.Settings.KeepAliveIntervalMs != 0)
                    {
                        QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_KEEP_ALIVE, Connection.Settings.KeepAliveIntervalMs);
                    }
                }
            }

            while (!Connection.State.UpdateWorker && OperationCount++ < MaxOperationCount)
            {
                Oper = QuicOperationDequeue(Connection.OperQ, Connection.Partition);
                if (Oper == null)
                {
                    HasMoreWorkToDo = false;
                    break;
                }

                bool FreeOper = Oper.FreeAfterProcess;
                switch (Oper.Type)
                {
                    case QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL:
                        NetLog.Assert(Oper.API_CALL.Context != null);
                        QuicConnProcessApiOperation(Connection, Oper.API_CALL.Context);
                        break;

                    case QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_RECV:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }

                        if (!QuicConnFlushRecv(Connection))
                        {
                            FreeOper = false;
                            QuicOperationEnqueue(Connection.OperQ, Connection.Partition, Oper);
                        }
                        break;

                    case QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_UNREACHABLE:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }
                        QuicConnProcessUdpUnreachable(Connection, Oper.UNREACHABLE.RemoteAddress);
                        break;

                    case QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_STREAM_RECV:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }
                        QuicStreamRecvFlush(Oper.FLUSH_STREAM_RECEIVE.Stream);
                        break;

                    case QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_SEND:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }
                        if (QuicSendFlush(Connection.Send))
                        {
                            Connection.Send.FlushOperationPending = false;
                        }
                        else
                        {
                            FreeOper = false;
                            QuicOperationEnqueue(Connection.OperQ, Connection.Partition, Oper);
                        }
                        break;

                    case QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_TIMER_EXPIRED:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }
                        QuicConnProcessExpiredTimer(Connection, Oper.TIMER_EXPIRED.Type);
                        break;

                    case QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_TRACE_RUNDOWN:
                        QuicConnTraceRundownOper(Connection);
                        break;

                    case QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_ROUTE_COMPLETION:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }
                        QuicConnProcessRouteCompletion(Connection, Oper.ROUTE.PhysicalAddress, Oper.ROUTE.PathId, Oper.ROUTE.Succeeded);
                        break;

                    default:
                        NetLog.Assert(false);
                        break;
                }

                QuicConnValidate(Connection);

                if (FreeOper)
                {
                    QuicOperationFree(Connection.Partition, Oper);
                }

                Connection.Stats.Schedule.OperationCount++;
                QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_COMPLETED);
            }

            if (Connection.State.ProcessShutdownComplete)
            {
                QuicConnOnShutdownComplete(Connection);
            }

            if (!Connection.State.ShutdownComplete)
            {
                if (OperationCount >= MaxOperationCount && BoolOk(Connection.Send.SendFlags & QUIC_CONN_SEND_FLAG_ACK))
                {
                    QuicSendFlush(Connection.Send);
                }
            }

            QuicStreamSetDrainClosedStreams(Connection.Streams);
            QuicConnValidate(Connection);

            if (HasMoreWorkToDo)
            {
                StillHasPriorityWork = QuicOperationHasPriority(Connection.OperQ);
                return true;
            }

            return false;
        }

        static void QuicConnProcessIdleTimerOperation(QUIC_CONNECTION Connection)
        {
            QuicConnCloseLocally(
                Connection,
                QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS,
                QUIC_STATUS_CONNECTION_IDLE,
                null);
        }

        static void QuicConnProcessKeepAliveOperation(QUIC_CONNECTION Connection)
        {
            Connection.Send.TailLossProbeNeeded = true;
            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_PING);
            QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_KEEP_ALIVE, Connection.Settings.KeepAliveIntervalMs);
        }

        static void QuicConnProcessShutdownTimerOperation(QUIC_CONNECTION Connection)
        {
            Connection.State.ClosedRemotely = true;
            Connection.State.ProcessShutdownComplete = true;
        }

        static void QuicConnProcessExpiredTimer(QUIC_CONNECTION Connection, QUIC_CONN_TIMER_TYPE Type)
        {
            switch (Type)
            {
                case QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_IDLE:
                    QuicConnProcessIdleTimerOperation(Connection);
                    break;
                case QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_LOSS_DETECTION:
                    QuicLossDetectionProcessTimerOperation(Connection.LossDetection);
                    break;
                case QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_KEEP_ALIVE:
                    QuicConnProcessKeepAliveOperation(Connection);
                    break;
                case QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_SHUTDOWN:
                    QuicConnProcessShutdownTimerOperation(Connection);
                    break;
                default:
                    NetLog.Assert(false);
                    break;
            }
        }

        static void QuicConnProcessApiOperation(QUIC_CONNECTION Connection, QUIC_API_CONTEXT ApiCtx)
        {
            int Status = QUIC_STATUS_SUCCESS;
            int ApiStatus = ApiCtx.Status;
            EventWaitHandle ApiCompleted = ApiCtx.Completed;

            switch (ApiCtx.Type)
            {
                case QUIC_API_TYPE.QUIC_API_TYPE_CONN_CLOSE:
                    QuicConnCloseHandle(Connection);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_CONN_SHUTDOWN:
                    QuicConnShutdown(
                        Connection,
                        ApiCtx.CONN_SHUTDOWN.Flags,
                        ApiCtx.CONN_SHUTDOWN.ErrorCode,
                        ApiCtx.CONN_SHUTDOWN.RegistrationShutdown,
                        ApiCtx.CONN_SHUTDOWN.TransportShutdown);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_CONN_START:
                    Status = QuicConnStart(
                            Connection,
                            ApiCtx.CONN_START.Configuration,
                            ApiCtx.CONN_START.Family,
                            ApiCtx.CONN_START.ServerName,
                            ApiCtx.CONN_START.ServerPort);
                    ApiCtx.CONN_START.ServerName = null;
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_CONN_SET_CONFIGURATION:
                    Status = QuicConnSetConfiguration(Connection, ApiCtx.CONN_SET_CONFIGURATION.Configuration);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_CONN_SEND_RESUMPTION_TICKET:
                    NetLog.Assert(QuicConnIsServer(Connection));
                    Status = QuicConnSendResumptionTicket(Connection, ApiCtx.CONN_SEND_RESUMPTION_TICKET.ResumptionAppData);
                    ApiCtx.CONN_SEND_RESUMPTION_TICKET.ResumptionAppData = null;
                    if (BoolOk(ApiCtx.CONN_SEND_RESUMPTION_TICKET.Flags & QUIC_SEND_RESUMPTION_FLAG_FINAL))
                    {
                        Connection.State.ResumptionEnabled = false;
                    }
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION:
                    NetLog.Assert(QuicConnIsServer(Connection));
                    QuicCryptoCustomTicketValidationComplete(Connection.Crypto, ApiCtx.CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION.Result);
                    break;
                case QUIC_API_TYPE.QUIC_API_TYPE_CONN_COMPLETE_CERTIFICATE_VALIDATION:
                    QuicCryptoCustomCertValidationComplete(Connection.Crypto,
                        ApiCtx.CONN_COMPLETE_CERTIFICATE_VALIDATION.Result,
                        ApiCtx.CONN_COMPLETE_CERTIFICATE_VALIDATION.TlsAlert);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_CLOSE:
                    QuicStreamClose(ApiCtx.STRM_CLOSE.Stream);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_SHUTDOWN:
                    QuicStreamShutdown(ApiCtx.STRM_SHUTDOWN.Stream, ApiCtx.STRM_SHUTDOWN.Flags, ApiCtx.STRM_SHUTDOWN.ErrorCode);
                    break;
                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_START:
                    Status = QuicStreamStart(ApiCtx.STRM_START.Stream, ApiCtx.STRM_START.Flags, false);
                    break;
                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_SEND:
                    QuicStreamSendFlush(ApiCtx.STRM_SEND.Stream);
                    break;
                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_COMPLETE:
                    QuicStreamReceiveCompletePending(ApiCtx.STRM_RECV_COMPLETE.Stream);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_SET_ENABLED:
                    Status = QuicStreamRecvSetEnabledState(ApiCtx.STRM_RECV_SET_ENABLED.Stream, ApiCtx.STRM_RECV_SET_ENABLED.IsEnabled);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_SET_PARAM:
                    Status = QuicLibrarySetParam(ApiCtx.SET_PARAM.Handle, ApiCtx.SET_PARAM.Param, ApiCtx.SET_PARAM.Buffer.GetSpan());
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_GET_PARAM:
                    Status = QuicLibraryGetParam(ApiCtx.GET_PARAM.Handle, ApiCtx.GET_PARAM.Param, ApiCtx.GET_PARAM.Buffer);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_DATAGRAM_SEND:
                    QuicDatagramSendFlush(Connection.Datagram);
                    break;

                default:
                    NetLog.Assert(false);
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
            }

            if(QUIC_FAILED(Status))
            {
                NetLog.LogError($"Operation ApiCtx.Type: {ApiCtx.Type}, Status: {Status}");
            }

            if (ApiStatus != 0)
            {
                ApiStatus = Status;
            }

            if (ApiCompleted != null)
            {
                CxPlatEventSet(ApiCompleted);
            }
        }

        static int QuicConnStart(QUIC_CONNECTION Connection, QUIC_CONFIGURATION Configuration, AddressFamily Family, string ServerName, int ServerPort)
        {
            int Status;
            QUIC_PATH Path = Connection.Paths[0];
            NetLog.Assert(QuicConnIsClient(Connection));

            if (Connection.State.ClosedLocally || Connection.State.Started)
            {
                return QUIC_STATUS_INVALID_STATE;
            }

            bool RegistrationShutingDown;
            int ShutdownErrorCode;
            CxPlatDispatchLockAcquire(Connection.Registration.ConnectionLock);
            ShutdownErrorCode = Connection.Registration.ShutdownErrorCode;
            QUIC_CONNECTION_SHUTDOWN_FLAGS ShutdownFlags = Connection.Registration.ShutdownFlags;
            RegistrationShutingDown = Connection.Registration.ShuttingDown;
            CxPlatDispatchLockRelease(Connection.Registration.ConnectionLock);

            if (RegistrationShutingDown)
            {
                QuicConnShutdown(Connection, ShutdownFlags, ShutdownErrorCode, false, false);
                return QUIC_STATUS_INVALID_STATE;
            }

            NetLog.Assert(Path.Binding == null);
            if (!Connection.State.RemoteAddressSet)
            {
                NetLog.Assert(ServerName != null);
                Connection.State.RemoteAddressSet = true;
            }

            if (QuicAddrIsWildCard(Path.Route.RemoteAddress))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            CXPLAT_UDP_CONFIG UdpConfig = new CXPLAT_UDP_CONFIG();
            UdpConfig.LocalAddress = Connection.State.LocalAddressSet ? Path.Route.LocalAddress : null;
            UdpConfig.RemoteAddress = Path.Route.RemoteAddress;
            UdpConfig.Flags = Connection.State.ShareBinding ? CXPLAT_SOCKET_FLAG_SHARE : 0;
            UdpConfig.InterfaceIndex = Connection.State.LocalInterfaceSet ? (int)Path.Route.LocalAddress.Ip.ScopeId : 0;
            UdpConfig.PartitionIndex = QuicPartitionIdGetIndex(Connection.PartitionID);
            Status = QuicLibraryGetBinding(UdpConfig, out Path.Binding);

            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            QUIC_CID SourceCid;
            if (Connection.State.ShareBinding)
            {
                SourceCid = QuicCidNewRandomSource(Connection, QUIC_SSBuffer.Empty, Connection.PartitionID, Connection.CibirId[0], new QUIC_SSBuffer(Connection.CibirId, 2));
            }
            else
            {
                SourceCid = QuicCidNewNullSource(Connection);
            }

            if (SourceCid == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            Connection.NextSourceCidSequenceNumber++;
            CxPlatListInsertHead(Connection.SourceCids, SourceCid.Link);

            if (!QuicBindingAddSourceConnectionID(Path.Binding, SourceCid))
            {
                QuicLibraryReleaseBinding(Path.Binding);
                Path.Binding = null;
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            Connection.State.LocalAddressSet = true;
            QuicBindingGetLocalAddress(Path.Binding, out Path.Route.LocalAddress);
            Connection.RemoteServerName = ServerName;

            Status = QuicCryptoInitialize(Connection.Crypto);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            Status = QuicConnSetConfiguration(Connection, Configuration);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            if (Connection.Settings.KeepAliveIntervalMs != 0)
            {
                QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_KEEP_ALIVE, Connection.Settings.KeepAliveIntervalMs);
            }

        Exit:
            if (QUIC_FAILED(Status))
            {
                QuicConnCloseLocally(Connection,
                    QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS,
                    Status,
                    null);
            }

            return Status;
        }

        static void QuicConnShutdown(QUIC_CONNECTION Connection, QUIC_CONNECTION_SHUTDOWN_FLAGS Flags, int ErrorCode, bool ShutdownFromRegistration, bool ShutdownFromTransport)
        {
            if (ShutdownFromRegistration && !Connection.State.Started && QuicConnIsClient(Connection))
            {
                return;
            }

            uint CloseFlags = ShutdownFromTransport ? QUIC_CLOSE_INTERNAL : QUIC_CLOSE_APPLICATION;
            if (BoolOk((uint)Flags & (uint)QUIC_CONNECTION_SHUTDOWN_FLAGS.QUIC_CONNECTION_SHUTDOWN_FLAG_SILENT) ||
                (!Connection.State.Started && QuicConnIsClient(Connection)))
            {
                CloseFlags |= QUIC_CLOSE_SILENT;
            }

            QuicConnCloseLocally(Connection, CloseFlags, ErrorCode, null);
        }

        static void QuicConnQueueRecvPackets(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packets, int PacketChainLength, int PacketChainByteLength)
        {
            QUIC_RX_PACKET PacketsTail = Packets;
            QUIC_RX_PACKET Last_PacketsTail = null; //尾包应该是一个非空包
            while (PacketsTail != null)
            {
                Last_PacketsTail = PacketsTail;
                PacketsTail.QueuedOnConnection = true;
                PacketsTail.AssignedToConnection = true;
                PacketsTail = (QUIC_RX_PACKET)PacketsTail.Next;
            }
            //上面是计算出最后一个尾包
            
            int QueueLimit = Math.Max(10, (int)Connection.Settings.ConnFlowControlWindow >> 10);
            bool QueueOperation;
            CxPlatDispatchLockAcquire(Connection.ReceiveQueueLock);
            if (Connection.ReceiveQueueCount >= QueueLimit)
            {
                QueueOperation = false;
            }
            else
            {
                if (Connection.ReceiveQueueTail == null)
                {
                    Connection.ReceiveQueue = Packets;
                }
                else
                {
                    Connection.ReceiveQueueTail.Next = Packets;
                }
                Connection.ReceiveQueueTail = Last_PacketsTail;

                Packets = null;
                QueueOperation = (Connection.ReceiveQueueCount == 0);
                Connection.ReceiveQueueCount += PacketChainLength;
                Connection.ReceiveQueueByteCount += PacketChainByteLength;
            }

            CxPlatDispatchLockRelease(Connection.ReceiveQueueLock);

            if (Packets != null)
            {
                QUIC_RX_PACKET Packet = Packets;
                do
                {
                    Packet.QueuedOnConnection = false;
                    QuicPacketLogDrop(Connection, Packet, "Max queue limit reached");
                } while ((Packet = (QUIC_RX_PACKET)Packet.Next) != null);
                CxPlatRecvDataReturn(Packets);
                return;
            }

            if (QueueOperation)
            {
                QUIC_OPERATION ConnOper = QuicOperationAlloc(Connection.Partition, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_RECV);
                if (ConnOper != null)
                {
                    QuicConnQueueOper(Connection, ConnOper);
                }
            }
        }

        static void QuicConnTransportError(QUIC_CONNECTION Connection, int ErrorCode, string reason = null)
        {
            if(string.IsNullOrWhiteSpace(reason))
            {
                NetLog.LogError($"QuicConnTransportError: {ErrorCode}");
            }
            else
            {
                NetLog.LogError($"QuicConnTransportError: {ErrorCode}, {reason}");
            }
              
            QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL, ErrorCode, null);
        }
        
        static QUIC_CID QuicConnGenerateNewSourceCid(QUIC_CONNECTION Connection, bool IsInitial)
        {
            int TryCount = 0;
            QUIC_CID SourceCid;

            if (!Connection.State.ShareBinding)
            {
                return null;
            }

            do
            {
                SourceCid = QuicCidNewRandomSource(
                        Connection,
                        Connection.ServerID,
                        Connection.PartitionID,
                        Connection.CibirId[0],
                        new QUIC_SSBuffer(Connection.CibirId, 2));

                if (SourceCid == null)
                {
                    QuicConnFatalError(Connection, QUIC_STATUS_INTERNAL_ERROR, null);
                    return null;
                }

                if (!QuicBindingAddSourceConnectionID(Connection.Paths[0].Binding, SourceCid))
                {
                    SourceCid = null;
                    if (++TryCount > QUIC_CID_MAX_COLLISION_RETRY)
                    {
                        QuicConnFatalError(Connection, QUIC_STATUS_INTERNAL_ERROR, null);
                        return null;
                    }
                }
            }while (SourceCid == null);

            SourceCid.SequenceNumber = Connection.NextSourceCidSequenceNumber++;
            if (SourceCid.SequenceNumber > 0)
            {
                SourceCid.NeedsToSend = true;
                QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_NEW_CONNECTION_ID);
            }

            if (IsInitial)
            {
                SourceCid.IsInitial = true;
                CxPlatListInsertHead(Connection.SourceCids, SourceCid.Link);
            }
            else
            {
                CxPlatListInsertTail(Connection.SourceCids, SourceCid.Link);
            }
            return SourceCid;
        }

        static int QuicConnProcessPeerTransportParameters(QUIC_CONNECTION Connection, bool FromResumptionTicket)
        {
            int Status = QUIC_STATUS_SUCCESS;
            Connection.State.PeerTransportParameterValid = true;

            if (BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_ACTIVE_CONNECTION_ID_LIMIT))
            {
                NetLog.Assert(Connection.PeerTransportParams.ActiveConnectionIdLimit >= QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_MIN);
                if (Connection.SourceCidLimit > Connection.PeerTransportParams.ActiveConnectionIdLimit)
                {
                    Connection.SourceCidLimit = (byte)Connection.PeerTransportParams.ActiveConnectionIdLimit;
                }
            }
            else
            {
                Connection.SourceCidLimit = QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_DEFAULT;
            }

            if (!FromResumptionTicket)
            {
                if (Connection.Settings.VersionNegotiationExtEnabled && BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_VERSION_NEGOTIATION))
                {
                    Status = QuicConnProcessPeerVersionNegotiationTP(Connection);
                    if (QUIC_FAILED(Status))
                    {
                        goto Error;
                    }
                }
                if (QuicConnIsClient(Connection) &&
                    (Connection.State.CompatibleVerNegotiationAttempted || Connection.PreviousQuicVersion != 0) &&
                    !BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_VERSION_NEGOTIATION))
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_VERSION_NEGOTIATION_ERROR);
                    Status = QUIC_STATUS_PROTOCOL_ERROR;
                    goto Error;
                }

                if (BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_STATELESS_RESET_TOKEN))
                {
                    NetLog.Assert(!CxPlatListIsEmpty(Connection.DestCids));
                    NetLog.Assert(QuicConnIsClient(Connection));
                    QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.DestCids.Next);
                    Array.Copy(Connection.PeerTransportParams.StatelessResetToken, DestCid.ResetToken, QUIC_STATELESS_RESET_TOKEN_LENGTH);
                    DestCid.HasResetToken = true;
                }

                if (BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_PREFERRED_ADDRESS))
                {

                }

                if (Connection.Settings.GreaseQuicBitEnabled && (Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_GREASE_QUIC_BIT) > 0)
                {
                    byte RandomValue = CxPlatRandom.RandomByte();
                    Connection.State.FixedBit = BoolOk(RandomValue % 2);
                    Connection.Stats.GreaseBitNegotiated = true;
                }

                if (Connection.Settings.ReliableResetEnabled)
                {
                    Connection.State.ReliableResetStreamNegotiated = BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_RELIABLE_RESET_ENABLED);
                    QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                    Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_RELIABLE_RESET_NEGOTIATED;
                    Event.RELIABLE_RESET_NEGOTIATED.IsNegotiated = Connection.State.ReliableResetStreamNegotiated;
                    QuicConnIndicateEvent(Connection, ref Event);
                }

                if (Connection.Settings.OneWayDelayEnabled)
                {
                    Connection.State.TimestampSendNegotiated = BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED);
                    Connection.State.TimestampRecvNegotiated = BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED);

                    QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                    Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_ONE_WAY_DELAY_NEGOTIATED;
                    Event.ONE_WAY_DELAY_NEGOTIATED.SendNegotiated = Connection.State.TimestampSendNegotiated;
                    Event.ONE_WAY_DELAY_NEGOTIATED.ReceiveNegotiated = Connection.State.TimestampRecvNegotiated;
                    QuicConnIndicateEvent(Connection, ref Event);
                }

                if (!QuicConnValidateTransportParameterCIDs(Connection))
                {
                    goto Error;
                }

                if (QuicConnIsClient(Connection) &&
                    !QuicConnPostAcceptValidatePeerTransportParameters(Connection))
                {
                    goto Error;
                }
            }

            Connection.Send.PeerMaxData = Connection.PeerTransportParams.InitialMaxData;
            QuicStreamSetInitializeTransportParameters(
                Connection.Streams,
                Connection.PeerTransportParams.InitialMaxBidiStreams,
                Connection.PeerTransportParams.InitialMaxUniStreams,
                !FromResumptionTicket);

            QuicDatagramOnSendStateChanged(Connection.Datagram);

            if (Connection.State.Started)
            {
                if (Connection.State.Disable1RttEncrytion && BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_DISABLE_1RTT_ENCRYPTION))
                {

                }
                else
                {
                    Connection.State.Disable1RttEncrytion = false;
                }
            }

            return QUIC_STATUS_SUCCESS;
        Error:
            if (Status == QUIC_STATUS_SUCCESS)
            {
                QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                Status = QUIC_STATUS_PROTOCOL_ERROR;
            }
            return Status;
        }

        static int QuicConnProcessPeerVersionNegotiationTP(QUIC_CONNECTION Connection)
        {
            int Status;
            if (QuicConnIsServer(Connection))
            {
                var SupportedVersions = DefaultSupportedVersionsList;

                int CurrentVersionIndex = 0;
                for (; CurrentVersionIndex < SupportedVersions.Count; ++CurrentVersionIndex)
                {
                    if (Connection.Stats.QuicVersion == SupportedVersions[CurrentVersionIndex])
                    {
                        break;
                    }
                }

                if (CurrentVersionIndex == SupportedVersions.Count)
                {
                    NetLog.Assert(false, "Incompatible Version Negotation should happen in binding layer");
                    return QUIC_STATUS_VER_NEG_ERROR;
                }

                QUIC_VERSION_INFORMATION_V1 ClientVI = new QUIC_VERSION_INFORMATION_V1();
                Status = QuicVersionNegotiationExtParseVersionInfo(Connection,
                        Connection.PeerTransportParams.VersionInfo,
                        ClientVI);

                if (QUIC_FAILED(Status))
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                    return QUIC_STATUS_PROTOCOL_ERROR;
                }

                if (ClientVI.ChosenVersion == 0)
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                    return QUIC_STATUS_PROTOCOL_ERROR;
                }

                if (Connection.Stats.QuicVersion != ClientVI.ChosenVersion)
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                    return QUIC_STATUS_PROTOCOL_ERROR;
                }

                for (int ServerVersionIdx = 0; ServerVersionIdx < CurrentVersionIndex; ++ServerVersionIdx)
                {
                    if (QuicIsVersionReserved(SupportedVersions[ServerVersionIdx]))
                    {
                        continue;
                    }

                    for (int ClientVersionIdx = 0; ClientVersionIdx < ClientVI.AvailableVersions.Count; ++ClientVersionIdx)
                    {
                        if (ClientVI.AvailableVersions[ClientVersionIdx] == 0)
                        {
                            QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                            return QUIC_STATUS_PROTOCOL_ERROR;
                        }

                        if (!QuicIsVersionReserved(ClientVI.AvailableVersions[ClientVersionIdx]) &&
                            ClientVI.AvailableVersions[ClientVersionIdx] == SupportedVersions[ServerVersionIdx] &&
                            QuicVersionNegotiationExtAreVersionsCompatible(ClientVI.ChosenVersion, ClientVI.AvailableVersions[ClientVersionIdx]))
                        {
                            Connection.Stats.QuicVersion = SupportedVersions[ServerVersionIdx];
                            QuicConnOnQuicVersionSet(Connection);
                            Status = QuicCryptoOnVersionChange(Connection.Crypto);
                            if (QUIC_FAILED(Status))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_VERSION_NEGOTIATION_ERROR);
                                return QUIC_STATUS_INTERNAL_ERROR;
                            }
                        }
                    }
                }
            }
            else
            {
                QUIC_VERSION_INFORMATION_V1 ServerVI = new QUIC_VERSION_INFORMATION_V1();
                Status = QuicVersionNegotiationExtParseVersionInfo(Connection,
                        Connection.PeerTransportParams.VersionInfo,
                        ServerVI);

                if (QUIC_FAILED(Status))
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                    return QUIC_STATUS_PROTOCOL_ERROR;
                }

                if (ServerVI.ChosenVersion == 0)
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                    return QUIC_STATUS_PROTOCOL_ERROR;
                }

                if (Connection.Stats.QuicVersion != ServerVI.ChosenVersion)
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                    return QUIC_STATUS_PROTOCOL_ERROR;
                }

                uint ClientChosenVersion = 0;
                bool OriginalVersionFound = false;
                for (int i = 0; i < ServerVI.AvailableVersions.Count; ++i)
                {
                    if (ServerVI.AvailableVersions[i] == 0)
                    {
                        QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                        return QUIC_STATUS_PROTOCOL_ERROR;
                    }

                    if (Connection.Stats.VersionNegotiation != null && ClientChosenVersion == 0 &&
                        QuicVersionNegotiationExtIsVersionClientSupported(Connection, ServerVI.AvailableVersions[i]))
                    {
                        ClientChosenVersion = ServerVI.AvailableVersions[i];
                    }
                    if (Connection.OriginalQuicVersion == ServerVI.AvailableVersions[i])
                    {
                        OriginalVersionFound = true;
                    }
                }
                if (ClientChosenVersion == 0 && QuicVersionNegotiationExtIsVersionClientSupported(Connection, ServerVI.ChosenVersion))
                {
                    ClientChosenVersion = ServerVI.ChosenVersion;
                }

                if (ClientChosenVersion == 0 || (ClientChosenVersion != Connection.OriginalQuicVersion && ClientChosenVersion != ServerVI.ChosenVersion))
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_VERSION_NEGOTIATION_ERROR);
                    return QUIC_STATUS_PROTOCOL_ERROR;
                }

                if (Connection.PreviousQuicVersion != 0)
                {
                    if (Connection.PreviousQuicVersion == ServerVI.ChosenVersion)
                    {
                        QuicConnTransportError(Connection, QUIC_ERROR_VERSION_NEGOTIATION_ERROR);
                        return QUIC_STATUS_PROTOCOL_ERROR;
                    }

                    if (!QuicIsVersionReserved(Connection.PreviousQuicVersion))
                    {
                        for (int i = 0; i < ServerVI.AvailableVersions.Count; ++i)
                        {
                            if (Connection.PreviousQuicVersion == ServerVI.AvailableVersions[i])
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_VERSION_NEGOTIATION_ERROR);
                                return QUIC_STATUS_PROTOCOL_ERROR;
                            }
                        }
                    }
                }

                if (Connection.State.CompatibleVerNegotiationAttempted)
                {
                    if (!QuicVersionNegotiationExtAreVersionsCompatible(Connection.OriginalQuicVersion, ServerVI.ChosenVersion))
                    {
                        QuicConnTransportError(Connection, QUIC_ERROR_VERSION_NEGOTIATION_ERROR);
                        return QUIC_STATUS_PROTOCOL_ERROR;
                    }
                    if (!OriginalVersionFound)
                    {
                        QuicConnTransportError(Connection, QUIC_ERROR_VERSION_NEGOTIATION_ERROR);
                        return QUIC_STATUS_PROTOCOL_ERROR;
                    }
                    Connection.State.CompatibleVerNegotiationCompleted = true;
                }
            }
            return QUIC_STATUS_SUCCESS;
        }

        static bool QuicConnAddOutFlowBlockedReason(QUIC_CONNECTION Connection, uint Reason)
        {
            NetLog.Assert((Reason & (Reason - 1)) == 0, "More than one reason is not allowed");
            if (!BoolOk(Connection.OutFlowBlockedReasons & Reason))
            {
                long Now = CxPlatTime();
                if (BoolOk(Reason & QUIC_FLOW_BLOCKED_PACING))
                {
                    Connection.BlockedTimings.Pacing.LastStartTimeUs = Now;
                }
                if (BoolOk(Reason & QUIC_FLOW_BLOCKED_SCHEDULING))
                {
                    Connection.BlockedTimings.Scheduling.LastStartTimeUs = Now;
                }
                if (BoolOk(Reason & QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT))
                {
                    Connection.BlockedTimings.AmplificationProt.LastStartTimeUs = Now;
                }
                if (BoolOk(Reason & QUIC_FLOW_BLOCKED_CONGESTION_CONTROL))
                {
                    Connection.BlockedTimings.CongestionControl.LastStartTimeUs = Now;
                }
                if (BoolOk(Reason & QUIC_FLOW_BLOCKED_CONN_FLOW_CONTROL))
                {
                    Connection.BlockedTimings.FlowControl.LastStartTimeUs = Now;
                }

                Connection.OutFlowBlockedReasons = (byte)(Reason | Connection.OutFlowBlockedReasons);
                return true;
            }
            return false;
        }

        static long QuicConnGetAckDelay(QUIC_CONNECTION Connection)
        {
            if (Connection.Settings.MaxAckDelayMs > 0 && (MsQuicLib.ExecutionConfig == null ||
                Connection.Settings.MaxAckDelayMs > MsQuicLib.ExecutionConfig.PollingIdleTimeoutUs))
            {
                return Connection.Settings.MaxAckDelayMs + MsQuicLib.TimerResolutionMs;
            }
            return Connection.Settings.MaxAckDelayMs;
        }

        static void QuicConnUpdatePeerPacketTolerance(QUIC_CONNECTION Connection, byte NewPacketTolerance)
        {
            if (BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_MIN_ACK_DELAY) && Connection.PeerPacketTolerance != NewPacketTolerance)
            {
                Connection.SendAckFreqSeqNum++;
                Connection.PeerPacketTolerance = NewPacketTolerance;
                QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_ACK_FREQUENCY);
            }
        }

        static bool QuicConnFlushRecv(QUIC_CONNECTION Connection)
        {
            bool FlushedAll;
            int ReceiveQueueCount, ReceiveQueueByteCount;
            CxPlatDispatchLockAcquire(Connection.ReceiveQueueLock);
            QUIC_RX_PACKET ReceiveQueue = Connection.ReceiveQueue;
            if (Connection.ReceiveQueueCount > QUIC_MAX_RECEIVE_FLUSH_COUNT)
            {
                FlushedAll = false;
                Connection.ReceiveQueueCount -= QUIC_MAX_RECEIVE_FLUSH_COUNT;
                QUIC_RX_PACKET Tail = Connection.ReceiveQueue;
                ReceiveQueueCount = 0;
                ReceiveQueueByteCount = 0;
                while (++ReceiveQueueCount < QUIC_MAX_RECEIVE_FLUSH_COUNT)
                {
                    ReceiveQueueByteCount += Tail.Buffer.Length;
                    Tail = Connection.ReceiveQueue;
                }
                Connection.ReceiveQueueByteCount -= ReceiveQueueByteCount;
                Connection.ReceiveQueue = (QUIC_RX_PACKET)Tail.Next;
                Tail.Next = null;
            }
            else
            {
                FlushedAll = true;
                ReceiveQueueCount = Connection.ReceiveQueueCount;
                ReceiveQueueByteCount = Connection.ReceiveQueueByteCount;
                Connection.ReceiveQueueCount = 0;
                Connection.ReceiveQueueByteCount = 0;
                Connection.ReceiveQueueTail = Connection.ReceiveQueue = null;
            }
            CxPlatDispatchLockRelease(Connection.ReceiveQueueLock);

            QuicConnRecvDatagrams(Connection, ReceiveQueue, ReceiveQueueCount, ReceiveQueueByteCount, false);
            return FlushedAll;
        }
        
        static void QuicConnRecvDatagrams(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packets, int PacketChainCount, int PacketChainByteCount, bool IsDeferred)
        {
            QUIC_RX_PACKET ReleaseChain = null;
            QUIC_RX_PACKET ReleaseChainTail = ReleaseChain;
            int ReleaseChainCount = 0;
            QUIC_RECEIVE_PROCESSING_STATE RecvState = new QUIC_RECEIVE_PROCESSING_STATE()
            {
                ResetIdleTimeout = false,
                UpdatePartitionId = false,
                PartitionIndex = 0
            };
            
            RecvState.PartitionIndex = QuicPartitionIdGetIndex(Connection.PartitionID);
            int BatchCount = 0;
            QUIC_RX_PACKET[] Batch = new QUIC_RX_PACKET[QUIC_MAX_CRYPTO_BATCH_COUNT];
            QUIC_SSBuffer Cipher = new byte[CXPLAT_HP_SAMPLE_LENGTH * QUIC_MAX_CRYPTO_BATCH_COUNT];
            QUIC_PATH CurrentPath = null;

            QUIC_RX_PACKET Packet;
            while ((Packet = Packets) != null)
            {
                NetLog.Assert(Packet.Allocated);
                NetLog.Assert(Packet.QueuedOnConnection != null);
                Packets = (QUIC_RX_PACKET)Packet.Next;
                Packet.Next = null;

                NetLog.Assert(Packet != null);
                NetLog.Assert(Packet.PacketId != 0);
                NetLog.Assert(Packet.ReleaseDeferred == IsDeferred);
                Packet.ReleaseDeferred = false;

                QUIC_PATH DatagramPath = QuicConnGetPathForPacket(Connection, Packet);
                if (DatagramPath == null)
                {
                    QuicPacketLogDrop(Connection, Packet, "Max paths already tracked");
                    goto Drop;
                }

                CxPlatUpdateRoute(DatagramPath.Route, Packet.Route);
                if (DatagramPath != CurrentPath)
                {
                    if (BatchCount != 0)
                    {
                        NetLog.Assert(CurrentPath != null);
                        QuicConnRecvDatagramBatch(
                            Connection,
                            CurrentPath,
                            BatchCount,
                            Batch,
                            Cipher,
                            RecvState);
                        BatchCount = 0;
                    }
                    CurrentPath = DatagramPath;
                }

                if (!IsDeferred)
                {
                    Connection.Stats.Recv.TotalBytes += Packet.Buffer.Length;
                    if (!CurrentPath.IsPeerValidated)
                    {
                        QuicPathIncrementAllowance(
                            Connection,
                            CurrentPath,
                            QUIC_AMPLIFICATION_RATIO * Packet.Buffer.Length);
                    }
                }
                
                do
                {
                    NetLog.Assert(BatchCount < QUIC_MAX_CRYPTO_BATCH_COUNT);
                    NetLog.Assert(Packet.Allocated);
                    Connection.Stats.Recv.TotalPackets++;

                    if (!Packet.ValidatedHeaderInv)
                    {
                        Packet.AvailBufferLength = Packet.Buffer.Length - (Packet.AvailBuffer - Packet.Buffer);
                    }

                    if (!QuicConnRecvHeader(Connection, Packet, Cipher.Slice(BatchCount * CXPLAT_HP_SAMPLE_LENGTH)))
                    {
                        if (Packet.ReleaseDeferred)
                        {
                            Connection.Stats.Recv.TotalPackets--; // Don't count the packet right now.
                        }
                        else if (!Packet.IsShortHeader && Packet.ValidatedHeaderVer)
                        {
                            goto NextPacket;
                        }
                        break;
                    }

                    if (!Packet.IsShortHeader && BatchCount != 0)
                    {
                        QuicConnRecvDatagramBatch(
                            Connection,
                            CurrentPath,
                            BatchCount,
                            Batch,
                            Cipher,
                            RecvState);

                        for (int i = 0; i < CXPLAT_HP_SAMPLE_LENGTH; i++)
                        {
                            Cipher[BatchCount * CXPLAT_HP_SAMPLE_LENGTH + i] = Cipher[i];
                        }
                        BatchCount = 0;
                    }

                    Batch[BatchCount++] = Packet;
                    if (Packet.IsShortHeader && BatchCount < QUIC_MAX_CRYPTO_BATCH_COUNT)
                    {
                        break;
                    }

                    QuicConnRecvDatagramBatch(
                        Connection,
                        CurrentPath,
                        BatchCount,
                        Batch,
                        Cipher,
                        RecvState);

                    BatchCount = 0;
                    if (Packet.IsShortHeader)
                    {
                        break;
                    }

                NextPacket:
                    Packet.AvailBuffer += Packet.AvailBufferLength;
                    Packet.ValidatedHeaderInv = false;
                    Packet.ValidatedHeaderVer = false;
                    Packet.ValidToken = false;
                    Packet.PacketNumberSet = false;
                    Packet.EncryptedWith0Rtt = false;
                    Packet.ReleaseDeferred = false;
                    Packet.CompletelyValid = false;
                    Packet.NewLargestPacketNumber = false;
                    Packet.HasNonProbingFrame = false;
                    Packet.OnAvailBufferChanged();
                } while (Packet.AvailBuffer - Packet.Buffer < Packet.Buffer.Length);


            Drop:
                if (!Packet.ReleaseDeferred)
                {
                    if(ReleaseChain == null)
                    {
                        ReleaseChain = ReleaseChainTail = Packet;
                    }
                    else
                    {
                        ReleaseChainTail.Next = Packet;
                        ReleaseChainTail = Packet;
                    }
                    
                    Packet.QueuedOnConnection = false;
                    if (++ReleaseChainCount == QUIC_MAX_RECEIVE_BATCH_COUNT)
                    {
                        if (BatchCount != 0)
                        {
                            QuicConnRecvDatagramBatch(
                                Connection,
                                CurrentPath,
                                BatchCount,
                                Batch,
                                Cipher,
                                RecvState);
                            BatchCount = 0;
                        }
                        CxPlatRecvDataReturn(ReleaseChain);
                        ReleaseChainTail = ReleaseChain = null;
                        ReleaseChainCount = 0;
                    }
                }
            }

            if (BatchCount != 0)
            {
                QuicConnRecvDatagramBatch(
                    Connection,
                    CurrentPath,
                    BatchCount,
                    Batch,
                    Cipher,
                    RecvState);
                BatchCount = 0; // cppcheck-suppress unreadVariable; NOLINT
            }

            if (Connection.State.DelayedApplicationError && Connection.CloseStatus == 0)
            {
                QuicConnTryClose(
                    Connection,
                    QUIC_CLOSE_REMOTE | QUIC_CLOSE_SEND_NOTIFICATION,
                    QUIC_ERROR_APPLICATION_ERROR,
                    null);
            }

            if (RecvState.ResetIdleTimeout)
            {
                QuicConnResetIdleTimeout(Connection);
            }

            if (ReleaseChain != null)
            {
                CxPlatRecvDataReturn(ReleaseChain);
            }

            if (QuicConnIsServer(Connection) && Connection.Stats.Recv.ValidPackets == 0 && !Connection.State.ClosedLocally)
            {
                QuicConnSilentlyAbort(Connection);
            }

            for (int i = Connection.PathsCount - 1; i > 0; --i)
            {
                if (!Connection.Paths[i].GotValidPacket)
                {
                    QuicPathRemove(Connection, i);
                }
            }

            if (!Connection.State.UpdateWorker && Connection.State.Connected &&
                !Connection.State.ShutdownComplete && RecvState.UpdatePartitionId)
            {
                NetLog.Assert(Connection.Registration != null);
                NetLog.Assert(!Connection.Registration.NoPartitioning);
                NetLog.Assert(RecvState.PartitionIndex != QuicPartitionIdGetIndex(Connection.PartitionID));
                Connection.PartitionID = QuicPartitionIdCreate(RecvState.PartitionIndex);
                QuicConnGenerateNewSourceCids(Connection, true);
                Connection.State.UpdateWorker = true;
            }
        }

        static void QuicConnRecvDatagramBatch(QUIC_CONNECTION Connection, QUIC_PATH Path, int BatchCount, QUIC_RX_PACKET[] Packets, QUIC_SSBuffer Cipher, QUIC_RECEIVE_PROCESSING_STATE RecvState)
        {
            QUIC_SSBuffer HpMask = new byte[CXPLAT_HP_SAMPLE_LENGTH * QUIC_MAX_CRYPTO_BATCH_COUNT];

            NetLog.Assert(BatchCount > 0 && BatchCount <= QUIC_MAX_CRYPTO_BATCH_COUNT);
            QUIC_RX_PACKET Packet = Packets[0];

            if (Connection.Crypto.TlsState.ReadKeys[(int)Packet.KeyType] == null) {
                QuicPacketLogDrop(Connection, Packet, "Key no longer accepted (batch)");
                return;
            }

            if (Packet.Encrypted && Connection.State.HeaderProtectionEnabled)
            {
                var HeaderKey = Connection.Crypto.TlsState.ReadKeys[(int)Packet.KeyType].HeaderKey;
                if (QUIC_FAILED(CxPlatHpComputeMask(HeaderKey, BatchCount, Cipher, HpMask)))
                {
                    QuicPacketLogDrop(Connection, Packet, "Failed to compute HP mask");
                    return;
                }

                //NetLog.Log($"Receive HpMask KeyType: {Packet.KeyType}, BatchCount: {BatchCount}");
                //NetLogHelper.PrintByteArray("Receive HeaderKey", HeaderKey.Key);
                //NetLogHelper.PrintByteArray("Receive HpMask", HpMask.GetSpan());
            }
            
           
            for (int i = 0; i < BatchCount; ++i)
            {
                NetLog.Assert(Packets[i].Allocated);
                CXPLAT_ECN_TYPE ECN = CXPLAT_ECN_FROM_TOS(Packets[i].TypeOfService);
                Packet = Packets[i];
                NetLog.Assert(Packet.PacketId != 0);

                if (!QuicConnRecvPrepareDecrypt(Connection, Packet, HpMask.Slice(i * CXPLAT_HP_SAMPLE_LENGTH)) ||
                    !QuicConnRecvDecryptAndAuthenticate(Connection, Path, Packet))
                {
                    if (Connection.State.CompatibleVerNegotiationAttempted &&
                        !Connection.State.CompatibleVerNegotiationCompleted)
                    {
                        Connection.Stats.QuicVersion = Connection.OriginalQuicVersion;
                        Connection.State.CompatibleVerNegotiationAttempted = false;
                    }
                }
                else if (QuicConnRecvFrames(Connection, Path, Packet, ECN))
                {
                    QuicConnRecvPostProcessing(Connection, Path, Packet);
                    RecvState.ResetIdleTimeout |= Packet.CompletelyValid;

                    if (Connection.Registration != null && !Connection.Registration.NoPartitioning &&
                        Path.IsActive && !Path.PartitionUpdated && Packet.CompletelyValid &&
                        (Packets[i].PartitionIndex % MsQuicLib.PartitionCount) != RecvState.PartitionIndex)
                    {
                        RecvState.PartitionIndex = Packets[i].PartitionIndex % MsQuicLib.PartitionCount;
                        RecvState.UpdatePartitionId = true;
                        Path.PartitionUpdated = true;
                    }

                    if (Packet.IsShortHeader && Packet.NewLargestPacketNumber)
                    {
                        if (QuicConnIsServer(Connection))
                        {
                            Path.SpinBit = BoolOk(Packet.SH.SpinBit);
                        }
                        else
                        {
                            Path.SpinBit = !BoolOk(Packet.SH.SpinBit);
                        }
                    }
                }
            }
        }

        static bool QuicConnRecvPrepareDecrypt(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packet, QUIC_SSBuffer HpMask)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.ValidatedHeaderVer);
            NetLog.Assert(Packet.HeaderLength <= Packet.AvailBufferLength);
            NetLog.Assert(Packet.PayloadLength <= Packet.AvailBufferLength);
            NetLog.Assert(Packet.HeaderLength + Packet.PayloadLength <= Packet.AvailBufferLength);

            int CompressedPacketNumberLength = 0;
            if (Packet.IsShortHeader)
            {
                Packet.AvailBuffer[0] ^= (byte)(HpMask[0] & 0x1f);
                Packet.OnAvailBufferChanged();
                CompressedPacketNumberLength = Packet.SH.PnLength + 1;
            }
            else
            {
                Packet.AvailBuffer[0] ^= (byte)(HpMask[0] & 0x0f);
                Packet.OnAvailBufferChanged();
                CompressedPacketNumberLength = Packet.LH.PnLength + 1;
            }

            NetLog.Assert(CompressedPacketNumberLength >= 1 && CompressedPacketNumberLength <= 4);
            NetLog.Assert(Packet.HeaderLength + CompressedPacketNumberLength <= Packet.AvailBufferLength);

            for (int i = 0; i < CompressedPacketNumberLength; i++)
            {
                Packet.AvailBuffer[Packet.HeaderLength + i] ^= HpMask[1 + i];
            }

            ulong CompressedPacketNumber = 0;
            QuicPktNumDecode(CompressedPacketNumberLength, Packet.AvailBuffer.Slice(Packet.HeaderLength), out CompressedPacketNumber);

            Packet.HeaderLength += CompressedPacketNumberLength;
            Packet.PayloadLength -= CompressedPacketNumberLength;

            QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(Packet.KeyType);
            Packet.PacketNumber = QuicPktNumDecompress(Connection.Packets[(int)EncryptLevel].NextRecvPacketNumber, CompressedPacketNumber, CompressedPacketNumberLength);
            Packet.PacketNumberSet = true;

            //NetLog.Log($"QuicPktNumDecompress: {Connection.Packets[(int)EncryptLevel].NextRecvPacketNumber}, {CompressedPacketNumber}, {CompressedPacketNumberLength}");
            //NetLog.Log($"QuicConnRecvPrepareDecrypt PacketNumber: " + Packet.PacketNumber);
            if (Packet.PacketNumber > QUIC_VAR_INT_MAX)
            {
                QuicPacketLogDrop(Connection, Packet, "Packet number too big");
                return false;
            }

            NetLog.Assert(Packet.IsShortHeader ||
                ((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1) ||
                (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type != (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2)));

            if (Packet.Encrypted && Packet.PayloadLength < CXPLAT_ENCRYPTION_OVERHEAD)
            {
                QuicPacketLogDrop(Connection, Packet, "Payload length less than encryption tag");
                return false;
            }

            QUIC_PACKET_SPACE PacketSpace = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT];
            if (Packet.IsShortHeader && EncryptLevel == QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT && BoolOk(Packet.SH.KeyPhase) != PacketSpace.CurrentKeyPhase)
            {
                if (Packet.PacketNumber < PacketSpace.ReadKeyPhaseStartPacketNumber)
                {
                    NetLog.Assert(Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD] != null);
                    NetLog.Assert(Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD] != null);
                    Packet.KeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD;
                }
                else
                {
                    int Status = QuicCryptoGenerateNewKeys(Connection);
                    if (QUIC_FAILED(Status))
                    {
                        QuicPacketLogDrop(Connection, Packet, "Generate new packet keys");
                        return false;
                    }
                    Packet.KeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_NEW;
                }
            }

            return true;
        }

        static bool QuicConnRecvDecryptAndAuthenticate(QUIC_CONNECTION Connection, QUIC_PATH Path, QUIC_RX_PACKET Packet)
        {
            NetLog.Assert(Packet.AvailBuffer.Length >= Packet.HeaderLength + Packet.PayloadLength);
            QUIC_SSBuffer Payload = Packet.AvailBuffer.Slice(Packet.HeaderLength);

            bool CanCheckForStatelessReset = false;
            QUIC_SSBuffer PacketResetToken = new byte[QUIC_STATELESS_RESET_TOKEN_LENGTH];
            if (QuicConnIsClient(Connection) && Packet.IsShortHeader && Packet.HeaderLength + Packet.PayloadLength >= QUIC_MIN_STATELESS_RESET_PACKET_LENGTH)
            {
                CanCheckForStatelessReset = true;
                Payload.Slice(Packet.PayloadLength - QUIC_STATELESS_RESET_TOKEN_LENGTH, QUIC_STATELESS_RESET_TOKEN_LENGTH).CopyTo(PacketResetToken);
            }

            NetLog.Assert(Packet.PacketId != 0);

            byte[] Iv = new byte[CXPLAT_MAX_IV_LENGTH];
            QuicCryptoCombineIvAndPacketNumber(Connection.Crypto.TlsState.ReadKeys[(int)Packet.KeyType].Iv, Packet.PacketNumber, Iv);

            if (Packet.Encrypted)
            {
                if (QUIC_FAILED(CxPlatDecrypt(Connection.Crypto.TlsState.ReadKeys[(int)Packet.KeyType].PacketKey, Iv,
                        Packet.AvailBuffer.Slice(0, Packet.HeaderLength),
                        Payload.Slice(0, Packet.PayloadLength))))
                {
                    if (CanCheckForStatelessReset)
                    {
                        for (CXPLAT_LIST_ENTRY Entry = Connection.DestCids.Next; Entry != Connection.DestCids; Entry = Entry.Next)
                        {
                            QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);

                            if (DestCid.HasResetToken && !DestCid.Retired &&
                                orBufferEqual(DestCid.ResetToken, PacketResetToken.Buffer, QUIC_STATELESS_RESET_TOKEN_LENGTH))
                            {
                                QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS, QUIC_STATUS_ABORTED, null);
                                return false;
                            }
                        }
                    }

                    Connection.Stats.Recv.DecryptionFailures++;
                    QuicPacketLogDrop(Connection, Packet, "Decryption failure");
                    QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_DECRYPTION_FAIL);
                    if (Connection.Stats.Recv.DecryptionFailures >= CXPLAT_AEAD_INTEGRITY_LIMIT)
                    {
                        QuicConnTransportError(Connection, QUIC_ERROR_AEAD_LIMIT_REACHED);
                    }

                    return false;
                }
            }

            Connection.Stats.Recv.ValidPackets++;
            if (Packet.IsShortHeader)
            {
                if (Packet.SH.Reserved != 0)
                {
                    QuicPacketLogDrop(Connection, Packet, "Invalid SH Reserved bits values");
                    QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                    return false;
                }
            }
            else
            {
                if (Packet.LH.Reserved != 0)
                {
                    QuicPacketLogDrop(Connection, Packet, "Invalid LH Reserved bits values");
                    QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                    return false;
                }
            }

            if (Packet.Encrypted)
            {
                Packet.PayloadLength -= CXPLAT_ENCRYPTION_OVERHEAD;
            }

            QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(Packet.KeyType);
            if (QuicAckTrackerAddPacketNumber(Connection.Packets[(int)EncryptLevel].AckTracker, Packet.PacketNumber))
            {
                QuicPacketLogDrop(Connection, Packet, "Duplicate packet number");
                Connection.Stats.Recv.DuplicatePackets++;
                return false;
            }

            if (!Packet.IsShortHeader)
            {
                bool IsVersion2 = (Connection.Stats.QuicVersion == QUIC_VERSION_2);
                if ((!IsVersion2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1) ||
                    (IsVersion2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2))
                {
                    if (!Connection.State.Connected && QuicConnIsClient(Connection) && !QuicConnUpdateDestCid(Connection, Packet))
                    {
                        return false;
                    }
                }
                else if ((!IsVersion2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_0_RTT_PROTECTED_V1) ||
                    (IsVersion2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_0_RTT_PROTECTED_V2))
                {
                    NetLog.Assert(QuicConnIsServer(Connection));
                    Packet.EncryptedWith0Rtt = true;
                }
            }

            if (Packet.IsShortHeader)
            {
                QUIC_PACKET_SPACE PacketSpace = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT];
                if (Packet.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_NEW)
                {
                    QuicCryptoUpdateKeyPhase(Connection, false);
                    PacketSpace.ReadKeyPhaseStartPacketNumber = Packet.PacketNumber;
                }
                else if (Packet.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT &&
                    BoolOk(Packet.SH.KeyPhase) == PacketSpace.CurrentKeyPhase &&
                    Packet.PacketNumber < PacketSpace.ReadKeyPhaseStartPacketNumber)
                {
                    PacketSpace.ReadKeyPhaseStartPacketNumber = Packet.PacketNumber;
                }
            }

            if (Packet.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE && QuicConnIsServer(Connection))
            {
                QuicCryptoDiscardKeys(Connection.Crypto, QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL);
                QuicPathSetValid(Connection, Path, QUIC_PATH_VALID_REASON.QUIC_PATH_VALID_HANDSHAKE_PACKET);
            }

            return true;
        }

        static bool QuicConnUpdateDestCid(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packet)
        {
            NetLog.Assert(QuicConnIsClient(Connection));
            NetLog.Assert(!Connection.State.Connected);

            if (CxPlatListIsEmpty(Connection.DestCids))
            {
                QuicConnTransportError(Connection, QUIC_ERROR_INTERNAL_ERROR);
                return false;
            }

            QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.DestCids.Next);
            NetLog.Assert(Connection.Paths[0].DestCid == DestCid);

            if (!orBufferEqual(Packet.SourceCid.Data, DestCid.Data))
            {
                if (Packet.SourceCid.Data.Length <= DestCid.Data.Length)
                {
                    DestCid.IsInitial = false;
                    DestCid.Data.Length = Packet.SourceCid.Data.Length;
                    Packet.SourceCid.Data.CopyTo(DestCid.Data);
                }
                else
                {
                    CxPlatListEntryRemove(DestCid.Link);
                    DestCid = QuicCidNewDestination(Packet.SourceCid.Data);
                    if (DestCid == null)
                    {
                        Connection.DestCidCount--;
                        Connection.Paths[0].DestCid = null;
                        QuicConnFatalError(Connection, QUIC_STATUS_OUT_OF_MEMORY, "Out of memory");
                        return false;
                    }

                    Connection.Paths[0].DestCid = DestCid;
                    DestCid.UsedLocally = true;
                    CxPlatListInsertHead(Connection.DestCids, DestCid.Link);
                }
            }
            return true;
        }

        static void QuicConnIndicateShutdownBegin(QUIC_CONNECTION Connection)
        {
            QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
            if (Connection.State.AppClosed)
            {
                Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_PEER;
                Event.SHUTDOWN_INITIATED_BY_PEER.ErrorCode = Connection.CloseErrorCode;
            }
            else
            {
                Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_TRANSPORT;
                Event.SHUTDOWN_INITIATED_BY_TRANSPORT.Status = Connection.CloseStatus;
                Event.SHUTDOWN_INITIATED_BY_TRANSPORT.ErrorCode = Connection.CloseErrorCode;
            }
            QuicConnIndicateEvent(Connection, ref Event);
        }

        static void QuicConnCleanupServerResumptionState(QUIC_CONNECTION Connection)
        {
            NetLog.Assert(QuicConnIsServer(Connection));
            if (!Connection.State.ResumptionEnabled)
            {
                if (Connection.HandshakeTP != null)
                {
                    QuicCryptoTlsCleanupTransportParameters(Connection.HandshakeTP);
                    QuicLibraryGetPerProc().TransportParamPool.CxPlatPoolFree(Connection.HandshakeTP);
                    Connection.HandshakeTP = null;
                }

                QUIC_CRYPTO Crypto = Connection.Crypto;
                if (Crypto.TLS != null)
                {
                    Crypto.TLS = null;
                }

                if (Crypto.Initialized)
                {
                    QuicRecvBufferUninitialize(Crypto.RecvBuffer);
                    QuicRangeUninitialize(Crypto.SparseAckRanges);
                    Crypto.TlsState.Buffer = null;
                    Crypto.Initialized = false;
                }
            }
        }

        static void QuicConnFlushDeferred(QUIC_CONNECTION Connection)
        {
            NetLog.Log("QuicConnFlushDeferred");
            for (int i = 1; i <= (int)Connection.Crypto.TlsState.ReadKey; ++i)
            {
                if (Connection.Crypto.TlsState.ReadKeys[i] == null)
                {
                    continue;
                }

                QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel((QUIC_PACKET_KEY_TYPE)i);
                QUIC_PACKET_SPACE Packets = Connection.Packets[(int)EncryptLevel];

                if (Packets.DeferredPackets != null)
                {
                    QUIC_RX_PACKET DeferredPackets = Packets.DeferredPackets;
                    int DeferredPacketsCount = Packets.DeferredPacketsCount;

                    Packets.DeferredPacketsCount = 0;
                    Packets.DeferredPackets = null;
                    QuicConnRecvDatagrams(Connection, DeferredPackets, DeferredPacketsCount, 0, true);
                }
            }
        }

        static void QuicConnDiscardDeferred0Rtt(QUIC_CONNECTION Connection)
        {
            QUIC_RX_PACKET ReleaseChain = null;
            QUIC_RX_PACKET ReleaseChainTail = ReleaseChain;
            QUIC_PACKET_SPACE Packets = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT];
            NetLog.Assert(Packets != null);

            QUIC_RX_PACKET DeferredPackets = Packets.DeferredPackets;
            QUIC_RX_PACKET DeferredPacketsTail = Packets.DeferredPackets;
            while (DeferredPackets != null)
            {
                QUIC_RX_PACKET Packet = DeferredPackets;
                DeferredPackets = (QUIC_RX_PACKET)DeferredPackets.Next;

                if (Packet.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT)
                {
                    QuicPacketLogDrop(Connection, Packet, "0-RTT rejected");
                    Packets.DeferredPacketsCount--;

                    if(ReleaseChainTail == null)
                    {
                        ReleaseChain = ReleaseChainTail = Packet;
                    }
                    else
                    {
                        ReleaseChainTail.Next = Packet;
                        ReleaseChainTail = Packet;
                    }
                }
                else
                {
                    if(DeferredPacketsTail == null)
                    {
                        Packets.DeferredPackets = DeferredPacketsTail = Packet;
                    }
                    else
                    {
                        DeferredPacketsTail.Next = Packet;
                        DeferredPacketsTail = Packet;
                    }
                }
            }

            if (ReleaseChain != null)
            {
                CxPlatRecvDataReturn((CXPLAT_RECV_DATA)ReleaseChain);
            }
        }

        static bool QuicConnValidateTransportParameterCIDs(QUIC_CONNECTION Connection)
        {
            if (!BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_INITIAL_SOURCE_CONNECTION_ID))
            {
                return false;
            }

            QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.DestCids.Next);
            if (!orBufferEqual(DestCid.Data, Connection.PeerTransportParams.InitialSourceConnectionID))
            {
                return false;
            }

            if (QuicConnIsClient(Connection))
            {
                if (!BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_ORIGINAL_DESTINATION_CONNECTION_ID))
                {
                    return false;
                }

                NetLog.Assert(Connection.OrigDestCID != null);
                if (!orBufferEqual(Connection.OrigDestCID.Data, Connection.PeerTransportParams.OriginalDestinationConnectionID))
                {
                    return false;
                }

                if (Connection.State.HandshakeUsedRetryPacket)
                {
                    if (!BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_RETRY_SOURCE_CONNECTION_ID))
                    {
                        return false;
                    }
                }
                else
                {
                    if (BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_RETRY_SOURCE_CONNECTION_ID))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static bool QuicConnPostAcceptValidatePeerTransportParameters(QUIC_CONNECTION Connection)
        {
            if (Connection.CibirId[0] != 0)
            {
                if (!BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_CIBIR_ENCODING))
                {
                    return false;
                }
                if (Connection.PeerTransportParams.CibirLength != Connection.CibirId[0])
                {
                    return false;
                }
                if (Connection.PeerTransportParams.CibirOffset != Connection.CibirId[1])
                {
                    return false;
                }
            }
            else
            {
                if (BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_CIBIR_ENCODING))
                {
                    return false;
                }
            }

            return true;
        }

        static bool QuicConnRecvHeader(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packet, QUIC_SSBuffer Cipher)
        {
            if (!Packet.ValidatedHeaderInv)
            {
                NetLog.Assert(Packet.DestCid != null);
                if (!QuicPacketValidateInvariant(Connection, Packet, Connection.State.ShareBinding))
                {
                    NetLog.LogError("Error");
                    return false;
                }
            }

            if (!Packet.IsShortHeader)
            {
                if (Packet.Invariant.LONG_HDR.Version != Connection.Stats.QuicVersion)
                {
                    if (QuicConnIsClient(Connection) &&
                        !Connection.State.CompatibleVerNegotiationAttempted &&
                        QuicVersionNegotiationExtIsVersionCompatible(Connection, Packet.Invariant.LONG_HDR.Version))
                    {
                        Connection.OriginalQuicVersion = Connection.Stats.QuicVersion;
                        Connection.State.CompatibleVerNegotiationAttempted = true;
                        Connection.Stats.QuicVersion = Packet.Invariant.LONG_HDR.Version;
                        QuicConnOnQuicVersionSet(Connection);
                        if (QUIC_FAILED(QuicCryptoOnVersionChange(Connection.Crypto)))
                        {
                            NetLog.LogError("Error");
                            return false;
                        }
                    }
                    else if (QuicConnIsClient(Connection) && Packet.Invariant.LONG_HDR.Version == QUIC_VERSION_VER_NEG && !BoolOk(Connection.Stats.VersionNegotiation))
                    {
                        NetLog.LogError("Error");
                        Connection.Stats.VersionNegotiation = 1;
                        QuicConnRecvVerNeg(Connection, Packet);
                        return false;
                    }
                    else
                    {
                        NetLog.LogError("Error");
                        QuicPacketLogDropWithValue(Connection, Packet, "Invalid version", Packet.Invariant.LONG_HDR.Version);
                        return false;
                    }
                }
            }
            else
            {
                if (!QuicIsVersionSupported(Connection.Stats.QuicVersion))
                {
                    QuicPacketLogDrop(Connection, Packet, "SH packet during version negotiation");
                    return false;
                }
            }

            NetLog.Assert(QuicIsVersionSupported(Connection.Stats.QuicVersion));
            if (!Packet.IsShortHeader)
            {
                if ((Packet.LH.Version != QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_RETRY_V1) ||
                    (Packet.LH.Version == QUIC_VERSION_2 && Packet.LH.Type == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_RETRY_V2))
                {
                    NetLog.LogError("Error");
                    QuicConnRecvRetry(Connection, Packet);
                    return false;
                }

                QUIC_SSBuffer TokenBuffer = QUIC_SSBuffer.Empty;
                if (!Packet.ValidatedHeaderVer &&
                    !QuicPacketValidateLongHeaderV1(
                        Connection,
                        QuicConnIsServer(Connection),
                        Packet,
                        ref TokenBuffer,
                        Connection.Settings.GreaseQuicBitEnabled))
                {
                    NetLog.LogError("Error");
                    return false;
                }

                QUIC_PATH Path = Connection.Paths[0];
                if (!Path.IsPeerValidated && (Packet.ValidToken || TokenBuffer.Length != 0))
                {

                    bool InvalidRetryToken = false;
                    if (Packet.ValidToken)
                    {
                        NetLog.Assert(TokenBuffer.IsEmpty);
                        QuicPacketDecodeRetryTokenV1(Packet, ref TokenBuffer);
                    }
                    else
                    {
                        NetLog.Assert(!TokenBuffer.IsEmpty);
                        if (!QuicPacketValidateInitialToken(
                                Connection,
                                Packet,
                                TokenBuffer,
                                ref InvalidRetryToken) && InvalidRetryToken)
                        {
                            return false;
                        }
                    }

                    if (!InvalidRetryToken)
                    {
                        NetLog.Assert(!TokenBuffer.IsEmpty);
                        NetLog.Assert(TokenBuffer.Length == sizeof_QUIC_TOKEN_CONTENTS);

                        QUIC_TOKEN_CONTENTS Token = null;
                        if (!QuicRetryTokenDecrypt(Packet, TokenBuffer, ref Token))
                        {
                            NetLog.Assert(false);
                            QuicPacketLogDrop(Connection, Packet, "Retry token decrypt failure");
                            return false;
                        }

                        NetLog.Assert(Token.Encrypted.OrigConnId.Length <= Token.Encrypted.OrigConnId.Length);
                        NetLog.Assert(QuicAddrCompare(Path.Route.RemoteAddress, Token.Encrypted.RemoteAddress));

                        if (Connection.OrigDestCID != null)
                        {
                            Connection.OrigDestCID = null;
                        }

                        Connection.OrigDestCID = new QUIC_CID(Token.Encrypted.OrigConnId.Length);
                        if (Connection.OrigDestCID == null)
                        {
                            return false;
                        }

                        Connection.OrigDestCID.Data.Length = Token.Encrypted.OrigConnId.Length;
                        Token.Encrypted.OrigConnId.CopyTo(Connection.OrigDestCID.Data);
                        Connection.State.HandshakeUsedRetryPacket = true;
                        QuicPathSetValid(Connection, Path, QUIC_PATH_VALID_REASON.QUIC_PATH_VALID_INITIAL_TOKEN);
                    }
                }

                if (Connection.OrigDestCID == null)
                {
                    Connection.OrigDestCID = new QUIC_CID(Packet.DestCid.Data.Length);
                    Packet.DestCid.Data.CopyTo(Connection.OrigDestCID.Data);
                }

                if (Packet.LH.Version == QUIC_VERSION_2)
                {
                    Packet.KeyType = QuicPacketTypeToKeyTypeV2((QUIC_LONG_HEADER_TYPE_V2)Packet.LH.Type);
                }
                else
                {
                    Packet.KeyType = QuicPacketTypeToKeyTypeV1((QUIC_LONG_HEADER_TYPE_V1)Packet.LH.Type);
                }
                Packet.Encrypted = true;
            }
            else
            {
                if (!Packet.ValidatedHeaderVer && !QuicPacketValidateShortHeaderV1(Connection, Packet, Connection.Settings.GreaseQuicBitEnabled))
                {
                    return false;
                }
                Packet.KeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT;
                Packet.Encrypted = !Connection.State.Disable1RttEncrytion && !Connection.Paths[0].EncryptionOffloading;
            }

            if (Packet.Encrypted && Connection.State.HeaderProtectionEnabled && Packet.PayloadLength < 4 + CXPLAT_HP_SAMPLE_LENGTH)
            {
                QuicPacketLogDrop(Connection, Packet, "Too short for HP");
                return false;
            }

            if (!QuicConnGetKeyOrDeferDatagram(Connection, Packet))
            {
                NetLog.Log("QuicConnGetKeyOrDeferDatagram");
                return false;
            }

            Packet.AvailBuffer.Slice(Packet.HeaderLength + 4, CXPLAT_HP_SAMPLE_LENGTH).CopyTo(Cipher);
            return true;
        }

        static void QuicConnRecvVerNeg(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packet)
        {
            uint SupportedVersion = 0;
            QUIC_SSBuffer ServerVersionList = Packet.VerNeg.DestCid + Packet.VerNeg.DestCid.Length + sizeof(byte) + Packet.VerNeg.DestCid[Packet.VerNeg.DestCid.Length];
            int ServerVersionListLength = (Packet.AvailBufferLength - (ServerVersionList - (QUIC_SSBuffer)Packet.AvailBuffer)) / sizeof(uint);

            for (int i = 0; i < ServerVersionListLength; i++)
            {
                uint ServerVersion = ServerVersionList[i];
                if (ServerVersion == Connection.Stats.QuicVersion && !QuicIsVersionReserved(ServerVersion))
                {
                    QuicPacketLogDrop(Connection, Packet, "Version Negotation that includes the current version");
                    return;
                }

                if (SupportedVersion == 0 &&
                    ((QuicConnIsClient(Connection) && QuicVersionNegotiationExtIsVersionClientSupported(Connection, ServerVersion)) ||
                    (QuicConnIsServer(Connection) && QuicVersionNegotiationExtIsVersionServerSupported(ServerVersion))))
                {
                    SupportedVersion = ServerVersion;
                }
            }

            if (SupportedVersion == 0)
            {
                QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS, QUIC_STATUS_VER_NEG_ERROR, null);
                return;
            }

            Connection.PreviousQuicVersion = Connection.Stats.QuicVersion;
            Connection.Stats.QuicVersion = SupportedVersion;
            QuicConnOnQuicVersionSet(Connection);
            int Status = QuicCryptoOnVersionChange(Connection.Crypto);
            if (QUIC_FAILED(Status))
            {
                QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS, Status, null);
                return;
            }
            QuicConnRestart(Connection, true);
        }

        static void QuicConnRestart(QUIC_CONNECTION Connection, bool CompleteReset)
        {
            NetLog.Assert(Connection.State.Started);
            if (CompleteReset)
            {
                QUIC_PATH Path = Connection.Paths[0];
                Path.GotFirstRttSample = false;
                Path.SmoothedRtt = Connection.Settings.InitialRttMs;
                Path.RttVariance = Path.SmoothedRtt / 2;
            }

            for (int i = 0; i < Connection.Packets.Length; ++i)
            {
                NetLog.Assert(Connection.Packets[i] != null);
                QuicPacketSpaceReset(Connection.Packets[i]);
            }

            QuicCongestionControlReset(Connection.CongestionControl, true);
            QuicSendReset(Connection.Send);
            QuicLossDetectionReset(Connection.LossDetection);
            QuicCryptoTlsCleanupTransportParameters(Connection.PeerTransportParams);

            if (CompleteReset)
            {
                NetLog.Assert(Connection.Configuration != null);

                QUIC_TRANSPORT_PARAMETERS LocalTP = new QUIC_TRANSPORT_PARAMETERS();
                int Status = QuicConnGenerateLocalTransportParameters(Connection, LocalTP);
                NetLog.Assert(QUIC_SUCCEEDED(Status));

                Status = QuicCryptoInitializeTls(Connection.Crypto, Connection.Configuration.SecurityConfig, LocalTP);
                if (QUIC_FAILED(Status))
                {
                    QuicConnFatalError(Connection, Status, null);
                }

                QuicCryptoTlsCleanupTransportParameters(LocalTP);

            }
            else
            {
                QuicCryptoReset(Connection.Crypto);
            }
        }

        static void QuicPacketSpaceReset(QUIC_PACKET_SPACE Packets)
        {
            QuicAckTrackerReset(Packets.AckTracker);
        }

        static int QuicConnGenerateLocalTransportParameters(QUIC_CONNECTION Connection, QUIC_TRANSPORT_PARAMETERS LocalTP)
        {
            NetLog.Assert(Connection.Configuration != null);
            NetLog.Assert(Connection.SourceCids.Next != null);

            QUIC_CID SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.SourceCids.Next);
            LocalTP.InitialMaxData = (int)Connection.Send.MaxData;
            LocalTP.InitialMaxStreamDataBidiLocal = Connection.Settings.StreamRecvWindowBidiLocalDefault;
            LocalTP.InitialMaxStreamDataBidiRemote = Connection.Settings.StreamRecvWindowBidiRemoteDefault;
            LocalTP.InitialMaxStreamDataUni = Connection.Settings.StreamRecvWindowUnidiDefault;
            LocalTP.MaxUdpPayloadSize = MaxUdpPayloadSizeFromMTU(CxPlatSocketGetLocalMtu(Connection.Paths[0].Binding.Socket));
            LocalTP.MaxAckDelay = QuicConnGetAckDelay(Connection);
            LocalTP.MinAckDelay = MsQuicLib.ExecutionConfig != null && MsQuicLib.ExecutionConfig.PollingIdleTimeoutUs != 0 ? 0 : MsQuicLib.TimerResolutionMs;
            LocalTP.ActiveConnectionIdLimit = QUIC_ACTIVE_CONNECTION_ID_LIMIT;

            LocalTP.Flags =
                QUIC_TP_FLAG_INITIAL_MAX_DATA |
                QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_LOCAL |
                QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_REMOTE |
                QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_UNI |
                QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE |
                QUIC_TP_FLAG_MAX_ACK_DELAY |
                QUIC_TP_FLAG_MIN_ACK_DELAY |
                QUIC_TP_FLAG_ACTIVE_CONNECTION_ID_LIMIT;

            if (Connection.Settings.IdleTimeoutMs != 0)
            {
                LocalTP.Flags |= QUIC_TP_FLAG_IDLE_TIMEOUT;
                LocalTP.IdleTimeout = Connection.Settings.IdleTimeoutMs;
            }

            if (Connection.AckDelayExponent != QUIC_TP_ACK_DELAY_EXPONENT_DEFAULT)
            {
                LocalTP.Flags |= QUIC_TP_FLAG_ACK_DELAY_EXPONENT;
                LocalTP.AckDelayExponent = Connection.AckDelayExponent;
            }

            LocalTP.Flags |= QUIC_TP_FLAG_INITIAL_SOURCE_CONNECTION_ID;
            SourceCid.Data.CopyTo(LocalTP.InitialSourceConnectionID);
            LocalTP.InitialSourceConnectionID.Length = SourceCid.Data.Length;

            if (Connection.Settings.DatagramReceiveEnabled)
            {
                LocalTP.Flags |= QUIC_TP_FLAG_MAX_DATAGRAM_FRAME_SIZE;
                LocalTP.MaxDatagramFrameSize = QUIC_DEFAULT_MAX_DATAGRAM_LENGTH;
            }

            if (Connection.State.Disable1RttEncrytion)
            {
                LocalTP.Flags |= QUIC_TP_FLAG_DISABLE_1RTT_ENCRYPTION;
            }

            if (Connection.CibirId[0] != 0)
            {
                LocalTP.Flags |= QUIC_TP_FLAG_CIBIR_ENCODING;
                LocalTP.CibirLength = Connection.CibirId[0];
                LocalTP.CibirOffset = Connection.CibirId[1];
            }

            if (Connection.Settings.VersionNegotiationExtEnabled)
            {
                LocalTP.VersionInfo = QuicVersionNegotiationExtEncodeVersionInfo(Connection);
                if (LocalTP.VersionInfo != null)
                {
                    LocalTP.Flags |= QUIC_TP_FLAG_VERSION_NEGOTIATION;
                }
            }

            if (Connection.Settings.GreaseQuicBitEnabled)
            {
                LocalTP.Flags |= QUIC_TP_FLAG_GREASE_QUIC_BIT;
            }

            if (Connection.Settings.ReliableResetEnabled)
            {
                LocalTP.Flags |= QUIC_TP_FLAG_RELIABLE_RESET_ENABLED;
            }

            if (Connection.Settings.OneWayDelayEnabled)
            {
                LocalTP.Flags |= QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED | QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED;
            }

            if (QuicConnIsServer(Connection))
            {
                if (Connection.Streams.Types[(int)(STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_BI_DIR)].MaxTotalStreamCount > 0)
                {
                    LocalTP.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRMS_BIDI;
                    LocalTP.InitialMaxBidiStreams = Connection.Streams.Types[STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_BI_DIR].MaxTotalStreamCount;
                }

                if (Connection.Streams.Types[(int)(STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_UNI_DIR)].MaxTotalStreamCount > 0)
                {
                    LocalTP.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRMS_UNI;
                    LocalTP.InitialMaxUniStreams = Connection.Streams.Types[STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_UNI_DIR].MaxTotalStreamCount;
                }

                if (!Connection.Settings.MigrationEnabled)
                {
                    LocalTP.Flags |= QUIC_TP_FLAG_DISABLE_ACTIVE_MIGRATION;
                }

                LocalTP.Flags |= QUIC_TP_FLAG_STATELESS_RESET_TOKEN;
                int Status = QuicLibraryGenerateStatelessResetToken(SourceCid.Data, LocalTP.StatelessResetToken);
                if (QUIC_FAILED(Status))
                {
                    return Status;
                }

                if (Connection.OrigDestCID != null)
                {
                    NetLog.Assert(Connection.OrigDestCID.Data.Length <= QUIC_MAX_CONNECTION_ID_LENGTH_V1);
                    LocalTP.Flags |= QUIC_TP_FLAG_ORIGINAL_DESTINATION_CONNECTION_ID;
                    LocalTP.OriginalDestinationConnectionID.Length = Connection.OrigDestCID.Data.Length;
                    Connection.OrigDestCID.Data.CopyTo(LocalTP.OriginalDestinationConnectionID);

                    if (Connection.State.HandshakeUsedRetryPacket)
                    {
                        NetLog.Assert(SourceCid.Link.Next != null);
                        QUIC_CID PrevSourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(SourceCid.Link.Next);
                        LocalTP.Flags |= QUIC_TP_FLAG_RETRY_SOURCE_CONNECTION_ID;
                        LocalTP.RetrySourceConnectionID.Length = PrevSourceCid.Data.Length;
                        PrevSourceCid.Data.CopyTo(LocalTP.RetrySourceConnectionID);
                    }
                }
            }
            else
            {
                if (Connection.Streams.Types[(int)(STREAM_ID_FLAG_IS_SERVER | STREAM_ID_FLAG_IS_BI_DIR)].MaxTotalStreamCount > 0)
                {
                    LocalTP.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRMS_BIDI;
                    LocalTP.InitialMaxBidiStreams = Connection.Streams.Types[STREAM_ID_FLAG_IS_SERVER | STREAM_ID_FLAG_IS_BI_DIR].MaxTotalStreamCount;
                }
                if (Connection.Streams.Types[(int)(STREAM_ID_FLAG_IS_SERVER | STREAM_ID_FLAG_IS_UNI_DIR)].MaxTotalStreamCount > 0)
                {
                    LocalTP.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRMS_UNI;
                    LocalTP.InitialMaxUniStreams = Connection.Streams.Types[STREAM_ID_FLAG_IS_SERVER | STREAM_ID_FLAG_IS_UNI_DIR].MaxTotalStreamCount;
                }
            }
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicConnRecvRetry(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packet)
        {
            if (QuicConnIsServer(Connection))
            {
                QuicPacketLogDrop(Connection, Packet, "Retry sent to server");
                return;
            }

            if (Connection.State.GotFirstServerResponse)
            {
                QuicPacketLogDrop(Connection, Packet, "Already received server response");
                return;
            }

            if (Connection.State.ClosedLocally || Connection.State.ClosedRemotely)
            {
                QuicPacketLogDrop(Connection, Packet, "Retry while shutting down");
                return;
            }

            if (Packet.AvailBufferLength - Packet.HeaderLength <= QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1)
            {
                QuicPacketLogDrop(Connection, Packet, "No room for Retry Token");
                return;
            }

            if (!QuicVersionNegotiationExtIsVersionClientSupported(Connection, Packet.LH.Version))
            {
                QuicPacketLogDrop(Connection, Packet, "Retry Version not supported by client");
            }

            QUIC_VERSION_INFO VersionInfo = null;
            for (int i = 0; i < QuicSupportedVersionList.Length; ++i)
            {
                if (QuicSupportedVersionList[i].Number == Packet.LH.Version)
                {
                    VersionInfo = QuicSupportedVersionList[i];
                    break;
                }
            }

            NetLog.Assert(VersionInfo != null);
            QUIC_SSBuffer Token = Packet.AvailBuffer.Slice(Packet.HeaderLength);
            int TokenLength = Packet.AvailBufferLength - (Packet.HeaderLength + QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1);
            NetLog.Assert(!CxPlatListIsEmpty(Connection.DestCids));
            QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.DestCids.Next);
            QUIC_SSBuffer CalculatedIntegrityValue = new byte[QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1];

            if (QUIC_FAILED(QuicPacketGenerateRetryIntegrity(
                    VersionInfo,
                    DestCid.Data.Slice(0, DestCid.Data.Length),
                    Packet.AvailBuffer.Slice(0, Packet.AvailBufferLength - QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1),
                    CalculatedIntegrityValue)))
            {
                QuicPacketLogDrop(Connection, Packet, "Failed to generate integrity field");
                return;
            }

            if (!orBufferEqual(CalculatedIntegrityValue,
                    Packet.AvailBuffer.Slice(Packet.AvailBufferLength - QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1,
                    QUIC_RETRY_INTEGRITY_TAG_LENGTH_V1)))
            {
                QuicPacketLogDrop(Connection, Packet, "Invalid integrity field");
                return;
            }

            Connection.Send.InitialToken = new byte[TokenLength];
            if (Connection.Send.InitialToken == null)
            {
                QuicPacketLogDrop(Connection, Packet, "InitialToken alloc failed");
                return;
            }

            Connection.Send.InitialToken.Length = TokenLength;
            Token.CopyTo(Connection.Send.InitialToken);

            if (!QuicConnUpdateDestCid(Connection, Packet))
            {
                return;
            }

            Connection.State.GotFirstServerResponse = true;
            Connection.State.HandshakeUsedRetryPacket = true;
            QuicPacketKeyFree(Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL]);
            QuicPacketKeyFree(Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL]);
            Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] = null;
            Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL] = null;

            NetLog.Assert(!CxPlatListIsEmpty(Connection.DestCids));
            DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.DestCids.Next);

            int Status;
            if (QUIC_FAILED(Status = QuicPacketKeyCreateInitial(
                    QuicConnIsServer(Connection),
                    VersionInfo.HkdfLabels,
                    VersionInfo.Salt,
                    DestCid.Data,
                    ref Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL],
                   ref Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL])))
            {
                QuicConnFatalError(Connection, Status, "Failed to create initial keys");
                return;
            }

            Connection.Stats.StatelessRetry = 1;
            QuicConnRestart(Connection, false);
            Packet.CompletelyValid = true;
        }

        static bool QuicConnGetKeyOrDeferDatagram(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packet)
        {
            if (Packet.KeyType > Connection.Crypto.TlsState.ReadKey)
            {
                if (Packet.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT &&
                    Connection.Crypto.TlsState.EarlyDataState != CXPLAT_TLS_EARLY_DATA_STATE.CXPLAT_TLS_EARLY_DATA_UNKNOWN)
                {
                    NetLog.Assert(Connection.Crypto.TlsState.EarlyDataState != CXPLAT_TLS_EARLY_DATA_STATE.CXPLAT_TLS_EARLY_DATA_ACCEPTED);
                    QuicPacketLogDrop(Connection, Packet, "0-RTT not currently accepted");
                }
                else
                {
                    QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(Packet.KeyType);
                    QUIC_PACKET_SPACE Packets = Connection.Packets[(int)EncryptLevel];
                    if (Packets.DeferredPacketsCount == QUIC_MAX_PENDING_DATAGRAMS)
                    {
                        QuicPacketLogDrop(Connection, Packet, "Max deferred packet count reached");
                    }
                    else
                    {
                        Packets.DeferredPacketsCount++;
                        Packet.ReleaseDeferred = true;

                        if (Packets.DeferredPackets == null)
                        {
                            Packets.DeferredPackets = Packet;
                            Packets.DeferredPackets.Next = null;
                        }
                        else
                        {
                            QUIC_RX_PACKET Tail = Packets.DeferredPackets;
                            while (Tail != null && Tail.Next != null)
                            {
                                Tail = (QUIC_RX_PACKET)(Tail.Next);
                            }
                            Tail.Next = Packet;   
                            Packet.Next = null;
                        }
                    }
                }

                return false;
            }

            if (QuicConnIsServer(Connection) && !Connection.State.HandshakeConfirmed && Packet.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                NetLog.Assert(false);
                return false;
            }

            NetLog.Assert(Packet.KeyType >= 0 && Packet.KeyType < QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT);
            if (Connection.Crypto.TlsState.ReadKeys[(int)Packet.KeyType] == null)
            {
                QuicPacketLogDrop(Connection, Packet, "Key no longer accepted: " + Packet.KeyType);
                return false;
            }

            return true;
        }

        static void QuicConnResetIdleTimeout(QUIC_CONNECTION Connection)
        {
            long IdleTimeoutMs;
            QUIC_PATH Path = Connection.Paths[0];
            if (Connection.State.Connected)
            {
                IdleTimeoutMs = Connection.PeerTransportParams.IdleTimeout;
                if (IdleTimeoutMs == 0 ||
                    (Connection.Settings.IdleTimeoutMs != 0 && Connection.Settings.IdleTimeoutMs < IdleTimeoutMs))
                {
                    IdleTimeoutMs = Connection.Settings.IdleTimeoutMs;
                }
            }
            else
            {
                IdleTimeoutMs = Connection.Settings.HandshakeIdleTimeoutMs;
            }

            if (IdleTimeoutMs != 0)
            {
                if (Connection.State.Connected)
                {
                    long MinIdleTimeoutMs = QuicLossDetectionComputeProbeTimeout(Connection.LossDetection, Path, QUIC_CLOSE_PTO_COUNT);
                    if (IdleTimeoutMs < MinIdleTimeoutMs)
                    {
                        IdleTimeoutMs = MinIdleTimeoutMs;
                    }
                }

                QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_IDLE, IdleTimeoutMs);
            }
            else
            {
                QuicConnTimerCancel(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_IDLE);
            }

            if (Connection.Settings.KeepAliveIntervalMs != 0)
            {
                QuicConnTimerSet(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_KEEP_ALIVE, Connection.Settings.KeepAliveIntervalMs);
            }
        }

        static bool QuicConnOnRetirePriorToUpdated(QUIC_CONNECTION Connection)
        {
            bool ReplaceRetiredCids = false;

            for (CXPLAT_LIST_ENTRY Entry = Connection.DestCids.Next; Entry != Connection.DestCids; Entry = Entry.Next)
            {
                QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                if (DestCid.SequenceNumber >= Connection.RetirePriorTo || DestCid.Retired)
                {
                    continue;
                }

                if (DestCid.UsedLocally)
                {
                    ReplaceRetiredCids = true;
                }
                QuicConnRetireCid(Connection, DestCid);
            }

            return ReplaceRetiredCids;
        }

        static QUIC_CID QuicConnGetDestCidFromSeq(QUIC_CONNECTION Connection, ulong SequenceNumber, bool RemoveFromList)
        {
            for (CXPLAT_LIST_ENTRY Entry = Connection.DestCids.Next; Entry != Connection.DestCids; Entry = Entry.Next)
            {
                QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                if (DestCid.SequenceNumber == SequenceNumber)
                {
                    if (RemoveFromList)
                    {
                        CxPlatListEntryRemove(Entry);
                    }
                    return DestCid;
                }
            }
            return null;
        }

        static bool QuicConnReplaceRetiredCids(QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.PathsCount <= QUIC_MAX_PATH_COUNT);
            for (int i = 0; i < Connection.PathsCount; ++i)
            {
                QUIC_PATH Path = Connection.Paths[i];
                if (Path.DestCid == null || !Path.DestCid.Retired)
                {
                    continue;
                }

                QUIC_CID NewDestCid = QuicConnGetUnusedDestCid(Connection);
                if (NewDestCid == null)
                {
                    if (Path.IsActive)
                    {
                        QuicConnSilentlyAbort(Connection); // Must silently abort because we can't send anything now.
                        return false;
                    }
                    NetLog.Assert(i != 0);
                    QuicPathRemove(Connection, i--);
                    continue;
                }

                NetLog.Assert(NewDestCid != Path.DestCid);
                Path.DestCid = NewDestCid;
                Path.DestCid.UsedLocally = true;
                Path.InitiatedCidUpdate = true;
            }

            return true;
        }

        static QUIC_CID QuicConnGetSourceCidFromSeq(QUIC_CONNECTION Connection, ulong SequenceNumber, bool RemoveFromList, ref bool IsLastCid)
        {
            for (CXPLAT_LIST_ENTRY Entry = Connection.SourceCids.Next; Entry != Connection.SourceCids; Entry = Entry.Next)
            {
                QUIC_CID SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                if (SourceCid.SequenceNumber == SequenceNumber)
                {
                    if (RemoveFromList)
                    {
                        QuicBindingRemoveSourceConnectionID(Connection.Paths[0].Binding, SourceCid);
                    }
                    IsLastCid = Connection.SourceCids.Next == Connection.SourceCids;
                    return SourceCid;
                }
            }
            return null;
        }


        static bool QuicConnRecvFrames(QUIC_CONNECTION Connection, QUIC_PATH Path, QUIC_RX_PACKET Packet, CXPLAT_ECN_TYPE ECN)
        {
            bool AckEliciting = false;
            bool AckImmediately = false;
            bool UpdatedFlowControl = false;
            QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(Packet.KeyType);
            bool Closed = Connection.State.ClosedLocally || Connection.State.ClosedRemotely;
            QUIC_SSBuffer Payload = Packet.AvailBuffer.Slice(Packet.HeaderLength, Packet.PayloadLength);
            long RecvTime = CxPlatTime();

            if (QuicConnIsClient(Connection) && !Connection.State.GotFirstServerResponse)
            {
                Connection.State.GotFirstServerResponse = true;
            }

            while (Payload.Length > 0)
            {
                ulong nFrameType = 0;
                if (!QuicVarIntDecode(ref Payload, ref nFrameType))
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                    return false;
                }

                QUIC_FRAME_TYPE FrameType = (QUIC_FRAME_TYPE)nFrameType;
                if (!QUIC_FRAME_IS_KNOWN(FrameType))
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR, "QUIC_FRAME_IS_KNOWN");
                    return false;
                }

                if (EncryptLevel != QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT)
                {
                    switch (FrameType)
                    {
                        case QUIC_FRAME_TYPE.QUIC_FRAME_PADDING:
                        case QUIC_FRAME_TYPE.QUIC_FRAME_PING:
                        case QUIC_FRAME_TYPE.QUIC_FRAME_ACK:
                        case QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1:
                        case QUIC_FRAME_TYPE.QUIC_FRAME_CRYPTO:
                        case QUIC_FRAME_TYPE.QUIC_FRAME_CONNECTION_CLOSE:
                            break;
                        default:
                            QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                            return false;
                    }
                }
                else if (Packet.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT)
                {
                    switch (FrameType)
                    {
                        case QUIC_FRAME_TYPE.QUIC_FRAME_ACK:
                        case QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1:
                        case QUIC_FRAME_TYPE.QUIC_FRAME_HANDSHAKE_DONE:
                            QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                            return false;
                        default:
                            break;
                    }
                }

                //NetLog.Log("FrameType: " + FrameType);
                switch (FrameType)
                {
                    case QUIC_FRAME_TYPE.QUIC_FRAME_PADDING:
                        {
                            while (Payload.Length > 0 && Payload[0] == (byte)QUIC_FRAME_TYPE.QUIC_FRAME_PADDING)
                            {
                                Payload += sizeof(byte);
                            }
                            break;
                        }
                    case QUIC_FRAME_TYPE.QUIC_FRAME_PING:
                        {
                            AckEliciting = true;
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_ACK:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1:
                        {
                            bool InvalidAckFrame = false;
                            if (!QuicLossDetectionProcessAckFrame(
                                    Connection.LossDetection,
                                    Path,
                                    Packet,
                                    EncryptLevel,
                                    FrameType,
                                    ref Payload,
                                    ref InvalidAckFrame))
                            {
                                if (InvalidAckFrame)
                                {
                                    QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                }
                                return false;
                            }

                            Connection.Stats.Recv.ValidAckFrames++;
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_CRYPTO:
                        {
                            QUIC_CRYPTO_EX Frame = new QUIC_CRYPTO_EX();
                            if (!QuicCryptoFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            int Status = QuicCryptoProcessFrame(Connection.Crypto, Packet.KeyType, Frame);
                            if (QUIC_SUCCEEDED(Status))
                            {
                                AckEliciting = true;
                            }
                            else if (Status == QUIC_STATUS_OUT_OF_MEMORY)
                            {
                                QuicPacketLogDrop(Connection, Packet, "Crypto frame process OOM");
                                return false;
                            }
                            else
                            {
                                if (Status == QUIC_STATUS_VER_NEG_ERROR)
                                {
                                    if (QuicBindingQueueStatelessOperation(Connection.Paths[0].Binding, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_VERSION_NEGOTIATION, Packet))
                                    {
                                        Packet.ReleaseDeferred = true;
                                    }
                                    QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL_SILENT, QUIC_ERROR_VERSION_NEGOTIATION_ERROR, null);
                                }
                                else if (Status != QUIC_STATUS_INVALID_STATE)
                                {
                                    QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                }
                                return false;
                            }

                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_NEW_TOKEN:
                        {
                            QUIC_NEW_TOKEN_EX Frame = new QUIC_NEW_TOKEN_EX();
                            if (!QuicNewTokenFrameDecode(ref Payload, Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            AckEliciting = true;
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_RESET_STREAM:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STOP_SENDING:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_1:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_2:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_3:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_4:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_5:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_6:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_7:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAM_DATA:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_DATA_BLOCKED:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM:
                        {
                            if (Closed)
                            {
                                if (!QuicStreamFrameSkip(FrameType, ref Payload))
                                {
                                    QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                    return false;
                                }
                                break;
                            }

                            ulong StreamId = 0;
                            if (!QuicStreamFramePeekID(Payload, ref StreamId))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            AckEliciting = true;
                            bool PeerOriginatedStream = QuicConnIsServer(Connection) ? STREAM_ID_IS_CLIENT(StreamId) : STREAM_ID_IS_SERVER(StreamId);

                            if (STREAM_ID_IS_UNI_DIR(StreamId))
                            {
                                bool IsReceiverSideFrame = FrameType == QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAM_DATA || FrameType == QUIC_FRAME_TYPE.QUIC_FRAME_STOP_SENDING;
                                if (PeerOriginatedStream == IsReceiverSideFrame)
                                {
                                    QuicConnTransportError(Connection, QUIC_ERROR_STREAM_STATE_ERROR);
                                    break;
                                }
                            }

                            bool FatalError = false;
                            QUIC_STREAM Stream = QuicStreamSetGetStreamForPeer(
                                    Connection.Streams,
                                    StreamId,
                                    Packet.EncryptedWith0Rtt,
                                    PeerOriginatedStream,
                                    ref FatalError);

                            if (Stream != null)
                            {
                                int Status = QuicStreamRecv(Stream, Packet, FrameType, ref Payload, ref UpdatedFlowControl);
                                QuicStreamRelease(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_LOOKUP);
                                if (Status == QUIC_STATUS_OUT_OF_MEMORY)
                                {
                                    QuicPacketLogDrop(Connection, Packet, "Stream frame process OOM");
                                    return false;
                                }

                                if (QUIC_FAILED(Status))
                                {
                                    QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                    return false;
                                }
                            }
                            else if (FatalError)
                            {
                                return false;
                            }
                            else
                            {
                                if (!QuicStreamFrameSkip(FrameType, ref Payload))
                                {
                                    QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                    return false;
                                }
                            }

                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_MAX_DATA:
                        {
                            QUIC_MAX_DATA_EX Frame = new QUIC_MAX_DATA_EX();
                            if (!QuicMaxDataFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            if (Connection.Send.PeerMaxData < Frame.MaximumData)
                            {
                                Connection.Send.PeerMaxData = Frame.MaximumData;
                                UpdatedFlowControl = true;
                                QuicConnRemoveOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_CONN_FLOW_CONTROL);
                                QuicSendQueueFlush(Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_CONNECTION_FLOW_CONTROL);
                            }

                            AckEliciting = true;
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS_1:
                        {
                            QUIC_MAX_STREAMS_EX Frame = new QUIC_MAX_STREAMS_EX();
                            if (!QuicMaxStreamsFrameDecode(FrameType, ref Payload, Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            if (Frame.MaximumStreams > QUIC_TP_MAX_STREAMS_MAX)
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_STREAM_LIMIT_ERROR);
                                break;
                            }

                            QuicStreamSetUpdateMaxStreams(Connection.Streams, Frame.BidirectionalStreams, Frame.MaximumStreams);
                            AckEliciting = true;
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_DATA_BLOCKED:
                        {
                            QUIC_DATA_BLOCKED_EX Frame = new QUIC_DATA_BLOCKED_EX();
                            if (!QuicDataBlockedFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_MAX_DATA);
                            AckEliciting = true;
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAMS_BLOCKED:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_STREAMS_BLOCKED_1:
                        {
                            QUIC_STREAMS_BLOCKED_EX Frame = new QUIC_STREAMS_BLOCKED_EX();
                            if (!QuicStreamsBlockedFrameDecode(FrameType, ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break; // Ignore frame if we are closed.
                            }

                            AckEliciting = true;

                            uint Type = (QuicConnIsServer(Connection) ? STREAM_ID_FLAG_IS_CLIENT : STREAM_ID_FLAG_IS_SERVER) |
                                (Frame.BidirectionalStreams ? STREAM_ID_FLAG_IS_BI_DIR : STREAM_ID_FLAG_IS_UNI_DIR);

                            ref QUIC_STREAM_TYPE_INFO Info = ref Connection.Streams.Types[Type];

                            if (Info.MaxTotalStreamCount > Frame.StreamLimit)
                            {
                                break;
                            }

                            QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                            Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_PEER_NEEDS_STREAMS;
                            Event.PEER_NEEDS_STREAMS.Bidirectional = Frame.BidirectionalStreams;
                            QuicConnIndicateEvent(Connection, ref Event);
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_NEW_CONNECTION_ID:
                        {
                            QUIC_NEW_CONNECTION_ID_EX Frame = new QUIC_NEW_CONNECTION_ID_EX();
                            if (!QuicNewConnectionIDFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            bool ReplaceRetiredCids = false;
                            if (Connection.RetirePriorTo < Frame.RetirePriorTo)
                            {
                                Connection.RetirePriorTo = Frame.RetirePriorTo;
                                ReplaceRetiredCids = QuicConnOnRetirePriorToUpdated(Connection);
                            }

                            if (QuicConnGetDestCidFromSeq(Connection, Frame.Sequence, false) == null)
                            {
                                QUIC_CID DestCid = QuicCidNewDestination(Frame.Buffer);
                                if (DestCid == null)
                                {
                                    if (ReplaceRetiredCids)
                                    {
                                        QuicConnSilentlyAbort(Connection);
                                    }
                                    else
                                    {
                                        QuicConnFatalError(Connection, QUIC_STATUS_OUT_OF_MEMORY, null);
                                    }
                                    return false;
                                }

                                DestCid.HasResetToken = true;
                                DestCid.SequenceNumber = Frame.Sequence;

                                Frame.Buffer.GetSpan().CopyTo(DestCid.ResetToken.AsSpan().Slice(0, QUIC_STATELESS_RESET_TOKEN_LENGTH));
                                CxPlatListInsertTail(Connection.DestCids, DestCid.Link);
                                Connection.DestCidCount++;

                                if (DestCid.SequenceNumber < Connection.RetirePriorTo)
                                {
                                    QuicConnRetireCid(Connection, DestCid);
                                }

                                if (Connection.DestCidCount > QUIC_ACTIVE_CONNECTION_ID_LIMIT)
                                {
                                    if (ReplaceRetiredCids)
                                    {
                                        QuicConnSilentlyAbort(Connection);
                                    }
                                    else
                                    {
                                        QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                                    }
                                    return false;
                                }
                            }

                            if (ReplaceRetiredCids && !QuicConnReplaceRetiredCids(Connection))
                            {
                                return false;
                            }

                            AckEliciting = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_RETIRE_CONNECTION_ID:
                        {
                            QUIC_RETIRE_CONNECTION_ID_EX Frame = new QUIC_RETIRE_CONNECTION_ID_EX();
                            if (!QuicRetireConnectionIDFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            bool IsLastCid = false;
                            QUIC_CID SourceCid = QuicConnGetSourceCidFromSeq(
                                    Connection,
                                    Frame.Sequence,
                                    true,
                                    ref IsLastCid);
                            if (SourceCid != null)
                            {
                                bool CidAlreadyRetired = SourceCid.Retired;
                                SourceCid = null;

                                if (IsLastCid)
                                {
                                    QuicConnCloseLocally(
                                        Connection,
                                        QUIC_CLOSE_INTERNAL_SILENT,
                                        QUIC_ERROR_PROTOCOL_VIOLATION,
                                        null);
                                }
                                else if (!CidAlreadyRetired)
                                {
                                    if (QuicConnGenerateNewSourceCid(Connection, false) == null)
                                    {
                                        break;
                                    }
                                }
                            }

                            AckEliciting = true;
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_PATH_CHALLENGE:
                        {
                            QUIC_PATH_CHALLENGE_EX Frame = new QUIC_PATH_CHALLENGE_EX();
                            if (!QuicPathChallengeFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            Path.SendResponse = true;
                            Array.Copy(Frame.Data, Path.Response, Frame.Data.Length);
                            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_PATH_RESPONSE);
                            AckEliciting = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_PATH_RESPONSE:
                        {
                            QUIC_PATH_CHALLENGE_EX Frame = new QUIC_PATH_CHALLENGE_EX();
                            if (!QuicPathChallengeFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Closed)
                            {
                                break;
                            }

                            NetLog.Assert(Connection.PathsCount <= QUIC_MAX_PATH_COUNT);
                            for (int i = 0; i < Connection.PathsCount; ++i)
                            {
                                QUIC_PATH TempPath = Connection.Paths[i];
                                if (!TempPath.IsPeerValidated && !orBufferEqual(Frame.Data, TempPath.Challenge, Frame.Data.Length))
                                {
                                    QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PATH_VALIDATED);
                                    QuicPathSetValid(Connection, TempPath, QUIC_PATH_VALID_REASON.QUIC_PATH_VALID_PATH_RESPONSE);
                                    break;
                                }
                            }

                            AckEliciting = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_CONNECTION_CLOSE:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_CONNECTION_CLOSE_1:
                        {
                            QUIC_CONNECTION_CLOSE_EX Frame = new QUIC_CONNECTION_CLOSE_EX();
                            if (!QuicConnCloseFrameDecode(FrameType, ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            uint Flags = QUIC_CLOSE_REMOTE | QUIC_CLOSE_SEND_NOTIFICATION;
                            if (Frame.ApplicationClosed)
                            {
                                Flags |= QUIC_CLOSE_APPLICATION;
                            }

                            if (!Frame.ApplicationClosed && Frame.ErrorCode == QUIC_ERROR_APPLICATION_ERROR)
                            {
                                Connection.State.DelayedApplicationError = true;
                            }
                            else
                            {
                                QuicConnTryClose(
                                    Connection,
                                    Flags,
                                    Frame.ErrorCode,
                                    Frame.ReasonPhrase);
                            }

                            AckEliciting = true;
                            Packet.HasNonProbingFrame = true;
                            if (Connection.State.HandleClosed)
                            {
                                goto Done;
                            }
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_HANDSHAKE_DONE:
                        {
                            if (QuicConnIsServer(Connection))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                                return false;
                            }

                            if (!Connection.State.HandshakeConfirmed)
                            {
                                QuicCryptoHandshakeConfirmed(Connection.Crypto, true);
                            }

                            AckEliciting = true;
                            Packet.HasNonProbingFrame = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM:
                    case QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM_1:
                        {
                            if (!Connection.Settings.DatagramReceiveEnabled)
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                                return false;
                            }
                            if (!QuicDatagramProcessFrame(Connection.Datagram, Packet, FrameType, ref Payload))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }
                            AckEliciting = true;
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_ACK_FREQUENCY:
                        {
                            QUIC_ACK_FREQUENCY_EX Frame = new QUIC_ACK_FREQUENCY_EX();
                            if (!QuicAckFrequencyFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            if (Frame.UpdateMaxAckDelay < MsQuicLib.TimerResolutionMs)
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                                return false;
                            }

                            AckEliciting = true;
                            if (Frame.SequenceNumber < Connection.NextRecvAckFreqSeqNum)
                            {
                                break;
                            }

                            Connection.NextRecvAckFreqSeqNum = Frame.SequenceNumber + 1;
                            Connection.State.IgnoreReordering = Frame.IgnoreOrder;
                            if (Frame.UpdateMaxAckDelay == 0)
                            {
                                Connection.Settings.MaxAckDelayMs = 0;
                            }
                            else if (Frame.UpdateMaxAckDelay < 1000)
                            {
                                Connection.Settings.MaxAckDelayMs = 1;
                            }
                            else
                            {
                                NetLog.Assert(Frame.UpdateMaxAckDelay <= long.MaxValue);
                                Connection.Settings.MaxAckDelayMs = Frame.UpdateMaxAckDelay;
                            }
                            if (Frame.PacketTolerance < byte.MaxValue)
                            {
                                Connection.PacketTolerance = (byte)Frame.PacketTolerance;
                            }
                            else
                            {
                                Connection.PacketTolerance = byte.MaxValue; // Cap to 0xFF for space savings.
                            }
                            break;
                        }

                    case QUIC_FRAME_TYPE.QUIC_FRAME_IMMEDIATE_ACK: // Always accept the frame, because we always enable support.
                        AckImmediately = true;
                        break;

                    case QUIC_FRAME_TYPE.QUIC_FRAME_TIMESTAMP:
                        {
                            if (!Connection.State.TimestampRecvNegotiated)
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_PROTOCOL_VIOLATION);
                                return false;
                            }

                            QUIC_TIMESTAMP_EX Frame = new QUIC_TIMESTAMP_EX();
                            if (!QuicTimestampFrameDecode(ref Payload, ref Frame))
                            {
                                QuicConnTransportError(Connection, QUIC_ERROR_FRAME_ENCODING_ERROR);
                                return false;
                            }

                            Packet.HasNonProbingFrame = true;
                            Packet.SendTimestamp = Frame.Timestamp;
                            break;
                        }

                    default:
                        break;
                }
            }

        Done:

            if (UpdatedFlowControl)
            {
                QuicConnLogOutFlowStats(Connection);
            }

            if (Connection.State.ShutdownComplete || Connection.State.HandleClosed)
            {

            }
            else if (Connection.Packets[(int)EncryptLevel] != null)
            {

                if (Connection.Packets[(int)EncryptLevel].NextRecvPacketNumber <= Packet.PacketNumber)
                {
                    Connection.Packets[(int)EncryptLevel].NextRecvPacketNumber = Packet.PacketNumber + 1;
                    Packet.NewLargestPacketNumber = true;
                }

                QUIC_ACK_TYPE AckType;
                if (AckImmediately)
                {
                    AckType = QUIC_ACK_TYPE.QUIC_ACK_TYPE_ACK_IMMEDIATE;
                }
                else if (AckEliciting)
                {
                    AckType = QUIC_ACK_TYPE.QUIC_ACK_TYPE_ACK_ELICITING;
                }
                else
                {
                    AckType = QUIC_ACK_TYPE.QUIC_ACK_TYPE_NON_ACK_ELICITING;
                }

                QuicAckTrackerAckPacket(Connection.Packets[(int)EncryptLevel].AckTracker,
                    Packet.PacketNumber,
                    RecvTime,
                    ECN,
                    AckType);
            }

            Packet.CompletelyValid = true;
            return true;
        }

        static QUIC_CID QuicConnGetSourceCidFromBuf(QUIC_CONNECTION Connection, QUIC_SSBuffer CidBuffer)
        {
            for (CXPLAT_LIST_ENTRY Entry = Connection.SourceCids.Next; Entry != Connection.SourceCids; Entry = Entry.Next)
            {
                QUIC_CID SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                if (orBufferEqual(CidBuffer, SourceCid.Data))
                {
                    return SourceCid;
                }
            }
            return null;
        }

        static void QuicConnRecvPostProcessing(QUIC_CONNECTION Connection, QUIC_PATH Path, QUIC_RX_PACKET Packet)
        {
            bool PeerUpdatedCid = false;
            if (Packet.DestCid.Data.Length != 0)
            {
                QUIC_CID SourceCid = QuicConnGetSourceCidFromBuf(Connection, Packet.DestCid.Data);
                if (SourceCid != null && !SourceCid.UsedByPeer)
                {
                    SourceCid.UsedByPeer = true;
                    if (!SourceCid.IsInitial)
                    {
                        PeerUpdatedCid = true; //这里是更新 Server Peer 的源CID
                    }
                }
            }

            if (!Path.GotValidPacket)
            {
                Path.GotValidPacket = true;

                if (!Path.IsActive)
                {
                    if (Path.DestCid == null || (PeerUpdatedCid && Path.DestCid.Data.Length != 0))
                    {
                        QUIC_CID NewDestCid = QuicConnGetUnusedDestCid(Connection);
                        if (NewDestCid == null)
                        {
                            Path.GotValidPacket = false; // Don't have a new CID to use!!!
                            Path.DestCid = null;
                            return;
                        }
                        NetLog.Assert(NewDestCid != Path.DestCid);
                        Path.DestCid = NewDestCid;
                        Path.DestCid.UsedLocally = true;
                    }

                    NetLog.Assert((Path).DestCid != null);
                    Path.SendChallenge = true;
                    Path.PathValidationStartTime = CxPlatTime();

                    CxPlatRandom.Random(Path.Challenge);
                    NetLog.Assert(Connection.Paths[0].IsActive);
                    if (Connection.Paths[0].IsPeerValidated)
                    {
                        Connection.Paths[0].IsPeerValidated = false;
                        Connection.Paths[0].SendChallenge = true;
                        Connection.Paths[0].PathValidationStartTime = CxPlatTime();
                        CxPlatRandom.Random(Connection.Paths[0].Challenge);
                    }

                    QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_PATH_CHALLENGE);
                }
            }
            else if (PeerUpdatedCid)
            {
                if (!Path.InitiatedCidUpdate)
                {
                    QuicConnRetireCurrentDestCid(Connection, Path);
                }
                else
                {
                    Path.InitiatedCidUpdate = false;
                }
            }

            if (Packet.HasNonProbingFrame && Packet.NewLargestPacketNumber && !Path.IsActive)
            {
                QuicPathSetActive(Connection, Path);
                Path = Connection.Paths[0];

                QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_PEER_ADDRESS_CHANGED;
                Event.PEER_ADDRESS_CHANGED = new QUIC_CONNECTION_EVENT.PEER_ADDRESS_CHANGED_DATA();
                Event.PEER_ADDRESS_CHANGED.Address = Path.Route.RemoteAddress;
                QuicConnIndicateEvent(Connection, ref Event);
            }
        }

        static void QuicConnGenerateNewSourceCids(QUIC_CONNECTION Connection, bool ReplaceExistingCids)
        {
            if (!Connection.State.ShareBinding)
            {
                return;
            }

            int NewCidCount;
            if (ReplaceExistingCids)
            {
                NewCidCount = Connection.SourceCidLimit;
                CXPLAT_LIST_ENTRY Entry = Connection.SourceCids.Next;
                while (Entry != Connection.SourceCids)
                {
                    QUIC_CID SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                    SourceCid.Retired = true;
                    Entry = Entry.Next;
                }
            }
            else
            {
                int CurrentCidCount = QuicConnSourceCidsCount(Connection);
                NetLog.Assert(CurrentCidCount <= Connection.SourceCidLimit);
                if (CurrentCidCount < Connection.SourceCidLimit)
                {
                    NewCidCount = Connection.SourceCidLimit - CurrentCidCount;
                }
                else
                {
                    NewCidCount = 0;
                }
            }

            for (int i = 0; i < NewCidCount; ++i)
            {
                if (QuicConnGenerateNewSourceCid(Connection, false) == null)
                {
                    break;
                }
            }
        }

        static int QuicConnSourceCidsCount(QUIC_CONNECTION Connection)
        {
            int Count = 0;
            CXPLAT_LIST_ENTRY Entry = Connection.SourceCids.Next;
            while (Entry != Connection.SourceCids)
            {
                ++Count;
                Entry = Entry.Next;
            }
            return Count;
        }

        static void QuicConnUpdateRtt(QUIC_CONNECTION Connection, QUIC_PATH Path, long LatestRtt, long OurSendTimestamp, long PeerSendTimestamp)
        {
            if (LatestRtt == 0)
            {
                LatestRtt = 1;
            }

            bool NewMinRtt = false;
            Path.LatestRttSample = LatestRtt;
            if (LatestRtt < Path.MinRtt)
            {
                Path.MinRtt = LatestRtt;
                NewMinRtt = true;
            }

            if (LatestRtt > Path.MaxRtt)
            {
                Path.MaxRtt = LatestRtt;
            }

            if (!Path.GotFirstRttSample)
            {
                Path.GotFirstRttSample = true;
                Path.SmoothedRtt = LatestRtt;
                Path.RttVariance = LatestRtt / 2;

            }
            else
            {
                if (Path.SmoothedRtt > LatestRtt)
                {
                    Path.RttVariance = (3 * Path.RttVariance + Path.SmoothedRtt - LatestRtt) / 4;
                }
                else
                {
                    Path.RttVariance = (3 * Path.RttVariance + LatestRtt - Path.SmoothedRtt) / 4;
                }
                Path.SmoothedRtt = (7 * Path.SmoothedRtt + LatestRtt) / 8;
            }

            if (OurSendTimestamp != long.MaxValue)
            {
                if (Connection.Stats.Timing.PhaseShift == 0 || NewMinRtt)
                {
                    Connection.Stats.Timing.PhaseShift = PeerSendTimestamp - OurSendTimestamp - LatestRtt / 2;
                    Path.OneWayDelayLatest = Path.OneWayDelay = LatestRtt / 2;
                }
                else
                {
                    Path.OneWayDelayLatest = PeerSendTimestamp - OurSendTimestamp - Connection.Stats.Timing.PhaseShift;
                    Path.OneWayDelay = (7 * Path.OneWayDelay + Path.OneWayDelayLatest) / 8;
                }
            }

            NetLog.Assert(Path.SmoothedRtt != 0);
        }

        static void QuicConnFree(QUIC_CONNECTION Connection)
        {
            NetLog.Assert(!Connection.State.Freed);
            NetLog.Assert(Connection.RefCount == 0);
            if (Connection.State.ExternalOwner)
            {
                NetLog.Assert(Connection.State.HandleClosed);
            }

            NetLog.Assert(Connection.SourceCids.Next == null);
            NetLog.Assert(CxPlatListIsEmpty(Connection.Streams.ClosedStreams));
            QuicRangeUninitialize(Connection.DecodedAckRanges);
            QuicCryptoUninitialize(Connection.Crypto);
            QuicLossDetectionUninitialize(Connection.LossDetection);
            QuicSendUninitialize(Connection.Send);
            for (int i = 0; i < Connection.Packets.Length; i++)
            {
                if (Connection.Packets[i] != null)
                {
                    QuicPacketSpaceUninitialize(Connection.Packets[i]);
                    Connection.Packets[i] = null;
                }
            }

            while (!CxPlatListIsEmpty(Connection.DestCids))
            {
                QUIC_CID CID = CXPLAT_CONTAINING_RECORD<QUIC_CID>(CxPlatListRemoveHead(Connection.DestCids));
            }
            QuicConnUnregister(Connection);
            if (Connection.Worker != null)
            {
                QuicTimerWheelRemoveConnection(Connection.Worker.TimerWheel, Connection);
                QuicOperationQueueClear(Connection.OperQ, Connection.Partition);
            }

            if (Connection.ReceiveQueue != null)
            {
                QUIC_RX_PACKET Packet = Connection.ReceiveQueue;
                do
                {
                    Packet.QueuedOnConnection = false;
                } while ((Packet = (QUIC_RX_PACKET)Packet.Next) != null);
                CxPlatRecvDataReturn((CXPLAT_RECV_DATA)Connection.ReceiveQueue);
                Connection.ReceiveQueue = null;
            }
            QUIC_PATH Path = Connection.Paths[0];
            if (Path.Binding != null)
            {
                QuicLibraryReleaseBinding(Path.Binding);
                Path.Binding = null;
            }

            QuicDatagramSendShutdown(Connection.Datagram);

            if (Connection.Configuration != null)
            {
                Connection.Configuration = null;
            }
            if (Connection.RemoteServerName != null)
            {
                Connection.RemoteServerName = null;
            }
            if (Connection.OrigDestCID != null)
            {
                Connection.OrigDestCID = null;
            }
            if (Connection.HandshakeTP != null)
            {
                QuicCryptoTlsCleanupTransportParameters(Connection.HandshakeTP);
                QuicLibraryGetPerProc().TransportParamPool.CxPlatPoolFree(Connection.HandshakeTP);
                Connection.HandshakeTP = null;
            }
            QuicCryptoTlsCleanupTransportParameters(Connection.PeerTransportParams);
            QuicSettingsCleanup(Connection.Settings);
            if (Connection.State.Started && !Connection.State.Connected)
            {
                QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_HANDSHAKE_FAIL);
            }
            if (Connection.State.Connected)
            {
                QuicPerfCounterDecrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_CONNECTED);
            }
            if (Connection.Registration != null)
            {
                CxPlatRundownRelease(Connection.Registration.Rundown);
            }
            if (Connection.CloseReasonPhrase != null)
            {
                Connection.CloseReasonPhrase = null;
            }
            Connection.State.Freed = true;

            QuicLibraryGetPerProc().ConnectionPool.CxPlatPoolFree(Connection);
            QuicPerfCounterDecrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_ACTIVE);
        }

        static void QuicConnProcessUdpUnreachable(QUIC_CONNECTION Connection, QUIC_ADDR RemoteAddress)
        {
            if (Connection.Crypto.TlsState.ReadKey > QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL)
            {


            }
            else if (QuicAddrCompare(Connection.Paths[0].Route.RemoteAddress, RemoteAddress))
            {
                QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS, QUIC_STATUS_UNREACHABLE, null);
            }
        }

        static void QuicConnTraceRundownOper(QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.Registration != null);
        }

        static void QuicConnProcessRouteCompletion(QUIC_CONNECTION Connection, byte[] PhysicalAddress, byte PathId, bool Succeeded)
        {
            int PathIndex = 0;
            QUIC_PATH Path = QuicConnGetPathByID(Connection, PathId, ref PathIndex);
            if (Path != null)
            {
                if (Succeeded)
                {
                    CxPlatResolveRouteComplete(Connection, Path.Route, PhysicalAddress, PathId);
                    if (!QuicSendFlush(Connection.Send))
                    {
                        QuicSendQueueFlush(Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_ROUTE_COMPLETION);
                    }
                }
                else
                {
                    if (Path.IsActive && Connection.PathsCount > 1)
                    {
                        QuicPathSetActive(Connection, Connection.Paths[1]);
                        QuicPathRemove(Connection, 1);
                        if (!QuicSendFlush(Connection.Send))
                        {
                            QuicSendQueueFlush(Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_ROUTE_COMPLETION);
                        }
                    }
                    else
                    {
                        QuicPathRemove(Connection, PathIndex);
                    }
                }
            }

            if (Connection.PathsCount == 0)
            {
                QuicConnCloseLocally(
                    Connection,
                    QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS,
                    QUIC_STATUS_UNREACHABLE,
                    null);
            }
        }

        static int QuicConnSetConfiguration(QUIC_CONNECTION Connection, QUIC_CONFIGURATION Configuration)
        {
            if (Connection.Configuration != null || QuicConnIsClosed(Connection))
            {
                return QUIC_STATUS_INVALID_STATE;
            }

            int Status;
            QUIC_TRANSPORT_PARAMETERS LocalTP = new QUIC_TRANSPORT_PARAMETERS();

            NetLog.Assert(Connection.Configuration == null);
            NetLog.Assert(Configuration != null);
            NetLog.Assert(Configuration.SecurityConfig != null);

            QuicConfigurationAddRef(Configuration);
            Connection.Configuration = Configuration;
            QuicConnApplyNewSettings(Connection, false, Configuration.Settings);

            if (QuicConnIsClient(Connection))
            {
                if (Connection.Stats.QuicVersion == 0)
                {
                    Connection.Stats.QuicVersion = QUIC_VERSION_LATEST;
                    QuicConnOnQuicVersionSet(Connection);
                    Status = QuicCryptoOnVersionChange(Connection.Crypto);
                    if (QUIC_FAILED(Status))
                    {
                        goto Error;
                    }
                }

                NetLog.Assert(!CxPlatListIsEmpty(Connection.DestCids));
                QUIC_CID DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.DestCids.Next);
                Connection.OrigDestCID = new QUIC_CID(DestCid.Data.Length);
                Connection.OrigDestCID.Data.Length = DestCid.Data.Length;
                DestCid.Data.CopyTo(Connection.OrigDestCID.Data);
            }
            else
            {
                if (!QuicConnPostAcceptValidatePeerTransportParameters(Connection))
                {
                    QuicConnTransportError(Connection, QUIC_ERROR_CONNECTION_REFUSED);
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Cleanup;
                }

                Status = QuicCryptoReNegotiateAlpn(Connection, Connection.Configuration.AlpnList);
                if (QUIC_FAILED(Status))
                {
                    goto Cleanup;
                }
                Connection.Crypto.TlsState.ClientAlpnList = null;
            }

            Status = QuicConnGenerateLocalTransportParameters(Connection, LocalTP);
            if (QUIC_FAILED(Status))
            {
                goto Cleanup;
            }

            if (QuicConnIsServer(Connection) && Connection.HandshakeTP != null)
            {
                NetLog.Assert(Connection.State.ResumptionEnabled);
                QuicCryptoTlsCopyTransportParameters(LocalTP, Connection.HandshakeTP);
            }

            Connection.State.Started = true;
            Connection.Stats.Timing.Start = CxPlatTime();
            Status = QuicCryptoInitializeTls(Connection.Crypto, Configuration.SecurityConfig, LocalTP);

        Cleanup:
            QuicCryptoTlsCleanupTransportParameters(LocalTP);
        Error:
            return Status;
        }

        static bool QuicConnApplyNewSettings(QUIC_CONNECTION Connection, bool OverWrite, QUIC_SETTINGS NewSettings)
        {
            if (!QuicSettingApply(Connection.Settings, OverWrite, !Connection.State.Started, NewSettings))
            {
                return false;
            }

            if (!Connection.State.Started)
            {

                Connection.Paths[0].SmoothedRtt = Connection.Settings.InitialRttMs;
                Connection.Paths[0].RttVariance = Connection.Paths[0].SmoothedRtt / 2;
                Connection.Paths[0].Mtu = Connection.Settings.MinimumMtu;

                if (Connection.Settings.ServerResumptionLevel > QUIC_SERVER_RESUMPTION_LEVEL.QUIC_SERVER_NO_RESUME && Connection.HandshakeTP == null)
                {
                    NetLog.Assert(!Connection.State.Started);
                    Connection.HandshakeTP = QuicLibraryGetPerProc().TransportParamPool.CxPlatPoolAlloc();
                    if (Connection.HandshakeTP == null)
                    {

                    }
                    else
                    {
                        Connection.State.ResumptionEnabled = true;
                    }
                }

                QuicSendApplyNewSettings(Connection.Send, Connection.Settings);
                QuicCongestionControlInitialize(out Connection.CongestionControl, Connection);

                if (QuicConnIsClient(Connection) && HasFlag(Connection.Settings.IsSetFlags, E_SETTING_FLAG_VersionSettings))
                {
                    Connection.Stats.QuicVersion = Connection.Settings.VersionSettings.FullyDeployedVersions[0];
                    QuicConnOnQuicVersionSet(Connection);

                    if (QUIC_FAILED(QuicCryptoOnVersionChange(Connection.Crypto)))
                    {
                        return false;
                    }
                }

                if (QuicConnIsServer(Connection) && Connection.Settings.GreaseQuicBitEnabled && BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_GREASE_QUIC_BIT))
                {
                    byte RandomValue = CxPlatRandom.RandomByte();
                    Connection.State.FixedBit = BoolOk(RandomValue % 2);
                    Connection.Stats.GreaseBitNegotiated = true;
                }

                if (QuicConnIsServer(Connection) && Connection.Settings.ReliableResetEnabled)
                {
                    Connection.State.ReliableResetStreamNegotiated = BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_RELIABLE_RESET_ENABLED);
                    QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                    Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_RELIABLE_RESET_NEGOTIATED;
                    Event.RELIABLE_RESET_NEGOTIATED = new QUIC_CONNECTION_EVENT.RELIABLE_RESET_NEGOTIATED_DATA();
                    Event.RELIABLE_RESET_NEGOTIATED.IsNegotiated = Connection.State.ReliableResetStreamNegotiated;
                    QuicConnIndicateEvent(Connection, ref Event);
                }

                if (QuicConnIsServer(Connection) && Connection.Settings.OneWayDelayEnabled)
                {
                    Connection.State.TimestampSendNegotiated = BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED);
                    Connection.State.TimestampRecvNegotiated = BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED);

                    QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                    Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_ONE_WAY_DELAY_NEGOTIATED;
                    Event.ONE_WAY_DELAY_NEGOTIATED.SendNegotiated = Connection.State.TimestampSendNegotiated;
                    Event.ONE_WAY_DELAY_NEGOTIATED.ReceiveNegotiated = Connection.State.TimestampRecvNegotiated;
                    QuicConnIndicateEvent(Connection, ref Event);
                }

                if (Connection.Settings.EcnEnabled)
                {
                    QUIC_PATH Path = Connection.Paths[0];
                    Path.EcnValidationState = ECN_VALIDATION_STATE.ECN_VALIDATION_TESTING;
                }
            }

            if (Connection.State.Started && Connection.Settings.EncryptionOffloadAllowed != Connection.Paths[0].EncryptionOffloading)
            {
                NetLog.Assert(false);
            }

            uint PeerStreamType = QuicConnIsServer(Connection) ? STREAM_ID_FLAG_IS_CLIENT : STREAM_ID_FLAG_IS_SERVER;

            if (HasFlag(NewSettings.IsSetFlags, E_SETTING_FLAG_PeerBidiStreamCount))
            {
                QuicStreamSetUpdateMaxCount(Connection.Streams, PeerStreamType | STREAM_ID_FLAG_IS_BI_DIR, Connection.Settings.PeerBidiStreamCount);
            }
            if (HasFlag(NewSettings.IsSetFlags, E_SETTING_FLAG_PeerUnidiStreamCount))
            {
                QuicStreamSetUpdateMaxCount(Connection.Streams, PeerStreamType | STREAM_ID_FLAG_IS_UNI_DIR, Connection.Settings.PeerUnidiStreamCount);
            }

            if (HasFlag(NewSettings.IsSetFlags, E_SETTING_FLAG_KeepAliveIntervalMs) && Connection.State.Started)
            {
                if (Connection.Settings.KeepAliveIntervalMs != 0)
                {
                    QuicConnProcessKeepAliveOperation(Connection);
                }
                else
                {
                    QuicConnTimerCancel(Connection, QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_KEEP_ALIVE);
                }
            }

            if (OverWrite)
            {
                //QuicSettingsDumpNew(NewSettings);
            }
            else
            {
                //QuicSettingsDump(Connection.Settings); // TODO - Really necessary?
            }

            return true;
        }

        static int QuicConnSendResumptionTicket(QUIC_CONNECTION Connection, QUIC_SSBuffer AppResumptionData)
        {
            int Status;
            QUIC_SSBuffer TicketBuffer = QUIC_SSBuffer.Empty;
            int TicketLength = 0;
            int AlpnLength = Connection.Crypto.TlsState.NegotiatedAlpn[0];

            if (Connection.HandshakeTP == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            Status = QuicCryptoEncodeServerTicket(
                    Connection,
                    Connection.Stats.QuicVersion,
                    AppResumptionData,
                    Connection.HandshakeTP,
                    Connection.Crypto.TlsState.NegotiatedAlpn.Slice(1, AlpnLength),
                    ref TicketBuffer);

            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Status = QuicCryptoProcessAppData(Connection.Crypto, TicketBuffer);

        Error:
            if (TicketBuffer != QUIC_SSBuffer.Empty)
            {

            }

            if (AppResumptionData != QUIC_SSBuffer.Empty)
            {

            }

            return Status;
        }

        static void QuicConnQueueTraceRundown(QUIC_CONNECTION Connection)
        {
            QUIC_OPERATION Oper = null;
            if ((Oper = QuicOperationAlloc(Connection.Partition,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_TRACE_RUNDOWN)) != null)
            {
                QuicConnQueueOper(Connection, Oper);
            }
        }

        static bool QUIC_CONN_BAD_START_STATE(QUIC_CONNECTION CONN)
        {
            return CONN.State.Started || CONN.State.ClosedLocally;
        }

        static bool QuicConnPeerCertReceived(QUIC_CONNECTION Connection, ReadOnlySpan<byte> Certificate, ReadOnlySpan<byte> Chain, uint DeferredErrorFlags, int DeferredStatus)
        {
            QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
            Connection.Crypto.CertValidationPending = true;
            Event.Type =  QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_PEER_CERTIFICATE_RECEIVED;
            Event.PEER_CERTIFICATE_RECEIVED.Certificate = Certificate.ToArray();
            Event.PEER_CERTIFICATE_RECEIVED.Chain = Chain.ToArray();
            Event.PEER_CERTIFICATE_RECEIVED.DeferredErrorFlags = DeferredErrorFlags;
            Event.PEER_CERTIFICATE_RECEIVED.DeferredStatus = DeferredStatus;

            int Status = QuicConnIndicateEvent(Connection, ref Event);
            if (QUIC_FAILED(Status))
            {
                Connection.Crypto.CertValidationPending = false;
                return false;
            }
            if (Status == QUIC_STATUS_PENDING)
            {
                
            }
            else if (Status == QUIC_STATUS_SUCCESS)
            {
                Connection.Crypto.CertValidationPending = false;
            }
            return true; // Treat pending as success to the TLS layer.
        }

        static bool QuicConnRecvResumptionTicket(QUIC_CONNECTION Connection, ReadOnlySpan<byte> Ticket)
        {
            bool ResumptionAccepted = false;
            QUIC_TRANSPORT_PARAMETERS ResumedTP = new QUIC_TRANSPORT_PARAMETERS();
            if (QuicConnIsServer(Connection))
            {
                if (Connection.Crypto.TicketValidationRejecting)
                {
                    Connection.Crypto.TicketValidationRejecting = false;
                    Connection.Crypto.TicketValidationPending = false;
                    goto Error;
                }
                Connection.Crypto.TicketValidationPending = true;
                QUIC_SSBuffer AppData = QUIC_SSBuffer.Empty;

                int Status =
                    QuicCryptoDecodeServerTicket(
                        Connection,
                        Ticket,
                        Connection.Configuration.AlpnList,
                        ref ResumedTP,
                        out AppData);

                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }
                
                if (ResumedTP.ActiveConnectionIdLimit > QUIC_ACTIVE_CONNECTION_ID_LIMIT ||
                    ResumedTP.InitialMaxData > Connection.Send.MaxData ||
                    ResumedTP.InitialMaxStreamDataBidiLocal > Connection.Settings.StreamRecvWindowBidiLocalDefault ||
                    ResumedTP.InitialMaxStreamDataBidiRemote > Connection.Settings.StreamRecvWindowBidiRemoteDefault ||
                    ResumedTP.InitialMaxStreamDataUni > Connection.Settings.StreamRecvWindowUnidiDefault ||
                    ResumedTP.InitialMaxUniStreams > Connection.Streams.Types[STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_UNI_DIR].MaxTotalStreamCount ||
                    ResumedTP.InitialMaxBidiStreams > Connection.Streams.Types[STREAM_ID_FLAG_IS_CLIENT | STREAM_ID_FLAG_IS_BI_DIR].MaxTotalStreamCount)
                {
                    goto Error;
                }

                QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                Event.Type =  QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_RESUMED;
                Event.RESUMED.ResumptionState = AppData;
                Status = QuicConnIndicateEvent(Connection, ref Event);
                if (Status == QUIC_STATUS_SUCCESS)
                {
                    ResumptionAccepted = true;
                    Connection.Crypto.TicketValidationPending = false;
                }
                else if (Status == QUIC_STATUS_PENDING)
                {
                    ResumptionAccepted = true;
                }
                else
                {
                    ResumptionAccepted = false;
                    Connection.Crypto.TicketValidationPending = false;
                }

            }
            else
            {

                QUIC_SSBuffer ClientTicket = QUIC_SSBuffer.Empty;
                NetLog.Assert(Connection.State.PeerTransportParameterValid);

                if (QUIC_SUCCEEDED(QuicCryptoEncodeClientTicket(
                        Connection,
                        Ticket,
                        Connection.PeerTransportParams,
                        Connection.Stats.QuicVersion,
                        out ClientTicket)))
                {
                    QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                    Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_RESUMPTION_TICKET_RECEIVED;
                    Event.RESUMPTION_TICKET_RECEIVED.ResumptionTicket = ClientTicket;
                    QuicConnIndicateEvent(Connection, ref Event);
                    ResumptionAccepted = true;
                }
            }

        Error:
            QuicCryptoTlsCleanupTransportParameters(ResumedTP);
            return ResumptionAccepted;
        }

    }



}
