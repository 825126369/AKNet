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

        public const int CONFIG_MAX_SKB_FRAGS = 17;
        public const int MAX_SKB_FRAGS = CONFIG_MAX_SKB_FRAGS;

        public const int TCP_INIT_CWND = 10;

        public const int TCP_SKB_MIN_TRUESIZE = 2048;
        public const int SOCK_MIN_SNDBUF = (TCP_SKB_MIN_TRUESIZE * 2);
        public const int SOCK_MIN_RCVBUF = TCP_SKB_MIN_TRUESIZE;

        public const int TCP_TIMEOUT_MIN_US = (int)(2 * USEC_PER_MSEC); /* Min TCP timeout in microsecs */
        public const int TCP_TIMEOUT_INIT = 1 * tcp_sock.HZ;	/* RFC6298 2.1 initial RTO value	*/


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
    }

}
