using System;
using System.Diagnostics;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal static class TcpStanardRTOFunc
    {
        static readonly Stopwatch mStopwatch = Stopwatch.StartNew();
        const long DefaultRtt = 1000;
        const long DefaultRttStd = 50;

        static long RttOld = 0;
        static long RttNew = DefaultRtt;
        static long RttAverage  = 0;
        static long RttStdOld = 0;
        static long RttStd = DefaultRttStd;

        static long nStartTime = 0;

        private static long GetNowTime()
        {
            return mStopwatch.ElapsedMilliseconds;
        }

        public static void BeginRtt()
        {
            nStartTime = GetNowTime();
        }

        public static void FinishRttSuccess()
        {
            long nRtt = GetNowTime() - nStartTime;

            RttOld = RttNew;
            RttNew = nRtt;
            RttAverage = (long)(0.125 * RttOld + (1 - 0.125) * RttNew);
            RttStdOld = RttStd;
            RttStd = (long)(0.25 * RttStdOld + (1 - 0.25) * Math.Abs(RttAverage - RttNew));
        }

        public static long GetRTOTime()
        {
            if (RttAverage == 0)
            {
                return RttAverage + 4 * RttStd;
            }
            return DefaultRtt;
        }
    }
}
