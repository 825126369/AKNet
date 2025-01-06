namespace AKNet.LinuxTcp
{
    public class tcp_metrics_block
    {
        public tcp_metrics_block tcpm_next;
        public net tcpm_net;
        public inetpeer_addr tcpm_saddr;
        public inetpeer_addr tcpm_daddr;
        public long tcpm_stamp;
        public uint tcpm_lock;
        public uint tcpm_vals[TCP_METRIC_MAX_KERNEL + 1];
        public tcp_fastopen_metrics tcpm_fastopen;
    }

    internal partial class LinuxTcpFunc
    {
        static void tcp_init_metrics(tcp_sock tp)
        {
            dst_entry dst = __sk_dst_get(tp);
                net net = sock_net(tp);
            tcp_metrics_block tm;
            uint val, crtt = 0;

                sk_dst_confirm(sk);
                /* ssthresh may have been reduced unnecessarily during.
                 * 3WHS. Restore it back to its initial default.
                 */
                tp->snd_ssthresh = TCP_INFINITE_SSTHRESH;
	        if (!dst)
		        goto reset;

	        rcu_read_lock();
                tm = tcp_get_metrics(sk, dst, false);
	        if (!tm) {
		        rcu_read_unlock();
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
