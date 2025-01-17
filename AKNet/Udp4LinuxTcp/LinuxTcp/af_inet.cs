namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        static int inet_init()
        {
            tcp_init();
            return 0;
        }
    }
}
