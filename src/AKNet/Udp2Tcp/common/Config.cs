/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp2Tcp.Common
{
    public static class Config
	{
        public const bool bUdpCheck = true;
        public const bool bUseSendAsync = true;

        public const ushort nUdpMinOrderId = UdpNetCommand.COMMAND_MAX + 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;
		public const int nUdpPackageFixedSize = 1400;
		public const int nUdpPackageFixedHeadSize = 8;
        public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
        public const int nMaxDataLength = ushort.MaxValue;

		public static readonly double fReceiveHeartBeatTimeOut = 5.0;
		public static readonly double fMySendHeartBeatMaxTime = 2.0;
        public static readonly double fReConnectMaxCdTime = 3.0;
        public static readonly int MaxPlayerCount = 10000;
	}
}
