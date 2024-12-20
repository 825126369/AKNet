/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp2Tcp.Common
{
    internal class Config
	{
        public const bool bUdpCheck = true;
        public const bool bUseSocketLock = false;
        public const bool bUseSendAsync = true;

        public const ushort nUdpMinOrderId = UdpNetCommand.COMMAND_MAX + 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;
		public const int nUdpPackageFixedSize = 1024;
		public const int nUdpPackageFixedHeadSize = 10;
        public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
        public const int nMaxDataLength = ushort.MaxValue;

		public readonly double fReceiveHeartBeatTimeOut = 5.0;
		public readonly double fMySendHeartBeatMaxTime = 2.0;
        public readonly double fReConnectMaxCdTime = 3.0;

        public readonly int client_socket_receiveBufferSize = 0;
        public readonly int server_socket_receiveBufferSize = 0;
        public readonly int MaxPlayerCount = 10000;

        //加解密
        public readonly ECryptoType nECryptoType = ECryptoType.None;
        public readonly string CryptoPasswrod1 = string.Empty;
        public readonly string CryptoPasswrod2 = string.Empty;

        public Config(Udp2TcpConfig mUserConfig = null)
        {
            NetLog.Assert(nUdpMaxOrderId - nUdpMinOrderId >= 1024);
            server_socket_receiveBufferSize = nUdpPackageFixedSize * MaxPlayerCount;
            client_socket_receiveBufferSize = nUdpPackageFixedSize * 64;

            if (mUserConfig != null)
            {
                if (mUserConfig.fReceiveHeartBeatTimeOut > 0)
                {
                    fReceiveHeartBeatTimeOut = mUserConfig.fReceiveHeartBeatTimeOut;
                }
                if (mUserConfig.fMySendHeartBeatMaxTime > 0)
                {
                    fMySendHeartBeatMaxTime = mUserConfig.fMySendHeartBeatMaxTime;
                }
                if (mUserConfig.fReConnectMaxCdTime > 0)
                {
                    fReConnectMaxCdTime = mUserConfig.fReConnectMaxCdTime;
                }
                if (mUserConfig.MaxPlayerCount > 0)
                {
                    MaxPlayerCount = mUserConfig.MaxPlayerCount;
                }

                client_socket_receiveBufferSize = Math.Max(client_socket_receiveBufferSize, mUserConfig.client_socket_receiveBufferSize);
                server_socket_receiveBufferSize = Math.Max(server_socket_receiveBufferSize, mUserConfig.server_socket_receiveBufferSize);

                nECryptoType = mUserConfig.nECryptoType;
                CryptoPasswrod1 = mUserConfig.CryptoPasswrod1;
                CryptoPasswrod2 = mUserConfig.CryptoPasswrod2;
            }
        }

	}
}
