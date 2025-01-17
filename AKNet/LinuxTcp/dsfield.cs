namespace AKNet.LinuxTcp
{
    internal partial class LinuxTcpFunc
    {
        static byte ipv4_get_dsfield(iphdr iph)
        {
	        return iph.tos;
        }
    }
}
