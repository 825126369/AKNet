/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4Tcp.Common
{
    internal static class Config
	{
        public static readonly int nSocketCount = 1;
        public const bool bUseSocketAsyncEventArgsTwoComplete = true;
        public const bool bUseSingleSendArgs = true;

        public const int nUdpPackageFixedSize = 1400;
		public const int nUdpPackageFixedHeadSize = 12;
        public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
        public const int nMaxDataLength = ushort.MaxValue;

        public const uint nUdpMinOrderId = UdpNetCommand.COMMAND_MAX + 1;
        public const uint nUdpMaxOrderId = uint.MaxValue;

        public const double fReceiveHeartBeatTimeOut = 5.0;
		public const double fSendHeartBeatMaxTime = 2.0;
        public const double fReConnectMaxCdTime = 3.0;
        public const int MaxPlayerCount = 10000;
	}
}
