using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        public const uint VER_MAJOR = 2;
        public const uint VER_MINOR = 5;
        public const uint VER_PATCH = 0;
        public const uint VER_BUILD_ID = 0;

        public static long QUIC_TIME_REORDER_THRESHOLD(long rtt)
        {
            return rtt + (rtt / 8);
        }

        public const int QUIC_INITIAL_RTT = 333; // 毫秒
        public const int QUIC_MIN_INITIAL_PACKET_LENGTH = 1200;
        public const int QUIC_MIN_UDP_PAYLOAD_LENGTH_FOR_VN = QUIC_MIN_INITIAL_PACKET_LENGTH;
        public const int QUIC_INITIAL_WINDOW_PACKETS = 10;
        public const int QUIC_MAX_CONNECTION_ID_LENGTH_INVARIANT = 255;
        public const int QUIC_MAX_CONNECTION_ID_LENGTH_V1 = 20;

        //
        // Minimum number of bytes required for a connection ID in the client"s
        // Initial packet.
        //
        public const int QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH = 8;

        //
        // The amount of packet amplification allowed by the server. Until the
        // client address is validated, a server will send no more than
        // QUIC_AMPLIFICATION_RATIO UDP payload bytes for each received byte.
        //
        public const int QUIC_AMPLIFICATION_RATIO = 3;

        //
        // The max expected reordering in terms of number of packets
        // (for FACK loss detection).
        //
        public const int QUIC_PACKET_REORDER_THRESHOLD = 3;

        //
        // Number of consecutive PTOs after which the network is considered to be
        // experiencing persistent congestion.
        //
        public const int QUIC_PERSISTENT_CONGESTION_THRESHOLD = 2;

        //
        // The number of probe timeouts" worth of time to wait in the closing period
        // before timing out.
        //
        public const int QUIC_CLOSE_PTO_COUNT = 3;

        //
        // The congestion window to use after persistent congestion. TCP uses one
        // packet, but here we use two, as recommended by the QUIC spec.
        //
        public const int QUIC_PERSISTENT_CONGESTION_WINDOW_PACKETS = 2;

        //
        // The minimum number of ACK eliciting packets to receive before overriding ACK
        // delay.
        //
        public const int QUIC_MIN_ACK_SEND_NUMBER = 2;

        //
        // The value for Reordering threshold when no ACK_FREQUENCY frame is received.
        // This means that the receiver will immediately acknowledge any out-of-order packets.
        //
        public const int QUIC_MIN_REORDERING_THRESHOLD = 1;

        //
        // The size of the stateless reset token.
        //
        public const int QUIC_STATELESS_RESET_TOKEN_LENGTH = 16;

        //
        // The minimum length for a stateless reset packet.
        //
        public const int QUIC_MIN_STATELESS_RESET_PACKET_LENGTH = (5 + QUIC_STATELESS_RESET_TOKEN_LENGTH);

        //
        // The recommended (minimum) length for a stateless reset packet so that it is
        // difficult to distinguish from other packets (by middleboxes).
        //
        public const int QUIC_RECOMMENDED_STATELESS_RESET_PACKET_LENGTH = (25 + QUIC_STATELESS_RESET_TOKEN_LENGTH);


        public const int QUIC_MAX_PARTITION_COUNT = 512;

        //
        // The number of partitions (cores) to offset from the receive (RSS) core when
        // using the QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT profile.
        //
        public const int QUIC_MAX_THROUGHPUT_PARTITION_OFFSET = 2; // Two to skip over hyper-threaded cores

        //
        // The fraction ((0 to UINT16_MAX) / UINT16_MAX) of memory that must be
        // exhausted before enabling retry.
        //
        public const int QUIC_DEFAULT_RETRY_MEMORY_FRACTION = 65; // ~0.1%

        //
        // The maximum amount of queue delay a worker should take on (in ms).
        //
        public const int QUIC_MAX_WORKER_QUEUE_DELAY = 250;

        //
        // The maximum number of simultaneous stateless operations that can be queued on
        // a single worker.
        //
        public const int QUIC_MAX_STATELESS_OPERATIONS = 16;

        //
        // The maximum number of simultaneous stateless operations that can be queued on
        // a single binding.
        //
        public const int QUIC_MAX_BINDING_STATELESS_OPERATIONS = 100;

        //
        // The number of milliseconds we keep an entry in the binding stateless
        // operation table before removing it.
        //
        public const int QUIC_STATELESS_OPERATION_EXPIRATION_MS = 100;

        //
        // The maximum number of operations a connection will drain from its queue per
        // call to QuicConnDrainOperations.
        //
        public const int QUIC_MAX_OPERATIONS_PER_DRAIN = 16;

        //
        // Used as a hint for the maximum number of UDP datagrams to send for each
        // FLUSH_SEND operation. The actual number will generally exceed this value up
        // to the limit of the current USO buffer being filled.
        //
        public const int QUIC_MAX_DATAGRAMS_PER_SEND = 40;

        //
        // The number of packets we write for a single stream before going to the next
        // one in the round robin.
        //
        public const int QUIC_STREAM_SEND_BATCH_COUNT = 8;

        //
        // The maximum number of received packets to batch process at a time.
        //
        public const int QUIC_MAX_RECEIVE_BATCH_COUNT = 32;

        //
        // The maximum number of crypto operations to batch.
        //
        public const int QUIC_MAX_CRYPTO_BATCH_COUNT = 8;

        //
        // The maximum number of received packets that may be processed in a single
        // flush operation.
        //
        public const int QUIC_MAX_RECEIVE_FLUSH_COUNT = 100;

        //
        // The maximum number of pending datagrams we will hold on to, per connection,
        // per packet number space. We base our max on the expected initial window size
        // of the peer with a little bit of extra.
        //
        public const int QUIC_MAX_PENDING_DATAGRAMS = (QUIC_INITIAL_WINDOW_PACKETS + 5);

        //
        // The maximum crypto FC window we will use/allow for client buffers.
        //
        public const int QUIC_MAX_TLS_CLIENT_SEND_BUFFER = (4 * 1024);

        //
        // The maximum crypto FC window we will use/allow for server buffers.
        //
        public const int QUIC_MAX_TLS_SERVER_SEND_BUFFER = (8 * 1024);

        //
        // The initial stream FC window size reported to peers.
        //
        public const int QUIC_DEFAULT_STREAM_FC_WINDOW_SIZE = 0x10000;  // 65536

        //
        // The initial stream receive buffer allocation size.
        //
        public const int QUIC_DEFAULT_STREAM_RECV_BUFFER_SIZE = 0x1000;  // 4096

        //
        // The default connection flow control window value, in bytes.
        //
        public const int QUIC_DEFAULT_CONN_FLOW_CONTROL_WINDOW = 0x1000000;  // 16MB

        //
        // Maximum memory allocated (in bytes) for different range tracking structures
        //
        public const int QUIC_MAX_RANGE_ALLOC_SIZE = 0x100000;    // 1084576
        public const int QUIC_MAX_RANGE_DUPLICATE_PACKETS = 0x1000;     // 4096
        public const int QUIC_MAX_RANGE_ACK_PACKETS = 0x800;      // 2048
        public const int QUIC_MAX_RANGE_DECODE_ACKS = 0x1000;      // 4096
        public const int QUIC_DPLPMTUD_MIN_MTU = (QUIC_MIN_INITIAL_PACKET_LENGTH + CXPLAT_MIN_IPV6_HEADER_SIZE + CXPLAT_UDP_HEADER_SIZE);
        public const int QUIC_INITIAL_PACKET_LENGTH = 1240;
        public const int QUIC_DPLPMTUD_DEFAULT_MIN_MTU = (QUIC_INITIAL_PACKET_LENGTH + CXPLAT_MIN_IPV6_HEADER_SIZE + CXPLAT_UDP_HEADER_SIZE);
        public const int QUIC_DPLPMTUD_DEFAULT_MAX_MTU = 1500;
        public const int QUIC_MAX_CALLBACK_TIME_WARNING = MS_TO_US(10);
        public const int QUIC_MAX_CALLBACK_TIME_ERROR = MS_TO_US(1000);
        public const int QUIC_DEFAULT_DISCONNECT_TIMEOUT = 16000;  // 16 seconds, in ms
        public const int QUIC_MAX_DISCONNECT_TIMEOUT = 600000;  // 10 minutes, in ms
        public const int QUIC_DEFAULT_IDLE_TIMEOUT = 30000;
        public const int QUIC_DEFAULT_HANDSHAKE_IDLE_TIMEOUT = 10000;
        public const bool QUIC_DEFAULT_KEEP_ALIVE_ENABLE = false;

        public const int QUIC_DEFAULT_KEEP_ALIVE_INTERVAL = 0;
        public const int QUIC_RECV_BUFFER_DRAIN_RATIO = 4;
        public const bool QUIC_DEFAULT_SEND_BUFFERING_ENABLE = true;

        //
        // The default ideal send buffer size (in bytes).
        //
        public const int QUIC_DEFAULT_IDEAL_SEND_BUFFER_SIZE = 0x20000; // 131072

        //
        // The max ideal send buffer size (in bytes). Note that this is not
        // a hard max on the number of bytes buffered for the connection.
        //
        public const int QUIC_MAX_IDEAL_SEND_BUFFER_SIZE = 0x8000000; // 134217728

        //
        // The minimum number of bytes of send allowance we must have before we will
        // send another packet.
        //
        public const int QUIC_MIN_SEND_ALLOWANCE = 76;  // Magic number to indicate a threshold of "enough" allowance to send another packet.

        //
        // The minimum buffer space that we require before we will pack another
        // compound packet in the UDP payload or stream into a QUIC packet.
        //
        public const int QUIC_MIN_PACKET_SPARE_SPACE = 64;

        //
        // The maximum number of paths a single connection will keep track of.
        //
        public const int QUIC_MAX_PATH_COUNT = 4;
        public const int QUIC_ACTIVE_CONNECTION_ID_LIMIT = 4;
        public const int QUIC_DEFAULT_SEND_PACING = true;
        public const int QUIC_MIN_PACING_RTT = 1000;
        public const int QUIC_SEND_PACING_INTERVAL = 1000;

        public const ulong QUIC_DEFAULT_MAX_BYTES_PER_KEY = 0x4000000000;

        //
        // Default minimum time without any sends before the congestion window is reset.
        //
        public const int QUIC_DEFAULT_SEND_IDLE_TIMEOUT_MS = 1000;

        //
        // The scaling factor used locally for AckDelay field in the ACK_FRAME.
        //
        public const int QUIC_ACK_DELAY_EXPONENT = 8;

        //
        // The lifetime of a QUIC stateless retry token encryption key.
        // This is also the interval that generates new keys.
        //
        public const int QUIC_STATELESS_RETRY_KEY_LIFETIME_MS = 30000;

        //
        // The default value for migration being enabled or not.
        //
        public const bool QUIC_DEFAULT_MIGRATION_ENABLED = true;

        //
        // The default value for load balancing mode.
        //
        public const bool QUIC_DEFAULT_LOAD_BALANCING_MODE = QUIC_LOAD_BALANCING_DISABLED;

        //
        // The default value for datagrams being enabled or not.
        //
        public const bool QUIC_DEFAULT_DATAGRAM_RECEIVE_ENABLED = false;

        //
        // The default max_datagram_frame_length transport parameter value we send. Set
        // to max uint16 to not explicitly limit the length of datagrams.
        //
        public const int QUIC_DEFAULT_MAX_DATAGRAM_LENGTH = 0xFFFF;

        //
        // By default, resumption and 0-RTT are not enabled for servers.
        // If an application want to use these features, it must explicitly enable them.
        //
        public const int QUIC_DEFAULT_SERVER_RESUMPTION_LEVEL = QUIC_SERVER_NO_RESUME;

        //
        // Version of the wire-format for resumption tickets.
        // This needs to be incremented for each change in order or count of fields.
        //
        public const int CXPLAT_TLS_RESUMPTION_TICKET_VERSION = 1;

        //
        // Version of the blob for client resumption tickets.
        // This needs to be incremented for each change in order or count of fields.
        //
        public const int CXPLAT_TLS_RESUMPTION_CLIENT_TICKET_VERSION = 1;

        //
        // By default the Version Negotiation Extension is disabled.
        //
        public const bool QUIC_DEFAULT_VERSION_NEGOTIATION_EXT_ENABLED = false;

        //
        // The AEAD Integrity limit for maximum failed decryption packets over the
        // lifetime of a connection. Set to the lowest limit, which is for
        // AEAD_AES_128_CCM at 2^23.5 (rounded down)
        //
        public const int CXPLAT_AEAD_INTEGRITY_LIMIT = 11863283;

        //
        // Maximum length, in bytes, for a connection_close reason phrase.
        //
        public const int QUIC_MAX_CONN_CLOSE_REASON_LENGTH = 512;

        //
        // The maximum number of probe packets sent before considering an MTU too large.
        //
        public const int QUIC_DPLPMTUD_MAX_PROBES = 3;

        public const int QUIC_DPLPMTUD_RAISE_TIMER_TIMEOUT = S_TO_US(600);

        public const int QUIC_DPLPMTUD_INCREMENT = 80;
        public const int QUIC_CONGESTION_CONTROL_ALGORITHM_DEFAULT = QUIC_CONGESTION_CONTROL_ALGORITHM_CUBIC;
        public const int QUIC_DEFAULT_DEST_CID_UPDATE_IDLE_TIMEOUT_MS = 20000;

        //
        // The default value for enabling grease quic bit extension.
        //
        public const int QUIC_DEFAULT_GREASE_QUIC_BIT_ENABLED = false;

        //
        // The default value for enabling sender-side ECN support.
        //
        public const int QUIC_DEFAULT_ECN_ENABLED = false;

        //
        // The default settings for enabling HyStart support.
        //
        public const int QUIC_DEFAULT_HYSTART_ENABLED = false;

        //
        // The default settings for allowing QEO support.
        //
        public const int QUIC_DEFAULT_ENCRYPTION_OFFLOAD_ALLOWED = false;

        //
        // The default settings for allowing Reliable Reset support.
        //
        public const int QUIC_DEFAULT_RELIABLE_RESET_ENABLED = false;

        //
        // The default settings for allowing One-Way Delay support.
        //
        public const int QUIC_DEFAULT_ONE_WAY_DELAY_ENABLED = false;

        //
        // The default settings for allowing Network Statistics event to be raised.
        //
        public const int QUIC_DEFAULT_NET_STATS_EVENT_ENABLED = false;

        //
        // The default settings for using multiple parallel receives for streams.
        //
        public const int QUIC_DEFAULT_STREAM_MULTI_RECEIVE_ENABLED = false;

        //
        // The number of rounds in Cubic Slow Start to sample RTT.
        //
        public const int QUIC_HYSTART_DEFAULT_N_SAMPLING = 8;

        //
        // The minimum RTT threshold to exit Cubic Slow Start (in microseconds).
        //
        public const int QUIC_HYSTART_DEFAULT_MIN_ETA = 4000;

        //
        // The maximum RTT threshold to exit Cubic Slow Start (in microseconds).
        //
        public const int QUIC_HYSTART_DEFAULT_MAX_ETA = 16000;

        //
        // The number of rounds to spend in Conservative Slow Start before switching
        // to Congestion Avoidance.
        //
        public const int QUIC_CONSERVATIVE_SLOW_START_DEFAULT_ROUNDS = 5;

        //
        // The Congestion Window growth divisor during Conservative Slow Start.
        //
        public const int QUIC_CONSERVATIVE_SLOW_START_DEFAULT_GROWTH_DIVISOR = 4;

        /*************************************************************
                          TRANSPORT PARAMETERS
        *************************************************************/

        public const int QUIC_TP_FLAG_INITIAL_MAX_DATA = 0x00000001;
        public const int QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_LOCAL = 0x00000002;
        public const int QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_REMOTE = 0x00000004;
        public const int QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_UNI = 0x00000008;
        public const int QUIC_TP_FLAG_INITIAL_MAX_STRMS_BIDI = 0x00000010;
        public const int QUIC_TP_FLAG_INITIAL_MAX_STRMS_UNI = 0x00000020;
        public const int QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE = 0x00000040;
        public const int QUIC_TP_FLAG_ACK_DELAY_EXPONENT = 0x00000080;
        public const int QUIC_TP_FLAG_STATELESS_RESET_TOKEN = 0x00000100;
        public const int QUIC_TP_FLAG_PREFERRED_ADDRESS = 0x00000200;
        public const int QUIC_TP_FLAG_DISABLE_ACTIVE_MIGRATION = 0x00000400;
        public const int QUIC_TP_FLAG_IDLE_TIMEOUT = 0x00000800;
        public const int QUIC_TP_FLAG_MAX_ACK_DELAY = 0x00001000;
        public const int QUIC_TP_FLAG_ORIGINAL_DESTINATION_CONNECTION_ID = 0x00002000;
        public const int QUIC_TP_FLAG_ACTIVE_CONNECTION_ID_LIMIT = 0x00004000;
        public const int QUIC_TP_FLAG_MAX_DATAGRAM_FRAME_SIZE = 0x00008000;
        public const int QUIC_TP_FLAG_INITIAL_SOURCE_CONNECTION_ID = 0x00010000;
        public const int QUIC_TP_FLAG_RETRY_SOURCE_CONNECTION_ID = 0x00020000;
        public const int QUIC_TP_FLAG_DISABLE_1RTT_ENCRYPTION = 0x00040000;
        public const int QUIC_TP_FLAG_VERSION_NEGOTIATION = 0x00080000;
        public const int QUIC_TP_FLAG_MIN_ACK_DELAY = 0x00100000;
        public const int QUIC_TP_FLAG_CIBIR_ENCODING = 0x00200000;
        public const int QUIC_TP_FLAG_GREASE_QUIC_BIT = 0x00400000;
        public const int QUIC_TP_FLAG_RELIABLE_RESET_ENABLED = 0x00800000;
        public const int QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED = 0x01000000;
        public const int QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED = 0x02000000;
        public const int QUIC_TP_FLAG_TIMESTAMP_SHIFT = 24;

        public const int QUIC_TP_MAX_PACKET_SIZE_DEFAULT = 65527;
        public const int QUIC_TP_MAX_UDP_PAYLOAD_SIZE_MIN = 1200;
        public const int QUIC_TP_MAX_UDP_PAYLOAD_SIZE_MAX = 65527;

        public const int QUIC_TP_ACK_DELAY_EXPONENT_DEFAULT = 3;
        public const int QUIC_TP_ACK_DELAY_EXPONENT_MAX = 20;

        public const int QUIC_TP_MAX_ACK_DELAY_DEFAULT = 25; // ms
        public const int QUIC_TP_MAX_ACK_DELAY_MAX = ((1 << 14) - 1);
        public const int QUIC_TP_MIN_ACK_DELAY_MAX = ((1 << 24) - 1);

        public const int QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_DEFAULT = 2;
        public const int QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_MIN = 2;

        //
        // Max allowed value of a MAX_STREAMS frame or transport parameter.
        // Any larger value would allow a max stream ID that cannot be expressed
        // as a variable-length integer.
        //
        public const ulong QUIC_TP_MAX_STREAMS_MAX = ((1UL << 60) - 1);

        /*************************************************************
                          PERSISTENT SETTINGS
        *************************************************************/

        public const string QUIC_SETTING_APP_KEY = "Apps\\";

        public const string QUIC_SETTING_MAX_PARTITION_COUNT = "MaxPartitionCount";
        public const string QUIC_SETTING_RETRY_MEMORY_FRACTION = "RetryMemoryFraction";
        public const string QUIC_SETTING_LOAD_BALANCING_MODE = "LoadBalancingMode";
        public const string QUIC_SETTING_FIXED_SERVER_ID = "FixedServerID";
        public const string QUIC_SETTING_MAX_WORKER_QUEUE_DELAY = "MaxWorkerQueueDelayMs";
        public const string QUIC_SETTING_MAX_STATELESS_OPERATIONS = "MaxStatelessOperations";
        public const string QUIC_SETTING_MAX_BINDING_STATELESS_OPERATIONS = "MaxBindingStatelessOperations";
        public const string QUIC_SETTING_STATELESS_OPERATION_EXPIRATION = "StatelessOperationExpirationMs";
        public const string QUIC_SETTING_MAX_OPERATIONS_PER_DRAIN = "MaxOperationsPerDrain";

        public const string QUIC_SETTING_SEND_BUFFERING_DEFAULT = "SendBufferingDefault";
        public const string QUIC_SETTING_SEND_PACING_DEFAULT = "SendPacingDefault";
        public const string QUIC_SETTING_MIGRATION_ENABLED = "MigrationEnabled";
        public const string QUIC_SETTING_DATAGRAM_RECEIVE_ENABLED = "DatagramReceiveEnabled";
        public const string QUIC_SETTING_GREASE_QUIC_BIT_ENABLED = "GreaseQuicBitEnabled";
        public const string QUIC_SETTING_ECN_ENABLED = "EcnEnabled";
        public const string QUIC_SETTING_HYSTART_ENABLED = "HyStartEnabled";
        public const string QUIC_SETTING_ENCRYPTION_OFFLOAD_ALLOWED = "EncryptionOffloadAllowed";
        public const string QUIC_SETTING_RELIABLE_RESET_ENABLED = "ReliableResetEnabled";
        public const string QUIC_SETTING_ONE_WAY_DELAY_ENABLED = "OneWayDelayEnabled";
        public const string QUIC_SETTING_NET_STATS_EVENT_ENABLED = "NetStatsEventEnabled";
        public const string QUIC_SETTING_STREAM_MULTI_RECEIVE_ENABLED = "StreamMultiReceiveEnabled";

        public const string QUIC_SETTING_INITIAL_WINDOW_PACKETS = "InitialWindowPackets";
        public const string QUIC_SETTING_SEND_IDLE_TIMEOUT_MS = "SendIdleTimeoutMs";
        public const string QUIC_SETTING_DEST_CID_UPDATE_IDLE_TIMEOUT_MS = "DestCidUpdateIdleTimeoutMs";

        public const string QUIC_SETTING_INITIAL_RTT = "InitialRttMs";
        public const string QUIC_SETTING_MAX_ACK_DELAY = "MaxAckDelayMs";
        public const string QUIC_SETTING_DISCONNECT_TIMEOUT = "DisconnectTimeoutMs";
        public const string QUIC_SETTING_KEEP_ALIVE_INTERVAL = "KeepAliveIntervalMs";
        public const string QUIC_SETTING_IDLE_TIMEOUT = "IdleTimeoutMs";
        public const string QUIC_SETTING_HANDSHAKE_IDLE_TIMEOUT = "HandshakeIdleTimeoutMs";

        public const string QUIC_SETTING_MAX_TLS_CLIENT_SEND_BUFFER = "TlsClientMaxSendBuffer";
        public const string QUIC_SETTING_MAX_TLS_SERVER_SEND_BUFFER = "TlsServerMaxSendBuffer";
        public const string QUIC_SETTING_STREAM_FC_WINDOW_SIZE = "StreamRecvWindowDefault";
        public const string QUIC_SETTING_STREAM_FC_BIDI_LOCAL_WINDOW_SIZE = "StreamRecvWindowBidiLocalDefault";
        public const string QUIC_SETTING_STREAM_FC_BIDI_REMOTE_WINDOW_SIZE = "StreamRecvWindowBidiRemoteDefault";
        public const string QUIC_SETTING_STREAM_FC_UNIDI_WINDOW_SIZE = "StreamRecvWindowUnidiDefault";
        public const string QUIC_SETTING_STREAM_RECV_BUFFER_SIZE = "StreamRecvBufferDefault";
        public const string QUIC_SETTING_CONN_FLOW_CONTROL_WINDOW = "ConnFlowControlWindow";

        public const string QUIC_SETTING_MAX_BYTES_PER_KEY_PHASE = "MaxBytesPerKey";

        public const string QUIC_SETTING_SERVER_RESUMPTION_LEVEL = "ResumptionLevel";

        public const string QUIC_SETTING_VERSION_NEGOTIATION_EXT_ENABLE = "VersionNegotiationExtEnabled";

        public const string QUIC_SETTING_ACCEPTABLE_VERSIONS = "AcceptableVersions";
        public const string QUIC_SETTING_OFFERED_VERSIONS = "OfferedVersions";
        public const string QUIC_SETTING_FULLY_DEPLOYED_VERSIONS = "FullyDeployedVersions";

        public const string QUIC_SETTING_MINIMUM_MTU = "MinimumMtu";
        public const string QUIC_SETTING_MAXIMUM_MTU = "MaximumMtu";
        public const string QUIC_SETTING_MTU_SEARCH_COMPLETE_TIMEOUT = "MtuDiscoverySearchCompleteTimeoutUs";
        public const string QUIC_SETTING_MTU_MISSING_PROBE_COUNT = "MtuDiscoveryMissingProbeCount";

        public const string QUIC_SETTING_CONGESTION_CONTROL_ALGORITHM = "CongestionControlAlgorithm";

        public const uint QUIC_CONN_SEND_FLAG_ACK = 0x00000001U;
        public const uint QUIC_CONN_SEND_FLAG_CRYPTO = 0x00000002U;
        public const uint QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE = 0x00000004U;
        public const uint QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE = 0x00000008U;
        public const uint QUIC_CONN_SEND_FLAG_DATA_BLOCKED = 0x00000010U;
        public const uint QUIC_CONN_SEND_FLAG_MAX_DATA = 0x00000020U;
        public const uint QUIC_CONN_SEND_FLAG_MAX_STREAMS_BIDI = 0x00000040U;
        public const uint QUIC_CONN_SEND_FLAG_MAX_STREAMS_UNI = 0x00000080U;
        public const uint QUIC_CONN_SEND_FLAG_NEW_CONNECTION_ID = 0x00000100U;
        public const uint QUIC_CONN_SEND_FLAG_RETIRE_CONNECTION_ID = 0x00000200U;
        public const uint QUIC_CONN_SEND_FLAG_PATH_CHALLENGE = 0x00000400U;
        public const uint QUIC_CONN_SEND_FLAG_PATH_RESPONSE = 0x00000800U;
        public const uint QUIC_CONN_SEND_FLAG_PING = 0x00001000U;
        public const uint QUIC_CONN_SEND_FLAG_HANDSHAKE_DONE = 0x00002000U;
        public const uint QUIC_CONN_SEND_FLAG_DATAGRAM = 0x00004000U;
        public const uint QUIC_CONN_SEND_FLAG_ACK_FREQUENCY = 0x00008000U;
        public const uint QUIC_CONN_SEND_FLAG_BIDI_STREAMS_BLOCKED = 0x00010000U;
        public const uint QUIC_CONN_SEND_FLAG_UNI_STREAMS_BLOCKED = 0x00020000U;
        public const uint QUIC_CONN_SEND_FLAG_DPLPMTUD = 0x80000000U;
        public const uint QUIC_CONN_SEND_FLAGS_BYPASS_CC = QUIC_CONN_SEND_FLAG_ACK | QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE | QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE;
        public const uint QUIC_CONN_SEND_FLAG_CONN_CLOSED_MASK =
            QUIC_CONN_SEND_FLAG_DATA_BLOCKED |
            QUIC_CONN_SEND_FLAG_MAX_DATA |
            QUIC_CONN_SEND_FLAG_MAX_STREAMS_BIDI |
            QUIC_CONN_SEND_FLAG_MAX_STREAMS_UNI |
            QUIC_CONN_SEND_FLAG_NEW_CONNECTION_ID |
            QUIC_CONN_SEND_FLAG_RETIRE_CONNECTION_ID |
            QUIC_CONN_SEND_FLAG_PATH_CHALLENGE |
            QUIC_CONN_SEND_FLAG_PATH_RESPONSE |
            QUIC_CONN_SEND_FLAG_PING |
            QUIC_CONN_SEND_FLAG_DATAGRAM |
            QUIC_CONN_SEND_FLAG_ACK_FREQUENCY |
            QUIC_CONN_SEND_FLAG_DPLPMTUD |
            QUIC_CONN_SEND_FLAG_BIDI_STREAMS_BLOCKED |
            QUIC_CONN_SEND_FLAG_UNI_STREAMS_BLOCKED;

        public const uint QUIC_CONN_SEND_FLAG_ALLOWED_HANDSHAKE = QUIC_CONN_SEND_FLAG_ACK |
            QUIC_CONN_SEND_FLAG_CRYPTO |
            QUIC_CONN_SEND_FLAG_CONNECTION_CLOSE |
            QUIC_CONN_SEND_FLAG_APPLICATION_CLOSE |
            QUIC_CONN_SEND_FLAG_PING;


        public const int QUIC_TIMER_WHEEL_INITIAL_SLOT_COUNT = 32;
        public const int QUIC_TIMER_WHEEL_MAX_LOAD_FACTOR = 32;

        public const int QUIC_RANGE_NO_MAX_ALLOC_SIZE = int.MaxValue;
        public const int QUIC_RANGE_USE_BINARY_SEARCH = 1;
        public const int QUIC_RANGE_INITIAL_SUB_COUNT = 8;
        public const long QUIC_VAR_INT_MAX = 1 << 62 - 1;

        public const int QUIC_MAX_FRAMES_PER_PACKET = 12;

        public const ushort CXPLAT_MIN_IPV4_HEADER_SIZE = 20;
        public const ushort CXPLAT_MIN_IPV6_HEADER_SIZE = 40;
        public const ushort CXPLAT_UDP_HEADER_SIZE = 8;
        public const ushort CXPLAT_TCP_HEADER_SIZE = 20;

        public const uint QUIC_CLOSE_SILENT = 0x00000001U; // Don't send connection close or wait for response
        public const uint QUIC_CLOSE_SEND_NOTIFICATION = 0x00000002U; // Send notification to API client
        public const uint QUIC_CLOSE_APPLICATION = 0x00000004U; // Application closed the connection.
        public const uint QUIC_CLOSE_REMOTE = 0x00000008U; // Connection closed remotely.
        public const uint QUIC_CLOSE_QUIC_STATUS = 0x00000010U; // QUIC_STATUS used for closing.
        public const uint QUIC_CLOSE_INTERNAL = QUIC_CLOSE_SEND_NOTIFICATION;
        public const uint QUIC_CLOSE_INTERNAL_SILENT = (QUIC_CLOSE_INTERNAL | QUIC_CLOSE_SILENT);

        public const ulong QUIC_UINT62_MAX = ((1 << 62) - 1);
        public const int QUIC_MAX_ALPN_LENGTH = 255;
        public const int QUIC_MAX_SNI_LENGTH = 65535;
        public const int QUIC_MAX_RESUMPTION_APP_DATA_LENGTH = 1000;

        public const int NUMBER_OF_STREAM_TYPES = 4;
        public const uint STREAM_ID_MASK = 0b11;
        public const uint STREAM_ID_FLAG_IS_CLIENT = 0b00;
        public const uint STREAM_ID_FLAG_IS_SERVER = 0b01;
        public const uint STREAM_ID_FLAG_IS_BI_DIR = 0b00;
        public const uint STREAM_ID_FLAG_IS_UNI_DIR = 0b10;
        public const uint QUIC_STREAM_SHUTDOWN_SILENT = 0x8000;
        public const uint QUIC_STREAM_EVENT_RECEIVE_TLS_INIT = 0xff;
        public const uint QUIC_SEND_FLAG_BUFFERED = 0x80000000;
        public const uint QUIC_SEND_FLAGS_INTERNAL = QUIC_SEND_FLAG_BUFFERED;
        public const uint QUIC_STREAM_PRIORITY_DEFAULT = 0x7FFF;

        public const uint QUIC_ERROR_NO_ERROR = 0x0;
        public const uint QUIC_ERROR_INTERNAL_ERROR = 0x1;
        public const uint QUIC_ERROR_CONNECTION_REFUSED = 0x2;
        public const uint QUIC_ERROR_FLOW_CONTROL_ERROR = 0x3;
        public const uint QUIC_ERROR_STREAM_LIMIT_ERROR = 0x4;
        public const uint QUIC_ERROR_STREAM_STATE_ERROR = 0x5;
        public const uint QUIC_ERROR_FINAL_SIZE_ERROR = 0x6;
        public const uint QUIC_ERROR_FRAME_ENCODING_ERROR = 0x7;
        public const uint QUIC_ERROR_TRANSPORT_PARAMETER_ERROR = 0x8;
        public const uint QUIC_ERROR_PROTOCOL_VIOLATION = 0xA;
        public const uint QUIC_ERROR_APPLICATION_ERROR = 0xB;
        public const uint QUIC_ERROR_CRYPTO_BUFFER_EXCEEDED = 0xD;
        public const uint QUIC_ERROR_KEY_UPDATE_ERROR = 0xE;
        public const uint QUIC_ERROR_AEAD_LIMIT_REACHED = 0xF;
        public const uint QUIC_ERROR_CRYPTO_HANDSHAKE_FAILURE = 0x100 | 40;  // TLS error code for 'handshake_failure'
        public const uint QUIC_ERROR_CRYPTO_USER_CANCELED = 0x100 | 90;  // TLS error code for 'user_canceled'
        public const uint QUIC_ERROR_CRYPTO_NO_APPLICATION_PROTOCOL = 0x100 | 120; // TLS error code for 'no_application_protocol'
        public const uint QUIC_ERROR_VERSION_NEGOTIATION_ERROR = 0x11;

        public const uint QUIC_STREAM_SEND_FLAG_DATA_BLOCKED = 0x0001U;
        public const uint QUIC_STREAM_SEND_FLAG_MAX_DATA = 0x0002U;
        public const uint QUIC_STREAM_SEND_FLAG_SEND_ABORT = 0x0004U;
        public const uint QUIC_STREAM_SEND_FLAG_RECV_ABORT = 0x0008U;
        public const uint QUIC_STREAM_SEND_FLAG_DATA = 0x0010U;
        public const uint QUIC_STREAM_SEND_FLAG_OPEN = 0x0020U;
        public const uint QUIC_STREAM_SEND_FLAG_FIN = 0x0040U;
        public const uint QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT = 0x0080U;

        public const uint QUIC_STREAM_SEND_FLAGS_ALL = 0xFFFFU;
        public const uint QUIC_STREAM_SEND_FLAG_ALL_SEND_PATH = QUIC_STREAM_SEND_FLAG_DATA_BLOCKED | QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT |
            QUIC_STREAM_SEND_FLAG_DATA | QUIC_STREAM_SEND_FLAG_OPEN | QUIC_STREAM_SEND_FLAG_FIN;

        public const int QUIC_TLS_SECRETS_MAX_SECRET_LEN = 64;
        public const long QUIC_PERF_SAMPLE_INTERVAL_S = 1000; // 1 second

        public const string QUIC_POOL_GENERIC = "CIUQ";
        public const string QUIC_POOL_SILO = "00cQ";
        public const string QUIC_POOL_CONN = "10cQ";
        public const string QUIC_POOL_TP = "20cQ";
        public const string QUIC_POOL_STREAM = "30cQ";
        public const string QUIC_POOL_SBUF = "40cQ";
        public const string QUIC_POOL_META = "50cQ";
        public const string QUIC_POOL_DATA = "60cQ";
        public const string QUIC_POOL_TEST = "70cQ";
        public const string QUIC_POOL_PERF = "80cQ";
        public const string QUIC_POOL_TOOL = "90cQ";
        public const string QUIC_POOL_WORKER = "A0cQ";
        public const string QUIC_POOL_LISTENER = "B0cQ";
        public const string QUIC_POOL_CID = "C0cQ";
        public const string QUIC_POOL_CIDHASH = "D0cQ";
        public const string QUIC_POOL_CIDLIST = "E0cQ";
        public const string QUIC_POOL__UNUSED_1_ = "F0cQ";
        public const string QUIC_POOL_ALPN = "01cQ";
        public const string QUIC_POOL_RANGE = "11cQ";
        public const string QUIC_POOL_SENDBUF = "21cQ";
        public const string QUIC_POOL_RECVBUF = "31cQ";
        public const string QUIC_POOL_TIMERWHEEL = "41cQ";
        public const string QUIC_POOL_REGISTRATION = "51cQ";
        public const string QUIC_POOL_CONFIG = "61cQ";
        public const string QUIC_POOL_BINDING = "71cQ";
        public const string QUIC_POOL_API = "81cQ";
        public const string QUIC_POOL_PERPROC = "91cQ";
        public const string QUIC_POOL_PLATFORM_SENDCTX = "A1cQ";
        public const string QUIC_POOL_TLS_ACHCTX = "B1cQ";
        public const string QUIC_POOL_TLS_SNI = "C1cQ";
        public const string QUIC_POOL_TLS_PRINCIPAL = "D1cQ";
        public const string QUIC_POOL_TLS_CTX = "E1cQ";
        public const string QUIC_POOL_TLS_TRANSPARAMS = "F1cQ";
        public const string QUIC_POOL_CUSTOM_THREAD = "02cQ";
        public const string QUIC_POOL_TLS_SECCONF = "12cQ";
        public const string QUIC_POOL_TLS_PACKETKEY = "22cQ";
        public const string QUIC_POOL_TLS_KEY = "32cQ";
        public const string QUIC_POOL_TLS_HP_KEY = "42cQ";
        public const string QUIC_POOL_TLS_HASH = "52cQ";
        public const string QUIC_POOL_TLS_EXTRAS = "62cQ";
        public const string QUIC_POOL_TMP_ALLOC = "72cQ";
        public const string QUIC_POOL_PLATFORM_TMP_ALLOC = "82cQ";
        public const string QUIC_POOL_PLATFORM_PROC = "92cQ";
        public const string QUIC_POOL_PLATFORM_GENERIC = "A2cQ";
        public const string QUIC_POOL_DATAPATH = "B2cQ";
        public const string QUIC_POOL_SOCKET = "C2cQ";
        public const string QUIC_POOL_STORAGE = "D2cQ";
        public const string QUIC_POOL_HASHTABLE = "E2cQ";
        public const string QUIC_POOL_HASHTABLE_MEMBER = "F2cQ";
        public const string QUIC_POOL_LOOKUP_HASHTABLE = "03cQ";
        public const string QUIC_POOL_REMOTE_HASH = "13cQ";
        public const string QUIC_POOL_SERVERNAME = "23cQ";
        public const string QUIC_POOL_APP_RESUMPTION_DATA = "33cQ";
        public const string QUIC_POOL_INITIAL_TOKEN = "43cQ";
        public const string QUIC_POOL_CLOSE_REASON = "53cQ";
        public const string QUIC_POOL_SERVER_CRYPTO_TICKET = "63cQ";
        public const string QUIC_POOL_CLIENT_CRYPTO_TICKET = "73cQ";
        public const string QUIC_POOL_CRYPTO_RESUMPTION_TICKET = "83cQ";
        public const string QUIC_POOL_TLS_BUFFER = "93cQ";
        public const string QUIC_POOL_SEND_REQUEST = "A3cQ";
        public const string QUIC_POOL_API_CTX = "B3cQ";
        public const string QUIC_POOL_STATELESS_CTX = "C3cQ";
        public const string QUIC_POOL_OPER = "D3cQ";
        public const string QUIC_POOL_EVENT = "E3cQ";
        public const string QUIC_POOL_TLS_PFX = "F3cQ";
        public const string QUIC_POOL_VERSION_SETTINGS = "04cQ";
        public const string QUIC_POOL_DEFAULT_COMPAT_VER_LIST = "14cQ";
        public const string QUIC_POOL_VERSION_INFO = "24cQ";
        public const string QUIC_POOL_PROCESS = "34cQ";
        public const string QUIC_POOL_TLS_TMP_TP = "44cQ";
        public const string QUIC_POOL_PCP = "54cQ";
        public const string QUIC_POOL_DATAPATH_ADDRESSES = "64cQ";
        public const string QUIC_POOL_TLS_TICKET_KEY = "74cQ";
        public const string QUIC_POOL_TLS_CIPHER_SUITE_STRING = "84cQ";
        public const string QUIC_POOL_PLATFORM_WORKER = "94cQ";
        public const string QUIC_POOL_ROUTE_RESOLUTION_WORKER = "A4cQ";
        public const string QUIC_POOL_ROUTE_RESOLUTION_OPER = "B4cQ";
        public const string QUIC_POOL_EXECUTION_CONFIG = "C4cQ";
        public const string QUIC_POOL_APP_BUFFER_CHUNK = "D4cQ";

        public const uint QUIC_PARAM_PREFIX_GLOBAL = 0x01000000;
        public const uint QUIC_PARAM_PREFIX_REGISTRATION = 0x02000000;
        public const uint QUIC_PARAM_PREFIX_CONFIGURATION = 0x03000000;
        public const uint QUIC_PARAM_PREFIX_LISTENER = 0x04000000;
        public const uint QUIC_PARAM_PREFIX_CONNECTION = 0x05000000;
        public const uint QUIC_PARAM_PREFIX_TLS = 0x06000000;
        public const uint QUIC_PARAM_PREFIX_TLS_SCHANNEL = 0x07000000;
        public const uint QUIC_PARAM_PREFIX_STREAM = 0x08000000;
        public const uint QUIC_PARAM_HIGH_PRIORITY = 0x40000000; // Combine with any param to make it high priority.

        public const uint QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT = 0x01000000;  // uint16_t
        public const uint QUIC_PARAM_GLOBAL_SUPPORTED_VERSIONS = 0x01000001;  // uint32_t[] - network byte order
        public const uint QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE = 0x01000002;  // uint16_t - QUIC_LOAD_BALANCING_MODE
        public const uint QUIC_PARAM_GLOBAL_PERF_COUNTERS = 0x01000003;  // uint64_t[] - Array size is QUIC_PERF_COUNTER_MAX
        public const uint QUIC_PARAM_GLOBAL_LIBRARY_VERSION = 0x01000004;  // uint32_t[4]
        public const uint QUIC_PARAM_GLOBAL_SETTINGS = 0x01000005;  // QUIC_SETTINGS
        public const uint QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS = 0x01000006;  // QUIC_GLOBAL_SETTINGS
        public const uint QUIC_PARAM_GLOBAL_VERSION_SETTINGS = 0x01000007;  // QUIC_VERSION_SETTINGS
        public const uint QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH = 0x01000008;  // char[64]
        public const uint QUIC_PARAM_GLOBAL_EXECUTION_CONFIG = 0x01000009;  // QUIC_EXECUTION_CONFIG
        public const uint QUIC_PARAM_GLOBAL_TLS_PROVIDER = 0x0100000A;  // QUIC_TLS_PROVIDER
        public const uint QUIC_PARAM_GLOBAL_STATELESS_RESET_KEY = 0x0100000B;  // uint8_t[] - Array size is QUIC_STATELESS_RESET_KEY_LENGTH

        public const uint QUIC_PARAM_CONFIGURATION_SETTINGS = 0x03000000;  // QUIC_SETTINGS
        public const uint QUIC_PARAM_CONFIGURATION_TICKET_KEYS = 0x03000001; // QUIC_TICKET_KEY_CONFIG[]
        public const uint QUIC_PARAM_CONFIGURATION_VERSION_SETTINGS = 0x03000002;  // QUIC_VERSION_SETTINGS

        public const uint QUIC_PARAM_LISTENER_LOCAL_ADDRESS = 0x04000000;  // QUIC_ADDR
        public const uint QUIC_PARAM_LISTENER_STATS = 0x04000001;  // QUIC_LISTENER_STATISTICS
        public const uint QUIC_PARAM_LISTENER_CIBIR_ID = 0x04000002;  // uint8_t[] {offset, id[]}

        public const uint QUIC_PARAM_DOS_MODE_EVENTS = 0x04000004;  // BOOLEAN
        public const uint QUIC_PARAM_CONN_QUIC_VERSION = 0x05000000;  // uint32_t
        public const uint QUIC_PARAM_CONN_LOCAL_ADDRESS = 0x05000001; // QUIC_ADDR
        public const uint QUIC_PARAM_CONN_REMOTE_ADDRESS = 0x05000002;  // QUIC_ADDR
        public const uint QUIC_PARAM_CONN_IDEAL_PROCESSOR = 0x05000003;  // uint16_t
        public const uint QUIC_PARAM_CONN_SETTINGS = 0x05000004;  // QUIC_SETTINGS
        public const uint QUIC_PARAM_CONN_STATISTICS = 0x05000005; // QUIC_STATISTICS
        public const uint QUIC_PARAM_CONN_STATISTICS_PLAT = 0x05000006;  // QUIC_STATISTICS
        public const uint QUIC_PARAM_CONN_SHARE_UDP_BINDING = 0x05000007;  // uint8_t (BOOLEAN)
        public const uint QUIC_PARAM_CONN_LOCAL_BIDI_STREAM_COUNT = 0x05000008;  // uint16_t
        public const uint QUIC_PARAM_CONN_LOCAL_UNIDI_STREAM_COUNT = 0x05000009;  // uint16_t
        public const uint QUIC_PARAM_CONN_MAX_STREAM_IDS = 0x0500000A;  // uint64_t[4]
        public const uint QUIC_PARAM_CONN_CLOSE_REASON_PHRASE = 0x0500000B;  // char[]
        public const uint QUIC_PARAM_CONN_STREAM_SCHEDULING_SCHEME = 0x0500000C;  // QUIC_STREAM_SCHEDULING_SCHEME
        public const uint QUIC_PARAM_CONN_DATAGRAM_RECEIVE_ENABLED = 0x0500000D;  // uint8_t (BOOLEAN)
        public const uint QUIC_PARAM_CONN_DATAGRAM_SEND_ENABLED = 0x0500000E;  // uint8_t (BOOLEAN)
        public const uint QUIC_PARAM_CONN_DISABLE_1RTT_ENCRYPTION = 0x0500000F;  // uint8_t (BOOLEAN)
        public const uint QUIC_PARAM_CONN_RESUMPTION_TICKET = 0x05000010;  // uint8_t[]
        public const uint QUIC_PARAM_CONN_PEER_CERTIFICATE_VALID = 0x05000011;  // uint8_t (BOOLEAN)
        public const uint QUIC_PARAM_CONN_LOCAL_INTERFACE = 0x05000012;  // uint32_t
        public const uint QUIC_PARAM_CONN_TLS_SECRETS = 0x05000013;  // QUIC_TLS_SECRETS (SSLKEYLOGFILE compatible)
        public const uint QUIC_PARAM_CONN_VERSION_SETTINGS = 0x05000014;  // QUIC_VERSION_SETTINGS
        public const uint QUIC_PARAM_CONN_CIBIR_ID = 0x05000015;  // uint8_t[] {offset, id[]}
        public const uint QUIC_PARAM_CONN_STATISTICS_V2 = 0x05000016;  // QUIC_STATISTICS_V2
        public const uint QUIC_PARAM_CONN_STATISTICS_V2_PLAT = 0x05000017;  // QUIC_STATISTICS_V2
        public const uint QUIC_PARAM_CONN_ORIG_DEST_CID = 0x05000018;  // uint8_t[]
        public const uint QUIC_PARAM_CONN_SEND_DSCP = 0x05000019;  // uint8_t
        public const uint QUIC_PARAM_TLS_HANDSHAKE_INFO = 0x06000000;  // QUIC_HANDSHAKE_INFO
        public const uint QUIC_PARAM_TLS_NEGOTIATED_ALPN = 0x06000001;  // uint8_t[] (max 255 bytes)
        public const uint QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_W = 0x07000000;  // QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_W
        public const uint QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W = 0x07000001;  // QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W
        public const uint QUIC_PARAM_TLS_SCHANNEL_SECURITY_CONTEXT_TOKEN = 0x07000002;  // HANDLE
        public const uint QUIC_PARAM_STREAM_ID = 0x08000000;  // QUIC_UINT62
        public const uint QUIC_PARAM_STREAM_0RTT_LENGTH = 0x08000001;  // uint64_t
        public const uint QUIC_PARAM_STREAM_IDEAL_SEND_BUFFER_SIZE = 0x08000002; // uint64_t - bytes
        public const uint QUIC_PARAM_STREAM_PRIORITY = 0x08000003; // uint16_t - 0 (low) to 0xFFFF (high) - 0x7FFF (default)
        public const uint QUIC_PARAM_STREAM_STATISTICS = 0X08000004;  // QUIC_STREAM_STATISTICS
        public const uint QUIC_PARAM_STREAM_RELIABLE_OFFSET = 0x08000005;  // uint64_t
        
        public const uint QUIC_CONN_HANDSHAKE_MEMORY_USAGE = 1000;
        public const uint CXPLAT_SOCKET_FLAG_PCP = 0x00000001;  // Socket is used for internal PCP support
        public const uint CXPLAT_SOCKET_FLAG_SHARE = 0x00000002;  // Forces sharing of the address and port
        public const uint CXPLAT_SOCKET_SERVER_OWNED = 0x00000004; // Indicates socket is a listener socket

        public const ushort CXPLAT_MAX_MTU = 1500;
        public const uint CXPLAT_DATAPATH_FEATURE_RECV_SIDE_SCALING = 0x0001;
        public const uint CXPLAT_DATAPATH_FEATURE_RECV_COALESCING = 0x0002;
        public const uint CXPLAT_DATAPATH_FEATURE_SEND_SEGMENTATION = 0x0004;
        public const uint CXPLAT_DATAPATH_FEATURE_LOCAL_PORT_SHARING = 0x0008;
        public const uint CXPLAT_DATAPATH_FEATURE_PORT_RESERVATIONS = 0x0010;
        public const uint CXPLAT_DATAPATH_FEATURE_TCP = 0x0020;
        public const uint CXPLAT_DATAPATH_FEATURE_RAW = 0x0040;


        public const int UDP_SEND_MSG_SIZE = 2;
        public const int UDP_RECV_MAX_COALESCED_SIZE = 3;
        public const int UDP_COALESCED_INFO = 3;
        public const int CXPLAT_MAX_BATCH_SEND = 1;
        public const int MAX_RECV_PAYLOAD_LENGTH = (CXPLAT_MAX_MTU - CXPLAT_MIN_IPV4_HEADER_SIZE - CXPLAT_UDP_HEADER_SIZE);
        public const int MAX_URO_PAYLOAD_LENGTH = (ushort.MaxValue - CXPLAT_UDP_HEADER_SIZE);
        public const int CXPLAT_LARGE_SEND_BUFFER_SIZE = 0xFFFF;
        public const int URO_MAX_DATAGRAMS_PER_INDICATION = 64;
        public const int RIO_RECV_QUEUE_DEPTH = 256;
        public const int RIO_MAX_RECV_POOL_SIZE = 8192;
        public const int RIO_SEND_QUEUE_DEPTH = 256;
        public const int RIO_MAX_SEND_POOL_SIZE = 8192;

        public const int CXPLAT_CQE_TYPE_QUIC_BASE = 0x8000; // to 0xFFFF
        public const int CXPLAT_CQE_TYPE_WORKER_WAKE = CXPLAT_CQE_TYPE_QUIC_BASE + 1;
        public const int CXPLAT_CQE_TYPE_WORKER_UPDATE_POLL = CXPLAT_CQE_TYPE_QUIC_BASE + 2;
        public const int CXPLAT_CQE_TYPE_SOCKET_SHUTDOWN = CXPLAT_CQE_TYPE_QUIC_BASE + 3;
        public const int CXPLAT_CQE_TYPE_SOCKET_IO = CXPLAT_CQE_TYPE_QUIC_BASE + 4;
        public const int CXPLAT_CQE_TYPE_SOCKET_FLUSH_TX = CXPLAT_CQE_TYPE_QUIC_BASE + 5;
        public const int CXPLAT_CQE_TYPE_XDP_SHUTDOWN = CXPLAT_CQE_TYPE_QUIC_BASE + 6;
        public const int CXPLAT_CQE_TYPE_XDP_IO = CXPLAT_CQE_TYPE_QUIC_BASE + 7;
        public const int CXPLAT_CQE_TYPE_XDP_FLUSH_TX = CXPLAT_CQE_TYPE_QUIC_BASE + 8;

        public const int SEND_PACKET_SHORT_HEADER_TYPE = 0xff;
        public const byte QUIC_SENT_FRAME_FLAG_STREAM_OPEN = 0x01; // STREAM frame opened stream
        public const byte QUIC_SENT_FRAME_FLAG_STREAM_FIN = 0x02;   // STREAM frame included FIN bit

        public const int QUIC_STATELESS_RESET_KEY_LENGTH = 32;

        public const uint CXPLAT_SEND_FLAGS_NONE = 0;
        public const uint CXPLAT_SEND_FLAGS_MAX_THROUGHPUT = 1;
        public const int MIN_SHORT_HEADER_LENGTH_V1 = sizeof_QUIC_SHORT_HEADER_V1 + 4 * sizeof(byte);
        public const int DATAGRAM_FRAME_HEADER_LENGTH = 3;


        static bool QUIC_PARAM_IS_GLOBAL(uint Param)
        {
            return (Param & 0x3F000000) == QUIC_PARAM_PREFIX_GLOBAL;
        }

        static uint QUIC_ERROR_CRYPTO_ERROR(uint TlsAlertCode)
        {
            return 0x100 | TlsAlertCode;
        }

        static bool IS_QUIC_CRYPTO_ERROR(ulong QuicCryptoError)
        {
            return (QuicCryptoError & 0xFF00) == 0x100;
        }

        static int QUIC_DATAGRAM_OVERHEAD(int CidLength)
        {
            return MIN_SHORT_HEADER_LENGTH_V1 + (CidLength) + DATAGRAM_FRAME_HEADER_LENGTH;
        }
    }

}
