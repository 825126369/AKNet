using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static ulong CxPlatTimeDiff(ulong T1, ulong T2)
        {
            return T2 - T1;
        }

        static ulong CxPlatTime()
        {
            return (ulong)mStopwatch.ElapsedMilliseconds;
        }

        static ulong CxPlatTimeDiff64(ulong T1, ulong T2)
        {
            NetLog.Assert(T2 >= T1);
            return T2 - T1;
        }

        static bool CxPlatTimeAtOrBefore64(ulong T1,ulong T2)
        {
            return T1 <= T2;
        }
    }
}
