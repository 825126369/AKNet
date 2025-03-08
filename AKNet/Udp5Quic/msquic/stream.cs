using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SEND_REQUEST
    {
        public QUIC_SEND_REQUEST Next;
        public List<QUIC_BUFFER> Buffers;
        public QUIC_SEND_FLAGS Flags;
        public int StreamOffset;
        public int TotalLength;
        public QUIC_BUFFER InternalBuffer;
    }

    public class QUIC_STREAM_FLAGS
    {
        public ulong AllFlags;

        public bool Allocated              ;    // Allocated by Connection. Used for Debugging.
        public bool Initialized            ;    // Initialized successfully. Used for Debugging.
        public bool Started                ;    // The app has started the stream.
        public bool StartedIndicated       ;    // The app received a start complete event.
        public bool PeerStreamStartEventActive; // The app is processing QUIC_CONNECTION_EVENT_PEER_STREAM_STARTED
        public bool Unidirectional         ;    // Sends/receives in 1 direction only.
        public bool Opened0Rtt             ;    // A 0-RTT packet opened the stream.
        public bool IndicatePeerAccepted   ;    // The app requested the PEER_ACCEPTED event.
        
        public bool SendOpen               ;    // Send a STREAM frame immediately on start.
        public bool SendOpenAcked          ;    // A STREAM frame has been acknowledged.

        public bool LocalNotAllowed        ;    // Peer's unidirectional stream.
        public bool LocalCloseFin          ;    // Locally closed (graceful).
        public bool LocalCloseReset        ;    // Locally closed (locally aborted).
        public bool LocalCloseResetReliable;    // Indicates that we should shutdown the send path once we sent/ACK'd ReliableOffsetSend bytes.
        public bool LocalCloseResetReliableAcked; // Indicates the peer has acknowledged we will stop sending once we sent/ACK'd ReliableOffsetSend bytes.
        public bool RemoteCloseResetReliable;   // Indicates that the peer initiated a reliable reset. Keep Recv path available for RecvMaxLength bytes.
        public bool ReceivedStopSending    ;    // Peer sent STOP_SENDING frame.
        public bool LocalCloseAcked        ;    // Any close acknowledged.
        public bool FinAcked               ;    // Our FIN was acknowledged.
        public bool InRecovery             ;    // Lost data is being retransmitted and is
                                                // unacknowledged.

        public bool RemoteNotAllowed       ;    // Our unidirectional stream.
        public bool RemoteCloseFin         ;    // Remotely closed.
        public bool RemoteCloseReset       ;    // Remotely closed (remotely aborted).
        public bool SentStopSending        ;    // We sent STOP_SENDING frame.
        public bool RemoteCloseAcked       ;    // Any close acknowledged.

        public bool SendEnabled            ;    // Application is allowed to send data.
        public bool ReceiveEnabled         ;    // Application is ready for receive callbacks.
        public bool ReceiveMultiple        ;    // The app supports multiple parallel receive indications.
        public bool UseAppOwnedRecvBuffers ;    // The stream is using app provided receive buffers.
        public bool ReceiveFlushQueued     ;    // The receive flush operation is queued.
        public bool ReceiveDataPending     ;    // Data (or FIN) is queued and ready for delivery.
        public bool ReceiveCallActive      ;    // There is an active receive to the app.
        public bool SendDelayed            ;    // A delayed send is currently queued.
        public bool CancelOnLoss           ;    // Indicates that the stream is to be canceled
                                                // if loss is detected.

        public bool HandleSendShutdown     ;    // Send shutdown complete callback delivered.
        public bool HandleShutdown         ;    // Shutdown callback delivered.
        public bool HandleClosed           ;    // Handle closed by application layer.

        public bool ShutdownComplete       ;    // Both directions have been shutdown and acknowledged.
        public bool Uninitialized          ;    // Uninitialize started/completed. Used for Debugging.
        public bool Freed                  ;    // Freed after last ref count released. Used for Debugging.

        public bool InStreamTable          ;    // The stream is currently in the connection's table.
        public bool InWaitingList          ;    // The stream is currently in the waiting list for stream id FC.
        public bool DelayIdFcUpdate        ;    // Delay stream ID FC updates to StreamClose.

    }

    internal enum QUIC_STREAM_SEND_STATE
    {
        QUIC_STREAM_SEND_DISABLED,
        QUIC_STREAM_SEND_STARTED,
        QUIC_STREAM_SEND_RESET,
        QUIC_STREAM_SEND_RESET_ACKED,
        QUIC_STREAM_SEND_FIN,
        QUIC_STREAM_SEND_FIN_ACKED,
        QUIC_STREAM_SEND_RELIABLE_RESET,
        QUIC_STREAM_SEND_RELIABLE_RESET_ACKED
    }

    internal enum QUIC_STREAM_RECV_STATE
    {
        QUIC_STREAM_RECV_DISABLED,
        QUIC_STREAM_RECV_STARTED,
        QUIC_STREAM_RECV_PAUSED,
        QUIC_STREAM_RECV_STOPPED,
        QUIC_STREAM_RECV_RESET,
        QUIC_STREAM_RECV_FIN,
        QUIC_STREAM_RECV_RELIABLE_RESET
    }

    internal enum QUIC_STREAM_REF
    {
        QUIC_STREAM_REF_APP,
        QUIC_STREAM_REF_STREAM_SET,
        QUIC_STREAM_REF_SEND,
        QUIC_STREAM_REF_SEND_PACKET,
        QUIC_STREAM_REF_LOOKUP,
        QUIC_STREAM_REF_OPERATION,
        QUIC_STREAM_REF_COUNT
    }
    internal class QUIC_STREAM
    {
        public long RefCount;
        public short[] RefTypeCount = new short[(int)QUIC_STREAM_REF.QUIC_STREAM_REF_COUNT];
        public uint OutstandingSentMetadata;

        // CXPLAT_HASHTABLE_ENTRY TableEntry;
        CXPLAT_LIST_ENTRY WaitingLink;
        CXPLAT_LIST_ENTRY ClosedLink;



        CXPLAT_LIST_ENTRY SendLink;
        CXPLAT_LIST_ENTRY AllStreamsLink;

        QUIC_CONNECTION Connection;
        public ulong ID;
        public QUIC_STREAM_FLAGS Flags;
        public ushort SendFlags;

        public byte OutFlowBlockedReasons;
        CXPLAT_DISPATCH_LOCK ApiSendRequestLock;
        public QUIC_SEND_REQUEST ApiSendRequests;
        QUIC_SEND_REQUEST SendRequests;
        QUIC_SEND_REQUEST SendRequestsTail;

        QUIC_SEND_REQUEST SendBookmark;
        QUIC_SEND_REQUEST* SendBufferBookmark;
        public ulong QueuedSendOffset;
        public ulong Queued0Rtt;
        public ulong Sent0Rtt;
        public ulong MaxAllowedSendOffset;
        public uint SendWindow;
        public ulong LastIdealSendBuffer;
        public ulong MaxSentLength;

        public ulong UnAckedOffset;
        public ulong NextSendOffset;
        public ulong RecoveryNextOffset;
        public ulong RecoveryEndOffset;
        public ulong ReliableOffsetSend;


        public QUIC_VAR_INT SendShutdownErrorCode;
        QUIC_RANGE SparseAckRanges;
        public ushort SendPriority;
        public ulong MaxAllowedRecvOffset;
        public ulong RecvWindowBytesDelivered;

        public ulong RecvWindowLastUpdate;

        QUIC_RECV_BUFFER RecvBuffer;

        public ulong RecvMax0RttLength;
        public ulong RecvMaxLength;
        public ulong RecvPendingLength;
        public ulong RecvCompletionLength;

        QUIC_VAR_INT RecvShutdownErrorCode;
        QUIC_STREAM_CALLBACK_HANDLER ClientCallbackHandler;
        QUIC_OPERATION* ReceiveCompleteOperation;
        QUIC_OPERATION ReceiveCompleteOperationStorage;
        QUIC_API_CONTEXT ReceiveCompleteApiCtxStorage;

        public class BlockedTimings
        {
            QUIC_FLOW_BLOCKED_TIMING_TRACKER StreamIdFlowControl;
            QUIC_FLOW_BLOCKED_TIMING_TRACKER FlowControl;
            QUIC_FLOW_BLOCKED_TIMING_TRACKER App;
            public ulong CachedConnSchedulingUs;
            public ulong CachedConnPacingUs;
            public ulong CachedConnAmplificationProtUs;
            public ulong CachedConnCongestionControlUs;
            public ulong CachedConnFlowControlUs;
        }
    }
}
