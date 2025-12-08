/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Runtime.CompilerServices;

namespace AKNet.Udp4Tcp.Common
{
    public static class UdpStatistical
    {
        static long nFrameCount = 0;

        static ulong nSendPackageCount = 0;
        static ulong nReceivePackageCount = 0;
        static ulong nSendCheckPackageCount = 0;
        static ulong nReceiveCheckPackageCount = 0;
        static ulong nSendSureOrderIdPackageCount = 0;
        static ulong nReceiveSureOrderIdPackageCount = 0;

        static ulong nFirstSendCheckPackageCount = 0;
        static ulong nReSendCheckPackageCount = 0;

        static ulong nHitTargetOrderPackageCount = 0;
        static ulong nHitReceiveCachePoolPackageCount = 0;
        static ulong nGarbagePackageCount = 0;

        static long nRttCount = 0;
        static long nRttSumTime = 0;
        static long nRttMinTime = long.MaxValue;
        static long nRttMaxTime = 0;

        static long nRTOCount = 0;
        static long nRTOSumTime = 0;
        static long nRTOMinTime = long.MaxValue;
        static long nRTOMaxTime = 0;

        static long nMinSearchCount = 0;
        static long nMaxSearchCount = 0;
        static long nAverageFrameSearchCount = 0;

        static long nQuickReSendCount = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddSendCheckPackageCount()
        {
            nSendCheckPackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddReceiveCheckPackageCount()
        {
            nReceiveCheckPackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddSendSureOrderIdPackageCount()
        {
            nSendSureOrderIdPackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddReceiveSureOrderIdPackageCount()
        {
            nReceiveSureOrderIdPackageCount++;
        }

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
        internal static void AddHitTargetOrderPackageCount()
        {
            nHitTargetOrderPackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddHitReceiveCachePoolPackageCount()
        {
            nHitReceiveCachePoolPackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddGarbagePackageCount()
        {
            nGarbagePackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddFirstSendCheckPackageCount()
        {
            nFirstSendCheckPackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddReSendCheckPackageCount()
        {
            nReSendCheckPackageCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddRTO(long nTime)
        {
            nRTOCount++;
            nRTOSumTime += nTime;
            if(nRTOMinTime > nTime)
            {
                nRTOMinTime = nTime;
            }
            if (nRTOMaxTime < nTime)
            {
                nRTOMaxTime = nTime;
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

            NetLog.Log($"nSendPackageCount: {nSendPackageCount}");
            NetLog.Log($"nSendCheckPackageCount: {nSendCheckPackageCount}");
            NetLog.Log($"nSendSureOrderIdPackageCount: {nSendSureOrderIdPackageCount}");
            NetLog.Log($"nFirstSendCheckPackageCount: {nFirstSendCheckPackageCount}");
            NetLog.Log($"nReSendCheckPackageCount: {nReSendCheckPackageCount}");
            NetLog.Log($"nQuickReSendPackageCount: {nQuickReSendCount}");
            NetLog.Log("");

            NetLog.Log($"nReceivePackageCount: {nReceivePackageCount}");
            NetLog.Log($"nReceiveCheckPackageCount: {nReceiveCheckPackageCount}");
            NetLog.Log($"nReceiveSureOrderIdPackageCount: {nReceiveSureOrderIdPackageCount}");

            NetLog.Log("");
            if (nSendCheckPackageCount > nReceiveCheckPackageCount)
            {
                NetLog.Log($"Send Lose Package: {nSendPackageCount - nReceivePackageCount}");
                NetLog.Log($"Send Lose Package Rate: {(nSendCheckPackageCount - nReceiveCheckPackageCount) / (double)nSendCheckPackageCount}");
            }
            else
            {
                NetLog.Log($"Receive Lose Package: {nReceivePackageCount - nSendPackageCount}");
                NetLog.Log($"Receive Lose Package Rate: {(nReceiveCheckPackageCount - nSendCheckPackageCount) / (double)nReceiveCheckPackageCount}");
            }
            NetLog.Log("");

            NetLog.Log($"nRttCount: {nRttCount}");
            NetLog.Log($"nRttMinTime: {nRttMinTime / (double)1000}");
            NetLog.Log($"nRttMaxTime: {nRttMaxTime / (double)1000}");
            NetLog.Log($"nRttAverageTime: {nRttSumTime / (double)nRttCount / 1000}");
            NetLog.Log("");

            NetLog.Log($"nRTOCount: {nRTOCount}");
            NetLog.Log($"nRTOMinTime: {nRTOMinTime / (double)1000}");
            NetLog.Log($"nRTOMaxTime: {nRTOMaxTime / (double)1000}");
            NetLog.Log($"nRTOAverageTime: {nRTOSumTime / (double)nRTOCount / 1000}");
            NetLog.Log("");

            NetLog.Log($"nGarbagePackageCount: {nGarbagePackageCount}");
            NetLog.Log($"nHitTargetOrderPackageCount: {nHitTargetOrderPackageCount}");
            NetLog.Log($"nHitReceiveCachePoolPackageCount: {nHitReceiveCachePoolPackageCount}");
            NetLog.Log("");

            NetLog.Log($"ReSend Rate: {nReSendCheckPackageCount / (double)nFirstSendCheckPackageCount}");
            NetLog.Log($"GarbagePackage Rate: {nGarbagePackageCount / (double)(nReceiveCheckPackageCount)}");
            NetLog.Log($"HitPackage Rate: {nHitTargetOrderPackageCount / (double)(nReceiveCheckPackageCount)}");
            NetLog.Log($"Hit CachePool Rate: {nHitReceiveCachePoolPackageCount / (double)(nReceiveCheckPackageCount)}");
            NetLog.Log("");
            
            NetLog.Log($"nMaxSearchCount: {nMaxSearchCount}");
            NetLog.Log($"nMinSearchCount: {nMinSearchCount}");
            NetLog.Log($"nAverageSearchCount: {nAverageFrameSearchCount / (double)nFrameCount}");
            NetLog.Log("");
        }
    }
}
