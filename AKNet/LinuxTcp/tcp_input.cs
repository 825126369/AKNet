﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
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


    }

}
