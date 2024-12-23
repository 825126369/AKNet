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

    }
}
