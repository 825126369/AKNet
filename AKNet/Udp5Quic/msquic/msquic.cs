using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace AKNet.Udp5Quic.Common
{
    internal delegate ulong QUIC_LISTENER_CALLBACK(QUIC_HANDLE Listener, object Context, QUIC_LISTENER_EVENT Info);
    internal delegate ulong QUIC_STREAM_CALLBACK(QUIC_HANDLE Stream, object Context, QUIC_STREAM_EVENT Event);
    internal delegate ulong QUIC_CONNECTION_CALLBACK(QUIC_HANDLE Connection, QUIC_CONNECTION_EVENT Event);

    internal class QUIC_BUFFER : CXPLAT_POOL_Interface<QUIC_BUFFER>
    {
        public int Offset;
        public int Length;
        public byte[] Buffer;

        public readonly CXPLAT_POOL_ENTRY<QUIC_BUFFER> POOL_ENTRY = null;
        public QUIC_BUFFER()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_BUFFER>(this);
        }

        public QUIC_BUFFER(int nInitSize)
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_BUFFER>(this);
            Buffer = new byte[nInitSize];
            Offset = 0;
            Length = Buffer.Length;
        }

        public CXPLAT_POOL_ENTRY<QUIC_BUFFER> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    internal enum QUIC_LOAD_BALANCING_MODE
    {
        QUIC_LOAD_BALANCING_DISABLED,               // Default
        QUIC_LOAD_BALANCING_SERVER_ID_IP,           // Encodes IP address in Server ID
        QUIC_LOAD_BALANCING_SERVER_ID_FIXED,        // Encodes a fixed 4-byte value in Server ID
        QUIC_LOAD_BALANCING_COUNT,                  // The number of supported load balancing modes
    }

    internal enum QUIC_STREAM_EVENT_TYPE
    {
        QUIC_STREAM_EVENT_START_COMPLETE = 0,
        QUIC_STREAM_EVENT_RECEIVE = 1,
        QUIC_STREAM_EVENT_SEND_COMPLETE = 2,
        QUIC_STREAM_EVENT_PEER_SEND_SHUTDOWN = 3,
        QUIC_STREAM_EVENT_PEER_SEND_ABORTED = 4,
        QUIC_STREAM_EVENT_PEER_RECEIVE_ABORTED = 5,
        QUIC_STREAM_EVENT_SEND_SHUTDOWN_COMPLETE = 6,
        QUIC_STREAM_EVENT_SHUTDOWN_COMPLETE = 7,
        QUIC_STREAM_EVENT_IDEAL_SEND_BUFFER_SIZE = 8,
        QUIC_STREAM_EVENT_PEER_ACCEPTED = 9,
        QUIC_STREAM_EVENT_CANCEL_ON_LOSS = 10,
    }

    internal enum QUIC_TLS_ALERT_CODES
    {
        QUIC_TLS_ALERT_CODE_SUCCESS = 0xFFFF,       // Not a real TlsAlert
        QUIC_TLS_ALERT_CODE_UNEXPECTED_MESSAGE = 10,
        QUIC_TLS_ALERT_CODE_BAD_CERTIFICATE = 42,
        QUIC_TLS_ALERT_CODE_UNSUPPORTED_CERTIFICATE = 43,
        QUIC_TLS_ALERT_CODE_CERTIFICATE_REVOKED = 44,
        QUIC_TLS_ALERT_CODE_CERTIFICATE_EXPIRED = 45,
        QUIC_TLS_ALERT_CODE_CERTIFICATE_UNKNOWN = 46,
        QUIC_TLS_ALERT_CODE_ILLEGAL_PARAMETER = 47,
        QUIC_TLS_ALERT_CODE_UNKNOWN_CA = 48,
        QUIC_TLS_ALERT_CODE_ACCESS_DENIED = 49,
        QUIC_TLS_ALERT_CODE_INSUFFICIENT_SECURITY = 71,
        QUIC_TLS_ALERT_CODE_INTERNAL_ERROR = 80,
        QUIC_TLS_ALERT_CODE_USER_CANCELED = 90,
        QUIC_TLS_ALERT_CODE_CERTIFICATE_REQUIRED = 116,
        QUIC_TLS_ALERT_CODE_MAX = 255,
    }

    internal enum QUIC_CREDENTIAL_TYPE
    {
        QUIC_CREDENTIAL_TYPE_NONE,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH_STORE,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_CONTEXT,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_PKCS12,
    }

    internal struct QUIC_LISTENER_STATISTICS
    {
        public long TotalAcceptedConnections;
        public long TotalRejectedConnections;
        public long BindingRecvDroppedPackets;
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

    internal enum QUIC_EXECUTION_PROFILE
    {
        QUIC_EXECUTION_PROFILE_TYPE_INTERNAL,
        QUIC_EXECUTION_PROFILE_LOW_LATENCY,         // Default
        QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT,
        QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER,
        QUIC_EXECUTION_PROFILE_TYPE_REAL_TIME,
    }

    internal class QUIC_TLS_SECRETS
    {
        public byte SecretLength;
        internal class IsSet_Class
        {
            public bool ClientRandom;
            public bool ClientEarlyTrafficSecret;
            public bool ClientHandshakeTrafficSecret;
            public bool ServerHandshakeTrafficSecret;
            public bool ClientTrafficSecret0;
            public bool ServerTrafficSecret0;
        }

        public IsSet_Class IsSet;
        public byte[] ClientRandom = new byte[32];
        public byte[] ClientEarlyTrafficSecret = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
        public byte[] ClientHandshakeTrafficSecret = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
        public byte[] ServerHandshakeTrafficSecret = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
        public byte[] ClientTrafficSecret0 = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
        public byte[] ServerTrafficSecret0 = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
    }

    internal class QUIC_PRIVATE_TRANSPORT_PARAMETER
    {
        public uint Type;
        public ushort Length;
        public byte[] Buffer;
    }

    internal class QUIC_REGISTRATION_CONFIG
    {
        public string AppName;
        public QUIC_EXECUTION_PROFILE ExecutionProfile;
    }

    internal class QUIC_EXECUTION_CONFIG
    {
        public uint Flags;
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
        public readonly List<uint> FullyDeployedVersions = new List<uint>();
        public int AcceptableVersionsLength;
        public int OfferedVersionsLength;
    }

    internal struct QUIC_STREAM_EVENT
    {
        public QUIC_STREAM_EVENT_TYPE Type;
        public START_COMPLETE_Class START_COMPLETE;
        public RECEIVE_Class RECEIVE;
        public SEND_COMPLETE_Class SEND_COMPLETE;
        public PEER_SEND_ABORTED_Class PEER_SEND_ABORTED;
        public PEER_RECEIVE_ABORTED_Class PEER_RECEIVE_ABORTED;
        public SEND_SHUTDOWN_COMPLETE_Class SEND_SHUTDOWN_COMPLETE;
        public IDEAL_SEND_BUFFER_SIZE_Class IDEAL_SEND_BUFFER_SIZE;
        public CANCEL_ON_LOSS_Class CANCEL_ON_LOSS;
        public SHUTDOWN_COMPLETE_Class SHUTDOWN_COMPLETE;

        public class START_COMPLETE_Class
        {
            public ulong Status;
            public ulong ID;
            public bool PeerAccepted;
            public bool RESERVED;
        }

        public struct RECEIVE_Class
        {
            public int AbsoluteOffset;
            public int TotalBufferLength;
            public readonly List<QUIC_BUFFER> Buffers;
            public uint Flags;

            public RECEIVE_Class(int _ = 0)
            {
                Buffers = new List<QUIC_BUFFER>();
            }
        }

        public struct SEND_COMPLETE_Class
        {
            public bool Canceled;
            public object ClientContext;
        }

        public struct PEER_SEND_ABORTED_Class
        {
            public ulong ErrorCode;
        }

        public struct PEER_RECEIVE_ABORTED_Class
        {
            public ulong ErrorCode;
        }

        public struct SEND_SHUTDOWN_COMPLETE_Class
        {
            public bool Graceful;
        }

        public struct SHUTDOWN_COMPLETE_Class
        {
            public bool ConnectionShutdown;
            public bool AppCloseInProgress;
            public bool ConnectionShutdownByApp;
            public bool ConnectionClosedRemotely;
            public bool RESERVED;
            public ulong ConnectionErrorCode;
            public ulong ConnectionCloseStatus;
        }

        public struct IDEAL_SEND_BUFFER_SIZE_Class
        {
            public int ByteCount;
        }

        public struct CANCEL_ON_LOSS_Class
        {
            public ulong ErrorCode;
        }
    }

    internal enum QUIC_CONNECTION_EVENT_TYPE
    {
        QUIC_CONNECTION_EVENT_CONNECTED = 0,
        QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_TRANSPORT = 1,    // The transport started the shutdown process.
        QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_PEER = 2,    // The peer application started the shutdown process.
        QUIC_CONNECTION_EVENT_SHUTDOWN_COMPLETE = 3,    // Ready for the handle to be closed.
        QUIC_CONNECTION_EVENT_LOCAL_ADDRESS_CHANGED = 4,
        QUIC_CONNECTION_EVENT_PEER_ADDRESS_CHANGED = 5,
        QUIC_CONNECTION_EVENT_PEER_STREAM_STARTED = 6,
        QUIC_CONNECTION_EVENT_STREAMS_AVAILABLE = 7,
        QUIC_CONNECTION_EVENT_PEER_NEEDS_STREAMS = 8,
        QUIC_CONNECTION_EVENT_IDEAL_PROCESSOR_CHANGED = 9,
        QUIC_CONNECTION_EVENT_DATAGRAM_STATE_CHANGED = 10,
        QUIC_CONNECTION_EVENT_DATAGRAM_RECEIVED = 11,
        QUIC_CONNECTION_EVENT_DATAGRAM_SEND_STATE_CHANGED = 12,
        QUIC_CONNECTION_EVENT_RESUMED = 13,   // Server-only; provides resumption data, if any.
        QUIC_CONNECTION_EVENT_RESUMPTION_TICKET_RECEIVED = 14,   // Client-only; provides ticket to persist, if any.
        QUIC_CONNECTION_EVENT_PEER_CERTIFICATE_RECEIVED = 15,   // Only with QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED set
        QUIC_CONNECTION_EVENT_RELIABLE_RESET_NEGOTIATED = 16,   // Only indicated if QUIC_SETTINGS.ReliableResetEnabled is TRUE.
        QUIC_CONNECTION_EVENT_ONE_WAY_DELAY_NEGOTIATED = 17,   // Only indicated if QUIC_SETTINGS.OneWayDelayEnabled is TRUE.
        QUIC_CONNECTION_EVENT_NETWORK_STATISTICS = 18,   // Only indicated if QUIC_SETTINGS.EnableNetStatsEvent is TRUE.
    }
    
    internal enum QUIC_DATAGRAM_SEND_STATE
    {
        QUIC_DATAGRAM_SEND_UNKNOWN,                         // Not yet sent.
        QUIC_DATAGRAM_SEND_SENT,                            // Sent and awaiting acknowledgment
        QUIC_DATAGRAM_SEND_LOST_SUSPECT,                    // Suspected as lost, but still tracked
        QUIC_DATAGRAM_SEND_LOST_DISCARDED,                  // Lost and not longer being tracked
        QUIC_DATAGRAM_SEND_ACKNOWLEDGED,                    // Acknowledged
        QUIC_DATAGRAM_SEND_ACKNOWLEDGED_SPURIOUS,           // Acknowledged after being suspected lost
        QUIC_DATAGRAM_SEND_CANCELED,                        // Canceled before send
    }

    internal class QUIC_NEW_CONNECTION_INFO
    {
        public uint QuicVersion;
        public QUIC_ADDR LocalAddress;
        public QUIC_ADDR RemoteAddress;
        public int CryptoBufferLength;
        public int ClientAlpnListLength;
        public byte NegotiatedAlpnLength;
        public byte[] CryptoBuffer;
        public byte[] ClientAlpnList = new byte[1024];
        public byte[] NegotiatedAlpn;
        public string ServerName;
    }

    internal struct QUIC_CONNECTION_EVENT
    {
        public QUIC_CONNECTION_EVENT_TYPE Type;

        public CONNECTED_DATA CONNECTED;
        public SHUTDOWN_INITIATED_BY_TRANSPORT_DATA SHUTDOWN_INITIATED_BY_TRANSPORT;
        public SHUTDOWN_INITIATED_BY_PEER_DATA SHUTDOWN_INITIATED_BY_PEER;
        public SHUTDOWN_COMPLETE_DATA SHUTDOWN_COMPLETE;
        public LOCAL_ADDRESS_CHANGED_DATA LOCAL_ADDRESS_CHANGED;
        public PEER_ADDRESS_CHANGED_DATA PEER_ADDRESS_CHANGED;
        public PEER_STREAM_STARTED_DATA PEER_STREAM_STARTED;
        public STREAMS_AVAILABLE_DATA STREAMS_AVAILABLE;
        public PEER_NEEDS_STREAMS_DATA PEER_NEEDS_STREAMS;
        public IDEAL_PROCESSOR_CHANGED_DATA IDEAL_PROCESSOR_CHANGED;
        public DATAGRAM_STATE_CHANGED_DATA DATAGRAM_STATE_CHANGED;
        public DATAGRAM_RECEIVED_DATA DATAGRAM_RECEIVED;
        public DATAGRAM_SEND_STATE_CHANGED_DATA DATAGRAM_SEND_STATE_CHANGED;
        public RESUMED_DATA RESUMED;
        public RESUMPTION_TICKET_RECEIVED_DATA RESUMPTION_TICKET_RECEIVED;
        public PEER_CERTIFICATE_RECEIVED_DATA PEER_CERTIFICATE_RECEIVED;
        public RELIABLE_RESET_NEGOTIATED_DATA RELIABLE_RESET_NEGOTIATED;
        public ONE_WAY_DELAY_NEGOTIATED_DATA ONE_WAY_DELAY_NEGOTIATED;
        public NETWORK_STATISTICS_DATA NETWORK_STATISTICS;

        public struct CONNECTED_DATA
        {
            public bool SessionResumed;
            public byte NegotiatedAlpnLength;
            public string NegotiatedAlpn;
        }
        public struct SHUTDOWN_INITIATED_BY_TRANSPORT_DATA
        {
            public ulong Status;
            public ulong ErrorCode; // Wire format error code.
        }
        public struct SHUTDOWN_INITIATED_BY_PEER_DATA
        {
            public ulong ErrorCode;
        }
        public class SHUTDOWN_COMPLETE_DATA
        {
            public bool HandshakeCompleted;
            public bool PeerAcknowledgedShutdown;
            public bool AppCloseInProgress;
        }
        public class LOCAL_ADDRESS_CHANGED_DATA
        {
            public QUIC_ADDR Address;
        }
        public class PEER_ADDRESS_CHANGED_DATA
        {
            public QUIC_ADDR Address;
        }

        public class PEER_STREAM_STARTED_DATA
        {
            public QUIC_HANDLE Stream;
            public uint Flags;
        }
        public class STREAMS_AVAILABLE_DATA
        {
            public int BidirectionalCount;
            public int UnidirectionalCount;
        }
        public class PEER_NEEDS_STREAMS_DATA
        {
            public bool Bidirectional;
        }
        public class IDEAL_PROCESSOR_CHANGED_DATA
        {
            public int IdealProcessor;
            public int PartitionIndex;
        }
        public class DATAGRAM_STATE_CHANGED_DATA
        {
            public bool SendEnabled;
            public int MaxSendLength;
        }
        public class DATAGRAM_RECEIVED_DATA
        {
            public QUIC_BUFFER Buffer;
            public uint Flags;
        }
        public class DATAGRAM_SEND_STATE_CHANGED_DATA
        {
            public QUIC_DATAGRAM_SEND_STATE State;
            public object ClientContext;
        }
        public class RESUMED_DATA
        {
            public ushort ResumptionStateLength;
            public byte[] ResumptionState;
        }
        public class RESUMPTION_TICKET_RECEIVED_DATA
        {
            public uint ResumptionTicketLength;
            public byte[] ResumptionTicket;
        }
        public class PEER_CERTIFICATE_RECEIVED_DATA
        {
            public X509Certificate2 Certificate;      // Peer certificate (platform specific). Valid only during QUIC_CONNECTION_EVENT_PEER_CERTIFICATE_RECEIVED callback.
            public uint DeferredErrorFlags;        // Bit flag of errors (only valid with QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION) - Schannel only, zero otherwise.
            public ulong DeferredStatus;         // Most severe error status (only valid with QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION)
            public X509Chain Chain;      // Peer certificate chain (platform specific). Valid only during QUIC_CONNECTION_EVENT_PEER_CERTIFICATE_RECEIVED callback.
        }
        public class RELIABLE_RESET_NEGOTIATED_DATA
        {
            public bool IsNegotiated;
        }
        public class ONE_WAY_DELAY_NEGOTIATED_DATA
        {
            public bool SendNegotiated;             // TRUE if sending one-way delay timestamps is negotiated.
            public bool ReceiveNegotiated;          // TRUE if receiving one-way delay timestamps is negotiated.
        }
        public class NETWORK_STATISTICS_DATA
        {
            public int BytesInFlight;              // Bytes that were sent on the wire, but not yet acked
            public long PostedBytes;                // Total bytes queued, but not yet acked. These may contain sent bytes that may have portentially lost too.
            public long IdealBytes;                 // Ideal number of bytes required to be available to  avoid limiting throughput
            public long SmoothedRTT;                // Smoothed RTT value
            public int CongestionWindow;           // Congestion Window
            public long Bandwidth;                  // Estimated bandwidth
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

        public const uint QUIC_EXECUTION_CONFIG_FLAG_NONE = 0x0000;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_QTIP = 0x0001;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_RIO = 0x0002;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_XDP = 0x0004;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_NO_IDEAL_PROC = 0x0008;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_HIGH_PRIORITY = 0x0010;
        public const uint QUIC_EXECUTION_CONFIG_FLAG_AFFINITIZE = 0x0020;
        
        public const byte QUIC_FLOW_BLOCKED_SCHEDULING = 0x01;
        public const byte QUIC_FLOW_BLOCKED_PACING = 0x02;
        public const byte QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT = 0x04;
        public const byte QUIC_FLOW_BLOCKED_CONGESTION_CONTROL = 0x08;
        public const byte QUIC_FLOW_BLOCKED_CONN_FLOW_CONTROL = 0x10;
        public const byte QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL = 0x20;
        public const byte QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL = 0x40;
        public const byte QUIC_FLOW_BLOCKED_APP = 0x80;

        static bool QuicAddrCompare(QUIC_ADDR Addr1, QUIC_ADDR Addr2)
        {
            if (Addr1.AddressFamily != Addr2.AddressFamily || Addr1.nPort != Addr2.nPort)
            {
                return false;
            }
            return QuicAddrCompareIp(Addr1, Addr2);
        }

        static bool QuicAddrCompareIp(QUIC_ADDR Addr1, QUIC_ADDR Addr2)
        {
            return Addr1.Ip == Addr2.Ip;
        }

        static int QuicAddrGetPort(QUIC_ADDR Addr)
        {
            return Addr.nPort;
        }

        static void QuicAddrSetPort(QUIC_ADDR Addr, int Port)
        {
            Addr.nPort = Port;
        }


        static void UPDATE_HASH(uint value, ref uint Hash)
        {
            Hash = (Hash << 5) - Hash + (value);
        }

        static uint QuicAddrHash(QUIC_ADDR Addr)
        {
            uint Hash = 5387;
            UPDATE_HASH((uint)(Addr.nPort & 0xFF), ref Hash);
            UPDATE_HASH((uint)Addr.nPort >> 8, ref Hash);
            byte[] addr_bytes = Addr.Ip.GetAddressBytes();
            for (int i = 0; i < addr_bytes.Length; ++i)
            {
                UPDATE_HASH(addr_bytes[i], ref Hash);
            }
            return Hash;
        }

        static bool QuicAddrIsWildCard(QUIC_ADDR Addr)
        {
            if (Addr.AddressFamily == AddressFamily.Unspecified)
            {
                return true;
            }
            else
            {
                return Addr.Ip == IPAddress.Any;
            }
        }

        static bool QuicAddrIsValid(QUIC_ADDR Addr)
        {
            return Addr.AddressFamily == AddressFamily.Unspecified ||
                Addr.AddressFamily == AddressFamily.InterNetwork ||
                Addr.AddressFamily == AddressFamily.InterNetworkV6;
        }
    }
}
