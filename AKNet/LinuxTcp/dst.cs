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
    }
}
