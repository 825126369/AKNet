﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:42
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Tcp.Common
{
    public static class TcpStatistical
    {
        static ulong nSendPackageCount = 0;
        static ulong nReceivePackageCount = 0;
        static ulong nSendBytesCount = 0;
        static ulong nReceiveBytesCount = 0;
        
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

        internal static void AddSendBytesCount(int nBytesLength)
        {
#if DEBUG
            nSendBytesCount += (ulong)nBytesLength;
#endif
        }

        internal static void AddReceiveBytesCount(int nBytesLength)
        {
#if DEBUG
            nReceiveBytesCount += (ulong)nBytesLength;
#endif
        }

        private static double GetMBytes(ulong nBytesLength)
        {
            return nBytesLength / 1024.0 / 1024.0;
        }

        public static void PrintLog()
        {
            NetLog.Log($"PackageStatistical: {nSendPackageCount}, {nReceivePackageCount}, {GetMBytes(nSendBytesCount)}M, {GetMBytes(nReceiveBytesCount)}M");
        }
    }
}