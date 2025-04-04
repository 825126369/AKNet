using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp5Quic.Common
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
    }

    internal class QUIC_CONN_STATS
    {
        public long CorrelationId;
        public uint VersionNegotiation;
        public uint StatelessRetry;
        public uint ResumptionAttempted;
        public uint ResumptionSucceeded;
        public uint GreaseBitNegotiated;
        public uint EncryptionOffloaded;
        public uint QuicVersion;

        public class Timing_Class
        {
            public long Start;
            public ulong InitialFlightEnd;      // Processed all peer's Initial packets
            public ulong HandshakeFlightEnd;    // Processed all peer's Handshake packets
            public long PhaseShift;             // Time between local and peer epochs
        }

        public class Schedule_Class
        {
            public long LastQueueTime;         // Time the connection last entered the work queue.
            public ulong DrainCount;            // Sum of drain calls
            public ulong OperationCount;        // Sum of operations processed
        }

        public class Handshake_Class
        {
            public uint ClientFlight1Bytes;    // Sum of TLS payloads
            public uint ServerFlight1Bytes;    // Sum of TLS payloads
            public uint ClientFlight2Bytes;    // Sum of TLS payloads
            public byte HandshakeHopLimitTTL;   // TTL value in the initial packet of the handshake.
        }

        public class Send_Class
        {
            ulong TotalPackets;          // QUIC packets; could be coalesced into fewer UDP datagrams.
            ulong RetransmittablePackets;
            ulong SuspectedLostPackets;
            ulong SpuriousLostPackets;   // Actual lost is (SuspectedLostPackets - SpuriousLostPackets)

            ulong TotalBytes;            // Sum of UDP payloads
            ulong TotalStreamBytes;      // Sum of stream payloads

            public uint CongestionCount;
            public uint EcnCongestionCount;
            public uint PersistentCongestionCount;
        }

        public class Recv_Class
        {
            ulong TotalPackets;          // QUIC packets; could be coalesced into fewer UDP datagrams.
            ulong ReorderedPackets;      // Packets where packet number is less than highest seen.
            ulong DroppedPackets;        // Includes DuplicatePackets.
            ulong DuplicatePackets;
            ulong DecryptionFailures;    // Count of packets that failed to decrypt.
            ulong ValidPackets;          // Count of packets that successfully decrypted or had no encryption.
            ulong ValidAckFrames;        // Count of receive ACK frames.

            ulong TotalBytes;            // Sum of UDP payloads
            ulong TotalStreamBytes;      // Sum of stream payloads
        }

        public class Misc_Class
        {
            public uint KeyUpdateCount;        // Count of key updates completed.
            public uint DestCidUpdateCount;    // Number of times the destination CID changed.
        }

        public Timing_Class Timing;
        public Schedule_Class Schedule;
        public Handshake_Class Handshake;
        public Send_Class Send;
        public Recv_Class Recv;
        public Misc_Class Misc;
    }

    internal class QUIC_CONNECTION : QUIC_HANDLE, CXPLAT_POOL_Interface<QUIC_CONNECTION>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_CONNECTION> POOL_ENTRY = null;

        public CXPLAT_LIST_ENTRY RegistrationLink;
        public CXPLAT_LIST_ENTRY WorkerLink;
        public readonly CXPLAT_LIST_ENTRY_QUIC_CONNECTION TimerLink = null;
        public QUIC_WORKER Worker;
        public QUIC_REGISTRATION Registration;
        public QUIC_CONFIGURATION Configuration;
        public QUIC_SETTINGS_INTERNAL Settings;
        public long RefCount;
        public readonly int[] RefTypeCount = new int[(int)QUIC_CONNECTION_REF.QUIC_CONN_REF_COUNT];

        public QUIC_CONNECTION_STATE State;
        public int WorkerThreadID;
        
        public readonly byte[] ServerID = new byte[MSQuicFunc.QUIC_MAX_CID_SID_LENGTH];
        public byte PartitionID;
        public byte DestCidCount;
        public byte RetiredDestCidCount;
        public byte SourceCidLimit;
        public byte PathsCount;
        public byte NextPathId;
        public bool WorkerProcessing;
        public bool HasQueuedWork;
        public bool HasPriorityWork;
        
        public byte OutFlowBlockedReasons; // Set of QUIC_FLOW_BLOCKED_* flags
        public byte AckDelayExponent;
        public byte PacketTolerance;
        public byte PeerPacketTolerance;
        public byte ReorderingThreshold;
        public byte PeerReorderingThreshold;
        public byte DSCP;
        public ulong SendAckFreqSeqNum;
        public ulong NextRecvAckFreqSeqNum;
        public ulong NextSourceCidSequenceNumber;
        public ulong RetirePriorTo;
        public QUIC_PATH[] Paths = new QUIC_PATH[MSQuicFunc.QUIC_MAX_PATH_COUNT];
        public CXPLAT_SLIST_ENTRY SourceCids;
        public CXPLAT_LIST_ENTRY DestCids;
        public QUIC_CID OrigDestCID;
        public readonly byte[] CibirId = new byte[2 + MSQuicFunc.QUIC_MAX_CIBIR_LENGTH];
        public readonly long[] ExpirationTimes = new long[(int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_COUNT];
        public long EarliestExpirationTime;
        public uint ReceiveQueueCount;
        public uint ReceiveQueueByteCount;
        public QUIC_RX_PACKET ReceiveQueue;
        public QUIC_RX_PACKET ReceiveQueueTail;
        public readonly object ReceiveQueueLock = new object();
        public QUIC_OPERATION_QUEUE OperQ;
        public QUIC_OPERATION BackUpOper;
        public QUIC_API_CONTEXT BackupApiContext;
        public int BackUpOperUsed;
        public ulong CloseStatus;
        public ulong CloseErrorCode;
        public byte[] CloseReasonPhrase;

        public string RemoteServerName;
        public QUIC_TRANSPORT_PARAMETERS PeerTransportParams;
        public QUIC_RANGE DecodedAckRanges;
        public QUIC_STREAM_SET Streams;
        public QUIC_CONGESTION_CONTROL CongestionControl;
        public QUIC_LOSS_DETECTION LossDetection;
        public QUIC_PACKET_SPACE[] Packets = new QUIC_PACKET_SPACE[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_COUNT];
        public QUIC_CRYPTO Crypto;
        public QUIC_SEND Send;
        public QUIC_SEND_BUFFER SendBuffer;
        public QUIC_DATAGRAM Datagram;
        public QUIC_CONNECTION_CALLBACK ClientCallbackHandler;
        
        public QUIC_TRANSPORT_PARAMETERS HandshakeTP;
        public QUIC_CONN_STATS Stats;
        public QUIC_PRIVATE_TRANSPORT_PARAMETER TestTransportParameter;
        public QUIC_TLS_SECRETS TlsSecrets;
        public uint PreviousQuicVersion;
        public uint OriginalQuicVersion;
        public ushort KeepAlivePadding;

        public class BlockedTimings
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
            NetLog.Assert(!Connection.State.Freed);
        }

        static void QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS Type)
        {
            QuicPerfCounterAdd(Type, 1);
        }

        static void QuicPerfCounterDecrement(QUIC_PERFORMANCE_COUNTERS Type)
        {
            QuicPerfCounterAdd(Type, -1);
        }

        static void QuicConnAddRef(QUIC_CONNECTION Connection, QUIC_CONNECTION_REF Ref)
        {
            QuicConnValidate(Connection);
#if DEBUG
            Interlocked.Increment(ref Connection.RefTypeCount[(int)Ref]);
#else
    
#endif
            Interlocked.Increment(ref Connection.RefCount);
        }

        static bool QuicConnIsServer(QUIC_CONNECTION Connection)
        {
            return Connection.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER;
        }

        static bool QuicConnIsClient(QUIC_CONNECTION Connection)
        {
            return Connection.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT;
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
            if (QuicOperationEnqueue(Connection.OperQ, Oper))
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
            if (QuicOperationEnqueuePriority(Connection.OperQ, Oper))
            {
                QuicWorkerQueuePriorityConnection(Connection.Worker, Connection);
            }
        }

        static void QuicConnQueueHighestPriorityOper(QUIC_CONNECTION Connection, QUIC_OPERATION Oper)
        {
            if (QuicOperationEnqueueFront(Connection.OperQ, Oper))
            {
                QuicWorkerQueuePriorityConnection(Connection.Worker, Connection);
            }
        }

        static void QuicConnRelease(QUIC_CONNECTION Connection, QUIC_CONNECTION_REF Ref)
        {
            QuicConnValidate(Connection);
            NetLog.Assert(Connection.RefTypeCount[Ref] > 0);
            ushort result = (ushort)Interlocked.Decrement(ref Connection.RefTypeCount[(int)Ref]);
            NetLog.Assert(result != 0xFFFF);

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

        static void QuicConnOnQuicVersionSet(QUIC_CONNECTION Connection)
        {
            switch (Connection.Stats.QuicVersion)
            {
                case QUIC_VERSION_1:
                case QUIC_VERSION_DRAFT_29:
                case QUIC_VERSION_MS_1:
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

        static ulong QuicConnAlloc(QUIC_REGISTRATION Registration, QUIC_WORKER Worker, QUIC_RX_PACKET Packet, ref QUIC_CONNECTION NewConnection)
        {
            bool IsServer = Packet != null;
            NewConnection = null;
            ulong Status;

            int PartitionIndex = IsServer ? Packet.PartitionIndex : QuicLibraryGetCurrentPartition();
            int PartitionId = QuicPartitionIdCreate(PartitionIndex);
            NetLog.Assert(PartitionIndex == QuicPartitionIdGetIndex(PartitionId));

            QUIC_CONNECTION Connection = QuicLibraryGetPerProc().ConnectionPool.Pop();
            if (Connection == null)
            {
                QuicTraceEvent(QuicEventId.AllocFailure, "Allocation of '%s' failed. (%llu bytes)", "connection");
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_CREATED);
            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_ACTIVE);
            Connection.Stats.CorrelationId = Interlocked.Increment(ref MsQuicLib.ConnectionCorrelationId) - 1;
            Connection.RefCount = 1;

            Connection.PartitionID = (byte)PartitionId;
            Connection.State.Allocated = true;
            Connection.State.ShareBinding = IsServer;
            Connection.State.FixedBit = true;
            Connection.Stats.Timing.Start = mStopwatch.ElapsedMilliseconds;
            Connection.SourceCidLimit = QUIC_ACTIVE_CONNECTION_ID_LIMIT;
            Connection.AckDelayExponent = QUIC_ACK_DELAY_EXPONENT;
            Connection.PacketTolerance = QUIC_MIN_ACK_SEND_NUMBER;
            Connection.PeerPacketTolerance = QUIC_MIN_ACK_SEND_NUMBER;
            Connection.ReorderingThreshold = QUIC_MIN_REORDERING_THRESHOLD;
            Connection.PeerReorderingThreshold = QUIC_MIN_REORDERING_THRESHOLD;
            Connection.PeerTransportParams.AckDelayExponent = QUIC_TP_ACK_DELAY_EXPONENT_DEFAULT;
            Connection.ReceiveQueueTail = Connection.ReceiveQueue;
            QuicSettingsCopy(Connection.Settings, MsQuicLib.Settings);
            Connection.Settings.IsSetFlags = 0; // Just grab the global values, not IsSet flags.

            Monitor.Enter(Connection.ReceiveQueueLock);
            CxPlatListInitializeHead(Connection.DestCids);
            QuicStreamSetInitialize(Connection.Streams);
            QuicSendBufferInitialize(Connection.SendBuffer);
            QuicOperationQueueInitialize(Connection.OperQ);
            QuicSendInitialize(Connection.Send, Connection.Settings);
            QuicCongestionControlInitialize(Connection.CongestionControl, Connection.Settings);
            QuicLossDetectionInitialize(Connection.LossDetection);
            QuicDatagramInitialize(Connection.Datagram);

            QuicRangeInitialize(QUIC_MAX_RANGE_DECODE_ACKS, Connection.DecodedAckRanges);
            for (int i = 0; i < Connection.Packets.Length; i++)
            {
                Status = QuicPacketSpaceInitialize(Connection, (QUIC_ENCRYPT_LEVEL)i, Connection.Packets[i]);
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
                    CxPlatRandom(1, Connection.ServerID);
                    if (Packet.Route.LocalAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] IP_Array = IPAddressHelper.ConvertIPToByte(Packet.Route.LocalAddress);
                        Array.Copy(IP_Array, 0, Connection.ServerID, 1, 4);
                    }
                    else if (Packet.Route.LocalAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] IP_Array = IPAddressHelper.ConvertIPToByte(Packet.Route.LocalAddress);
                        Array.Copy(IP_Array, 12, Connection.ServerID, 1, 4);
                    }
                }
                else if (MsQuicLib.Settings.LoadBalancingMode == QUIC_LOAD_BALANCING_MODE.QUIC_LOAD_BALANCING_SERVER_ID_FIXED)
                {
                    CxPlatRandom(1, Connection.ServerID);
                    EndianBitConverter.SetBytes(Connection.ServerID, 1, MsQuicLib.Settings.FixedServerID);
                }

                Connection.Stats.QuicVersion = Packet.Invariant.LONG_HDR.Version;
                QuicConnOnQuicVersionSet(Connection);
                QuicCopyRouteInfo(Path.Route, Packet.Route);
                Connection.State.LocalAddressSet = true;
                Connection.State.RemoteAddressSet = true;

                Path.DestCid = QuicCidNewDestination(Packet.SourceCidLen, Packet.SourceCid);
                if (Path.DestCid == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Error;
                }

                Path.DestCid.CID.UsedLocally = true;
                CxPlatListInsertTail(Connection.DestCids, Path.DestCid.Link);

                QUIC_CID_HASH_ENTRY SourceCid = QuicCidNewSource(Connection, Packet.DestCidLen, Packet.DestCid);
                if (SourceCid == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Error;
                }
                SourceCid.CID.IsInitial = true;
                SourceCid.CID.UsedByPeer = true;
                CxPlatListPushEntry(Connection.SourceCids, SourceCid.Link);
            }
            else
            {
                Connection.Type = QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT;
                Connection.State.ExternalOwner = true;
                Path.IsPeerValidated = true;
                Path.Allowance = uint.MaxValue;

                Path.DestCid = QuicCidNewRandomDestination();
                if (Path.DestCid == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Error;
                }
                Path.DestCid.CID.UsedLocally = true;
                Connection.DestCidCount++;
                CxPlatListInsertTail(Connection.DestCids, Path.DestCid.Link);
                Connection.State.Initialized = true;
            }

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

            if (Packet != null && Connection.SourceCids.Next != null)
            {
                Connection.SourceCids.Next = null;
            }
            while (!CxPlatListIsEmpty(Connection.DestCids))
            {
                CxPlatListRemoveHead(Connection.DestCids);
            }
            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_HANDLE_OWNER);
            return Status;
        }

        static ushort CxPlatSocketGetLocalMtu(CXPLAT_SOCKET Socket)
        {
            NetLog.Assert(Socket != null);
            //if (Socket.UseTcp || (Socket.RawSocketAvailable && !IPAddress.IsLoopback(Socket.RemoteAddress)))
            //{
            //    return RawSocketGetLocalMtu(Socket.raw);
            //}
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
            long TimeNow = mStopwatch.ElapsedMilliseconds;
            QuicConnTimerSetEx(Connection, Type, DelayUs, TimeNow);
        }

        static ulong QuicErrorCodeToStatus(ulong ErrorCode)
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

        static void QuicConnTryClose(QUIC_CONNECTION Connection, uint Flags, ulong ErrorCode, byte[] RemoteReasonPhrase, ushort RemoteReasonPhraseLength)
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
                Connection.State.ClosedRemotely = true;
            }
            else
            {
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
                        QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_PROTOCOL_ERRORS);
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

                if (RemoteReasonPhraseLength != 0)
                {
                    Connection.CloseReasonPhrase = CXPLAT_ALLOC_NONPAGED(RemoteReasonPhraseLength + 1, QUIC_POOL_CLOSE_REASON);
                    if (Connection.CloseReasonPhrase != null)
                    {
                        Array.Copy(RemoteReasonPhrase, Connection.CloseReasonPhrase, RemoteReasonPhraseLength);
                    }
                    else
                    {
                        RemoteReasonPhraseLength + 1);
                    }
                }

                if (Connection.State.Started)
                {
                    QuicConnLogStatistics(Connection);
                }

                QuicStreamSetShutdown(Connection.Streams);
                QuicDatagramSendShutdown(Connection.Datagram);
            }

            if (SilentClose)
            {
                QuicSendClear(Connection.Send);
            }

            if (SilentClose ||
                (Connection.State.ClosedRemotely && Connection.State.ClosedLocally))
            {
                Connection.State.ShutdownCompleteTimedOut = false;
                Connection.State.ProcessShutdownComplete = true;
            }
        }

        static void QuicConnCloseLocally(QUIC_CONNECTION Connection, uint Flags, ulong ErrorCode, string ErrorMsg)
        {
            NetLog.Assert(ErrorMsg == null || ErrorMsg.Length < ushort.MaxValue);
            QuicConnTryClose(
                Connection,
                Flags,
                ErrorCode,
                ErrorMsg,
                ErrorMsg == null ? 0 : (ushort)ErrorMsg.Length;
        }

        static void QuicConnCloseHandle(QUIC_CONNECTION Connection)
        {
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
            return StreamSet.mCONNECTION;
        }

        static ulong QuicConnIndicateEvent(QUIC_CONNECTION Connection, QUIC_CONNECTION_EVENT Event)
        {
            ulong Status;
            if (Connection.ClientCallbackHandler != null)
            {
                NetLog.Assert(!Connection.State.InlineApiExecution || Connection.State.HandleClosed);
                Status = Connection.ClientCallbackHandler(Connection, null, Event);
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
                QuicBindingRemoveConnection(Connection->Paths[0].Binding, Connection);
            }

            //
            // Clean up the rest of the internal state.
            //
            QuicTimerWheelRemoveConnection(&Connection->Worker->TimerWheel, Connection);
            QuicLossDetectionUninitialize(&Connection->LossDetection);
            QuicSendUninitialize(&Connection->Send);
            QuicDatagramSendShutdown(&Connection->Datagram);

            if (Connection->State.ExternalOwner)
            {

                QUIC_CONNECTION_EVENT Event;
                Event.Type = QUIC_CONNECTION_EVENT_SHUTDOWN_COMPLETE;
                Event.SHUTDOWN_COMPLETE.HandshakeCompleted =
                    Connection->State.Connected;
                Event.SHUTDOWN_COMPLETE.PeerAcknowledgedShutdown =
                    !Connection->State.ShutdownCompleteTimedOut;
                Event.SHUTDOWN_COMPLETE.AppCloseInProgress =
                    Connection->State.HandleClosed;

                QuicTraceLogConnVerbose(
                    IndicateConnectionShutdownComplete,
                    Connection,
                    "Indicating QUIC_CONNECTION_EVENT_SHUTDOWN_COMPLETE");
                (void)QuicConnIndicateEvent(Connection, &Event);

                // This need to be later than QuicLossDetectionUninitialize to indicate
                // status change of Datagram frame for an app to free its buffer
                Connection->ClientCallbackHandler = NULL;
            }
            else
            {
                //
                // If the connection was never indicated to the application, then the
                // "owner" ref still resides with the stack and needs to be released.
                //
                QuicConnUnregister(Connection);
                QuicConnRelease(Connection, QUIC_CONN_REF_HANDLE_OWNER);
            }
        }

        static void QuicConnLogOutFlowStats(QUIC_CONNECTION Connection)
        {
            if (!QuicTraceEventEnabled(ConnOutFlowStats)) {
                return;
            }

            QuicCongestionControlLogOutFlowStatus(&Connection->CongestionControl);

            uint64_t FcAvailable, SendWindow;
            QuicStreamSetGetFlowControlSummary(
            &Connection->Streams,
                &FcAvailable,
                &SendWindow);

            QuicTraceEvent(
                ConnOutFlowStreamStats,
                "[conn][%p] OUT: StreamFC=%llu StreamSendWindow=%llu",
                Connection,
                FcAvailable,
                SendWindow);
        }

        static void QuicConnQueueUnreachable(QUIC_CONNECTION Connection, IPAddress RemoteAddress)
        {
            if (Connection.Crypto.TlsState.ReadKey >  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL)
            {
                return;
            }

            QUIC_OPERATION ConnOper = QuicOperationAlloc(Connection.Worker,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_UNREACHABLE);
            if (ConnOper != null)
            {
                ConnOper.UNREACHABLE.RemoteAddress = RemoteAddress;
                QuicConnQueueOper(Connection, ConnOper);
            }
        }
    }

}
