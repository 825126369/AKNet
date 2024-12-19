namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static int tcp_rack_skb_timeout(tcp_sock tp, sk_buff skb, uint reo_wnd)
        {
	        return (int)(tp.rack.rtt_us + reo_wnd - tcp_stamp_us_delta(tp.tcp_mstamp, tcp_skb_timestamp_us(skb)));
        }
    }
}
