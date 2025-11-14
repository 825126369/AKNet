/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Platform;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MSQuic2
{
    internal delegate int QUIC_LISTENER_CALLBACK(QUIC_LISTENER Listener, object Context, ref QUIC_LISTENER_EVENT Info);
    internal delegate int QUIC_STREAM_CALLBACK(QUIC_STREAM Stream, object Context, ref QUIC_STREAM_EVENT Event);
    internal delegate int QUIC_CONNECTION_CALLBACK(QUIC_CONNECTION Connection, object Contex, ref QUIC_CONNECTION_EVENT Event);

    internal enum QUIC_LOAD_BALANCING_MODE
    {
        QUIC_LOAD_BALANCING_DISABLED,               // Default
        QUIC_LOAD_BALANCING_SERVER_ID_IP,           // Encodes IP address in Server ID
        QUIC_LOAD_BALANCING_SERVER_ID_FIXED,        // Encodes a fixed 4-byte value in Server ID
        QUIC_LOAD_BALANCING_COUNT,                  // The number of supported load balancing modes
    }

    internal enum QUIC_STREAM_EVENT_TYPE:byte
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
        QUIC_PERF_COUNTER_CONN_CREATED,         // 自 QUIC 实例启动以来，创建的总连接数。
        QUIC_PERF_COUNTER_CONN_HANDSHAKE_FAIL,  // 在TLS握手阶段失败的连接总数。
        QUIC_PERF_COUNTER_CONN_APP_REJECT,      // 被应用程序层主动拒绝的连接尝试总数
        QUIC_PERF_COUNTER_CONN_RESUMED,         // 成功恢复的连接总数
        QUIC_PERF_COUNTER_CONN_ACTIVE,          // 当前处于活动状态（已分配）的连接数。
        QUIC_PERF_COUNTER_CONN_CONNECTED,       // 当前处于“已连接”状态（即握手完成，数据可以传输）的连接数。
        QUIC_PERF_COUNTER_CONN_PROTOCOL_ERRORS, // 因协议错误（如无效帧、违反状态机规则）而关闭的连接总数。
        QUIC_PERF_COUNTER_CONN_NO_ALPN,         // 因客户端和服务器无法就应用层协议（如 h3(自定义的协议))，达成一致而被拒绝的连接尝试总数。
        QUIC_PERF_COUNTER_STRM_ACTIVE,          // 当前处于活动状态的流（stream）总数。
        QUIC_PERF_COUNTER_PKTS_SUSPECTED_LOST,  // 根据 ACK 信息推断出的疑似丢失的数据包总数。这是网络质量（特别是丢包率）的关键指标。
        QUIC_PERF_COUNTER_PKTS_DROPPED,         // 因任何原因（格式错误、缓冲区溢出）被 QUIC 实现直接丢弃的数据包总数。
        QUIC_PERF_COUNTER_PKTS_DECRYPTION_FAIL, // 解密失败的数据包总数。这可能由密钥错误、数据损坏或重放攻击引起。
        QUIC_PERF_COUNTER_UDP_RECV,             // 接收到的 UDP 数据报总数。
        QUIC_PERF_COUNTER_UDP_SEND,             // 发送的 UDP 数据报总数。
        QUIC_PERF_COUNTER_UDP_RECV_BYTES,       // 接收到的 UDP 负载（payload）总字节数（不包括 UDP/IP 头）。
        QUIC_PERF_COUNTER_UDP_SEND_BYTES,       // 发送的 UDP 负载（payload）总字节数（不包括 UDP/IP 头）。
        QUIC_PERF_COUNTER_UDP_RECV_EVENTS,      // 从底层网络接收到 UDP 数据报的事件总数（可能一次事件接收多个数据报）。
        QUIC_PERF_COUNTER_UDP_SEND_CALLS,       // 调用底层 UDP 发送 API 的总次数（一次调用可能发送多个数据报）。
        QUIC_PERF_COUNTER_APP_SEND_BYTES,       // 应用程序通过 QUIC 连接发送的总字节数（在 QUIC 层之上测量）。
        QUIC_PERF_COUNTER_APP_RECV_BYTES,       //  应用程序通过 QUIC 连接接收到的总字节数
        QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH,     // 当前排队等待处理的连接数（例如，新连接请求）
        QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH,// 当前排队等待处理的连接级操作数。
        QUIC_PERF_COUNTER_CONN_OPER_QUEUED,     // 历史累计的连接级操作入队总数。
        QUIC_PERF_COUNTER_CONN_OPER_COMPLETED,  // 历史累计的已完成连接级操作总数。
        QUIC_PERF_COUNTER_WORK_OPER_QUEUE_DEPTH,// 当前排队等待处理的工作线程操作数。
        QUIC_PERF_COUNTER_WORK_OPER_QUEUED,     // 历史累计的工作线程操作入队总数。
        QUIC_PERF_COUNTER_WORK_OPER_COMPLETED,  // 历史累计的已完成工作线程操作总数。
        QUIC_PERF_COUNTER_PATH_VALIDATED,       // 成功验证的网络路径（IP地址/端口对）挑战总数。
        QUIC_PERF_COUNTER_PATH_FAILURE,         // 验证失败的网络路径挑战总数。
        QUIC_PERF_COUNTER_SEND_STATELESS_RESET, // 发送的无状态重置（stateless reset）数据包总数。
        QUIC_PERF_COUNTER_SEND_STATELESS_RETRY, // 发送的无状态重试（stateless retry）数据包总数。
        QUIC_PERF_COUNTER_CONN_LOAD_REJECT,     // 因QUIC实例内部工作负载过高（例如，CPU、内存或队列满）而被拒绝的连接总数。这是系统过载的信号。
        QUIC_PERF_COUNTER_MAX,                  // 这是一个占位符，表示枚举中有效计数器的最大数量
    }

    internal enum QUIC_CREDENTIAL_FLAGS
    {
        QUIC_CREDENTIAL_FLAG_NONE = 0x00000000,
        QUIC_CREDENTIAL_FLAG_CLIENT = 0x00000001, // 表示这是一个客户端凭据；否则是服务器端
        QUIC_CREDENTIAL_FLAG_LOAD_ASYNCHRONOUS = 0x00000002, //异步加载证书
        QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION = 0x00000004, //不进行证书验证（测试用途，不推荐生产环境使用）
        QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED = 0x00000010,//接收到证书时通知应用层（用于调试或自定义处理）
        QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION = 0x00000020,//	延迟证书验证到应用层处理
        QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION = 0x00000040, //要求客户端身份验证（双向 TLS）
        QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION = 0x00000080, // 使用内置的 TLS 证书验证逻辑（OpenSSL 专用）
        QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES = 0x00002000,//显式设置允许的加密套件
        QUIC_CREDENTIAL_FLAG_USE_PORTABLE_CERTIFICATES = 0x00004000,//使用可移植格式的证书（跨平台兼容）
        QUIC_CREDENTIAL_FLAG_SET_CA_CERTIFICATE_FILE = 0x00100000, // 设置信任的 CA 证书文件路径（OpenSSL 专用）
    }

    internal enum QUIC_EXECUTION_PROFILE
    {
        //目的: 这是默认配置文件，旨在最小化操作延迟和响应时间。
        //适用场景: 交互式应用，如网页浏览、在线游戏、实时通信（VoIP、视频会议）、远程桌面等，这些应用对延迟非常敏感。
        QUIC_EXECUTION_PROFILE_LOW_LATENCY,         // Default

        //目的: 旨在最大化数据传输速率和整体带宽利用率。
        //适用场景: 大文件下载、视频流（非实时）、备份、数据同步等，这些应用更关心在单位时间内传输尽可能多的数据，可以容忍稍高的延迟。
        QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT,

        //目的: 以最低的优先级运行，仅在系统资源（如 CPU 和网络带宽）空闲时才使用它们。其目标是“捡拾”其他高优先级任务留下的空闲资源。
        //适用场景: 后台更新、非紧急的数据同步、日志上传、预取等，这些任务不紧急，不应影响前台用户体验。
        QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER,

        //目的: 为需要严格、可预测延迟保证的任务提供最高级别的服务质量（QoS）。这通常需要操作系统级别的支持（如实时调度类）。
        //适用场景: 极其关键的实时应用，如工业控制、高精度金融交易、某些类型的实时音视频处理，这些应用要求操作在确定的时间内完成。
        QUIC_EXECUTION_PROFILE_TYPE_REAL_TIME,

        // 目的: 这个配置文件很可能是为 QUIC 库内部使用而保留的。它可能用于特定的测试、调试场景，或者表示一种未明确分类的默认/基础状态。
        // 适用场景: 开发、测试或当没有明确的性能目标时。在生产环境中，通常建议使用前四种更明确的配置文件之一。
        QUIC_EXECUTION_PROFILE_TYPE_INTERNAL,
    }

    internal enum QUIC_STREAM_SHUTDOWN_FLAGS
    {
        QUIC_STREAM_SHUTDOWN_FLAG_NONE = 0x0000,
        QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL = 0x0001,   // Cleanly closes the send path.
        QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND = 0x0002,   // Abruptly closes the send path.
        QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE = 0x0004,   // Abruptly closes the receive path.
        QUIC_STREAM_SHUTDOWN_FLAG_ABORT = 0x0006,   // Abruptly closes both send and receive paths.
        QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE = 0x0008,   // Immediately sends completion events to app.
        QUIC_STREAM_SHUTDOWN_FLAG_INLINE = 0x0010,   // Process the shutdown immediately inline. Only for calls on callbacks.
    }

    internal enum QUIC_STREAM_OPEN_FLAGS
    {
        QUIC_STREAM_OPEN_FLAG_NONE = 0x0000,
        QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL = 0x0001,   // Indicates the stream is unidirectional.
        QUIC_STREAM_OPEN_FLAG_0_RTT = 0x0002,   // The stream was opened via a 0-RTT packet.
        QUIC_STREAM_OPEN_FLAG_DELAY_ID_FC_UPDATES = 0x0004, // Indicates stream ID flow control limit updates for the
                                                            // connection should be delayed to StreamClose.
    }

    internal enum QUIC_STREAM_START_FLAGS
    {
        QUIC_STREAM_START_FLAG_NONE = 0x0000,
        QUIC_STREAM_START_FLAG_IMMEDIATE = 0x0001,   // Immediately informs peer that stream is open.
        QUIC_STREAM_START_FLAG_FAIL_BLOCKED = 0x0002,   // Only opens the stream if flow control allows.
        QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL = 0x0004,   // Shutdown the stream immediately after start failure.
        QUIC_STREAM_START_FLAG_INDICATE_PEER_ACCEPT = 0x0008,   // Indicate PEER_ACCEPTED event if not accepted at start.
        QUIC_STREAM_START_FLAG_PRIORITY_WORK = 0x0010,   // Higher priority than other connection work.
    }

    
    internal enum QUIC_SEND_FLAGS:uint
    {
        QUIC_SEND_FLAG_NONE = 0x0000, 
        QUIC_SEND_FLAG_ALLOW_0_RTT = 0x0001,  //允许使用 0-RTT（零往返时间）加密密钥发送数据。这可以加快连接建立过程，适用于已知服务器密钥的客户端快速发送数据。
        QUIC_SEND_FLAG_START = 0x0002,   //异步启动流，并将当前数据作为流的开始部分发送。适用于初始化一个流并同时发送初始数据。
        QUIC_SEND_FLAG_FIN = 0x0004,   // 表示这是流上最后要发送的数据。相当于 TCP 中的 FIN 标志，用于关闭写端
        QUIC_SEND_FLAG_DGRAM_PRIORITY = 0x0008,   // 表示这个 UDP 数据报（datagram）比其他数据报具有更高的优先级，应被优先处理。
        QUIC_SEND_FLAG_DELAY_SEND = 0x0010,   // 表示此次发送操作应该延迟执行，因为预计很快会有更多数据被加入队列，以提高传输效率。
        QUIC_SEND_FLAG_CANCEL_ON_LOSS = 0x0020,   //如果检测到数据包丢失，则取消此流的发送。适用于对可靠性要求不高的场景。
        QUIC_SEND_FLAG_PRIORITY_WORK = 0x0040,   // 表示这项发送任务比其他连接上的工作具有更高的优先级，应优先调度。
        QUIC_SEND_FLAG_CANCEL_ON_BLOCKED = 0x0080,  //如果当前帧不能立即发送（例如由于流量控制限制），则直接丢弃该帧。

        //表示该发送请求的数据已经被 QUIC 栈“保留”，而不是已经完全发送并释放。
        //通常，当数据不能立即发送（比如因为流量控制窗口不足、拥塞控制限制、或者需要等待 ACK 确认），栈会将这个请求缓存起来，并设置此标志。
        //在后续条件允许时（如收到 ACK、窗口更新等），栈会再次尝试发送这些“buffered”的请求
        QUIC_SEND_FLAG_BUFFERED = 0x80000000,
    }

    internal enum QUIC_STREAM_SCHEDULING_SCHEME
    {
        QUIC_STREAM_SCHEDULING_SCHEME_FIFO = 0x0000, // 流调度策略 先进先出
        QUIC_STREAM_SCHEDULING_SCHEME_ROUND_ROBIN = 0x0001, //采用轮询（Round Robin）的调度策略。即在多个流之间均匀地分配发送机会，每个流依次发送数据。
        QUIC_STREAM_SCHEDULING_SCHEME_COUNT, //表示枚举中流调度策略的总数
    }

    internal class QUIC_TLS_SECRETS
    {
        public int SecretLength;
        internal class IsSet_Class
        {
            public bool ClientRandom;
            public bool ClientEarlyTrafficSecret;
            public bool ClientHandshakeTrafficSecret;
            public bool ServerHandshakeTrafficSecret;
            public bool ClientTrafficSecret0;
            public bool ServerTrafficSecret0;
        }

        public readonly IsSet_Class IsSet = new IsSet_Class();
        public byte[] ClientRandom = new byte[32];
        public byte[] ClientEarlyTrafficSecret = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
        public byte[] ClientHandshakeTrafficSecret = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
        public byte[] ServerHandshakeTrafficSecret = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
        public byte[] ClientTrafficSecret0 = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];
        public byte[] ServerTrafficSecret0 = new byte[MSQuicFunc.QUIC_TLS_SECRETS_MAX_SECRET_LEN];

        public static implicit operator QUIC_TLS_SECRETS(ReadOnlySpan<byte> ssBuffer)
        {
            QUIC_TLS_SECRETS mm = new QUIC_TLS_SECRETS();
            mm.WriteFrom(ssBuffer);
            return mm;
        }
        public void WriteTo(Span<byte> Buffer)
        {

        }
        public void WriteFrom(ReadOnlySpan<byte> Buffer)
        {

        }
    }

    internal class QUIC_PRIVATE_TRANSPORT_PARAMETER
    {
        public uint Type;
        public QUIC_BUFFER Buffer;

        public static implicit operator QUIC_PRIVATE_TRANSPORT_PARAMETER(ReadOnlySpan<byte> ssBuffer)
        {
            QUIC_PRIVATE_TRANSPORT_PARAMETER mm = new QUIC_PRIVATE_TRANSPORT_PARAMETER();
            mm.WriteFrom(ssBuffer);
            return mm;
        }
        public void WriteTo(Span<byte> Buffer)
        {

        }
        public void WriteFrom(ReadOnlySpan<byte> Buffer)
        {

        }
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
        public readonly List<int> ProcessorList = new List<int>();
    }

    internal class QUIC_GLOBAL_EXECUTION_CONFIG
    {
        public QUIC_GLOBAL_EXECUTION_CONFIG_FLAGS Flags;
        public long PollingIdleTimeoutUs;      // Time before a polling thread, with no work to do, sleeps.
        public readonly List<int> ProcessorList = new List<int>();
    }

    internal enum QUIC_GLOBAL_EXECUTION_CONFIG_FLAGS
    {
        QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_NONE = 0x0000,
        QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_NO_IDEAL_PROC = 0x0008,
        QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_HIGH_PRIORITY = 0x0010,
        QUIC_GLOBAL_EXECUTION_CONFIG_FLAG_AFFINITIZE = 0x0020,
    }

    internal enum QUIC_CONNECTION_SHUTDOWN_FLAGS
    {
        QUIC_CONNECTION_SHUTDOWN_FLAG_NONE = 0x0000,
        QUIC_CONNECTION_SHUTDOWN_FLAG_SILENT = 0x0001,
        QUIC_CONNECTION_SHUTDOWN_FLAG_STATUS = 0x8000
    } 

    internal enum QUIC_CONNECTION_EVENT_TYPE:byte
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
        // QUIC_DATAGRAM_SEND_UNKNOWN (未知)
        // 含义: 这是数据报的初始状态。表示该数据报尚未被 QUIC 实现处理或发送。
        // 触发时机: 数据报对象刚被创建时。
        // 后续状态: 通常会过渡到 QUIC_DATAGRAM_SEND_SENT（如果开始发送）或 QUIC_DATAGRAM_SEND_CANCELED（如果在发送前被取消）
        QUIC_DATAGRAM_SEND_UNKNOWN,                         // Not yet sent.

        //含义: 数据报已经被 QUIC 实现封装成一个或多个 QUIC 数据包 (packet)，并已提交给底层 UDP 套接字进行发送。
        //此时，QUIC 正在等待来自对端的确认（ACK）来证明数据报已被成功接收。
        QUIC_DATAGRAM_SEND_SENT,                            // Sent and awaiting acknowledgment
        
        //含义: 根据 QUIC 的丢包检测机制（主要是基于 ACK 的缺失和重传超时 RTO），该数据报被认为可能已经丢失。
        //QUIC 实现会基于此状态决定是否需要重传（对于可重传的数据报）或采取其他措施。
        QUIC_DATAGRAM_SEND_LOST_SUSPECT,                    // 怀疑丢包

        //在经过多次重传尝试或基于其他启发式方法后，QUIC 实现最终确定该数据报已经丢失，并且不再尝试重传。
        //该数据报的发送任务被彻底放弃
        QUIC_DATAGRAM_SEND_LOST_DISCARDED,                  // 丢包了

        //QUIC 实现收到了来自对端的确认（ACK），明确表明包含该数据报的 QUIC 数据包已经被对方成功接收并处理。
        //这是数据报发送成功的最终状态。
        QUIC_DATAGRAM_SEND_ACKNOWLEDGED,                    // 确认

        //这是一个特殊情况。QUIC 实现收到了该数据报的确认（ACK），
        //但在收到此 ACK 之前，已经因为超时而重传了该数据报（或其部分）。
        //这意味着原始的数据包虽然被确认收到了，但这个确认来得太晚，导致了不必要的重传。这个 ACK 被称为“虚假”（spurious）确认。
        QUIC_DATAGRAM_SEND_ACKNOWLEDGED_SPURIOUS,           // Acknowledged after being suspected lost

        //在数据报被实际发送到网络之前，应用程序或 QUIC 实现本身主动取消了发送请求。
        QUIC_DATAGRAM_SEND_CANCELED,                        // Canceled before send
    }

    internal class QUIC_NEW_CONNECTION_INFO
    {
        public uint QuicVersion;
        public QUIC_ADDR LocalAddress;
        public QUIC_ADDR RemoteAddress;
        public QUIC_BUFFER CryptoBuffer;
        public QUIC_ALPN_BUFFER ClientAlpnList;
        public QUIC_BUFFER NegotiatedAlpn;
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
            private QUIC_ALPN_BUFFER m_NegotiatedAlpn;
            public QUIC_ALPN_BUFFER NegotiatedAlpn
            {
                get
                {
                    if(m_NegotiatedAlpn == null)
                    {
                        m_NegotiatedAlpn = new QUIC_ALPN_BUFFER();
                    }
                    return m_NegotiatedAlpn;
                }
            }
        }
        public struct SHUTDOWN_INITIATED_BY_TRANSPORT_DATA
        {
            public int Status;
            public int ErrorCode; // Wire format error code.
        }
        public struct SHUTDOWN_INITIATED_BY_PEER_DATA
        {
            public int ErrorCode;
        }
        public struct SHUTDOWN_COMPLETE_DATA
        {
            public bool HandshakeCompleted;
            public bool PeerAcknowledgedShutdown;
            public bool AppCloseInProgress;
        }
        public struct LOCAL_ADDRESS_CHANGED_DATA
        {
            public QUIC_ADDR Address;
        }
        public struct PEER_ADDRESS_CHANGED_DATA
        {
            public QUIC_ADDR Address;
        }

        public struct PEER_STREAM_STARTED_DATA
        {
            public QUIC_STREAM Stream;
            public QUIC_STREAM_OPEN_FLAGS Flags;
        }
        public struct STREAMS_AVAILABLE_DATA
        {
            public int BidirectionalCount;
            public int UnidirectionalCount;
        }
        public struct PEER_NEEDS_STREAMS_DATA
        {
            public bool Bidirectional;
        }
        public struct IDEAL_PROCESSOR_CHANGED_DATA
        {
            public int IdealProcessor;
            public int PartitionIndex;
        }
        public struct DATAGRAM_STATE_CHANGED_DATA
        {
            public bool SendEnabled;
            public int MaxSendLength;
        }
        public struct DATAGRAM_RECEIVED_DATA
        {
            public QUIC_BUFFER Buffer;
            public uint Flags;
        }
        public struct DATAGRAM_SEND_STATE_CHANGED_DATA
        {
            public QUIC_DATAGRAM_SEND_STATE State;
            public object ClientContext;
        }
        public struct RESUMED_DATA
        {
            public QUIC_BUFFER ResumptionState;
        }
        public struct RESUMPTION_TICKET_RECEIVED_DATA
        {
            public QUIC_BUFFER ResumptionTicket;
        }
        public struct PEER_CERTIFICATE_RECEIVED_DATA
        {
            public byte[] Certificate;      // Peer certificate (platform specific). Valid only during QUIC_CONNECTION_EVENT_PEER_CERTIFICATE_RECEIVED callback.
            public uint DeferredErrorFlags;        // Bit flag of errors (only valid with QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION) - Schannel only, zero otherwise.
            public int DeferredStatus;         // Most severe error status (only valid with QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION)
            public byte[] Chain;      // Peer certificate chain (platform specific). Valid only during QUIC_CONNECTION_EVENT_PEER_CERTIFICATE_RECEIVED callback.
        }
        public struct RELIABLE_RESET_NEGOTIATED_DATA
        {
            public bool IsNegotiated;
        }
        public struct ONE_WAY_DELAY_NEGOTIATED_DATA
        {
            public bool SendNegotiated;             // TRUE if sending one-way delay timestamps is negotiated.
            public bool ReceiveNegotiated;          // TRUE if receiving one-way delay timestamps is negotiated.
        }
        public struct NETWORK_STATISTICS_DATA
        {
            public int BytesInFlight;              // Bytes that were sent on the wire, but not yet acked
            public long PostedBytes;                // Total bytes queued, but not yet acked. These may contain sent bytes that may have portentially lost too.
            public long IdealBytes;                 // Ideal number of bytes required to be available to  avoid limiting throughput
            public long SmoothedRTT;                // Smoothed RTT value
            public int CongestionWindow;           // Congestion Window
            public long Bandwidth;                  // Estimated bandwidth
        }
    }

    internal enum QUIC_SERVER_RESUMPTION_LEVEL
    {
        QUIC_SERVER_NO_RESUME,
        QUIC_SERVER_RESUME_ONLY,
        QUIC_SERVER_RESUME_AND_ZERORTT,
    }

    internal enum QUIC_RECEIVE_FLAGS
    {
        QUIC_RECEIVE_FLAG_NONE = 0x0000,
        QUIC_RECEIVE_FLAG_0_RTT = 0x0001,   // Data was encrypted with 0-RTT key.
        QUIC_RECEIVE_FLAG_FIN = 0x0002,   // FIN was included with this data.
    }

    internal enum QUIC_TLS_PROTOCOL_VERSION
    {
        QUIC_TLS_PROTOCOL_UNKNOWN = 0,
        QUIC_TLS_PROTOCOL_1_3 = 0x3000,
    }

    internal enum QUIC_CIPHER_ALGORITHM
    {
        QUIC_CIPHER_ALGORITHM_NONE = 0,
        QUIC_CIPHER_ALGORITHM_AES_128 = 0x660E,
        QUIC_CIPHER_ALGORITHM_AES_256 = 0x6610,
        QUIC_CIPHER_ALGORITHM_CHACHA20 = 0x6612,     // Not supported on Schannel/BCrypt
    }

    internal enum QUIC_HASH_ALGORITHM
    {
        QUIC_HASH_ALGORITHM_NONE = 0,
        QUIC_HASH_ALGORITHM_SHA_256 = 0x800C,
        QUIC_HASH_ALGORITHM_SHA_384 = 0x800D,
    }

    internal enum QUIC_KEY_EXCHANGE_ALGORITHM
    {
        QUIC_KEY_EXCHANGE_ALGORITHM_NONE = 0,
    }

    internal enum QUIC_CIPHER_SUITE
    {
        QUIC_CIPHER_SUITE_TLS_AES_128_GCM_SHA256 = 0x1301,
        QUIC_CIPHER_SUITE_TLS_AES_256_GCM_SHA384 = 0x1302,
        QUIC_CIPHER_SUITE_TLS_CHACHA20_POLY1305_SHA256 = 0x1303, // Not supported on Schannel
    }
    
    internal class QUIC_HANDSHAKE_INFO
    {
        public QUIC_TLS_PROTOCOL_VERSION TlsProtocolVersion;
        public QUIC_CIPHER_ALGORITHM CipherAlgorithm;
        public QUIC_HASH_ALGORITHM Hash;
        public QUIC_KEY_EXCHANGE_ALGORITHM KeyExchangeAlgorithm;
        public QUIC_CIPHER_SUITE CipherSuite;

        public int CipherStrength;
        public int HashStrength;
        public int KeyExchangeStrength;
    }

    internal class QUIC_TICKET_KEY_CONFIG
    {
        byte[] Id = new byte[16];
        byte[] Material = new byte[64];
        byte MaterialLength;
    }

    internal static unsafe partial class MSQuicFunc
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
    }
}
