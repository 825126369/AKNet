using System;
using System.Collections.Generic;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal static class TcpStanardFunc
    {
        private static readonly List<long> mRttTimeList = new List<long>();
        private static readonly List<long> mRttStdList = new List<long>();

        static long RttOld = 0;
        static long RttNew = 0;
        static long RttAverage  = 0;
        static long RttStdOld = 0;
        static long RttStd = 0;
        public void FinishRttSuccess(long nRtt)
        {
            mRttTimeList.Add(nRtt);
            RttStdOld = RttStd;
            if (mRttTimeList.Count >= 2)
            {
                RttOld = mRttTimeList[0];
                RttNew = mRttTimeList[1];
                RttAverage = (long)(0.125 * RttOld + (1 - 0.125) * RttNew);
                RttStd = (long)(0.25 * RttStdOld + (1 - 0.25) * Math.Abs(RttAverage - RttNew));
            }
        }

        private long GetRTOTime()
        {
            if (mRttTimeList.Count >= 2)
            {
                long finalRtt = RttAverage + 4 * RttStd;
                RttStdOld = RttNew;
                return finalRtt;
            }
            return 1000;
        }
    }
}
