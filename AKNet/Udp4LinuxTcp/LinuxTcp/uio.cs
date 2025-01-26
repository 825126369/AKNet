namespace AKNet.Udp4LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        static long iov_iter_count(iov_iter i)
        {
	        return i.count;
        }
    }
}
