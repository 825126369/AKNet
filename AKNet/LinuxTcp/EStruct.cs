/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal enum sk_family
    {
        AF_INET	=	2,	/* Internet IP Protocol 	*/
    }

    internal enum inet_csk_ack_state_t
    {
        ICSK_ACK_SCHED = 1,// ACK 已被安排发送
        ICSK_ACK_TIMER = 2,// 使用定时器来触发 ACK 发送
        ICSK_ACK_PUSHED = 4,// ACK 已经被“推送”出去（即已经准备好发送）
        ICSK_ACK_PUSHED2 = 8,// 另一个 ACK 推送标记，可能用于特定场景下的额外确认
        ICSK_ACK_NOW = 16,  // 立即发送下一个 ACK（仅一次）
        ICSK_ACK_NOMEM = 32,// 由于内存不足无法发送 ACK
    };

    internal enum tcp_skb_cb_sacked_flags
    {
        TCPCB_SACKED_ACKED = (1 << 0),  // 数据段已被 SACK 块确认
        TCPCB_SACKED_RETRANS = (1 << 1),    // 数据段已被重传
        TCPCB_LOST = (1 << 2),  // 数据段被认为已丢失
        TCPCB_TAGBITS = (TCPCB_SACKED_ACKED | TCPCB_SACKED_RETRANS | TCPCB_LOST), // 所有标签位
        TCPCB_REPAIRED = (1 << 4),  // 数据段已被修复（无 skb_mstamp_ns）
        TCPCB_EVER_RETRANS = (1 << 7),  // 数据段曾经被重传过
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
        TCP_CA_Open = 0, // 初始状态或没有检测到拥塞
        TCP_CA_Disorder = 1, //出现失序的数据包，但未确认丢失
        TCP_CA_CWR = 2, //进入拥塞窗口减少 (Congestion Window Reduced) 状态
        TCP_CA_Recovery = 3,// 恢复状态，当检测到丢失时进入此状态
        TCP_CA_Loss = 4 // 检测到数据包丢失，进入损失状态
    }

    public enum tcpf_ca_state
    {
        TCPF_CA_Open = (1 << tcp_ca_state.TCP_CA_Open),
        TCPF_CA_Disorder = (1 << tcp_ca_state.TCP_CA_Disorder),
        TCPF_CA_CWR = (1 << tcp_ca_state.TCP_CA_CWR),
        TCPF_CA_Recovery = (1 << tcp_ca_state.TCP_CA_Recovery),
        TCPF_CA_Loss = (1 << tcp_ca_state.TCP_CA_Loss)
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
