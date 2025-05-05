using AKNet.Common;
using System;
using System.Diagnostics;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static Stopwatch mStopwatch = Stopwatch.StartNew();
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

        static bool CxPlatTimeAtOrBefore64(long T1, long T2)
        {
            return T1 <= T2;
        }

        static long CxPlatGetTimerResolution()
        {
            return Stopwatch.Frequency;
        }

        static long CxPlatTimeUs64()
        {
            return (long)Math.Floor(mStopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000000);
        }
    }
}
