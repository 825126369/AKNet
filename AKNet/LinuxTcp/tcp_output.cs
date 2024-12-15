namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
		public static long tcp_jiffies32
		{
			get { return mStopwatch.ElapsedMilliseconds; }
		}
		
        public static void tcp_chrono_stop(tcp_sock tp, tcp_chrono type)
		{
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

        public static void tcp_mstamp_refresh(tcp_sock tp)
		{
			tp.tcp_mstamp = tcp_jiffies32;
		}

}
}
