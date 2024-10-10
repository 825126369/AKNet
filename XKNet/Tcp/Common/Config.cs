using System;
using System.Collections.Generic;
using System.Text;

namespace XKNet.Tcp.Common
{
    internal static class Config
    {
        //Common
        public const int nPackageFixedHeadSize = 8;

        public static int nSendReceiveCacheBufferInitLength = 1024 * 24;
        public static int nMsgPackageBufferMaxLength = 1024 * 8;
        public static int nIOContexBufferLength = 1024;

        //Client
        public static double fSendHeartBeatMaxTimeOut = 1.0;
        public static double fReceiveHeartBeatMaxTimeOut = 5.0;
        public static double fReceiveReConnectMaxTimeOut = 2.0;

        //Server
        public static int numConnections = 10000;
    }
}
