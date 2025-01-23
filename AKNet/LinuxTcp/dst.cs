using System.Security.Cryptography;

namespace AKNet.LinuxTcp
{
    internal class dst_entry
    {
        public ulong _metrics;
    }
    
    internal partial class LinuxTcpFunc
    {
        static ulong dst_metric_raw(dst_entry dst, ulong metric)
        {
            return dst._metrics & metric;
        }
        
        static ulong dst_metric(dst_entry dst, ulong metric)
        {
            return dst_metric_raw(dst, metric);
        }

        static bool dst_metric_locked(dst_entry dst, int metric)
        {
	        return BoolOk((int)dst_metric(dst, RTAX_LOCK) & (1 << metric));
        }

        //static int dst_output(net net, tcp_sock tp, sk_buff skb)
        //{
        //    return ip_output(net, tp, skb);
        //}
    }
}
