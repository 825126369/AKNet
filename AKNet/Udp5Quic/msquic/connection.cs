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

        public class Schedule
        {
            public uint LastQueueTime;         // Time the connection last entered the work queue.
            ulong DrainCount;            // Sum of drain calls
            ulong OperationCount;        // Sum of operations processed
        }

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

        int WorkerThreadID;

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

        //
        // Set of current reasons sending more packets is currently blocked.
        //
        public byte OutFlowBlockedReasons; // Set of QUIC_FLOW_BLOCKED_* flags

        //
        // Ack Delay Exponent. Used to scale actual wire encoded value by
        // 2 ^ ack_delay_exponent.
        //
        public byte AckDelayExponent;

        //
        // The number of packets that must be received before eliciting an immediate
        // acknowledgment. May be updated by the peer via the ACK_FREQUENCY frame.
        //
        public byte PacketTolerance;

        //
        // The number of packets we want the peer to wait before sending an
        // immediate acknowledgment. Requires the ACK_FREQUENCY extension/frame to
        // be able to send to the peer.
        //
        public byte PeerPacketTolerance;

        //
        // The maximum number of packets that can be out of order before an immediate
        // acknowledgment (ACK) is triggered. If no specific instructions (ACK_FREQUENCY
        // frames) are received from the peer, the receiver will immediately acknowledge
        // any out-of-order packets, which means the default value is 1. A value of 0
        // means out-of-order packets do not trigger an immediate ACK.
        //
        public byte ReorderingThreshold;

        //
        // The maximum number of packets that the peer can be out of order before an immediate
        // acknowledgment (ACK) is triggered.
        //
        public byte PeerReorderingThreshold;

        //
        // DSCP value to set on all sends from this connection.
        // Default value of 0.
        //
        public byte DSCP;

        //
        // The ACK frequency sequence number we are currently using to send.
        //
        public ulong SendAckFreqSeqNum;

        //
        // The next ACK frequency sequence number we expect to receive.
        //
        public ulong NextRecvAckFreqSeqNum;

        //
        // The sequence number to use for the next source CID.
        //
        public ulong NextSourceCidSequenceNumber;

        //
        // The most recent Retire Prior To field received in a NEW_CONNECTION_ID
        // frame.
        //
        public ulong RetirePriorTo;

        //
        // Per-path state. The first entry in the list is the active path. All the
        // rest (if any) are other tracked paths, sorted from most to least recently
        // used.
        //
        QUIC_PATH[] Paths = new QUIC_PATH[QUIC_MAX_PATH_COUNT];

        //
        // The list of connection IDs used for receiving.
        //
        quic_platform_cxplat_slist_entry SourceCids;

        //
        // The list of connection IDs used for sending. Given to us by the peer.
        //
        CXPLAT_LIST_ENTRY DestCids;

        //
        // The original CID used by the Client in its first Initial packet.
        //
        QUIC_CID* OrigDestCID;

        //
        // An app configured prefix for all connection IDs. The first byte indicates
        // the length of the ID, the second byte the offset of the ID in the CID and
        // the rest payload of the identifier.
        //
        public byte CibirId[2 + QUIC_MAX_CIBIR_LENGTH];
        public long[] ExpirationTimes = new long[(int)QUIC_CONN_TIMER_TYPE.QUIC_CONN_TIMER_COUNT];
        public long EarliestExpirationTime;
        public uint ReceiveQueueCount;
        public uint ReceiveQueueByteCount;
        QUIC_RX_PACKET ReceiveQueue;
        QUIC_RX_PACKET ReceiveQueueTail;
        CXPLAT_DISPATCH_LOCK ReceiveQueueLock;

        //
        // The queue of operations to process.
        //
        QUIC_OPERATION_QUEUE OperQ;
        QUIC_OPERATION BackUpOper;
        QUIC_API_CONTEXT BackupApiContext;
        uint16_t BackUpOperUsed;

        //
        // The status code used for indicating transport closed notifications.
        //
        QUIC_STATUS CloseStatus;

        //
        // The locally set error code we use for sending the connection close.
        //
        ulong CloseErrorCode;
        char* CloseReasonPhrase;

        //
        // The name of the remote server.
        //
        _Field_z_
    const char* RemoteServerName;

        //
        // The entry into the remote hash lookup table, which is used only during the
        // handshake.
        //
        QUIC_REMOTE_HASH_ENTRY* RemoteHashEntry;

        //
        // Transport parameters received from the peer.
        //
        public QUIC_TRANSPORT_PARAMETERS PeerTransportParams;

        //
        // Working space for decoded ACK ranges. All ACK frames that are received
        // are first decoded into this range.
        //
        QUIC_RANGE DecodedAckRanges;

        //
        // All the information and management logic for streams.
        //
        QUIC_STREAM_SET Streams;

        //
        // Congestion control state.
        //
        QUIC_CONGESTION_CONTROL CongestionControl;

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
        QUIC_CONNECTION_CALLBACK_HANDLER ClientCallbackHandler;

        //
        // (Server-only) Transport parameters used during handshake.
        // Only non-null when resumption is enabled.
        //
        QUIC_TRANSPORT_PARAMETERS* HandshakeTP;
        QUIC_CONN_STATS Stats;
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

        static void QuicConnAddRef(QUIC_CONNECTION Connection, QUIC_CONNECTION_REF Ref)
        {
            QuicConnValidate(Connection);
#if DEBUG
            Interlocked.Increment(ref Connection.RefTypeCount[(int)Ref]);
#else
    
#endif
            Interlocked.Increment(ref Connection.RefCount);
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

        static void QuicConnRelease (QUIC_CONNECTION Connection, QUIC_CONNECTION_REF Ref)
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
    }

}
