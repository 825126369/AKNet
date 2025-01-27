namespace AKNet.Udp4LinuxTcp.Common
{
    internal partial class LinuxTcpFunc
    {
        static byte ipv4_get_dsfield(iphdr iph)
        {
	        return iph.tos;
        }
    }
}
