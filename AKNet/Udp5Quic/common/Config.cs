/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal class Config
	{   
		public const int nUdpPackageFixedSize = 1400;
		public const int nUdpPackageFixedHeadSize = 14;
        public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
        public const int nMaxDataLength = ushort.MaxValue;

        public const uint nUdpMinOrderId = UdpNetCommand.COMMAND_MAX + 1;
        public const uint nUdpMaxOrderId = uint.MaxValue;

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

        public Config(Udp3TcpConfig mUserConfig = null)
        {
            server_socket_receiveBufferSize = 1024 * 64 * MaxPlayerCount;
            client_socket_receiveBufferSize = 1024 * 64;

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
