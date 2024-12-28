namespace AKNet.LinuxTcp
{
    internal class dst_entry
    {

    }
    
    internal partial class LinuxTcpFunc
    {
        static uint dst_metric_raw(dst_entry dst, int metric)
        {
            uint p = DST_METRICS_PTR(dst);
            return p[metric - 1];
        }
        
        static uint dst_metric(dst_entry dst, int metric)
        {
            return dst_metric_raw(dst, metric);
        }
    }
}
