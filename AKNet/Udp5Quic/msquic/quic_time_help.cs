using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static long CxPlatTimeDiff(long T1, long T2)
        {
            return T2 - T1;
        }

        static long CxPlatTime()
        {
            return mStopwatch.ElapsedMilliseconds;
        }

        static long CxPlatTimeDiff64(long T1, long T2)
        {
            NetLog.Assert(T2 >= T1);
            return T2 - T1;
        }

        static bool CxPlatTimeAtOrBefore64(long T1,long T2)
        {
            return T1 <= T2;
        }
    }
}
