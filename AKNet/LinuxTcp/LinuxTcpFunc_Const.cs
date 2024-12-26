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

        public const int TCPOLEN_TSTAMP_ALIGNED = 12;
        public const int TCPOLEN_WSCALE_ALIGNED = 4;
        public const int TCPOLEN_SACKPERM_ALIGNED = 4;
        public const int TCPOLEN_SACK_BASE = 2;
        public const int TCPOLEN_SACK_BASE_ALIGNED = 4;
        public const int TCPOLEN_SACK_PERBLOCK = 8;
        public const int TCPOLEN_MD5SIG_ALIGNED = 20;
        public const int TCPOLEN_MSS_ALIGNED = 4;
        public const int TCPOLEN_EXP_SMC_BASE_ALIGNED = 8;

        public static readonly int SKBFL_ZEROCOPY_ENABLE = (int)LinuxTcpFunc.BIT(0);
        public static readonly int SKBFL_SHARED_FRAG = (int)LinuxTcpFunc.BIT(1);
        public static readonly int SKBFL_PURE_ZEROCOPY = (int)LinuxTcpFunc.BIT(2);
        public static readonly int SKBFL_DONT_ORPHAN = (int)LinuxTcpFunc.BIT(3);
        public static readonly int SKBFL_MANAGED_FRAG_REFS = (int)LinuxTcpFunc.BIT(4);

        public const int MAX_TCP_OPTION_SPACE = 40;
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

    }
}
