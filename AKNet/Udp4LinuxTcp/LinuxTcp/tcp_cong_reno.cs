using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        /* Slow start is used when congestion window is no greater than the slow start
         * threshold. We base on RFC2581 and also handle stretch ACKs properly.
         * We do not implement RFC3465 Appropriate Byte Counting (ABC) per se but
         * something better;) a packet is only considered (s)acked in its entirety to
         * defend the ACK attacks described in the RFC. Slow start processes a stretch
         * ACK of degree N as if N acks of degree 1 are received back to back except
         * ABC caps N to 2. Slow start exits when cwnd grows over ssthresh and
         * returns the leftover acks to adjust cwnd in congestion avoidance mode.
         */

        static uint tcp_slow_start(tcp_sock tp, uint acked)
        {
            uint cwnd = Math.Min(tcp_snd_cwnd(tp) + acked, tp.snd_ssthresh);

            acked -= cwnd - tcp_snd_cwnd(tp);
            tcp_snd_cwnd_set(tp, Math.Min(cwnd, tp.snd_cwnd_clamp));

            return acked;
        }

        static void tcp_cong_avoid_ai(tcp_sock tp, uint w, uint acked)
        {
            if (tp.snd_cwnd_cnt >= w)
            {
                tp.snd_cwnd_cnt = 0;
                tcp_snd_cwnd_set(tp, tcp_snd_cwnd(tp) + 1);
            }

            tp.snd_cwnd_cnt += acked;
            if (tp.snd_cwnd_cnt >= w)
            {
                uint delta = tp.snd_cwnd_cnt / w;

                tp.snd_cwnd_cnt -= delta * w;
                tcp_snd_cwnd_set(tp, tcp_snd_cwnd(tp) + delta);
            }
            tcp_snd_cwnd_set(tp, Math.Min(tcp_snd_cwnd(tp), tp.snd_cwnd_clamp));
        }

        static void tcp_reno_cong_avoid(tcp_sock tp, uint ack, uint acked)
        {

            if (!tcp_is_cwnd_limited(tp))
            {
                return;
            }

            if (tcp_in_slow_start(tp))
            {
                acked = tcp_slow_start(tp, acked);
                if (acked == 0)
                {
                    return;
                }
            }

            tcp_cong_avoid_ai(tp, tcp_snd_cwnd(tp), acked);
        }

        static uint tcp_reno_ssthresh(tcp_sock tp)
        {
            return Math.Max(tcp_snd_cwnd(tp) >> 1, 2U);
        }

        static uint tcp_reno_undo_cwnd(tcp_sock tp)
        {

            return Math.Max(tcp_snd_cwnd(tp), tp.prior_cwnd);
        }

        static tcp_congestion_ops tcp_reno = new tcp_congestion_ops()
        {
            flags = TCP_CONG_NON_RESTRICTED,
            name = "reno",
            ssthresh = tcp_reno_ssthresh,
            cong_avoid = tcp_reno_cong_avoid,
            undo_cwnd = tcp_reno_undo_cwnd
        };
    }
}