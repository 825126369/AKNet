namespace AKNet.Udp4LinuxTcp.Common
{
	internal static partial class LinuxTcpFunc
	{
		static void INET_ECN_xmit(tcp_sock tp)
		{
			tp.tos |= INET_ECN_ECT_0;
		}

		static void INET_ECN_dontxmit(tcp_sock tp)
		{
			tp.tos = (byte)(tp.tos & (~INET_ECN_MASK));
		}
	}
}
