namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        static bool b_inet_init = false;
        static void inet_init(tcp_sock tp)
        {
            if (!b_inet_init)
            {
                b_inet_init = true;
                tcp_init();
            }
            inet_create(tp);
        }

        static void inet_create(tcp_sock tp)
        {
            sock_init_data(tp);
        }

    }
}
