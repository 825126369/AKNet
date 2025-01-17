/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal partial class LinuxTcpFunc
    {
        public static readonly int OPTION_SACK_ADVERTISE = (int)LinuxTcpFunc.BIT(0);
        public static readonly int OPTION_TS = (int)LinuxTcpFunc.BIT(1);
        public static readonly int OPTION_MD5 = (int)LinuxTcpFunc.BIT(2);
        public static readonly int OPTION_WSCALE = (int)LinuxTcpFunc.BIT(3);
        public static readonly int OPTION_FAST_OPEN_COOKIE = (int)LinuxTcpFunc.BIT(8);
        public static readonly int OPTION_SMC = (int)LinuxTcpFunc.BIT(9);
        public static readonly int OPTION_MPTCP = (int)LinuxTcpFunc.BIT(10);
        public static readonly int OPTION_AO = (int)LinuxTcpFunc.BIT(11);

        public static readonly int SKBFL_ZEROCOPY_ENABLE = (int)LinuxTcpFunc.BIT(0);
        public static readonly int SKBFL_SHARED_FRAG = (int)LinuxTcpFunc.BIT(1);
        public static readonly int SKBFL_PURE_ZEROCOPY = (int)LinuxTcpFunc.BIT(2);
        public static readonly int SKBFL_DONT_ORPHAN = (int)LinuxTcpFunc.BIT(3);
        public static readonly int SKBFL_MANAGED_FRAG_REFS = (int)LinuxTcpFunc.BIT(4);

        public const int sizeof_tcphdr = 20;

        /* generate hardware time stamp */
        public const int SKBTX_HW_TSTAMP = 1 << 0;
        public const int SKBTX_SW_TSTAMP = 1 << 1;
        public const int SKBTX_IN_PROGRESS = 1 << 2;
        public const int SKBTX_HW_TSTAMP_USE_CYCLES = 1 << 3;
        public const int SKBTX_WIFI_STATUS = 1 << 4;
        public const int SKBTX_HW_TSTAMP_NETDEV = 1 << 5;
        public const int SKBTX_SCHED_TSTAMP = 1 << 6;

        public const int SKBTX_ANY_SW_TSTAMP = (SKBTX_SW_TSTAMP | SKBTX_SCHED_TSTAMP);
        public const int SKBTX_ANY_TSTAMP = (SKBTX_HW_TSTAMP | SKBTX_HW_TSTAMP_USE_CYCLES | SKBTX_ANY_SW_TSTAMP);



        public const int RTAX_UNSPEC = 0;
        public const int RTAX_LOCK = 1;
        public const int RTAX_MTU = 2;
        public const int RTAX_WINDOW = 3;
        public const int RTAX_RTT = 4;
        public const int RTAX_RTTVAR = 5;
        public const int RTAX_SSTHRESH = 6;
        public const int RTAX_CWND = 7;
        public const int RTAX_ADVMSS = 8;
        public const int RTAX_REORDERING = 9;
        public const int RTAX_HOPLIMIT = 10;
        public const int RTAX_INITCWND = 11;
        public const int RTAX_FEATURES = 12;
        public const int RTAX_RTO_MIN = 13;
        public const int RTAX_INITRWND = 14;
        public const int RTAX_QUICKACK = 15;
        public const int RTAX_CC_ALGO = 16;
        public const int RTAX_FASTOPEN_NO_COOKIE = 17;
        public const int __RTAX_MAX = 18;


        public const int FLAG_DATA = 0x01; /* Incoming frame contained data.		*/
        public const int FLAG_WIN_UPDATE = 0x02; /* Incoming ACK was a window update.	*/
        public const int FLAG_DATA_ACKED = 0x04; /* This ACK acknowledged new data.		*/
        public const int FLAG_RETRANS_DATA_ACKED = 0x08; /* "" "" some of which was retransmitted.	*/
        public const int FLAG_SYN_ACKED = 0x10; /* This ACK acknowledged SYN.		*/
        public const int FLAG_DATA_SACKED = 0x20; /* New SACK.				*/
        public const int FLAG_ECE = 0x40; /* ECE in this ACK				*/
        public const int FLAG_LOST_RETRANS = 0x80; /* This ACK marks some retransmission lost */
        public const int FLAG_SLOWPATH = 0x100; /* Do not skip RFC checks for window update.*/
        public const int FLAG_ORIG_SACK_ACKED = 0x200; /* Never retransmitted data are (s)acked	*/
        public const int FLAG_SND_UNA_ADVANCED = 0x400; /* Snd_una was changed (!= FLAG_DATA_ACKED) */
        public const int FLAG_DSACKING_ACK = 0x800; /* SACK blocks contained D-SACK info */
        public const int FLAG_SET_XMIT_TIMER = 0x1000; /* Set TLP or RTO timer */
        public const int FLAG_SACK_RENEGING = 0x2000; /* snd_una advanced to a sacked seq */
        public const int FLAG_UPDATE_TS_RECENT = 0x4000; /* tcp_replace_ts_recent() */
        public const int FLAG_NO_CHALLENGE_ACK = 0x8000; /* do not call tcp_send_challenge_ack()	*/
        public const int FLAG_ACK_MAYBE_DELAYED = 0x10000; /* Likely a delayed ACK */
        public const int FLAG_DSACK_TLP = 0x20000; /* DSACK for tail loss probe */

        public const int FLAG_ACKED = (FLAG_DATA_ACKED | FLAG_SYN_ACKED);
        public const int FLAG_NOT_DUP = (FLAG_DATA | FLAG_WIN_UPDATE | FLAG_ACKED);
        public const int FLAG_CA_ALERT = (FLAG_DATA_SACKED | FLAG_ECE | FLAG_DSACKING_ACK);
        public const int FLAG_FORWARD_PROGRESS = (FLAG_ACKED | FLAG_DATA_SACKED);

        public const int TCP_NAGLE_OFF = 1;	/* Nagle's algo is disabled */
        public const int TCP_NAGLE_CORK = 2;	/* Socket is corked	    */
        public const int TCP_NAGLE_PUSH = 4;    /* Cork is overridden for already queued data */

        public const int SK_MEM_SEND = 0;
        public const int SK_MEM_RECV = 1;

        public const int PAGE_SHIFT = 13;
        public const int PAGE_SIZE = 1 << (PAGE_SHIFT);
        public const int PAGE_MASK = ~(PAGE_SIZE - 1);

        public const int MAX_HEADER = 32;
        public const int L1_CACHE_SHIFT = 5;
        public const int L1_CACHE_BYTES = (1 << L1_CACHE_SHIFT);
        public const int MAX_TCP_HEADER = 192;

        public const int CHECKSUM_NONE = 0;
        public const int CHECKSUM_UNNECESSARY = 1;
        public const int CHECKSUM_COMPLETE = 2;
        public const int CHECKSUM_PARTIAL = 3; //它表示传输层（如 TCP 或 UDP）的校验和已经被部分计算

        public const int CONFIG_MAX_SKB_FRAGS = 17;
        public const int MAX_SKB_FRAGS = CONFIG_MAX_SKB_FRAGS;

        public const int TCP_SKB_MIN_TRUESIZE = 2048;
        public const int SOCK_MIN_SNDBUF = (TCP_SKB_MIN_TRUESIZE * 2);
        public const int SOCK_MIN_RCVBUF = TCP_SKB_MIN_TRUESIZE;

        public const int TCP_TIMEOUT_MIN_US = (int)(2 * USEC_PER_MSEC); /* Min TCP timeout in microsecs */
        public const int TCP_TIMEOUT_INIT = 1 * HZ;	/* RFC6298 2.1 initial RTO value	*/

        public const int MSG_OOB = 1;
        public const int MSG_PEEK = 2;
        public const int MSG_DONTROUTE = 4;
        public const int MSG_TRYHARD = 4;       /* Synonym for MSG_DONTROUTE for DECnet */
        public const int MSG_CTRUNC = 8;
        public const int MSG_PROBE = 0x10;	/* Do not send. Only probe path f.e. for MTU */
        public const int MSG_TRUNC = 0x20;
        public const int MSG_DONTWAIT = 0x40;	/* Nonblocking io		 */
        public const int MSG_EOR = 0x80;	/* End of record */
        public const int MSG_WAITALL = 0x100;	/* Wait for a full request */
        public const int MSG_FIN = 0x200;
        public const int MSG_SYN = 0x400;
        public const int MSG_CONFIRM = 0x800;	/* Confirm path validity */
        public const int MSG_RST = 0x1000;
        public const int MSG_ERRQUEUE = 0x2000;	/* Fetch message from error queue */
        public const int MSG_NOSIGNAL = 0x4000;	/* Do not generate SIGPIPE */
        public const int MSG_MORE = 0x8000;	/* Sender will send more */
        public const int MSG_WAITFORONE = 0x10000;	/* recvmmsg(): block until 1+ packets avail */
        public const int MSG_SENDPAGE_NOPOLICY = 0x10000; /* sendpage() internal : do no apply policy */
        public const int MSG_BATCH = 0x40000; /* sendmmsg(): more messages coming */
        public const int MSG_EOF = MSG_FIN;
        public const int MSG_NO_SHARED_FRAGS = 0x80000; /* sendpage() internal : page frags are not shared */
        public const int MSG_SENDPAGE_DECRYPTED = 0x100000;
        public const int MSG_SOCK_DEVMEM = 0x2000000;	/* Receive devmem skbs as cmsg */
        public const int MSG_ZEROCOPY = 0x4000000;	/* Use user data in kernel path */
        public const int MSG_SPLICE_PAGES = 0x8000000;	/* Splice the pages from the iterator in sendmsg() */
        public const int MSG_FASTOPEN = 0x20000000;	/* Send data in TCP SYN */
        public const int MSG_CMSG_CLOEXEC = 0x40000000;
        public const int MSG_CMSG_COMPAT = 0;
        public const int MSG_INTERNAL_SENDMSG_FLAGS = (MSG_SPLICE_PAGES | MSG_SENDPAGE_NOPOLICY | MSG_SENDPAGE_DECRYPTED);


        public const int SOCKWQ_ASYNC_NOSPACE = 0;
        public const int SOCKWQ_ASYNC_WAITDATA = 1;
        public const int SOCK_NOSPACE = 2;
        public const int SOCK_PASSCRED = 3;
        public const int SOCK_PASSSEC = 4;
        public const int SOCK_SUPPORT_ZC = 5;
        public const int SOCK_CUSTOM_SOCKOPT = 6;
        public const int SOCK_PASSPIDFD = 7;


        public const int TCP_DEFERRED_ALL = (int)(tsq_flags.TCPF_TSQ_DEFERRED |
                tsq_flags.TCPF_WRITE_TIMER_DEFERRED |
                tsq_flags.TCPF_DELACK_TIMER_DEFERRED |
                tsq_flags.TCPF_MTU_REDUCED_DEFERRED |
                tsq_flags.TCPF_ACK_DEFERRED);


        public const int SOCKCM_FLAG_TS_OPT_ID = 1 << 31;

        public const int TCP_CMSG_INQ = 1;
        public const int TCP_CMSG_TS = 2;

        public const int TCP_TS_HZ = 1000;

        public const int AF_INET = 2;   /* Internet IP Protocol 	*/
        public const int IPPROTO_TCP = 6;
        public const int IPPROTO_UDP = 17;

        public const int TCP_METRICS_TIMEOUT = 60 * 60 * HZ;

        public const int TCP_METRIC_MAX = (byte)tcp_metric_index.__TCP_METRIC_MAX - 1;
        public const int TCP_METRIC_MAX_KERNEL = (TCP_METRIC_MAX - 2);

        public const int TCP_FASTOPEN_COOKIE_MIN = 4;   /* Min Fast Open Cookie size in bytes */
        public const int TCP_FASTOPEN_COOKIE_MAX = 16;  /* Max Fast Open Cookie size in bytes */
        public const int TCP_FASTOPEN_COOKIE_SIZE = 8;  /* the size employed by this impl. */

        public const int TCP_TIMEOUT_FALLBACK = 3 * HZ;

        public const int TCP_CONG_NON_RESTRICTED = 0x1;
        public const int TCP_CONG_NEEDS_ECN = 0x2;
        public const int TCP_CONG_MASK = (TCP_CONG_NON_RESTRICTED | TCP_CONG_NEEDS_ECN);

        public const int INET_ECN_NOT_ECT = 0;
        public const int INET_ECN_ECT_1 = 1;
        public const int INET_ECN_ECT_0 = 2;
        public const int INET_ECN_CE = 3;
        public const int INET_ECN_MASK = 3;

        public const int BPF_SOCK_OPS_VOID = 0;
        public const int BPF_SOCK_OPS_TIMEOUT_INIT = 1;
        public const int BPF_SOCK_OPS_RWND_INIT = 2;
        public const int BPF_SOCK_OPS_TCP_CONNECT_CB = 3;
        public const int BPF_SOCK_OPS_ACTIVE_ESTABLISHED_CB = 4;
        public const int BPF_SOCK_OPS_PASSIVE_ESTABLISHED_CB = 5;
        public const int BPF_SOCK_OPS_NEEDS_ECN = 6;
        public const int BPF_SOCK_OPS_BASE_RTT = 7;
        public const int BPF_SOCK_OPS_RTO_CB = 8;
        public const int BPF_SOCK_OPS_RETRANS_CB = 9;
        public const int BPF_SOCK_OPS_STATE_CB = 10;
        public const int BPF_SOCK_OPS_TCP_LISTEN_CB = 11;
        public const int BPF_SOCK_OPS_RTT_CB = 12;
        public const int BPF_SOCK_OPS_PARSE_HDR_OPT_CB = 13;
        public const int BPF_SOCK_OPS_HDR_OPT_LEN_CB = 14;
        public const int BPF_SOCK_OPS_WRITE_HDR_OPT_CB = 15;



        public const int TCP_INFINITE_SSTHRESH = 0x7fffffff;
        public const ushort TCP_MSS_DEFAULT = 536;
        public const int TCP_INIT_CWND = 10;

        public const ushort HZ = 1000;
        public const long TCP_RTO_MAX = 120 * HZ;
        public const long TCP_RTO_MIN = HZ / 5;

        public const int TCP_FASTRETRANS_THRESH = 3;
        public const int sysctl_tcp_comp_sack_slack_ns = 100; //启动一个高分辨率定时器，用于管理TCP累积ACK的发送

        public const int TCP_DELACK_MIN = HZ / 25;
        public const int TCP_DELACK_MAX = HZ / 5;
        public const int TCP_ATO_MIN = HZ / 25;
        public const uint TCP_RESOURCE_PROBE_INTERVAL = (HZ / 2);
        public const uint TCP_TIMEOUT_MIN = 2;

        public const int TCP_ECN_OK = 1;//这个标志位表示TCP连接已经协商好并且双方都同意使用ECN功能。
        public const int TCP_ECN_QUEUE_CWR = 2;//这个标志位表明需要将CWR（Congestion Window Reduced）标志置入即将发送的数据包中。它用于确认发送方已经响应了接收到的ECE（Echo Congestion Experienced）标志，即发送方已经减少了其拥塞窗口以应对网络拥塞
        public const int TCP_ECN_DEMAND_CWR = 4;//这个标志位指示接收方希望从发送方那里得到一个CWR标志作为对之前报告的拥塞情况的回应。
        public const int TCP_ECN_SEEN = 8;//这个标志位表示在这次连接中至少有一个数据包携带了CE（Congestion Experienced）标志

        public const int TCP_RACK_LOSS_DETECTION = 0x1; //启用 RACK 来检测丢失的数据包。
        public const int TCP_RACK_STATIC_REO_WND = 0x2; //使用静态的 RACK 重排序窗口
        public const int TCP_RACK_NO_DUPTHRESH = 0x4; //在 RACK 中不使用重复确认（DUPACK）阈值。

        public const byte TCPHDR_FIN = 0x01;
        public const byte TCPHDR_SYN = 0x02;
        public const byte TCPHDR_RST = 0x04;
        public const byte TCPHDR_PSH = 0x08;
        public const byte TCPHDR_ACK = 0x10;
        public const byte TCPHDR_URG = 0x20;
        public const byte TCPHDR_ECE = 0x40;
        public const byte TCPHDR_CWR = 0x80;

        public const byte TCPHDR_SYN_ECN = (TCPHDR_SYN | TCPHDR_ECE | TCPHDR_CWR);

        /* TCP thin-stream limits */
        public const byte TCP_THIN_LINEAR_RETRIES = 6;       /* After 6 linear retries, do exp. backoff */
        public const int TCP_RMEM_TO_WIN_SCALE = 8;

        //ICSK_TIME_RETRANS (1):
        //重传超时定时器:
        //用于设置或重置重传超时（RTO, Retransmission TimeOut）定时器。当发送的数据包没有在预期时间内收到确认（ACK）时，TCP 协议会启动 RTO 定时器，并在超时后重传数据包。
        //ICSK_TIME_DACK(2) :
        //延迟确认定时器:
        //用于设置延迟确认（Delayed ACK）定时器。发送方可以在一定时间内等待更多的数据包一起确认，以减少 ACK 报文的数量，从而提高效率。这个定时器确保即使没有累积足够的数据，也会在合理的时间内发送确认。
        //ICSK_TIME_PROBE0(3) :
        //零窗口探测定时器:
        //当接收方通告其接收窗口为零时，发送方可以定期发送探测包以检查接收方是否已经清空了一些缓冲区并准备好接收更多数据。这个定时器控制这些探测包的发送频率。
        //ICSK_TIME_LOSS_PROBE(5) :
        //尾丢失探测定时器:
        //用于处理尾丢失（Tail Loss Probe）的情况。当 TCP 发送的数据包在传输队列尾部丢失时，这个定时器可以帮助更快地检测到丢失并触发重传，而不必等到完整的 RTO 超时。
        //ICSK_TIME_REO_TIMEOUT(6) :
        //重排序超时定时器:
        //用于处理数据包重排序（Reordering）的情况。在网络环境中，由于各种原因（如不同的路由路径），数据包可能会按非顺序到达。这个定时器帮助确定什么时候认为一个数据包真正丢失，而不是仅仅因为重排序而延迟到达。

        public const byte ICSK_TIME_RETRANS = 1;    /* Retransmit timer */
        public const byte ICSK_TIME_DACK = 2;   /* Delayed ack timer */
        public const byte ICSK_TIME_PROBE0 = 3; /* Zero window probe timer */
        public const byte ICSK_TIME_LOSS_PROBE = 5; /* Tail loss probe timer */
        public const byte ICSK_TIME_REO_TIMEOUT = 6;    /* Reordering timer */

        public const ushort TCP_MIN_MSS = 88;

        public const int MAX_TCP_OPTION_SPACE = 40;
        public const int TCP_MIN_SND_MSS = 48;
        public const int TCP_MIN_GSO_SIZE = (TCP_MIN_SND_MSS - MAX_TCP_OPTION_SPACE);

        public const ushort MAX_TCP_WINDOW = 32767;

        public const int TCP_MD5SIG_MAXKEYLEN = 80;


        public const uint TCP_FLAG_CWR = 0x00800000;
        public const uint TCP_FLAG_ECE = 0x00400000;
        public const uint TCP_FLAG_URG = 0x00200000;
        public const uint TCP_FLAG_ACK = 0x00100000;
        public const uint TCP_FLAG_PSH = 0x00080000;
        public const uint TCP_FLAG_RST = 0x00040000;
        public const uint TCP_FLAG_SYN = 0x00020000;
        public const uint TCP_FLAG_FIN = 0x00010000;
        public const uint TCP_RESERVED_BITS = 0x0F000000;
        public const uint TCP_DATA_OFFSET = 0xF0000000;

        public const uint TCP_REMNANT = (TCP_FLAG_FIN | TCP_FLAG_URG | TCP_FLAG_SYN | TCP_FLAG_PSH);
        public const uint TCP_HP_BITS = (~(TCP_RESERVED_BITS | TCP_FLAG_PSH));

        public const uint TCP_MAX_QUICKACKS = 16;
        public const uint TCP_MAX_WSCALE = 14;

        public const int TCP_PAWS_WRAP = (int.MaxValue / 1000);
        public const int TCP_PAWS_MSL = 60;
        public const int TCP_PAWS_WINDOW = 1;

        public const int REXMIT_NONE = 0; /* no loss recovery to do */
        public const int REXMIT_LOST = 1; /* retransmit packets marked lost */
        public const int REXMIT_NEW = 2; /* FRTO-style transmit of unsent/new packets */

        public const int SCM_TSTAMP_SND = 0;        /* driver passed skb to NIC, or HW */
        public const int SCM_TSTAMP_SCHED = 1;  /* data entered the packet scheduler */
        public const int SCM_TSTAMP_ACK = 2;        /* data acknowledged by peer */

        public const int TCP_RACK_RECOVERY_THRESH = 16;
        public const int TCP_NUM_SACKS = 4;

        public const int TCP_SACK_SEEN = (1 << 0);   /*1 = peer is SACK capable, */
        public const int TCP_DSACK_SEEN = (1 << 2);   /*1 = DSACK was received from peer*/

        public const int TCPCB_DELIVERED_CE_MASK = (1 << 20) - 1;

        public const uint TCPOPT_NOP = 1;	/* Padding */
        public const uint TCPOPT_EOL = 0;	/* End of options */
        public const uint TCPOPT_MSS = 2;	/* Segment size negotiating */
        public const uint TCPOPT_WINDOW = 3;	/* Window scaling */
        public const uint TCPOPT_SACK_PERM = 4;       /* SACK Permitted */
        public const uint TCPOPT_SACK = 5;       /* SACK Block */
        public const uint TCPOPT_TIMESTAMP = 8;	/* Better RTT estimations/PAWS */
        public const uint TCPOPT_MD5SIG = 19;	/* MD5 Signature (RFC2385) */
        public const uint TCPOPT_AO = 29;	/* Authentication Option (RFC5925) */
        public const uint TCPOPT_MPTCP = 30;	/* Multipath TCP (RFC6824) */
        public const uint TCPOPT_FASTOPEN = 34;	/* Fast open (RFC7413) */
        public const uint TCPOPT_EXP = 254;	/* Experimental */
        public const uint TCPOPT_FASTOPEN_MAGIC = 0xF989;
        public const uint TCPOPT_SMC_MAGIC = 0xE2D4C3D9;

        public const int TCPOLEN_MSS = 4;
        public const int TCPOLEN_WINDOW = 3;
        public const int TCPOLEN_SACK_PERM = 2;
        public const int TCPOLEN_TIMESTAMP = 10;
        public const int TCPOLEN_MD5SIG = 18;
        public const int TCPOLEN_FASTOPEN_BASE = 2;
        public const int TCPOLEN_EXP_FASTOPEN_BASE = 4;
        public const int TCPOLEN_EXP_SMC_BASE = 6;
        public const int TCPOLEN_TSTAMP_ALIGNED = 12;
        public const int TCPOLEN_WSCALE_ALIGNED = 4;
        public const int TCPOLEN_SACKPERM_ALIGNED = 4;
        public const int TCPOLEN_SACK_BASE = 2;
        public const int TCPOLEN_SACK_BASE_ALIGNED = 4;
        public const int TCPOLEN_SACK_PERBLOCK = 8;
        public const int TCPOLEN_MD5SIG_ALIGNED = 20;
        public const int TCPOLEN_MSS_ALIGNED = 4;
        public const int TCPOLEN_EXP_SMC_BASE_ALIGNED = 8;

        public const byte TCP_ESTABLISHED = 1;
        public const byte TCP_SYN_SENT = 2;
        public const byte TCP_SYN_RECV = 3;
        public const byte TCP_FIN_WAIT1 = 4;
        public const byte TCP_FIN_WAIT2 = 5;
        public const byte TCP_TIME_WAIT = 6;
        public const byte TCP_CLOSE = 7;
        public const byte TCP_CLOSE_WAIT = 8;
        public const byte TCP_LAST_ACK = 9;
        public const byte TCP_LISTEN = 10;
        public const byte TCP_CLOSING = 11;    /* Now a valid state */
        public const byte TCP_NEW_SYN_RECV = 12;
        public const byte TCP_BOUND_INACTIVE = 13; /* Pseudo-state for inet_diag */
        public const byte TCP_MAX_STATES = 14; /* Leave at the end! */

        public const byte TCP_STATE_MASK = 0xF;
        public const int TCP_ACTION_FIN = (1 << TCP_CLOSE);

        public const int TCPF_ESTABLISHED = (1 << TCP_ESTABLISHED);
        public const int TCPF_SYN_SENT = (1 << TCP_SYN_SENT);
        public const int TCPF_SYN_RECV = (1 << TCP_SYN_RECV);
        public const int TCPF_FIN_WAIT1 = (1 << TCP_FIN_WAIT1);
        public const int TCPF_FIN_WAIT2 = (1 << TCP_FIN_WAIT2);
        public const int TCPF_TIME_WAIT = (1 << TCP_TIME_WAIT);
        public const int TCPF_CLOSE = (1 << TCP_CLOSE);
        public const int TCPF_CLOSE_WAIT = (1 << TCP_CLOSE_WAIT);
        public const int TCPF_LAST_ACK = (1 << TCP_LAST_ACK);
        public const int TCPF_LISTEN = (1 << TCP_LISTEN);
        public const int TCPF_CLOSING = (1 << TCP_CLOSING);
        public const int TCPF_NEW_SYN_RECV = (1 << TCP_NEW_SYN_RECV);
        public const int TCPF_BOUND_INACTIVE = (1 << TCP_BOUND_INACTIVE);

        public const int PACKET_HOST = 0;

        //CHECKSUM_BREAK 是一个在 Linux 内核中使用的宏，用于确定在何时需要对整个数据包进行完整的校验和计算。
        //这个宏的值通常设置为一个特定的字节数，当数据包的长度小于或等于这个值时，内核会计算整个数据包的校验和，而不是仅计算伪头部校验和。
        //完整校验和计算：当数据包的长度小于或等于 CHECKSUM_BREAK 时，内核会计算整个数据包的校验和，而不是仅计算伪头部校验和。
        //性能优化：通过设置 CHECKSUM_BREAK，内核可以在处理较短的数据包时进行完整的校验和计算，从而提高处理效率。
        public const int CHECKSUM_BREAK = 76;
        public const bool ip4_min_ttl = false;
        public const int TCP_SACK_BLOCKS_EXPECTED = 2;

        public const int TCP_DEFAULT_SCALING_RATIO = 1 << (TCP_RMEM_TO_WIN_SCALE - 1);

        public const int TCP_BASE_MSS = 1024;
        public const int TCP_PROBE_INTERVAL = 600;
        public const int TCP_PROBE_THRESHOLD = 8;

        public const int TCP_KEEPALIVE_TIME = (2 * 60 * 60 * HZ);  /* two hours */
        public const int TCP_KEEPALIVE_PROBES = 9; 
        public const int TCP_KEEPALIVE_INTVL = (75 * HZ);

        public const int TCP_SYN_RETRIES = 6;
        public const int TCP_SYNACK_RETRIES = 5;
        public const int TCP_RETR1 = 3;
        public const int TCP_RETR2 = 15;
        public const int TCP_TIMEWAIT_LEN = (60 * HZ);
        public const int TCP_FIN_TIMEOUT = TCP_TIMEWAIT_LEN;

        public const int TCP_CA_UNSPEC = 0;
    }
}
