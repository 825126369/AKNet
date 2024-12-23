namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        static int ip_queue_xmit(tcp_sock tp, sk_buff skb, flowi fl)
        {
            return __ip_queue_xmit(tp, skb, fl, tp.tos);
        }

        static int __ip_queue_xmit(tcp_sock tp, sk_buff skb, flowi fl, byte tos)
		{
            return 0;
        }

    }
}
