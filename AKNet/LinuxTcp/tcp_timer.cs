using System;
using System.Diagnostics;

namespace AKNet.LinuxTcp
{
    /*
		 * tcp_write_timer //管理TCP发送窗口，并处理重传机制。
		 * tcp_delack_timer //实现延迟ACK（Delayed ACK），减少不必要的ACK流量。
		 * tcp_keepalive_timer;//用于检测长时间空闲的TCP连接是否仍然活跃。
		 * pacing_timer//实施速率控制（Pacing），优化数据包的发送速率，避免突发流量导致的网络拥塞。
		 * compressed_ack_timer //优化ACK报文的发送，特别是在高带宽延迟网络环境中。
	*/

    internal static partial class LinuxTcpFunc
	{
		static readonly Stopwatch mStopwatch = Stopwatch.StartNew();

		//它用于确保 TCP 的重传超时（RTO, Retransmission Timeout）不会超过用户设定的连接超时时间。
		public static uint tcp_clamp_rto_to_user_timeout(tcp_sock tp)
		{
			long user_timeout = tp.icsk_user_timeout;
			if (user_timeout == 0)
			{
				return tp.icsk_rto;
			}

			long elapsed = tcp_time_stamp_ts(tp) - tp.retrans_stamp;
			long remaining = user_timeout - elapsed;
			if (remaining <= 0)
			{
				return 1;
			}

			return tp.icsk_rto;
		}
		
		public static void tcp_write_err(tcp_sock tp)
		{
			tcp_done_with_error(tp, tp.sk_err_soft > 0 ? tp.sk_err_soft : (int)ErrorCode.ETIMEDOUT);
		}

		static int tcp_out_of_resources(tcp_sock tp, bool do_reset)
		{
			return 0;
		}

		static long ilog2(long value)
		{
			if (value <= 0) throw new ArgumentException("Value must be positive.", nameof(value));
			return (long)Math.Floor(Math.Log(value, 2));
		}

		static long tcp_model_timeout(tcp_sock tp, long boundary, long rto_base)
		{
			long linear_backoff_thresh, timeout;
			linear_backoff_thresh = ilog2(tcp_sock.TCP_RTO_MAX / rto_base);
			if (boundary <= linear_backoff_thresh)
			{
				timeout = ((2 << (int)boundary) - 1) * rto_base;
			}
			else
			{
				timeout = ((2 << (int)linear_backoff_thresh) - 1) * rto_base + (boundary - linear_backoff_thresh) * tcp_sock.TCP_RTO_MAX;
			}
			return timeout;
		}

		static bool retransmits_timed_out(tcp_sock tp, long boundary, long timeout)
		{
			long start_ts, delta;
			if (tp.icsk_retransmits == 0)
			{
				return false;
			}
			start_ts = tp.retrans_stamp;
			if (timeout == 0)
			{
				long rto_base = tcp_sock.TCP_RTO_MIN;
				if (((1 << tp.sk_state) & (TCPF_STATE.TCPF_SYN_SENT | TCPF_STATE.TCPF_SYN_RECV)) > 0)
				{
					rto_base = tcp_timeout_init(tp);
				}
				timeout = tcp_model_timeout(tp, boundary, rto_base);
			}
			return (int)(tp.tcp_mstamp - start_ts - timeout) >= 0;
		}

		public static int tcp_write_timeout(tcp_sock tp)
		{
			bool expired = false, do_reset;
			int retry_until, max_retransmits;
			var net = sock_net(tp);

			if (retransmits_timed_out(tp, net.ipv4.sysctl_tcp_retries1, 0))
			{

			}

			retry_until = net.ipv4.sysctl_tcp_retries2;
			if (!expired)
			{
				expired = retransmits_timed_out(tp, retry_until, tp.icsk_user_timeout);
			}

			if (expired)
			{
				tcp_write_err(tp);
				return 1;
			}
			return 0;
		}

        /* Called with BH disabled */
        void tcp_delack_timer_handler(tcp_sock tp)
		{
			if (((1 << tp.sk_state) & (TCPF_STATE.TCPF_CLOSE | TCPF_STATE.TCPF_LISTEN)) > 0)
			{
				return;
			}
			
			if (tp.compressed_ack) 
			{
				tcp_mstamp_refresh(tp);
				tcp_sack_compress_send_ack(sk);
				return;
			}

			if (!(icsk->icsk_ack.pending & ICSK_ACK_TIMER))
				return;

			if (time_after(icsk->icsk_ack.timeout, jiffies)) {
				sk_reset_timer(sk, &icsk->icsk_delack_timer, icsk->icsk_ack.timeout);
				return;
			}
			icsk->icsk_ack.pending &= ~ICSK_ACK_TIMER;

			if (inet_csk_ack_scheduled(sk))
			{
				if (!inet_csk_in_pingpong_mode(sk))
				{
					/* Delayed ACK missed: inflate ATO. */
					icsk->icsk_ack.ato = min_t(u32, icsk->icsk_ack.ato << 1, icsk->icsk_rto);
				}
				else
				{
					/* Delayed ACK missed: leave pingpong mode and
					 * deflate ATO.
					 */
					inet_csk_exit_pingpong_mode(sk);
					icsk->icsk_ack.ato = TCP_ATO_MIN;
				}
				tcp_mstamp_refresh(tp);
				tcp_send_ack(sk);
				__NET_INC_STATS(sock_net(sk), LINUX_MIB_DELAYEDACKS);
			}
		}

	}
}
