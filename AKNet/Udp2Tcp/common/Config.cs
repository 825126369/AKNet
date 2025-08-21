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

namespace AKNet.Udp2Tcp.Common
{
    public class Config
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
        public readonly int MaxPlayerCount = 10000;

        //加解密
        public ECryptoType nECryptoType = ECryptoType.None;
	}
}
