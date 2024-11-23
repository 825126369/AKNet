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
	internal class Config
	{
        public const bool bUdpCheck = true;
        public const bool bUseSocketLock = false;
        public const bool bUseSendAsync = true;
        public const bool bSocketSendMultiPackage = true;

        public const ushort nUdpMinOrderId = 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;
		public const int nUdpPackageFixedSize = 1024;
		public const int nUdpPackageFixedHeadSize = 14;
        public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
        public const int nMaxDataLength = ushort.MaxValue;

		public readonly double fReceiveHeartBeatTimeOut = 5.0;
		public readonly double fMySendHeartBeatMaxTime = 2.0;

        //=====================Client============================================================================
        public readonly int client_socket_receiveBufferSize = 1024 * 64; //暂时没用到

        //=====================Server============================================================================
        public readonly int numConnections = 10000;
        public readonly int server_socket_receiveBufferSize = 1024 * 1024;     //接收缓冲区对丢包影响特别大

        //加解密
        public readonly ECryptoType nECryptoType = ECryptoType.None;
        public readonly string password1 = string.Empty;
        public readonly string password2 = string.Empty;

        public Config(UdpConfig mUserConfig = null)
        {
            NetLog.Assert(nUdpMaxOrderId - nUdpMinOrderId >= 1024);
            if (mUserConfig != null)
            {
                fReceiveHeartBeatTimeOut = mUserConfig.fReceiveHeartBeatTimeOut;
                fMySendHeartBeatMaxTime = mUserConfig.fMySendHeartBeatMaxTime;
                client_socket_receiveBufferSize = mUserConfig.client_socket_receiveBufferSize;
                server_socket_receiveBufferSize = mUserConfig.server_socket_receiveBufferSize;
                numConnections = mUserConfig.numConnections;
            }
        }

	}
}
