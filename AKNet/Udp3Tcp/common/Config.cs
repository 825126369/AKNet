/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp3Tcp.Common
{
    internal class Config
	{   
		public const int nUdpPackageFixedSize = 1024;
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
        public ECryptoType nECryptoType = ECryptoType.None;
	}
}
