using System.Collections.Generic;
using System.Security.Cryptography;

namespace AKNet.LinuxTcp
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

    public class tcp_fastopen_cookie
    {
        public long[] val = new long[2];
        public byte len;
        public bool exp;   /* In RFC6994 experimental option format */
    }

    public class tcp_fastopen_metrics
    {
        public ushort mss;
        public ushort syn_loss;        /* Recurring Fast Open SYN losses */
        public ushort try_exp;      /* Request w/ exp. option (once) */
        public long last_syn_loss;    /* Last Fast Open SYN loss */
        public tcp_fastopen_cookie cookie;
    }

    public class tcp_metrics_block
    {
        public tcp_metrics_block tcpm_next;
        public net tcpm_net;
        public string tcpm_saddr;
        public string tcpm_daddr;
        public long tcpm_stamp;
        public uint tcpm_lock;
        public uint[] tcpm_vals = new uint[LinuxTcpFunc.TCP_METRIC_MAX_KERNEL + 1];
        public tcp_fastopen_metrics tcpm_fastopen;
    }

    internal partial class LinuxTcpFunc
    {
        static Dictionary<string, tcp_metrics_block> tcp_metrics_dic = new Dictionary<string, tcp_metrics_block>();

        static tcp_metrics_block __tcp_get_metrics(string daddr)
        {
            tcp_metrics_block tm;
            if (tcp_metrics_dic.TryGetValue(daddr, out tm))
            {
                return tm;
            }
            return null;
        }

        static tcp_metrics_block tcp_get_metrics(tcp_sock tp, dst_entry dst, bool create)
        {
            string daddr = string.Empty;
            var tm = __tcp_get_metrics(daddr);
            if (tm == null)
            {
                tm = new tcp_metrics_block();
            }
            else
            {
                tcpm_check_stamp(tm, dst);
            }
	        return tm;
        }

        static void tcp_metric_set(tcp_metrics_block tm,  tcp_metric_index idx, uint val)
        {
            tm.tcpm_vals[(int)idx] = val;
        }

        static void tcpm_suck_dst(tcp_metrics_block tm, dst_entry dst, bool fastopen_clear)
        {
            uint msval;
            uint val;

            tm.tcpm_stamp = tcp_jiffies32;
            val = 0;

            if (dst_metric_locked(dst, RTAX_RTT))
            {
                val |= 1 << (byte)tcp_metric_index.TCP_METRIC_RTT;
            }
            if (dst_metric_locked(dst, RTAX_RTTVAR))
            {
                val |= 1 << (byte)tcp_metric_index.TCP_METRIC_RTTVAR;
            }
            if (dst_metric_locked(dst, RTAX_SSTHRESH))
            {
                val |= 1 << (byte)tcp_metric_index.TCP_METRIC_SSTHRESH;
            }
            if (dst_metric_locked(dst, RTAX_CWND))
            {
                val |= 1 << (byte)tcp_metric_index.TCP_METRIC_CWND;
            }
            if (dst_metric_locked(dst, RTAX_REORDERING))
            {
                val |= 1 << (byte)tcp_metric_index.TCP_METRIC_REORDERING;
            }

            tm.tcpm_lock = val;
            msval = (uint)dst_metric_raw(dst, RTAX_RTT);

            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_RTT, (uint)(msval * USEC_PER_MSEC));

            msval = (uint)dst_metric_raw(dst, RTAX_RTTVAR);
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_RTTVAR, (uint)(msval * USEC_PER_MSEC));
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_SSTHRESH, (uint)dst_metric_raw(dst, RTAX_SSTHRESH));
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_CWND, (uint)dst_metric_raw(dst, RTAX_CWND));
            tcp_metric_set(tm, tcp_metric_index.TCP_METRIC_REORDERING, (uint)dst_metric_raw(dst, RTAX_REORDERING));

            if (fastopen_clear)
            {
                tm.tcpm_fastopen.mss = 0;
                tm.tcpm_fastopen.syn_loss = 0;
                tm.tcpm_fastopen.try_exp = 0;
                tm.tcpm_fastopen.cookie.exp = false;
                tm.tcpm_fastopen.cookie.len = 0;
            }
        }

        static void tcpm_check_stamp(tcp_metrics_block tm, dst_entry dst)
        {
            if (tm == null)
            {
                return;
            }
            long limit = tm.tcpm_stamp + TCP_METRICS_TIMEOUT;
            if ((long)(tcp_jiffies32 - limit) > 0)
            {
                tcpm_suck_dst(tm, dst, false);
            }
        }

        static void tcp_init_metrics(tcp_sock tp)
        {
            dst_entry dst = __sk_dst_get(tp);
                net net = sock_net(tp);
            tcp_metrics_block tm;
            uint val, crtt = 0;

            sk_dst_confirm(tp);
            tp.snd_ssthresh = tcp_sock.TCP_INFINITE_SSTHRESH;

            if (dst == null)
            {
                goto reset;
            }

            tm = tcp_get_metrics(tp, dst, false);
	        if (tm == null) 
            {
		        goto reset;
	        }

	        if (tcp_metric_locked(tm, TCP_METRIC_CWND))
		        tp->snd_cwnd_clamp = tcp_metric_get(tm, TCP_METRIC_CWND);

            val = READ_ONCE(net->ipv4.sysctl_tcp_no_ssthresh_metrics_save) ?
	              0 : tcp_metric_get(tm, TCP_METRIC_SSTHRESH);
	        if (val) {
		        tp->snd_ssthresh = val;
		        if (tp->snd_ssthresh > tp->snd_cwnd_clamp)
			        tp->snd_ssthresh = tp->snd_cwnd_clamp;
	        }
        val = tcp_metric_get(tm, TCP_METRIC_REORDERING);
        if (val && tp->reordering != val)
            tp->reordering = val;

        crtt = tcp_metric_get(tm, TCP_METRIC_RTT);
        rcu_read_unlock();
        reset:
        /* The initial RTT measurement from the SYN/SYN-ACK is not ideal
         * to seed the RTO for later data packets because SYN packets are
         * small. Use the per-dst cached values to seed the RTO but keep
         * the RTT estimator variables intact (e.g., srtt, mdev, rttvar).
         * Later the RTO will be updated immediately upon obtaining the first
         * data RTT sample (tcp_rtt_estimator()). Hence the cached RTT only
         * influences the first RTO but not later RTT estimation.
         *
         * But if RTT is not available from the SYN (due to retransmits or
         * syn cookies) or the cache, force a conservative 3secs timeout.
         *
         * A bit of theory. RTT is time passed after "normal" sized packet
         * is sent until it is ACKed. In normal circumstances sending small
         * packets force peer to delay ACKs and calculation is correct too.
         * The algorithm is adaptive and, provided we follow specs, it
         * NEVER underestimate RTT. BUT! If peer tries to make some clever
         * tricks sort of "quick acks" for time long enough to decrease RTT
         * to low value, and then abruptly stops to do it and starts to delay
         * ACKs, wait for troubles.
         */
        if (crtt > tp->srtt_us)
        {
            /* Set RTO like tcp_rtt_estimator(), but from cached RTT. */
            crtt /= 8 * USEC_PER_SEC / HZ;
            inet_csk(sk)->icsk_rto = crtt + max(2 * crtt, tcp_rto_min(sk));
        }
        else if (tp->srtt_us == 0)
        {
            /* RFC6298: 5.7 We've failed to get a valid RTT sample from
             * 3WHS. This is most likely due to retransmission,
             * including spurious one. Reset the RTO back to 3secs
             * from the more aggressive 1sec to avoid more spurious
             * retransmission.
             */
            tp->rttvar_us = jiffies_to_usecs(TCP_TIMEOUT_FALLBACK);
            tp->mdev_us = tp->mdev_max_us = tp->rttvar_us;

            inet_csk(sk)->icsk_rto = TCP_TIMEOUT_FALLBACK;
        }
        }
    }

}
