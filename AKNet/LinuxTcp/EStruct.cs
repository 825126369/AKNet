using System;
using System.Collections.Generic;
using System.Text;

namespace AKNet.LinuxTcp
{
    enum tcp_skb_cb_sacked_flags
    {
        TCPCB_SACKED_ACKED = (1 << 0),  /* SKB ACK'd by a SACK block	*/
        TCPCB_SACKED_RETRANS = (1 << 1),    /* SKB retransmitted		*/
        TCPCB_LOST = (1 << 2),  /* SKB is lost			*/
        TCPCB_TAGBITS = (TCPCB_SACKED_ACKED | TCPCB_SACKED_RETRANS | TCPCB_LOST), /* All tag bits			*/
        TCPCB_REPAIRED = (1 << 4),  /* SKB repaired (no skb_mstamp_ns)	*/
        TCPCB_EVER_RETRANS = (1 << 7),  /* Ever retransmitted frame	*/
        TCPCB_RETRANS = (TCPCB_SACKED_RETRANS | TCPCB_EVER_RETRANS | TCPCB_REPAIRED),
    };

    enum tcp_state
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

    enum tcp_ca_state
    {
        TCP_CA_Open = 0,
        TCP_CA_Disorder = 1,
        TCP_CA_CWR = 2,
        TCP_CA_Recovery = 3,
        TCP_CA_Loss = 4
    }

    struct tcphdr
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
    };

    enum tcp_chrono
    {
        TCP_CHRONO_UNSPEC,
        TCP_CHRONO_BUSY, //标记连接处于活跃发送数据的状态（即写队列非空）。这表明应用程序正在积极地向网络发送数据。
        TCP_CHRONO_RWND_LIMITED, //表明连接由于接收窗口不足而被阻塞。这意味着接收端的窗口大小不足以容纳更多数据，导致发送端暂停发送新数据直到窗口空间可用。
        TCP_CHRONO_SNDBUF_LIMITED, //指出连接因发送缓冲区不足而被限制。当本地系统的发送缓冲区已满时，应用程序将无法继续发送数据，直到有足够的空间释放出来。
        __TCP_CHRONO_MAX,
    };
}
