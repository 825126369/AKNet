using System.Diagnostics;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal static class UdpStaticCommon
    {
        static readonly Stopwatch mStopwatch = Stopwatch.StartNew();
        public static long GetNowTime()
        {
            return mStopwatch.ElapsedMilliseconds;
        }
    }
}
