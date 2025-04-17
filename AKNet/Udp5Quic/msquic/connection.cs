using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Xml;
using static System.Net.WebRequestMethods;

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

        public Timing_DATA Timing;
        public Schedule_DATA Schedule;
        public Handshake_DATA Handshake;
        public Send_DATA Send;
        public Recv_DATA Recv;
        public Misc_DATA Misc;

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
            public ulong TotalPackets;          // QUIC packets; could be coalesced into fewer UDP datagrams.
            public ulong RetransmittablePackets;
            public ulong SuspectedLostPackets;
            public ulong SpuriousLostPackets;   // Actual lost is (SuspectedLostPackets - SpuriousLostPackets)
            public ulong TotalBytes;            // Sum of UDP payloads
            public ulong TotalStreamBytes;      // Sum of stream payloads
            public uint CongestionCount;
            public uint EcnCongestionCount;
            public uint PersistentCongestionCount;
        }

        public class Recv_DATA
        {
            public long TotalPackets;          // QUIC packets; could be coalesced into fewer UDP datagrams.
            public long ReorderedPackets;      // Packets where packet number is less than highest seen.
            public long DroppedPackets;        // Includes DuplicatePackets.
            public long DuplicatePackets;
            public long DecryptionFailures;    // Count of packets that failed to decrypt.
            public long ValidPackets;          // Count of packets that successfully decrypted or had no encryption.
            public long ValidAckFrames;        // Count of receive ACK frames.
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

        public CXPLAT_LIST_ENTRY RegistrationLink;
        public CXPLAT_LIST_ENTRY WorkerLink;
        public readonly CXPLAT_LIST_ENTRY<QUIC_CONNECTION> TimerLink = null;
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
        public int ReceiveQueueCount;
        public int ReceiveQueueByteCount;
        public QUIC_RX_PACKET ReceiveQueue;
        public QUIC_RX_PACKET ReceiveQueueTail;
        public readonly object ReceiveQueueLock = new object();
        public QUIC_OPERATION_QUEUE OperQ;
        public QUIC_OPERATION BackUpOper;
        public QUIC_API_CONTEXT BackupApiContext;
        public int BackUpOperUsed;
        public ulong CloseStatus;
        public ulong CloseErrorCode;
        public string CloseReasonPhrase;

        public string RemoteServerName;
        public QUIC_REMOTE_HASH_ENTRY RemoteHashEntry;
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

        public class BlockedTimings_Class
        {
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER Scheduling;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER Pacing;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER AmplificationProt;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER CongestionControl;
            public QUIC_FLOW_BLOCKED_TIMING_TRACKER FlowControl;
        }
        public BlockedTimings_Class BlockedTimings;

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

        static void QuicConnTryClose(QUIC_CONNECTION Connection, uint Flags, ulong ErrorCode, string RemoteReasonPhrase)
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

                Connection.CloseReasonPhrase = RemoteReasonPhrase;
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
                QuicBindingRemoveConnection(Connection.Paths[0].Binding, Connection);
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

        static void QuicConnTimerExpired(QUIC_CONNECTION Connection, long TimeNow)
        {
            bool FlushSendImmediate = false;
            Connection.EarliestExpirationTime = long.MaxValue;
            for (int Type = 0; Type <  (int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_COUNT; ++Type)
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
                        if ((Oper = QuicOperationAlloc(Connection.Worker,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_TIMER_EXPIRED)) != null)
                        {
                            Oper.TIMER_EXPIRED.Type = Type;
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

        static void QuicConnRetireCid(QUIC_CONNECTION Connection, QUIC_CID_LIST_ENTRY DestCid)
        {
            Connection.DestCidCount--;
            DestCid.CID.Retired = true;
            DestCid.CID.NeedsToSend = true;
            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_RETIRE_CONNECTION_ID);

            Connection.RetiredDestCidCount++;
            if (Connection.RetiredDestCidCount > 8 * QUIC_ACTIVE_CONNECTION_ID_LIMIT)
            {
                QuicConnSilentlyAbort(Connection);
            }
        }

        static QUIC_CID_LIST_ENTRY QuicConnGetUnusedDestCid(QUIC_CONNECTION Connection)
        {
            for (CXPLAT_LIST_ENTRY Entry = Connection.DestCids.Flink; Entry != Connection.DestCids; Entry = Entry.Flink)
            {
                QUIC_CID_LIST_ENTRY DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_LIST_ENTRY>(Entry);
                if (!DestCid.CID.UsedLocally && !DestCid.CID.Retired)
                {
                    return DestCid;
                }
            }
            return null;
        }

        static bool QuicConnRetireCurrentDestCid(QUIC_CONNECTION Connection, QUIC_PATH Path)
        {
            if (Path.DestCid.CID.Length == 0)
            {
                return true;
            }

            QUIC_CID_LIST_ENTRY NewDestCid = QuicConnGetUnusedDestCid(Connection);
            if (NewDestCid == null)
            {
                return false;
            }

            NetLog.Assert(Path.DestCid != NewDestCid);
            QUIC_CID_LIST_ENTRY OldDestCid = Path.DestCid;
            QuicConnRetireCid(Connection, Path.DestCid);
            Path.DestCid = NewDestCid;
            Path.DestCid.CID.UsedLocally = true;
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
                if (CxPlatTimeDiff64(Path.MtuDiscovery.SearchCompleteEnterTimeUs, TimeNow) >= TimeoutTime)
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
                    Connection.BlockedTimings.Pacing.CumulativeTimeUs += CxPlatTime(Connection.BlockedTimings.Pacing.LastStartTimeUs, Now);
                    Connection.BlockedTimings.Pacing.LastStartTimeUs = 0;
                }
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_SCHEDULING) && BoolOk(Reason & QUIC_FLOW_BLOCKED_SCHEDULING))
                {
                    Connection.BlockedTimings.Scheduling.CumulativeTimeUs += CxPlatTimeDiff64(Connection.BlockedTimings.Scheduling.LastStartTimeUs, Now);
                    Connection.BlockedTimings.Scheduling.LastStartTimeUs = 0;
                }
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT) && BoolOk(Reason & QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT))
                {
                    Connection.BlockedTimings.AmplificationProt.CumulativeTimeUs += CxPlatTimeDiff64(Connection.BlockedTimings.AmplificationProt.LastStartTimeUs, Now);
                    Connection.BlockedTimings.AmplificationProt.LastStartTimeUs = 0;
                }
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_CONGESTION_CONTROL) && BoolOk(Reason & QUIC_FLOW_BLOCKED_CONGESTION_CONTROL))
                {
                    Connection.BlockedTimings.CongestionControl.CumulativeTimeUs += CxPlatTimeDiff64(Connection.BlockedTimings.CongestionControl.LastStartTimeUs, Now);
                    Connection.BlockedTimings.CongestionControl.LastStartTimeUs = 0;
                }
                if (BoolOk(Connection.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_CONN_FLOW_CONTROL) && BoolOk(Reason & QUIC_FLOW_BLOCKED_CONN_FLOW_CONTROL))
                {
                    Connection.BlockedTimings.FlowControl.CumulativeTimeUs += CxPlatTimeDiff64(Connection.BlockedTimings.FlowControl.LastStartTimeUs, Now);
                    Connection.BlockedTimings.FlowControl.LastStartTimeUs = 0;
                }

                Connection.OutFlowBlockedReasons = (byte)(Connection.OutFlowBlockedReasons & ~Reason);
                return true;
            }
            return false;
        }

        static void QuicConnFatalError(QUIC_CONNECTION Connection, ulong Status, string ErrorMsg)
        {
            QuicConnCloseLocally(
                Connection,
                QUIC_CLOSE_INTERNAL | QUIC_CLOSE_QUIC_STATUS,
                Status,
                ErrorMsg);
        }

        static bool QuicConnDrainOperations(QUIC_CONNECTION Connection, bool StillHasPriorityWork)
        {
            QUIC_OPERATION Oper;
            int MaxOperationCount = Connection.Settings.MaxOperationsPerDrain;
            int OperationCount = 0;
            bool HasMoreWorkToDo = true;

            if (!Connection.State.Initialized && !Connection.State.ShutdownComplete)
            {
                NetLog.Assert(QuicConnIsServer(Connection));
                ulong Status;
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
                Oper = QuicOperationDequeue(Connection.OperQ);
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

                    case  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_RECV:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }

                        if (!QuicConnFlushRecv(Connection))
                        {
                            FreeOper = false;
                            QuicOperationEnqueue(Connection.OperQ, Oper);
                        }
                        break;

                    case  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_UNREACHABLE:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }
                        QuicConnProcessUdpUnreachable(Connection, Oper.UNREACHABLE.RemoteAddress);
                        break;

                    case  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_STREAM_RECV:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }
                        QuicStreamRecvFlush(Oper.FLUSH_STREAM_RECEIVE.Stream);
                        break;

                    case  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_SEND:
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
                            QuicOperationEnqueue(Connection.OperQ, Oper);
                        }
                        break;

                    case  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_TIMER_EXPIRED:
                        if (Connection.State.ShutdownComplete)
                        {
                            break;
                        }
                        QuicConnProcessExpiredTimer(Connection, Oper.TIMER_EXPIRED.Type);
                        break;

                    case  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_TRACE_RUNDOWN:
                        QuicConnTraceRundownOper(Connection);
                        break;

                    case  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_ROUTE_COMPLETION:
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
                    QuicOperationFree(Connection.Worker, Oper);
                }

                Connection.Stats.Schedule.OperationCount++;
                QuicPerfCounterIncrement( QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_COMPLETED);
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

        static void QuicConnProcessApiOperation(QUIC_CONNECTION Connection, QUIC_API_CONTEXT ApiCtx)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            ulong ApiStatus = ApiCtx.Status;
            CXPLAT_EVENT ApiCompleted = ApiCtx.Completed;

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
                    Status =
                        QuicConnStart(
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
                    Status = QuicConnSendResumptionTicket(
                            Connection,
                            ApiCtx.CONN_SEND_RESUMPTION_TICKET.AppDataLength,
                            ApiCtx.CONN_SEND_RESUMPTION_TICKET.ResumptionAppData);
                    ApiCtx.CONN_SEND_RESUMPTION_TICKET.ResumptionAppData = NULL;
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
                    QuicCryptoCustomCertValidationComplete(
                        &Connection->Crypto,
                        ApiCtx->CONN_COMPLETE_CERTIFICATE_VALIDATION.Result,
                        ApiCtx->CONN_COMPLETE_CERTIFICATE_VALIDATION.TlsAlert);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_CLOSE:
                    QuicStreamClose(ApiCtx->STRM_CLOSE.Stream);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_SHUTDOWN:
                    QuicStreamShutdown(
                        ApiCtx->STRM_SHUTDOWN.Stream,
                        ApiCtx->STRM_SHUTDOWN.Flags,
                        ApiCtx->STRM_SHUTDOWN.ErrorCode);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_START:
                    Status =
                        QuicStreamStart(
                            ApiCtx->STRM_START.Stream,
                            ApiCtx->STRM_START.Flags,
                            FALSE);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_SEND:
                    QuicStreamSendFlush(
                        ApiCtx->STRM_SEND.Stream);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_COMPLETE:
                    QuicStreamReceiveCompletePending(
                        ApiCtx->STRM_RECV_COMPLETE.Stream);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_SET_ENABLED:
                    Status =
                        QuicStreamRecvSetEnabledState(
                            ApiCtx->STRM_RECV_SET_ENABLED.Stream,
                            ApiCtx->STRM_RECV_SET_ENABLED.IsEnabled);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_SET_PARAM:
                    Status =
                        QuicLibrarySetParam(
                            ApiCtx->SET_PARAM.Handle,
                            ApiCtx->SET_PARAM.Param,
                            ApiCtx->SET_PARAM.BufferLength,
                            ApiCtx->SET_PARAM.Buffer);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_GET_PARAM:
                    Status =
                        QuicLibraryGetParam(
                            ApiCtx->GET_PARAM.Handle,
                            ApiCtx->GET_PARAM.Param,
                            ApiCtx->GET_PARAM.BufferLength,
                            ApiCtx->GET_PARAM.Buffer);
                    break;

                case QUIC_API_TYPE.QUIC_API_TYPE_DATAGRAM_SEND:
                    QuicDatagramSendFlush(&Connection->Datagram);
                    break;

                default:
                    CXPLAT_TEL_ASSERT(FALSE);
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
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

        static ulong QuicConnStart(QUIC_CONNECTION Connection, QUIC_CONFIGURATION Configuration, AddressFamily Family, string ServerName, int ServerPort)
        {
            ulong Status;
            QUIC_PATH Path = Connection.Paths[0];
            NetLog.Assert(QuicConnIsClient(Connection));

            if (Connection.State.ClosedLocally || Connection.State.Started)
            {
                return QUIC_STATUS_INVALID_STATE;
            }

            bool RegistrationShutingDown;
            ulong ShutdownErrorCode;
            QUIC_CONNECTION_SHUTDOWN_FLAGS ShutdownFlags;
            CxPlatDispatchLockAcquire(Connection.Registration.ConnectionLock);
            ShutdownErrorCode = Connection.Registration.ShutdownErrorCode;
            ShutdownFlags = Connection.Registration.ShutdownFlags;
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
            UdpConfig.Flags = Connection.State.ShareBinding ? MSQuicFunc.CXPLAT_SOCKET_FLAG_SHARE : 0;
            UdpConfig.InterfaceIndex = Connection.State.LocalInterfaceSet ? (int)Path.Route.LocalAddress.Ip.ScopeId : 0;
            UdpConfig.PartitionIndex = QuicPartitionIdGetIndex(Connection.PartitionID);

            Status = QuicLibraryGetBinding(UdpConfig, ref Path.Binding);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }
            
            QUIC_CID_HASH_ENTRY SourceCid;
            if (Connection.State.ShareBinding)
            {
                SourceCid = QuicCidNewRandomSource(Connection, null, Connection.PartitionID, Connection.CibirId[0], Connection.CibirId.AsSpan().Slice(2));
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
            CxPlatListPushEntry(Connection.SourceCids, SourceCid.Link);

            if (!QuicBindingAddSourceConnectionID(Path.Binding, SourceCid))
            {
                QuicLibraryReleaseBinding(Path.Binding);
                Path.Binding = null;
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            Connection.State.LocalAddressSet = true;
            QuicBindingGetLocalAddress(Path.Binding, ref Path.Route.LocalAddress);
            Connection.RemoteServerName = ServerName;
            ServerName = null;

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

        static void QuicConnShutdown(QUIC_CONNECTION Connection, QUIC_CONNECTION_SHUTDOWN_FLAGS Flags, ulong ErrorCode, bool ShutdownFromRegistration, bool ShutdownFromTransport)
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
            QUIC_RX_PACKET PacketsTail = (QUIC_RX_PACKET)Packets.Next;
            Packets.QueuedOnConnection = true;
            Packets.AssignedToConnection = true;
            while (PacketsTail != null)
            {
                (PacketsTail).QueuedOnConnection = true;
                (PacketsTail).AssignedToConnection = true;
                PacketsTail = (QUIC_RX_PACKET)PacketsTail.Next;
            }

            int QueueLimit = Math.Max(10, (int)Connection.Settings.ConnFlowControlWindow >> 10);

            bool QueueOperation;
            CxPlatDispatchLockAcquire(Connection.ReceiveQueueLock);
            if (Connection.ReceiveQueueCount >= QueueLimit)
            {
                QueueOperation = false;
            }
            else
            {
                Connection.ReceiveQueueTail = Packets;
                Connection.ReceiveQueueTail = PacketsTail;
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
                CxPlatRecvDataReturn((CXPLAT_RECV_DATA)Packets);
                return;
            }

            if (QueueOperation)
            {
                QUIC_OPERATION ConnOper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_RECV);
                if (ConnOper != null)
                {
                    QuicConnQueueOper(Connection, ConnOper);
                }
            }
        }

        static void QuicConnTransportError(QUIC_CONNECTION Connection, ulong ErrorCode)
        {
            QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL, ErrorCode, null);
        }

        static QUIC_CID_HASH_ENTRY QuicConnGenerateNewSourceCid(QUIC_CONNECTION Connection, bool IsInitial)
        {
            int TryCount = 0;
            QUIC_CID_HASH_ENTRY SourceCid;

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
                        Connection.CibirId + 2);

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

            SourceCid.CID.SequenceNumber = Connection.NextSourceCidSequenceNumber++;
            if (SourceCid.CID.SequenceNumber > 0)
            {
                SourceCid.CID.NeedsToSend = true;
                QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_NEW_CONNECTION_ID);
            }

            if (IsInitial)
            {
                SourceCid.CID.IsInitial = true;
                CxPlatListPushEntry(Connection.SourceCids, SourceCid.Link);
            }
            else
            {
                CXPLAT_SLIST_ENTRY Tail = Connection.SourceCids.Next;
                while (Tail != null)
                {
                    Tail = Tail.Next;
                }
                Tail = SourceCid.Link;
                SourceCid.Link.Next = null;
            }
            return SourceCid;
        }

        static ulong QuicConnProcessPeerTransportParameters(QUIC_CONNECTION Connection, bool FromResumptionTicket)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
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
                    (Connection->State.CompatibleVerNegotiationAttempted || Connection->PreviousQuicVersion != 0) &&
                    !(Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_VERSION_NEGOTIATION))
                {
                    //
                    // Client responded to a version negotiation packet, or compatible version negotiation,
                    // but server didn't send Version Info TP. Kill the connection.
                    //
                    QuicConnTransportError(Connection, QUIC_ERROR_VERSION_NEGOTIATION_ERROR);
                    Status = QUIC_STATUS_PROTOCOL_ERROR;
                    goto Error;
                }

                if (Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_STATELESS_RESET_TOKEN)
                {
                    CXPLAT_DBG_ASSERT(!CxPlatListIsEmpty(&Connection->DestCids));
                    CXPLAT_DBG_ASSERT(QuicConnIsClient(Connection));
                    QUIC_CID_LIST_ENTRY* DestCid =
                        CXPLAT_CONTAINING_RECORD(
                            Connection->DestCids.Flink,
                            QUIC_CID_LIST_ENTRY,
                            Link);
                    CxPlatCopyMemory(
                        DestCid->ResetToken,
                        Connection->PeerTransportParams.StatelessResetToken,
                        QUIC_STATELESS_RESET_TOKEN_LENGTH);
                    DestCid->CID.HasResetToken = TRUE;
                }

                if (Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_PREFERRED_ADDRESS)
                {
                    /*QuicTraceLogConnInfo(
                        PeerPreferredAddress,
                        Connection,
                        "Peer configured preferred address %!ADDR!",
                        CASTED_CLOG_BYTEARRAY(sizeof(Connection->PeerTransportParams.PreferredAddress), &Connection->PeerTransportParams.PreferredAddress));*/

                    //
                    // TODO - Implement preferred address feature.
                    //
                }

                if (Connection->Settings.GreaseQuicBitEnabled &&
                    (Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_GREASE_QUIC_BIT) > 0)
                {
                    //
                    // Endpoints that receive the grease_quic_bit transport parameter from
                    // a peer SHOULD set the QUIC Bit to an unpredictable value extension
                    // assigns specific meaning to the value of the bit.
                    //
                    uint8_t RandomValue;
                    (void)CxPlatRandom(sizeof(RandomValue), &RandomValue);
                    Connection->State.FixedBit = (RandomValue % 2);
                    Connection->Stats.GreaseBitNegotiated = TRUE;
                }

                if (Connection->Settings.ReliableResetEnabled)
                {
                    Connection->State.ReliableResetStreamNegotiated =
                        !!(Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_RELIABLE_RESET_ENABLED);

                    //
                    // Send event to app to indicate result of negotiation if app cares.
                    //
                    QUIC_CONNECTION_EVENT Event;
                    Event.Type = QUIC_CONNECTION_EVENT_RELIABLE_RESET_NEGOTIATED;
                    Event.RELIABLE_RESET_NEGOTIATED.IsNegotiated = Connection->State.ReliableResetStreamNegotiated;

                    QuicTraceLogConnVerbose(
                        IndicateReliableResetNegotiated,
                        Connection,
                        "Indicating QUIC_CONNECTION_EVENT_RELIABLE_RESET_NEGOTIATED [IsNegotiated=%hhu]",
                        Event.RELIABLE_RESET_NEGOTIATED.IsNegotiated);
                    QuicConnIndicateEvent(Connection, &Event);
                }

                if (Connection->Settings.OneWayDelayEnabled)
                {
                    Connection->State.TimestampSendNegotiated = // Peer wants to recv, so we can send
                        !!(Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED);
                    Connection->State.TimestampRecvNegotiated = // Peer wants to send, so we can recv
                        !!(Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED);

                    //
                    // Send event to app to indicate result of negotiation if app cares.
                    //
                    QUIC_CONNECTION_EVENT Event;
                    Event.Type = QUIC_CONNECTION_EVENT_ONE_WAY_DELAY_NEGOTIATED;
                    Event.ONE_WAY_DELAY_NEGOTIATED.SendNegotiated = Connection->State.TimestampSendNegotiated;
                    Event.ONE_WAY_DELAY_NEGOTIATED.ReceiveNegotiated = Connection->State.TimestampRecvNegotiated;

                    QuicTraceLogConnVerbose(
                        IndicateOneWayDelayNegotiated,
                        Connection,
                        "Indicating QUIC_CONNECTION_EVENT_ONE_WAY_DELAY_NEGOTIATED [Send=%hhu,Recv=%hhu]",
                        Event.ONE_WAY_DELAY_NEGOTIATED.SendNegotiated,
                        Event.ONE_WAY_DELAY_NEGOTIATED.ReceiveNegotiated);
                    QuicConnIndicateEvent(Connection, &Event);
                }

                //
                // Fully validate all exchanged connection IDs.
                //
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

            Connection->Send.PeerMaxData =
                Connection->PeerTransportParams.InitialMaxData;

            QuicStreamSetInitializeTransportParameters(
                &Connection->Streams,
                Connection->PeerTransportParams.InitialMaxBidiStreams,
                Connection->PeerTransportParams.InitialMaxUniStreams,
                !FromResumptionTicket);

            QuicDatagramOnSendStateChanged(&Connection->Datagram);

            if (Connection->State.Started)
            {
                if (Connection->State.Disable1RttEncrytion &&
                    Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_DISABLE_1RTT_ENCRYPTION)
                {
                    QuicTraceLogConnInfo(
                        NegotiatedDisable1RttEncryption,
                        Connection,
                        "Negotiated Disable 1-RTT Encryption");
                }
                else
                {
                    Connection->State.Disable1RttEncrytion = FALSE;
                }
            }

            return QUIC_STATUS_SUCCESS;

        Error:
            //
            // Errors from Version Negotiation Extension parsing are treated differently
            // so Incompatible Version Negotiation can be done.
            //
            if (Status == QUIC_STATUS_SUCCESS)
            {
                QuicConnTransportError(Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                Status = QUIC_STATUS_PROTOCOL_ERROR;
            }
            return Status;
        }

        static ulong QuicConnProcessPeerVersionNegotiationTP(QUIC_CONNECTION Connection)
        {
            ulong Status;
            if (QuicConnIsServer(Connection))
            {
                int SupportedVersionsLength = 0;
                uint[] SupportedVersions = null;

                SupportedVersionsLength = DefaultSupportedVersionsList.Length;
                SupportedVersions = DefaultSupportedVersionsList;

                int CurrentVersionIndex = 0;
                for (; CurrentVersionIndex < SupportedVersionsLength; ++CurrentVersionIndex)
                {
                    if (Connection.Stats.QuicVersion == SupportedVersions[CurrentVersionIndex])
                    {
                        break;
                    }
                }

                if (CurrentVersionIndex == SupportedVersionsLength)
                {
                    NetLog.Assert(false, "Incompatible Version Negotation should happen in binding layer");
                    return QUIC_STATUS_VER_NEG_ERROR;
                }

                QUIC_VERSION_INFORMATION_V1 ClientVI = new QUIC_VERSION_INFORMATION_V1();
                Status = QuicVersionNegotiationExtParseVersionInfo(
                        Connection,
                        Connection.PeerTransportParams.VersionInfo.AsSpan().Slice(0, Connection.PeerTransportParams.VersionInfoLength),
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
                        Connection.PeerTransportParams.VersionInfo.AsSpan().Slice(0, Connection.PeerTransportParams.VersionInfoLength),
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

        static ulong QuicConnGetAckDelay(QUIC_CONNECTION Connection)
        {
            if (Connection.Settings.MaxAckDelayMs > 0 && (MsQuicLib.ExecutionConfig == null || 
                Connection.Settings.MaxAckDelayMs > MsQuicLib.ExecutionConfig.PollingIdleTimeoutUs))
            {
                return (ulong) Connection.Settings.MaxAckDelayMs + (ulong)MsQuicLib.TimerResolutionMs;
            }
            return (ulong) Connection.Settings.MaxAckDelayMs;
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
            QUIC_RX_PACKET ReceiveQueue;

            CxPlatDispatchLockAcquire(Connection.ReceiveQueueLock);
            ReceiveQueue = Connection.ReceiveQueue;
            if (Connection.ReceiveQueueCount > QUIC_MAX_RECEIVE_FLUSH_COUNT)
            {
                FlushedAll = false;
                Connection.ReceiveQueueCount -= QUIC_MAX_RECEIVE_FLUSH_COUNT;
                QUIC_RX_PACKET Tail = Connection.ReceiveQueue;
                ReceiveQueueCount = 0;
                ReceiveQueueByteCount = 0;
                while (++ReceiveQueueCount < QUIC_MAX_RECEIVE_FLUSH_COUNT)
                {
                    ReceiveQueueByteCount += Tail.BufferLength;
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
                Connection.ReceiveQueue = null;
                Connection.ReceiveQueueTail = Connection.ReceiveQueue;
            }
            CxPlatDispatchLockRelease(Connection.ReceiveQueueLock);
            QuicConnRecvDatagrams(Connection, ReceiveQueue, ReceiveQueueCount, ReceiveQueueByteCount, false);
            return FlushedAll;
        }

        static void QuicConnRecvDatagrams(QUIC_CONNECTION Connection,QUIC_RX_PACKET Packets,int PacketChainCount,int PacketChainByteCount,bool IsDeferred)
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

            if (IsDeferred)
            {
                
            }
            else
            {
                
            }

            int BatchCount = 0;
            QUIC_RX_PACKET[] Batch = new QUIC_RX_PACKET[QUIC_MAX_CRYPTO_BATCH_COUNT];
            byte[] Cipher = new byte[CXPLAT_HP_SAMPLE_LENGTH * QUIC_MAX_CRYPTO_BATCH_COUNT];
            QUIC_PATH CurrentPath = null;

            QUIC_RX_PACKET Packet;
            while ((Packet = Packets) != null)
            {
                NetLog.Assert(Packet.Allocated > 0);
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
                            &RecvState);
                        BatchCount = 0;
                    }
                    CurrentPath = DatagramPath;
                }

                if (!IsDeferred)
                {
                    Connection->Stats.Recv.TotalBytes += Packet->BufferLength;
                    QuicConnLogInFlowStats(Connection);

                    if (!CurrentPath->IsPeerValidated)
                    {
                        QuicPathIncrementAllowance(
                            Connection,
                            CurrentPath,
                            QUIC_AMPLIFICATION_RATIO * Packet->BufferLength);
                    }
                }

                do
                {
                    CXPLAT_DBG_ASSERT(BatchCount < QUIC_MAX_CRYPTO_BATCH_COUNT);
                    CXPLAT_DBG_ASSERT(Packet->Allocated);
                    Connection->Stats.Recv.TotalPackets++;

                    if (!Packet->ValidatedHeaderInv)
                    {
                        //
                        // Only calculate the buffer length from the available UDP
                        // payload length if the long header hasn't already been
                        // validated (which indicates the actual length);
                        //
                        Packet->AvailBufferLength =
                            Packet->BufferLength - (uint16_t)(Packet->AvailBuffer - Packet->Buffer);
                    }

                    if (!QuicConnRecvHeader(
                            Connection,
                            Packet,
                            Cipher + BatchCount * CXPLAT_HP_SAMPLE_LENGTH))
                    {
                        if (Packet->ReleaseDeferred)
                        {
                            Connection->Stats.Recv.TotalPackets--; // Don't count the packet right now.
                        }
                        else if (!Packet->IsShortHeader && Packet->ValidatedHeaderVer)
                        {
                            goto NextPacket;
                        }
                        break;
                    }

                    if (!Packet->IsShortHeader && BatchCount != 0)
                    {
                        //
                        // We already had some batched short header packets and then
                        // encountered a long header packet. Finish off the short
                        // headers first and then continue with the current packet.
                        //
                        QuicConnRecvDatagramBatch(
                            Connection,
                            CurrentPath,
                            BatchCount,
                            Batch,
                            Cipher,
                            &RecvState);
                        CxPlatMoveMemory(
                            Cipher + BatchCount * CXPLAT_HP_SAMPLE_LENGTH,
                            Cipher,
                            CXPLAT_HP_SAMPLE_LENGTH);
                        BatchCount = 0;
                    }

                    Batch[BatchCount++] = Packet;
                    if (Packet->IsShortHeader && BatchCount < QUIC_MAX_CRYPTO_BATCH_COUNT)
                    {
                        break;
                    }

                    QuicConnRecvDatagramBatch(
                        Connection,
                        CurrentPath,
                        BatchCount,
                        Batch,
                        Cipher,
                        &RecvState);
                    BatchCount = 0;

                    if (Packet->IsShortHeader)
                    {
                        break; // Short header packets aren't followed by additional packets.
                    }

                //
                // Move to the next QUIC packet (if available) and reset the packet
                // state.
                //

                NextPacket:

                    Packet->AvailBuffer += Packet->AvailBufferLength;

                    Packet->ValidatedHeaderInv = FALSE;
                    Packet->ValidatedHeaderVer = FALSE;
                    Packet->ValidToken = FALSE;
                    Packet->PacketNumberSet = FALSE;
                    Packet->EncryptedWith0Rtt = FALSE;
                    Packet->ReleaseDeferred = FALSE;
                    Packet->CompletelyValid = FALSE;
                    Packet->NewLargestPacketNumber = FALSE;
                    Packet->HasNonProbingFrame = FALSE;

                } while (Packet->AvailBuffer - Packet->Buffer < Packet->BufferLength);

            Drop:

                if (!Packet->ReleaseDeferred)
                {
                    *ReleaseChainTail = Packet;
                    ReleaseChainTail = (QUIC_RX_PACKET**)&Packet->Next;
                    Packet->QueuedOnConnection = FALSE;
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
                                &RecvState);
                            BatchCount = 0;
                        }
                        CxPlatRecvDataReturn((CXPLAT_RECV_DATA*)ReleaseChain);
                        ReleaseChain = NULL;
                        ReleaseChainTail = &ReleaseChain;
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
                    &RecvState);
                BatchCount = 0; // cppcheck-suppress unreadVariable; NOLINT
            }

            if (Connection->State.DelayedApplicationError && Connection->CloseStatus == 0)
            {
                //
                // We received transport APPLICATION_ERROR, but didn't receive the expected
                // CONNECTION_ERROR frame, so close the connection with originally postponed
                // APPLICATION_ERROR.
                //
                QuicConnTryClose(
                    Connection,
                    QUIC_CLOSE_REMOTE | QUIC_CLOSE_SEND_NOTIFICATION,
                    QUIC_ERROR_APPLICATION_ERROR,
                    NULL,
                    (uint16_t)0);
            }

            if (RecvState.ResetIdleTimeout)
            {
                QuicConnResetIdleTimeout(Connection);
            }

            if (ReleaseChain != NULL)
            {
                CxPlatRecvDataReturn((CXPLAT_RECV_DATA*)ReleaseChain);
            }

            if (QuicConnIsServer(Connection) &&
                Connection->Stats.Recv.ValidPackets == 0 &&
                !Connection->State.ClosedLocally)
            {
                //
                // The packet(s) that created this connection weren't valid. We should
                // immediately throw away the connection.
                //
                QuicTraceLogConnWarning(
                    InvalidInitialPackets,
                    Connection,
                    "Aborting connection with invalid initial packets");
                QuicConnSilentlyAbort(Connection);
            }

            //
            // Any new paths created here were created before packet validation. Now
            // remove any non-active paths that didn't get any valid packets.
            // NB: Traversing the array backwards is simpler and more efficient here due
            // to the array shifting that happens in QuicPathRemove.
            //
            for (uint8_t i = Connection->PathsCount - 1; i > 0; --i)
            {
                if (!Connection->Paths[i].GotValidPacket)
                {
                    QuicTraceLogConnInfo(
                        PathDiscarded,
                        Connection,
                        "Removing invalid path[%hhu]",
                        Connection->Paths[i].ID);
                    QuicPathRemove(Connection, i);
                }
            }

            if (!Connection->State.UpdateWorker && Connection->State.Connected &&
                !Connection->State.ShutdownComplete && RecvState.UpdatePartitionId)
            {
                CXPLAT_DBG_ASSERT(Connection->Registration);
                CXPLAT_DBG_ASSERT(!Connection->Registration->NoPartitioning);
                CXPLAT_DBG_ASSERT(RecvState.PartitionIndex != QuicPartitionIdGetIndex(Connection->PartitionID));
                Connection->PartitionID = QuicPartitionIdCreate(RecvState.PartitionIndex);
                QuicConnGenerateNewSourceCids(Connection, TRUE);
                Connection->State.UpdateWorker = TRUE;
            }
        }

        static void QuicConnRecvDatagramBatch(QUIC_CONNECTION Connection, QUIC_PATH Path, int BatchCount, QUIC_RX_PACKET[] Packets, byte[] Cipher, QUIC_RECEIVE_PROCESSING_STATE RecvState)
        {
            byte[] HpMask = new byte[CXPLAT_HP_SAMPLE_LENGTH * QUIC_MAX_CRYPTO_BATCH_COUNT];

            NetLog.Assert(BatchCount > 0 && BatchCount <= QUIC_MAX_CRYPTO_BATCH_COUNT);
            QUIC_RX_PACKET Packet = Packets[0];

            if (Connection.Crypto.TlsState.ReadKeys[(int)Packet.KeyType] == null) {
                QuicPacketLogDrop(Connection, Packet, "Key no longer accepted (batch)");
                return;
            }

            if (Packet.Encrypted && Connection.State.HeaderProtectionEnabled)
            {
                if (QUIC_FAILED(CxPlatHpComputeMask(Connection.Crypto.TlsState.ReadKeys[(int)Packet.KeyType].HeaderKey, BatchCount, Cipher, HpMask)))
                {
                    QuicPacketLogDrop(Connection, Packet, "Failed to compute HP mask");
                    return;
                }
            }
            else
            {
                Array.Clear(HpMask, 0, BatchCount * CXPLAT_HP_SAMPLE_LENGTH);
            }

            for (int i = 0; i < BatchCount; ++i)
            {
                NetLog.Assert(Packets[i].Allocated > 0);
                CXPLAT_ECN_TYPE ECN = CXPLAT_ECN_FROM_TOS(Packets[i].TypeOfService);
                Packet = Packets[i];
                NetLog.Assert(Packet.PacketId != 0);

                if (!QuicConnRecvPrepareDecrypt(Connection, Packet, HpMask.AsSpan().Slice(i * CXPLAT_HP_SAMPLE_LENGTH)) ||
                    !QuicConnRecvDecryptAndAuthenticate(Connection, Path, Packet))
                {
                    if (Connection->State.CompatibleVerNegotiationAttempted &&
                        !Connection->State.CompatibleVerNegotiationCompleted)
                    {
                        //
                        // The packet which initiated compatible version negotation failed
                        // decryption, so undo the version change.
                        //
                        Connection->Stats.QuicVersion = Connection->OriginalQuicVersion;
                        Connection->State.CompatibleVerNegotiationAttempted = FALSE;
                    }
                }
                else if (QuicConnRecvFrames(Connection, Path, Packet, ECN))
                {

                    QuicConnRecvPostProcessing(Connection, &Path, Packet);
                    RecvState->ResetIdleTimeout |= Packet->CompletelyValid;

                    if (Connection->Registration != NULL && !Connection->Registration->NoPartitioning &&
                        Path->IsActive && !Path->PartitionUpdated && Packet->CompletelyValid &&
                        (Packets[i]->PartitionIndex % MsQuicLib.PartitionCount) != RecvState->PartitionIndex)
                    {
                        RecvState->PartitionIndex = Packets[i]->PartitionIndex % MsQuicLib.PartitionCount;
                        RecvState->UpdatePartitionId = TRUE;
                        Path->PartitionUpdated = TRUE;
                    }

                    if (Packet->IsShortHeader && Packet->NewLargestPacketNumber)
                    {

                        if (QuicConnIsServer(Connection))
                        {
                            Path->SpinBit = Packet->SH->SpinBit;
                        }
                        else
                        {
                            Path->SpinBit = !Packet->SH->SpinBit;
                        }
                    }
                }
            }
        }

        static bool QuicConnRecvPrepareDecrypt(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packet, ReadOnlySpan<byte> HpMask)
        {
            NetLog.Assert(Packet.ValidatedHeaderInv);
            NetLog.Assert(Packet.ValidatedHeaderVer);
            NetLog.Assert(Packet.HeaderLength <= Packet.AvailBufferLength);
            NetLog.Assert(Packet.PayloadLength <= Packet.AvailBufferLength);
            NetLog.Assert(Packet.HeaderLength + Packet.PayloadLength <= Packet.AvailBufferLength);

            int CompressedPacketNumberLength = 0;
            if (Packet.IsShortHeader)
            {
                Packet.AvailBuffer[0] = (byte)(Packet.AvailBuffer[0] ^ (HpMask[0] & 0x1f));
                CompressedPacketNumberLength = Packet.SH.PnLength + 1;
            }
            else
            {
                Packet.AvailBuffer[0] = (byte)(Packet.AvailBuffer[0] ^ (HpMask[0] & 0x0f));
                CompressedPacketNumberLength = Packet.LH.PnLength + 1;
            }

            NetLog.Assert(CompressedPacketNumberLength >= 1 && CompressedPacketNumberLength <= 4);
            NetLog.Assert(Packet.HeaderLength + CompressedPacketNumberLength <= Packet.AvailBufferLength);

            for (int i = 0; i < CompressedPacketNumberLength; i++)
            {
                Packet.AvailBuffer[Packet.HeaderLength + i] ^= HpMask[1 + i];
            }

            ulong CompressedPacketNumber = 0;
            QuicPktNumDecode(CompressedPacketNumberLength, Packet.AvailBuffer.AsSpan().Slice(Packet.HeaderLength), CompressedPacketNumber);

            Packet.HeaderLength += CompressedPacketNumberLength;
            Packet.PayloadLength -= CompressedPacketNumberLength;

            QUIC_ENCRYPT_LEVEL EncryptLevel = QuicKeyTypeToEncryptLevel(Packet.KeyType);
            Packet.PacketNumber = QuicPktNumDecompress(Connection.Packets[(int)EncryptLevel].NextRecvPacketNumber,
                    CompressedPacketNumber,
                    CompressedPacketNumberLength);
            Packet.PacketNumberSet = true;

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
            if (Packet.IsShortHeader && EncryptLevel == QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT && Packet.SH.KeyPhase != PacketSpace.CurrentKeyPhase)
            {
                if (Packet.PacketNumber < PacketSpace.ReadKeyPhaseStartPacketNumber)
                {
                    NetLog.Assert(Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD] != null);
                    NetLog.Assert(Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD] != null);
                    Packet.KeyType = QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_OLD;
                }
                else
                {
                    ulong Status = QuicCryptoGenerateNewKeys(Connection);
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
            NetLog.Assert(Packet.AvailBufferLength >= Packet.HeaderLength + Packet.PayloadLength);
            ReadOnlySpan<byte> Payload = Packet.AvailBuffer.AsSpan().Slice(Packet.HeaderLength);
            
            bool CanCheckForStatelessReset = false;
            byte[] PacketResetToken = new byte[QUIC_STATELESS_RESET_TOKEN_LENGTH];
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
                if (QUIC_FAILED(CxPlatDecrypt(Connection.Crypto.TlsState.ReadKeys[(int)Packet.KeyType].PacketKey,
                        Iv,
                        Packet.HeaderLength,   // HeaderLength
                        Packet.AvailBuffer,    // Header
                        Payload.Slice(0, Packet.PayloadLength))))
                {
                    if (CanCheckForStatelessReset)
                    {
                        for (CXPLAT_LIST_ENTRY Entry = Connection.DestCids.Flink; Entry != Connection.DestCids; Entry = Entry.Flink)
                        {
                            QUIC_CID_LIST_ENTRY DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_LIST_ENTRY>(Entry);

                            if (DestCid.CID.HasResetToken && !DestCid.CID.Retired &&
                                orBufferEqual(DestCid.ResetToken, PacketResetToken, QUIC_STATELESS_RESET_TOKEN_LENGTH))
                            {
                                QuicConnCloseLocally(Connection, QUIC_CLOSE_INTERNAL_SILENT | QUIC_CLOSE_QUIC_STATUS, QUIC_STATUS_ABORTED, null);
                                return false;
                            }
                        }
                    }

                    Connection.Stats.Recv.DecryptionFailures++;
                    QuicPacketLogDrop(Connection, Packet, "Decryption failure");
                    QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_PKTS_DECRYPTION_FAIL);
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
                    if (!Connection.State.Connected &&
                        QuicConnIsClient(Connection) &&
                        !QuicConnUpdateDestCid(Connection, Packet))
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
                if (Packet.KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT_NEW)
                {
                    QuicCryptoUpdateKeyPhase(Connection, false);
                    PacketSpace.ReadKeyPhaseStartPacketNumber = Packet.PacketNumber;
                }
                else if (Packet.KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT &&
                    Packet.SH.KeyPhase == PacketSpace.CurrentKeyPhase &&
                    Packet.PacketNumber < PacketSpace.ReadKeyPhaseStartPacketNumber)
                {
                    PacketSpace.ReadKeyPhaseStartPacketNumber = Packet.PacketNumber;
                }
            }

            if (Packet.KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_HANDSHAKE && QuicConnIsServer(Connection))
            {
                QuicCryptoDiscardKeys(Connection.Crypto,  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL);
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

            QUIC_CID_LIST_ENTRY DestCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_LIST_ENTRY>(Connection.DestCids.Flink);
            NetLog.Assert(Connection.Paths[0].DestCid == DestCid);

            if (Packet.SourceCidLen != DestCid.CID.Length || !orBufferEqual(Packet.SourceCid, DestCid.CID.Data, DestCid.CID.Length))
            {
                if (Packet.SourceCidLen <= DestCid.CID.Length)
                {
                    DestCid.CID.IsInitial = false;
                    DestCid.CID.Length = Packet.SourceCidLen;
                    Array.Copy(Packet.SourceCid, DestCid.CID.Data, DestCid.CID.Length);
                }
                else
                {
                    CxPlatListEntryRemove(DestCid.Link);
                    DestCid = QuicCidNewDestination(Packet.SourceCidLen, Packet.SourceCid);
                    if (DestCid == null)
                    {
                        Connection.DestCidCount--;
                        Connection.Paths[0].DestCid = null;
                        QuicConnFatalError(Connection, QUIC_STATUS_OUT_OF_MEMORY, "Out of memory");
                        return false;
                    }

                    Connection.Paths[0].DestCid = DestCid;
                    DestCid.CID.UsedLocally = true;
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
                Event.Type =  QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_PEER;
                Event.SHUTDOWN_INITIATED_BY_PEER.ErrorCode = Connection.CloseErrorCode;
            }
            else
            {
                Event.Type =  QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_TRANSPORT;
                Event.SHUTDOWN_INITIATED_BY_TRANSPORT.Status = Connection.CloseStatus;
                Event.SHUTDOWN_INITIATED_BY_TRANSPORT.ErrorCode = Connection.CloseErrorCode;
            }
            QuicConnIndicateEvent(Connection, Event);
        }



    }

}
