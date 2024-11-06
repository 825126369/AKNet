/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
	internal static class Config
	{
        public const bool bUseSocketLock = true;
        public const bool bUdpCheck = true;
        public const bool bUseSendAsync = true;
        //Udp Package OrderId
        public const ushort nUdpMinOrderId = 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;

		public const int nUdpPackageFixedSize = 1024;
		public const int nUdpPackageFixedHeadSize = 12;
		public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
		public const int nUdpCombinePackageInitSize = 1024 * 8; //合并包是可变的
		public const int nMsgPackageBufferMaxLength = 1024 * 8 - nUdpPackageFixedHeadSize;

		public const double fReceiveHeartBeatTimeOut = 5.0;
		public const double fMySendHeartBeatMaxTime = 2.0;

        //=====================Client============================================================================
        public const int client_socket_receiveBufferSize = 1024 * 64; //暂时没用到

        //=====================Server============================================================================
        public const int numConnections = 10000;
        public const int server_socket_receiveBufferSize = 1024 * 64;		//接收缓冲区对丢包影响特别大
		
        static Config()
		{
			NetLog.Assert(nUdpMaxOrderId - nUdpMinOrderId >= 1024);
		}
	}
}
