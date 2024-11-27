/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Runtime.CompilerServices;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    public static class UdpStatistical
    {
        static long nFrameCount = 0;

        static ulong nSendPackageCount = 0;
        static ulong nReceivePackageCount = 0;

        static ulong nFirstSendCheckPackageCount = 0;
        static ulong nReSendCheckPackageCount = 0;

        static ulong nHitTargetOrderPackageCount = 0;
        static ulong nHitReceiveCachePoolPackageCount = 0;
        static ulong nLosePackageCount = 0;

        static long nRttCount = 0;
        static long nRttSumTime = 0;
        static long nRttMinTime = 0;
        static long nRttMaxTime = 0;

        static long nSendIOSumCount = 0;
        static long nSendIOSyncCompleteCount = 0;
        static long nReceiveIOSumCount = 0;
        static long nReceiveIOSyncCompleteCount = 0;

        static long nMinSearchCount = 0;
        static long nMaxSearchCount = 0;
        static long nAverageFrameSearchCount = 0;

        static long nQuickReSendCount = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddSendPackageCount()
        {
            nSendPackageCount++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddReceivePackageCount()
        {
            nReceivePackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddRtt(long nRtt)
        {
            nRttCount++;
            nRttSumTime += nRtt;
            if (nRtt < nRttMinTime)
            {
                nRttMinTime = nRtt;
            }
            else if (nRtt > nRttMaxTime)
            {
                nRttMaxTime = nRtt;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddHitTargetOrderPackageCount(int nCount = 1)
        {
            nHitTargetOrderPackageCount += (ulong)nCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddHitReceiveCachePoolPackageCount(int nCount = 1)
        {
            nHitReceiveCachePoolPackageCount += (ulong)nCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddLosePackageCount(int nCount = 1)
        {
            nLosePackageCount += (ulong)nCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddFirstSendCheckPackageCount(int nCount = 1)
        {
            nFirstSendCheckPackageCount += (ulong)nCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddReSendCheckPackageCount(int nCount = 1)
        {
            nReSendCheckPackageCount += (ulong)nCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddSendIOCount(bool bIOSyncCompleted)
        {
            nSendIOSumCount++;
            if (bIOSyncCompleted)
            {
                nSendIOSyncCompleteCount++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddReceiveIOCount(bool bIOSyncCompleted)
        {
            nReceiveIOSumCount++;
            if (bIOSyncCompleted)
            {
                nReceiveIOSyncCompleteCount++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddFrameCount()
        {
            nFrameCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddQuickReSendCount()
        {
            nQuickReSendCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddSearchCount(int nCount)
        {
            nAverageFrameSearchCount += nCount;
            if (nCount > nMaxSearchCount)
            {
                nMaxSearchCount = nCount;
            }
            else if (nCount < nMinSearchCount)
            {
                nMinSearchCount = nCount;
            }
        }

        public static void PrintLog()
        {
            NetLog.Log($"Udp PackageStatistical:");
            NetLog.Log("");

            NetLog.Log($"nFrameCount: {nFrameCount}");
            NetLog.Log("");

            NetLog.Log($"nReceivePackageCount: {nReceivePackageCount}");
            NetLog.Log($"nSendPackageCount: {nSendPackageCount}");
            
            NetLog.Log($"nFirstSendCheckPackageCount: {nFirstSendCheckPackageCount}");
            NetLog.Log($"nReSendCheckPackageCount: {nReSendCheckPackageCount}");
            NetLog.Log("");

            NetLog.Log($"nRttMinTime: {nRttMinTime / (double)1000}");
            NetLog.Log($"nRttMaxTime: {nRttMaxTime / (double)1000}");
            NetLog.Log($"nRttAverageTime: {nRttSumTime / (double)nRttCount / 1000}");
            NetLog.Log("");

            NetLog.Log($"nLosePackageCount: {nLosePackageCount}");
            NetLog.Log($"nHitTargetOrderPackageCount: {nHitTargetOrderPackageCount}");
            NetLog.Log($"nHitReceiveCachePoolPackageCount: {nHitReceiveCachePoolPackageCount}");
            NetLog.Log("");

            NetLog.Log($"ReSend Rate: {nReSendCheckPackageCount / (double)nFirstSendCheckPackageCount}");
            NetLog.Log($"LosePackage Rate: {nLosePackageCount / (double)(nLosePackageCount + nHitTargetOrderPackageCount + nHitReceiveCachePoolPackageCount)}");
            NetLog.Log($"HitPackage Rate: {nHitTargetOrderPackageCount / (double)(nLosePackageCount + nHitTargetOrderPackageCount + nHitReceiveCachePoolPackageCount)}");
            NetLog.Log($"Hit CachePool Rate: {nHitReceiveCachePoolPackageCount / (double)(nLosePackageCount + nHitTargetOrderPackageCount + nHitReceiveCachePoolPackageCount)}");
            NetLog.Log("");

            NetLog.Log($"nQuickReSendCount: {nQuickReSendCount}");
            NetLog.Log($"nMaxSearchCount: {nMaxSearchCount}");
            NetLog.Log($"nMinSearchCount: {nMinSearchCount}");
            NetLog.Log($"nAverageSearchCount: {nAverageFrameSearchCount / (double)nFrameCount}");
            NetLog.Log("");

            NetLog.Log($"nSendIOSyncCompleteCount: {nSendIOSyncCompleteCount}");
            NetLog.Log($"nSendIOSumCount: {nSendIOSumCount}");
            NetLog.Log($"nReceiveIOSyncCompleteCount: {nReceiveIOSyncCompleteCount}");
            NetLog.Log($"nReceiveIOSumCount: {nReceiveIOSumCount}");
            NetLog.Log($"SendIOSyncComplete Rate: {nSendIOSyncCompleteCount / (double)nSendIOSumCount}");
            NetLog.Log($"ReceiveIOSyncComplete Rate: {nReceiveIOSyncCompleteCount / (double)nReceiveIOSumCount}");
        }
    }
}
