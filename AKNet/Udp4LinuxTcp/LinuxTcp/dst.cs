namespace AKNet.Udp4LinuxTcp.Common
{
    internal class dst_entry
    {
        public net net = null;
        public int[] _metrics = new int[LinuxTcpFunc.__RTAX_MAX];
    }

    internal partial class LinuxTcpFunc
    {
        //得到测量值
        static int dst_metric(dst_entry dst, int metric)
        {
            return dst._metrics[metric];
        }

        //判断这个测量值，是否被锁定
        static bool dst_metric_locked(dst_entry dst, int metric)
        {
            return BoolOk(dst_metric(dst, RTAX_LOCK) & (1 << metric));
        }

        static int dst_feature(dst_entry dst, int feature)
        {
	        return dst_metric(dst, RTAX_FEATURES) & feature;
        }

        static ushort dst_metric_advmss(dst_entry dst)
        {
            ushort advmss = (ushort)dst_metric(dst, RTAX_ADVMSS);
            if (advmss == 0)
            {
                advmss = ipv4_default_advmss(dst);
            }
            return advmss;
        }
    }
}
