/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    internal enum inet_csk_ack_state_t:byte
    {
        ICSK_ACK_SCHED = 1,// ACK 已被安排发送
        ICSK_ACK_TIMER = 2,// 使用定时器来触发 ACK 发送
        ICSK_ACK_PUSHED = 4,// ACK 已经被“推送”出去（即已经准备好发送）
        ICSK_ACK_PUSHED2 = 8,// 另一个 ACK 推送标记，可能用于特定场景下的额外确认
        ICSK_ACK_NOW = 16,  // 立即发送下一个 ACK（仅一次）
        ICSK_ACK_NOMEM = 32,// 由于内存不足无法发送 ACK
    }

    internal enum tcp_skb_cb_sacked_flags:byte
    {
        TCPCB_SACKED_ACKED = (1 << 0),  // 数据段已被 SACK 块确认
        TCPCB_SACKED_RETRANS = (1 << 1),    // 数据段已被重传
        TCPCB_LOST = (1 << 2),  // 数据段被认为已丢失
        TCPCB_TAGBITS = (TCPCB_SACKED_ACKED | TCPCB_SACKED_RETRANS | TCPCB_LOST), // 所有标签位
        TCPCB_REPAIRED = (1 << 4),  // 数据段已被修复（无 skb_mstamp_ns）
        TCPCB_EVER_RETRANS = (1 << 7),  // 数据段曾经被重传过
        TCPCB_RETRANS = (TCPCB_SACKED_RETRANS | TCPCB_EVER_RETRANS | TCPCB_REPAIRED),
    }

    internal enum tcp_ca_state:byte
    {
        TCP_CA_Open = 0, // 初始状态或没有检测到拥塞
        TCP_CA_Disorder = 1, //出现失序的数据包，但未确认丢失
        TCP_CA_CWR = 2, //进入拥塞窗口减少 (Congestion Window Reduced) 状态
        TCP_CA_Recovery = 3,// 恢复状态，当检测到丢失时进入此状态
        TCP_CA_Loss = 4 // 检测到数据包丢失，进入损失状态
    }

    internal enum tcpf_ca_state : byte
    {
        TCPF_CA_Open = (1 << tcp_ca_state.TCP_CA_Open),
        TCPF_CA_Disorder = (1 << tcp_ca_state.TCP_CA_Disorder),
        TCPF_CA_CWR = (1 << tcp_ca_state.TCP_CA_CWR),
        TCPF_CA_Recovery = (1 << tcp_ca_state.TCP_CA_Recovery),
        TCPF_CA_Loss = (1 << tcp_ca_state.TCP_CA_Loss)
    }

    internal enum tcp_ca_ack_event_flags : byte
    {
        CA_ACK_SLOWPATH = (1 << 0), /* In slow path processing */
        CA_ACK_WIN_UPDATE = (1 << 1),   /* ACK updated window */
        CA_ACK_ECE = (1 << 2),  /* ECE bit is set on ack */
    }

    //tcp_tw_status 是一个枚举类型，用于表示 TCP 连接在 TIME_WAIT 状态下对输入数据包的处理结果。
    //这个枚举类型定义在内核源码中，用于 tcp_timewait_state_process 函数的返回值，该函数处理处于 TIME_WAIT 状态的 TCP 连接接收到的数据包
    //1. TIME_WAIT 状态的定义和作用
    //TIME_WAIT 状态是 TCP 连接终止过程中的一个状态。
    //当一个连接的一方（假设为客户端）发送了 FIN 包请求终止连接，并接收到服务端的 ACK 确认后，该连接会进入 TIME_WAIT 状态。
    //此时，客户端会等待一段时间，通常是 2 倍的 MSL（Maximum Segment Lifetime，最大报文段生存时间）。
    //TIME_WAIT 状态有两个主要目的：
    //确保最后一个 ACK 包的可靠传输：如果最后一个 ACK 包没有被对端收到，对端会重发 FIN 包，TIME_WAIT 状态的一方可以重新发送 ACK 包，确保连接正确关闭。
    //避免“幽灵”连接：确保在相同的源地址、目标地址、源端口和目标端口的新连接建立之前，旧的连接完全关闭，避免可能的数据混乱。
    //2. TIME_WAIT 状态的持续时间
    //在 Linux 系统中，TIME_WAIT 状态的持续时间通常为 60 秒，这个时间是 2 倍的 MSL。
    //这个时间足够长，可以最大限度地消除延迟的数据包可能对新连接造成的影响。
    internal enum tcp_tw_status : byte
    {
        TCP_TW_SUCCESS = 0,
        TCP_TW_RST = 1,
        TCP_TW_ACK = 2,
        TCP_TW_SYN = 3
    }

    internal enum tcp_chrono : byte
    {
        TCP_CHRONO_UNSPEC,
        TCP_CHRONO_BUSY, //标记连接处于活跃发送数据的状态（即写队列非空）。这表明应用程序正在积极地向网络发送数据。
        TCP_CHRONO_RWND_LIMITED, //表明连接由于接收窗口不足而被阻塞。这意味着接收端的窗口大小不足以容纳更多数据，导致发送端暂停发送新数据直到窗口空间可用。
        TCP_CHRONO_SNDBUF_LIMITED, //指出连接因发送缓冲区不足而被限制。当本地系统的发送缓冲区已满时，应用程序将无法继续发送数据，直到有足够的空间释放出来。
        __TCP_CHRONO_MAX,
    }

    internal enum sock_flags : byte
    {
        SOCK_DEAD,
        SOCK_DONE,
        SOCK_URGINLINE,
        SOCK_KEEPOPEN,
        SOCK_LINGER,
        SOCK_DESTROY,
        SOCK_BROADCAST,
        SOCK_TIMESTAMP,
        SOCK_ZAPPED, //表示套接字已经被“清除”或“释放”
        SOCK_USE_WRITE_QUEUE, /* whether to call sk->sk_write_space in sock_wfree */
        SOCK_DBG, /* %SO_DEBUG setting */
        SOCK_RCVTSTAMP, /* %SO_TIMESTAMP setting */
        SOCK_RCVTSTAMPNS, /* %SO_TIMESTAMPNS setting */
        SOCK_LOCALROUTE, /* route locally only, %SO_DONTROUTE setting */
        SOCK_MEMALLOC, /* VM depends on this socket for swapping */
        SOCK_TIMESTAMPING_RX_SOFTWARE,  /* %SOF_TIMESTAMPING_RX_SOFTWARE */
        SOCK_FASYNC, /* fasync() active */
        SOCK_RXQ_OVFL,
        SOCK_ZEROCOPY, /* buffers from userspace */
        SOCK_WIFI_STATUS, /* push wifi status to userspace */
        SOCK_NOFCS, /* Tell NIC not to do the Ethernet FCS.
		     * Will use last 4 bytes of packet sent from
		     * user-space instead.
		     */
        SOCK_FILTER_LOCKED, /* Filter cannot be changed anymore */
        SOCK_SELECT_ERR_QUEUE, /* Wake select on error queue */
        SOCK_RCU_FREE, /* wait rcu grace period in sk_destruct() */
        SOCK_TXTIME,
        SOCK_XDP, /* XDP is attached */
        SOCK_TSTAMP_NEW, /* Indicates 64 bit timestamps always */
        SOCK_RCVMARK, /* Receive SO_MARK  ancillary data with packet */
    };

    //在Linux内核网络栈中，enum sk_pacing 定义了套接字（socket）的pacing状态，
    //这用于控制TCP数据包的发送速率。通过设置不同的枚举值，可以启用或禁用pacing功能，
    //并指定使用哪种方式来实现流量控制。具体来说，sk_pacing 枚举包含以下三个成员：
    internal enum sk_pacing : byte
    {
        ////表示不启用pacing功能，即允许TCP连接以尽可能快的速度发送数据包
        SK_PACING_NONE = 0,

        ////指示需要启用TCP自身的pacing机制，这意味着当满足一定条件时，
        /// 例如当前发送速率不为零且不等于最大无符号整数值的情况下，内核会根据设定的pacing速率计算每个数据包发送所需的时间，
        //并启动高精度定时器（hrtimer）来确保按照计算出的时间间隔发送数据包
        SK_PACING_NEEDED = 1,

        //表明将使用公平队列（Fair Queue, FQ）调度器来进行pacing。
        //这种方式依赖于FQ算法对流量进行管理和调节，从而避免了直接由TCP子系统执行pacing所带来的额外CPU开销18。
        SK_PACING_FQ = 2,
    }

    //tsq: Timestamp and Socket Queue
    //enum tsq_enum 是 Linux 内核 TCP 协议栈中用于表示不同类型的延迟（deferred）或节流（throttled）状态的枚举类型
    internal enum tsq_enum : byte
    {
        TSQ_THROTTLED, //表示套接字已被节流（throttled）。当系统资源紧张时，TCP 可能会暂时停止发送数据以减轻负载。
        TSQ_QUEUED,//表示任务已经被排队等待处理。这通常意味着当前没有立即执行该任务的资源或时机，因此它被放入队列中稍后处理。
        TCP_TSQ_DEFERRED,//当 tcp_tasklet_func() 发现套接字正在被其他线程持有（owned by another thread），则将任务推迟到稍后再处理。这种情况可以防止并发访问冲突，并确保数据的一致性。
        TCP_WRITE_TIMER_DEFERRED, //当 tcp_write_timer() 发现套接字正在被其他线程持有，则将写操作推迟。这有助于避免在不适当的时间点进行写操作，从而提高性能和稳定性。
        TCP_DELACK_TIMER_DEFERRED, //当 tcp_delack_timer() 发现套接字正在被其他线程持有，则将延迟确认（delayed acknowledgment）的操作推迟。延迟确认是一种优化技术，通过减少确认的数量来降低网络流量
        TCP_MTU_REDUCED_DEFERRED, //当 tcp_v4_err() 或 tcp_v6_err() 无法立即调用 tcp_v4_mtu_reduced() 或 tcp_v6_mtu_reduced() 来响应 MTU 减少事件时，任务会被推迟。这通常发生在 ICMP 错误消息处理过程中，表明路径 MTU 已经改变。
        TCP_ACK_DEFERRED,  //表示纯确认（pure ACK）的发送被推迟。在某些情况下，为了避免不必要的小包传输，TCP 可能会选择推迟发送仅包含确认信息的数据包。
    }

    internal enum tsq_flags : byte
    {
        TSQF_THROTTLED = 1 << tsq_enum.TSQ_THROTTLED,
        TSQF_QUEUED = 1 << tsq_enum.TSQ_QUEUED,
        TCPF_TSQ_DEFERRED = 1 << tsq_enum.TCP_TSQ_DEFERRED,
        TCPF_WRITE_TIMER_DEFERRED = 1 << tsq_enum.TCP_WRITE_TIMER_DEFERRED,
        TCPF_DELACK_TIMER_DEFERRED = 1 << tsq_enum.TCP_DELACK_TIMER_DEFERRED,
        TCPF_MTU_REDUCED_DEFERRED = 1 << tsq_enum.TCP_MTU_REDUCED_DEFERRED,
        TCPF_ACK_DEFERRED = 1 << tsq_enum.TCP_ACK_DEFERRED,
    }

    internal enum tcp_queue : byte
    {
        TCP_FRAG_IN_WRITE_QUEUE,
        TCP_FRAG_IN_RTX_QUEUE,
    };
}
