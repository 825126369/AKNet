namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        //它用于确保 TCP 的重传超时（RTO, Retransmission Timeout）不会超过用户设定的连接超时时间。
        public static int rounddown(int __x, int y)
        {
            return __x - (__x % (y));
        }
    }
}
