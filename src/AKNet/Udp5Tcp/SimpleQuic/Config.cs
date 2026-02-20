/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp5Tcp.Common
{
    internal static class Config
	{
        public static readonly int nSocketCount = 1;
        public const bool bUseSocketAsyncEventArgsTwoComplete = true;
        public const bool bUseSingleSendArgs = false;

        public const int nUdpPackageFixedSize = CommonUdpLayerConfig.nUdpPackageFixedSize;
		public const int nUdpPackageFixedHeadSize = 12;
        public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
        public const int nMaxDataLength = CommonTcpLayerConfig.nDataMaxLength;

        public const uint nUdpMinOrderId = UdpNetCommand.COMMAND_MAX + 1;
        public const uint nUdpMaxOrderId = uint.MaxValue;
	}
}
