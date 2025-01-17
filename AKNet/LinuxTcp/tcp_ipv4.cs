/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.LinuxTcp
{
    internal partial class LinuxTcpFunc
    {
        static void tcp_v4_init()
        {
           
        }

        static int tcp_sk_init(net net)
        {
	        net.ipv4.sysctl_tcp_ecn = 2;
	        net.ipv4.sysctl_tcp_ecn_fallback = 1;

	        net.ipv4.sysctl_tcp_base_mss = TCP_BASE_MSS;
	        net.ipv4.sysctl_tcp_min_snd_mss = TCP_MIN_SND_MSS;
	        net.ipv4.sysctl_tcp_probe_threshold = TCP_PROBE_THRESHOLD;
	        net.ipv4.sysctl_tcp_probe_interval = TCP_PROBE_INTERVAL;

	        net.ipv4.sysctl_tcp_keepalive_time = TCP_KEEPALIVE_TIME;
	        net.ipv4.sysctl_tcp_keepalive_probes = TCP_KEEPALIVE_PROBES;
	        net.ipv4.sysctl_tcp_keepalive_intvl = TCP_KEEPALIVE_INTVL;

	        net.ipv4.sysctl_tcp_syn_retries = TCP_SYN_RETRIES;
	        net.ipv4.sysctl_tcp_synack_retries = TCP_SYNACK_RETRIES;
	        net.ipv4.sysctl_tcp_syncookies = 1;
	        net.ipv4.sysctl_tcp_reordering = TCP_FASTRETRANS_THRESH;
	        net.ipv4.sysctl_tcp_retries1 = TCP_RETR1;
	        net.ipv4.sysctl_tcp_retries2 = TCP_RETR2;
	        net.ipv4.sysctl_tcp_orphan_retries = 0;
	        net.ipv4.sysctl_tcp_fin_timeout = TCP_FIN_TIMEOUT;
	        net.ipv4.sysctl_tcp_notsent_lowat = uint.MaxValue;
	        net.ipv4.sysctl_tcp_tw_reuse = 2;
	        net.ipv4.sysctl_tcp_no_ssthresh_metrics_save = 1;

            net.ipv4.sysctl_tcp_sack = 1;
	        net.ipv4.sysctl_tcp_window_scaling = 1;
	        net.ipv4.sysctl_tcp_timestamps = 1;
	        net.ipv4.sysctl_tcp_early_retrans = 3;
	        net.ipv4.sysctl_tcp_recovery = TCP_RACK_LOSS_DETECTION;
	        net.ipv4.sysctl_tcp_slow_start_after_idle = true; /* By default, RFC2861 behavior.  */
	        net.ipv4.sysctl_tcp_retrans_collapse = 1;
	        net.ipv4.sysctl_tcp_max_reordering = 300;
	        net.ipv4.sysctl_tcp_dsack = 1;
	        net.ipv4.sysctl_tcp_app_win = 31;
	        net.ipv4.sysctl_tcp_frto = 2;
	        net.ipv4.sysctl_tcp_moderate_rcvbuf = 1;
	        net.ipv4.sysctl_tcp_tso_win_divisor = 3;
	        net.ipv4.sysctl_tcp_limit_output_bytes = 16 * 65536;
	        net.ipv4.sysctl_tcp_challenge_ack_limit = int.MaxValue;

	        net.ipv4.sysctl_tcp_min_tso_segs = 2;
	        net.ipv4.sysctl_tcp_tso_rtt_log = 9;
	        net.ipv4.sysctl_tcp_min_rtt_wlen = 300;
	        net.ipv4.sysctl_tcp_autocorking = 1;
	        net.ipv4.sysctl_tcp_invalid_ratelimit = HZ/2;
	        net.ipv4.sysctl_tcp_pacing_ss_ratio = 200;
	        net.ipv4.sysctl_tcp_pacing_ca_ratio = 120;

            Array.Copy(init_net.ipv4.sysctl_tcp_rmem, net.ipv4.sysctl_tcp_rmem, init_net.ipv4.sysctl_tcp_rmem.Length);
            Array.Copy(init_net.ipv4.sysctl_tcp_wmem, net.ipv4.sysctl_tcp_wmem, init_net.ipv4.sysctl_tcp_wmem.Length);

            net.ipv4.sysctl_tcp_comp_sack_delay_ns = NSEC_PER_MSEC;
            net.ipv4.sysctl_tcp_comp_sack_nr = 44;
	        net.ipv4.sysctl_tcp_backlog_ack_defer = 1;
		    net.ipv4.tcp_congestion_control = tcp_reno;
	        net.ipv4.sysctl_tcp_syn_linear_timeouts = 4;
	        net.ipv4.sysctl_tcp_shrink_window = 0;
	        net.ipv4.sysctl_tcp_pingpong_thresh = 1;
	        net.ipv4.sysctl_tcp_rto_min_us = TCP_RTO_MIN;

	        return 0;
        }

        public static void tcp_v4_send_check(tcp_sock tp, sk_buff skb)
        {
            __tcp_v4_send_check(skb, tp.inet_saddr, tp.inet_daddr);
        }

        public static void __tcp_v4_send_check(sk_buff skb, uint saddr, uint daddr)
        {
            //tcphdr th = tcp_hdr(skb);
            //th.check = ~tcp_v4_check(skb.len, saddr, daddr, 0);
            //skb.csum_start = skb_transport_header(skb) - skb.head;
            //skb.csum_offset = offsetof(tcphdr, check);
        }

        static void tcp_v4_send_reset(tcp_sock tp, sk_buff skb, skb_drop_reason reason)
        {

        }

        public static int tcp_v4_do_rcv(tcp_sock tp, sk_buff skb)
        {
            skb_drop_reason reason = skb_drop_reason.SKB_DROP_REASON_NOT_SPECIFIED;
            if (tp.sk_state == TCP_ESTABLISHED)
            {
                tcp_rcv_established(tp, skb);
                return 0;
            }

            if (tcp_checksum_complete(skb))
            {
                goto csum_err;
            }

            if (tp.sk_state == TCP_LISTEN)
            {

            }

            reason = tcp_rcv_state_process(tp, skb);
            if (reason > 0)
            {
                goto reset;
            }
            return 0;

        reset:
            tcp_v4_send_reset(tp, skb, reason);
        discard:
            sk_skb_reason_drop(tp, skb, reason);
            return 0;

        csum_err:
            reason = skb_drop_reason.SKB_DROP_REASON_TCP_CSUM;
            TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_CSUMERRORS, 1);
            TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_INERRS, 1);
            goto discard;
        }

        static void tcp_v4_fill_cb(sk_buff skb, iphdr iph, tcphdr th)
        {
            TCP_SKB_CB(skb).header.h4 = IPCB(skb);

            TCP_SKB_CB(skb).seq = th.seq;
            TCP_SKB_CB(skb).end_seq = (uint)(TCP_SKB_CB(skb).seq + th.syn + th.fin + skb.len - th.doff * 4);
            TCP_SKB_CB(skb).ack_seq = th.ack_seq;
            TCP_SKB_CB(skb).tcp_flags = tcp_flag_byte(skb);
            TCP_SKB_CB(skb).ip_dsfield = ipv4_get_dsfield(iph);
            TCP_SKB_CB(skb).sacked = 0;
            TCP_SKB_CB(skb).has_rxtstamp = skb.tstamp > 0 || skb_hwtstamps(skb).hwtstamp > 0;
        }

        static bool tcp_add_backlog(tcp_sock tp, sk_buff skb)
        {
            uint tail_gso_size, tail_gso_segs;
            skb_shared_info shinfo;
            tcphdr th;
            tcphdr thtail;
            sk_buff tail;
            uint hdrlen;
            bool fragstolen;
            uint gso_segs;
            uint gso_size;
            ulong limit;
            int delta = 0;

            if (tcp_checksum_complete(skb))
            {
                TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_CSUMERRORS, 1);
                TCP_ADD_STATS(sock_net(tp), TCPMIB.TCP_MIB_INERRS, 1);
                return true;
            }

            th = tcp_hdr(skb);
            hdrlen = (uint)th.doff * 4;

            tail = tp.sk_backlog.tail;
            if (tail == null)
            {
                goto no_coalesce;
            }

            thtail = tcp_hdr(tail);

            if (TCP_SKB_CB(tail).end_seq != TCP_SKB_CB(skb).seq ||
                TCP_SKB_CB(tail).ip_dsfield != TCP_SKB_CB(skb).ip_dsfield ||
                BoolOk((TCP_SKB_CB(tail).tcp_flags | TCP_SKB_CB(skb).tcp_flags) & (TCPHDR_SYN | TCPHDR_RST | TCPHDR_URG)) ||
                !BoolOk((TCP_SKB_CB(tail).tcp_flags & TCP_SKB_CB(skb).tcp_flags) & TCPHDR_ACK) ||
                BoolOk((TCP_SKB_CB(tail).tcp_flags ^ TCP_SKB_CB(skb).tcp_flags) & (TCPHDR_ECE | TCPHDR_CWR)) ||
                !tcp_skb_can_collapse_rx(tail, skb) ||
                thtail.doff != th.doff)
            {
                skb.mBuffer.AsSpan().Slice(sizeof_tcphdr, (int)(hdrlen - sizeof_tcphdr)).CopyTo(tail.mBuffer.AsSpan().Slice(sizeof_tcphdr));
                goto no_coalesce;
            }

            shinfo = skb_shinfo(skb);
            gso_size = (uint)(shinfo.gso_size > 0 ? shinfo.gso_size : skb.len);
            gso_segs = (uint)(shinfo.gso_segs > 0 ? shinfo.gso_segs : 1);

            shinfo = skb_shinfo(tail);
            tail_gso_size = (uint)(shinfo.gso_size > 0 ? shinfo.gso_size : (tail.len - hdrlen));
            tail_gso_segs = (uint)(shinfo.gso_segs > 0 ? shinfo.gso_segs : 1);

            if (skb_try_coalesce(tail, skb))
            {
                TCP_SKB_CB(tail).end_seq = TCP_SKB_CB(skb).end_seq;

                if (!before(TCP_SKB_CB(skb).ack_seq, TCP_SKB_CB(tail).ack_seq))
                {
                    TCP_SKB_CB(tail).ack_seq = TCP_SKB_CB(skb).ack_seq;
                    thtail.window = th.window;
                }

                thtail.fin |= th.fin;
                TCP_SKB_CB(tail).tcp_flags |= TCP_SKB_CB(skb).tcp_flags;

                if (TCP_SKB_CB(skb).has_rxtstamp)
                {
                    TCP_SKB_CB(tail).has_rxtstamp = true;
                    tail.tstamp = skb.tstamp;
                    skb_hwtstamps(tail).hwtstamp = skb_hwtstamps(skb).hwtstamp;
                }

                shinfo.gso_size = (ushort)Math.Max(gso_size, tail_gso_size);
                shinfo.gso_segs = (ushort)Math.Min(gso_segs + tail_gso_segs, 0xFFFF);

                tp.sk_backlog.len += (int)delta;
                NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPBACKLOGCOALESCE, 1);
                return false;
            }

        no_coalesce:
            limit = (ulong)(tp.sk_rcvbuf << 1);
            limit += (ulong)(tp.sk_sndbuf >> 1);
            limit += 64 * 1024;

            limit = Math.Min(limit, uint.MaxValue);

            if (sk_add_backlog(tp, skb) > 0)
            {
                NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPBACKLOGDROP, 1);
                return true;
            }
            return false;
        }

        static void tcp_v4_rcv(tcp_sock tp, sk_buff skb)
        {
            if (skb.pkt_type != PACKET_HOST)
            {
                return;
            }

            var th = tcp_hdr(skb);
            if (th.doff < sizeof_tcphdr / 4)
            {
                return;
            }

            if (skb_checksum_init(skb, IPPROTO_TCP, inet_compute_pseudo) > 0)
            {
                return;
            }

            var iph = ip_hdr(skb);
            tcp_v4_fill_cb(skb, iph, th);
            tcp_segs_in(tp, skb);
            if (true)
            {
                tcp_v4_do_rcv(tp, skb);
            }
            else
            {
                tcp_add_backlog(tp, skb);
            }
        }

        static int tcp_v4_init_sock(tcp_sock tp)
        {
            tcp_init_sock(tp);
            //tp.icsk_af_ops = ipv4_specific;
            return 0;
        }
    }

}
