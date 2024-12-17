using System;
using System.Net.Sockets;
using System.Threading;

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
                bool reordering;
                
                tcp_timeout_mark_lost(sk);
                
	        if (tp.icsk_ca_state <= (int)tcp_ca_state.TCP_CA_Disorder ||
	            !after(tp.high_seq, tp.snd_una) ||
	            (tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Loss && tp.icsk_retransmits == 0)) 
            {
		        tp.prior_ssthresh = tcp_current_ssthresh(tp);
                tp.prior_cwnd = tp.snd_cwnd;
                tp.snd_ssthresh = tp.icsk_ca_ops.ssthresh(tp);
                tcp_ca_event_func(tp, tcp_ca_event.CA_EVENT_LOSS);
                tcp_init_undo(tp);
            }

            tcp_snd_cwnd_set(tp, tcp_packets_in_flight(tp) + 1);
            tp->snd_cwnd_cnt   = 0;
	        tp->snd_cwnd_stamp = tcp_jiffies32;

	        /* Timeout in disordered state after receiving substantial DUPACKs
	         * suggests that the degree of reordering is over-estimated.
	         */
	        reordering = READ_ONCE(net->ipv4.sysctl_tcp_reordering);
	        if (icsk->icsk_ca_state <= TCP_CA_Disorder &&
	            tp->sacked_out >= reordering)
		        tp->reordering = min_t(unsigned int, tp->reordering,
                               reordering);

            tcp_set_ca_state(sk, TCP_CA_Loss);
            tp->high_seq = tp->snd_nxt;
	        tp->tlp_high_seq = 0;
	        tcp_ecn_queue_cwr(tp);

            /* F-RTO RFC5682 sec 3.1 step 1: retransmit SND.UNA if no previous
	         * loss recovery is underway except recurring timeout(s) on
	         * the same SND.UNA (sec 3.2). Disable F-RTO on path MTU probing
	         */
            tp->frto = READ_ONCE(net->ipv4.sysctl_tcp_frto) &&
		           (new_recovery || icsk->icsk_retransmits) &&
		           !inet_csk(sk)->icsk_mtup.probe_size;
        }

        public static void tcp_timeout_mark_lost(tcp_sock tp)
        {
            sk_buff skb, head;
	        bool is_reneg;

            head = tcp_rtx_queue_head(sk);
            is_reneg = head && (TCP_SKB_CB(head)->sacked & tcp_skb_cb_sacked_flags.TCPCB_SACKED_ACKED);
	        if (is_reneg) {
		        NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPSACKRENEGING);
                tp->sacked_out = 0;
		        /* Mark SACK reneging until we recover from this loss event. */
		        tp->is_sack_reneg = 1;
	        } else if (tcp_is_reno(tp)) {
		        tcp_reset_reno_sack(tp);
            }

            skb = head;
            skb_rbtree_walk_from(skb) {
                if (is_reneg)
                    TCP_SKB_CB(skb)->sacked &= ~TCPCB_SACKED_ACKED;
                else if (tcp_is_rack(sk) && skb != head &&
                     tcp_rack_skb_timeout(tp, skb, 0) > 0)
                    continue; /* Don't mark recently sent ones lost yet */
                tcp_mark_skb_lost(sk, skb);
            }
            tcp_verify_left_out(tp);
            tcp_clear_all_retrans_hints(tp);
        }

        public static void tcp_init_undo(tcp_sock tp)
        {
            tp.undo_marker = tp.snd_una;
            tp.undo_retrans = tp.retrans_out;

            if (tp.tlp_high_seq > 0 && tp.tlp_retrans > 0)
            {
                tp.undo_retrans++;
            }

            if (tp.undo_retrans == 0)
            {
                tp.undo_retrans = -1;
            }
        }
    }
}
