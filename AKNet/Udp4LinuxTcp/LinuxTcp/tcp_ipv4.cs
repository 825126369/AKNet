/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp4LinuxTcp.Common
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

            net.ipv4.ip_rt_min_pmtu = DEFAULT_MIN_PMTU;
            net.ipv4.ip_rt_mtu_expires = DEFAULT_MTU_EXPIRES;
            net.ipv4.ip_rt_min_advmss = DEFAULT_MIN_ADVMSS;

            return 0;
        }

        public static void tcp_v4_send_check(tcp_sock tp, sk_buff skb)
        {
            __tcp_v4_send_check(skb, tp.inet_saddr, tp.inet_daddr);
        }

        public static void __tcp_v4_send_check(sk_buff skb, uint saddr, uint daddr)
        {
            //tcphdr th = tcp_hdr(skb);
            //th.check = ~tcp_v4_check(skb.nBufferLength, saddr, daddr, 0);
            //skb.csum_start = skb_transport_header(skb) - skb.head;
            //skb.csum_offset = offsetof(tcphdr, check);
        }

        static void tcp_v4_send_reset(tcp_sock tp, sk_buff skb, skb_drop_reason reason)
        {

        }

        public static void tcp_v4_do_rcv(tcp_sock tp, sk_buff skb)
        {
            tcp_rcv_established(tp, skb);
        }

        static void tcp_v4_fill_cb(sk_buff skb, tcphdr th)
        {
            TCP_SKB_CB(skb).seq = th.seq;
            TCP_SKB_CB(skb).end_seq = th.seq + th.tot_len - th.doff;
            TCP_SKB_CB(skb).ack_seq = th.ack_seq;
            TCP_SKB_CB(skb).tcp_flags = tcp_flag_byte(skb);
            TCP_SKB_CB(skb).sacked = 0;
            TCP_SKB_CB(skb).has_rxtstamp = skb.tstamp > 0;
        }

        static void tcp_v4_rcv(tcp_sock tp, sk_buff skb)
        {
            var th = tcp_hdr(skb);
            if (th.doff < sizeof_tcphdr)
            {
                return;
            }

            tcp_v4_fill_cb(skb, th);
            tcp_v4_do_rcv(tp, skb);
        }

        static int tcp_v4_init_sock(tcp_sock tp)
        {
            tcp_init_sock(tp);
            return 0;
        }

        public static int tcp_v4_connect(tcp_sock tp)
        {
            tp.rx_opt.ts_recent = 0;
            tp.rx_opt.ts_recent_stamp = 0;
            tp.rx_opt.mss_clamp = TCP_MSS_DEFAULT;

            tp.write_seq = 0;
            tp.icsk_ext_hdr_len = 0;
            tcp_set_state(tp, TCP_SYN_SENT);
            tcp_connect(tp);
            return 0;
        }
    }

}
