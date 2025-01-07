/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.LinuxTcp;
using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        //它用于确保 TCP 的重传超时（RTO, Retransmission Timeout）不会超过用户设定的连接超时时间。
        public static void tcp_done_with_error(tcp_sock tp, int err)
        {
            tp.sk_err = err;
        }

        public static void tcp_sack_compress_send_ack(tcp_sock tp)
        {
            if (tp.compressed_ack == 0)
            {
                return;
            }

            if (tp.compressed_ack_timer.TryToCancel())
            {
                __sock_put(tp);
            }

            NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPACKCOMPRESSED, tp.compressed_ack - 1);
            tp.compressed_ack = 0;
            tcp_send_ack(tp);
        }

        public static void tcp_enter_loss(tcp_sock tp)
        {
            net net = sock_net(tp);
            bool new_recovery = tp.icsk_ca_state < (int)tcp_ca_state.TCP_CA_Recovery;
            uint reordering;

            tcp_timeout_mark_lost(tp);

            if (tp.icsk_ca_state <= (int)tcp_ca_state.TCP_CA_Disorder || !after(tp.high_seq, tp.snd_una) ||
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

            reordering = (uint)net.ipv4.sysctl_tcp_reordering;
            if (tp.icsk_ca_state <= (int)tcp_ca_state.TCP_CA_Disorder && tp.sacked_out >= reordering)
            {
                tp.reordering = Math.Min(tp.reordering, reordering);
            }
            tcp_set_ca_state(tp, tcp_ca_state.TCP_CA_Loss);
            tp.high_seq = tp.snd_nxt;
            tp.tlp_high_seq = 0;
            tcp_ecn_queue_cwr(tp);

            tp.frto = (byte)((net.ipv4.sysctl_tcp_frto > 0 && (new_recovery || tp.icsk_retransmits > 0) && tp.icsk_mtup.probe_size == 0) ? 1 : 0);
        }

        public static void tcp_timeout_mark_lost(tcp_sock tp)
        {
            bool is_reneg;

            RedBlackTreeNode<sk_buff> headNode = tp.tcp_rtx_queue.FirstNode();
            sk_buff head = tp.tcp_rtx_queue.FirstValue();

            is_reneg = head != null && (TCP_SKB_CB(head).sacked & (byte)tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED) > 0;
            if (is_reneg)
            {
                NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPSACKRENEGING, 1);
                tp.sacked_out = 0;
                tp.is_sack_reneg = 1;
            }
            else if (tcp_is_reno(tp))
            {
                tcp_reset_reno_sack(tp);
            }

            var skbNode = headNode;
            sk_buff skb = head;
            for (; skb != null; skbNode = tp.tcp_rtx_queue.NextNode(skbNode))
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
            WARN_ON(tcp_left_out(tp) <= tp.packets_out);
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

        public static void tcp_ecn_queue_cwr(tcp_sock tp)
        {
            if ((tp.ecn_flags & tcp_sock.TCP_ECN_OK) > 0)
            {
                tp.ecn_flags |= tcp_sock.TCP_ECN_QUEUE_CWR;
            }
        }

        public static void tcp_reset_reno_sack(tcp_sock tp)
        {
            tp.sacked_out = 0;
        }

        public static bool tcp_is_rack(tcp_sock tp)
        {
            return (sock_net(tp).ipv4.sysctl_tcp_recovery & tcp_sock.TCP_RACK_LOSS_DETECTION) > 0;
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
                    tp.retrans_out -= (uint)tcp_skb_pcount(skb);
                    NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPLOSTRETRANSMIT, tcp_skb_pcount(skb));
                    tcp_notify_skb_loss_event(tp, skb);
                }
            }
            else
            {
                tp.lost_out += (uint)tcp_skb_pcount(skb);
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
            tp.lost += (uint)tcp_skb_pcount(skb);
        }

        public static int tcp_skb_shift(sk_buff to, sk_buff from, int pcount, int shiftlen)
        {
            if (to.len + shiftlen >= 65535 * tcp_sock.TCP_MIN_GSO_SIZE)
            {
                return 0;
            }
            if ((tcp_skb_pcount(to) + pcount > 65535))
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
            LINUXMIB mib_idx;
            tcp_retrans_stamp_cleanup(tp);

            if (tcp_is_reno(tp))
            {
                mib_idx = LINUXMIB.LINUX_MIB_TCPRENORECOVERY;
            }
            else
            {
                mib_idx = LINUXMIB.LINUX_MIB_TCPSACKRECOVERY;
            }

            NET_ADD_STATS(sock_net(tp), mib_idx, 1);

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

            tp.prr_delivered += newly_acked_sacked;
            if (delta < 0)
            {
                long dividend = tp.snd_ssthresh * tp.prr_delivered + tp.prior_cwnd - 1;
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

        static void tcp_rearm_rto(tcp_sock tp)
        {
            if (tp.packets_out == 0)
            {
                inet_csk_clear_xmit_timer(tp, tcp_sock.ICSK_TIME_RETRANS);
            }
            else
            {
                uint rto = (uint)tp.icsk_rto;
                if (tp.icsk_pending == tcp_sock.ICSK_TIME_REO_TIMEOUT || tp.icsk_pending == tcp_sock.ICSK_TIME_LOSS_PROBE)
                {
                    long delta_us = tcp_rto_delta_us(tp);
                    rto = (uint)Math.Max(delta_us, 1);
                }
                tcp_reset_xmit_timer(tp, tcp_sock.ICSK_TIME_RETRANS, rto, tcp_sock.TCP_RTO_MAX);
            }
        }

        static void tcp_check_space(tcp_sock tp)
        {

        }

        static uint tcp_init_cwnd(tcp_sock tp, dst_entry dst)
        {
            uint cwnd = (uint)(dst != null ? dst_metric(dst, (ulong)RTAX_INITCWND) : 0);

            if (cwnd == 0)
            {
                cwnd = TCP_INIT_CWND;
            }
            return (uint)Math.Min(cwnd, tp.snd_cwnd_clamp);
        }

        static void tcp_rbtree_insert(AkRBTree<sk_buff> mRBTree, sk_buff skb)
        {
            mRBTree.Add(skb);
        }

        static void tcp_rcv_space_adjust(tcp_sock tp)
        {

        }

        static void tcp_update_rtt_min(tcp_sock tp, long rtt_us, int flag)
        {
            long wlen = (uint)(sock_net(tp).ipv4.sysctl_tcp_min_rtt_wlen * tcp_sock.HZ);
            if (BoolOk(flag & FLAG_ACK_MAYBE_DELAYED) && rtt_us > tcp_min_rtt(tp))
            {
                return;
            }
            minmax_running_min(tp.rtt_min, wlen, tcp_jiffies32, rtt_us > 0 ? rtt_us : 1000);
        }

        static long tcp_rtt_tsopt_us(tcp_sock tp)
        {
            long delta, delta_us;
            delta = tcp_time_stamp_ts(tp) - tp.rx_opt.rcv_tsecr;
            if (tp.tcp_usec_ts)
            {
                return delta;
            }

            if (delta < int.MaxValue / (USEC_PER_SEC / TCP_TS_HZ))
            {
                if (delta == 0)
                {
                    delta = 1;
                }
                delta_us = delta * (USEC_PER_SEC / TCP_TS_HZ);
                return delta_us;
            }

            return -1;
        }

        static bool tcp_ack_update_rtt(tcp_sock tp, int flag,
            long seq_rtt_us, long sack_rtt_us, long ca_rtt_us, rate_sample rs)
        {
            if (seq_rtt_us < 0)
            {
                seq_rtt_us = sack_rtt_us;
            }

            if (seq_rtt_us < 0 && tp.rx_opt.saw_tstamp > 0 &&
                tp.rx_opt.rcv_tsecr > 0 && BoolOk(flag & FLAG_ACKED))
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

        static void tcp_try_undo_spurious_syn(tcp_sock tp)
        {
            long syn_stamp = tp.retrans_stamp;
            if (tp.undo_marker > 0 && syn_stamp > 0 && tp.rx_opt.saw_tstamp > 0 && syn_stamp == tp.rx_opt.rcv_tsecr)
            {
                tp.undo_marker = 0;
            }
        }


        static void tcp_sndbuf_expand(tcp_sock tp)
        {
            tcp_congestion_ops ca_ops = tp.icsk_ca_ops;
            int sndmem, per_mss;
            uint nr_segs;
            per_mss = (int)Math.Max(tp.rx_opt.mss_clamp, tp.mss_cache) + MAX_TCP_HEADER;
            per_mss = roundup_pow_of_two(per_mss);

            nr_segs = Math.Max(TCP_INIT_CWND, tcp_snd_cwnd(tp));
            nr_segs = Math.Max(nr_segs, tp.reordering + 1);
            sndmem = ca_ops.sndbuf_expand != null ? (int)ca_ops.sndbuf_expand(tp) : 2;
            sndmem = (int)(sndmem * nr_segs * per_mss);

            if (tp.sk_sndbuf < sndmem)
            {
                tp.sk_sndbuf = Math.Min(sndmem, sock_net(tp).ipv4.sysctl_tcp_wmem[2]));
            }
        }


        static void tcp_init_buffer_space(tcp_sock tp)
        {
            int tcp_app_win = sock_net(tp).ipv4.sysctl_tcp_app_win;

            if (true)
            {
                tcp_sndbuf_expand(tp);
            }

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

        static void tcp_init_transfer(tcp_sock tp, sk_buff skb)
        {
            tcp_mtup_init(tp);
            tp.icsk_af_ops.rebuild_header(tp);
            tcp_init_metrics(tp);

            if (tp.total_retrans > 1 && tp.undo_marker > 0)
            {
                tcp_snd_cwnd_set(tp, 1);
            }
            else
            {
                tcp_snd_cwnd_set(tp, tcp_init_cwnd(tp, __sk_dst_get(tp)));
            }
            tp.snd_cwnd_stamp = tcp_jiffies32;

            if (!tp.icsk_ca_initialized)
            {
                tcp_init_congestion_control(tp);
            }
            tcp_init_buffer_space(tp);
        }

        static void tcp_update_pacing_rate(tcp_sock tp)
        {
            long rate = (long)tp.mss_cache * ((USEC_PER_SEC / 100) << 3);

            if (tcp_snd_cwnd(tp) < tp.snd_ssthresh / 2)
            {
                rate *= sock_net(tp).ipv4.sysctl_tcp_pacing_ss_ratio);
            }
            else
            {
                rate *= sock_net(tp).ipv4.sysctl_tcp_pacing_ca_ratio);
            }

            rate *= Math.Max(tcp_snd_cwnd(tp), tp.packets_out);

            if (tp.srtt_us > 0)
            {
                rate /= tp.srtt_us;
            }
            tp.sk_pacing_rate = Math.Min(rate, tp.sk_max_pacing_rate);
        }

        static void tcp_initialize_rcv_mss(tcp_sock tp)
        {
            uint hint = Math.Min(tp.advmss, tp.mss_cache);
            hint = Math.Min(hint, tp.rcv_wnd / 2);
            hint = Math.Min(hint, TCP_MSS_DEFAULT);
            hint = Math.Max(hint, TCP_MIN_MSS);
            tp.icsk_ack.rcv_mss = (ushort)hint;
        }

        static bool tcp_try_coalesce(tcp_sock tp, sk_buff to, sk_buff from, out bool fragstolen)
        {
            int delta;
            fragstolen = false;

            if (TCP_SKB_CB(from).seq != TCP_SKB_CB(to).end_seq)
            {
                return false;
            }

            if (!tcp_skb_can_collapse_rx(to, from))
            {
                return false;
            }

	        if (!skb_try_coalesce(to, from, fragstolen, &delta))
		        return false;

	        atomic_add(delta, &sk->sk_rmem_alloc);
                sk_mem_charge(sk, delta);
                NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPRCVCOALESCE);
                TCP_SKB_CB(to)->end_seq = TCP_SKB_CB(from)->end_seq;
	        TCP_SKB_CB(to)->ack_seq = TCP_SKB_CB(from)->ack_seq;
	        TCP_SKB_CB(to)->tcp_flags |= TCP_SKB_CB(from)->tcp_flags;

	        if (TCP_SKB_CB(from)->has_rxtstamp) {
		        TCP_SKB_CB(to)->has_rxtstamp = true;
		        to->tstamp = from->tstamp;
		        skb_hwtstamps(to)->hwtstamp = skb_hwtstamps(from)->hwtstamp;
	        }

	        return true;
        }

        static int tcp_queue_rcv(tcp_sock tp, sk_buff skb, out bool fragstolen)
        {
            fragstolen = false;
            sk_buff tail = skb_peek_tail(tp.sk_receive_queue);
            int eaten = (tail && tcp_try_coalesce(tp, tail, skb, fragstolen)) ? 1 : 0;

	        tcp_rcv_nxt_update(tcp_sk(sk), TCP_SKB_CB(skb)->end_seq);
	        if (!eaten) {
		        __skb_queue_tail(&sk->sk_receive_queue, skb);
                skb_set_owner_r(skb, sk);
            }
	        return eaten;
        }

        static void tcp_data_queue(tcp_sock tp, sk_buff skb)
        {
            skb_drop_reason reason;
	        bool fragstolen;
            int eaten;

	        if (TCP_SKB_CB(skb).seq == TCP_SKB_CB(skb).end_seq) 
            {
		        __kfree_skb(skb);
		        return;
	        }


            __skb_pull(skb, tcp_hdr(skb).doff * 4);

            reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;
            tp.rx_opt.dsack = 0;

            if (TCP_SKB_CB(skb).seq == tp.rcv_nxt)
            {
                if (tcp_receive_window(tp) == 0)
                {
                    if (skb.len == 0 && BoolOk(TCP_SKB_CB(skb).tcp_flags & TCPHDR_FIN))
                    {
                        goto queue_and_out;
                    }

                    reason = skb_drop_reason.SKB_DROP_REASON_TCP_ZEROWINDOW;
                    NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPZEROWINDOWDROP, 1);
                    goto out_of_window;
                }

            queue_and_out:
                eaten = tcp_queue_rcv(tp, skb, fragstolen);
                if (skb.len > 0)
                {
                    tcp_event_data_recv(tp, skb);
                }
                if (BoolOk(TCP_SKB_CB(skb).tcp_flags & TCPHDR_FIN))
                {
                    tcp_fin(sk);
                }

                if (!RB_EMPTY_ROOT(tp.out_of_order_queue))
                {
                    tcp_ofo_queue(sk);
                    if (RB_EMPTY_ROOT(&tp->out_of_order_queue))
                        inet_csk(sk)->icsk_ack.pending |= ICSK_ACK_NOW;
                }

                if (tp.rx_opt.num_sacks > 0)
                {
                    tcp_sack_remove(tp);
                }
                tcp_fast_path_check(tp);

                if (eaten > 0)
                    kfree_skb_partial(skb, fragstolen);
                if (!sock_flag(tp, SOCK_DEAD))
                {
                    tcp_data_ready(sk);
                }
                return;
            }

        if (!after(TCP_SKB_CB(skb)->end_seq, tp->rcv_nxt))
        {
            tcp_rcv_spurious_retrans(sk, skb);
            /* A retransmit, 2nd most common case.  Force an immediate ack. */
            reason = SKB_DROP_REASON_TCP_OLD_DATA;
            NET_INC_STATS(sock_net(sk), LINUX_MIB_DELAYEDACKLOST);
            tcp_dsack_set(sk, TCP_SKB_CB(skb)->seq, TCP_SKB_CB(skb)->end_seq);

        out_of_window:
            tcp_enter_quickack_mode(sk, TCP_MAX_QUICKACKS);
            inet_csk_schedule_ack(sk);
        drop:
            tcp_drop_reason(sk, skb, reason);
            return;
        }

        /* Out of window. F.e. zero window probe. */
        if (!before(TCP_SKB_CB(skb)->seq,
                tp->rcv_nxt + tcp_receive_window(tp)))
        {
            reason = SKB_DROP_REASON_TCP_OVERWINDOW;
            goto out_of_window;
        }

        if (before(TCP_SKB_CB(skb)->seq, tp->rcv_nxt))
        {
            /* Partial packet, seq < rcv_next < end_seq */
            tcp_dsack_set(sk, TCP_SKB_CB(skb)->seq, tp->rcv_nxt);

            /* If window is closed, drop tail of packet. But after
             * remembering D-SACK for its head made in previous line.
             */
            if (!tcp_receive_window(tp))
            {
                reason = SKB_DROP_REASON_TCP_ZEROWINDOW;
                NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPZEROWINDOWDROP);
                goto out_of_window;
            }
            goto queue_and_out;
        }

        tcp_data_queue_ofo(sk, skb);
        }

        static skb_drop_reason tcp_rcv_state_process(tcp_sock tp, sk_buff skb)
        {
                tcphdr th = skb.hdr;
                request_sock req = null;
	            int queued = 0;
                skb_drop_reason reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;

            switch ((TCP_STATE)tp.sk_state)
            {
                case TCP_STATE.TCP_CLOSE:
                    {
                        SKB_DR_SET(reason, TCP_CLOSE);
                        goto discard;
                        break;
                    }
                case TCP_STATE.TCP_LISTEN:
                    {
                        if (th.ack > 0)
                        {
                            return SKB_DROP_REASON_TCP_FLAGS;
                        }

                        if (th.rst > 0)
                        {
                            reason = skb_drop_reason.SKB_DROP_REASON_TCP_RESET;
                            goto discard;
                        }
                        if (th.syn > 0)
                        {
                            if (th.fin > 0)
                            {
                                reason = skb_drop_reason.SKB_DROP_REASON_TCP_FLAGS;
                                goto discard;
                            }

                            tp.icsk_af_ops.conn_request(tp, skb);
                            consume_skb(skb);
                            return 0;
                        }

                        reason = skb_drop_reason.SKB_DROP_REASON_TCP_FLAGS;
                        goto discard;
                        break;
                    }
                case TCP_STATE.TCP_SYN_SENT:
                    {
                        tp.rx_opt.saw_tstamp = 0;
                        tcp_mstamp_refresh(tp);
                        queued = tcp_rcv_synsent_state_process(tp, skb, th);
                        if (queued >= 0)
                        {
                            return queued;
                        }
                            
                        __kfree_skb(skb);
                        tcp_data_snd_check(sk);
                        return 0;
                    }
            }

            tcp_mstamp_refresh(tp);
            tp.rx_opt.saw_tstamp = 0;


            if (th.ack == 0 && th.rst == 0 && th.syn == 0)
            {
                reason = skb_drop_reason.SKB_DROP_REASON_TCP_FLAGS;
                goto discard;
            }

            if (!tcp_validate_incoming(tp, skb, th, 0))
            {
                return 0;
            }
        
        reason = tcp_ack(tp, skb, FLAG_SLOWPATH |
                      FLAG_UPDATE_TS_RECENT |
                      FLAG_NO_CHALLENGE_ACK);

            if ((int)reason <= 0)
            {
                if (tp.sk_state == (byte)TCP_STATE.TCP_SYN_RECV)
                {
                    if (reason == 0)
                    {
                        return skb_drop_reason.SKB_DROP_REASON_TCP_OLD_ACK;
                    }
                    return -reason;
                }

                if ((int)reason < 0)
                {
                    tcp_send_challenge_ack(tp);
                    reason = -reason;
                    goto discard;
                }
            }

            reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;

        switch (tp.sk_state)
        {
            case (byte)TCP_STATE.TCP_SYN_RECV:
                    tp.delivered++;
                    if (tp.srtt_us == 0)
                    {
                        tcp_synack_rtt_meas(tp, req);
                    }
                    
                    tcp_try_undo_spurious_syn(tp);
                    tp.retrans_stamp = 0;
                    tcp_init_transfer(tp,skb);
                    tp.copied_seq = tp.rcv_nxt;
                    
                //tcp_ao_established(tp);
                tcp_set_state(tp, (byte)TCP_STATE.TCP_ESTABLISHED);
                //tp.sk_state_change(tp);

     

                tp.snd_una = TCP_SKB_CB(skb).ack_seq;
                tp.snd_wnd = htonl(th.window) << tp.rx_opt.snd_wscale;
                tcp_init_wl(tp, TCP_SKB_CB(skb).seq);

                if (tp.rx_opt.tstamp_ok > 0)
                {
                    tp.advmss -= TCPOLEN_TSTAMP_ALIGNED;
                }

                if (tp.icsk_ca_ops.cong_control == null)
                {
                    tcp_update_pacing_rate(tp);
                }

                tp.lsndtime = tcp_jiffies32;
                tcp_initialize_rcv_mss(tp);
                tcp_fast_path_on(tp);
                break;

            case (byte)TCP_STATE.TCP_FIN_WAIT1:
                    {
                        int tmo;

                        if (req != null)
                        {
                            // tcp_rcv_synrecv_state_fastopen(sk);
                        }

                        if (tp.snd_una != tp.write_seq)
                        {
                            break;
                        }
                        tcp_set_state(tp, (int)TCP_STATE.TCP_FIN_WAIT2);
                        sk_dst_confirm(tp);

                        if (!sock_flag(sk, SOCK_DEAD))
                        {
                            tp.sk_state_change(tp);
                            break;
                        }

                        if (tp.linger2 < 0)
                        {
                            tcp_done(tp);
                            NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPABORTONDATA, 1);
                            return skb_drop_reason.SKB_DROP_REASON_TCP_ABORT_ON_DATA;
                        }

                        if (TCP_SKB_CB(skb).end_seq != TCP_SKB_CB(skb).seq && after(TCP_SKB_CB(skb).end_seq - th.fin, tp.rcv_nxt))
                        {
                            if (tp.syn_fastopen > 0 && th.fin > 0)
                            {
                                tcp_fastopen_active_disable(tp);
                            }
                            tcp_done(tp);
                            NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPABORTONDATA, 1);
                            return skb_drop_reason.SKB_DROP_REASON_TCP_ABORT_ON_DATA;
                        }

                        tmo = tcp_fin_time(tp);
                        if (tmo > TCP_TIMEWAIT_LEN)
                        {
                            inet_csk_reset_keepalive_timer(tp, tmo - TCP_TIMEWAIT_LEN);
                        }
                        else if (th.fin > 0 || sock_owned_by_user(tp))
                        {
                            inet_csk_reset_keepalive_timer(sk, tmo);
                        }
                        else
                        {
                            tcp_time_wait(tp, TCP_FIN_WAIT2, tmo);
                            goto consume;
                        }
                        break;
                    }

            case (byte)TCP_STATE.TCP_CLOSING:
                if (tp.snd_una == tp.write_seq)
                {
                    tcp_time_wait(tp, TCP_TIME_WAIT, 0);
                    goto consume;
                }
                break;

            case (byte)TCP_STATE.TCP_LAST_ACK:
                if (tp.snd_una == tp.write_seq)
                {
                    tcp_update_metrics(tp);
                    tcp_done(tp);
                    goto consume;
                }
                break;
        }
        
        switch ((TCP_STATE)tp.sk_state)
        {
            case TCP_STATE.TCP_CLOSE_WAIT:
            case TCP_STATE.TCP_CLOSING:
            case TCP_STATE.TCP_LAST_ACK:
                    if (!before(TCP_SKB_CB(skb).seq, tp.rcv_nxt))
                    {
                        break;
                    }
                break;
            case TCP_STATE.TCP_FIN_WAIT1:
            case TCP_STATE.TCP_FIN_WAIT2:
                    break;
            case TCP_STATE.TCP_ESTABLISHED:
                tcp_data_queue(tp, skb);
                queued = 1;
                break;
        }

        /* tcp_data could move socket to TIME-WAIT */
        if (sk->sk_state != TCP_CLOSE)
        {
            tcp_data_snd_check(sk);
            tcp_ack_snd_check(sk);
        }

        if (!queued)
        {
        discard:
            tcp_drop_reason(sk, skb, reason);
        }
        return 0;

        consume:
        __kfree_skb(skb);
        return 0;
        }

    }

}
