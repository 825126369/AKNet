/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Diagnostics;
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

		public static bool tcp_write_timeout(tcp_sock tp)
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
				return false;
			}
			return true;
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
				long rtx_delta = tcp_time_stamp_ts(tp) - (tp.retrans_stamp > 0 ? tp.retrans_stamp : tcp_skb_timestamp_ts(tp.tcp_usec_ts, skb));
				if (tp.sk_family == sk_family.AF_INET)
				{

				}

				if (tcp_rtx_probe0_timed_out(tp, skb, rtx_delta))
				{
					tcp_write_err(tp);
					return;
				}

				tcp_enter_loss(tp);
				tcp_retransmit_skb(tp, skb, 1);
				goto out_reset_timer;
			}

			NET_ADD_STATS(sock_net(tp), LINUXMIB.LINUX_MIB_TCPTIMEOUTS, 1);
			if (tcp_write_timeout(tp))
			{
				return;
			}

			if (tp.icsk_retransmits == 0)
			{
				int mib_idx = 0;
				if (tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Recovery)
				{
					if (tcp_is_sack(tp))
					{
						mib_idx = (int)LINUXMIB.LINUX_MIB_TCPSACKRECOVERYFAIL;
					}
					else
					{
						mib_idx = (int)LINUXMIB.LINUX_MIB_TCPRENORECOVERYFAIL;
					}
				}
				else if (tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Loss)
				{
					mib_idx = (int)LINUXMIB.LINUX_MIB_TCPLOSSFAILURES;
				}
				else if ((tp.icsk_ca_state == (byte)tcp_ca_state.TCP_CA_Disorder) || tp.sacked_out > 0)
				{
					if (tcp_is_sack(tp))
					{
						mib_idx = (int)LINUXMIB.LINUX_MIB_TCPSACKFAILURES;
					}
					else
					{
						mib_idx = (int)LINUXMIB.LINUX_MIB_TCPRENOFAILURES;
					}
				}
				if (mib_idx > 0)
				{
					NET_ADD_STATS(sock_net(tp), (LINUXMIB)mib_idx, 1);
				}
			}

			tcp_enter_loss(tp);
			tcp_update_rto_stats(tp);

			if (tcp_retransmit_skb(tp, tcp_rtx_queue_head(tp), 1) > 0)
			{
				inet_csk_reset_xmit_timer(tp, tcp_sock.ICSK_TIME_RETRANS, tcp_sock.TCP_RESOURCE_PROBE_INTERVAL, tcp_sock.TCP_RTO_MAX);
				return;
			}

		out_reset_timer:
			if (tp.sk_state == TCP_STATE.TCP_ESTABLISHED &&
				(tp.thin_lto > 0 || net.ipv4.sysctl_tcp_thin_linear_timeouts > 0) &&
				tcp_stream_is_thin(tp) &&
				tp.icsk_retransmits <= tcp_sock.TCP_THIN_LINEAR_RETRIES)
			{
				tp.icsk_backoff = 0;
				tp.icsk_rto = (uint)Math.Clamp(__tcp_set_rto(tp), tcp_rto_min(tp), tcp_sock.TCP_RTO_MAX);
			}
			else if (tp.sk_state != TCP_STATE.TCP_SYN_SENT || tp.total_rto > net.ipv4.sysctl_tcp_syn_linear_timeouts)
			{
				tp.icsk_backoff++;
				tp.icsk_rto = (uint)Math.Min(tp.icsk_rto << 1, tcp_sock.TCP_RTO_MAX);
			}

			inet_csk_reset_xmit_timer(tp, tcp_sock.ICSK_TIME_RETRANS, tcp_clamp_rto_to_user_timeout(tp), tcp_sock.TCP_RTO_MAX);
			if (retransmits_timed_out(tp, net.ipv4.sysctl_tcp_retries1 + 1, 0))
			{
				__sk_dst_reset(tp);
			}
		}

		static int tcp_orphan_retries(tcp_sock tp, bool alive)
		{
			int retries = sock_net(tp).ipv4.sysctl_tcp_orphan_retries;
			if (tp.sk_err_soft > 0 && !alive)
			{
				retries = 0;
			}

			if (retries == 0 && alive)
			{
				retries = 8;
			}
			return retries;
		}

		static long tcp_clamp_probe0_to_user_timeout(tcp_sock tp, long when)
		{
			long remaining, user_timeout;
			long elapsed;

			user_timeout = tp.icsk_user_timeout;
			if (user_timeout == 0 || tp.icsk_probes_tstamp == 0)
			{
				return when;
			}

			elapsed = tcp_jiffies32 - tp.icsk_probes_tstamp;
			if (elapsed < 0)
			{
				elapsed = 0;
			}
			remaining = user_timeout - elapsed;
			remaining = Math.Max(remaining, tcp_sock.TCP_TIMEOUT_MIN);

			return Math.Min(remaining, when);
		}

		static void tcp_send_probe0(tcp_sock tp)
		{
			net net = sock_net(tp);
			long timeout;
			int err = tcp_write_wakeup(tp, (int)LINUXMIB.LINUX_MIB_TCPWINPROBE);

			if (tp.packets_out > 0 || tcp_write_queue_empty(tp))
			{
				tp.icsk_probes_out = 0;
				tp.icsk_backoff = 0;
				tp.icsk_probes_tstamp = 0;
				return;
			}

			tp.icsk_probes_out++;
			if (err <= 0)
			{
				if (tp.icsk_backoff < net.ipv4.sysctl_tcp_retries2)
				{
					tp.icsk_backoff++;
				}
				timeout = tcp_probe0_when(tp, tcp_sock.TCP_RTO_MAX);
			}
			else
			{
				timeout = (long)tcp_sock.TCP_RESOURCE_PROBE_INTERVAL;
			}

			timeout = tcp_clamp_probe0_to_user_timeout(tp, timeout);
			tcp_reset_xmit_timer(tp, tcp_sock.ICSK_TIME_PROBE0, timeout, tcp_sock.TCP_RTO_MAX);
		}

		static void tcp_probe_timer(tcp_sock tp)
		{
			sk_buff skb = tcp_send_head(tp);
			int max_probes;

			if (tp.packets_out > 0 || skb == null)
			{
				tp.icsk_probes_out = 0;
				tp.icsk_probes_tstamp = 0;
				return;
			}

			if (tp.icsk_probes_tstamp == 0)
			{
				tp.icsk_probes_tstamp = tcp_jiffies32;
			}
			else
			{
				long user_timeout = tp.icsk_user_timeout;
				if (user_timeout > 0 && (int)(tcp_jiffies32 - tp.icsk_probes_tstamp) >= user_timeout)
				{
					tcp_write_err(tp);
					return;
				}
			}

			max_probes = sock_net(tp).ipv4.sysctl_tcp_retries2;
			if (sock_flag(tp, sock_flags.SOCK_DEAD))
			{
				bool alive = inet_csk_rto_backoff(tp, tcp_sock.TCP_RTO_MAX) < tcp_sock.TCP_RTO_MAX;

				max_probes = tcp_orphan_retries(tp, alive);
				if (!alive && tp.icsk_backoff >= max_probes)
				{
					tcp_write_err(tp);
					return;
				}
				if (tcp_out_of_resources(tp, true) > 0)
				{
					return;
				}
			}

			if (tp.icsk_probes_out >= max_probes)
			{
				tcp_write_err(tp);
			}
			else
			{
				tcp_send_probe0(tp);
			}
		}

		static void tcp_write_timer_handler(tcp_sock tp)
		{
			int mEvent;
			if (BoolOk((1 << (int)tp.sk_state) & ((int)TCPF_STATE.TCPF_CLOSE | (int)TCPF_STATE.TCPF_LISTEN)) || tp.icsk_pending == 0)
			{
				return;
			}

			if (tp.icsk_timeout > tcp_jiffies32)
			{
				sk_reset_timer(tp, tp.icsk_retransmit_timer, tp.icsk_timeout);
				return;
			}

			mEvent = tp.icsk_pending;
			switch (mEvent)
			{
				case tcp_sock.ICSK_TIME_REO_TIMEOUT:
					tcp_rack_reo_timeout(tp);
					break;
				case tcp_sock.ICSK_TIME_LOSS_PROBE:
					tcp_send_loss_probe(tp);
					break;
				case tcp_sock.ICSK_TIME_RETRANS:
					tcp_retransmit_timer(tp);
					break;
				case tcp_sock.ICSK_TIME_PROBE0:
					tcp_probe_timer(tp);
					break;
			}
		}

		static void tcp_write_timer(tcp_sock tp)
		{
			if (tp.icsk_pending == 0)
			{
				return;
			}

			if (true)
			{
				tcp_write_timer_handler(tp);
			}
			else
			{
				tp.sk_tsq_flags = tp.sk_tsq_flags | (ulong)tsq_enum.TCP_WRITE_TIMER_DEFERRED;
			}
		}

		static void tcp_syn_ack_timeout(request_sock req)
		{
			//net net = inet_rsk(req).ireq_net;
			//__NET_INC_STATS(net, LINUX_MIB_TCPTIMEOUTS);
		}

		static void tcp_set_keepalive(tcp_sock tp, int val)
		{
			if (BoolOk((1 << (int)tp.sk_state) & (int)(TCPF_STATE.TCPF_CLOSE | TCPF_STATE.TCPF_LISTEN)))
			{
				return;
			}

			if (val > 0 && !sock_flag(tp, sock_flags.SOCK_KEEPOPEN))
			{
				inet_csk_reset_keepalive_timer(tp, keepalive_time_when(tp));
			}
			else if (val == 0)
			{
				inet_csk_delete_keepalive_timer(tp);
			}
		}

		static void tcp_keepalive_timer(tcp_sock tp)
		{
			long elapsed;
			if (sock_owned_by_user(tp))
			{
				inet_csk_reset_keepalive_timer(tp, tcp_sock.HZ / 20);
				return;
			}

			if (tp.sk_state == TCP_STATE.TCP_LISTEN)
			{
				NetLog.LogError("Hmm... keepalive on a LISTEN ???\n");
				return;
			}

			tcp_mstamp_refresh(tp);
			if (tp.sk_state == TCP_STATE.TCP_FIN_WAIT2 && sock_flag(tp, sock_flags.SOCK_DEAD))
			{
				return;
			}

			if (!sock_flag(tp, sock_flags.SOCK_KEEPOPEN) || BoolOk((1 << (int)tp.sk_state) & (byte)(TCPF_STATE.TCPF_CLOSE | TCPF_STATE.TCPF_SYN_SENT)))
			{
				return;
			}

			elapsed = keepalive_time_when(tp);
			if (tp.packets_out > 0 || !tcp_write_queue_empty(tp))
			{
				inet_csk_reset_keepalive_timer(tp, elapsed);
				return;
			}

			elapsed = keepalive_time_elapsed(tp);
			if (elapsed >= keepalive_time_when(tp))
			{
				long user_timeout = tp.icsk_user_timeout;
				if ((user_timeout != 0 && elapsed >= user_timeout && tp.icsk_probes_out > 0) ||
					(user_timeout == 0 && tp.icsk_probes_out >= keepalive_probes(tp)))
				{
					tcp_send_active_reset(tp, sk_rst_reason.SK_RST_REASON_TCP_KEEPALIVE_TIMEOUT);
					tcp_write_err(tp);
					return;
				}

				if (tcp_write_wakeup(tp, (int)LINUXMIB.LINUX_MIB_TCPKEEPALIVE) <= 0)
				{
					tp.icsk_probes_out++;
					elapsed = keepalive_intvl_when(tp);
				}
				else
				{
					elapsed = tcp_sock.TCP_RESOURCE_PROBE_INTERVAL;
				}
			}
			else
			{
				elapsed = keepalive_time_when(tp) - elapsed;
			}
		}

	}

}
