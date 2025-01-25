namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        static int inet_init()
        {
            return 0;
        }

        static void inet_create(tcp_sock tp)
        {
            sock_init_data(tp);
        }

    }
}
