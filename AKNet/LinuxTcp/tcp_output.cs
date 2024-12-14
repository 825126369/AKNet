namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
		public static long tcp_jiffies32
		{
			get { return mStopwatch.ElapsedTicks; }
		}

        //它用于确保 TCP 的重传超时（RTO, Retransmission Timeout）不会超过用户设定的连接超时时间。
        public static void tcp_chrono_stop(tcp_sock tp, tcp_chrono type)
		{
			/* There are multiple conditions worthy of tracking in a
			 * chronograph, so that the highest priority enum takes
			 * precedence over the other conditions (see tcp_chrono_start).
			 * If a condition stops, we only stop chrono tracking if
			 * it's the "most interesting" or current chrono we are
			 * tracking and starts busy chrono if we have pending data.
			 */
			if (tcp_rtx_and_write_queues_empty(tp))
			{
				tcp_chrono_set(tp, tcp_chrono.TCP_CHRONO_UNSPEC);
			}
			else if (type == tp.chrono_type)
			{
				tcp_chrono_set(tp, tcp_chrono.TCP_CHRONO_BUSY);
			}
		}

		public static void tcp_chrono_set(tcp_sock tp, tcp_chrono newType)
		{
			long now = tcp_jiffies32;
			tcp_chrono old = tp.chrono_type;

			if (old > tcp_chrono.TCP_CHRONO_UNSPEC)
			{
				tp.chrono_stat[(int)old - 1] += now - tp.chrono_start;
			}
			tp.chrono_start = now;
			tp.chrono_type = newType;
		}

	}
}
