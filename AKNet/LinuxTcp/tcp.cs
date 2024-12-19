using AKNet.Common;
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

    //是 Linux 内核 TCP 协议栈中用于收集和存储与传输速率相关的统计信息的一个结构体。
    //它主要用于拥塞控制算法，特别是那些需要基于详细的流量反馈来调整发送速率的高级算法（如 BBR）。
    //这个结构体帮助算法理解当前网络状况，从而更智能地管理数据包的发送。
    internal class rate_sample
    {
        public long prior_mstamp; //表示采样区间的开始时间戳，单位为微秒。这有助于计算不同时间段内的性能指标。
        public uint prior_delivered; //记录在 prior_mstamp 时点之前已成功交付的数据包数量。这提供了基准线，以便后续比较。
        public uint prior_delivered_ce; //记录在 prior_mstamp 时点之前带有 ECN（Explicit Congestion Notification）CE 标记的数据包数量。这对于评估网络中的拥塞情况非常重要。
        public int delivered;      //表示在此采样区间内新交付的数据包数量。正值表示有新的数据包被确认，负值可能表示丢失或重传。
        public int delivered_ce;   //表示在此采样区间内带有 CE 标记的新交付的数据包数量。这反映了网络拥塞的程度。
        public long interval_us;   //表示从 prior_delivered 到当前 delivered 的增量所花费的时间，单位为微秒。这对于计算吞吐量和其他时间敏感的指标非常有用。
        public uint snd_interval_us;    //表示发送端发送这些数据包所花费的时间，单位为微秒。这有助于了解发送端的性能。
        public uint rcv_interval_us; //表示接收端接收到这些数据包所花费的时间，单位为微秒。这有助于了解接收端的性能。
        public long rtt_us;    //表示最后一个 (S)ACKed 数据包的往返时间（RTT），单位为微秒。如果无法测量，则设置为 -1。
        public int losses; //表示在此 ACK 上标记为丢失的数据包数量。这对于检测和处理丢包事件非常重要。
        public uint acked_sacked;   //表示在此 ACK 上新确认（包括 SACKed）的数据包数量。这有助于了解有多少数据被成功接收。
        public uint prior_in_flight;    //表示在此 ACK 到达之前仍然在网络中的数据包数量（即“飞行中”的数据包）。这对于评估当前网络负载和潜在的拥塞情况非常有用。
        public uint last_end_seq; //表示最近被 ACK 确认的数据包的结束序列号。这有助于跟踪最新的传输状态。
        public bool is_app_limited;  //指示此样本是否来自一个应用程序受限的场景，即发送方的应用程序未能及时提供足够的数据进行发送。这有助于区分网络拥塞和应用程序行为的影响。
        public bool is_retrans;    //指示此样本是否来自重传的数据包。
        public bool is_ack_delayed;   //指示此 ACK 是否可能是延迟 ACK
    }

    /*
        ECN 通过在 IP 数据包头部和 TCP 报头中使用两个标志位来工作：
        ECT (ECN-Capable Transport)：表示该数据包来自一个支持 ECN 的传输层协议。
        ECT(0) 和 ECT(1) 表示两种不同的编码方式，但都表明数据包是 ECN 能力的。
        CE (Congestion Experienced)：当路径中的某个路由器经历了拥塞并选择不丢弃数据包时，会将此位设置为 1。 
     */

    public interface module
    {

    }


    internal class tcp_congestion_ops
    {
        public Func<tcp_sock, uint> ssthresh;
        public Action<tcp_sock, uint, uint> cong_avoid;
        public Action<tcp_sock, tcp_ca_state> set_state;

        public Action<tcp_sock, tcp_ca_event> cwnd_event;
        public Action<tcp_sock, uint> in_ack_event;
        public Action<tcp_sock, ack_sample> pkts_acked;
        public Func<tcp_sock, uint> min_tso_segs;
        public Action<tcp_sock, uint, int, rate_sample> cong_control;
        public Func<tcp_sock, uint> undo_cwnd;
        public Func<tcp_sock, uint> sndbuf_expand;
        public Func<tcp_sock, uint, int, long> get_info;

        public string name;
        public module owner;
        public uint key;
        public uint flags;

        public Action<tcp_sock> init;
        public Action<tcp_sock> release;
    }

    public class tcp_options_received
    {
        public int ts_recent_stamp; //存储最近一次更新 ts_recent 的时间戳，用于老化机制
        public uint ts_recent; //下一个要回显的时间戳值。
        public uint rcv_tsval;  //接收到的时间戳值。
        public uint rcv_tsecr;  //接收到的时间戳回显回复。
        public ushort saw_tstamp; //如果上一个包包含时间戳选项，则为1。
        public ushort tstamp_ok;  //如果在SYN包中看到时间戳选项，则为1。
        public ushort dsack;  //如果调度了D-SACK（选择性确认重复数据段），则为1。
        public ushort wscale_ok;  //如果在SYN包中看到了窗口缩放选项，则为1。
        public ushort sack_ok;   // 表示SACK（选择性确认）选项的状态，用3位表示，可能是因为需要表示不同的SACK状态或级别。
        public ushort smc_ok; //如果在SYN包中看到了SMC（Software Module Communication）选项，则为1。
        public ushort snd_wscale; //发送方从接收方接收到的窗口缩放因子。
        public ushort rcv_wscale; //发送给发送方的窗口缩放因子。
        public byte saw_unknown; //如果接收到未知选项，则为1。
        public byte unused; //未使用的位。
        public byte num_sacks;  // SACK块的数量。
        public ushort user_mss; //用户通过ioctl请求的最大报文段大小。
        public ushort mss_clamp;  //在连接设置期间协商的最大MSS（最大报文段大小）。
    }

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
            return tp.tcp_rtx_queue.isEmpty();
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
            return ((int)(tcpf_ca_state.TCPF_CA_CWR | tcpf_ca_state.TCPF_CA_Recovery) & (1 << tp.icsk_ca_state)) > 0;
        }

        public static void tcp_ca_event_func(tcp_sock tp, tcp_ca_event mEvent)
        {
            if (tp.icsk_ca_ops.cwnd_event != null)
            {
                tp.icsk_ca_ops.cwnd_event(tp, mEvent);
            }
        }

        public static uint tcp_snd_cwnd(tcp_sock tp)
        {
            return tp.snd_cwnd;
        }

        public static void tcp_snd_cwnd_set(tcp_sock tp, uint val)
        {
            NetLog.Assert((int)val > 0);
            tp.snd_cwnd = val;
        }

        public static int tcp_is_sack(tcp_sock tp)
        {
            return tp.rx_opt.sack_ok;
        }

        public static bool tcp_is_reno(tcp_sock tp)
        {
            return tcp_is_sack(tp) == 0;
        }

        public static uint tcp_left_out(tcp_sock tp)
        {
            return tp.sacked_out + tp.lost_out;
        }

        public static uint tcp_packets_in_flight(tcp_sock tp)
        {
            return tp.packets_out - tcp_left_out(tp) + tp.retrans_out;
        }

        public static sk_buff tcp_rtx_queue_head(tcp_sock tp)
        {
            return skb_rb_first(tp.tcp_rtx_queue);
        }

        public static tcp_skb_cb TCP_SKB_CB(sk_buff __skb)
        {
            return __skb.cb[0];
        }

        public static int tcp_skb_pcount(sk_buff skb)
        {
            return TCP_SKB_CB(skb).tcp_gso_segs;
        }

        public static void tcp_clear_retrans_hints_partial(tcp_sock tp)
        {
            tp.lost_skb_hint = null;
        }

        public static void tcp_clear_all_retrans_hints(tcp_sock tp)
        {
            tcp_clear_retrans_hints_partial(tp);
            tp.retransmit_skb_hint = null;
        }

        public static long tcp_stamp_us_delta(long t1, long t0)
        {
            return Math.Max(t1 - t0, 0);
        }

        public static long tcp_skb_timestamp_us(sk_buff skb)
        {
	        return skb.skb_mstamp_ns / 1000;
        }
    }
}
