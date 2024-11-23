/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    public static class UdpStatistical
    {
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

        internal static void AddSendPackageCount()
        {
#if DEBUG
            nSendPackageCount++;
#endif
        }

        internal static void AddReceivePackageCount()
        {
#if DEBUG
            nReceivePackageCount++;
#endif
        }

        internal static void AddRtt(long nRtt)
        {
#if DEBUG
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
#endif
        }

        internal static void AddHitTargetOrderPackageCount(int nCount = 1)
        {
#if DEBUG
            nHitTargetOrderPackageCount += (ulong)nCount;
#endif
        }

        internal static void AddHitReceiveCachePoolPackageCount(int nCount = 1)
        {
#if DEBUG
            nHitReceiveCachePoolPackageCount += (ulong)nCount;
#endif
        }

        internal static void AddLosePackageCount(int nCount = 1)
        {
#if DEBUG
            nLosePackageCount += (ulong)nCount;
#endif
        }

        internal static void AddFirstSendCheckPackageCount(int nCount = 1)
        {
#if DEBUG
            nFirstSendCheckPackageCount += (ulong)nCount;
#endif
        }

        internal static void AddReSendCheckPackageCount(int nCount = 1)
        {
#if DEBUG
            nReSendCheckPackageCount += (ulong)nCount;
#endif
        }


        internal static void AddSendIOCount(bool bIOSyncCompleted)
        {
#if DEBUG
            nSendIOSumCount++;
            if (bIOSyncCompleted)
            {
                nSendIOSyncCompleteCount++;
            }
#endif
        }

        internal static void AddReceiveIOCount(bool bIOSyncCompleted)
        {
#if DEBUG
            nReceiveIOSumCount++;
            if (bIOSyncCompleted)
            {
                nReceiveIOSyncCompleteCount++;
            }
#endif
        }

        public static void PrintLog()
        {
            NetLog.Log($"Udp PackageStatistical:");

            NetLog.Log($"nReceivePackageCount: {nReceivePackageCount}");

            NetLog.Log($"nSendPackageCount: {nSendPackageCount}");
            NetLog.Log($"nFirstSendCheckPackageCount: {nFirstSendCheckPackageCount}");
            NetLog.Log($"nReSendCheckPackageCount: {nReSendCheckPackageCount}");

            NetLog.Log($"nRttMinTime: {nRttMinTime / (double)1000}");
            NetLog.Log($"nRttMaxTime: {nRttMaxTime / (double)1000}");
            NetLog.Log($"nRttAverageTime: {nRttSumTime / (double)nRttCount / 1000}");

            NetLog.Log($"nLosePackageCount: {nLosePackageCount}");
            NetLog.Log($"nHitTargetOrderPackageCount: {nHitTargetOrderPackageCount}");
            NetLog.Log($"nHitReceiveCachePoolPackageCount: {nHitReceiveCachePoolPackageCount}");

            NetLog.Log($"ReSend Rate: {nReSendCheckPackageCount / (double)nFirstSendCheckPackageCount}");
            NetLog.Log($"LosePackage Rate: {nLosePackageCount / (double)(nLosePackageCount + nHitTargetOrderPackageCount + nHitReceiveCachePoolPackageCount)}");
            NetLog.Log($"HitPackage Rate: {nHitTargetOrderPackageCount / (double)(nLosePackageCount + nHitTargetOrderPackageCount + nHitReceiveCachePoolPackageCount)}");
            NetLog.Log($"Hit CachePool Rate: {nHitReceiveCachePoolPackageCount / (double)(nLosePackageCount + nHitTargetOrderPackageCount + nHitReceiveCachePoolPackageCount)}");

            NetLog.Log($"nSendIOSyncCompleteCount: {nSendIOSyncCompleteCount}");
            NetLog.Log($"nSendIOSumCount: {nSendIOSumCount}");
            NetLog.Log($"nReceiveIOSyncCompleteCount: {nReceiveIOSyncCompleteCount}");
            NetLog.Log($"nReceiveIOSumCount: {nReceiveIOSumCount}");
            NetLog.Log($"SendIOSyncComplete Rate: {nSendIOSyncCompleteCount / (double)nSendIOSumCount}");
            NetLog.Log($"ReceiveIOSyncComplete Rate: {nReceiveIOSyncCompleteCount / (double)nReceiveIOSumCount}");
        }

    }
}
