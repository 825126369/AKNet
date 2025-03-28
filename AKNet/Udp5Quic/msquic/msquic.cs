using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal delegate void QUIC_LISTENER_CALLBACK_HANDLER(QUIC_LISTENER Listener, IntPtr Context, ref QUIC_NEW_CONNECTION_INFO Info);
    internal delegate long QUIC_STREAM_CALLBACK(QUIC_HANDLE Stream, void* Context, QUIC_STREAM_EVENT Event);
    internal delegate long QUIC_CONNECTION_CALLBACK(QUIC_HANDLE Connection, void* Context, QUIC_CONNECTION_EVENT Event);

    internal class QUIC_BUFFER
    {
        public int Length;
        public byte[] Buffer;
    }

    internal enum QUIC_PERFORMANCE_COUNTERS
    {
        QUIC_PERF_COUNTER_CONN_CREATED,         // Total connections ever allocated.
        QUIC_PERF_COUNTER_CONN_HANDSHAKE_FAIL,  // Total connections that failed during handshake.
        QUIC_PERF_COUNTER_CONN_APP_REJECT,      // Total connections rejected by the application.
        QUIC_PERF_COUNTER_CONN_RESUMED,         // Total connections resumed.
        QUIC_PERF_COUNTER_CONN_ACTIVE,          // Connections currently allocated.
        QUIC_PERF_COUNTER_CONN_CONNECTED,       // Connections currently in the connected state.
        QUIC_PERF_COUNTER_CONN_PROTOCOL_ERRORS, // Total connections shutdown with a protocol error.
        QUIC_PERF_COUNTER_CONN_NO_ALPN,         // Total connection attempts with no matching ALPN.
        QUIC_PERF_COUNTER_STRM_ACTIVE,          // Current streams allocated.
        QUIC_PERF_COUNTER_PKTS_SUSPECTED_LOST,  // Total suspected packets lost
        QUIC_PERF_COUNTER_PKTS_DROPPED,         // Total packets dropped for any reason.
        QUIC_PERF_COUNTER_PKTS_DECRYPTION_FAIL, // Total packets with decryption failures.
        QUIC_PERF_COUNTER_UDP_RECV,             // Total UDP datagrams received.
        QUIC_PERF_COUNTER_UDP_SEND,             // Total UDP datagrams sent.
        QUIC_PERF_COUNTER_UDP_RECV_BYTES,       // Total UDP payload bytes received.
        QUIC_PERF_COUNTER_UDP_SEND_BYTES,       // Total UDP payload bytes sent.
        QUIC_PERF_COUNTER_UDP_RECV_EVENTS,      // Total UDP receive events.
        QUIC_PERF_COUNTER_UDP_SEND_CALLS,       // Total UDP send API calls.
        QUIC_PERF_COUNTER_APP_SEND_BYTES,       // Total bytes sent by applications.
        QUIC_PERF_COUNTER_APP_RECV_BYTES,       // Total bytes received by applications.
        QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH,     // Current connections queued for processing.
        QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH,// Current connection operations queued.
        QUIC_PERF_COUNTER_CONN_OPER_QUEUED,     // Total connection operations queued ever.
        QUIC_PERF_COUNTER_CONN_OPER_COMPLETED,  // Total connection operations processed ever.
        QUIC_PERF_COUNTER_WORK_OPER_QUEUE_DEPTH,// Current worker operations queued.
        QUIC_PERF_COUNTER_WORK_OPER_QUEUED,     // Total worker operations queued ever.
        QUIC_PERF_COUNTER_WORK_OPER_COMPLETED,  // Total worker operations processed ever.
        QUIC_PERF_COUNTER_PATH_VALIDATED,       // Total path challenges that succeed ever.
        QUIC_PERF_COUNTER_PATH_FAILURE,         // Total path challenges that fail ever.
        QUIC_PERF_COUNTER_SEND_STATELESS_RESET, // Total stateless reset packets sent ever.
        QUIC_PERF_COUNTER_SEND_STATELESS_RETRY, // Total stateless retry packets sent ever.
        QUIC_PERF_COUNTER_CONN_LOAD_REJECT,     // Total connections rejected due to worker load.
        QUIC_PERF_COUNTER_MAX,
    }

    internal class QUIC_REGISTRATION_CONFIG
    {
        public string AppName;
        public uint ExecutionProfile;
    }

    internal class QUIC_EXECUTION_CONFIG
    {
        public QUIC_EXECUTION_CONFIG_FLAGS Flags;
        public uint PollingIdleTimeoutUs;
        public readonly List<ushort> ProcessorList = new List<ushort>();
    }

    internal enum QUIC_CONNECTION_SHUTDOWN_FLAGS
    {
        QUIC_CONNECTION_SHUTDOWN_FLAG_NONE = 0x0000,
        QUIC_CONNECTION_SHUTDOWN_FLAG_SILENT = 0x0001, 
    }

    internal class QUIC_VERSION_SETTINGS
    {
        public uint[] AcceptableVersions = null;
        public uint[] OfferedVersions;
        public uint[] FullyDeployedVersions;
    }

    internal class QUIC_STREAM_EVENT
    {
        public QUIC_STREAM_EVENT_TYPE Type;
        public START_COMPLETE_Class START_COMPLETE;

        public class START_COMPLETE_Class
        {
            public ulong Status;
            public ulong ID;
            public bool PeerAccepted;
            public bool RESERVED;
        }

        public class RECEIVE_Class
        {
            public ulong AbsoluteOffset;
            public ulong TotalBufferLength;
            public readonly List<QUIC_BUFFER> Buffers = new List<QUIC_BUFFER>();
            public QUIC_RECEIVE_FLAGS Flags;
        }

        public class SEND_COMPLETE_Class
        {
            public bool Canceled;
            void* ClientContext;
        }

        public class PEER_SEND_ABORTED_Class
        {
            public ulong ErrorCode;
        }

        public class PEER_RECEIVE_ABORTED_Class
        {
            public ulong ErrorCode;
        }

        public class SEND_SHUTDOWN_COMPLETE_Class
        {
            public bool Graceful;
        }

        public class SHUTDOWN_COMPLETE_Class
        {
            public bool ConnectionShutdown;
            public bool AppCloseInProgress;
            public bool ConnectionShutdownByApp;
            public bool ConnectionClosedRemotely;
            public bool RESERVED;
            public ulong ConnectionErrorCode;
            public long ConnectionCloseStatus;
        }
        
        public class IDEAL_SEND_BUFFER_SIZE_Class
        {
            public ulong ByteCount;
        }

        public class CANCEL_ON_LOSS_Class
        {
            public ulong ErrorCode;
        }
    }

    internal static partial class MSQuicFunc
    {
        public const uint QUIC_STREAM_EVENT_START_COMPLETE = 0;
        public const uint QUIC_STREAM_EVENT_RECEIVE = 1;
        public const uint QUIC_STREAM_EVENT_SEND_COMPLETE = 2;
        public const uint QUIC_STREAM_EVENT_PEER_SEND_SHUTDOWN = 3;
        public const uint QUIC_STREAM_EVENT_PEER_SEND_ABORTED = 4;
        public const uint QUIC_STREAM_EVENT_PEER_RECEIVE_ABORTED = 5;
        public const uint QUIC_STREAM_EVENT_SEND_SHUTDOWN_COMPLETE = 6;
        public const uint QUIC_STREAM_EVENT_SHUTDOWN_COMPLETE = 7;
        public const uint QUIC_STREAM_EVENT_IDEAL_SEND_BUFFER_SIZE = 8;
        public const uint QUIC_STREAM_EVENT_PEER_ACCEPTED = 9;
        public const uint QUIC_STREAM_EVENT_CANCEL_ON_LOSS = 10;

        public const uint QUIC_SEND_FLAG_NONE = 0x0000;
        public const uint QUIC_SEND_FLAG_ALLOW_0_RTT = 0x0001;   // Allows the use of encrypting with 0-RTT key.
        public const uint QUIC_SEND_FLAG_START = 0x0002;  // Asynchronously starts the stream with the sent data.
        public const uint QUIC_SEND_FLAG_FIN = 0x0004;   // Indicates the request is the one last sent on the stream.
        public const uint QUIC_SEND_FLAG_DGRAM_PRIORITY = 0x0008;   // Indicates the datagram is higher priority than others.
        public const uint QUIC_SEND_FLAG_DELAY_SEND = 0x0010;   // Indicates the send should be delayed because more will be queued soon.
        public const uint QUIC_SEND_FLAG_CANCEL_ON_LOSS = 0x0020;   // Indicates that a stream is to be cancelled when packet loss is detected.
        public const uint QUIC_SEND_FLAG_PRIORITY_WORK = 0x0040;   // Higher priority than other connection work.
        public const uint QUIC_SEND_FLAG_CANCEL_ON_BLOCKED = 0x0080;   // Indicates that a frame should be dropped when it can't be sent immediately.
        
        public const uint QUIC_SEND_RESUMPTION_FLAG_NONE = 0x0000;
        public const uint QUIC_SEND_RESUMPTION_FLAG_FINAL = 0x0001;   // Free TLS state after sending this ticket.
            
        public const uint QUIC_RECEIVE_FLAG_NONE = 0x0000;
        public const uint QUIC_RECEIVE_FLAG_0_RTT = 0x0001;   // Data was encrypted with 0-RTT key.
        public const uint QUIC_RECEIVE_FLAG_FIN = 0x0002;  // FIN was included with this data.

        public const uint QUIC_EXECUTION_PROFILE_LOW_LATENCY = 1;        // 低延迟
        public const uint QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT = 2; // 最大吞吐量
        public const uint QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER = 3;     //收集
        public const uint QUIC_EXECUTION_PROFILE_TYPE_REAL_TIME = 4;     //实时
        
        public const uint QUIC_EXECUTION_CONFIG_FLAG_NONE = 0x0000;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_QTIP = 0x0001;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_RIO = 0x0002;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_XDP = 0x0004;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_NO_IDEAL_PROC = 0x0008;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_HIGH_PRIORITY = 0x0010;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_AFFINITIZE = 0x0020;
        
        public const uint QUIC_STREAM_START_FLAG_NONE = 0x0000;
        public const uint QUIC_STREAM_START_FLAG_IMMEDIATE = 0x0001;   // Immediately informs peer that stream is open.
        public const uint QUIC_STREAM_START_FLAG_FAIL_BLOCKED = 0x0002;   // Only opens the stream if flow control allows.
        public const uint QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL = 0x0004;   // Shutdown the stream immediately after start failure.
        public const uint QUIC_STREAM_START_FLAG_INDICATE_PEER_ACCEPT = 0x0008;   // Indicate PEER_ACCEPTED event if not accepted at start.
        public const uint QUIC_STREAM_START_FLAG_PRIORITY_WORK = 0x0010;   // Higher priority than other connection work.
        
        public const uint QUIC_STREAM_OPEN_FLAG_NONE = 0x0000;
        public const uint QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL = 0x0001;   // Indicates the stream is unidirectional.
        public const uint QUIC_STREAM_OPEN_FLAG_0_RTT = 0x0002;  // The stream was opened via a 0-RTT packet.
        public const uint QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES = 0x0004; // Indicates stream ID flow control limit updates for the                                          // connection should be delayed to StreamClose.
        public const uint QUIC_STREAM_OPEN_FLAG_APP_OWNED_BUFFERS = 0x0008;   // No buffer will be allocated for the stream, the app must
        
        public const uint QUIC_STREAM_SHUTDOWN_FLAG_NONE = 0x0000;
        public const uint QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL = 0x0001;  // Cleanly closes the send path.
        public const uint QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND = 0x0002;  // Abruptly closes the send path.
        public const uint QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE = 0x0004;   // Abruptly closes the receive path.
        public const uint QUIC_STREAM_SHUTDOWN_FLAG_ABORT = 0x0006;   // Abruptly closes both send and receive paths.
        public const uint QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE = 0x0008;   // Immediately sends completion events to app.
        public const uint QUIC_STREAM_SHUTDOWN_FLAG_INLINE = 0x0010;  // Process the shutdown immediately inline. Only for calls on callbacks.
        

    }
}
