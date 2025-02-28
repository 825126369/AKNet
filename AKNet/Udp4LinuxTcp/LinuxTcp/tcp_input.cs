/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        public static void tcp_sack_compress_send_ack(tcp_sock tp)
        {
            if (tp.compressed_ack == 0)
            {
                return;
            }

            tp.compressed_ack_timer.Stop();
            tp.compressed_ack = 0;
            tcp_send_ack(tp);
        }

        public static void tcp_enter_loss(tcp_sock tp)
        {
            net net = sock_net(tp);
            bool new_recovery = tp.icsk_ca_state < (byte)tcp_ca_state.TCP_CA_Recovery;

            tcp_timeout_mark_lost(tp);

            if (tp.icsk_ca_state <= (byte)tcp_ca_state.TCP_CA_Disorder || !after(tp.high_seq, tp.snd_una) ||
                (tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Loss && tp.icsk_retransmits == 0))
            {
                tp.prior_ssthresh = tcp_current_ssthresh(tp);
                tp.prior_cwnd = tp.snd_cwnd;
                tp.snd_ssthresh = tp.icsk_ca_ops.ssthresh(tp);
                tcp_ca_event_func(tp, tcp_ca_event.CA_EVENT_LOSS);
                tcp_init_undo(tp);
            }

            tcp_snd_cwnd_set(tp, tcp_packets_in_flight(tp) + 1);
            tp.snd_cwnd_cnt = 0;
            tp.snd_cwnd_stamp = tcp_jiffies32;

            int reordering = net.ipv4.sysctl_tcp_reordering;
            if (tp.icsk_ca_state <= (int)tcp_ca_state.TCP_CA_Disorder && tp.sacked_out >= reordering)
            {
                tp.reordering = (uint)Math.Min(tp.reordering, reordering);
            }
            tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_Loss);
            tp.high_seq = tp.snd_nxt;
            tp.tlp_high_seq = 0;
            tcp_ecn_queue_cwr(tp);
            
            //这段代码的作用是决定是否启用 F-RTO 功能。它通过检查以下条件来决定：
            //内核是否启用了 F-RTO 功能（sysctl_tcp_frto）。
            //当前是否处于新的恢复阶段或者已经发生了重传。
            //当前是否没有正在进行的 MTU 探测。
            //如果所有条件都满足，则启用 F-RTO 功能（tp->frto = 1）；否则，不启用 F-RTO 功能（tp->frto = 0）。
            tp.frto = net.ipv4.sysctl_tcp_frto > 0 && 
                (new_recovery || tp.icsk_retransmits > 0) && 
                tp.icsk_mtup.probe_size == 0;
        }

        public static void tcp_timeout_mark_lost(tcp_sock tp)
        {
            sk_buff head = tcp_rtx_queue_head(tp);
            bool is_reneg = head != null && BoolOk(TCP_SKB_CB(head).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED);
            if (is_reneg)
            {
                tp.sacked_out = 0;
                tp.is_sack_reneg = true;
            }
            else if (tcp_is_reno(tp))
            {
                tcp_reset_reno_sack(tp);
            }

            sk_buff skb = head;
            for (; skb != null; skb = skb_rb_next(skb))
            {
                if (is_reneg)
                {
                    TCP_SKB_CB(skb).sacked = (byte)(TCP_SKB_CB(skb).sacked & ~(byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED);
                }
                else if (tcp_is_rack(tp) && skb != head && tcp_rack_skb_timeout(tp, skb, 0) > 0)
                {
                    continue;
                }
                tcp_mark_skb_lost(tp, skb);
            }
            
            tcp_clear_all_retrans_hints(tp);
        }

        public static void tcp_init_undo(tcp_sock tp)
        {
            tp.undo_marker = tp.snd_una;
            tp.undo_retrans = (int)tp.retrans_out;

            if (tp.tlp_high_seq > 0 && tp.tlp_retrans > 0)
            {
                tp.undo_retrans++;
            }

            if (tp.undo_retrans == 0)
            {
                tp.undo_retrans = -1;
            }
        }

        //tcp_ecn_queue_cwr 是 Linux 内核 TCP 协议栈中与显式拥塞通知（Explicit Congestion Notification, ECN）机制相关的一个函数。
        //ECN 是一种改进的拥塞控制机制，它允许路由器在发生拥塞之前就通知发送方和接收方网络状况，从而使得它们可以提前采取措施来避免数据包丢失。
        public static void tcp_ecn_queue_cwr(tcp_sock tp)
        {
            if (BoolOk(tp.ecn_flags & TCP_ECN_OK))
            {
                tp.ecn_flags |= TCP_ECN_QUEUE_CWR;
            }
        }

        public static void tcp_reset_reno_sack(tcp_sock tp)
        {
            tp.sacked_out = 0;
        }

        public static void tcp_mark_skb_lost(tcp_sock tp, sk_buff skb)
        {
            byte sacked = TCP_SKB_CB(skb).sacked;

            if ((sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED) > 0)
            {
                return;
            }

            tcp_verify_retransmit_hint(tp, skb);
            if ((sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST) > 0)
            {
                if ((sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS) > 0)
                {
                    TCP_SKB_CB(skb).sacked = (byte)(TCP_SKB_CB(skb).sacked & ~(byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS);
                    tp.retrans_out--;
                    tcp_notify_skb_loss_event(tp, skb);
                }
            }
            else
            {
                tp.lost_out++;
                TCP_SKB_CB(skb).sacked |= (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST;
                tcp_notify_skb_loss_event(tp, skb);
            }
        }

        public static void tcp_verify_retransmit_hint(tcp_sock tp, sk_buff skb)
        {
            if ((tp.retransmit_skb_hint == null && tp.retrans_out >= tp.lost_out) ||
                (tp.retransmit_skb_hint != null && before(TCP_SKB_CB(skb).seq, TCP_SKB_CB(tp.retransmit_skb_hint).seq))
               )
            {
                tp.retransmit_skb_hint = skb;
            }
        }

        public static void tcp_notify_skb_loss_event(tcp_sock tp, sk_buff skb)
        {
            tp.lost++;
        }

        public static int tcp_skb_shift(sk_buff to, sk_buff from, int pcount, int shiftlen)
        {
            if (to.nBufferLength + shiftlen >= 65535 * TCP_MIN_GSO_SIZE)
            {
                return 0;
            }

            if (pcount > ushort.MaxValue)
            {
                return 0;
            }
            return skb_shift(to, from, shiftlen);
        }

        public static void tcp_enter_cwr(tcp_sock tp)
        {
            tp.prior_ssthresh = 0;
            if (tp.icsk_ca_state < (byte)tcp_ca_state.TCP_CA_CWR)
            {
                tp.undo_marker = 0;
                tcp_init_cwnd_reduction(tp);
                tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_CWR);
            }
        }

        static void tcp_init_cwnd_reduction(tcp_sock tp)
        {
            tp.high_seq = tp.snd_nxt;
            tp.tlp_high_seq = 0;
            tp.snd_cwnd_cnt = 0;
            tp.prior_cwnd = tcp_snd_cwnd(tp);
            tp.prr_delivered = 0;
            tp.prr_out = 0;
            tp.snd_ssthresh = tp.icsk_ca_ops.ssthresh(tp);
            tcp_ecn_queue_cwr(tp);
        }

        static bool tcp_any_retrans_done(tcp_sock tp)
        {
            sk_buff skb;
            if (tp.retrans_out > 0)
            {
                return true;
            }

            skb = tcp_rtx_queue_head(tp);
            if (skb != null && BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_EVER_RETRANS))
            {
                return true;
            }
            return false;
        }

        static void tcp_retrans_stamp_cleanup(tcp_sock tp)
        {
            if (!tcp_any_retrans_done(tp))
            {
                tp.retrans_stamp = 0;
            }
        }

        static void tcp_enter_recovery(tcp_sock tp, bool ece_ack)
        {
            tcp_retrans_stamp_cleanup(tp);
            tp.prior_ssthresh = 0;
            tcp_init_undo(tp);

            if (!tcp_in_cwnd_reduction(tp))
            {
                if (!ece_ack)
                {
                    tp.prior_ssthresh = tcp_current_ssthresh(tp);
                }
                tcp_init_cwnd_reduction(tp);
            }
            tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_Recovery);
        }

        static void tcp_cwnd_reduction(tcp_sock tp, int newly_acked_sacked, int newly_lost, int flag)
        {
            int sndcnt = 0;
            int delta = (int)(tp.snd_ssthresh - tcp_packets_in_flight(tp));

            if (newly_acked_sacked <= 0 || tp.prior_cwnd == 0)
            {
                return;
            }

            tp.prr_delivered += (uint)newly_acked_sacked;
            if (delta < 0)
            {
                ulong dividend = (ulong)tp.snd_ssthresh * tp.prr_delivered + tp.prior_cwnd - 1;
                sndcnt = (int)(dividend / tp.prior_cwnd - tp.prr_out);
            }
            else
            {
                sndcnt = (int)Math.Max(tp.prr_delivered - tp.prr_out, newly_acked_sacked);
                if (BoolOk(flag & FLAG_SND_UNA_ADVANCED) && newly_lost == 0)
                {
                    sndcnt++;
                }
                sndcnt = Math.Min(delta, sndcnt);
            }

            sndcnt = Math.Max(sndcnt, (tp.prr_out > 0 ? 0 : 1));
            tcp_snd_cwnd_set(tp, (uint)(tcp_packets_in_flight(tp) + sndcnt));
        }

        //用于重新设置 TCP 连接的重传超时时间（RTO，Retransmission Timeout）。
        //这个函数在 TCP 协议栈中起着关键作用，确保在数据包丢失或网络延迟的情况下，能够及时重传数据包。
        static void tcp_rearm_rto(tcp_sock tp)
        {
            if (tp.packets_out == 0)
            {
                inet_csk_clear_xmit_timer(tp, ICSK_TIME_RETRANS);
            }
            else
            {
                uint rto = (uint)tp.icsk_rto;
                if (tp.icsk_pending == ICSK_TIME_REO_TIMEOUT || tp.icsk_pending == ICSK_TIME_LOSS_PROBE)
                {
                    long delta_us = tcp_rto_delta_us(tp);
                    rto = (uint)Math.Max(delta_us, 1);
                }
                tcp_reset_xmit_timer(tp, ICSK_TIME_RETRANS, rto, TCP_RTO_MAX);
            }
        }

        static uint tcp_init_cwnd(tcp_sock tp)
        {
            uint cwnd = TCP_INIT_CWND;
            return Math.Min(cwnd, tp.snd_cwnd_clamp);
        }

        static void tcp_rbtree_insert(rb_root root, sk_buff skb)
        {
            if (root.rb_node == null)
            {
                root.rb_node = rb_link_node(skb.rbnode);
                rb_insert_color(skb.rbnode, root);
            }
            else
            {
                rb_node p = root.rb_node;
                while (true)
                {
                    rb_node parent = p;
                    sk_buff skb1 = rb_to_skb(parent);
                    if (before(TCP_SKB_CB(skb).seq, TCP_SKB_CB(skb1).seq))
                    {
                        p = parent.rb_left;
                        if (p == null)
                        {
                            rb_link_node(skb.rbnode, parent, true);
                            rb_insert_color(skb.rbnode, root);
                            break;
                        }
                    }
                    else
                    {
                        p = parent.rb_right;
                        if (p == null)
                        {
                            rb_link_node(skb.rbnode, parent, false);
                            rb_insert_color(skb.rbnode, root);
                            break;
                        }
                    }
                }
            }

            //print_draw_rb_tree(root);
        }

        static void tcp_rcv_space_adjust(tcp_sock tp)
        {
            tcp_mstamp_refresh(tp);
            long time = tcp_stamp_us_delta(tp.tcp_mstamp, tp.rcvq_space.time);
            if (time < (tp.rcv_rtt_est.rtt_us >> 3) || tp.rcv_rtt_est.rtt_us == 0)
            {
                return;
            }

            uint copied = tp.copied_seq - tp.rcvq_space.seq;
            if (copied <= tp.rcvq_space.space)
            {
                goto new_measure;
            }

            if (sock_net(tp).ipv4.sysctl_tcp_moderate_rcvbuf > 0 && !BoolOk(tp.sk_userlocks & SOCK_RCVBUF_LOCK))
            {
                int rcvwin = ((int)copied << 1) + 16 * (int)tp.advmss;
                int grow = rcvwin * (int)(copied - tp.rcvq_space.space);
                grow /= (int)tp.rcvq_space.space;
                rcvwin += (grow << 1);

                int rcvbuf = Math.Min(tcp_space_from_win(tp, rcvwin), sock_net(tp).ipv4.sysctl_tcp_rmem[2]);
                if (rcvbuf > tp.sk_rcvbuf)
                {
                    tp.sk_rcvbuf = rcvbuf;
                    tp.window_clamp = (uint)tcp_win_from_space(tp, rcvbuf);

                    TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.sk_rcvbuf, (tp.sk_rcvbuf / 1024));
                }
            }
            tp.rcvq_space.space = copied;

        new_measure:
            tp.rcvq_space.seq = tp.copied_seq;
            tp.rcvq_space.time = tp.tcp_mstamp;
        }

        static void tcp_update_rtt_min(tcp_sock tp, long rtt_us, int flag)
        {
            long wlen = (uint)(sock_net(tp).ipv4.sysctl_tcp_min_rtt_wlen * HZ);
            if (BoolOk(flag & FLAG_ACK_MAYBE_DELAYED) && rtt_us > tcp_min_rtt(tp))
            {
                return;
            }
            minmax_running_min(tp.rtt_min, wlen, tcp_jiffies32, rtt_us > 0 ? rtt_us : 1000);
        }

        static long tcp_rtt_tsopt_us(tcp_sock tp)
        {
            long delta = tcp_time_stamp_ms(tp) - tp.rx_opt.rcv_tsecr;
            if (delta < int.MaxValue)
            {
                if (delta == 0)
                {
                    delta = 1;
                }
                return delta;
            }
            return -1;
        }

        static bool tcp_ack_update_rtt(tcp_sock tp, int flag, long seq_rtt_us, long sack_rtt_us, long ca_rtt_us, rate_sample rs)
        {
            TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.seq_rtt_us, seq_rtt_us);
            TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.sack_rtt_us, sack_rtt_us);
            TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.ca_rtt_us, ca_rtt_us);

            if (seq_rtt_us < 0)
            {
                seq_rtt_us = sack_rtt_us;
            }

            if (seq_rtt_us < 0 && tp.rx_opt.saw_tstamp && tp.rx_opt.rcv_tsecr > 0 && BoolOk(flag & FLAG_ACKED))
            {
                seq_rtt_us = ca_rtt_us = tcp_rtt_tsopt_us(tp);
            }

            rs.rtt_us = ca_rtt_us;
            if (seq_rtt_us < 0)
            {
                return false;
            }

            tcp_update_rtt_min(tp, ca_rtt_us, flag);
            tcp_rtt_estimator(tp, seq_rtt_us);
            tcp_set_rto(tp);

            TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.RTO_AVERAGE, tp.icsk_rto);
            TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.RTT_AVERAGE, seq_rtt_us);

            tp.icsk_backoff = 0;
            return true;
        }

        static void tcp_synack_rtt_meas(tcp_sock tp, tcp_request_sock req)
        {
            rate_sample rs = null;
            long rtt_us = -1;
            if (req != null && req.num_retrans == 0 && req.snt_synack > 0)
            {
                rtt_us = tcp_stamp_us_delta(tcp_jiffies32, req.snt_synack);
            }
            tcp_ack_update_rtt(tp, FLAG_SYN_ACKED, rtt_us, -1, rtt_us, rs);
        }

        //然而，在某些情况下，可能会出现所谓的“spurious SYN”——即看起来像是来自某个主机的新连接请求，但实际上可能是由于网络重传、旧的数据包或者恶意活动导致的。
        //为了应对这种情况，TCP 半连接队列（用于存储正在等待完成三次握手的连接请求）和完全建立的连接队列都有一定的容量限制。
        //如果这些队列满了，新的 SYN 请求将被丢弃，并且可能触发 ICMP 源站抑制消息以通知远端减慢发送速度。
        static void tcp_try_undo_spurious_syn(tcp_sock tp)
        {
            long syn_stamp = tp.retrans_stamp;
            if (tp.undo_marker > 0 && syn_stamp > 0 && tp.rx_opt.saw_tstamp && syn_stamp == tp.rx_opt.rcv_tsecr)
            {
                tp.undo_marker = 0;
            }
        }

        //用于动态扩展 TCP 发送缓冲区的大小。这个函数在连接建立后，根据当前的网络条件和拥塞控制窗口，调整发送缓冲区的大小，以优化性能
        static void tcp_sndbuf_expand(tcp_sock tp)
        {
            tcp_congestion_ops ca_ops = tp.icsk_ca_ops;
            int sndmem, per_mss;
            uint nr_segs;
            per_mss = (int)Math.Max(tp.rx_opt.mss_clamp, tp.mss_cache) + max_tcphdr_length;
            per_mss = roundup_pow_of_two(per_mss);

            nr_segs = Math.Max(TCP_INIT_CWND, tcp_snd_cwnd(tp));
            nr_segs = Math.Max(nr_segs, tp.reordering + 1);
            sndmem = ca_ops.sndbuf_expand != null ? (int)ca_ops.sndbuf_expand(tp) : 2;
            sndmem = (int)(sndmem * nr_segs * per_mss);

            if (tp.sk_sndbuf < sndmem)
            {
                tp.sk_sndbuf = Math.Min(sndmem, sock_net(tp).ipv4.sysctl_tcp_wmem[2]);
            }

            TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.sk_sndbuf, tp.sk_sndbuf / 1024);
        }

        static void tcp_init_buffer_space(tcp_sock tp)
        {
            int tcp_app_win = sock_net(tp).ipv4.sysctl_tcp_app_win;

            tcp_sndbuf_expand(tp);
            tcp_mstamp_refresh(tp);
            tp.rcvq_space.time = tp.tcp_mstamp;
            tp.rcvq_space.seq = tp.copied_seq;
            int maxwin = (int)tcp_full_space(tp);

            if (tp.window_clamp >= maxwin)
            {
                tp.window_clamp = (uint)maxwin;

                if (tcp_app_win > 0 && maxwin > 4 * tp.advmss)
                {
                    tp.window_clamp = (uint)Math.Max(maxwin - (maxwin >> tcp_app_win), 4 * tp.advmss);
                }
            }

            if (tcp_app_win > 0 && tp.window_clamp > 2 * tp.advmss && tp.window_clamp + tp.advmss > maxwin)
            {
                tp.window_clamp = (uint)Math.Max(2 * tp.advmss, maxwin - tp.advmss);
            }

            tp.rcv_ssthresh = Math.Min(tp.rcv_ssthresh, tp.window_clamp);
            tp.snd_cwnd_stamp = tcp_jiffies32;
            tp.rcvq_space.space = (uint)min3((int)tp.rcv_ssthresh, (int)tp.rcv_wnd, TCP_INIT_CWND * tp.advmss);
        }

        static void tcp_init_transfer(tcp_sock tp)
        {
            tcp_mtup_init(tp);
            tcp_init_metrics(tp);

            if (tp.total_retrans > 1 && tp.undo_marker > 0)
            {
                tcp_snd_cwnd_set(tp, 1);
            }
            else
            {
                tcp_snd_cwnd_set(tp, tcp_init_cwnd(tp));
            }
            tp.snd_cwnd_stamp = tcp_jiffies32;

            if (!tp.icsk_ca_initialized)
            {
                tcp_init_congestion_control(tp);
            }
            tcp_init_buffer_space(tp);
        }

        //用于更新 TCP 连接的发送速率限制（sk_pacing_rate），以控制数据包的发送速率，避免网络拥塞
        static void tcp_update_pacing_rate(tcp_sock tp)
        {
            long rate = (long)tp.mss_cache * 80000;
            if (tcp_snd_cwnd(tp) < tp.snd_ssthresh / 2)
            {
                rate *= sock_net(tp).ipv4.sysctl_tcp_pacing_ss_ratio;
            }
            else
            {
                rate *= sock_net(tp).ipv4.sysctl_tcp_pacing_ca_ratio;
            }

            rate *= Math.Max(tcp_snd_cwnd(tp), tp.packets_out);

            if (tp.srtt_us > 0)
            {
                rate /= tp.srtt_us;
            }
            tp.sk_pacing_rate = Math.Min(rate, tp.sk_max_pacing_rate);

            TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.sk_pacing_rate, tp.sk_pacing_rate);
        }

        static void tcp_initialize_rcv_mss(tcp_sock tp)
        {
            uint hint = Math.Min(tp.advmss, tp.mss_cache);
            hint = Math.Min(hint, tp.rcv_wnd / 2);
            hint = Math.Min(hint, TCP_MSS_DEFAULT);
            hint = Math.Max(hint, TCP_MIN_MSS);
            tp.icsk_ack.rcv_mss = (ushort)hint;
        }

        //判断是否合并，并且 判断True后进行合并操作
        static bool tcp_try_coalesce(tcp_sock tp, sk_buff tailSkb, sk_buff newSkb)
        {
            if (TCP_SKB_CB(tailSkb).end_seq != TCP_SKB_CB(newSkb).seq) //如果不是连续的Seq，则不合并
            {
                return false;
            }

            if (!skb_try_coalesce(tailSkb, newSkb))
            {
                return false;
            }

            TCP_SKB_CB(tailSkb).end_seq = TCP_SKB_CB(newSkb).end_seq;
            TCP_SKB_CB(tailSkb).ack_seq = TCP_SKB_CB(newSkb).ack_seq;
            TCP_SKB_CB(tailSkb).tcp_flags |= TCP_SKB_CB(newSkb).tcp_flags;

            if (TCP_SKB_CB(newSkb).has_rxtstamp)
            {
                TCP_SKB_CB(tailSkb).has_rxtstamp = true;
                tailSkb.tstamp = newSkb.tstamp;
            }

            return true;
        }

        static void tcp_rcv_nxt_update(tcp_sock tp, uint seq)
        {
            uint delta = seq - tp.rcv_nxt;
            tp.bytes_received += delta;
            tp.rcv_nxt = seq;
        }

        static int tcp_queue_rcv(tcp_sock tp, sk_buff skb)
        {
            sk_buff tail = skb_peek_tail(tp.sk_receive_queue);
            int eaten = (tail != null && tcp_try_coalesce(tp, tail, skb)) ? 1 : 0;
            tcp_rcv_nxt_update(tp, TCP_SKB_CB(skb).end_seq);
            if (eaten == 0)
            {
                __skb_queue_tail(tp.sk_receive_queue, skb);
            }
            return eaten;
        }

        static void tcp_measure_rcv_mss(tcp_sock tp, sk_buff skb)
        {
            uint lss = tp.icsk_ack.last_seg_size;
            tp.icsk_ack.last_seg_size = 0;

            int len = skb.nBufferLength;
            if (len >= tp.icsk_ack.rcv_mss)
            {
                if (len != tp.icsk_ack.rcv_mss)
                {
                    ulong val = (ulong)skb.nBufferLength << TCP_RMEM_TO_WIN_SCALE;
                    byte old_ratio = tp.scaling_ratio;
                    val /= (ulong)skb.nBufferLength;
                    val = Math.Clamp(val, 1, byte.MaxValue);

                    tp.scaling_ratio = (byte)val;

                    if (old_ratio != tp.scaling_ratio)
                    {
                        tp.window_clamp = (uint)tcp_win_from_space(tp, tp.sk_rcvbuf);
                    }
                }

                tp.icsk_ack.rcv_mss = (ushort)Math.Min(len, tp.advmss);
                if (BoolOk(TCP_SKB_CB(skb).tcp_flags & TCPHDR_PSH))
                {
                    tp.icsk_ack.pending |= (byte)inet_csk_ack_state_t.ICSK_ACK_PUSHED;
                }
            }
            else
            {
                if (len >= TCP_MSS_DEFAULT || (len >= TCP_MIN_MSS && !BoolOk(tcp_flag_word(tcp_hdr(skb)) & TCP_REMNANT)))
                {
                    tp.icsk_ack.last_seg_size = (ushort)len;
                    if (len == lss)
                    {
                        tp.icsk_ack.rcv_mss = (ushort)len;
                        return;
                    }
                }

                if (BoolOk(tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_PUSHED))
                {
                    tp.icsk_ack.pending |= (byte)inet_csk_ack_state_t.ICSK_ACK_PUSHED2;
                }
                tp.icsk_ack.pending |= (byte)inet_csk_ack_state_t.ICSK_ACK_PUSHED;
            }
        }

        //win_dep：一个标志，指示是否需要对样本进行微调.
        static void tcp_rcv_rtt_update(tcp_sock tp, long sample, int win_dep)
        {
            long new_sample = tp.rcv_rtt_est.rtt_us;
            long m = sample;

            if (new_sample != 0)
            {
                if (win_dep == 0)
                {
                    m -= (new_sample >> 3);
                    new_sample += m;
                }
                else
                {
                    m <<= 3;
                    if (m < new_sample)
                    {
                        new_sample = m;
                    }
                }
            }
            else
            {
                new_sample = m << 3;
            }

            tp.rcv_rtt_est.rtt_us = new_sample;
        }

        static void tcp_rcv_rtt_measure(tcp_sock tp)
        {
            if (tp.rcv_rtt_est.time == 0)
            {
                goto new_measure;
            }

            if (before(tp.rcv_nxt, tp.rcv_rtt_est.seq))
            {
                return;
            }

            long delta_us = tcp_stamp_us_delta(tp.tcp_mstamp, tp.rcv_rtt_est.time);
            if (delta_us == 0)
            {
                delta_us = 1;
            }
            tcp_rcv_rtt_update(tp, delta_us, 1);

        new_measure:
            tp.rcv_rtt_est.seq = tp.rcv_nxt + tp.rcv_wnd;
            tp.rcv_rtt_est.time = tp.tcp_mstamp;
        }

        static void tcp_incr_quickack(tcp_sock tp, uint max_quickacks)
        {
            uint quickacks = (uint)(tp.rcv_wnd / (2 * tp.icsk_ack.rcv_mss));
            if (quickacks == 0)
            {
                quickacks = 2;
            }

            quickacks = Math.Min(quickacks, max_quickacks);
            if (quickacks > tp.icsk_ack.quick)
            {
                tp.icsk_ack.quick = (byte)quickacks;
            }
        }

        static int __tcp_grow_window(tcp_sock tp, sk_buff skb, int skbtruesize)
        {
            int truesize = (int)(tcp_win_from_space(tp, skbtruesize) >> 1);
            int window = (int)(tcp_win_from_space(tp, sock_net(tp).ipv4.sysctl_tcp_rmem[2]) >> 1);

            while (tp.rcv_ssthresh <= window)
            {
                if (truesize <= skb.nBufferLength)
                {
                    return 2 * tp.icsk_ack.rcv_mss;
                }
                truesize >>= 1;
                window >>= 1;
            }
            return 0;
        }

        static void tcp_grow_window(tcp_sock tp, sk_buff skb, bool adjust)
        {
            int room = (int)(Math.Min(tp.window_clamp, tcp_space(tp)) - tp.rcv_ssthresh);
            if (room <= 0)
            {
                return;
            }

            if (!tcp_under_memory_pressure(tp))
            {
                int truesize = skb.nBufferLength;
                int incr = 0;

                if (tcp_win_from_space(tp, truesize) <= skb.nBufferLength)
                {
                    incr = 2 * tp.advmss;
                }
                else
                {
                    incr = __tcp_grow_window(tp, skb, truesize);
                }

                if (incr > 0)
                {
                    incr = Math.Max(incr, 2 * skb.nBufferLength);
                    tp.rcv_ssthresh += (uint)Math.Min(room, incr);
                    tp.icsk_ack.quick |= 1;
                }
            }
            else
            {
                tcp_adjust_rcv_ssthresh(tp);
            }
        }

        static void tcp_event_data_recv(tcp_sock tp, sk_buff skb)
        {
            long now = tcp_jiffies32;
            inet_csk_schedule_ack(tp);
            tcp_measure_rcv_mss(tp, skb);
            tcp_rcv_rtt_measure(tp);

            if (tp.icsk_ack.ato == 0)
            {
                tcp_incr_quickack(tp, TCP_MAX_QUICKACKS);
                tp.icsk_ack.ato = TCP_ATO_MIN;
            }
            else
            {
                long m = now - tp.icsk_ack.lrcvtime;
                if (m <= TCP_ATO_MIN / 2)
                {
                    tp.icsk_ack.ato = (tp.icsk_ack.ato >> 1) + TCP_ATO_MIN / 2;
                }
                else if (m < tp.icsk_ack.ato)
                {
                    tp.icsk_ack.ato = (tp.icsk_ack.ato >> 1) + m;
                    if (tp.icsk_ack.ato > tp.icsk_rto)
                    {
                        tp.icsk_ack.ato = tp.icsk_rto;
                    }
                }
                else if (m > tp.icsk_rto)
                {
                    tcp_incr_quickack(tp, TCP_MAX_QUICKACKS);
                }
            }

            tp.icsk_ack.lrcvtime = now;
            tcp_ecn_check_ce(tp, skb);

            if (skb.nBufferLength >= 128)
            {
                tcp_grow_window(tp, skb, true);
            }
        }

        static void tcp_dsack_set(tcp_sock tp, uint seq, uint end_seq)
        {
            if (tcp_is_sack(tp) && sock_net(tp).ipv4.sysctl_tcp_dsack > 0)
            {
                tp.rx_opt.dsack = 1;
                tp.duplicate_sack[0].start_seq = seq;
                tp.duplicate_sack[0].end_seq = end_seq;
            }
        }

        static bool tcp_sack_extend(tcp_sack_block sp, uint seq, uint end_seq)
        {
            if (!after(seq, sp.end_seq) && !after(sp.start_seq, end_seq))
            {
                if (before(seq, sp.start_seq))
                {
                    sp.start_seq = seq;
                }
                if (after(end_seq, sp.end_seq))
                {
                    sp.end_seq = end_seq;
                }
                return true;
            }
            return false;
        }

        static void tcp_dsack_extend(tcp_sock tp, uint seq, uint end_seq)
        {
            if (tp.rx_opt.dsack == 0)
            {
                tcp_dsack_set(tp, seq, end_seq);
            }
            else
            {
                tcp_sack_extend(tp.duplicate_sack[0], seq, end_seq);
            }
        }

        static void tcp_drop_reason(tcp_sock tp, sk_buff skb, int reason)
        {
            sk_drops_add(tp, skb);
            sk_skb_reason_drop(tp, skb, reason);
        }

        //tcp_ofo_queue 是 TCP 协议栈中用于处理乱序数据包的队列。
        //当 TCP 接收到的数据包不是按顺序到达时，这些数据包会被放入 tcp_ofo_queue 中，等待后续处理
        //存储乱序数据包：tcp_ofo_queue 用于存储那些序列号不在当前接收窗口内的数据包。
        //这些数据包可能因为网络延迟或丢包等原因而乱序到达
        //数据包重组：当后续的数据包到达并填补了乱序数据包之间的空缺时，
        //tcp_ofo_queue 中的数据包会被重新排序并移入接收队列中，以便应用程序按顺序读取
        static void tcp_ofo_queue(tcp_sock tp)
        {
            uint dsack_high = tp.rcv_nxt;
            bool fin, fragstolen, eaten;
            sk_buff skb, tail;
            rb_node p;

            p = rb_first(tp.out_of_order_queue);
            while (p != null)
            {
                skb = rb_to_skb(p);
                if (after(TCP_SKB_CB(skb).seq, tp.rcv_nxt))
                {
                    break;
                }

                if (before(TCP_SKB_CB(skb).seq, dsack_high))
                {
                    uint dsack = dsack_high;
                    if (before(TCP_SKB_CB(skb).end_seq, dsack_high))
                    {
                        dsack_high = TCP_SKB_CB(skb).end_seq;
                    }
                    tcp_dsack_extend(tp, TCP_SKB_CB(skb).seq, dsack);
                }

                p = rb_next(p);
                rb_erase(skb.rbnode, tp.out_of_order_queue);

                if (!after(TCP_SKB_CB(skb).end_seq, tp.rcv_nxt))
                {
                    tcp_drop_reason(tp, skb, skb_drop_reason.SKB_DROP_REASON_TCP_OFO_DROP);
                    continue;
                }

                tail = skb_peek_tail(tp.sk_receive_queue);
                eaten = tail != null && tcp_try_coalesce(tp, tail, skb);
                tcp_rcv_nxt_update(tp, TCP_SKB_CB(skb).end_seq);
                if (!eaten)
                {
                    __skb_queue_tail(tp.sk_receive_queue, skb);
                }
                else
                {
                    kfree_skb(tp, skb);
                }
            }
        }

        // TCP 使用 SACK 选项时，它可以更精确地告诉 ###发送方### 哪些数据段已经被成功接收，而不需要等待丢失的数据段被重传。
        //tcp_sack_remove 函数的主要功能是从一个 TCP 连接的 SACK 列表中移除已经成功到达的数据段。这通常发生在以下几种情况：
        //数据段被确认：当一个之前被 SACK 标记为丢失的数据段最终被确认时，它需要从 SACK 列表中移除。
        //重传成功：当一个重传的数据段被确认后，它也不再需要在 SACK 列表中。
        //窗口滑动：当 TCP 窗口滑动，且之前 SACK 的数据段已经不再是“未来”的数据时，它们也应该从 SACK 列表中移除。
        static void tcp_sack_remove(tcp_sock tp)
        {
            if (RB_EMPTY_ROOT(tp.out_of_order_queue))
            {
                tp.rx_opt.num_sacks = 0;
                return;
            }

            int num_sacks = tp.rx_opt.num_sacks;
            for (int i = num_sacks - 1; i >= 0; i--)
            {
                tcp_sack_block sp = tp.selective_acks[i];
                if (!before(tp.rcv_nxt, sp.start_seq)) //如果rcv_nxt 在这个区间块的后面，那么则移除这个区间块
                {
                    for (int j = i + 1; j < num_sacks; j++)
                    {
                        tp.selective_acks[j - 1] = tp.selective_acks[j];
                    }
                    num_sacks--;
                }
            }
            tp.rx_opt.num_sacks = (byte)num_sacks;
        }

        static void tcp_enter_quickack_mode(tcp_sock tp, uint max_quickacks)
        {
            tcp_incr_quickack(tp, max_quickacks);
            inet_csk_exit_pingpong_mode(tp);
            tp.icsk_ack.ato = TCP_ATO_MIN;
        }

        static void __tcp_ecn_check_ce(tcp_sock tp, sk_buff skb)
        {
            switch (TCP_SKB_CB(skb).ip_dsfield & INET_ECN_MASK)
            {
                case INET_ECN_NOT_ECT:
                    if (BoolOk(tp.ecn_flags & TCP_ECN_SEEN))
                    {
                        tcp_enter_quickack_mode(tp, 2);
                    }
                    break;
                case INET_ECN_CE:
                    if (tcp_ca_needs_ecn(tp))
                    {
                        tcp_ca_event_func(tp, tcp_ca_event.CA_EVENT_ECN_IS_CE);
                    }

                    if (!BoolOk(tp.ecn_flags & TCP_ECN_DEMAND_CWR))
                    {
                        tcp_enter_quickack_mode(tp, 2);
                        tp.ecn_flags |= TCP_ECN_DEMAND_CWR;
                    }
                    tp.ecn_flags |= TCP_ECN_SEEN;
                    break;
                default:
                    if (tcp_ca_needs_ecn(tp))
                    {
                        tcp_ca_event_func(tp, tcp_ca_event.CA_EVENT_ECN_NO_CE);
                    }
                    tp.ecn_flags |= TCP_ECN_SEEN;
                    break;
            }
        }

        static void tcp_ecn_check_ce(tcp_sock tp, sk_buff skb)
        {
            if (BoolOk(tp.ecn_flags & TCP_ECN_OK))
            {
                __tcp_ecn_check_ce(tp, skb);
            }
        }

        static bool tcp_ooo_try_coalesce(tcp_sock tp, sk_buff to, sk_buff from)
        {
            bool res = tcp_try_coalesce(tp, to, from);
            return res;
        }

        //当接收方收到的数据段与已有的 SACK 块相邻或部分重叠时，tcp_sack_maybe_coalesce 函数会尝试将这些 SACK 块合并成一个更大的连续块
        static void tcp_sack_maybe_coalesce(tcp_sock tp)
        {
            int this_sack;
            tcp_sack_block[] sp = tp.selective_acks;
            int swalkIndex = 1;
            tcp_sack_block swalk = tp.selective_acks[swalkIndex];

            for (this_sack = 1; this_sack < tp.rx_opt.num_sacks;)
            {
                if (tcp_sack_extend(sp[0], swalk.start_seq, swalk.end_seq))
                {
                    int i;
                    tp.rx_opt.num_sacks--;
                    for (i = this_sack; i < tp.rx_opt.num_sacks; i++)
                    {
                        sp[i] = sp[i + 1];
                    }
                    continue;
                }
                this_sack++;
                swalkIndex++;
                swalk = tp.selective_acks[swalkIndex];
            }
        }

        static void tcp_sack_new_ofo_skb(tcp_sock tp, uint seq, uint end_seq)
        {
            int spIndex = 0;
            tcp_sack_block sp = tp.selective_acks[spIndex];

            int cur_sacks = tp.rx_opt.num_sacks;
            int this_sack;

            if (cur_sacks == 0)
            {
                goto new_sack;
            }

            for (this_sack = 0; this_sack < cur_sacks; this_sack++, spIndex++)
            {
                sp = tp.selective_acks[spIndex];
                if (tcp_sack_extend(sp, seq, end_seq))
                {
                    if (this_sack >= TCP_SACK_BLOCKS_EXPECTED)
                    {
                        tcp_sack_compress_send_ack(tp);
                    }

                    for (; this_sack > 0; this_sack--, spIndex--)
                    {
                        sp = tp.selective_acks[spIndex];
                        var temp = sp;
                        sp = tp.selective_acks[spIndex - 1];
                        tp.selective_acks[spIndex - 1] = temp;
                    }

                    if (cur_sacks > 1)
                    {
                        tcp_sack_maybe_coalesce(tp);
                    }
                    return;
                }
            }

            if (this_sack >= TCP_SACK_BLOCKS_EXPECTED)
            {
                tcp_sack_compress_send_ack(tp);
            }

            if (this_sack >= TCP_NUM_SACKS)
            {
                this_sack--;
                tp.rx_opt.num_sacks--;
                spIndex--;
                sp = tp.selective_acks[spIndex];
            }

            for (; this_sack > 0; this_sack--, spIndex--)
            {
                sp = tp.selective_acks[spIndex];
                tp.selective_acks[spIndex] = tp.selective_acks[spIndex - 1];
            }

        new_sack:
            sp.start_seq = seq;
            sp.end_seq = end_seq;
            tp.rx_opt.num_sacks++;

        }

        static void tcp_clamp_window(tcp_sock tp)
        {
            net net = sock_net(tp);
            tp.icsk_ack.quick = 0;
            int rmem2 = net.ipv4.sysctl_tcp_rmem[2];

            if (tp.sk_rcvbuf < rmem2 && !tcp_under_memory_pressure(tp))
            {
                tp.sk_rcvbuf = Math.Min((int)tp.sk_rmem_alloc, rmem2);
            }

            if (tp.sk_rmem_alloc > tp.sk_rcvbuf)
            {
                tp.rcv_ssthresh = Math.Min(tp.window_clamp, 2U * tp.advmss);
            }
        }

        static sk_buff tcp_skb_next(sk_buff skb, sk_buff_head list)
        {
            if (list != null)
            {
                return !skb_queue_is_last(list, skb) ? skb.next : null;
            }
            return skb_rb_next(skb);
        }

        static sk_buff tcp_collapse_one(tcp_sock tp, sk_buff skb, sk_buff_head list, rb_root root)
        {
            sk_buff next = tcp_skb_next(skb, list);

            if (list != null)
            {
                __skb_unlink(skb, list);
            }
            else
            {
                rb_erase(skb.rbnode, root);
            }

            kfree_skb(tp, skb);
            return next;
        }

        static void tcp_collapse(tcp_sock tp, sk_buff_head list, rb_root root, sk_buff head, sk_buff tail, uint start, uint end)
        {
            sk_buff skb = head, n;
            sk_buff_head tmp = new sk_buff_head();
            bool end_of_skbs;

        restart:
            for (end_of_skbs = true; skb != null && skb != tail; skb = n)
            {
                n = tcp_skb_next(skb, list);

                if (!before(start, TCP_SKB_CB(skb).end_seq))
                {
                    skb = tcp_collapse_one(tp, skb, list, root);
                    if (skb == null)
                    {
                        break;
                    }
                    goto restart;
                }

                if (!BoolOk(TCP_SKB_CB(skb).tcp_flags & (TCPHDR_SYN | TCPHDR_FIN)) &&
                    (tcp_win_from_space(tp, skb.nBufferLength) > skb.nBufferLength ||
                     before(TCP_SKB_CB(skb).seq, start)))
                {
                    end_of_skbs = false;
                    break;
                }

                if (n != null && n != tail && TCP_SKB_CB(skb).end_seq != TCP_SKB_CB(n).seq)
                {
                    end_of_skbs = false;
                    break;
                }

            skip_this:
                start = TCP_SKB_CB(skb).end_seq;
            }

            if (end_of_skbs || BoolOk(TCP_SKB_CB(skb).tcp_flags & (TCPHDR_SYN | TCPHDR_FIN)))
            {
                return;
            }
            __skb_queue_head_init(tmp);

            while (before(start, end))
            {
                int copy = Math.Min(PAGE_SIZE, (int)(end - start));

                sk_buff nskb = tcp_stream_alloc_skb(tp);
                TCP_SKB_CB(nskb).CopyFrom(TCP_SKB_CB(skb));
                TCP_SKB_CB(nskb).seq = TCP_SKB_CB(nskb).end_seq = start;

                if (list != null)
                {
                    __skb_queue_before(list, skb, nskb);
                }
                else
                {
                    __skb_queue_tail(tmp, nskb);
                }

                while (copy > 0)
                {
                    int offset = (int)(start - TCP_SKB_CB(skb).seq);
                    int size = (int)(TCP_SKB_CB(skb).end_seq - start);

                    NetLog.Assert(offset >= 0);
                    if (size > 0)
                    {
                        size = Math.Min(copy, size);
                        skb.GetTcpReceiveBufferSpan().Slice(offset, size).CopyTo(nskb.mBuffer.AsSpan().Slice(nskb.nBufferOffset + nskb.nBufferLength));
                        nskb.nBufferLength += size;

                        TCP_SKB_CB(nskb).end_seq += (uint)size;
                        copy -= size;
                        start += (uint)size;
                    }

                    if (!before(start, TCP_SKB_CB(skb).end_seq))
                    {
                        skb = tcp_collapse_one(tp, skb, list, root);
                        if (skb == null || skb == tail || BoolOk(TCP_SKB_CB(skb).tcp_flags & (TCPHDR_SYN | TCPHDR_FIN)))
                        {
                            goto end;
                        }
                    }
                }
            }
        end:
            for (skb = tmp.next, n = skb.next; skb != tmp; skb = n, n = skb.next)
            {
                tcp_rbtree_insert(root, skb);
            }
        }

        static void tcp_collapse_ofo_queue(tcp_sock tp)
        {
            uint range_truesize, sum_tiny = 0;
            sk_buff skb, head;
            uint start, end;

            skb = skb_rb_first(tp.out_of_order_queue);
        new_range:
            if (skb == null)
            {
                tp.ooo_last_skb = skb_rb_last(tp.out_of_order_queue);
                return;
            }

            start = TCP_SKB_CB(skb).seq;
            end = TCP_SKB_CB(skb).end_seq;
            range_truesize = (uint)skb.nBufferLength;

            for (head = skb; ;)
            {
                skb = skb_rb_next(skb);
                if (skb == null || after(TCP_SKB_CB(skb).seq, end) ||
                    before(TCP_SKB_CB(skb).end_seq, start))
                {
                    if (range_truesize != head.nBufferLength ||
                        end - start >= PAGE_SIZE)
                    {
                        tcp_collapse(tp, null, tp.out_of_order_queue, head, skb, start, end);
                    }
                    else
                    {
                        sum_tiny += range_truesize;
                        if (sum_tiny > tp.sk_rcvbuf >> 3)
                        {
                            return;
                        }
                    }
                    goto new_range;
                }

                range_truesize += (uint)skb.nBufferLength;
                if (before(TCP_SKB_CB(skb).seq, start))
                {
                    start = TCP_SKB_CB(skb).seq;
                }
                if (after(TCP_SKB_CB(skb).end_seq, end))
                {
                    end = TCP_SKB_CB(skb).end_seq;
                }
            }
        }

        //tcp_prune_ofo_queue 的主要功能是在[接收缓存空间不足]时，清理乱序队列中的数据包，以腾出空间接收新的数据包。
        static bool tcp_prune_ofo_queue(tcp_sock tp, sk_buff in_skb)
        {
            rb_node node, prev;
            bool pruned = false;
            int goal;

            if (RB_EMPTY_ROOT(tp.out_of_order_queue))
            {
                return false;
            }

            goal = tp.sk_rcvbuf >> 3;
            node = tp.ooo_last_skb.rbnode;

            do {
                sk_buff skb = rb_to_skb(node);
                if (after(TCP_SKB_CB(in_skb).seq, TCP_SKB_CB(skb).seq))
                {
                    break;
                }
                pruned = true;
                prev = rb_prev(node);
                rb_erase(node, tp.out_of_order_queue);
                goal -= skb.nBufferLength;
                tp.ooo_last_skb = rb_to_skb(prev);
                if (prev == null || goal <= 0)
                {
                    if (tp.sk_rmem_alloc <= tp.sk_rcvbuf && !tcp_under_memory_pressure(tp))
                    {
                        break;
                    }
                    goal = tp.sk_rcvbuf >> 3;
                }
                node = prev;
            } while (node != null);

            if (pruned)
            {
                if (tp.rx_opt.sack_ok != 0)
                {
                    tcp_sack_reset(tp.rx_opt);
                }
            }
            return pruned;
        }

        //它主要在接收队列的内存占用超过一定阈值时被调用，目的是减少接收队列的内存占用，避免内存耗尽。
        static int tcp_prune_queue(tcp_sock tp, sk_buff in_skb)
        {
            if (tp.sk_rmem_alloc >= tp.sk_rcvbuf)
            {
                tcp_clamp_window(tp);
            }
            else if (tcp_under_memory_pressure(tp))
            {
                tcp_adjust_rcv_ssthresh(tp);
            }

            if (tp.sk_rmem_alloc <= tp.sk_rcvbuf)
            {
                return 0;
            }

	        tcp_collapse_ofo_queue(tp);
            if (!skb_queue_empty(tp.sk_receive_queue))
            {
                tcp_collapse(tp, tp.sk_receive_queue, null, skb_peek(tp.sk_receive_queue), null, tp.copied_seq, tp.rcv_nxt);
            }

            if (tp.sk_rmem_alloc <= tp.sk_rcvbuf)
            {
                return 0;
            }

	        tcp_prune_ofo_queue(tp, in_skb);

            if (tp.sk_rmem_alloc <= tp.sk_rcvbuf)
            {
                return 0;
            }

            tp.pred_flags = 0;
	        return -1;
        }

        //检查是否有足够的接收缓存：确保新到达的数据包可以被接收。
        //清理接收队列：如果接收缓存不足，尝试合并接收队列中的数据包以减少空间占用。
        //清理乱序队列：如果接收队列清理后仍不足，清理乱序队列中的数据包。
        //强制分配缓存：在某些情况下（如接收队列为空），强制分配缓存以确保数据包可以被接收
        static int tcp_try_rmem_schedule(tcp_sock tp, sk_buff skb)
        {
            if (tp.sk_rmem_alloc > tp.sk_rcvbuf)
            {
                if (tcp_prune_queue(tp, skb) < 0)
                {
                    return -1;
                }
            }
            return 0;
        }

        //tcp_data_queue_ofo 是一个用于处理 TCP 乱序数据包的函数。
        //当接收到的 TCP 数据包的序列号不是期望的下一个序列号时，该函数会将这些乱序数据包添加到乱序队列（out_of_order_queue）中。
        //这个队列的数据结构是红黑树，用于高效地管理和排序乱序数据包
        static void tcp_data_queue_ofo(tcp_sock tp, sk_buff skb)
        {
            rb_node p, parent;
            bool left_child = true;

            sk_buff skb1;
            uint seq, end_seq;

            tcp_ecn_check_ce(tp, skb);
            if (tcp_try_rmem_schedule(tp, skb) != 0)
            {
                tcp_drop_reason(tp, skb, skb_drop_reason.SKB_DROP_REASON_PROTO_MEM);
                return;
            }

            tp.pred_flags = 0;
            inet_csk_schedule_ack(tp);

            tp.rcv_ooopack += 1;
            TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.OFO_QUEUE);
            seq = TCP_SKB_CB(skb).seq;
            end_seq = TCP_SKB_CB(skb).end_seq;

            if (RB_EMPTY_ROOT(tp.out_of_order_queue))
            {
                if (tcp_is_sack(tp))
                {
                    tp.rx_opt.num_sacks = 1;
                    tp.selective_acks[0].start_seq = seq;
                    tp.selective_acks[0].end_seq = end_seq;
                }

                tcp_rbtree_insert(tp.out_of_order_queue, skb);
                tp.ooo_last_skb = skb;
                goto end;
            }

            bool b_coalesce_ok = tcp_ooo_try_coalesce(tp, tp.ooo_last_skb, skb);
        coalesce_done:
            if (b_coalesce_ok)
            {
                if (tcp_is_sack(tp))
                {
                    tcp_grow_window(tp, skb, true);
                }
                kfree_skb(tp, skb);
                skb = null;
                goto add_sack;
            }

            if (!before(seq, TCP_SKB_CB(tp.ooo_last_skb).end_seq))
            {
                parent = tp.ooo_last_skb.rbnode;
                p = parent.rb_right;
                left_child = false;
                goto insert;
            }

            parent = null;
            p = tp.out_of_order_queue.rb_node;
            while (p != null)
            {
                parent = p;
                skb1 = rb_entry(parent);
                if (before(seq, TCP_SKB_CB(skb1).seq))
                {
                    p = parent.rb_left;
                    left_child = true;
                    continue;
                }

                if (before(seq, TCP_SKB_CB(skb1).end_seq))
                {
                    if (!after(end_seq, TCP_SKB_CB(skb1).end_seq)) //已经完全有了覆盖了这个Skb
                    {
                        tcp_drop_reason(tp, skb, skb_drop_reason.SKB_DROP_REASON_TCP_OFOMERGE);
                        skb = null;
                        tcp_dsack_set(tp, seq, end_seq);
                        goto add_sack;
                    }

                    if (after(seq, TCP_SKB_CB(skb1).seq))
                    {
                        tcp_dsack_set(tp, seq, TCP_SKB_CB(skb1).end_seq);
                    }
                    else
                    {
                        rb_replace_node(skb1.rbnode, skb.rbnode, tp.out_of_order_queue);
                        tcp_dsack_extend(tp, TCP_SKB_CB(skb1).seq, TCP_SKB_CB(skb1).end_seq);
                        tcp_drop_reason(tp, skb1, skb_drop_reason.SKB_DROP_REASON_TCP_OFOMERGE);
                        goto merge_right;
                    }
                }
                else if (tcp_ooo_try_coalesce(tp, skb1, skb))
                {
                    goto coalesce_done;
                }

                p = parent.rb_right;
                left_child = false;
            }
        insert:
            rb_link_node(skb.rbnode, parent, left_child);
            rb_insert_color(skb.rbnode, tp.out_of_order_queue);
        merge_right:
            while ((skb1 = skb_rb_next(skb)) != null)
            {
                if (!after(end_seq, TCP_SKB_CB(skb1).seq))
                {
                    break;
                }

                if (before(end_seq, TCP_SKB_CB(skb1).end_seq))
                {
                    tcp_dsack_extend(tp, TCP_SKB_CB(skb1).seq, end_seq);
                    break;
                }

                rb_erase(skb1.rbnode, tp.out_of_order_queue);
                tcp_dsack_extend(tp, TCP_SKB_CB(skb1).seq, TCP_SKB_CB(skb1).end_seq);
                tcp_drop_reason(tp, skb1, skb_drop_reason.SKB_DROP_REASON_TCP_OFOMERGE);
            }

            if (skb1 == null)
            {
                tp.ooo_last_skb = skb;
            }

        add_sack:
            if (tcp_is_sack(tp))
            {
                tcp_sack_new_ofo_skb(tp, seq, end_seq);
            }

        end:
            if (skb != null)
            {
                if (tcp_is_sack(tp))
                {
                    tcp_grow_window(tp, skb, false);
                }
            }
        }

        static void tcp_data_queue(tcp_sock tp, sk_buff skb)
        {
            int reason;
            int eaten = 0;
            if (TCP_SKB_CB(skb).seq == TCP_SKB_CB(skb).end_seq)
            {
                kfree_skb(tp, skb);
                return;
            }

            skb_pull(skb, tcp_hdr(skb).doff);
            reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;
            tp.rx_opt.dsack = 0;
            if (TCP_SKB_CB(skb).seq == tp.rcv_nxt) //刚好是等待的seq
            {
                if (tcp_receive_window(tp) == 0)
                {
                    reason = skb_drop_reason.SKB_DROP_REASON_TCP_ZEROWINDOW;
                    goto out_of_window;
                }
                goto queue_and_out;
            }

            //这里判断了，end_seq的值，必须大于 rcv_nxt, 否则包被丢弃
            if (!after(TCP_SKB_CB(skb).end_seq, tp.rcv_nxt))
            {
                tcp_dsack_set(tp, TCP_SKB_CB(skb).seq, TCP_SKB_CB(skb).end_seq);
                goto out_of_window;
            }

            if (!before(TCP_SKB_CB(skb).seq, tp.rcv_nxt + tcp_receive_window(tp))) //接收了 太后面的数据
            {
                goto out_of_window;
            }

            if (before(TCP_SKB_CB(skb).seq, tp.rcv_nxt)) //接受了过期数据
            {
                tcp_dsack_set(tp, TCP_SKB_CB(skb).seq, tp.rcv_nxt);
                if (tcp_receive_window(tp) == 0)
                {
                    goto out_of_window;
                }

                //在 TCP 协议中，rcv_nxt 表示接收方期望的下一个序列号。
                //当接收到一个部分数据包（即其序列号小于 rcv_nxt）时，虽然这个数据包的部分内容已经接收，
                //但它的 end_seq（数据包的结束序列号）可能包含了新的数据。
                //因此，更新 rcv_nxt 为 end_seq 是为了确保接收方能够正确处理后续的数据包。
                goto queue_and_out;
            }

            tcp_data_queue_ofo(tp, skb); //进入乱序队列逻辑
            return;

        queue_and_out:
            if (tcp_try_rmem_schedule(tp, skb) != 0)
            {
                tp.icsk_ack.pending |= (byte)inet_csk_ack_state_t.ICSK_ACK_NOMEM | (byte)inet_csk_ack_state_t.ICSK_ACK_NOW;
                inet_csk_schedule_ack(tp);
                if (skb_queue_len(tp.sk_receive_queue) > 0 && skb.nBufferLength > 0)
                {
                    reason = skb_drop_reason.SKB_DROP_REASON_PROTO_MEM;
                    goto drop;
                }
            }

            eaten = tcp_queue_rcv(tp, skb);
            if (skb.nBufferLength > 0)
            {
                tcp_event_data_recv(tp, skb);
            }

            if (!RB_EMPTY_ROOT(tp.out_of_order_queue))
            {
                tcp_ofo_queue(tp);
                if (RB_EMPTY_ROOT(tp.out_of_order_queue))
                {
                    tp.icsk_ack.pending |= (byte)inet_csk_ack_state_t.ICSK_ACK_NOW;
                }
            }

            if (tp.rx_opt.num_sacks > 0)
            {
                tcp_sack_remove(tp);
            }
            tcp_fast_path_check(tp);
            if (eaten > 0)
            {
                kfree_skb(tp, skb);
            }
            return;

        out_of_window:
            tcp_enter_quickack_mode(tp, TCP_MAX_QUICKACKS);
            inet_csk_schedule_ack(tp);
        drop:
            tcp_drop_reason(tp, skb, reason);
            return;
        }

        static void tcp_data_snd_check(tcp_sock tp)
        {
            tcp_push_pending_frames(tp);
            tcp_check_space(tp);
        }

        static void tcp_store_ts_recent(tcp_sock tp)
        {
            tp.rx_opt.ts_recent = tp.rx_opt.rcv_tsval;
            tp.rx_opt.ts_recent_stamp = tcp_jiffies32;
        }

        static void tcp_replace_ts_recent(tcp_sock tp, uint seq)
        {
            if (tp.rx_opt.saw_tstamp && !after(seq, tp.rcv_wup))
            {
                if (tcp_paws_check(tp.rx_opt, 0))
                {
                    tcp_store_ts_recent(tp);
                }
            }
        }

        //tcp_paws_reject 是与 TCP（传输控制协议）中的 PAWS(Protection Against Wrapped Sequence numbers，防止序列号缠绕保护) 机制相关的术语。
        //PAWS 是一种用来防止旧的数据包在 TCP 连接中被错误接受的机制，尤其是在序列号可能重复的情况下。
        //TCP 使用32位序列号，这意味着有4,294,967,296个可能的序列号。
        //当一个连接持续时间非常长，或者网络中存在异常大的延迟时，可能会遇到序列号循环回开始的情况。
        //为了应对这种情况，TCP 引入了时间戳选项，这个选项可以在每个数据包中携带一个时间戳。
        //接收方可以使用这个时间戳来判断一个具有相同序列号的数据包是新的还是旧的重传。
        //如果一个数据包的时间戳比之前收到的同一个序列号的数据包的时间戳更早，那么这个数据包将被拒绝，
        //这便是 tcp_paws_reject 的含义：因为违反了 PAWS 规则而拒绝了该数据包
        static bool tcp_paws_check(tcp_options_received rx_opt, int paws_win)
        {
            if ((int)(rx_opt.ts_recent - rx_opt.rcv_tsval) <= paws_win)
            {
                return true;
            }

            if (tcp_jiffies32 > rx_opt.ts_recent_stamp + TCP_PAWS_WRAP)
            {
                return true;
            }

            if (rx_opt.ts_recent == 0)
            {
                return true;
            }
            return false;
        }

        //是 Linux 内核 TCP 协议栈中用于实现 PAWS（Protection Against Wrapped Sequences，防止序列号回绕攻击）机制的核心函数。
        //PAWS 是一种基于 TCP 时间戳选项的机制，用于检测并拒绝过期的重复报文，从而保护 TCP 连接免受旧数据包的干扰
        static bool tcp_paws_reject(tcp_options_received rx_opt, int rst)
        {
            if (tcp_paws_check(rx_opt, 0))
            {
                return false;
            }

            if (rst > 0 && tcp_jiffies32 > rx_opt.ts_recent_stamp + TCP_PAWS_MSL)
            {
                return false;
            }
            return true;
        }

        static void tcp_clear_options(tcp_options_received rx_opt)
        {
            rx_opt.tstamp_ok = rx_opt.sack_ok = 0;
            rx_opt.wscale_ok = rx_opt.snd_wscale = 0;
        }

        static bool __tcp_oow_rate_limited(net net, ref long last_oow_ack_time)
        {
            if (last_oow_ack_time > 0)
            {
                long elapsed = tcp_jiffies32 - last_oow_ack_time;
                if (elapsed >= 0 && elapsed < net.ipv4.sysctl_tcp_invalid_ratelimit)
                {
                    return true;
                }
            }

            last_oow_ack_time = tcp_jiffies32;
            return false;
        }

        //用于发送挑战确认（Challenge ACK）报文。挑战确认报文是一种特殊的 ACK 报文，用于应对 TCP 序列号预测攻击。
        //作用 应对 TCP 序列号预测攻击：当 TCP 连接处于已建立状态时，如果收到一个不符合预期的 SYN 报文，接收方会发送一个挑战 ACK 报文。
        //验证对端状态：通过发送挑战 ACK 报文，接收方可以验证对端是否仍然处于连接状态。
        static void tcp_send_challenge_ack(tcp_sock tp)
        {
            net net = sock_net(tp);
            if (__tcp_oow_rate_limited(net, ref tp.last_oow_ack_time))
            {
                return;
            }

            uint ack_limit = (uint)net.ipv4.sysctl_tcp_challenge_ack_limit;
            if (ack_limit == int.MaxValue)
            {
                tcp_send_ack(tp);
                return;
            }

            long now = tcp_jiffies32 / HZ;
            if (now != net.ipv4.tcp_challenge_timestamp)
            {
                uint half = (ack_limit + 1) >> 1;
                net.ipv4.tcp_challenge_timestamp = now;
                net.ipv4.tcp_challenge_count = RandomTool.Random(half, ack_limit + half - 1);
            }

            if (net.ipv4.tcp_challenge_count > 0)
            {
                net.ipv4.tcp_challenge_count--;
                tcp_send_ack(tp);
            }
        }

        static uint tcp_highest_sack_seq(tcp_sock tp)
        {
            if (tp.sacked_out == 0)
            {
                return tp.snd_una;
            }

            if (tp.highest_sack == null)
            {
                return tp.snd_nxt;
            }

            return TCP_SKB_CB(tp.highest_sack).seq;
        }


        static void tcp_snd_una_update(tcp_sock tp, uint ack)
        {
            uint delta = ack - tp.snd_una;
            tp.bytes_acked += delta;
            tp.snd_una = ack;
        }

        static void tcp_in_ack_event(tcp_sock tp, uint flags)
        {
            if (tp.icsk_ca_ops.in_ack_event != null)
            {
                tp.icsk_ca_ops.in_ack_event(tp, flags);
            }
        }

        static bool tcp_may_update_window(tcp_sock tp, uint ack, uint ack_seq, uint nwin)
        {
            return after(ack, tp.snd_una) || after(ack_seq, tp.snd_wl1) ||
                   (ack_seq == tp.snd_wl1 && (nwin > tp.snd_wnd || nwin == 0));
        }

        static int tcp_ack_update_window(tcp_sock tp, sk_buff skb, uint ack, uint ack_seq)
        {
            int flag = 0;
            uint nwin = tcp_hdr(skb).window;
            nwin <<= tp.rx_opt.snd_wscale;
            if (tcp_may_update_window(tp, ack, ack_seq, nwin))
            {
                flag |= FLAG_WIN_UPDATE;
                tcp_update_wl(tp, ack_seq);

                if (tp.snd_wnd != nwin)
                {
                    tp.snd_wnd = nwin;
                    tp.pred_flags = 0;
                    tcp_fast_path_check(tp);

                    if (!tcp_write_queue_empty(tp))
                    {
                        tcp_slow_start_after_idle_check(tp);
                    }

                    if (nwin > tp.max_window)
                    {
                        tp.max_window = nwin;
                        tcp_sync_mss(tp, tp.icsk_pmtu_cookie);
                    }

                    TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.snd_wnd, nwin);
                }
            }

            tcp_snd_una_update(tp, ack);
            return flag;
        }

        static bool tcp_ecn_rcv_ecn_echo(tcp_sock tp, tcphdr th)
        {
            if (th.ece > 0 && th.syn == 0 && BoolOk(tp.ecn_flags & TCP_ECN_OK))
            {
                return true;
            }
            return false;
        }

        static void tcp_count_delivered(tcp_sock tp, uint delivered, bool ece_ack)
        {
            tp.delivered += delivered;
            if (ece_ack)
            {
                tp.delivered_ce += delivered;
            }
        }

        static void tcp_ecn_accept_cwr(tcp_sock tp, sk_buff skb)
        {
            if (tcp_hdr(skb).cwr > 0)
            {
                tp.ecn_flags = (byte)(tp.ecn_flags & ~TCP_ECN_DEMAND_CWR);
                if (TCP_SKB_CB(skb).seq != TCP_SKB_CB(skb).end_seq)
                {
                    tp.icsk_ack.pending |= (byte)inet_csk_ack_state_t.ICSK_ACK_NOW;
                }
            }
        }

        static bool tcp_tsopt_ecr_before(tcp_sock tp, long when)
        {
            return tp.rx_opt.saw_tstamp && tp.rx_opt.rcv_tsecr > 0 && tp.rx_opt.rcv_tsecr <= when;
        }

        static bool tcp_skb_spurious_retrans(tcp_sock tp, sk_buff skb)
        {
            return BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_RETRANS) &&
                tcp_tsopt_ecr_before(tp, tcp_skb_timestamp(skb));
        }

        static uint tcp_tso_acked(tcp_sock tp, sk_buff skb)
        {
            tcp_trim_head(tp, skb, (int)(tp.snd_una - TCP_SKB_CB(skb).seq));
            return 0;
        }

        static void tcp_ack_tstamp(tcp_sock tp, sk_buff skb, sk_buff ack_skb, uint prior_snd_una)
        {
            if (!TCP_SKB_CB(skb).txstamp_ack)
            {
                return;
            }
            
            //这里主要是对一些不正确的序列号，发送时间戳报告
            if (!before(skb.tskey, prior_snd_una) && before(skb.tskey, tp.snd_una))
            {
                __skb_tstamp_tx(skb, ack_skb, tp, SCM_TSTAMP_ACK);
            }
        }

        static void tcp_mtup_probe_success(tcp_sock tp)
        {
            tp.prior_ssthresh = tcp_current_ssthresh(tp);
            ulong val = (ulong)tcp_snd_cwnd(tp) * tcp_mss_to_mtu(tp, tp.mss_cache);
            val /= tp.icsk_mtup.probe_size;

            tcp_snd_cwnd_set(tp, (uint)Math.Max(1, val));

            tp.snd_cwnd_cnt = 0;
            tp.snd_cwnd_stamp = tcp_jiffies32;
            tp.snd_ssthresh = tcp_current_ssthresh(tp);

            tp.icsk_mtup.search_low = (int)tp.icsk_mtup.probe_size;
            tp.icsk_mtup.probe_size = 0;
            tcp_sync_mss(tp, tp.icsk_pmtu_cookie);

            TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.MTUP_SUCCESS);
        }

        static bool tcp_limit_reno_sacked(tcp_sock tp)
        {
            uint holes = Math.Max(tp.lost_out, 1);
            holes = Math.Min(holes, tp.packets_out);
            if ((tp.sacked_out + holes) > tp.packets_out)
            {
                tp.sacked_out = tp.packets_out - holes;
                return true;
            }
            return false;
        }

        //用于在 TCP Reno 模式下，检测和处理报文的重排序情况，以避免误判丢包并触发不必要的重传
        static void tcp_check_reno_reordering(tcp_sock tp, int addend)
        {
            if (!tcp_limit_reno_sacked(tp))
            {
                return;
            }

            tp.reordering = (uint)Math.Min(tp.packets_out + addend, sock_net(tp).ipv4.sysctl_tcp_max_reordering);
            tp.reord_seen++;
        }

        static void tcp_remove_reno_sacks(tcp_sock tp, int acked, bool ece_ack)
        {
            if (acked > 0)
            {
                tcp_count_delivered(tp, (uint)Math.Max(acked - tp.sacked_out, 1), ece_ack);
                if (acked - 1 >= tp.sacked_out)
                {
                    tp.sacked_out = 0;
                }
                else
                {
                    tp.sacked_out -= (uint)acked - 1;
                }
            }

            tcp_check_reno_reordering(tp, acked);
        }

        static void tcp_check_sack_reordering(tcp_sock tp, uint low_seq)
        {
            uint mss = tp.mss_cache;
            uint fack, metric;

            fack = tcp_highest_sack_seq(tp);
            if (!before(low_seq, fack))
            {
                return;
            }

            metric = fack - low_seq;
            if ((metric > tp.reordering * mss) && mss > 0)
            {
                tp.reordering = (uint)Math.Min((metric + mss - 1) / mss, sock_net(tp).ipv4.sysctl_tcp_max_reordering);
            }

            tp.reord_seen++;
        }

        static int tcp_clean_rtx_queue(tcp_sock tp, sk_buff ack_skb, uint prior_fack, uint prior_snd_una, tcp_sacktag_state sack, bool ece_ack)
        {
            long first_ackt = 0, last_ackt = 0;
            uint prior_sacked = tp.sacked_out;
            uint reord = tp.snd_nxt;
            sk_buff skb, next;
            bool fully_acked = true;
            long sack_rtt_us = -1L;
            long seq_rtt_us = -1L;
            long ca_rtt_us = -1L;
            uint pkts_acked = 0;
            bool rtt_update;
            int flag = 0;

            //NetLog.Log("tp.tcp_rtx_queue Count: " + rb_count(tp.tcp_rtx_queue));

            first_ackt = 0;
            last_ackt = 0;
            for (skb = skb_rb_first(tp.tcp_rtx_queue); skb != null; skb = next)
            {
                tcp_skb_cb scb = TCP_SKB_CB(skb);
                uint start_seq = scb.seq;
                byte sacked = scb.sacked;
                uint acked_pcount;

                if (after(scb.end_seq, tp.snd_una))
                {
                    if (!after(tp.snd_una, scb.seq))
                    {
                        break;
                    }

                    acked_pcount = tcp_tso_acked(tp, skb);
                    if (acked_pcount == 0)
                    {
                        break;
                    }

                    fully_acked = false;
                }
                else
                {
                    acked_pcount = 1;
                }

                if (BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_RETRANS))
                {
                    if (BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS))
                    {
                        tp.retrans_out -= acked_pcount;
                    }
                    flag |= FLAG_RETRANS_DATA_ACKED;
                }
                else if (!BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
                {
                    last_ackt = tcp_skb_timestamp(skb);
                    if (first_ackt == 0)
                    {
                        first_ackt = last_ackt;
                    }

                    if (before(start_seq, reord))
                    {
                        reord = start_seq;
                    }
                    if (!after(scb.end_seq, tp.high_seq))
                    {
                        flag |= FLAG_ORIG_SACK_ACKED;
                    }
                }

                if (BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
                {
                    tp.sacked_out -= acked_pcount;
                }
                else if (tcp_is_sack(tp))
                {
                    tcp_count_delivered(tp, acked_pcount, ece_ack);
                    if (!tcp_skb_spurious_retrans(tp, skb))
                    {
                        tcp_rack_advance(tp, sacked, scb.end_seq, tcp_skb_timestamp(skb));
                    }
                }

                if (BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST))
                {
                    tp.lost_out -= acked_pcount;
                }
                tp.packets_out -= acked_pcount;
                pkts_acked += acked_pcount;
                tcp_rate_skb_delivered(tp, skb, sack.rate);

                flag |= FLAG_DATA_ACKED;
                if (!fully_acked)
                {
                    break;
                }

                tcp_ack_tstamp(tp, skb, ack_skb, prior_snd_una);
                next = skb_rb_next(skb);

                if (skb == tp.retransmit_skb_hint)
                {
                    tp.retransmit_skb_hint = null;
                }
                if (skb == tp.lost_skb_hint)
                {
                    tp.lost_skb_hint = null;
                }

                tcp_highest_sack_replace(tp, skb, next);
                tcp_rtx_queue_unlink_and_free(skb, tp);
            }

            if (skb == null)
            {
                tcp_chrono_stop(tp, tcp_chrono.TCP_CHRONO_BUSY);
            }

            if (between(tp.snd_up, prior_snd_una, tp.snd_una))
            {
                tp.snd_up = tp.snd_una;
            }

            if (skb != null)
            {
                tcp_ack_tstamp(tp, skb, ack_skb, prior_snd_una);
                if (BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
                {
                    flag |= FLAG_SACK_RENEGING;
                }
            }

            if (first_ackt > 0 && !BoolOk(flag & FLAG_RETRANS_DATA_ACKED))
            {
                seq_rtt_us = tcp_stamp_us_delta(tp.tcp_mstamp, first_ackt);
                ca_rtt_us = tcp_stamp_us_delta(tp.tcp_mstamp, last_ackt);

                if (pkts_acked == 1 && fully_acked && prior_sacked == 0 &&
                    (tp.snd_una - prior_snd_una) < tp.mss_cache &&
                    sack.rate.prior_delivered + 1 == tp.delivered &&
                    !BoolOk(flag & (FLAG_CA_ALERT | FLAG_SYN_ACKED)))
                {
                    flag |= FLAG_ACK_MAYBE_DELAYED;
                }
            }

            if (sack.first_sackt > 0)
            {
                sack_rtt_us = tcp_stamp_us_delta(tp.tcp_mstamp, sack.first_sackt);
                ca_rtt_us = tcp_stamp_us_delta(tp.tcp_mstamp, sack.last_sackt);
            }

            rtt_update = tcp_ack_update_rtt(tp, flag, seq_rtt_us, sack_rtt_us, ca_rtt_us, sack.rate);

            if (BoolOk(flag & FLAG_ACKED))
            {
                flag |= FLAG_SET_XMIT_TIMER;
                if (tp.icsk_mtup.probe_size > 0 && !after(tp.mtu_probe.probe_seq_end, tp.snd_una))
                {
                    tcp_mtup_probe_success(tp);
                }

                if (tcp_is_reno(tp))
                {
                    tcp_remove_reno_sacks(tp, (int)pkts_acked, ece_ack);
                    if (BoolOk(flag & FLAG_RETRANS_DATA_ACKED))
                    {
                        flag &= ~FLAG_ORIG_SACK_ACKED;
                    }
                }
                else
                {
                    int delta;
                    if (before(reord, prior_fack))
                    {
                        tcp_check_sack_reordering(tp, reord);
                    }
                    delta = (int)(prior_sacked - tp.sacked_out);
                    tp.lost_cnt_hint -= Math.Min(tp.lost_cnt_hint, delta);
                }
            }
            else if (skb != null && rtt_update && sack_rtt_us >= 0 &&
                    sack_rtt_us > tcp_stamp_us_delta(tp.tcp_mstamp, tcp_skb_timestamp(skb)))
            {
                flag |= FLAG_SET_XMIT_TIMER;
            }

            if (tp.icsk_ca_ops.pkts_acked != null)
            {
                ack_sample sample = new ack_sample { pkts_acked = pkts_acked, rtt_us = sack.rate.rtt_us };
                sample.in_flight = tp.mss_cache * (tp.delivered - sack.rate.prior_delivered);
                tp.icsk_ca_ops.pkts_acked(tp, sample);
            }

            return flag;
        }

        static void tcp_end_cwnd_reduction(tcp_sock tp)
        {
            if (tp.icsk_ca_ops.cong_control != null)
            {
                return;
            }

            if (tp.snd_ssthresh < TCP_INFINITE_SSTHRESH &&
                (tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_CWR || tp.undo_marker > 0))
            {
                tcp_snd_cwnd_set(tp, tp.snd_ssthresh);
                tp.snd_cwnd_stamp = tcp_jiffies32;
            }
            tcp_ca_event_func(tp, tcp_ca_event.CA_EVENT_COMPLETE_CWR);
        }

        static void tcp_try_keep_open(tcp_sock tp)
        {
            int state = (byte)tcp_ca_state.TCP_CA_Open;
            if (tcp_left_out(tp) > 0 || tcp_any_retrans_done(tp))
            {
                state = (byte)tcp_ca_state.TCP_CA_Disorder;
            }

            if (tp.icsk_ca_state != state)
            {
                tcp_set_ca_state(tp, (tcp_ca_state)state);
                tp.high_seq = tp.snd_nxt;
            }
        }

        //处理 Tail Loss Probe (TLP) 机制中的确认包（ACK）
        //Tail Loss Probe (TLP) 是 TCP 中的一种丢包检测和恢复机制，主要用于解决“尾部丢包”问题。
        //当发送方怀疑最后几个数据包可能丢失时，会发送一个探测包（TLP Probe），以触发接收方的确认（ACK）。
        static void tcp_process_tlp_ack(tcp_sock tp, uint ack, int flag)
        {
            if (before(ack, tp.tlp_high_seq))
            {
                return;
            }

            if (tp.tlp_retrans == 0)
            {
                tp.tlp_high_seq = 0;
            }
            else if (BoolOk(flag & FLAG_DSACK_TLP))
            {
                tp.tlp_high_seq = 0;
            }
            else if (after(ack, tp.tlp_high_seq))
            {
                tcp_init_cwnd_reduction(tp);
                tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_CWR);
                tcp_end_cwnd_reduction(tp);
                tcp_try_keep_open(tp);
            }
            else if (!BoolOk(flag & (FLAG_SND_UNA_ADVANCED | FLAG_NOT_DUP | FLAG_DATA_SACKED)))
            {
                tp.tlp_high_seq = 0;
            }
        }

        static uint tcp_dsack_seen(tcp_sock tp, uint start_seq, uint end_seq, tcp_sacktag_state state)
        {
            uint seq_len, dup_segs = 1;

            if (!before(start_seq, end_seq))
            {
                return 0;
            }

            seq_len = end_seq - start_seq;
            if (seq_len > tp.max_window)
            {
                return 0;
            }
            if (seq_len > tp.mss_cache)
            {
                dup_segs = (uint)DIV_ROUND_UP(seq_len, tp.mss_cache);
            }
            else if (tp.tlp_high_seq > 0 && tp.tlp_high_seq == end_seq)
            {
                state.flag |= FLAG_DSACK_TLP;
            }

            tp.dsack_dups += dup_segs;
            if (tp.dsack_dups > tp.total_retrans)
            {
                return 0;
            }

            tp.rx_opt.sack_ok |= (ushort)TCP_DSACK_SEEN;
            if (tp.reord_seen > 0 && !BoolOk(state.flag & FLAG_DSACK_TLP))
            {
                tp.rack.dsack_seen = 1;
            }
            state.flag |= FLAG_DSACKING_ACK;
            state.sack_delivered += dup_segs;
            return dup_segs;
        }

        static bool tcp_check_dsack(tcp_sock tp, sk_buff ack_skb, List<tcp_sack_block_wire> sp,
            int num_sacks, uint prior_snd_una, tcp_sacktag_state state)
        {
            uint start_seq_0 = sp[0].start_seq;
            uint end_seq_0 = sp[0].end_seq;
            uint dup_segs;

            if (before(start_seq_0, TCP_SKB_CB(ack_skb).ack_seq))
            {
                
            }
            else if (num_sacks > 1)
            {
                uint end_seq_1 = sp[1].end_seq;
                uint start_seq_1 = sp[1].start_seq;
                if (after(end_seq_0, end_seq_1) || before(start_seq_0, start_seq_1))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            dup_segs = tcp_dsack_seen(tp, start_seq_0, end_seq_0, state);
            if (dup_segs == 0)
            {
                return false;
            }

            if (tp.undo_marker > 0 && tp.undo_retrans > 0 &&
                !after(end_seq_0, prior_snd_una) &&
                after(end_seq_0, tp.undo_marker))
            {
                tp.undo_retrans = (int)Math.Max(0, tp.undo_retrans - dup_segs);
            }
            return true;
        }

        static bool tcp_is_sackblock_valid(tcp_sock tp, bool is_dsack, uint start_seq, uint end_seq)
        {
            if (after(end_seq, tp.snd_nxt) || !before(start_seq, end_seq))
            {
                return false;
            }

            if (!before(start_seq, tp.snd_nxt))
            {
                return false;
            }

            if (after(start_seq, tp.snd_una))
            {
                return true;
            }

            if (!is_dsack || tp.undo_marker == 0)
            {
                return false;
            }

            if (after(end_seq, tp.snd_una))
            {
                return false;
            }

            if (!before(start_seq, tp.undo_marker))
            {
                return true;
            }

            if (!after(end_seq, tp.undo_marker))
            {
                return false;
            }

            return !before(start_seq, end_seq - tp.max_window);
        }

        static bool tcp_sack_cache_ok(tcp_sock tp, int cacheIndex)
        {
            return cacheIndex < tp.recv_sack_cache.Length;
        }

        static sk_buff tcp_sacktag_bsearch(tcp_sock tp, uint seq)
        {
            rb_node parent, p = tp.tcp_rtx_queue.rb_node;
            sk_buff skb = null;

            while (p != null)
            {
                parent = p;
                skb = rb_to_skb(parent);
                if (before(seq, TCP_SKB_CB(skb).seq))
                {
                    p = parent.rb_left;
                    continue;
                }
                if (!before(seq, TCP_SKB_CB(skb).end_seq))
                {
                    p = parent.rb_right;
                    continue;
                }
                return skb;
            }

            return null;
        }

        static sk_buff tcp_sacktag_skip(sk_buff skb, tcp_sock tp, uint skip_to_seq)
        {
            if (skb != null && after(TCP_SKB_CB(skb).seq, skip_to_seq))
            {
                return skb;
            }
            return tcp_sacktag_bsearch(tp, skip_to_seq);
        }

        static bool tcp_match_skb_to_sack(tcp_sock tp, sk_buff skb, uint start_seq, uint end_seq)
        {
            bool in_sack = !after(start_seq, TCP_SKB_CB(skb).seq) && !before(end_seq, TCP_SKB_CB(skb).end_seq);
            return in_sack;
        }

        static int tcp_skb_seglen(sk_buff skb)
        {
            return skb.nBufferLength;
        }

        static byte tcp_sacktag_one(tcp_sock tp, tcp_sacktag_state state,
              byte sacked, uint start_seq, uint end_seq, bool dup_sack, int pcount, long xmit_time)
        {
            if (dup_sack && BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_RETRANS))
            {
                if (tp.undo_marker > 0 && tp.undo_retrans > 0 && after(end_seq, tp.undo_marker))
                {
                    tp.undo_retrans = Math.Max(0, tp.undo_retrans - pcount);
                }

                if (BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED) && before(start_seq, state.reord))
                {
                    state.reord = start_seq;
                }
            }

            if (!after(end_seq, tp.snd_una))
            {
                return sacked;
            }

            if (!BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
            {
                tcp_rack_advance(tp, sacked, end_seq, xmit_time);
                if (BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS))
                {
                    if (BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST))
                    {
                        sacked = (byte)(sacked & (~(byte)(tcp_skb_cb_sacked_flags.TCPCB_LOST | tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS)));
                        tp.lost_out -= (uint)pcount;
                        tp.retrans_out -= (uint)pcount;
                    }
                }
                else
                {
                    if (!BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_RETRANS))
                    {
                        if (before(start_seq, tcp_highest_sack_seq(tp)) && before(start_seq, state.reord))
                        {
                            state.reord = start_seq;
                        }

                        if (!after(end_seq, tp.high_seq))
                            state.flag |= FLAG_ORIG_SACK_ACKED;
                        if (state.first_sackt == 0)
                        {
                            state.first_sackt = xmit_time;
                        }
                        state.last_sackt = xmit_time;
                    }

                    if (BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST))
                    {
                        sacked = (byte)(sacked & (~(byte)tcp_skb_cb_sacked_flags.TCPCB_LOST));
                        tp.lost_out -= (uint)pcount;
                    }
                }

                sacked |= (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED;
                state.flag |= FLAG_DATA_SACKED;
                tp.sacked_out += (uint)pcount;
                state.sack_delivered += (uint)pcount;

                if (tp.lost_skb_hint != null && before(start_seq, TCP_SKB_CB(tp.lost_skb_hint).seq))
                {
                    tp.lost_cnt_hint += pcount;
                }
            }

            if (dup_sack && BoolOk(sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS))
            {
                sacked = (byte)(sacked & (~(byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS));
                tp.retrans_out -= (uint)pcount;
            }

            return sacked;
        }

        static bool tcp_shifted_skb(tcp_sock tp, sk_buff prev, sk_buff skb, tcp_sacktag_state state,
            uint pcount, int shifted, int mss, bool dup_sack)
        {
            uint start_seq = TCP_SKB_CB(skb).seq;
            uint end_seq = (uint)(start_seq + shifted);

            tcp_sacktag_one(tp, state, TCP_SKB_CB(skb).sacked, start_seq, end_seq, dup_sack, (int)pcount, tcp_skb_timestamp(skb));
            tcp_rate_skb_delivered(tp, skb, state.rate);

            if (skb == tp.lost_skb_hint)
            {
                tp.lost_cnt_hint += (int)pcount;
            }

            TCP_SKB_CB(prev).end_seq += (uint)shifted;
            TCP_SKB_CB(skb).seq += (uint)shifted;

            TCP_SKB_CB(prev).sacked |= (byte)(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_EVER_RETRANS);

            if (skb.nBufferLength > 0)
            {
                return false;
            }

            if (skb == tp.retransmit_skb_hint)
            {
                tp.retransmit_skb_hint = prev;
            }

            if (skb == tp.lost_skb_hint)
            {
                tp.lost_skb_hint = prev;
                tp.lost_cnt_hint--;
            }

            TCP_SKB_CB(prev).tcp_flags |= TCP_SKB_CB(skb).tcp_flags;
            if (BoolOk(TCP_SKB_CB(skb).tcp_flags & TCPHDR_FIN))
            {
                TCP_SKB_CB(prev).end_seq++;
            }

            if (skb == tcp_highest_sack(tp))
            {
                tcp_advance_highest_sack(tp, skb);
            }

            tcp_skb_collapse_tstamp(prev, skb);
            if (TCP_SKB_CB(prev).tx.delivered_mstamp > 0)
            {
                TCP_SKB_CB(prev).tx.delivered_mstamp = 0;
            }

            tcp_rtx_queue_unlink_and_free(skb, tp);
            return true;
        }

        //数据重组: 当 TCP 接收到乱序的数据包时，可能需要将数据重新排序并移动到正确的位置。
        //内存优化: 通过移动数据，可以合并相邻的小数据包，减少内存碎片，提高内存使用效率。
        //数据处理: 在处理 TCP 数据时，可能需要将数据从一个位置移动到另一个位置，以便进行进一步的处理
        static sk_buff tcp_shift_skb_data(tcp_sock tp, sk_buff skb, tcp_sacktag_state state, uint start_seq, uint end_seq, bool dup_sack)
        {
            sk_buff prev;
            int mss;
            int pcount = 0;
            int len;
            bool in_sack;

            if (!dup_sack && (TCP_SKB_CB(skb).sacked &
                (byte)(tcp_skb_cb_sacked_flags.TCPCB_LOST | tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS)) == (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_RETRANS)
            {
                goto fallback;
            }

            if (!after(TCP_SKB_CB(skb).end_seq, tp.snd_una))
            {
                goto fallback;
            }

            prev = skb_rb_prev(skb);
            if (prev == null)
            {
                goto fallback;
            }

            if ((TCP_SKB_CB(prev).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_TAGBITS) != (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED)
            {
                goto fallback;
            }

            in_sack = !after(start_seq, TCP_SKB_CB(skb).seq) && !before(end_seq, TCP_SKB_CB(skb).end_seq);

            if (in_sack)
            {
                len = skb.nBufferLength;
                pcount = 1;
                mss = tcp_skb_seglen(skb);

                if (mss != tcp_skb_seglen(prev))
                    goto fallback;
            }
            else
            {
                goto noop;
            }

            if (!after(TCP_SKB_CB(skb).seq + (uint)len, tp.snd_una))
            {
                goto fallback;
            }

            if (tcp_skb_shift(prev, skb, pcount, len) == 0)
            {
                goto fallback;
            }

            if (!tcp_shifted_skb(tp, prev, skb, state, (uint)pcount, len, mss, dup_sack))
            {
                goto label_out;
            }

            skb = skb_rb_next(prev);
            if (skb == null)
            {
                goto label_out;
            }

            if (!skb_can_shift(skb) || ((TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_TAGBITS) != (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED) || (mss != tcp_skb_seglen(skb)))
            {
                goto label_out;
            }

            len = skb.nBufferLength;
            pcount = 1;

            if (tcp_skb_shift(prev, skb, pcount, len) > 0)
            {
                tcp_shifted_skb(tp, prev, skb, state, (uint)pcount, len, mss, false);
            }
        label_out:
            return prev;
        noop:
            return skb;
        fallback:
            return null;
        }

        static sk_buff tcp_sacktag_walk(sk_buff skb, tcp_sock tp, tcp_sack_block next_dup, tcp_sacktag_state state,
                    uint start_seq, uint end_seq, bool dup_sack_in)
        {
            sk_buff tmp = null;
            for (; skb != null; skb = skb_rb_next(skb))
            {
                bool in_sack = false;
                bool dup_sack = dup_sack_in;

                if (!before(TCP_SKB_CB(skb).seq, end_seq))
                {
                    break;
                }

                if (next_dup != null && before(TCP_SKB_CB(skb).seq, next_dup.end_seq))
                {
                    in_sack = tcp_match_skb_to_sack(tp, skb, next_dup.start_seq, next_dup.end_seq);
                    if (in_sack)
                    {
                        dup_sack = true;
                    }
                }

                if (!in_sack)
                {
                    tmp = tcp_shift_skb_data(tp, skb, state, start_seq, end_seq, dup_sack);
                    if (tmp != null)
                    {
                        if (tmp != skb)
                        {
                            skb = tmp;
                            continue;
                        }

                        in_sack = false;
                    }
                    else
                    {
                        in_sack = tcp_match_skb_to_sack(
                            tp, skb, start_seq, end_seq);
                    }
                }

                if (in_sack)
                {
                    break;
                }

                if (in_sack)
                {
                    TCP_SKB_CB(skb).sacked = tcp_sacktag_one(
                        tp, state, TCP_SKB_CB(skb).sacked,
                        TCP_SKB_CB(skb).seq, TCP_SKB_CB(skb).end_seq,
                        dup_sack, 1,
                        tcp_skb_timestamp(skb));

                    tcp_rate_skb_delivered(tp, skb, state.rate);
                    if (BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
                    {
                        list_del_init(skb.tcp_tsorted_anchor);
                    }

                    if (!before(TCP_SKB_CB(skb).seq, tcp_highest_sack_seq(tp)))
                    {
                        tcp_advance_highest_sack(tp, skb);
                    }
                }
            }
            return skb;
        }

        static sk_buff tcp_maybe_skipping_dsack(sk_buff skb, tcp_sock tp, tcp_sack_block next_dup,
                        tcp_sacktag_state state, uint skip_to_seq)
        {
            if (next_dup == null)
            {
                return skb;
            }

            if (before(next_dup.start_seq, skip_to_seq))
            {
                skb = tcp_sacktag_skip(skb, tp, next_dup.start_seq);
                skb = tcp_sacktag_walk(skb, tp, null, state, next_dup.start_seq, next_dup.end_seq, true);
            }

            return skb;
        }

        //tcp_sacktag_write_queue 是 Linux 内核 TCP 协议栈中的一个函数，
        //用于处理接收到的选择性确认（SACK, Selective Acknowledgment）信息，并将其应用到发送方的重传队列中。
        //这个函数的主要职责是更新那些已经发送但尚未被确认的数据包的状态，以便更精确地管理哪些数据包需要重传以及如何优化未来的发送行为。
        static int tcp_sacktag_write_queue(tcp_sock tp, sk_buff ack_skb, uint prior_snd_una, tcp_sacktag_state state)
        {
            NetLog.Assert(ack_skb.nBufferOffset == 0);
            List<tcp_sack_block_wire> sp_wire = tp.sp_wire_cache;
            NetLog.Assert(sp_wire.Count == 0);
            get_sp_wire(ack_skb, tp.sp_wire_cache);

            TcpMibMgr.NET_ADD_AVERAGE_STATS(sock_net(tp), TCPMIB.receive_sack_count, sp_wire.Count);
            List<tcp_sack_block> sp = tp.sp_cache;
            
            int cacheIndex;
            tcp_sack_block cache = null;
            sk_buff skb = null;

            int num_sacks = Math.Min(TCP_NUM_SACKS, sp_wire.Count);
            int used_sacks = 0;
            bool found_dup_sack = false;
            int i, j;
            int first_sack_index;

            state.flag = 0;
            state.reord = tp.snd_nxt;

            if (tp.sacked_out == 0)
            {
                tcp_highest_sack_reset(tp);
            }

            found_dup_sack = tcp_check_dsack(tp, ack_skb, sp_wire, num_sacks, prior_snd_una, state);
            if(found_dup_sack)
            {
                TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.receive_dsack_count);
            }

            if (before(TCP_SKB_CB(ack_skb).ack_seq, prior_snd_una - tp.max_window))
            {
                return 0;
            }

            if (tp.packets_out == 0)
            {
                goto label_out;
            }

            first_sack_index = 0;
            for (i = 0; i < num_sacks; i++)
            {
                bool dup_sack = i == 0 && found_dup_sack;

                sp[used_sacks] = tp.m_tcp_sack_block_pool.Pop();
                sp[used_sacks].start_seq = sp_wire[i].start_seq;
                sp[used_sacks].end_seq = sp_wire[i].end_seq;

                if (!tcp_is_sackblock_valid(tp, dup_sack, sp[used_sacks].start_seq, sp[used_sacks].end_seq))
                {
                    if(!dup_sack)
                    {
                        if ((TCP_SKB_CB(ack_skb).ack_seq != tp.snd_una) && !after(sp[used_sacks].end_seq, tp.snd_una))
                        {
                            continue;
                        }
                    }

                    if (i == 0)
                    {
                        first_sack_index = -1;
                    }
                    continue;
                }

                if (!after(sp[used_sacks].end_seq, prior_snd_una))
                {
                    if (i == 0)
                    {
                        first_sack_index = -1;
                    }
                    continue;
                }

                used_sacks++;
            }

            for (i = used_sacks - 1; i > 0; i--)
            {
                for (j = 0; j < i; j++)
                {
                    if (after(sp[j].start_seq, sp[j + 1].start_seq))
                    {
                        var temp = sp[j];
                        sp[j] = sp[j + 1];
                        sp[j + 1] = temp;

                        if (j == first_sack_index)
                        {
                            first_sack_index = j + 1;
                        }
                    }
                }
            }

            state.mss_now = tcp_current_mss(tp);
            i = 0;

            if (tp.sacked_out == 0)
            {
                cacheIndex = tp.recv_sack_cache.Length;
                cache = null;
            }
            else
            {
                cacheIndex = 0;
                cache = tp.recv_sack_cache[cacheIndex];
                while (tcp_sack_cache_ok(tp, cacheIndex) && cache.start_seq == 0 && cache.end_seq == 0)
                {
                    cacheIndex++;
                    cache = tp.recv_sack_cache[cacheIndex];
                }
            }

            while (i < used_sacks)
            {
                uint start_seq = sp[i].start_seq;
                uint end_seq = sp[i].end_seq;
                bool dup_sack = (found_dup_sack && (i == first_sack_index));

                tcp_sack_block next_dup = null;

                if (found_dup_sack && ((i + 1) == first_sack_index))
                {
                    next_dup = sp[i + 1];
                }

                while (tcp_sack_cache_ok(tp, cacheIndex) && !before(start_seq, cache.end_seq))
                {
                    cacheIndex++;
                    cache = tp.recv_sack_cache[cacheIndex];
                }

                if (tcp_sack_cache_ok(tp, cacheIndex) && !dup_sack && after(end_seq, cache.start_seq))
                {
                    if (before(start_seq, cache.start_seq))
                    {
                        skb = tcp_sacktag_skip(skb, tp, start_seq);
                        skb = tcp_sacktag_walk(skb, tp, next_dup, state, start_seq, cache.start_seq, dup_sack);
                    }

                    if (!after(end_seq, cache.end_seq))
                    {
                        goto advance_sp;
                    }

                    skb = tcp_maybe_skipping_dsack(skb, tp, next_dup, state, cache.end_seq);
                    if (tcp_highest_sack_seq(tp) == cache.end_seq)
                    {
                        skb = tcp_highest_sack(tp);
                        if (skb == null)
                        {
                            break;
                        }
                        cacheIndex++;
                        cache = tp.recv_sack_cache[cacheIndex];
                        goto walk;
                    }

                    skb = tcp_sacktag_skip(skb, tp, cache.end_seq);

                    cacheIndex++;
                    cache = tp.recv_sack_cache[cacheIndex];
                    continue;
                }

                if (!before(start_seq, tcp_highest_sack_seq(tp)))
                {
                    skb = tcp_highest_sack(tp);
                    if (skb == null)
                    {
                        break;
                    }
                }
                skb = tcp_sacktag_skip(skb, tp, start_seq);

            walk:
                skb = tcp_sacktag_walk(skb, tp, next_dup, state, start_seq, end_seq, dup_sack);
            advance_sp:
                i++;
            }

            for (i = 0; i < tp.recv_sack_cache.Length - used_sacks; i++)
            {
                tp.recv_sack_cache[i].start_seq = 0;
                tp.recv_sack_cache[i].end_seq = 0;
            }

            for (j = 0; j < used_sacks; j++)
            {
                tp.recv_sack_cache[i++] = sp[j];
            }

            if (tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Loss || tp.undo_marker > 0)
            {
                tcp_check_sack_reordering(tp, state.reord);
            }
        label_out:
            return state.flag;
        }
        
        //一个 ACK 被认为是“可疑的”，如果满足以下任一条件：
        //冗余 ACK：!(flag & FLAG_NOT_DUP)，即 ACK 没有携带数据、窗口更新或确认新数据。
        //被标记为 CA_ALERT：(flag & FLAG_CA_ALERT)，即 ACK 携带了 SACK 或 ECN Echo 标志。
        //非 Open 拥塞状态：inet_csk(sk)->icsk_ca_state != TCP_CA_Open，即当前 TCP 连接不在开放拥塞状态（TCP_CA_Open）。
        static bool tcp_ack_is_dubious(tcp_sock tp, int flag)
        {
            return !BoolOk(flag & FLAG_NOT_DUP) ||
                BoolOk(flag & FLAG_CA_ALERT) ||
                tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Open;
        }

        static bool tcp_force_fast_retransmit(tcp_sock tp)
        {
            return after(tcp_highest_sack_seq(tp), tp.snd_una + tp.reordering * tp.mss_cache);
        }

        static bool tcp_check_sack_reneging(tcp_sock tp, ref int ack_flag)
        {
            if (BoolOk(ack_flag & FLAG_SACK_RENEGING) && BoolOk(ack_flag & FLAG_SND_UNA_ADVANCED))
            {
                long delay = Math.Max(tp.srtt_us >> 4, 10);
                inet_csk_reset_xmit_timer(tp, ICSK_TIME_RETRANS, delay, TCP_RTO_MAX);
                ack_flag &= ~FLAG_SET_XMIT_TIMER;
                return true;
            }
            return false;
        }

        static bool tcp_packet_delayed(tcp_sock tp)
        {
            if (tp.retrans_stamp > 0 && tcp_tsopt_ecr_before(tp, tp.retrans_stamp))
            {
                return true;
            }

            if (tp.retrans_stamp == 0 && tp.sk_state != TCP_SYN_SENT)
            {
                return true;
            }
            return false;
        }

        static bool tcp_may_undo(tcp_sock tp)
        {
            return tp.undo_marker > 0 && (tp.undo_retrans == 0 || tcp_packet_delayed(tp));
        }

        static void tcp_ecn_withdraw_cwr(tcp_sock tp)
        {
            tp.ecn_flags = (byte)(tp.ecn_flags & (~TCP_ECN_QUEUE_CWR));
        }

        static void tcp_undo_cwnd_reduction(tcp_sock tp, bool unmark_loss)
        {
            if (unmark_loss)
            {
                sk_buff skb;

                for (skb = skb_rb_first(tp.tcp_rtx_queue); skb != null; skb = skb_rb_next(skb))
                {
                    TCP_SKB_CB(skb).sacked = (byte)(TCP_SKB_CB(skb).sacked & (~(byte)tcp_skb_cb_sacked_flags.TCPCB_LOST));
                }
                tp.lost_out = 0;
                tcp_clear_all_retrans_hints(tp);
            }

            if (tp.prior_ssthresh > 0)
            {
                tcp_snd_cwnd_set(tp, tp.icsk_ca_ops.undo_cwnd(tp));

                if (tp.prior_ssthresh > tp.snd_ssthresh)
                {
                    tp.snd_ssthresh = tp.prior_ssthresh;
                    tcp_ecn_withdraw_cwr(tp);
                }
            }

            tp.snd_cwnd_stamp = tcp_jiffies32;
            tp.undo_marker = 0;
            tp.rack.advanced = 1;
        }

        static bool tcp_is_non_sack_preventing_reopen(tcp_sock tp)
        {
            if (tp.snd_una == tp.high_seq && tcp_is_reno(tp))
            {
                if (!tcp_any_retrans_done(tp))
                {
                    tp.retrans_stamp = 0;
                }
                return true;
            }
            return false;
        }

        static bool tcp_try_undo_recovery(tcp_sock tp)
        {
            if (tcp_may_undo(tp))
            {
                tcp_undo_cwnd_reduction(tp, false);
            }
            else if (tp.rack.reo_wnd_persist > 0)
            {
                tp.rack.reo_wnd_persist--;
            }
            if (tcp_is_non_sack_preventing_reopen(tp))
            {
                return true;
            }
            tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_Open);
            tp.is_sack_reneg = false;
            return false;
        }

        static void tcp_add_reno_sack(tcp_sock tp, int num_dupack, bool ece_ack)
        {
            if (num_dupack > 0)
            {
                uint prior_sacked = tp.sacked_out;
                tp.sacked_out += (uint)num_dupack;
                tcp_check_reno_reordering(tp, 0);
                int delivered = (int)(tp.sacked_out - prior_sacked);
                if (delivered > 0)
                {
                    tcp_count_delivered(tp, (uint)delivered, ece_ack);
                }
            }
        }

        static bool tcp_try_undo_partial(tcp_sock tp, uint prior_snd_una, ref bool do_lost)
        {
            if (tp.undo_marker > 0 && tcp_packet_delayed(tp))
            {
                tcp_check_sack_reordering(tp, prior_snd_una);
                if (tp.retrans_out > 0)
                {
                    return true;
                }

                if (!tcp_any_retrans_done(tp))
                {
                    tp.retrans_stamp = 0;
                }

                tcp_undo_cwnd_reduction(tp, true);
                tcp_try_keep_open(tp);
            }
            else
            {
                do_lost = tcp_force_fast_retransmit(tp);
            }
            return false;
        }

        static bool tcp_try_undo_dsack(tcp_sock tp)
        {
            if (tp.undo_marker > 0 && tp.undo_retrans == 0)
            {
                tp.rack.reo_wnd_persist = (byte)Math.Min(TCP_RACK_RECOVERY_THRESH, tp.rack.reo_wnd_persist + 1);
                tcp_undo_cwnd_reduction(tp, false);
                return true;
            }
            return false;
        }

        static void tcp_try_to_open(tcp_sock tp, int flag)
        {
            if (!tcp_any_retrans_done(tp))
            {
                tp.retrans_stamp = 0;
            }

            if (BoolOk(flag & FLAG_ECE))
            {
                tcp_enter_cwr(tp);
            }

            if (tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_CWR)
            {
                tcp_try_keep_open(tp);
            }
        }

        static void tcp_identify_packet_loss(tcp_sock tp, ref int ack_flag)
        {
            if (tcp_rtx_queue_empty(tp))
            {
                return;
            }

            if (tcp_is_reno(tp))
            {
                tcp_newreno_mark_lost(tp, BoolOk(ack_flag & FLAG_SND_UNA_ADVANCED));
            }
            else if (tcp_is_rack(tp))
            {
                uint prior_retrans = tp.retrans_out;

                if (tcp_rack_mark_lost(tp))
                {
                    ack_flag &= ~FLAG_SET_XMIT_TIMER;
                }
                if (prior_retrans > tp.retrans_out)
                {
                    ack_flag |= FLAG_LOST_RETRANS;
                }
            }
        }

        static int tcp_dupack_heuristics(tcp_sock tp)
        {
            return (int)tp.sacked_out + 1;
        }

        static bool tcp_time_to_recover(tcp_sock tp, int flag)
        {
            if (tp.lost_out > 0)
            {
                return true;
            }

            if (!tcp_is_rack(tp) && tcp_dupack_heuristics(tp) > tp.reordering)
            {
                return true;
            }

            return false;
        }

        static bool tcp_try_undo_loss(tcp_sock tp, bool frto_undo)
        {
            if (frto_undo || tcp_may_undo(tp))
            {
                tcp_undo_cwnd_reduction(tp, true);
                tp.icsk_retransmits = 0;
                if (tcp_is_non_sack_preventing_reopen(tp))
                {
                    return true;
                }

                if (frto_undo || tcp_is_sack(tp))
                {
                    tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_Open);
                    tp.is_sack_reneg = false;
                }
                return true;
            }
            return false;
        }

        static void tcp_process_loss(tcp_sock tp, int flag, int num_dupack, ref int rexmit)
        {
            bool recovered = !before(tp.snd_una, tp.high_seq);
            if ((BoolOk(flag & FLAG_SND_UNA_ADVANCED)) && tcp_try_undo_loss(tp, false))
            {
                return;
            }

            if (tp.frto)
            {
                if (BoolOk(flag & FLAG_ORIG_SACK_ACKED) && tcp_try_undo_loss(tp, true))
                {
                    return;
                }

                if (after(tp.snd_nxt, tp.high_seq))
                {
                    if (BoolOk(flag & FLAG_DATA_SACKED) || num_dupack > 0)
                    {
                        tp.frto = false;
                    }
                }
                else if (BoolOk(flag & FLAG_SND_UNA_ADVANCED) && !recovered)
                {
                    tp.high_seq = tp.snd_nxt;
                    if (!tcp_write_queue_empty(tp) && after(tcp_wnd_end(tp), tp.snd_nxt))
                    {
                        rexmit = REXMIT_NEW;
                        return;
                    }
                    tp.frto = false;
                }
            }

            if (recovered)
            {
                tcp_try_undo_recovery(tp);
                return;
            }

            if (tcp_is_reno(tp))
            {
                if (after(tp.snd_nxt, tp.high_seq) && num_dupack > 0)
                {
                    tcp_add_reno_sack(tp, num_dupack, BoolOk(flag & FLAG_ECE));
                }
                else if (BoolOk(flag & FLAG_SND_UNA_ADVANCED))
                {
                    tcp_reset_reno_sack(tp);
                }
            }

            rexmit = REXMIT_LOST;
        }

        static void tcp_update_rto_time(tcp_sock tp)
        {
            if (tp.rto_stamp > 0)
            {
                tp.total_rto_time += tcp_time_stamp_ms(tp) - tp.rto_stamp;
                tp.rto_stamp = 0;
            }
        }

        static void tcp_mtup_probe_failed(tcp_sock tp)
        {
            tp.icsk_mtup.search_high = (int)tp.icsk_mtup.probe_size - 1;
            tp.icsk_mtup.probe_size = 0;
            TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.MTUP_FAIL);
        }

        static void tcp_non_congestion_loss_retransmit(tcp_sock tp)
        {
            if (tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Loss)
            {
                tp.high_seq = tp.snd_nxt;
                tp.snd_ssthresh = tcp_current_ssthresh(tp);
                tp.prior_ssthresh = 0;
                tp.undo_marker = 0;
                tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_Loss);
            }
            tcp_xmit_retransmit_queue(tp);
        }

        static void tcp_simple_retransmit(tcp_sock tp)
        {
            sk_buff skb;
            uint mss = tcp_current_mss(tp);

            for (skb = skb_rb_first(tp.tcp_rtx_queue); skb != null; skb = skb_rb_next(skb))
            {
                if (tcp_skb_seglen(skb) > mss)
                {
                    tcp_mark_skb_lost(tp, skb);
                }
            }

            tcp_clear_retrans_hints_partial(tp);

            if (tp.lost_out == 0)
            {
                return;
            }

            if (tcp_is_reno(tp))
            {
                tcp_limit_reno_sacked(tp);
            }

            tcp_non_congestion_loss_retransmit(tp);
        }

        static void tcp_mark_head_lost(tcp_sock tp, int packets, int mark_head)
        {
            sk_buff skb;
            int cnt = 0;
            uint loss_high = tp.snd_nxt;
            skb = tp.lost_skb_hint;

            if (skb != null)
            {
                if (mark_head > 0 && after(TCP_SKB_CB(skb).seq, tp.snd_una))
                {
                    return;
                }
                cnt = tp.lost_cnt_hint;
            }
            else
            {
                skb = tcp_rtx_queue_head(tp);
                cnt = 0;
            }

            for (; skb != null; skb = skb_rb_next(skb))
            {
                tp.lost_skb_hint = skb;
                tp.lost_cnt_hint = cnt;

                if (after(TCP_SKB_CB(skb).end_seq, loss_high))
                {
                    break;
                }

                if (BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED))
                {
                    cnt++;
                }

                if (cnt > packets)
                {
                    break;
                }

                if (!BoolOk(TCP_SKB_CB(skb).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_LOST))
                {
                    tcp_mark_skb_lost(tp, skb);
                }

                if (mark_head > 0)
                {
                    break;
                }
            }
        }

        static void tcp_update_scoreboard(tcp_sock tp, int fast_rexmit)
        {
            if (tcp_is_sack(tp))
            {
                int sacked_upto = (int)(tp.sacked_out - tp.reordering);
                if (sacked_upto >= 0)
                {
                    tcp_mark_head_lost(tp, sacked_upto, 0);
                }
                else if (fast_rexmit > 0)
                {
                    tcp_mark_head_lost(tp, 1, 1);
                }
            }
        }

        static void tcp_fastretrans_alert(tcp_sock tp, uint prior_snd_una, int num_dupack, ref int ack_flag, ref int rexmit)
        {
            int fast_rexmit = 0, flag = ack_flag;
            bool ece_ack = BoolOk(flag & FLAG_ECE);
            bool do_lost = num_dupack > 0 || (BoolOk(flag & FLAG_DATA_SACKED) && tcp_force_fast_retransmit(tp));

            if (tp.packets_out == 0 && tp.sacked_out > 0)
            {
                tp.sacked_out = 0;
            }

            if (ece_ack)
            {
                tp.prior_ssthresh = 0;
            }

            if (tcp_check_sack_reneging(tp, ref ack_flag))
            {
                return;
            }

            if (tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Open)
            {
                tp.retrans_stamp = 0;
            }
            else if (!before(tp.snd_una, tp.high_seq))
            {
                switch (tp.icsk_ca_state)
                {
                    case (byte)tcp_ca_state.TCP_CA_CWR:
                        if (tp.snd_una != tp.high_seq)
                        {
                            tcp_end_cwnd_reduction(tp);
                            tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_Open);
                        }
                        break;
                    case (byte)tcp_ca_state.TCP_CA_Recovery:
                        if (tcp_is_reno(tp))
                        {
                            tcp_reset_reno_sack(tp);
                        }
                        if (tcp_try_undo_recovery(tp))
                        {
                            return;
                        }
                        tcp_end_cwnd_reduction(tp);
                        break;
                }
            }

            switch (tp.icsk_ca_state)
            {
                case (byte)tcp_ca_state.TCP_CA_Recovery:
                    if (!BoolOk(flag & FLAG_SND_UNA_ADVANCED))
                    {
                        if (tcp_is_reno(tp))
                        {
                            tcp_add_reno_sack(tp, num_dupack, ece_ack);
                        }
                    }
                    else if (tcp_try_undo_partial(tp, prior_snd_una, ref do_lost))
                    {
                        return;
                    }

                    if (tcp_try_undo_dsack(tp))
                    {
                        tcp_try_to_open(tp, flag);
                    }

                    tcp_identify_packet_loss(tp, ref ack_flag);
                    if (tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Recovery)
                    {
                        if (!tcp_time_to_recover(tp, flag))
                        {
                            return;
                        }

                        tcp_enter_recovery(tp, ece_ack);
                    }
                    break;
                case (byte)tcp_ca_state.TCP_CA_Loss:
                    tcp_process_loss(tp, flag, num_dupack, ref rexmit);
                    if (tp.icsk_ca_state != (byte)tcp_ca_state.TCP_CA_Loss)
                    {
                        tcp_update_rto_time(tp);
                    }

                    tcp_identify_packet_loss(tp, ref ack_flag);
                    if (!(tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Open || BoolOk(ack_flag & FLAG_LOST_RETRANS)))
                    {
                        return;
                    }
                    goto default;
                default:
                    if (tcp_is_reno(tp))
                    {
                        if (BoolOk(flag & FLAG_SND_UNA_ADVANCED))
                        {
                            tcp_reset_reno_sack(tp);
                        }
                        tcp_add_reno_sack(tp, num_dupack, ece_ack);
                    }

                    if (tp.icsk_ca_state <= (byte)tcp_ca_state.TCP_CA_Disorder)
                    {
                        tcp_try_undo_dsack(tp);
                    }

                    tcp_identify_packet_loss(tp, ref ack_flag);
                    if (!tcp_time_to_recover(tp, flag))
                    {
                        tcp_try_to_open(tp, flag);
                        return;
                    }

                    if (tp.icsk_ca_state < (byte)tcp_ca_state.TCP_CA_CWR && tp.icsk_mtup.probe_size > 0 &&
                        tp.snd_una == tp.mtu_probe.probe_seq_start)
                    {
                        tcp_mtup_probe_failed(tp);
                        tcp_snd_cwnd_set(tp, tcp_snd_cwnd(tp) + 1);
                        tcp_simple_retransmit(tp);
                        return;
                    }

                    tcp_enter_recovery(tp, ece_ack);
                    fast_rexmit = 1;
                    break;
            }

            if (!tcp_is_rack(tp) && do_lost)
            {
                tcp_update_scoreboard(tp, fast_rexmit);
            }
            rexmit = REXMIT_LOST;
        }

        static void tcp_set_xmit_timer(tcp_sock tp)
        {
            if (!tcp_schedule_loss_probe(tp, true))
            {
                tcp_rearm_rto(tp);
            }
        }

        static uint tcp_newly_delivered(tcp_sock tp, uint prior_delivered, int flag)
        {
            net net = sock_net(tp);
            uint delivered = tp.delivered - prior_delivered;
            return delivered;
        }

        static bool tcp_may_raise_cwnd(tcp_sock tp, int flag)
        {
            if (tp.reordering > sock_net(tp).ipv4.sysctl_tcp_reordering)
            {
                return BoolOk(flag & FLAG_FORWARD_PROGRESS);
            }
            return BoolOk(flag & FLAG_DATA_ACKED);
        }

        static void tcp_cong_avoid(tcp_sock tp, uint ack, uint acked)
        {
            tp.icsk_ca_ops.cong_avoid(tp, ack, acked);
            tp.snd_cwnd_stamp = tcp_jiffies32;
        }

        static void tcp_cong_control(tcp_sock tp, uint ack, uint acked_sacked, int flag, rate_sample rs)
        {
            if (tp.icsk_ca_ops.cong_control != null)
            {
                tp.icsk_ca_ops.cong_control(tp, ack, flag, rs);
                return;
            }

            if (tcp_in_cwnd_reduction(tp))
            {
                tcp_cwnd_reduction(tp, (int)acked_sacked, rs.losses, flag);
            }
            else if (tcp_may_raise_cwnd(tp, flag))
            {
                tcp_cong_avoid(tp, ack, acked_sacked);
            }
            tcp_update_pacing_rate(tp);
        }

        static void tcp_xmit_recovery(tcp_sock tp, int rexmit)
        {
            if (rexmit == REXMIT_NONE || tp.sk_state == TCP_SYN_SENT)
            {
                return;
            }

            if (rexmit == REXMIT_NEW)
            {
                __tcp_push_pending_frames(tp, tcp_current_mss(tp), TCP_NAGLE_OFF);
                if (after(tp.snd_nxt, tp.high_seq))
                {
                    return;
                }
                tp.frto = false;
            }
            tcp_xmit_retransmit_queue(tp);
        }

        static void tcp_ack_probe(tcp_sock tp)
        {
            sk_buff head = tcp_send_head(tp);
            if (head == null)
            {
                return;
            }

            if (!after(TCP_SKB_CB(head).end_seq, tcp_wnd_end(tp)))
            {
                tp.icsk_backoff = 0;
                tp.icsk_probes_tstamp = 0;
                inet_csk_clear_xmit_timer(tp, ICSK_TIME_PROBE0);
            }
            else
            {
                long when = tcp_probe0_when(tp, TCP_RTO_MAX);
                when = tcp_clamp_probe0_to_user_timeout(tp, when);
                tcp_reset_xmit_timer(tp, ICSK_TIME_PROBE0, when, TCP_RTO_MAX);
            }
        }

        //返回值： 
        //负数: 有错误
        static int tcp_ack(tcp_sock tp, sk_buff skb, int flag)
        {
            tcp_sacktag_state sack_state = tp.tcp_sacktag_state_cache;
            sack_state.Reset();
            rate_sample rs = sack_state.rate;

            uint prior_snd_una = tp.snd_una;
            bool is_sack_reneg = tp.is_sack_reneg;
            uint ack_seq = TCP_SKB_CB(skb).seq;
            uint ack = TCP_SKB_CB(skb).ack_seq;
            int num_dupack = 0;
            int prior_packets = (int)tp.packets_out;
            uint delivered = tp.delivered;
            uint lost = tp.lost;
            int rexmit = REXMIT_NONE;
            uint prior_fack;

            //NetLog.Log("tcp_ack: " + ack_seq + " | " + ack);
            if (before(ack, prior_snd_una)) //我收到了一个老的ACK
            {
                uint max_window = (uint)Math.Min(tp.max_window, tp.bytes_acked);
                if (before(ack, prior_snd_una - max_window)) //我收到了一个，太老的ACK
                {
                    if (!BoolOk(flag & FLAG_NO_CHALLENGE_ACK))
                    {
                        tcp_send_challenge_ack(tp);
                    }
                    return -skb_drop_reason.SKB_DROP_REASON_TCP_TOO_OLD_ACK;
                }
                goto old_ack;
            }

            if (after(ack, tp.snd_nxt)) //这个数据还没发送，竟然收到了ACK确认
            {
                return -skb_drop_reason.SKB_DROP_REASON_TCP_ACK_UNSENT_DATA;
            }

            if (after(ack, prior_snd_una))
            {
                flag |= FLAG_SND_UNA_ADVANCED;
                tp.icsk_retransmits = 0;
            }

            prior_fack = tcp_is_sack(tp) ? tcp_highest_sack_seq(tp) : tp.snd_una;
            rs.prior_in_flight = tcp_packets_in_flight(tp);

            if (BoolOk(flag & FLAG_UPDATE_TS_RECENT))
            {
                tcp_replace_ts_recent(tp, TCP_SKB_CB(skb).seq);
            }

            if ((flag & (FLAG_SLOWPATH | FLAG_SND_UNA_ADVANCED)) == FLAG_SND_UNA_ADVANCED)
            {
                tcp_update_wl(tp, ack_seq);
                tcp_snd_una_update(tp, ack);
                flag |= FLAG_WIN_UPDATE;
                tcp_in_ack_event(tp, (uint)tcp_ca_ack_event_flags.CA_ACK_WIN_UPDATE);

                TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.FLAG_SND_UNA_ADVANCED);
            }
            else
            {
                uint ack_ev_flags = (uint)tcp_ca_ack_event_flags.CA_ACK_SLOWPATH;
                if (ack_seq != TCP_SKB_CB(skb).end_seq)
                {
                    flag |= FLAG_DATA;
                }

                flag |= tcp_ack_update_window(tp, skb, ack, ack_seq);
                if (TCP_SKB_CB(skb).sacked > 0)
                {
                    flag |= tcp_sacktag_write_queue(tp, skb, prior_snd_una, sack_state);
                }

                if (tcp_ecn_rcv_ecn_echo(tp, tcp_hdr(skb)))
                {
                    flag |= FLAG_ECE;
                    ack_ev_flags |= (uint)tcp_ca_ack_event_flags.CA_ACK_ECE;
                }

                if (sack_state.sack_delivered > 0)
                {
                    tcp_count_delivered(tp, sack_state.sack_delivered, BoolOk(flag & FLAG_ECE));
                }

                if (BoolOk(flag & FLAG_WIN_UPDATE))
                {
                    ack_ev_flags |= (uint)tcp_ca_ack_event_flags.CA_ACK_WIN_UPDATE;
                }
                tcp_in_ack_event(tp, ack_ev_flags);
            }

            tcp_ecn_accept_cwr(tp, skb);
            tp.sk_err_soft = 0;
            tp.icsk_probes_out = 0;
            tp.rcv_tstamp = tcp_jiffies32;
            if (prior_packets == 0)
            {
                goto no_queue;
            }

            flag |= tcp_clean_rtx_queue(tp, skb, prior_fack, prior_snd_una, sack_state, BoolOk(flag & FLAG_ECE));
            tcp_rack_update_reo_wnd(tp, rs);

            if (tp.tlp_high_seq > 0)
            {
                tcp_process_tlp_ack(tp, ack, flag);
            }

            if (tcp_ack_is_dubious(tp, flag))
            {
                if (!BoolOk(flag & (FLAG_SND_UNA_ADVANCED | FLAG_NOT_DUP | FLAG_DSACKING_ACK)))
                {
                    num_dupack = 1;
                    if (!BoolOk(flag & FLAG_DATA))
                    {
                        num_dupack = 1;
                    }
                }
                tcp_fastretrans_alert(tp, prior_snd_una, num_dupack, ref flag, ref rexmit);
            }

            if (BoolOk(flag & FLAG_SET_XMIT_TIMER))
            {
                tcp_set_xmit_timer(tp);
            }

            delivered = tcp_newly_delivered(tp, delivered, flag);
            lost = tp.lost - lost;
            rs.is_ack_delayed = BoolOk(flag & FLAG_ACK_MAYBE_DELAYED);
            tcp_rate_gen(tp, delivered, lost, is_sack_reneg, sack_state.rate);
            tcp_cong_control(tp, ack, delivered, flag, sack_state.rate);
            tcp_xmit_recovery(tp, rexmit);
            return 1;

        no_queue:
            if (BoolOk(flag & FLAG_DSACKING_ACK))
            {
                tcp_fastretrans_alert(tp, prior_snd_una, num_dupack, ref flag, ref rexmit);
                tcp_newly_delivered(tp, delivered, flag);
            }
            tcp_ack_probe(tp);

            if (tp.tlp_high_seq > 0)
            {
                tcp_process_tlp_ack(tp, ack, flag);
            }
            return 1;
        old_ack:
            if (TCP_SKB_CB(skb).sacked > 0)
            {
                flag |= tcp_sacktag_write_queue(tp, skb, prior_snd_una, sack_state);
                tcp_fastretrans_alert(tp, prior_snd_una, num_dupack, ref flag, ref rexmit);
                tcp_newly_delivered(tp, delivered, flag);
                tcp_xmit_recovery(tp, rexmit);
            }
            return 0;
        }

        //延迟确认（Delayed ACK）：TCP 默认会延迟发送确认报文，通常延迟时间为 200 毫秒左右。
        //这种机制可以减少确认报文的数量，但可能会导致发送方等待较长时间。
        //Quick ACK：在某些情况下，接收方可以立即发送确认报文，而不是等待延迟时间。这通常在以下场景中发生：
        //接收到一个完整的 TCP 数据段后，立即发送确认。
        //接收到多个数据段后，连续发送确认，而不是等待延迟时间。
        static bool tcp_in_quickack_mode(tcp_sock tp)
        {
            return tp.icsk_ack.quick > 0 && !inet_csk_in_pingpong_mode(tp);
        }

        //int ofo_possible：一个布尔值，指示是否有可能发生乱序接收（out-of-order, OFO）。
        //如果设置为 1，则表示乱序接收的可能性存在，这可能影响如何处理确认和重传。
        //如果设置为 0,不存在乱序
        static void __tcp_ack_snd_check(tcp_sock tp, bool ofo_possible)
        {
            if ((
                (tp.rcv_nxt - tp.rcv_wup) > tp.icsk_ack.rcv_mss &&
                    (tp.rcv_nxt - tp.copied_seq < tp.sk_rcvlowat || __tcp_select_window(tp) >= tp.rcv_wnd)
                ) ||
                tcp_in_quickack_mode(tp) || BoolOk(tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_NOW)
              )
            {
                goto send_now;
            }

            if (!ofo_possible || RB_EMPTY_ROOT(tp.out_of_order_queue))
            {
                tcp_send_delayed_ack(tp);
                TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.DELAYED_ACK);
                return;
            }

            if (!tcp_is_sack(tp) || tp.compressed_ack >= sock_net(tp).ipv4.sysctl_tcp_comp_sack_nr)
            {
                goto send_now;
            }

            if (tp.compressed_ack_rcv_nxt != tp.rcv_nxt)
            {
                tp.compressed_ack_rcv_nxt = tp.rcv_nxt;
                tp.dup_ack_counter = 0;
            }

            if (tp.dup_ack_counter < TCP_FASTRETRANS_THRESH)
            {
                tp.dup_ack_counter++;
                goto send_now;
            }

            tp.compressed_ack++;
            if (tp.compressed_ack_timer.hrtimer_is_queued())
            {
                return;
            }

            long rtt = tp.rcv_rtt_est.rtt_us;
            if (tp.srtt_us > 0 && tp.srtt_us < rtt)
            {
                rtt = tp.srtt_us;
            }

            long delay = Math.Min(sock_net(tp).ipv4.sysctl_tcp_comp_sack_delay_ns, (long)Math.Ceiling(rtt / 8.0 / 20.0));
            delay = Math.Max(1, delay);
            tp.compressed_ack_timer.Start(delay);
            TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.COMPRESSED_ACK);
            return;
        send_now:
            tcp_send_ack(tp);
            TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.QUICK_ACK);
            return;
        }

        static void tcp_ack_snd_check(tcp_sock tp)
        {
            if (!inet_csk_ack_scheduled(tp))
            {
                return;
            }
            __tcp_ack_snd_check(tp, true);
        }

        //是一个用于解析 TCP 数据包中的时间戳选项的函数。这个函数假设时间戳选项是 4 字节对齐的，因此可以更高效地解析
        static bool tcp_parse_aligned_timestamp(tcp_sock tp, sk_buff skb)
        {
            //将指针 ptr 移动到 TCP 头部之后，指向选项的起始位置。
            var skbSpan = skb_transport_header(skb).Slice(sizeof_tcphdr);
            int nOffset = 0;
            int ptr = EndianBitConverter.ToInt32(skbSpan.Slice(nOffset));
            if (ptr == ((TCPOPT_NOP << 24) | (TCPOPT_NOP << 16) | (TCPOPT_TIMESTAMP << 8) | TCPOLEN_TIMESTAMP))
            {
                tp.rx_opt.saw_tstamp = true;

                nOffset += 4;
                ptr = EndianBitConverter.ToInt32(skbSpan.Slice(nOffset));
                tp.rx_opt.rcv_tsval = (uint)ptr;

                nOffset += 4;
                ptr = EndianBitConverter.ToInt32(skbSpan.Slice(nOffset));
                if (ptr > 0)
                {
                    tp.rx_opt.rcv_tsecr = (uint)(ptr - tp.tsoffset);
                }
                else
                {
                    tp.rx_opt.rcv_tsecr = 0;
                }
                return true;
            }
            return false;
        }

        static void tcp_rcv_rtt_measure_ts(tcp_sock tp, sk_buff skb)
        {
            if (tp.rx_opt.rcv_tsecr == tp.rcv_rtt_last_tsecr)
            {
                return;
            }

            tp.rcv_rtt_last_tsecr = tp.rx_opt.rcv_tsecr;
            if (TCP_SKB_CB(skb).end_seq - TCP_SKB_CB(skb).seq >= tp.icsk_ack.rcv_mss)
            {
                long delta = tcp_rtt_tsopt_us(tp);
                if (delta >= 0)
                {
                    tcp_rcv_rtt_update(tp, delta, 0);
                }
            }
        }

        // bTcpConnected: 是否已经建立了连接
        public static void tcp_parse_options(net net, sk_buff skb, tcp_options_received opt_rx, bool bTcpConnected)
        {
            tcphdr th = tcp_hdr(skb);
            int length = th.doff - sizeof_tcphdr;
            int ptrIndex = sizeof_tcphdr;
            opt_rx.saw_tstamp = false;
            opt_rx.saw_unknown = 0;

            while (length > 0)
            {
                uint opcode = skb.mBuffer[ptrIndex++];
                switch (opcode)
                {
                    case TCPOPT_EOL:
                        return;
                    case TCPOPT_NOP:
                        length--;
                        continue;
                    default:
                        {
                            if (length < 2)
                            {
                                return;
                            }
                            int opsize = skb.mBuffer[ptrIndex++];
                            if (opsize < 2)
                            {
                                return;
                            }
                            if (opsize > length)
                            {
                                return;
                            }

                            switch (opcode)
                            {
                                case TCPOPT_MSS:
                                    if (!bTcpConnected && opsize == TCPOLEN_MSS)
                                    {
                                        ushort in_mss = EndianBitConverter.ToUInt16(skb.mBuffer, ptrIndex);
                                        if (in_mss > 0)
                                        {
                                            opt_rx.mss_clamp = in_mss;
                                        }
                                    }
                                    break;

                                case TCPOPT_WINDOW:
                                    if (!bTcpConnected && opsize == TCPOLEN_WINDOW && net.ipv4.sysctl_tcp_window_scaling > 0)
                                    {
                                        byte snd_wscale = skb.mBuffer[ptrIndex];
                                        opt_rx.wscale_ok = 1;
                                        if (snd_wscale > TCP_MAX_WSCALE)
                                        {
                                            snd_wscale = (byte)TCP_MAX_WSCALE;
                                        }
                                        opt_rx.snd_wscale = snd_wscale;
                                    }
                                    break;
                                case TCPOPT_TIMESTAMP:
                                    if ((opsize == TCPOLEN_TIMESTAMP) &&
                                        ((bTcpConnected && opt_rx.tstamp_ok > 0) ||
                                         (!bTcpConnected && net.ipv4.sysctl_tcp_timestamps > 0)))
                                    {
                                        opt_rx.saw_tstamp = true;
                                        opt_rx.rcv_tsval = EndianBitConverter.ToUInt32(skb.mBuffer, ptrIndex);
                                        opt_rx.rcv_tsecr = EndianBitConverter.ToUInt32(skb.mBuffer, ptrIndex + 4);
                                    }
                                    break;
                                case TCPOPT_SACK_PERM:
                                    if (!bTcpConnected && opsize == TCPOLEN_SACK_PERM && net.ipv4.sysctl_tcp_sack > 0)
                                    {
                                        opt_rx.sack_ok = TCP_SACK_SEEN;
                                        tcp_sack_reset(opt_rx);
                                    }
                                    break;

                                case TCPOPT_SACK:
                                    if ((opsize >= (TCPOLEN_SACK_BASE + TCPOLEN_SACK_PERBLOCK)) &&
                                        ((opsize - TCPOLEN_SACK_BASE) % TCPOLEN_SACK_PERBLOCK) == 0 &&
                                        opt_rx.sack_ok > 0)
                                    {
                                        TCP_SKB_CB(skb).sacked = (byte)(ptrIndex - 2);
                                        TcpMibMgr.NET_ADD_AVERAGE_STATS(net, TCPMIB.sacked, TCP_SKB_CB(skb).sacked);
                                    }
                                    break;
                                case TCPOPT_MD5SIG:
                                case TCPOPT_AO:
                                case TCPOPT_FASTOPEN:
                                case TCPOPT_EXP:
                                    break;
                                default:
                                    opt_rx.saw_unknown = 1;
                                    break;
                            }
                            ptrIndex += (byte)(opsize - 2);
                            length -= opsize;
                        }
                        break;
                }
            }
        }

        static bool tcp_fast_parse_options(net net, sk_buff skb, tcphdr th, tcp_sock tp)
        {
            if (th.doff == sizeof_tcphdr)
            {
                tp.rx_opt.saw_tstamp = false;
                return false;
            }
            else if (tp.rx_opt.tstamp_ok > 0 && th.doff == sizeof_tcphdr + TCPOLEN_TSTAMP_ALIGNED)
            {
                if (tcp_parse_aligned_timestamp(tp, skb))
                {
                    return true;
                }
            }

            tcp_parse_options(net, skb, tp.rx_opt, true);
            if (tp.rx_opt.saw_tstamp && tp.rx_opt.rcv_tsecr > 0)
            {
                tp.rx_opt.rcv_tsecr -= (uint)tp.tsoffset;
            }
            return true;
        }

        //计算回显值：tcp_tsval_replay 计算一个时间戳回显值，用于判断接收到的数据包是否在有效的时间窗口内。
        //防止回绕攻击：通过比较接收到的数据包的时间戳和本地维护的时间戳，防止旧的数据包被错误地接受。
        static long tcp_tsval_replay(tcp_sock tp)
        {
            return tp.icsk_rto * 1200 / HZ;
        }

        //会检查该 ACK 是否是乱序的
        static bool tcp_disordered_ack(tcp_sock tp, sk_buff skb)
        {
            tcphdr th = tcp_hdr(skb);
            uint seq = TCP_SKB_CB(skb).seq;
            uint ack = TCP_SKB_CB(skb).ack_seq;

            return
                (th.ack > 0 && seq == TCP_SKB_CB(skb).end_seq && seq == tp.rcv_nxt) &&
                ack == tp.snd_una &&
                !tcp_may_update_window(tp, ack, seq, (uint)(th.window << tp.rx_opt.snd_wscale)) &&
                (int)(tp.rx_opt.ts_recent - tp.rx_opt.rcv_tsval) <= tcp_tsval_replay(tp);
        }
        
        static bool tcp_paws_discard(tcp_sock tp, sk_buff skb)
        {
            return !tcp_paws_check(tp.rx_opt, TCP_PAWS_WINDOW) &&
                   !tcp_disordered_ack(tp, skb);
        }

        //用于限制 TCP 接收到的“超出窗口”（Out-Of-Window，OOW）报文的处理速率的函数。
        //它的主要目的是防止因处理大量无效或恶意的超出窗口报文而导致的性能问题或拒绝服务攻击（DoS）
        static bool tcp_oow_rate_limited(net net, sk_buff skb, ref long last_oow_ack_time)
        {
            if (TCP_SKB_CB(skb).seq != TCP_SKB_CB(skb).end_seq && tcp_hdr(skb).syn == 0)
            {
                return false;
            }
            return __tcp_oow_rate_limited(net, ref last_oow_ack_time);
        }

        //tcp_send_dupack 是一个用于发送重复 ACK（Duplicate Acknowledgment）的函数。
        //在 TCP 协议中，重复 ACK 通常表示接收方已经收到了某个数据包，但期望发送方重新发送丢失的数据包。
        //这个函数在处理接收到的 TCP 数据包时，特别是在检测到乱序或丢失数据包时，会调用此函数发送重复 ACK。
        //发送重复 ACK：当接收方检测到乱序或丢失的数据包时，tcp_send_dupack 会发送一个重复 ACK，通知发送方重新发送丢失的数据包。
        //支持快速重传：通过发送重复 ACK，接收方可以触发发送方的快速重传机制，从而提高数据传输的效率。
        static void tcp_send_dupack(tcp_sock tp, sk_buff skb)
        {
            if (TCP_SKB_CB(skb).end_seq != TCP_SKB_CB(skb).seq && before(TCP_SKB_CB(skb).seq, tp.rcv_nxt))
            {
                tcp_enter_quickack_mode(tp, TCP_MAX_QUICKACKS);

                if (tcp_is_sack(tp) && sock_net(tp).ipv4.sysctl_tcp_dsack > 0)
                {
                    uint end_seq = TCP_SKB_CB(skb).end_seq;
                    if (after(TCP_SKB_CB(skb).end_seq, tp.rcv_nxt))
                    {
                        end_seq = tp.rcv_nxt;
                    }
                    tcp_dsack_set(tp, TCP_SKB_CB(skb).seq, end_seq);
                }
            }
            tcp_send_ack(tp);
        }

        static bool tcp_sequence(tcp_sock tp, uint seq, uint end_seq)
        {
            if (before(end_seq, tp.rcv_wup))
            {
                return false;
            }

            if (after(seq, tp.rcv_nxt + tcp_receive_window(tp)))
            {
                return false;
            }
            return true;
        }

        static bool tcp_reset_check(tcp_sock tp, sk_buff skb)
        {
            return TCP_SKB_CB(skb).seq == (tp.rcv_nxt - 1) &&
                    BoolOk((1 << tp.sk_state) & TCPF_CLOSE_WAIT | TCPF_LAST_ACK | TCPF_CLOSING);
        }

        //用于验证接收到的 TCP 报文是否合格的函数
        static bool tcp_validate_incoming(tcp_sock tp, sk_buff skb, tcphdr th)
        {
            if (tcp_fast_parse_options(sock_net(tp), skb, th, tp) && tp.rx_opt.saw_tstamp && tcp_paws_discard(tp, skb))
            {
                if (!tcp_oow_rate_limited(sock_net(tp), skb, ref tp.last_oow_ack_time))
                {
                    tcp_send_dupack(tp, skb);
                }
                return false;
            }

            bool bOk = tcp_sequence(tp, TCP_SKB_CB(skb).seq, TCP_SKB_CB(skb).end_seq);
            if (!bOk)
            {
                if (!tcp_oow_rate_limited(sock_net(tp), skb, ref tp.last_oow_ack_time))
                {
                    tcp_send_dupack(tp, skb);
                }
                return false;
            }
            return true;
        }

        static void tcp_rcv_established(tcp_sock tp, sk_buff skb)
        {
            int reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;
            tcphdr th = tcp_hdr(skb);
            int len = skb.nBufferLength;

            tcp_mstamp_refresh(tp);
            tp.rx_opt.saw_tstamp = false;

            if(tcp_hdr(skb).tot_len != tcp_hdr(skb).doff)
            {
                TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.RECEIVE_COUNT);
            }

            //这里是一个快速路径
            //检查当前报文的标志位是否与之前接收到的报文的标志位一致, 判断是否可以进入 快速路径
            if ((tcp_flag_word(th) & TCP_HP_BITS) == tp.pred_flags &&
                TCP_SKB_CB(skb).seq == tp.rcv_nxt &&
                !after(TCP_SKB_CB(skb).ack_seq, tp.snd_nxt))
            {
                TcpMibMgr.NET_ADD_STATS(sock_net(tp), TCPMIB.FAST_PATH);

                int tcp_header_len = tcp_hdr(skb).doff;
                if (tcp_header_len == sizeof_tcphdr + TCPOLEN_TSTAMP_ALIGNED)
                {
                    if (!tcp_parse_aligned_timestamp(tp, skb))
                    {
                        goto slow_path;
                    }

                    if ((int)(tp.rx_opt.rcv_tsval - tp.rx_opt.ts_recent) < 0)
                    {
                        goto slow_path;
                    }
                }

                // 如果没有数据，则表明，这里是一个纯粹的ACK包
                if (len == tcp_header_len)
                {
                    if (tcp_header_len == (sizeof_tcphdr + TCPOLEN_TSTAMP_ALIGNED) && tp.rcv_nxt == tp.rcv_wup)
                    {
                        tcp_store_ts_recent(tp);
                    }

                    tcp_ack(tp, skb, 0);
                    kfree_skb(tp, skb);

                    tcp_data_snd_check(tp);
                    tp.rcv_rtt_last_tsecr = tp.rx_opt.rcv_tsecr;

                    return;
                }
                else if (len < tcp_header_len)
                {
                    reason = skb_drop_reason.SKB_DROP_REASON_PKT_TOO_SMALL;
                    goto discard;
                }
                else
                {
                    if (tcp_header_len == (sizeof_tcphdr + TCPOLEN_TSTAMP_ALIGNED) && tp.rcv_nxt == tp.rcv_wup)
                    {
                        tcp_store_ts_recent(tp);
                    }

                    tcp_rcv_rtt_measure_ts(tp, skb);
                    skb_pull(skb, tcp_header_len);

                    int eaten = tcp_queue_rcv(tp, skb);
                    tcp_event_data_recv(tp, skb);

                    if (TCP_SKB_CB(skb).ack_seq != tp.snd_una)
                    {
                        tcp_ack(tp, skb, FLAG_DATA);
                        tcp_data_snd_check(tp);
                        if (!inet_csk_ack_scheduled(tp))
                        {
                            goto no_ack;
                        }
                    }
                    else
                    {
                        tcp_update_wl(tp, TCP_SKB_CB(skb).seq);
                    }

                    __tcp_ack_snd_check(tp, false);
                no_ack:
                    if (eaten > 0)
                    {
                        kfree_skb(tp, skb);
                    }
                    return;
                }
            }

        slow_path:
            if (len < th.doff)
            {
                goto discard;
            }
            
            if (th.ack == 0)
            {
                reason = skb_drop_reason.SKB_DROP_REASON_TCP_FLAGS;
                goto discard;
            }

            if (!tcp_validate_incoming(tp, skb, th))
            {
                return;
            }
        step5:
            reason = tcp_ack(tp, skb, FLAG_SLOWPATH | FLAG_UPDATE_TS_RECENT);
            if (reason < 0)
            {
                reason = -reason;
                goto discard;
            }

            tcp_rcv_rtt_measure_ts(tp, skb);
            tcp_data_queue(tp, skb);
            tcp_data_snd_check(tp);
            tcp_ack_snd_check(tp);
            return;
        discard:
            tcp_drop_reason(tp, skb, reason);
        }

        static void tcp_clear_retrans(tcp_sock tp)
        {
	        tp.retrans_out = 0;
	        tp.lost_out = 0;
	        tp.undo_marker = 0;
	        tp.undo_retrans = -1;
	        tp.sacked_out = 0;
	        tp.rto_stamp = 0;
	        tp.total_rto = 0;
	        tp.total_rto_recoveries = 0;
	        tp.total_rto_time = 0;
        }

        static void tcp_ecn_rcv_syn(tcp_sock tp, tcphdr th)
        {
            if (BoolOk(tp.ecn_flags & TCP_ECN_OK) && (th.ece == 0 || th.cwr == 0))
            {
                tp.ecn_flags = (byte)(tp.ecn_flags & ~TCP_ECN_OK);
            }
        }

        static void tcp_ecn_rcv_synack(tcp_sock tp, tcphdr th)
        {
            if (BoolOk(tp.ecn_flags & TCP_ECN_OK) && (th.ece == 0 || th.cwr > 0))
            {
                tp.ecn_flags = (byte)(tp.ecn_flags & ~TCP_ECN_OK);
            }
        }

        static void tcp_set_rto(tcp_sock tp)
        {
            tp.icsk_rto = __tcp_set_rto(tp);
            tcp_bound_rto(tp);
        }

        static void tcp_rtt_estimator(tcp_sock tp, long mrtt_us)
        {
            long m = mrtt_us;
            long srtt = tp.srtt_us;

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
                    tp.mdev_max_us = tcp_rto_min_us(tp);
                }
            }
            else
            {
                srtt = m << 3;
                tp.mdev_us = m << 1;
                tp.rttvar_us = Math.Max(tp.mdev_us, tcp_rto_min_us(tp));
                tp.mdev_max_us = tp.rttvar_us;
                tp.rtt_seq = tp.snd_nxt;
            }
            tp.srtt_us = Math.Max(1U, srtt);
        }

        static bool tcp_should_expand_sndbuf(tcp_sock tp)
        {
            if (tcp_packets_in_flight(tp) >= tcp_snd_cwnd(tp))
            {
                return false;
            }
	        return true;
        }

        static void tcp_new_space(tcp_sock tp)
        {
	        if (tcp_should_expand_sndbuf(tp)) 
            {
		        tcp_sndbuf_expand(tp);
                tp.snd_cwnd_stamp = tcp_jiffies32;
	        }

            //INDIRECT_CALL_1(sk->sk_write_space, sk_stream_write_space, sk);
        }

        static void tcp_check_space(tcp_sock tp)
        {
	        if (BoolOk(tp.sk_socket_flags & (1 << SOCK_NOSPACE))) 
            {
		        tcp_new_space(tp);
                if (!BoolOk(tp.sk_socket_flags & (1 << SOCK_NOSPACE)))
                {
                    tcp_chrono_stop(tp, tcp_chrono.TCP_CHRONO_SNDBUF_LIMITED);
                }
            }
        }

    }

}
