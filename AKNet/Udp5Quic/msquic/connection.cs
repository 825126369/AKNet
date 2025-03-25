using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime;
using System.Threading;
using static AKNet.Udp5Quic.Common.QUIC_BINDING;
using static AKNet.Udp5Quic.Common.QUIC_CONN_STATS;
using AKNet.Udp4LinuxTcp.Common;
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

        //
        // Indicates whether packet number encryption is enabled or not for the
        // connection.
        //
        public bool HeaderProtectionEnabled; // TODO - Remove since it's not used

        //
        // Indicates that 1-RTT encryption has been configured/negotiated to be
        // disabled.
        //
        public bool Disable1RttEncrytion;

        //
        // Indicates whether the current 'owner' of the connection is internal
        // or external. Client connections are always externally owned. Server
        // connections are internally owned until they are indicated to the
        // appliciation, via the listener callback.
        //
        public bool ExternalOwner;

        //
        // Indicate the connection is currently in the registration's list of
        // connections and needs to be removed.
        //
        public bool Registered;

        //
        // This flag indicates the client has gotten response from the server.
        // The response could either be a Retry or server Initial packet. Once
        // this happens, the client must not accept any received Retry packets.
        //
        public bool GotFirstServerResponse;

        //
        // This flag indicates the Retry packet was used during the handshake.
        //
        public bool HandshakeUsedRetryPacket;

        //
        // We have confirmed that the peer has completed the handshake.
        //
        public bool HandshakeConfirmed;

        //
        // The (server side) connection has been accepted by a listener.
        //
        public bool ListenerAccepted;

        //
        // Indicates whether the local address has been set. It can be set either
        // via the QUIC_PARAM_CONN_LOCAL_ADDRESS parameter by the application, or
        // via UDP binding creation during the connection start phase.
        //
        public bool LocalAddressSet;

        //
        // Indicates whether the remote address has been set. It can be set either
        // via the QUIC_PARAM_CONN_REMOTE_ADDRESS parameter by the application,
        // before starting the connection, or via name resolution during the
        // connection start phase.
        //
        public bool RemoteAddressSet;

        //
        // Indicates the peer transport parameters variable has been set.
        //
        public bool PeerTransportParameterValid;

        //
        // Indicates the connection needs to queue onto a new worker thread.
        //
        public bool UpdateWorker;

        //
        // The peer didn't acknowledge the shutdown.
        //
        public bool ShutdownCompleteTimedOut;

        //
        // The connection is shutdown and the completion for it needs to be run.
        //
        public bool ProcessShutdownComplete;

        //
        // Indicates whether this connection shares bindings with others.
        //
        public bool ShareBinding;

        //
        // Indicates the TestTransportParameter variable has been set by the app.
        //
        public bool TestTransportParameterSet;

        //
        // Indicates the connection is using the round robin stream scheduling
        // scheme.
        //
        public bool UseRoundRobinStreamScheduling;

        //
        // Indicates that this connection has resumption enabled and needs to
        // keep the TLS state and transport parameters until it is done sending
        // resumption tickets.
        //
        public bool ResumptionEnabled;

        //
        // When true, this indicates that the connection is currently executing
        // an API call inline (from a reentrant call on a callback).
        //
        public bool InlineApiExecution;

        //
        // True when a server attempts Compatible Version Negotiation
        public bool CompatibleVerNegotiationAttempted;

        //
        // True once a client connection has completed a compatible version
        // negotiation, and false otherwise. Used to prevent packets with invalid
        // version fields from being accepted.
        //
        public bool CompatibleVerNegotiationCompleted;

        //
        // When true, this indicates the app has set the local interface index.
        //
        public bool LocalInterfaceSet;

        //
        // This value of the fixed bit on send packets.
        //
        public bool FixedBit;

        //
        // Indicates that the peer accepts RELIABLE_RESET kind of frames, in addition to RESET_STREAM frames.
        //
        public bool ReliableResetStreamNegotiated;

        //
        // Sending timestamps has been negotiated.
        //
        public bool TimestampSendNegotiated;

        //
        // Receiving timestamps has been negotiated.
        //
        public bool TimestampRecvNegotiated;

        //
        // Indicates we received APPLICATION_ERROR transport error and are checking also
        // later packets in case they contain CONNECTION_CLOSE frame with application-layer error.
        //
        public bool DelayedApplicationError;
        //
        // The calling app is being verified (app or driver verifier).
        //
        public bool IsVerifying;
    }

    internal class QUIC_CONN_STATS
    {
        public ulong CorrelationId;
        public uint VersionNegotiation;
        public uint StatelessRetry;
        public uint ResumptionAttempted;
        public uint ResumptionSucceeded;
        public uint GreaseBitNegotiated;
        public uint EncryptionOffloaded;

        public uint QuicVersion;
        public class Timing
        {
            public ulong Start;
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
        public Schedule_Class Schedule;

        public class Handshake
        {
            public uint ClientFlight1Bytes;    // Sum of TLS payloads
            public uint ServerFlight1Bytes;    // Sum of TLS payloads
            public uint ClientFlight2Bytes;    // Sum of TLS payloads
            public byte HandshakeHopLimitTTL;   // TTL value in the initial packet of the handshake.
        }

        public class Send
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

        public class Recv
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

        public class Misc
        {
            public uint KeyUpdateCount;        // Count of key updates completed.
            public uint DestCidUpdateCount;    // Number of times the destination CID changed.
        }

    }

    internal class QUIC_CONNECTION : QUIC_HANDLE
    {
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

        public byte[] ServerID = new byte[MSQuicFunc.QUIC_MAX_CID_SID_LENGTH];
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
        public byte CibirId[2 + QUIC_MAX_CIBIR_LENGTH];
        public long[] ExpirationTimes = new long[(int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_COUNT];
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
        public long CloseStatus;
        public ulong CloseErrorCode;
        public byte[] CloseReasonPhrase;

        public string RemoteServerName;
        public QUIC_REMOTE_HASH_ENTRY RemoteHashEntry;
        public QUIC_TRANSPORT_PARAMETERS PeerTransportParams;
        public QUIC_RANGE DecodedAckRanges;
        public QUIC_STREAM_SET Streams;
        public QUIC_CONGESTION_CONTROL CongestionControl;

        //
        // Manages all the information for outstanding sent packets.
        //
        QUIC_LOSS_DETECTION LossDetection;

        //
        // Per-encryption level packet space information.
        //
        QUIC_PACKET_SPACE* Packets[QUIC_ENCRYPT_LEVEL_COUNT];

        //
        // Manages the stream of cryptographic TLS data sent and received.
        //
        QUIC_CRYPTO Crypto;

        //
        // The send manager for the connection.
        //
        public QUIC_SEND Send;
        public QUIC_SEND_BUFFER SendBuffer;

        //
        // Manages datagrams for the connection.
        //
        QUIC_DATAGRAM Datagram;

        //
        // The handler for the API client's callbacks.
        //
        public QUIC_CONNECTION_CALLBACK ClientCallbackHandler;

        //
        // (Server-only) Transport parameters used during handshake.
        // Only non-null when resumption is enabled.
        //
        public QUIC_TRANSPORT_PARAMETERS HandshakeTP;
        public QUIC_CONN_STATS Stats;
        QUIC_PRIVATE_TRANSPORT_PARAMETER TestTransportParameter;
        QUIC_TLS_SECRETS* TlsSecrets;
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
            TimerLink = new CXPLAT_LIST_ENTRY_QUIC_CONNECTION(this);
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

        static bool QuicConnIsClosed(QUIC_CONNECTION Connection)
        {
            return Connection.State.ClosedLocally || Connection.State.ClosedRemotely;
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


        static void QuicConnLogOutFlowStats(QUIC_CONNECTION Connection)
        {
            if (!QuicTraceEventEnabled(QuicEventId.ConnOutFlowStats))
            {
                return;
            }

            Connection.CongestionControl.QuicCongestionControlLogOutFlowStatus(Connection.CongestionControl);
            ulong FcAvailable, SendWindow;
            QuicStreamSetGetFlowControlSummary(Connection.Streams, ref FcAvailable, ref SendWindow);
        }

        static long QuicConnAlloc(QUIC_REGISTRATION Registration,QUIC_WORKER Worker,QUIC_RX_PACKET Packet, out QUIC_CONNECTION NewConnection)
{
            bool IsServer = Packet != null;
            NewConnection = null;
            long Status;
        
        ushort PartitionIndex = IsServer ? Packet.PartitionIndex : QuicLibraryGetCurrentPartition();
        ushort PartitionId = QuicPartitionIdCreate(PartitionIndex);
        NetLog.Assert(PartitionIndex == QuicPartitionIdGetIndex(PartitionId));

        QUIC_CONNECTION Connection = CxPlatPoolAlloc(QuicLibraryGetPerProc().ConnectionPool);
            if (Connection == null)
            {
                QuicTraceEvent(QuicEventId.AllocFailure, "Allocation of '%s' failed. (%llu bytes)", "connection");
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

#if DEBUG
        Interlocked.Increment(ref MsQuicLib.ConnectionCount);
#endif
        QuicPerfCounterIncrement(QUIC_PERF_COUNTER_CONN_CREATED);
        QuicPerfCounterIncrement(QUIC_PERF_COUNTER_CONN_ACTIVE);

    Connection->Stats.CorrelationId =
        InterlockedIncrement64((int64_t*)&MsQuicLib.ConnectionCorrelationId) - 1;
    QuicTraceEvent(
        ConnCreated,
        "[conn][%p] Created, IsServer=%hhu, CorrelationId=%llu",
        Connection,
        IsServer,
        Connection->Stats.CorrelationId);

    Connection->RefCount = 1;
#if DEBUG
    Connection->RefTypeCount[QUIC_CONN_REF_HANDLE_OWNER] = 1;
#endif
    Connection->PartitionID = PartitionId;
    Connection->State.Allocated = TRUE;
    Connection->State.ShareBinding = IsServer;
    Connection->State.FixedBit = TRUE;
    Connection->Stats.Timing.Start = CxPlatTimeUs64();
    Connection->SourceCidLimit = QUIC_ACTIVE_CONNECTION_ID_LIMIT;
    Connection->AckDelayExponent = QUIC_ACK_DELAY_EXPONENT;
    Connection->PacketTolerance = QUIC_MIN_ACK_SEND_NUMBER;
    Connection->PeerPacketTolerance = QUIC_MIN_ACK_SEND_NUMBER;
    Connection->ReorderingThreshold = QUIC_MIN_REORDERING_THRESHOLD;
    Connection->PeerReorderingThreshold = QUIC_MIN_REORDERING_THRESHOLD;
    Connection->PeerTransportParams.AckDelayExponent = QUIC_TP_ACK_DELAY_EXPONENT_DEFAULT;
    Connection->ReceiveQueueTail = &Connection->ReceiveQueue;
    QuicSettingsCopy(&Connection->Settings, &MsQuicLib.Settings);
    Connection->Settings.IsSetFlags = 0; // Just grab the global values, not IsSet flags.
    CxPlatDispatchLockInitialize(&Connection->ReceiveQueueLock);
    CxPlatListInitializeHead(&Connection->DestCids);
    QuicStreamSetInitialize(&Connection->Streams);
    QuicSendBufferInitialize(&Connection->SendBuffer);
    QuicOperationQueueInitialize(&Connection->OperQ);
    QuicSendInitialize(&Connection->Send, &Connection->Settings);
    QuicCongestionControlInitialize(&Connection->CongestionControl, &Connection->Settings);
    QuicLossDetectionInitialize(&Connection->LossDetection);
    QuicDatagramInitialize(&Connection->Datagram);
    QuicRangeInitialize(
        QUIC_MAX_RANGE_DECODE_ACKS,
        &Connection->DecodedAckRanges);

    for (uint32_t i = 0; i<ARRAYSIZE(Connection->Packets); i++) {
        Status =
            QuicPacketSpaceInitialize(
                Connection,
                (QUIC_ENCRYPT_LEVEL) i,
                &Connection->Packets[i]);
        if (QUIC_FAILED(Status)) {
            goto Error;
        }
    }

    QUIC_PATH* Path = &Connection->Paths[0];
QuicPathInitialize(Connection, Path);
Path->IsActive = TRUE;
Connection->PathsCount = 1;

Connection->EarliestExpirationTime = UINT64_MAX;
for (QUIC_CONN_TIMER_TYPE Type = 0; Type < QUIC_CONN_TIMER_COUNT; ++Type)
{
    Connection->ExpirationTimes[Type] = UINT64_MAX;
}

if (IsServer)
{

    Connection->Type = QUIC_HANDLE_TYPE_CONNECTION_SERVER;
    if (MsQuicLib.Settings.LoadBalancingMode == QUIC_LOAD_BALANCING_SERVER_ID_IP)
    {
        CxPlatRandom(1, Connection->ServerID); // Randomize the first byte.
        if (QuicAddrGetFamily(&Packet->Route->LocalAddress) == QUIC_ADDRESS_FAMILY_INET)
        {
            CxPlatCopyMemory(
                Connection->ServerID + 1,
                &Packet->Route->LocalAddress.Ipv4.sin_addr,
                4);
        }
        else
        {
            CxPlatCopyMemory(
                Connection->ServerID + 1,
                ((uint8_t*)&Packet->Route->LocalAddress.Ipv6.sin6_addr) + 12,
                4);
        }
    }
    else if (MsQuicLib.Settings.LoadBalancingMode == QUIC_LOAD_BALANCING_SERVER_ID_FIXED)
    {
        CxPlatRandom(1, Connection->ServerID); // Randomize the first byte.
        CxPlatCopyMemory(
            Connection->ServerID + 1,
            &MsQuicLib.Settings.FixedServerID,
            sizeof(MsQuicLib.Settings.FixedServerID));
    }

    Connection->Stats.QuicVersion = Packet->Invariant->LONG_HDR.Version;
    QuicConnOnQuicVersionSet(Connection);
    QuicCopyRouteInfo(&Path->Route, Packet->Route);
    Connection->State.LocalAddressSet = TRUE;
    Connection->State.RemoteAddressSet = TRUE;

    QuicTraceEvent(
        ConnLocalAddrAdded,
        "[conn][%p] New Local IP: %!ADDR!",
        Connection,
        CASTED_CLOG_BYTEARRAY(sizeof(Path->Route.LocalAddress), &Path->Route.LocalAddress));

    QuicTraceEvent(
        ConnRemoteAddrAdded,
        "[conn][%p] New Remote IP: %!ADDR!",
        Connection,
        CASTED_CLOG_BYTEARRAY(sizeof(Path->Route.RemoteAddress), &Path->Route.RemoteAddress));

    Path->DestCid =
        QuicCidNewDestination(Packet->SourceCidLen, Packet->SourceCid);
    if (Path->DestCid == NULL)
    {
        Status = QUIC_STATUS_OUT_OF_MEMORY;
        goto Error;
    }
    QUIC_CID_SET_PATH(Connection, Path->DestCid, Path);
    Path->DestCid->CID.UsedLocally = TRUE;
    CxPlatListInsertTail(&Connection->DestCids, &Path->DestCid->Link);
    QuicTraceEvent(
        ConnDestCidAdded,
        "[conn][%p] (SeqNum=%llu) New Destination CID: %!CID!",
        Connection,
        Path->DestCid->CID.SequenceNumber,
        CASTED_CLOG_BYTEARRAY(Path->DestCid->CID.Length, Path->DestCid->CID.Data));

    QUIC_CID_HASH_ENTRY* SourceCid =
        QuicCidNewSource(Connection, Packet->DestCidLen, Packet->DestCid);
    if (SourceCid == NULL)
    {
        Status = QUIC_STATUS_OUT_OF_MEMORY;
        goto Error;
    }
    SourceCid->CID.IsInitial = TRUE;
    SourceCid->CID.UsedByPeer = TRUE;
    CxPlatListPushEntry(&Connection->SourceCids, &SourceCid->Link);
    QuicTraceEvent(
        ConnSourceCidAdded,
        "[conn][%p] (SeqNum=%llu) New Source CID: %!CID!",
        Connection,
        SourceCid->CID.SequenceNumber,
        CASTED_CLOG_BYTEARRAY(SourceCid->CID.Length, SourceCid->CID.Data));

    //
    // Server lazily finishes initialization in response to first operation.
    //

}
else
{
    Connection->Type = QUIC_HANDLE_TYPE_CONNECTION_CLIENT;
    Connection->State.ExternalOwner = TRUE;
    Path->IsPeerValidated = TRUE;
    Path->Allowance = UINT32_MAX;

    Path->DestCid = QuicCidNewRandomDestination();
    if (Path->DestCid == NULL)
    {
        Status = QUIC_STATUS_OUT_OF_MEMORY;
        goto Error;
    }
    QUIC_CID_SET_PATH(Connection, Path->DestCid, Path);
    Path->DestCid->CID.UsedLocally = TRUE;
    Connection->DestCidCount++;
    CxPlatListInsertTail(&Connection->DestCids, &Path->DestCid->Link);
    QuicTraceEvent(
        ConnDestCidAdded,
        "[conn][%p] (SeqNum=%llu) New Destination CID: %!CID!",
        Connection,
        Path->DestCid->CID.SequenceNumber,
        CASTED_CLOG_BYTEARRAY(Path->DestCid->CID.Length, Path->DestCid->CID.Data));

    Connection->State.Initialized = TRUE;
    QuicTraceEvent(
        ConnInitializeComplete,
        "[conn][%p] Initialize complete",
        Connection);
}

QuicPathValidate(Path);
if (Worker != NULL)
{
    QuicWorkerAssignConnection(Worker, Connection);
}
if (!QuicConnRegister(Connection, Registration))
{
    Status = QUIC_STATUS_INVALID_STATE;
    goto Error;
}

*NewConnection = Connection;
return QUIC_STATUS_SUCCESS;

Error:

Connection->State.HandleClosed = TRUE;
for (uint32_t i = 0; i < ARRAYSIZE(Connection->Packets); i++)
{
    if (Connection->Packets[i] != NULL)
    {
        QuicPacketSpaceUninitialize(Connection->Packets[i]);
        Connection->Packets[i] = NULL;
    }
}
if (Packet != NULL && Connection->SourceCids.Next != NULL)
{
    CXPLAT_FREE(
        CXPLAT_CONTAINING_RECORD(
            Connection->SourceCids.Next,
            QUIC_CID_HASH_ENTRY,
            Link),
        QUIC_POOL_CIDHASH);
    Connection->SourceCids.Next = NULL;
}
while (!CxPlatListIsEmpty(&Connection->DestCids))
{
    QUIC_CID_LIST_ENTRY* CID =
        CXPLAT_CONTAINING_RECORD(
            CxPlatListRemoveHead(&Connection->DestCids),
            QUIC_CID_LIST_ENTRY,
            Link);
    CXPLAT_FREE(CID, QUIC_POOL_CIDLIST);
}
QuicConnRelease(Connection, QUIC_CONN_REF_HANDLE_OWNER);

return Status;
}

        
        static ushort QuicConnGetMaxMtuForPath(QUIC_CONNECTION Connection,QUIC_PATH Path)
        {
            ushort LocalMtu = Path.LocalMtu;
            if (LocalMtu == 0)
            {
                LocalMtu = CxPlatSocketGetLocalMtu(Path->Binding->Socket);
                Path.LocalMtu = LocalMtu;
            }

            ushort RemoteMtu = 0xFFFF;
            if ((Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE))
            {
                RemoteMtu = PacketSizeFromUdpPayloadSize(
                        QuicAddrGetFamily(&Path->Route.RemoteAddress),
                        (uint16_t)Connection->PeerTransportParams.MaxUdpPayloadSize);
            }
            uint16_t SettingsMtu = Connection->Settings.MaximumMtu;
            return CXPLAT_MIN(CXPLAT_MIN(LocalMtu, RemoteMtu), SettingsMtu);
        }

        static void QuicConnTryClose(QUIC_CONNECTION Connection, uint Flags, ulong ErrorCode, string RemoteReasonPhrase, ushort RemoteReasonPhraseLength)
        {
            bool ClosedRemotely = BoolOk(Flags &  QUIC_CLOSE_REMOTE);
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
                    //
                    // Enter 'draining period' to flush out any leftover packets.
                    //
                    QuicConnTimerSet(
                        Connection,
                        QUIC_CONN_TIMER_SHUTDOWN,
                        CXPLAT_MAX(MS_TO_US(15), Connection->Paths[0].SmoothedRtt * 2));

                    QuicSendSetSendFlag(
                        &Connection->Send,
                        QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE);
                }

            }
            else if (!ClosedRemotely && !Connection.State.ClosedRemotely)
            {
                if (!SilentClose)
                {
                    //
                    // Enter 'closing period' to wait for a (optional) connection close
                    // response.
                    //
                    uint64_t Pto =
                        QuicLossDetectionComputeProbeTimeout(
                            &Connection->LossDetection,
                            &Connection->Paths[0],
                            QUIC_CLOSE_PTO_COUNT);
                    QuicConnTimerSet(
                        Connection,
                        QUIC_CONN_TIMER_SHUTDOWN,
                        Pto);

                    QuicSendSetSendFlag(
                        &Connection->Send,
                        (Flags & QUIC_CLOSE_APPLICATION) ?
                            QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE :
                            QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE);
                }
            }
            else
            {
                if (QuicConnIsClient(Connection))
                {
                    //
                    // Client side can immediately clean up once its close frame was
                    // acknowledged because we will close the socket during clean up,
                    // which will automatically handle any leftover packets that
                    // get received afterward by dropping them.
                    //

                }
                else if (!SilentClose)
                {
                    //
                    // Server side transitions from the 'closing period' to the
                    // 'draining period' and waits an additional 2 RTT just to make
                    // sure all leftover packets have been flushed out.
                    //
                    QuicConnTimerSet(
                        Connection,
                        QUIC_CONN_TIMER_SHUTDOWN,
                        CXPLAT_MAX(MS_TO_US(15), Connection->Paths[0].SmoothedRtt * 2));
                }

                IsFirstCloseForConnection = FALSE;
            }

        if (IsFirstCloseForConnection)
        {
            //
            // Default to the timed out state.
            //
            Connection->State.ShutdownCompleteTimedOut = TRUE;

            //
            // Cancel all non-shutdown related timers.
            //
            for (QUIC_CONN_TIMER_TYPE TimerType = QUIC_CONN_TIMER_IDLE;
                TimerType < QUIC_CONN_TIMER_SHUTDOWN;
                ++TimerType)
            {
                QuicConnTimerCancel(Connection, TimerType);
            }

            if (ResultQuicStatus)
            {
                Connection->CloseStatus = (QUIC_STATUS)ErrorCode;
                Connection->CloseErrorCode = QUIC_ERROR_INTERNAL_ERROR;
            }
            else
            {
                Connection->CloseStatus = QuicErrorCodeToStatus(ErrorCode);
                Connection->CloseErrorCode = ErrorCode;
                if (QuicErrorIsProtocolError(ErrorCode))
                {
                    QuicPerfCounterIncrement(QUIC_PERF_COUNTER_CONN_PROTOCOL_ERRORS);
                }
            }

            if (Flags & QUIC_CLOSE_APPLICATION)
            {
                Connection->State.AppClosed = TRUE;
            }

            if (Flags & QUIC_CLOSE_SEND_NOTIFICATION &&
                Connection->State.ExternalOwner)
            {
                QuicConnIndicateShutdownBegin(Connection);
            }

            if (Connection->CloseReasonPhrase != NULL)
            {
                CXPLAT_FREE(Connection->CloseReasonPhrase, QUIC_POOL_CLOSE_REASON);
                Connection->CloseReasonPhrase = NULL;
            }

            if (RemoteReasonPhraseLength != 0)
            {
                Connection->CloseReasonPhrase =
                    CXPLAT_ALLOC_NONPAGED(RemoteReasonPhraseLength + 1, QUIC_POOL_CLOSE_REASON);
                if (Connection->CloseReasonPhrase != NULL)
                {
                    CxPlatCopyMemory(
                        Connection->CloseReasonPhrase,
                        RemoteReasonPhrase,
                        RemoteReasonPhraseLength);
                    Connection->CloseReasonPhrase[RemoteReasonPhraseLength] = 0;
                }
                else
                {
                    QuicTraceEvent(
                        AllocFailure,
                        "Allocation of '%s' failed. (%llu bytes)",
                        "close reason",
                        RemoteReasonPhraseLength + 1);
                }
            }

            if (Connection->State.Started)
            {
                QuicConnLogStatistics(Connection);
            }

            if (Flags & QUIC_CLOSE_APPLICATION)
            {
                QuicTraceEvent(
                    ConnAppShutdown,
                    "[conn][%p] App Shutdown: %llu (Remote=%hhu)",
                    Connection,
                    ErrorCode,
                    ClosedRemotely);
            }
            else
            {
                QuicTraceEvent(
                    ConnTransportShutdown,
                    "[conn][%p] Transport Shutdown: %llu (Remote=%hhu) (QS=%hhu)",
                    Connection,
                    ErrorCode,
                    ClosedRemotely,
                    !!(Flags & QUIC_CLOSE_QUIC_STATUS));
            }

            //
            // On initial close, we must shut down all the current streams and
            // clean up pending datagrams.
            //
            QuicStreamSetShutdown(&Connection->Streams);
            QuicDatagramSendShutdown(&Connection->Datagram);
        }

        if (SilentClose)
        {
            QuicSendClear(&Connection->Send);
        }

        if (SilentClose ||
            (Connection->State.ClosedRemotely && Connection->State.ClosedLocally))
        {
            Connection->State.ShutdownCompleteTimedOut = FALSE;
            Connection->State.ProcessShutdownComplete = TRUE;
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

            QuicConnCloseLocally(
                Connection,
                QUIC_CLOSE_SILENT | QUIC_CLOSE_QUIC_STATUS,
                (uint64_t)QUIC_STATUS_ABORTED,
                NULL);

            if (Connection.State.ProcessShutdownComplete)
            {
                QuicConnOnShutdownComplete(Connection);
            }

            QuicConnUnregister(Connection);
        }
    }

}
