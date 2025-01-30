using AKNet.Common;
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        static ushort ipv4_mtu(dst_entry dst)
        {
	        return (ushort)IPAddressHelper.GetMtu();
        }

        static ushort ipv4_default_advmss(dst_entry dst)
        {
            net net = dst.net;
            ushort advmss = (ushort)Math.Max(ipv4_mtu(dst) - mtu_max_head_length, net.ipv4.ip_rt_min_advmss);
            return (ushort)Math.Min((int)advmss, IPV4_MAX_PMTU - mtu_max_head_length);
        }
    }
}
