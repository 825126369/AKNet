using AKNet.LinuxTcp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

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

				TCPF_STATE sk_state = (TCPF_STATE)(1 << (int)tp.sk_state);
                if ((sk_state & (TCPF_STATE.TCPF_SYN_SENT | TCPF_STATE.TCPF_SYN_RECV)) > 0)
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
        public static void tcp_delack_timer_handler(tcp_sock tp)
		{
            TCPF_STATE sk_state = (TCPF_STATE)(1 << (int)tp.sk_state);
            if ((sk_state & (TCPF_STATE.TCPF_CLOSE | TCPF_STATE.TCPF_LISTEN)) > 0)
			{
				return;
			}
			
			if (tp.compressed_ack > 0) 
			{
				tcp_mstamp_refresh(tp);
				tcp_sack_compress_send_ack(tp);
				return;
			}

			if ((tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_TIMER) == 0)
			{
				return;
			}

			if (tp.icsk_ack.timeout - tcp_jiffies32 > 0)
			{
				sk_reset_timer(tp, tp.icsk_delack_timer, tp.icsk_ack.timeout);
				return;
			}

			//按位取反，与操作
            tp.icsk_ack.pending = (byte)(tp.icsk_ack.pending & ~(byte)inet_csk_ack_state_t.ICSK_ACK_TIMER);

            if (inet_csk_ack_scheduled(tp))
			{
				if (!inet_csk_in_pingpong_mode(tp))
				{
					tp.icsk_ack.ato = Math.Min(tp.icsk_ack.ato << 1, tp.icsk_rto);
				}
				else
				{
					inet_csk_exit_pingpong_mode(tp);
					tp.icsk_ack.ato = tcp_sock.TCP_ATO_MIN;
				}

				tcp_mstamp_refresh(tp);
				tcp_send_ack(tp);
                NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_DELAYEDACKS, 1);
			}
		}

		static void tcp_delack_timer(object data)
		{
			var tp = data as tcp_sock;

			if ((tp.icsk_ack.pending & (byte)inet_csk_ack_state_t.ICSK_ACK_TIMER) == 0 && tp.compressed_ack == 0)
			{

			}
			else
			{
				tcp_delack_timer_handler(tp);
			}
		}

        public static void tcp_update_rto_stats(tcp_sock tp)
		{
			if (tp.icsk_retransmits == 0) 
			{
				tp.total_rto_recoveries++;
				tp.rto_stamp = tcp_time_stamp_ms(tp);
			}

			tp.icsk_retransmits++;
			tp.total_rto++;
		}

		static bool tcp_rtx_probe0_timed_out(tcp_sock tp, sk_buff skb, long rtx_delta)
		{
			long user_timeout = tp.icsk_user_timeout;
			long timeout = tcp_sock.TCP_RTO_MAX * 2;
			long rcv_delta;

			if (user_timeout > 0)
			{
				if (rtx_delta > user_timeout)
				{
					return true;
				}
				timeout = Math.Min(timeout, user_timeout);
			}

			rcv_delta = tp.icsk_timeout - tp.rcv_tstamp;
			if (rcv_delta <= timeout)
			{
				return false;
			}
			return rtx_delta > timeout;
		}

		public static void tcp_retransmit_timer(tcp_sock tp)
		{
			net net = sock_net(tp);
			request_sock req;
			sk_buff skb = null;

			if (tp.packets_out == 0)
			{
				return;
			}

			//skb = tp.tcp_rtx_queue.rb_node;
			if (skb == null)
			{
				return;
			}

			if (tp.snd_wnd == 0 && !sock_flag(tp, sock_flags.SOCK_DEAD) &&
				((1 << (int)tp.sk_state) & (int)(TCPF_STATE.TCPF_SYN_SENT | TCPF_STATE.TCPF_SYN_RECV)) == 0)
			{
				long rtx_delta;

				rtx_delta = tcp_time_stamp_ts(tp) - (tp.retrans_stamp > 0 ? tp.retrans_stamp : tcp_skb_timestamp_ts(tp.tcp_usec_ts, skb));

				if (tp.sk_family == sk_family.AF_INET)
				{

				}

				if (tcp_rtx_probe0_timed_out(sk, skb, rtx_delta))
				{
					tcp_write_err(sk);
					return;
				}

		tcp_enter_loss(sk);
		tcp_retransmit_skb(sk, skb, 1);
		__sk_dst_reset(sk);
		goto out_reset_timer;
			}

			__NET_INC_STATS(sock_net(sk), LINUX_MIB_TCPTIMEOUTS);
		if (tcp_write_timeout(sk))
			goto out;

		if (icsk->icsk_retransmits == 0)
		{
			int mib_idx = 0;

			if (icsk->icsk_ca_state == TCP_CA_Recovery)
			{
				if (tcp_is_sack(tp))
					mib_idx = LINUX_MIB_TCPSACKRECOVERYFAIL;
				else
					mib_idx = LINUX_MIB_TCPRENORECOVERYFAIL;
			}
			else if (icsk->icsk_ca_state == TCP_CA_Loss)
			{
				mib_idx = LINUX_MIB_TCPLOSSFAILURES;
			}
			else if ((icsk->icsk_ca_state == TCP_CA_Disorder) ||
				   tp->sacked_out)
			{
				if (tcp_is_sack(tp))
					mib_idx = LINUX_MIB_TCPSACKFAILURES;
				else
					mib_idx = LINUX_MIB_TCPRENOFAILURES;
			}
			if (mib_idx)
				__NET_INC_STATS(sock_net(sk), mib_idx);
		}

		tcp_enter_loss(sk);

		tcp_update_rto_stats(sk);
		if (tcp_retransmit_skb(sk, tcp_rtx_queue_head(sk), 1) > 0)
		{
			/* Retransmission failed because of local congestion,
			 * Let senders fight for local resources conservatively.
			 */
			inet_csk_reset_xmit_timer(sk, ICSK_TIME_RETRANS,
						  TCP_RESOURCE_PROBE_INTERVAL,
						  TCP_RTO_MAX);
			goto out;
		}

		/* Increase the timeout each time we retransmit.  Note that
		* we do not increase the rtt estimate.  rto is initialized
		* from rtt, but increases here.  Jacobson (SIGCOMM 88) suggests
		* that doubling rto each time is the least we can get away with.
		* In KA9Q, Karn uses this for the first few times, and then
		* goes to quadratic.  netBSD doubles, but only goes up to *64,
		* and clamps at 1 to 64 sec afterwards.  Note that 120 sec is
		* defined in the protocol as the maximum possible RTT.  I guess
		* we'll have to use something other than TCP to talk to the
		* University of Mars.
		*
		* PAWS allows us longer timeouts and large windows, so once
		* implemented ftp to mars will work nicely. We will have to fix
		* the 120 second clamps though!
		*/

		out_reset_timer:
		/* If stream is thin, use linear timeouts. Since 'icsk_backoff' is
		 * used to reset timer, set to 0. Recalculate 'icsk_rto' as this
		 * might be increased if the stream oscillates between thin and thick,
		 * thus the old value might already be too high compared to the value
		 * set by 'tcp_set_rto' in tcp_input.c which resets the rto without
		 * backoff. Limit to TCP_THIN_LINEAR_RETRIES before initiating
		 * exponential backoff behaviour to avoid continue hammering
		 * linear-timeout retransmissions into a black hole
		 */
		if (sk->sk_state == TCP_ESTABLISHED &&
			(tp->thin_lto || READ_ONCE(net->ipv4.sysctl_tcp_thin_linear_timeouts)) &&
			tcp_stream_is_thin(tp) &&
			icsk->icsk_retransmits <= TCP_THIN_LINEAR_RETRIES)
		{
			icsk->icsk_backoff = 0;
			icsk->icsk_rto = clamp(__tcp_set_rto(tp),
						   tcp_rto_min(sk),
						   TCP_RTO_MAX);
		}
		else if (sk->sk_state != TCP_SYN_SENT ||
			   tp->total_rto >
			   READ_ONCE(net->ipv4.sysctl_tcp_syn_linear_timeouts))
		{
			/* Use normal (exponential) backoff unless linear timeouts are
			 * activated.
			 */
			icsk->icsk_backoff++;
			icsk->icsk_rto = min(icsk->icsk_rto << 1, TCP_RTO_MAX);
		}
		inet_csk_reset_xmit_timer(sk, ICSK_TIME_RETRANS, tcp_clamp_rto_to_user_timeout(sk), TCP_RTO_MAX);
		if (retransmits_timed_out(sk, READ_ONCE(net->ipv4.sysctl_tcp_retries1) + 1, 0)) __sk_dst_reset(sk);

		out:;
		}

	}
}
