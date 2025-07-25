﻿using AKNet.Common;
using AKNet.Platform;
using System;
using System.Diagnostics;

namespace AKNet.Udp1MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        //LInux TCP 我们用毫秒
        readonly static Stopwatch mStopwatch = Stopwatch.StartNew();

        static long CxPlatTimeDiff(long T1, long T2)
        {
           // NetLog.Assert(T2 >= T1, $"T1: {T1}, T2: {T2}");
            return (long)((ulong)T2 - (ulong)T1);
        }

        static long CxPlatTimeUs()
        {
            //Stopwatch.Frequency = 10000000 // 每秒 1000万个Tick
            return S_TO_US(mStopwatch.ElapsedTicks / (double)Stopwatch.Frequency);
        }

        static bool CxPlatTimeAtOrBefore64(long T1, long T2)
        {
            return T1 <= T2;
        }

        //系统默认的时钟中断间隔。
        //Thread.Sleep(1);实际上它可能会休眠 15.6 毫秒，而不是 1 毫秒，因为系统时钟的最小分辨率为 15.6ms。
        static long CxPlatGetTimerResolutionUs()
        {
            return OSPlatformFunc.GetSystemTimeAdjustment();
        }

        static long CxPlatTimeEpochMs64()
        {
            return DateTimeOffset.UtcNow.ToFileTime() / 10000;
        }

        static long S_TO_US(double second)
        {
            return (long)Math.Ceiling(second * 1000000);
        }

        static long S_TO_US(long second)
        {
            return second * 1000000;
        }

        static long MS_TO_US(long ms)
        {
            return ms * 1000;
        }

        static long US_TO_MS(long us)
        {
            return us / 1000;
        }
    }
}
