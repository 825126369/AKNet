namespace AKNet.Udp4LinuxTcp.Common
{
    internal partial class LinuxTcpFunc
    {
        static byte ipv4_get_dsfield(tcphdr iph)
        {
	        return iph.tos;
        }

        static void ipv4_change_dsfield(tcphdr iph, byte mask, byte value)
        {
            byte dsfield = (byte)((iph.tos & mask) | value);
            iph.tos = dsfield;
        }
    }
}
