/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:21
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.LinuxTcp.Common
{
    public class Config
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
        public readonly int MaxPlayerCount = 10000;
	}
}
