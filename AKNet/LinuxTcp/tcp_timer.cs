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
			if (tp.tcp_usec_ts)
				elapsed /= 1000;

            long remaining = user_timeout - elapsed;
			if (remaining <= 0)
				return 1;

			return tp.icsk_rto;
        }

	}
}
