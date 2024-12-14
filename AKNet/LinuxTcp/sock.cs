using System;

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

    /*
     * tcp_write_timer //管理TCP发送窗口，并处理重传机制。
     * tcp_delack_timer //实现延迟ACK（Delayed ACK），减少不必要的ACK流量。
     * tcp_keepalive_timer;//用于检测长时间空闲的TCP连接是否仍然活跃。
     * pacing_timer//实施速率控制（Pacing），优化数据包的发送速率，避免突发流量导致的网络拥塞。
     * compressed_ack_timer //优化ACK报文的发送，特别是在高带宽延迟网络环境中。
    */
    internal struct tcp_sock
    {
        public const ushort TCP_MSS_DEFAULT = 536;
        public const int TCP_INIT_CWND = 10;

        public const ushort HZ = 1000;
        public const long TCP_RTO_MAX = 120 * HZ;
        public const long TCP_RTO_MIN = HZ / 5;
        public const long TCP_TIMEOUT_INIT = 1 * HZ;

        public const int TCP_FASTRETRANS_THRESH = 3;
        public const int sysctl_tcp_comp_sack_slack_ns = 100; //启动一个高分辨率定时器，用于管理TCP累积ACK的发送

        public int sk_wmem_queued;
        public int sk_forward_alloc;//这个字段主要用于跟踪当前套接字还可以分配多少额外的内存来存储数据包
        public uint max_window;//

        //这个字段用于跟踪已经通过套接字发送给应用层的数据序列号（sequence number）。具体来说，pushed_seq 表示最近一次调用 tcp_push() 或类似函数后，
        //TCP 层认为应该被“推送”到网络上的数据的最后一个字节的序列号加一。
        public uint pushed_seq;
        public uint write_seq;  //应用程序通过 send() 或 write() 系统调用写入到TCP套接字中的最后一个字节的序列号。
        public uint rtt_seq;
        public uint snd_nxt;    //Tcp层 下一个将要发送的数据段的第一个字节的序列号。
        public uint snd_una;//表示未被确认的数据段的第一个字节的序列号。
        public uint mss_cache;  //单个数据包的最大大小

        public uint snd_cwnd;     //表示当前允许发送方发送的最大数据量（以字节为单位)
        public uint prior_cwnd; //它通常指的是在某些特定事件发生之前的拥塞窗口（Congestion Window, cwnd）大小
        public uint copied_seq; //记录了应用程序已经从接收缓冲区读取的数据的最后一个字节的序列号（seq）加一，即下一个期待被用户空间读取的数据的起始序列号

        //用于记录当前在网络中飞行的数据包数量。这些数据包已经发送出去但还未收到确认（ACK）
        public uint packets_out;  //当前飞行中的数据包数量
        public uint retrans_out;  //表示当前正在重传的数据包数量
        public uint sacked_out;//表示已经被选择性确认SACK的数据包数量。
        public uint lost_out; // 表示被认为已经丢失的数据包数量
        public uint app_limited;

        //用于记录已经成功传递给应用程序的数据包总数。这个字段包括了所有已传递的数据包，即使这些数据包可能因为重传而被多次传递。
        public uint delivered;

        public ushort gso_segs; //它用于表示通过 Generic Segmentation Offload(GSO) 分段的数据包的数量。GSO 是一种优化技术，允许操作系统将大的数据包交给网卡，然后由网卡硬件负责将这些大包分段成适合底层网络传输的小包

        public long srtt_us; //表示平滑后的往返时间，单位为微秒。
        public long rttvar_us;//表示往返时间变化的估计值，也称为均方差（mean deviation），单位为微秒。用来衡量RTT测量值的变化程度，帮助调整RTO以适应网络条件的变化。
        public long mdev_us;//是RTT变化的一个估计值，类似于rttvar_us，但可能有不同的更新逻辑或用途。
        public long mdev_max_us;//跟踪最大均方差，即mdev_us的最大值。可能用于调试目的或者特定的算法需求，比如设置RTO的上限。

    }


    public static class LinuxTcpFunc
    {
        static void tcp_rtt_estimator(tcp_sock tp, long mrtt_us)
        {
            long m = mrtt_us;
            long srtt = tp.srtt_us;

            static bool after(uint seq1, uint seq2)
            {
                return (Int32)(seq1) - seq2 > 0;
            }
            static long tcp_rto_min_us()
            {
                return tcp_sock.TCP_RTO_MIN;
            }

            if (srtt != 0)
            {
                m -= (srtt >> 3);
                srtt += m;
                if (m < 0)
                {
                    m = -m;
                    m -= (tp.mdev_us >> 2);
                    if (m > 0)
                    {
                        m >>= 3;
                    }
                }
                else
                {
                    m -= (tp.mdev_us >> 2);
                }

                tp.mdev_us += m;
                if (tp.mdev_us > tp.mdev_max_us)
                {
                    tp.mdev_max_us = tp.mdev_us;
                    if (tp.mdev_max_us > tp.rttvar_us)
                    {
                        tp.rttvar_us = tp.mdev_max_us;
                    }
                }
                if (after(tp.snd_una, tp.rtt_seq))
                {
                    if (tp.mdev_max_us < tp.rttvar_us)
                    {
                        tp.rttvar_us -= (tp.rttvar_us - tp.mdev_max_us) >> 2;
                    }
                    tp.rtt_seq = tp.snd_nxt;
                    tp.mdev_max_us = tcp_rto_min_us();
                }
            }
            else
            {
                srtt = m << 3;
                tp.mdev_us = m << 1;
                tp.rttvar_us = Math.Max(tp.mdev_us, tcp_rto_min_us());
                tp.mdev_max_us = tp.rttvar_us;
                tp.rtt_seq = tp.snd_nxt;
            }
            tp.srtt_us = Math.Max(1U, srtt);
        }

        static void tcp_set_rto(tcp_sock tp)
        {
            long icsk_rto = 0;
            icsk_rto = (tp.srtt_us >> 3) + tp.rttvar_us;

            if (icsk_rto > tcp_sock.TCP_RTO_MAX)
            {
                icsk_rto = tcp_sock.TCP_RTO_MAX;
            }
        }
    }
}
