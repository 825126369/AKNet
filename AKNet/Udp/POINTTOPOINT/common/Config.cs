/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
	internal static class Config
	{
        public const bool bUseSocketLock = true;
        public const bool bUseSendAsync = true;

        public const ushort nUdpMinOrderId = 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;
		public const int nUdpPackageFixedSize = 1024;
		public const int nUdpPackageFixedHeadSize = 12;
		public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;


        public static readonly bool bUdpCheck = true;
        public static readonly int nUdpCombinePackageInitSize = 1024 * 8; //合并包是可变的
		public static readonly int nMsgPackageBufferMaxLength = 1024 * 9;

		public static readonly double fReceiveHeartBeatTimeOut = 5.0;
		public static readonly double fMySendHeartBeatMaxTime = 2.0;

        //=====================Client============================================================================
        public static readonly int client_socket_receiveBufferSize = 1024 * 64; //暂时没用到

        //=====================Server============================================================================
        public static readonly int numConnections = 10000;
        public static readonly int server_socket_receiveBufferSize = 1024 * 1024;     //接收缓冲区对丢包影响特别大

        static Config()
        {
            NetLog.Assert(nUdpMaxOrderId - nUdpMinOrderId >= 1024);
            if (AKNetConfig.UdpConfig != null)
            {
                bUdpCheck = AKNetConfig.UdpConfig.bUdpCheck;
                nUdpCombinePackageInitSize = AKNetConfig.UdpConfig.nUdpCombinePackageInitSize;
                nMsgPackageBufferMaxLength = AKNetConfig.UdpConfig.nMsgPackageBufferMaxLength;
                fReceiveHeartBeatTimeOut = AKNetConfig.UdpConfig.fReceiveHeartBeatTimeOut;
                fMySendHeartBeatMaxTime = AKNetConfig.UdpConfig.fMySendHeartBeatMaxTime;
                client_socket_receiveBufferSize = AKNetConfig.UdpConfig.client_socket_receiveBufferSize;
                server_socket_receiveBufferSize = AKNetConfig.UdpConfig.server_socket_receiveBufferSize;
                numConnections = AKNetConfig.UdpConfig.numConnections;
            }
        }
	}
}
