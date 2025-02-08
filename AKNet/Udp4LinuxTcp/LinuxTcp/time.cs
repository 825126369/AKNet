namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        static long jiffies_to_msecs(long j)
        {
            return (MSEC_PER_SEC / HZ) * j;
        }
        
        static long jiffies_to_usecs(long j)
		{
			BUG_ON(HZ > USEC_PER_SEC);
            return (USEC_PER_SEC / HZ) * j;
        }
    }
}
