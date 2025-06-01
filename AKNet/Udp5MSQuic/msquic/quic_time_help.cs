using AKNet.Common;
using System;
using System.Diagnostics;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        //LInux TCP 我们用毫秒
        //QUIC 现在我们都用微秒表示
        readonly static Stopwatch mStopwatch = Stopwatch.StartNew();

        static long CxPlatTimeDiff(long T1, long T2)
        {
            NetLog.Assert(T2 >= T1, $"T1: {T1}, T2: {T2}");
            return T2 - T1;
        }

        static long CxPlatTime()
        {
            return (long)Math.Floor(mStopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000000);
        }

        static bool CxPlatTimeAtOrBefore64(long T1, long T2)
        {
            return T1 <= T2;
        }

        static long CxPlatGetTimerResolution()
        {
            return Stopwatch.Frequency;
        }

        static long S_TO_US(long second)
        {
            return second * 1000000;
        }

        static long MS_TO_US(long millisecond)
        {
            return millisecond * 1000;
        }
    }
}
