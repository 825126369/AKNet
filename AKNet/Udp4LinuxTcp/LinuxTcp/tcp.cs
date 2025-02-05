/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Diagnostics;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class tx
    {
        public uint is_app_limited; //表示应用层是否限制了 cwnd（拥塞窗口）的使用。
        public uint delivered_ce;//记录收到 ECN-CE（Congestion Experienced）标记的数据包数量。
        public byte unused;
        public uint delivered;//记录已确认的数据包数量。
        public long first_tx_mstamp;//记录第一次传输的时间戳。
        public long delivered_mstamp;//记录达到 delivered 计数时的时间戳。

        public void CopyFrom(tx other)
        {
            this.is_app_limited = other.is_app_limited;
            this.delivered_ce = other.delivered_ce;
            this.unused = other.unused;
            this.delivered = other.delivered;
            this.first_tx_mstamp = other.first_tx_mstamp;
            this.delivered_mstamp = other.delivered_mstamp;
        }
    }

    internal class header
    {
        public inet_skb_parm h4;
    }

    internal class tcp_skb_cb
    {
        public uint seq; //表示数据包的起始序列号
        public uint end_seq; //表示数据包的结束序列号（End sequence number），包括 FIN、SYN 和实际数据长度。

        public byte tcp_flags; //存储 TCP 头部标志位（如 SYN、ACK、FIN 等），通常对应于 TCP 头部的第 13 字节。
        public byte sacked;     //获取 SACK 选项在 TCP 头部的偏移量
        public byte ip_dsfield;   //存储 IP 数据报的服务类型（IPv4 TOS 或 IPv6 DSFIELD），用于 QoS 控制。
        public byte txstamp_ack;   //如果设置为 1，表示需要记录发送时间戳以供 ACK 使用。
        
        //标记最后一个数据包：
        //eor 字段用于标记一个 TCP 数据包是否是当前发送操作的最后一个数据包。当 eor 被设置为 1 时，表示该数据包是最后一个数据包。
        //这个字段在 tcp_sendmsg 和 tcp_sendpage 函数中使用，当传递 MSG_EOR 标志时，eor 字段会被设置。
        //避免合并数据包：当 eor 字段被设置时，内核会避免将后续的数据包合并到当前数据包中。这有助于确保数据的完整性和顺序性。
        //优化性能：通过正确设置 eor 字段，可以优化 TCP 数据的发送和接收，提高网络性能。
        //eor:用于标记该数据包是否被设置为消息结束（End Of Record, EOR）。
        public byte eor;  
        public bool has_rxtstamp;  //如果设置为 1，表示该数据包包含接收时间戳。
        public byte unused;
        public uint ack_seq;  //表示被确认的序列号（Sequence number ACK'd）。

        public tx tx = new tx(); //包含与发送路径相关的字段，主要用于出站数据包
        public header header = new header(); //包含与接收路径相关的字段，主要用于入站数据包：h4: 存储 IPv4 相关参数。h6: 如果启用了 IPv6 支持，则存储 IPv6 相关参数。

        public void CopyFrom(tcp_skb_cb other)
        {
            this.seq = other.seq;
            this.end_seq = other.end_seq;
            this.tcp_flags = other.tcp_flags;
            this.sacked = other.sacked;

            this.ip_dsfield = other.ip_dsfield;
            this.txstamp_ack = other.txstamp_ack;
            this.eor = other.eor;
            this.has_rxtstamp = other.has_rxtstamp;
            this.unused = other.unused;
            this.ack_seq = other.ack_seq;

            this.tx.CopyFrom(other.tx);
        }
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
        public long rtt_us; //表示往返时间（Round-Trip Time, RTT），以微秒为单位。RTT 是衡量网络延迟的重要指标，对拥塞控制算法非常重要，因为它反映了从发送数据到接收到确认的时间。
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

        //pkts_acked 是一个在TCP协议栈中常用的术语，特别是在Linux内核的TCP实现中。
        //它通常用来表示已经被确认（Acknowledged）的数据包数量，即发送方已经收到接收方对这些数据包的ACK（确认）消息。
        public Action<tcp_sock, ack_sample> pkts_acked;
        public Func<tcp_sock, uint> min_tso_segs;
        public Action<tcp_sock, uint, int, rate_sample> cong_control;
        public Func<tcp_sock, uint> undo_cwnd;
        public Func<tcp_sock, uint> sndbuf_expand;
        public Func<tcp_sock, uint, int, long> get_info;

        public string name;
        public uint flags;

        public Action<tcp_sock> init;
        public Action<tcp_sock> release;
    }

    public class tcp_options_received
    {
        public long ts_recent_stamp; //存储最近一次更新 ts_recent 的时间戳，用于老化机制
        public long ts_recent; //下一个要回显的时间戳值。
        public long rcv_tsval;  //时间戳值（TSVal）是发送方在发送数据包时附带的当前时间值。这个值是一个 32 位的无符号整数，通常以毫秒为单位。
        public long rcv_tsecr;  //是发送方在发送 ACK 数据包时附带的，表示上次接收到的数据包的 TSVal。这个值也是一个 32 位的无符号整数。
        public bool saw_tstamp; //如果上一个包包含时间戳选项，则为1。
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
        public ushort mss_clamp;  //在连接设置期间协商的最大MSS（最大报文段大小）。
    }

    public class rcv_rtt_est
    {
        public long rtt_us;
        public uint seq;
        public long time;
    }

    public class rcvq_space
    {
        public uint space;
        public uint seq;
        public long time;
    }

    internal static partial class LinuxTcpFunc
    {
        static tcp_sack_block_wire[] get_sp_wire(sk_buff skb)
        {
            if (skb.sp_wire_cache == null)
            {
                ReadOnlySpan<byte> ptr = skb_transport_header(skb).Slice(TCP_SKB_CB(skb).sacked);
                int nLength = (ptr[1] - TCPOLEN_SACK_BASE) / TCPOLEN_SACK_PERBLOCK;
                skb.sp_wire_cache = new tcp_sack_block_wire[nLength];
                for (int i = 0; i < nLength; i++)
                {
                    skb.sp_wire_cache[i] = new tcp_sack_block_wire();
                    skb.sp_wire_cache[i].start_seq = EndianBitConverter.ToUInt32(ptr, 2 + i * 2);
                    skb.sp_wire_cache[i].end_seq = EndianBitConverter.ToUInt32(ptr, 2 + i * 2 + 4);
                }
            }
            return skb.sp_wire_cache;
        }

        public static tcphdr tcp_hdr(sk_buff skb)
        {
            if (skb.tcphdr_cache == null)
            {
                skb.tcphdr_cache = new tcphdr();
                skb.tcphdr_cache.WriteFrom(skb);
            }
            return skb.tcphdr_cache;
        }

        static bool tcp_ca_needs_ecn(tcp_sock tp)
        {
            return BoolOk(tp.icsk_ca_ops.flags & TCP_CONG_NEEDS_ECN);
        }

        public static long tcp_timeout_init(tcp_sock tp)
        {
            long timeout = TCP_TIMEOUT_INIT;
            return Math.Min(timeout, TCP_RTO_MAX);
        }

        public static bool tcp_write_queue_empty(tcp_sock tp)
        {
            return tp.write_seq == tp.snd_nxt;
        }

        public static bool tcp_rtx_queue_empty(tcp_sock tp)
        {
            return RB_EMPTY_ROOT(tp.tcp_rtx_queue);
        }

        public static bool tcp_rtx_and_write_queues_empty(tcp_sock tp)
        {
            return tcp_rtx_queue_empty(tp) && tcp_write_queue_empty(tp);
        }

        static void tcp_rtx_queue_purge(tcp_sock tp)
        {
            rb_node p = rb_first(tp.tcp_rtx_queue);
            tp.highest_sack = null;
            while (p != null)
            {
                sk_buff skb = rb_to_skb(p);
                p = rb_next(p);
                tcp_rtx_queue_unlink(skb, tp);
            }
        }

        //用于清空 TCP 套接字的发送缓冲区（即写队列）。
        //这在某些情况下非常有用，例如当需要立即终止连接或重置连接时，可以确保所有待发送的数据都被丢弃。
        public static void tcp_write_queue_purge(tcp_sock tp)
        {
            sk_buff skb = null;
            tcp_chrono_stop(tp, tcp_chrono.TCP_CHRONO_BUSY);
            while ((skb = __skb_dequeue(tp.sk_write_queue)) != null)
            {

            }

            tcp_rtx_queue_purge(tp);
            INIT_LIST_HEAD(tp.tsorted_sent_queue);
            tcp_clear_all_retrans_hints(tp);
            tp.packets_out = 0;
            tp.icsk_backoff = 0;
        }

        public static long tcp_skb_timestamp_ts(sk_buff skb)
        {
            return skb.skb_mstamp;
        }

        public static bool before(uint seq1, uint seq2)
        {
            return (int)(seq1 - seq2) < 0;
        }

        public static bool after(uint seq1, uint seq2)
        {
            return before(seq2, seq1);
        }

        static bool between(uint seq1, uint seq2, uint seq3)
        {
            return seq3 - seq2 >= seq1 - seq2;
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
            Debug.Assert((int)val > 0);
            tp.snd_cwnd = val;
        }

        public static bool tcp_is_sack(tcp_sock tp)
        {
            return tp.rx_opt.sack_ok > 0;
        }

        public static bool tcp_is_reno(tcp_sock tp)
        {
            return !tcp_is_sack(tp);
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
            if (__skb.tcp_skb_cb_cache == null)
            {
                __skb.tcp_skb_cb_cache = new tcp_skb_cb();
            }
            return __skb.tcp_skb_cb_cache;
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
            return skb.skb_mstamp;
        }

        public static uint tcp_wnd_end(tcp_sock tp)
        {
            return tp.snd_una + tp.snd_wnd;
        }

        public static sk_buff tcp_rtx_queue_tail(tcp_sock tp)
        {
            return skb_rb_last(tp.tcp_rtx_queue);
        }

        public static void TCP_ADD_STATS(net net, TCPMIB field, int nCount)
        {
            (net).mib.tcp_statistics.mibs[(int)field] += nCount;
        }

        public static bool tcp_stream_is_thin(tcp_sock tp)
        {
            return tp.packets_out < 4 && !tcp_in_initial_slowstart(tp);
        }

        public static bool tcp_in_initial_slowstart(tcp_sock tp)
        {
            return tp.snd_ssthresh >= TCP_INFINITE_SSTHRESH;
        }

        public static bool tcp_skb_can_collapse(sk_buff to, sk_buff from)
        {
            return tcp_skb_can_collapse_to(to);
        }

        //是否可以合并sk_buff
        public static bool tcp_skb_can_collapse_to(sk_buff skb)
        {
            return TCP_SKB_CB(skb).eor == 0;
        }

        public static void tcp_highest_sack_replace(tcp_sock tp, sk_buff old, sk_buff newBuff)
        {
            if (old == tp.highest_sack)
            {
                tp.highest_sack = newBuff;
            }
        }

        static uint tcp_receive_window(tcp_sock tp)
        {
            int win = (int)tp.rcv_wup + (int)tp.rcv_wnd - (int)tp.rcv_nxt;
            if (win < 0)
            {
                win = 0;
            }
            return (uint)win;
        }

        static long tcp_space(tcp_sock tp)
        {
            return tcp_win_from_space(tp, tp.sk_rcvbuf - tp.sk_rmem_alloc);
        }

        static long tcp_full_space(tcp_sock tp)
        {
            return tcp_win_from_space(tp, tp.sk_rcvbuf);
        }
        
        //计算接收窗口大小：根据 space 和 tcp_adv_win_scale 计算实际的接收窗口大小。
        //tcp_adv_win_scale：内核参数 sysctl_tcp_adv_win_scale，用于调整接收窗口大小。其值可以是正数、负数或零。
        //正数：表示接收窗口大小为 space - (space >> tcp_adv_win_scale)。
        //负数：表示接收窗口大小为 space >> (-tcp_adv_win_scale)。
        //零：表示接收窗口大小为 space
        static long tcp_win_from_space(tcp_sock tp, long space)
        {
            return __tcp_win_from_space(tp.scaling_ratio, space);
        }

        static long __tcp_win_from_space(byte scaling_ratio, long space)
        {
            NetLog.Assert(scaling_ratio > 0, "scaling_ratio： " + scaling_ratio);
            long scaled_space = (long)space * scaling_ratio;
            return scaled_space >> TCP_RMEM_TO_WIN_SCALE;
        }

        static bool tcp_under_memory_pressure(tcp_sock tp)
        {
            return false;
        }

        static void tcp_adjust_rcv_ssthresh(tcp_sock tp)
        {
            __tcp_adjust_rcv_ssthresh(tp, (uint)4 * tp.advmss);
        }

        static void __tcp_adjust_rcv_ssthresh(tcp_sock tp, uint new_ssthresh)
        {
            int unused_mem = sk_unused_reserved_mem(tp);
            tp.rcv_ssthresh = Math.Min(tp.rcv_ssthresh, new_ssthresh);
            if (unused_mem > 0)
            {
                tp.rcv_ssthresh = (uint)Math.Max(tp.rcv_ssthresh, tcp_win_from_space(tp, unused_mem));
            }
        }

        static void tcp_dec_quickack_mode(tcp_sock tp)
        {
            if (tp.icsk_ack.quick > 0)
            {
                uint pkts = (uint)(inet_csk_ack_scheduled(tp) ? 1 : 0);
                if (pkts >= tp.icsk_ack.quick)
                {
                    tp.icsk_ack.quick = 0;
                    tp.icsk_ack.ato = TCP_ATO_MIN;
                }
                else
                {
                    tp.icsk_ack.quick -= (byte)pkts;
                }
            }
        }

        static int tcp_skb_mss(tcp_sock tp)
        {
            return (int)tcp_current_mss(tp);
        }

        static void tcp_add_tx_delay(sk_buff skb, tcp_sock tp)
        {
            if (tcp_tx_delay_enabled)
            {
                skb.skb_mstamp += tp.tcp_tx_delay;
            }
        }

        static void tcp_rtx_queue_unlink_and_free(sk_buff skb, tcp_sock tp)
        {
            list_del(skb.tcp_tsorted_anchor);
            tcp_rtx_queue_unlink(skb, tp);
        }

        static void tcp_rtx_queue_unlink(sk_buff skb, tcp_sock tp)
        {
            rb_erase(skb.rbnode, tp.tcp_rtx_queue);
        }

        static long __tcp_set_rto(tcp_sock tp)
        {
            return (tp.srtt_us >> 3) + tp.rttvar_us;
        }

        static long tcp_min_rtt(tcp_sock tp)
        {
            return minmax_get(tp.rtt_min);
        }

        static bool tcp_needs_internal_pacing(tcp_sock tp)
        {
            return tp.sk_pacing_status == (byte)sk_pacing.SK_PACING_NEEDED;
        }

        //tcp_pacing_delay 函数是 Linux 内核中用于计算 TCP 发送数据包的延迟时间的函数。
        //它的主要作用是根据当前的发送速率和数据包大小，计算出发送数据包所需的延迟时间，以确保数据包的发送速率符合设定的 pacing 速率。
        static long tcp_pacing_delay(tcp_sock tp)
        {
            long delay = tp.tcp_wstamp_ns - tp.tcp_clock_cache;
            return delay > 0 ? delay : 0;
        }

        static void tcp_reset_xmit_timer(tcp_sock tp, int what, long when, long max_when)
        {
            long pacing_delay = tcp_pacing_delay(tp);
            inet_csk_reset_xmit_timer(tp, what, when + pacing_delay, max_when);
        }

        static sk_buff tcp_send_head(tcp_sock tp)
        {
            return skb_peek(tp.sk_write_queue);
        }

        static long tcp_rto_delta_us(tcp_sock tp)
        {
            sk_buff skb = tcp_rtx_queue_head(tp);
            uint rto = (uint)tp.icsk_rto;
            if (skb != null)
            {
                long rto_time_stamp_us = tcp_skb_timestamp_us(skb) + rto;
                return rto_time_stamp_us - tp.tcp_mstamp;
            }
            else
            {
                return rto;
            }
        }

        static long tcp_probe0_base(tcp_sock tp)
        {
            return Math.Max(tp.icsk_rto, TCP_RTO_MIN);
        }

        static long tcp_probe0_when(tcp_sock tp, long max_when)
        {
            byte backoff = (byte)Math.Min(ilog2(TCP_RTO_MAX / TCP_RTO_MIN) + 1, tp.icsk_backoff);
            long when = tcp_probe0_base(tp) << backoff;
            return Math.Min(when, max_when);
        }


        static long keepalive_time_when(tcp_sock tp)
        {
            net net = sock_net(tp);
            long val = tp.keepalive_time;
            return val > 0 ? val : net.ipv4.sysctl_tcp_keepalive_time;
        }

        static long keepalive_time_elapsed(tcp_sock tp)
        {
            return Math.Min(tcp_jiffies32 - tp.icsk_ack.lrcvtime, tcp_jiffies32 - tp.rcv_tstamp);
        }

        static int keepalive_probes(tcp_sock tp)
        {
            net net = sock_net(tp);
            int val = tp.keepalive_probes;
            return val > 0 ? val : net.ipv4.sysctl_tcp_keepalive_probes;
        }

        static long keepalive_intvl_when(tcp_sock tp)
        {
            net net = sock_net(tp);
            long val = tp.keepalive_intvl;
            return val > 0 ? val : net.ipv4.sysctl_tcp_keepalive_intvl;
        }

        static void tcp_insert_write_queue_before(sk_buff newBuff, sk_buff skb, tcp_sock tp)
        {
            __skb_queue_before(tp.sk_write_queue, skb, newBuff);
        }

        static void tcp_unlink_write_queue(sk_buff skb, tcp_sock tp)
        {
            __skb_unlink(skb, tp.sk_write_queue);
        }

        static bool tcp_skb_is_last(tcp_sock tp, sk_buff skb)
        {
            return skb_queue_is_last(tp.sk_write_queue, skb);
        }

        static sk_buff tcp_write_queue_tail(tcp_sock tp)
        {
            return skb_peek_tail(tp.sk_write_queue);
        }

        static bool tcp_in_slow_start(tcp_sock tp)
        {
            return tcp_snd_cwnd(tp) < tp.snd_ssthresh;
        }

        static bool tcp_is_cwnd_limited(tcp_sock tp)
        {
            if (tp.is_cwnd_limited)
            {
                return true;
            }
            if (tcp_in_slow_start(tp))
            {
                return tcp_snd_cwnd(tp) < 2 * tp.max_packets_out;
            }
            return false;
        }

        static long tcp_rto_min(tcp_sock tp)
        {
            dst_entry dst = __sk_dst_get(tp);
            long rto_min = tp.icsk_rto_min;

            if (dst != null && dst_metric(dst, RTAX_RTO_MIN) > 0)
            {
                rto_min = (long)dst_metric(dst, RTAX_RTO_MIN);
            }
            return rto_min;
        }

        static long tcp_rto_min_us(tcp_sock tp)
        {
            return tcp_rto_min(tp);
        }

        static sk_buff tcp_stream_alloc_skb(tcp_sock tp)
        {
            sk_buff skb = tp.mClientPeer.GetObjectPoolManager().Skb_Pop();
            skb.nBufferOffset = max_tcphdr_length;
            skb.nBufferLength = 0;
            INIT_LIST_HEAD(skb.tcp_tsorted_anchor);
            return skb;
        }

        static uint tcp_max_tso_deferred_mss(tcp_sock tp)
        {
            return 3;
        }

        static bool tcp_skb_sent_after(long t1, long t2, uint seq1, uint seq2)
        {
            return t1 > t2 || (t1 == t2 && after(seq1, seq2));
        }

        static int tcp_bound_to_half_wnd(tcp_sock tp, int pktsize)
        {
            int cutoff;
            if (tp.max_window > TCP_MSS_DEFAULT)
            {
                cutoff = ((int)tp.max_window >> 1);
            }
            else
            {
                cutoff = (int)tp.max_window;
            }

            if (cutoff > 0 && pktsize > cutoff)
            {
                return (int)Math.Max(cutoff, 68U);
            }
            else
            {
                return pktsize;
            }
        }

        static int tcp_send_mss(tcp_sock tp)
        {
            int mss_now = (int)tcp_current_mss(tp);
            return mss_now;
        }

        static void tcp_add_write_queue_tail(tcp_sock tp, sk_buff skb)
        {
            __skb_queue_tail(tp.sk_write_queue, skb);
            if (tp.sk_write_queue.next == skb)
            {
                tcp_chrono_start(tp, tcp_chrono.TCP_CHRONO_BUSY);
            }
        }

        //用于决定TCP连接是否应该在经历了一段空闲期之后重新进入慢启动状态。
        static void tcp_slow_start_after_idle_check(tcp_sock tp)
        {
            tcp_congestion_ops ca_ops = tp.icsk_ca_ops;
            if (!sock_net(tp).ipv4.sysctl_tcp_slow_start_after_idle || tp.packets_out > 0 || ca_ops.cong_control != null)
            {
                return;
            }

            long delta = tcp_jiffies32 - tp.lsndtime;
            if (delta > tp.icsk_rto)
            {
                tcp_cwnd_restart(tp, delta);
            }
        }

        static void tcp_skb_entail(tcp_sock tp, sk_buff skb)
        {
            tcp_skb_cb tcb = TCP_SKB_CB(skb);
            tcb.seq = tcb.end_seq = tp.write_seq;
            tcb.tcp_flags = TCPHDR_ACK; //表示这是一个包含 ACK 的报文。
            tcp_add_write_queue_tail(tp, skb);

            //如果 nonagle 标志中包含 TCP_NAGLE_PUSH，则清除该标志。
            //这表示已经强制推送到发射队列里了
            if (BoolOk(tp.nonagle & TCP_NAGLE_PUSH))
            {
                tp.nonagle = (byte)(tp.nonagle & (~TCP_NAGLE_PUSH));
            }
            tcp_slow_start_after_idle_check(tp);
        }

        static bool forced_push(tcp_sock tp)
        {
            return after(tp.write_seq, tp.pushed_seq + (tp.max_window >> 1));
        }

        static void tcp_mark_push(tcp_sock tp, sk_buff skb)
        {
            TCP_SKB_CB(skb).tcp_flags |= TCPHDR_PSH;
            tp.pushed_seq = tp.write_seq;
        }

        static void tcp_check_probe_timer(tcp_sock tp)
        {
            if (tp.packets_out == 0 && tp.icsk_pending == 0)
            {
                tcp_reset_xmit_timer(tp, ICSK_TIME_PROBE0, tcp_probe0_base(tp), TCP_RTO_MAX);
            }
        }

        static void tcp_remove_empty_skb(tcp_sock tp)
        {
            sk_buff skb = tcp_write_queue_tail(tp);
            if (skb != null && TCP_SKB_CB(skb).seq == TCP_SKB_CB(skb).end_seq)
            {
                tcp_unlink_write_queue(skb, tp);
                if (tcp_write_queue_empty(tp))
                {
                    tcp_chrono_stop(tp, tcp_chrono.TCP_CHRONO_BUSY);
                }
            }
        }

        //判断是否应该 开启 自动软木塞 功能
        static bool tcp_should_autocork(tcp_sock tp, sk_buff skb, int size_goal)
        {
            return skb.nBufferLength < size_goal && sock_net(tp).ipv4.sysctl_tcp_autocorking > 0 &&
               !tcp_rtx_queue_empty(tp) &&
               tcp_skb_can_collapse_to(skb);
        }

        //tcp_push 函数是 Linux 内核中用于推动 TCP 发送队列中待发送数据包的函数。
        //它的主要作用是根据当前的发送条件，决定是否将数据包发送出去，并设置相应的 TCP 标志位。
        //以下是 tcp_push 函数的详细解释和相关代码。
        static void tcp_push(tcp_sock tp, int flags, int mss_now, int nonagle, int size_goal)
        {
            sk_buff skb = tcp_write_queue_tail(tp);
            if (skb == null)
            {
                return;
            }

            if (forced_push(tp))
            {
                tcp_mark_push(tp, skb);
            }

            if (tcp_should_autocork(tp, skb, size_goal))
            {
                tp.sk_tsq_flags |= 1 << (byte)tsq_enum.TSQ_THROTTLED;
            }

            if (BoolOk(flags & MSG_MORE))
            {
                nonagle = TCP_NAGLE_CORK;
            }

            __tcp_push_pending_frames(tp, (uint)mss_now, nonagle);
        }

        static void tcp_tx_timestamp(tcp_sock tp, sockcm_cookie sockc)
        {
            sk_buff skb = tcp_write_queue_tail(tp);
            uint tsflags = sockc.tsflags;
            if (tsflags > 0 && skb != null)
            {
                tcp_skb_cb tcb = TCP_SKB_CB(skb);
                sock_tx_timestamp(tp, sockc, out skb.tx_flags);
                if (BoolOk(tsflags & SOF_TIMESTAMPING_TX_ACK))
                {
                    tcb.txstamp_ack = 1;
                }

                if (BoolOk(tsflags & SOF_TIMESTAMPING_TX_RECORD_MASK))
                {
                    skb.tskey = (uint)(TCP_SKB_CB(skb).seq + skb.nBufferLength - 1);
                }
            }
        }

        public static void tcp_sendmsg(tcp_sock tp, ReadOnlySpan<byte> msg)
        {
            sk_buff skb = null;
            int flags = 0;
            int copied = 0;
            int mss_now = 0;

            tcp_rate_check_app_limited(tp);
            sockcm_cookie sockc = sockcm_init(tp);
            mss_now = tcp_send_mss(tp);

            while (msg.Length > 0)
            {
                skb = tcp_stream_alloc_skb(tp);
                tcp_skb_entail(tp, skb);
                
                int copy = mss_now;
                if (copy > msg.Length)
                {
                    copy = msg.Length;
                }

                //在这里负责Copy数据
                msg.Slice(0, copy).CopyTo(skb.mBuffer.AsSpan().Slice(skb.nBufferOffset));
                msg = msg.Slice(copy);
                skb_len_add(skb, copy);//这里把 包体长度加进来

                if (copied == 0)
                {
                    TCP_SKB_CB(skb).tcp_flags = (byte)(TCP_SKB_CB(skb).tcp_flags & ~TCPHDR_PSH);
                }

                tp.write_seq += (uint)copy;
                TCP_SKB_CB(skb).end_seq += (uint)copy;
                copied += copy;

                if (msg.Length == 0)
                {
                    if (copied > 0)
                    {
                        tcp_tx_timestamp(tp, sockc);
                        tcp_push(tp, flags, mss_now, tp.nonagle, mss_now);
                    }
                }
                else if (forced_push(tp)) //当发送很多数据的时候，就没必要再等了，直接发射
                {
                    tcp_mark_push(tp, skb);
                    __tcp_push_pending_frames(tp, (uint)mss_now, TCP_NAGLE_PUSH);
                }
                else if (skb == tcp_send_head(tp))
                {
                    tcp_push_one(tp, (uint)mss_now);
                }
            }
        }

        static void tcp_cleanup_rbuf(tcp_sock tp, int copied)
        {
            bool time_to_ack = false;

            if (inet_csk_ack_scheduled(tp))
            {
                if (tp.rcv_nxt - tp.rcv_wup > tp.icsk_ack.rcv_mss ||
                    (copied > 0 &&
                     (BoolOk(tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_PUSHED2) ||
                      (BoolOk(tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_PUSHED) &&
                       !inet_csk_in_pingpong_mode(tp))) &&
                      tp.sk_rmem_alloc == 0))
                {
                    time_to_ack = true;
                }
            }

            if (copied > 0 && !time_to_ack)
            {
                uint rcv_window_now = tcp_receive_window(tp);
                if (2 * rcv_window_now <= tp.window_clamp)
                {
                    uint new_window = __tcp_select_window(tp);
                    if (new_window > 0 && new_window >= 2 * rcv_window_now)
                    {
                        time_to_ack = true;
                    }
                }
            }

            if (time_to_ack)
            {
                tcp_send_ack(tp);
            }
        }

        static void tcp_update_recv_tstamps(sk_buff skb, scm_timestamping_internal tss)
        {
            if (skb.tstamp > 0)
            {
                tss.ts[0] = skb.tstamp;
            }
            else
            {
                tss.ts[0] = 0;
            }

            tss.ts[2] = 0;
        }

        static void tcp_eat_recv_skb(tcp_sock tp, sk_buff skb)
        {
            __skb_unlink(skb, tp.sk_receive_queue);
            tp.mClientPeer.GetObjectPoolManager().Skb_Recycle(skb);
        }

        static bool tcp_recvmsg_locked(tcp_sock tp, msghdr msg, scm_timestamping_internal tss)
        {
            int len = msg.MaxLength;
            int copied = 0;
            msg.nLength = 0;
            sk_buff skb = null;
            do
            {
                for (skb = tp.sk_receive_queue.next; skb != tp.sk_receive_queue; skb = skb.next)
                {
                    goto found_ok_skb;
                }

                tcp_cleanup_rbuf(tp, copied);
                return false;

            found_ok_skb:
                int nSKb_Data_Length = (int)(TCP_SKB_CB(skb).end_seq - TCP_SKB_CB(skb).seq);

                int copyLength = (int)(TCP_SKB_CB(skb).end_seq - tp.copied_seq);
                if (copyLength > len)
                {
                    copyLength = len;
                }

                int nOffset = (int)(tp.copied_seq - TCP_SKB_CB(skb).seq);
                NetLog.Assert(copyLength > 0, copyLength);
                NetLog.Assert(nOffset >= 0, $"{tp.copied_seq}, {TCP_SKB_CB(skb).seq}, {TCP_SKB_CB(skb).end_seq}");

                var mTcpBodyBuffer = skb.GetTcpReceiveBufferSpan().Slice(nOffset, copyLength);
                mTcpBodyBuffer.CopyTo(msg.mBuffer.AsSpan().Slice(copied));
                msg.nLength += copyLength;

                //NetLog.Log($"{tp.copied_seq}, {TCP_SKB_CB(skb).seq}, {TCP_SKB_CB(skb).end_seq}, {copyLength}");

                tp.copied_seq += (uint)copyLength;
                copied += copyLength;
                len -= copyLength;

                if (TCP_SKB_CB(skb).has_rxtstamp)
                {
                    tcp_update_recv_tstamps(skb, tss);
                }

                if (copyLength + nOffset == nSKb_Data_Length)
                {
                    tcp_eat_recv_skb(tp, skb); //SKB全部拷贝完成后，删除这个SKB
                }
            } while (len > 0);

            tcp_cleanup_rbuf(tp, copied);
            return true;
        }

        public static bool tcp_recvmsg(tcp_sock tp, msghdr msg)
        {
            bool bOk = false;
            scm_timestamping_internal tss = new scm_timestamping_internal();
            bOk = tcp_recvmsg_locked(tp, msg, tss);
            return bOk;
        }
        
        static void tcp_set_state(tcp_sock tp, byte state)
        {
            int oldstate = tp.sk_state;
            if (state != oldstate)
            {

            }
            tp.sk_state = state;
        }

        static void tcp_init_wl(tcp_sock tp, uint seq)
        {
            tp.snd_wl1 = seq;
        }

        static void __tcp_fast_path_on(tcp_sock tp, uint snd_wnd)
        {
            tp.pred_flags = TCP_FLAG_ACK | snd_wnd;
        }

        //用于检查当前 TCP 连接是否可以进入“快速路径”（Fast Path）。
        //快速路径是一种优化机制，用于处理那些不需要复杂处理的 TCP 数据包，从而提高性能。
        static void tcp_fast_path_on(tcp_sock tp)
        {
            __tcp_fast_path_on(tp, tp.snd_wnd >> tp.rx_opt.snd_wscale);
        }

        static void tcp_clear_xmit_timers(tcp_sock tp)
        {
            tp.pacing_timer.Stop();
            tp.compressed_ack_timer.Stop();
            inet_csk_clear_xmit_timers(tp);
        }

        static bool tcp_skb_can_collapse_rx(sk_buff to, sk_buff from)
        {
            return false;
        }

        static void tcp_fast_path_check(tcp_sock tp)
        {
            if (RB_EMPTY_ROOT(tp.out_of_order_queue) && tp.rcv_wnd > 0)
            {
                tcp_fast_path_on(tp);
            }
        }

        static bool tcp_rmem_pressure(tcp_sock tp)
        {
            int rcvbuf, threshold;
            if (tcp_under_memory_pressure(tp))
            {
                return true;
            }

            rcvbuf = tp.sk_rcvbuf;
            threshold = rcvbuf - (rcvbuf >> 3);
            return tp.sk_rmem_alloc > threshold;
        }

        static void tcp_push_pending_frames(tcp_sock tp)
        {
            if (tcp_send_head(tp) != null)
            {
                __tcp_push_pending_frames(tp, tcp_current_mss(tp), tp.nonagle);
            }
        }

        static void tcp_update_wl(tcp_sock tp, uint seq)
        {
            tp.snd_wl1 = seq;
        }

        static void tcp_highest_sack_reset(tcp_sock tp)
        {
            tp.highest_sack = tcp_rtx_queue_head(tp);
        }

        static sk_buff tcp_highest_sack(tcp_sock tp)
        {
            return tp.highest_sack;
        }

        static void tcp_advance_highest_sack(tcp_sock tp, sk_buff skb)
        {
            tp.highest_sack = skb_rb_next(skb);
        }

        static byte tcp_flag_byte(sk_buff skb)
        {
            var mData = skb_transport_header(skb);
            return mData[13];
        }

        static uint tcp_flag_word(tcphdr tp)
        {
            return (uint)(tp.tcp_flags << 16);
        }

        static void tcp_sack_reset(tcp_options_received rx_opt)
        {
            rx_opt.dsack = 0;
            rx_opt.num_sacks = 0;
        }

        static int tcp_hdrlen(sk_buff skb)
        {
            return tcp_hdr(skb).doff;
        }

        static void tcp_scaling_ratio_init(tcp_sock tp)
        {
            tp.scaling_ratio = TCP_DEFAULT_SCALING_RATIO;
        }

        static void tcp_init()
        {
            NetLog.Assert(TCP_MIN_SND_MSS > MAX_TCP_OPTION_SPACE);

            init_net.ipv4.sysctl_tcp_wmem[0] = 8 * 1024;
            init_net.ipv4.sysctl_tcp_wmem[1] = 16 * 1024;
            init_net.ipv4.sysctl_tcp_wmem[2] = 64 * 1024;

            init_net.ipv4.sysctl_tcp_rmem[0] = 8 * 1024;
            init_net.ipv4.sysctl_tcp_rmem[1] = 131072;
            init_net.ipv4.sysctl_tcp_rmem[2] = 131072;

            tcp_v4_init();
            tcp_metrics_init();
            BUG_ON(tcp_register_congestion_control(tcp_reno) != 0);
        }

        static void tcp_init_sock(tcp_sock tp)
        {
            tcp_init_xmit_timers(tp);

            INIT_LIST_HEAD(tp.tsorted_sent_queue);

            tp.icsk_rto = TCP_TIMEOUT_INIT;
            tp.icsk_rto_min = sock_net(tp).ipv4.sysctl_tcp_rto_min_us;
            tp.icsk_delack_max = TCP_DELACK_MAX;
            tp.mdev_us = TCP_TIMEOUT_INIT;
            minmax_reset(tp.rtt_min, tcp_jiffies32, ~0U);

            tcp_snd_cwnd_set(tp, TCP_INIT_CWND);

            tp.app_limited = uint.MaxValue;
            tp.rate_app_limited = true;
            tp.snd_ssthresh = TCP_INFINITE_SSTHRESH;
            tp.snd_cwnd_clamp = uint.MaxValue;
            tp.mss_cache = TCP_MSS_DEFAULT;

            tp.reordering = (uint)sock_net(tp).ipv4.sysctl_tcp_reordering;
            tcp_assign_congestion_control(tp);

            tp.tsoffset = 0;
            tp.rack.reo_wnd_steps = 1;
            sock_set_flag(tp, sock_flags.SOCK_USE_WRITE_QUEUE);
            tcp_scaling_ratio_init(tp);
        }

        public static void tcp_connect_init(tcp_sock tp)
        {
            dst_entry dst = __sk_dst_get(tp);
            byte rcv_wscale = 0;

            tp.advmss = dst_metric_advmss(dst);
            tp.max_window = 0;
            if (tp.window_clamp == 0)
            {
                tp.window_clamp = (uint)dst_metric(dst, RTAX_WINDOW);
            }

            if (tp.window_clamp > tcp_full_space(tp) || tp.window_clamp == 0)
            {
                tp.window_clamp = (uint)tcp_full_space(tp);
            }

            NetLog.Assert(tcp_full_space(tp) > 0, "tcp_full_space: 0");
            uint rcv_wnd = (uint)dst_metric(dst, RTAX_INITRWND);
            tcp_select_initial_window(tp, (int)tcp_full_space(tp),
                      tp.advmss, sock_net(tp).ipv4.sysctl_tcp_window_scaling, rcv_wnd,
                      ref tp.rcv_wnd,
                      ref tp.window_clamp,
                      ref rcv_wscale);

            tp.rx_opt.rcv_wscale = rcv_wscale;
            tp.rcv_ssthresh = tp.rcv_wnd;
            sock_set_flag(tp, sock_flags.SOCK_DONE);

            tp.snd_wnd = 0;
            tcp_init_wl(tp, 0);
            tcp_write_queue_purge(tp);

            if (tp.write_seq == 0)
            {
                tp.write_seq = (uint)RandomTool.Random(0, int.MaxValue - 1);
                tp.tsoffset = 0;
            }

            tp.snd_una = tp.write_seq;
            tp.snd_sml = tp.write_seq;
            tp.snd_up = tp.write_seq;
            tp.snd_nxt = tp.write_seq;

            tp.rcv_nxt = 0;
            tp.rcv_wup = tp.rcv_nxt;
            tp.copied_seq = tp.rcv_nxt;

            tp.icsk_rto = tcp_timeout_init(tp);
            tp.icsk_retransmits = 0;
            tcp_clear_retrans(tp);

            NetLog.Log($"tcp_connect_init: mss_cache={tp.mss_cache}, write_seq={tp.write_seq}, rcv_nxt={tp.rcv_nxt}, " +
    $"tp.snd_wnd={tp.snd_wnd} tp.rcv_wnd={tp.rcv_wnd} tp.scaling_ratio={tp.scaling_ratio}");
        }

        public static void tcp_connect_finish_init(tcp_sock tp, sk_buff skb)
        {
            dst_entry dst = __sk_dst_get(tp);

            var th = tcp_hdr(skb);
            tcp_v4_fill_cb(skb, th);

            tp.rx_opt.saw_tstamp = false;
            tcp_mstamp_refresh(tp);
            tcp_parse_options(sock_net(tp), skb, tp.rx_opt, false);

            if (tp.rx_opt.saw_tstamp)
            {
                tp.rx_opt.tstamp_ok = 1;
                tcp_store_ts_recent(tp);
            }

            tp.rcv_nxt = TCP_SKB_CB(skb).seq;
            tp.copied_seq = tp.rcv_nxt;
            tp.rcv_wup = TCP_SKB_CB(skb).seq;
            tp.snd_wnd = th.window;
            tp.snd_wl1 = TCP_SKB_CB(skb).seq;
            tp.max_window = tp.snd_wnd;

            tcp_ecn_rcv_syn(tp, th);

            tcp_mtup_init(tp);
            tcp_sync_mss(tp, ipv4_mtu(dst));
            tcp_initialize_rcv_mss(tp);
            tcp_connect_finish_init2(tp, skb);
        }

        //tcp_rcv_synsent_state_process
        //tcp_connect
        //tcp_v4_connect
        static void tcp_connect_finish_init2(tcp_sock tp, sk_buff skb)
        {
            var th = tcp_hdr(skb);
            if (tp.rx_opt.saw_tstamp && tp.rx_opt.rcv_tsecr > 0)
            {
                tp.rx_opt.rcv_tsecr -= tp.tsoffset;
            }

            tcp_ecn_rcv_synack(tp, th);
            tcp_init_wl(tp, TCP_SKB_CB(skb).seq);
            tcp_try_undo_spurious_syn(tp);

            tp.rcv_nxt = TCP_SKB_CB(skb).seq;
            tp.rcv_wup = TCP_SKB_CB(skb).seq;
            tp.snd_wnd = th.window;
            if (tp.rx_opt.wscale_ok == 0)
            {
                tp.rx_opt.snd_wscale = tp.rx_opt.rcv_wscale = 0;
                tp.window_clamp = Math.Min(tp.window_clamp, 65535U);
            }

            if (tp.rx_opt.saw_tstamp)
            {
                tp.rx_opt.tstamp_ok = 1;
                tcp_store_ts_recent(tp);
            }

            tcp_sync_mss(tp, tp.icsk_pmtu_cookie);
            tcp_initialize_rcv_mss(tp);
            tp.copied_seq = tp.rcv_nxt;

            tcp_finish_connect(tp, skb);
        }

        static void tcp_finish_connect(tcp_sock tp, sk_buff skb)
        {
            tcp_set_state(tp, TCP_ESTABLISHED);
            tp.icsk_ack.lrcvtime = tcp_jiffies32;

            tcp_init_transfer(tp);
            tp.lsndtime = tcp_jiffies32;

            if (sock_flag(tp, sock_flags.SOCK_KEEPOPEN))
            {
                inet_csk_reset_keepalive_timer(tp, keepalive_time_when(tp));
            }

            if (tp.rx_opt.snd_wscale == 0)
            {
                __tcp_fast_path_on(tp, tp.snd_wnd);
            }
            else
            {
                tp.pred_flags = 0;
            }

            NetLog.Log($"tcp_finish_connect: mss_cache={tp.mss_cache}, write_seq={tp.write_seq}, rcv_nxt={tp.rcv_nxt}, " +
                $"tp.snd_wnd={tp.snd_wnd} tp.rcv_wnd={tp.rcv_wnd} tp.scaling_ratio={tp.scaling_ratio}");
        }
    }

}
