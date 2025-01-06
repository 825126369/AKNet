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

        static skb_drop_reason tcp_rcv_state_process(tcp_sock tp, sk_buff skb)
        {
                tcphdr th = skb.hdr;
                request_sock req;
	            int queued = 0;
                skb_drop_reason reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;
                
	        switch (sk->sk_state) {
	        case TCP_CLOSE:
		        SKB_DR_SET(reason, TCP_CLOSE);
		        goto discard;

	        case TCP_LISTEN:
		        if (th->ack)
			        return SKB_DROP_REASON_TCP_FLAGS;

		        if (th->rst) {
			        SKB_DR_SET(reason, TCP_RESET);
			        goto discard;
		        }
		        if (th->syn) {
			        if (th->fin) {
				        SKB_DR_SET(reason, TCP_FLAGS);
				        goto discard;
			        }
        /* It is possible that we process SYN packets from backlog,
         * so we need to make sure to disable BH and RCU right there.
         */
        rcu_read_lock();
        local_bh_disable();
        icsk->icsk_af_ops->conn_request(sk, skb);
        local_bh_enable();
        rcu_read_unlock();

        consume_skb(skb);
        return 0;
		        }
		        SKB_DR_SET(reason, TCP_FLAGS);
        goto discard;


            case TCP_SYN_SENT:
            tp->rx_opt.saw_tstamp = 0;
            tcp_mstamp_refresh(tp);
            queued = tcp_rcv_synsent_state_process(sk, skb, th);
            if (queued >= 0)
                return queued;

            /* Do step6 onward by hand. */
            tcp_urg(sk, skb, th);
            __kfree_skb(skb);
            tcp_data_snd_check(sk);
            return 0;
        }

        tcp_mstamp_refresh(tp);
        tp->rx_opt.saw_tstamp = 0;
        req = rcu_dereference_protected(tp->fastopen_rsk,
                        lockdep_sock_is_held(sk));
        if (req)
        {
            bool req_stolen;

            WARN_ON_ONCE(sk->sk_state != TCP_SYN_RECV &&
                sk->sk_state != TCP_FIN_WAIT1);

            if (!tcp_check_req(sk, skb, req, true, &req_stolen))
            {
                SKB_DR_SET(reason, TCP_FASTOPEN);
                goto discard;
            }
        }

        if (!th->ack && !th->rst && !th->syn)
        {
            SKB_DR_SET(reason, TCP_FLAGS);
            goto discard;
        }
        if (!tcp_validate_incoming(sk, skb, th, 0))
            return 0;

        /* step 5: check the ACK field */
        reason = tcp_ack(sk, skb, FLAG_SLOWPATH |
                      FLAG_UPDATE_TS_RECENT |
                      FLAG_NO_CHALLENGE_ACK);

        if ((int)reason <= 0)
        {
            if (sk->sk_state == TCP_SYN_RECV)
            {
                /* send one RST */
                if (!reason)
                    return SKB_DROP_REASON_TCP_OLD_ACK;
                return -reason;
            }
            /* accept old ack during closing */
            if ((int)reason < 0)
            {
                tcp_send_challenge_ack(sk);
                reason = -reason;
                goto discard;
            }
        }
        SKB_DR_SET(reason, NOT_SPECIFIED);
        switch (sk->sk_state)
        {
            case TCP_SYN_RECV:
                tp->delivered++; /* SYN-ACK delivery isn't tracked in tcp_ack */
                if (!tp->srtt_us)
                    tcp_synack_rtt_meas(sk, req);

                if (req)
                {
                    tcp_rcv_synrecv_state_fastopen(sk);
                }
                else
                {
                    tcp_try_undo_spurious_syn(sk);
                    tp->retrans_stamp = 0;
                    tcp_init_transfer(sk, BPF_SOCK_OPS_PASSIVE_ESTABLISHED_CB,
                              skb);
                    WRITE_ONCE(tp->copied_seq, tp->rcv_nxt);
                }
                tcp_ao_established(sk);
                smp_mb();
                tcp_set_state(sk, TCP_ESTABLISHED);
                sk->sk_state_change(sk);

                /* Note, that this wakeup is only for marginal crossed SYN case.
		         * Passively open sockets are not waked up, because
		         * sk->sk_sleep == NULL and sk->sk_socket == NULL.
		         */
                if (sk->sk_socket)
                    sk_wake_async(sk, SOCK_WAKE_IO, POLL_OUT);

                tp->snd_una = TCP_SKB_CB(skb)->ack_seq;
                tp->snd_wnd = ntohs(th->window) << tp->rx_opt.snd_wscale;
                tcp_init_wl(tp, TCP_SKB_CB(skb)->seq);

                if (tp->rx_opt.tstamp_ok)
                    tp->advmss -= TCPOLEN_TSTAMP_ALIGNED;

                if (!inet_csk(sk)->icsk_ca_ops->cong_control)
                    tcp_update_pacing_rate(sk);

                /* Prevent spurious tcp_cwnd_restart() on first data packet */
                tp->lsndtime = tcp_jiffies32;

                tcp_initialize_rcv_mss(sk);
                tcp_fast_path_on(tp);
                if (sk->sk_shutdown & SEND_SHUTDOWN)
                    tcp_shutdown(sk, SEND_SHUTDOWN);
                break;

            case TCP_FIN_WAIT1:
                {
                    int tmo;

                    if (req)
                        tcp_rcv_synrecv_state_fastopen(sk);

                    if (tp->snd_una != tp->write_seq)
                        break;

                    tcp_set_state(sk, TCP_FIN_WAIT2);
                    WRITE_ONCE(sk->sk_shutdown, sk->sk_shutdown | SEND_SHUTDOWN);

                    sk_dst_confirm(sk);

                    if (!sock_flag(sk, SOCK_DEAD))
                    {
                        /* Wake up lingering close() */
                        sk->sk_state_change(sk);
                        break;
                    }

                    if (READ_ONCE(tp->linger2) < 0)
                    {
                        tcp_done(sk);
                        NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPABORTONDATA);
                        return SKB_DROP_REASON_TCP_ABORT_ON_DATA;
                    }
                    if (TCP_SKB_CB(skb)->end_seq != TCP_SKB_CB(skb)->seq &&
                        after(TCP_SKB_CB(skb)->end_seq - th->fin, tp->rcv_nxt))
                    {
                        /* Receive out of order FIN after close() */
                        if (tp->syn_fastopen && th->fin)
                            tcp_fastopen_active_disable(sk);
                        tcp_done(sk);
                        NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPABORTONDATA);
                        return SKB_DROP_REASON_TCP_ABORT_ON_DATA;
                    }

                    tmo = tcp_fin_time(sk);
                    if (tmo > TCP_TIMEWAIT_LEN)
                    {
                        inet_csk_reset_keepalive_timer(sk, tmo - TCP_TIMEWAIT_LEN);
                    }
                    else if (th->fin || sock_owned_by_user(sk))
                    {
                        /* Bad case. We could lose such FIN otherwise.
                         * It is not a big problem, but it looks confusing
                         * and not so rare event. We still can lose it now,
                         * if it spins in bh_lock_sock(), but it is really
                         * marginal case.
                         */
                        inet_csk_reset_keepalive_timer(sk, tmo);
                    }
                    else
                    {
                        tcp_time_wait(sk, TCP_FIN_WAIT2, tmo);
                        goto consume;
                    }
                    break;
                }

            case TCP_CLOSING:
                if (tp->snd_una == tp->write_seq)
                {
                    tcp_time_wait(sk, TCP_TIME_WAIT, 0);
                    goto consume;
                }
                break;

            case TCP_LAST_ACK:
                if (tp->snd_una == tp->write_seq)
                {
                    tcp_update_metrics(sk);
                    tcp_done(sk);
                    goto consume;
                }
                break;
        }

        /* step 6: check the URG bit */
        tcp_urg(sk, skb, th);

        /* step 7: process the segment text */
        switch (sk->sk_state)
        {
            case TCP_CLOSE_WAIT:
            case TCP_CLOSING:
            case TCP_LAST_ACK:
                if (!before(TCP_SKB_CB(skb)->seq, tp->rcv_nxt))
                {
                    /* If a subflow has been reset, the packet should not
			         * continue to be processed, drop the packet.
			         */
                    if (sk_is_mptcp(sk) && !mptcp_incoming_options(sk, skb))
                        goto discard;
                    break;
                }
                fallthrough;
            case TCP_FIN_WAIT1:
            case TCP_FIN_WAIT2:
                /* RFC 793 says to queue data in these states,
		         * RFC 1122 says we MUST send a reset.
		         * BSD 4.4 also does reset.
		         */
                if (sk->sk_shutdown & RCV_SHUTDOWN)
                {
                    if (TCP_SKB_CB(skb)->end_seq != TCP_SKB_CB(skb)->seq &&
                        after(TCP_SKB_CB(skb)->end_seq - th->fin, tp->rcv_nxt))
                    {
                        NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPABORTONDATA);
                        tcp_reset(sk, skb);
                        return SKB_DROP_REASON_TCP_ABORT_ON_DATA;
                    }
                }
                fallthrough;
            case TCP_ESTABLISHED:
                tcp_data_queue(sk, skb);
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
