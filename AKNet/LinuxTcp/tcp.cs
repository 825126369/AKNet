using System;

namespace AKNet.LinuxTcp
{
    internal class tcp_skb_cb
    {
        internal class tx
        {
            public long TCPCB_DELIVERED_CE_MASK = ((1U << 20) - 1)
            public uint is_app_limited;
            public uint delivered_ce;
            public byte unused;
            public uint delivered;
            public long first_tx_mstamp;
            public long delivered_mstamp;
        }

        internal class header
        {
            inet_skb_parm h4;
        }

        public uint seq;      /* Starting sequence number	*/
        public uint end_seq;  /* SEQ + FIN + SYN + datalen	*/

        public ushort tcp_gso_segs;
        public ushort tcp_gso_size;

        public byte tcp_flags; /* TCP header flags. (tcp[13])	*/
        public byte sacked;        /* State flags for SACK.	*/
        public byte ip_dsfield;    /* IPv4 tos or IPv6 dsfield	*/
        public byte txstamp_ack;   /* Record TX timestamp for ack? */

        public byte eor;   /* Is skb MSG_EOR marked? */
        public byte has_rxtstamp;   /* SKB has a RX timestamp	*/
        public byte unused;
        public uint ack_seq;  /* Sequence number ACK'd	*/

        public tx tx;
        public header header;
    }

    /* Events passed to congestion control interface */
    internal enum tcp_ca_event
    {
        //描述：表示第一次传输数据包，此时没有其他数据包在飞行中（即网络中）。这通常发生在连接刚开始或长时间空闲后首次发送数据时。 作用：拥塞控制算法可以利用这个事件来初始化或重置某些状态变量，确保从一个干净的状态开始。
        CA_EVENT_TX_START,
        //描述：表示拥塞窗口（CWND）重启。当之前的数据包被确认并且新的数据包开始发送时触发此事件。作用：帮助算法根据最新的网络反馈调整 CWND 的大小，以优化性能和避免不必要的拥塞。
        CA_EVENT_CWND_RESTART,
        //描述：表示拥塞恢复（Congestion Window Reduction, CWR）完成。这意味着拥塞控制算法已经成功地处理了一次拥塞事件，并且恢复正常操作。作用：允许算法调整其内部状态，如重置阈值或其他参数，以便更好地应对未来的网络条件。
        CA_EVENT_COMPLETE_CWR, 
        //描述：表示发生了丢包超时（Loss Timeout），意味着某些数据包被认为在网络中丢失了。 作用：触发快速重传或进入慢启动阶段等机制，以尝试重新发送丢失的数据并调整 CWND 和阈值。
        CA_EVENT_LOSS, 
        //描述：表示接收到带有 ECN（Explicit Congestion Notification）标志但没有 CE（Congestion Experienced）标记的 IP 数据包。这意味着路径上的路由器支持 ECN 但当前并未经历拥塞。作用：算法可以根据此信息调整其行为，例如增加对潜在拥塞的敏感度
        CA_EVENT_ECN_NO_CE,
        //CA_EVENT_ECN_IS_CE：描述：表示接收到带有 CE 标记的 IP 数据包。CE 标记指示路径中的某个路由器经历了拥塞，并已对数据包进行了标记。作用：这是拥塞的一个明确信号，算法应立即采取措施减少发送速率，如减小 CWND 或设置 CWR 标志。
        CA_EVENT_ECN_IS_CE, /* received CE marked IP packet */
    };

    //是 Linux 内核 TCP 协议栈中用于存储与 ACK（确认）相关统计信息的一个结构体。
    //它主要用于拥塞控制算法，特别是那些需要基于延迟和吞吐量反馈来调整发送速率的算法，
    //如 BBR (Bottleneck Bandwidth and RTT)。
    //这个结构体帮助算法理解当前网络状况，从而更智能地管理数据包的发送。
    internal struct ack_sample
    {
        public uint pkts_acked; // 表示在这次 ACK 中被确认的数据包数量。这有助于算法了解有多少数据已经被成功接收，并据此调整发送窗口大小。
        public int rtt_us; //表示往返时间（Round-Trip Time, RTT），以微秒为单位。RTT 是衡量网络延迟的重要指标，对拥塞控制算法非常重要，因为它反映了从发送数据到接收到确认的时间。
        public uint in_flight;//表示在此次 ACK 到达之前，仍然在网络中的数据包数量（即“飞行中”的数据包）。这对于评估当前网络负载和潜在的拥塞情况非常有用。
    };

    internal class rate_sample
    {
        public long prior_mstamp; /* starting timestamp for interval */
        public uint prior_delivered;    /* tp->delivered at "prior_mstamp" */
        public uint prior_delivered_ce;/* tp->delivered_ce at "prior_mstamp" */
        public int delivered;      /* number of packets delivered over interval */
        public int delivered_ce;   /* number of packets delivered w/ CE marks*/
        public long interval_us;   /* time for tp->delivered to incr "delivered" */
        public uint snd_interval_us;    /* snd interval for delivered packets */
        public uint rcv_interval_us;    /* rcv interval for delivered packets */
        public long rtt_us;        /* RTT of last (S)ACKed packet (or -1) */
        public int losses;     /* number of packets marked lost upon ACK */
        public uint acked_sacked;   /* number of packets newly (S)ACKed upon ACK */
        public uint prior_in_flight;    /* in flight before this ACK */
        public uint last_end_seq;   /* end_seq of most recently ACKed packet */
        public bool is_app_limited;    /* is sample from packet with bubble in pipe? */
        public bool is_retrans;    /* is sample from retransmission? */
        public bool is_ack_delayed;    /* is this (likely) a delayed ACK? */
    }

    internal class tcp_congestion_ops
    {
        public Func<tcp_sock, uint> ssthresh;
        public Action<tcp_sock, uint, uint> cong_avoid;
        public Action<tcp_sock, byte> set_state;

        public Action<tcp_sock, tcp_ca_event> cwnd_event;
        public Action<tcp_sock, uint> in_ack_event;
        public Action<tcp_sock, ack_sample> pkts_acked;
        public Func<tcp_sock, uint> min_tso_segs;
        public Action<tcp_sock, uint, int, rate_sample> cong_control;


	/* new value of cwnd after loss (required) */
	u32(*undo_cwnd)(struct sock *sk);
	/* returns the multiplier used in tcp_sndbuf_expand (optional) */
	u32(*sndbuf_expand)(struct sock *sk);

/* control/slow paths put last */
	/* get info for inet_diag (optional) */
	size_t(*get_info)(struct sock *sk, u32 ext, int* attr,
               union tcp_cc_info* info);

        char name[TCP_CA_NAME_MAX];
        struct module       *owner;
	struct list_head    list;
	u32 key;
        u32 flags;

        /* initialize private data (optional) */
        void (* init) (struct sock *sk);
	/* cleanup private data  (optional) */
	void (* release) (struct sock *sk);
}
    ____cacheline_aligned_in_smp;


    internal static partial class LinuxTcpFunc
    {
        public static long tcp_timeout_init(tcp_sock tp)
        {
            long timeout = tcp_sock.TCP_TIMEOUT_INIT;
            return Math.Min(timeout, tcp_sock.TCP_RTO_MAX);
        }

        public static bool tcp_write_queue_empty(tcp_sock tp)
        {
            return tp.write_seq == tp.snd_nxt;
        }

        public static bool tcp_rtx_queue_empty(tcp_sock tp)
        {
            return tp.tcp_rtx_queue.rb_node == null;
        }

        public static bool tcp_rtx_and_write_queues_empty(tcp_sock tp)
        {
            return tcp_rtx_queue_empty(tp) && tcp_write_queue_empty(tp);
        }

        public static void tcp_write_queue_purge(tcp_sock tp)
        {
            //sk_buff skb;
            //tcp_chrono_stop(tp, tcp_chrono.TCP_CHRONO_BUSY);
            //while ((skb = __skb_dequeue(tp.sk_write_queue)) != null) 
            //   {
            // tcp_skb_tsorted_anchor_cleanup(skb);
            //   }

            //   tcp_rtx_queue_purge(sk);
            //   INIT_LIST_HEAD(&tcp_sk(sk)->tsorted_sent_queue);
            //   tcp_clear_all_retrans_hints(tcp_sk(sk));
            //   tcp_sk(sk)->packets_out = 0;
            //inet_csk(sk)->icsk_backoff = 0;
        }

        public static long tcp_skb_timestamp_ts(bool usec_ts, sk_buff skb)
        {
            return skb.skb_mstamp_ns / 1000;
        }

        public static bool before(uint seq1, uint seq2)
        {
            return (int)(seq1 - seq2) < 0;
        }

        public static bool after(uint seq1, uint seq2)
        {
            return before(seq2, seq1);
        }

        public static uint tcp_current_ssthresh(tcp_sock tp)
        {
            if (tcp_in_cwnd_reduction(tp))
            {
                return tp.snd_ssthresh;
            }
            else
            {
                return Math.Max(tp.snd_ssthresh, ((tp.snd_cwnd >> 1) + (tp.snd_cwnd >> 2)));
            }
        }

        public static bool tcp_in_cwnd_reduction(tcp_sock tp)
        {
            return (tcp_ca_state.TCPF_CA_CWR | TCPF_STATE.TCPF_CA_Recovery) & (1 << inet_csk(sk)->icsk_ca_state);
        }

    }
}
