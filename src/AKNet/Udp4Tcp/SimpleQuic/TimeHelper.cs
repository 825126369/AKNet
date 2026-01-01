/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AKNet.Udp4Tcp.Common
{
    internal static partial class SimpleQuicFunc
    {
        readonly static Stopwatch mStopwatch = Stopwatch.StartNew();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long CxPlatTimeDiff(long T1, long T2)
        {
           // NetLog.Assert(T2 >= T1, $"T1: {T1}, T2: {T2}");
            return (long)((ulong)T2 - (ulong)T1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CxPlatTimeUs()
        {
            //Stopwatch.Frequency = 10000000 // 每秒 1000万个Tick
            return S_TO_US(mStopwatch.ElapsedTicks / (double)Stopwatch.Frequency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CxPlatTimeAtOrBefore64(long T1, long T2)
        {
            return T1 <= T2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long S_TO_US(double second)
        {
            return (long)Math.Ceiling(second * 1000000);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long S_TO_US(long second)
        {
            return second * 1000000;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long MS_TO_US(long ms)
        {
            return ms * 1000;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long US_TO_MS(long us)
        {
            return us / 1000;
        }
    }
}
