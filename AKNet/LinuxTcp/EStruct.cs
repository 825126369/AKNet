namespace AKNet.LinuxTcp
{
    public enum tcp_skb_cb_sacked_flags
    {
        TCPCB_SACKED_ACKED = (1 << 0),  /* SKB ACK'd by a SACK block	*/
        TCPCB_SACKED_RETRANS = (1 << 1),    /* SKB retransmitted		*/
        TCPCB_LOST = (1 << 2),  /* SKB is lost			*/
        TCPCB_TAGBITS = (TCPCB_SACKED_ACKED | TCPCB_SACKED_RETRANS | TCPCB_LOST), /* All tag bits			*/
        TCPCB_REPAIRED = (1 << 4),  /* SKB repaired (no skb_mstamp_ns)	*/
        TCPCB_EVER_RETRANS = (1 << 7),  /* Ever retransmitted frame	*/
        TCPCB_RETRANS = (TCPCB_SACKED_RETRANS | TCPCB_EVER_RETRANS | TCPCB_REPAIRED),
    }

    public enum TCP_STATE
    {
        TCP_ESTABLISHED = 1,

        TCP_SYN_SENT,
        TCP_SYN_RECV,
        TCP_FIN_WAIT1,
        TCP_FIN_WAIT2,
        TCP_TIME_WAIT,
        TCP_CLOSE,
        TCP_CLOSE_WAIT,
        TCP_LAST_ACK,

        TCP_LISTEN,
        TCP_CLOSING,    /* Now a valid state */
        TCP_NEW_SYN_RECV,
        TCP_BOUND_INACTIVE, /* Pseudo-state for inet_diag */

        TCP_MAX_STATES  /* Leave at the end! */
    }

    public enum TCPF_STATE
    {
        TCPF_ESTABLISHED = (1 << TCP_STATE.TCP_ESTABLISHED),
        TCPF_SYN_SENT = (1 << TCP_STATE.TCP_SYN_SENT),
        TCPF_SYN_RECV = (1 << TCP_STATE.TCP_SYN_RECV),
        TCPF_FIN_WAIT1 = (1 << TCP_STATE.TCP_FIN_WAIT1),
        TCPF_FIN_WAIT2 = (1 << TCP_STATE.TCP_FIN_WAIT2),
        TCPF_TIME_WAIT = (1 << TCP_STATE.TCP_TIME_WAIT),
        TCPF_CLOSE = (1 << TCP_STATE.TCP_CLOSE),
        TCPF_CLOSE_WAIT = (1 << TCP_STATE.TCP_CLOSE_WAIT),
        TCPF_LAST_ACK = (1 << TCP_STATE.TCP_LAST_ACK),
        TCPF_LISTEN = (1 << TCP_STATE.TCP_LISTEN),
        TCPF_CLOSING = (1 << TCP_STATE.TCP_CLOSING),
        TCPF_NEW_SYN_RECV = (1 << TCP_STATE.TCP_NEW_SYN_RECV),
        TCPF_BOUND_INACTIVE = (1 << TCP_STATE.TCP_BOUND_INACTIVE),
    }

    public enum tcp_ca_state
    {
        TCP_CA_Open = 0,
        TCP_CA_Disorder = 1,
        TCP_CA_CWR = 2,
        TCP_CA_Recovery = 3,
        TCP_CA_Loss = 4
    }

    public class tcphdr
    {
        ushort source;
        ushort dest;
        uint seq;
        uint ack_seq;

        ushort res1;
        ushort doff;
        ushort fin;
        ushort syn;
        ushort rst;
        ushort psh;
        ushort ack;
        ushort urg;
        ushort ece;
        ushort cwr;

        ushort window;
        ushort check;
        ushort urg_ptr;
    }

    public enum tcp_chrono
    {
        TCP_CHRONO_UNSPEC,
        TCP_CHRONO_BUSY, //标记连接处于活跃发送数据的状态（即写队列非空）。这表明应用程序正在积极地向网络发送数据。
        TCP_CHRONO_RWND_LIMITED, //表明连接由于接收窗口不足而被阻塞。这意味着接收端的窗口大小不足以容纳更多数据，导致发送端暂停发送新数据直到窗口空间可用。
        TCP_CHRONO_SNDBUF_LIMITED, //指出连接因发送缓冲区不足而被限制。当本地系统的发送缓冲区已满时，应用程序将无法继续发送数据，直到有足够的空间释放出来。
        __TCP_CHRONO_MAX,
    }

    public enum sock_flags
    {
        SOCK_DEAD,
        SOCK_DONE,
        SOCK_URGINLINE,
        SOCK_KEEPOPEN,
        SOCK_LINGER,
        SOCK_DESTROY,
        SOCK_BROADCAST,
        SOCK_TIMESTAMP,
        SOCK_ZAPPED,
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
}
