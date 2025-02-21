﻿using System;
using System.Collections.Generic;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal enum tcp_metric_index
    {
        TCP_METRIC_RTT,     /* in ms units */
        TCP_METRIC_RTTVAR,  /* in ms units */
        TCP_METRIC_SSTHRESH,
        TCP_METRIC_CWND,
        TCP_METRIC_REORDERING,

        TCP_METRIC_RTT_US,  /* in usec units */
        TCP_METRIC_RTTVAR_US,   /* in usec units */
        __TCP_METRIC_MAX,
    }

    internal class tcp_fastopen_cookie
    {
        public long[] val = new long[2];
        public sbyte len;
        public bool exp;   /* In RFC6994 experimental option format */
    }

    internal class tcp_fastopen_metrics
    {
        public ushort mss;
        public ushort syn_loss;        /* Recurring Fast Open SYN losses */
        public ushort try_exp;      /* Request w/ exp. option (once) */
        public long last_syn_loss;    /* Last Fast Open SYN loss */
        public tcp_fastopen_cookie cookie;
    }

    internal class tcp_metrics_block
    {
        public tcp_metrics_block tcpm_next;
        public net tcpm_net;
        public uint tcpm_saddr;
        public uint tcpm_daddr;
        public long tcpm_stamp;
        public uint tcpm_lock;
        public uint[] tcpm_vals = new uint[LinuxTcpFunc.TCP_METRIC_MAX_KERNEL + 1];
        public tcp_fastopen_metrics tcpm_fastopen;
    }

    internal partial class LinuxTcpFunc
    {
        static readonly Dictionary<uint, tcp_metrics_block> tcp_metrics_dic = new Dictionary<uint, tcp_metrics_block>();

        static void tcp_metrics_init()
        {
            
        }

        static tcp_metrics_block __tcp_get_metrics(uint daddr)
        {
            tcp_metrics_block tm;
            if (tcp_metrics_dic.TryGetValue(daddr, out tm))
            {
                return tm;
            }
            return null;
        }

        static tcp_metrics_block tcp_get_metrics(tcp_sock tp)
        {
            uint daddr = tp.inet_daddr;
            var tm = __tcp_get_metrics(daddr);
            if (tm == null)
            {
                tm = new tcp_metrics_block();
                tcp_metrics_dic.Add(daddr, tm);
            }
            else
            {
                tcpm_check_stamp(tm);
            }
            return tm;
        }

        static void tcp_metric_set(tcp_metrics_block tm,  tcp_metric_index idx, uint val)
        {
            tm.tcpm_vals[(int)idx] = val;
        }

        static bool tcp_metric_locked(tcp_metrics_block tm, tcp_metric_index idx)
        {
            return BoolOk(tm.tcpm_lock & (1 << (byte)idx));
        }

        static uint tcp_metric_get(tcp_metrics_block tm, tcp_metric_index idx)
        {
            return tm.tcpm_vals[(int)idx];
        }

        static void tcpm_suck_dst(tcp_metrics_block tm, bool fastopen_clear)
        {
            tm.tcpm_stamp = tcp_jiffies32;
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_RTT, 0);
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_RTTVAR, 0);
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_SSTHRESH, 0);
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_CWND, 0);
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_REORDERING,0);

            if (fastopen_clear)
            {
                tm.tcpm_fastopen.mss = 0;
                tm.tcpm_fastopen.syn_loss = 0;
                tm.tcpm_fastopen.try_exp = 0;
                tm.tcpm_fastopen.cookie.exp = false;
                tm.tcpm_fastopen.cookie.len = 0;
            }
        }

        static void tcpm_check_stamp(tcp_metrics_block tm)
        {
            if (tm == null)
            {
                return;
            }
            long limit = tm.tcpm_stamp + TCP_METRICS_TIMEOUT;
            if ((long)(tcp_jiffies32 - limit) > 0)
            {
                tcpm_suck_dst(tm, false);
            }
        }

        static void tcp_init_metrics(tcp_sock tp)
        {
            net net = sock_net(tp);
            tcp_metrics_block tm;
            uint val, crtt = 0;

            tp.snd_ssthresh = TCP_INFINITE_SSTHRESH;
            tm = tcp_get_metrics(tp);
            if (tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_CWND))
            {
                tp.snd_cwnd_clamp = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_CWND);
            }

            val = net.ipv4.sysctl_tcp_no_ssthresh_metrics_save > 0 ? 0 : tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_SSTHRESH);
            if (val > 0)
            {
                tp.snd_ssthresh = val;
                if (tp.snd_ssthresh > tp.snd_cwnd_clamp)
                {
                    tp.snd_ssthresh = tp.snd_cwnd_clamp;
                }
            }

            val = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_REORDERING);
            if (val > 0 && tp.reordering != val)
            {
                tp.reordering = val;
            }
            crtt = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_RTT);

        reset:
            {
                if (crtt > tp.srtt_us)
                {
                    crtt /= (8 * 1000);
                    tp.icsk_rto = crtt + Math.Max(2 * crtt, tcp_rto_min(tp));
                }
                else if (tp.srtt_us == 0)
                {
                    tp.rttvar_us = TCP_TIMEOUT_FALLBACK;
                    tp.mdev_us = tp.mdev_max_us = tp.rttvar_us;
                    tp.icsk_rto = TCP_TIMEOUT_FALLBACK;
                }
            }
        }

        static void tcp_update_metrics(tcp_sock tp)
        {
            net net = sock_net(tp);
            tcp_metrics_block tm;
            long rtt;
            uint val;
            int m;

            if (net.ipv4.sysctl_tcp_nometrics_save > 0)
            {
                return;
            }

            tm = tcp_get_metrics(tp);
            if (tp.icsk_backoff > 0 || tp.srtt_us == 0)
            {
                if (tm != null && !tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_RTT))
                {
                    tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_RTT, 0);
                }
                return;
            }

            rtt = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_RTT);
            m = (int)(rtt - tp.srtt_us);

            if (!tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_RTT))
            {
                if (m <= 0)
                {
                    rtt = tp.srtt_us;
                }
                else
                {
                    rtt -= (m >> 3);
                }

                tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_RTT, (uint)rtt);
            }

            if (!tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_RTTVAR))
            {
                long var;

                if (m < 0)
                {
                    m = -m;
                }

                m >>= 1;
                if (m < tp.mdev_us)
                {
                    m = (int)tp.mdev_us;
                }

                var = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_RTTVAR);
                if (m >= var)
                {
                    var = m;
                }
                else
                {
                    var -= (var - m) >> 2;
                }

                tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_RTTVAR, (uint)var);
            }

            if (tcp_in_initial_slowstart(tp))
            {
                if (net.ipv4.sysctl_tcp_no_ssthresh_metrics_save == 0 && !tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_SSTHRESH))
                {
                    val = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_SSTHRESH);
                    if (val > 0 && (tcp_snd_cwnd(tp) >> 1) > val)
                    {
                        tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_SSTHRESH, tcp_snd_cwnd(tp) >> 1);
                    }
                }
                if (!tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_CWND))
                {
                    val = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_CWND);
                    if (tcp_snd_cwnd(tp) > val)
                    {
                        tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_CWND, tcp_snd_cwnd(tp));
                    }
                }
            }
            else if (!tcp_in_slow_start(tp) && tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Open)
            {
                if (net.ipv4.sysctl_tcp_no_ssthresh_metrics_save == 0 && !tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_SSTHRESH))
                {
                    tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_SSTHRESH, Math.Max(tcp_snd_cwnd(tp) >> 1, tp.snd_ssthresh));
                }

                if (!tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_CWND))
                {
                    val = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_CWND);
                    tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_CWND, (val + tcp_snd_cwnd(tp)) >> 1);
                }
            }
            else
            {
                if (!tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_CWND))
                {
                    val = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_CWND);
                    tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_CWND, (val + tp.snd_ssthresh) >> 1);
                }

                if (net.ipv4.sysctl_tcp_no_ssthresh_metrics_save == 0 && !tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_SSTHRESH))
                {
                    val = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_SSTHRESH);
                    if (val > 0 && tp.snd_ssthresh > val)
                    {
                        tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_SSTHRESH, tp.snd_ssthresh);
                    }
                }

                if (!tcp_metric_locked(tm, tcp_metric_index.TCP_METRIC_REORDERING))
                {
                    val = tcp_metric_get(tm, tcp_metric_index.TCP_METRIC_REORDERING);
                    if (val < tp.reordering && tp.reordering != net.ipv4.sysctl_tcp_reordering)
                    {
                        tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_REORDERING, tp.reordering);
                    }
                }
            }

            tm.tcpm_stamp = tcp_jiffies32;
        }

    }

}
